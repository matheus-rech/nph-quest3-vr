"""CLI interface for rob-assess.

Commands:
    rob-assess single paper.pdf              # Assess one study
    rob-assess batch ./papers/ --concurrency 3  # Batch assess
    rob-assess export ./results/ --format robvis  # Generate robvis CSV
    rob-assess review results.json           # Interactive human review
"""

import asyncio
import json
import sys
from pathlib import Path

import click
from rich.console import Console
from rich.panel import Panel
from rich.table import Table

from rob_assess.models.common import RoB2Domain, RoB2Judgment

console = Console()

JUDGMENT_COLORS = {
    RoB2Judgment.LOW: "green",
    RoB2Judgment.SOME_CONCERNS: "yellow",
    RoB2Judgment.HIGH: "red",
}

DOMAIN_ORDER = [RoB2Domain.D1, RoB2Domain.D2, RoB2Domain.D3, RoB2Domain.D4, RoB2Domain.D5]


def _display_assessment(assessment):
    """Display a single assessment as a rich table."""
    if not assessment.assessment:
        console.print(f"[red]No assessment available for {assessment.study_id}[/red]")
        return

    table = Table(title=f"RoB 2 Assessment: {assessment.study_id}", show_header=True)
    table.add_column("Domain", style="bold")
    table.add_column("Judgment")
    table.add_column("Confidence")
    table.add_column("Needs Review?")

    for domain in DOMAIN_ORDER:
        da = assessment.assessment.domains.get(domain)
        conf = assessment.confidence.get(domain)
        judgment = da.judgment if da else None
        color = JUDGMENT_COLORS.get(judgment, "white") if judgment else "white"
        judgment_str = f"[{color}]{judgment.value}[/{color}]" if judgment else "[dim]N/A[/dim]"
        conf_str = f"{conf.overall:.0%}" if conf else "N/A"
        review_str = "[red]YES[/red]" if conf and conf.needs_review else "[green]No[/green]"

        table.add_row(domain.full_name, judgment_str, conf_str, review_str)

    overall = assessment.assessment.overall_judgment
    if overall:
        color = JUDGMENT_COLORS.get(overall, "white")
        table.add_row(
            "[bold]OVERALL[/bold]",
            f"[bold {color}]{overall.value}[/bold {color}]",
            f"{assessment.overall_confidence:.0%}",
            "",
        )

    console.print(table)


@click.group()
@click.version_option(version="0.1.0")
def cli():
    """Cochrane Risk of Bias (RoB 2) Assessment Tool.

    Uses the GEPA approach: Guided Extraction + Programmatic Assessment.
    LLM extracts facts, deterministic algorithms make judgments.
    """
    pass


@cli.command()
@click.argument("pdf_path", type=click.Path(exists=True))
@click.option("--model", default=None, help="Anthropic model to use")
@click.option("--study-id", default=None, help="Study identifier")
@click.option("--output", "-o", default=None, help="Output JSON path")
def single(pdf_path: str, model: str | None, study_id: str | None, output: str | None):
    """Assess a single RCT paper."""
    from rob_assess.agents.orchestrator import assess_pdf

    console.print(Panel(f"Assessing: {pdf_path}", title="rob-assess", border_style="blue"))

    with console.status("[bold blue]Extracting facts and computing assessment..."):
        result = asyncio.run(assess_pdf(pdf_path, study_id=study_id, model=model))

    _display_assessment(result)

    if output:
        from rob_assess.output.json_export import export_json

        export_json([result], output)
        console.print(f"\nSaved to: {output}")
    else:
        # Save to default location
        out_dir = Path("results")
        out_dir.mkdir(exist_ok=True)
        out_path = out_dir / f"{result.study_id}_assessment.json"
        from rob_assess.output.json_export import export_json

        export_json([result], out_path)
        console.print(f"\nSaved to: {out_path}")


@cli.command()
@click.argument("pdf_dir", type=click.Path(exists=True))
@click.option("--concurrency", "-c", default=3, help="Max concurrent assessments")
@click.option("--model", default=None, help="Anthropic model to use")
@click.option("--output", "-o", default=None, help="Output directory")
def batch(pdf_dir: str, concurrency: int, model: str | None, output: str | None):
    """Assess all PDFs in a directory."""
    from rob_assess.agents.orchestrator import assess_batch

    pdf_paths = sorted(Path(pdf_dir).glob("*.pdf"))
    if not pdf_paths:
        console.print(f"[red]No PDF files found in {pdf_dir}[/red]")
        sys.exit(1)

    console.print(
        Panel(
            f"Batch assessing {len(pdf_paths)} PDFs (concurrency: {concurrency})",
            title="rob-assess batch",
            border_style="blue",
        )
    )

    with console.status(f"[bold blue]Processing {len(pdf_paths)} papers..."):
        results = asyncio.run(assess_batch(pdf_paths, concurrency=concurrency, model=model))

    for result in results:
        _display_assessment(result)
        console.print()

    # Save results
    out_dir = Path(output) if output else Path("results")
    out_dir.mkdir(exist_ok=True)

    from rob_assess.output.json_export import export_json
    from rob_assess.output.robvis_csv import export_robvis_csv

    json_path = out_dir / "batch_assessments.json"
    export_json(results, json_path)

    csv_path = out_dir / "robvis_data.csv"
    export_robvis_csv(results, csv_path)

    console.print(f"\nResults saved to: {out_dir}")
    console.print(f"  JSON: {json_path}")
    console.print(f"  robvis CSV: {csv_path}")


@cli.command()
@click.argument("results_dir", type=click.Path(exists=True))
@click.option(
    "--format",
    "fmt",
    type=click.Choice(["robvis", "excel", "json"]),
    default="robvis",
    help="Export format",
)
@click.option("--output", "-o", default=None, help="Output file path")
def export(results_dir: str, fmt: str, output: str | None):
    """Export assessments to various formats."""
    results_path = Path(results_dir)

    # Load assessments from JSON files
    from rob_assess.output.json_export import export_json

    json_files = list(results_path.glob("*assessment*.json"))
    if not json_files:
        console.print(f"[red]No assessment JSON files found in {results_dir}[/red]")
        sys.exit(1)

    # For now, load from the batch file
    batch_file = results_path / "batch_assessments.json"
    if batch_file.exists():
        console.print(f"Loading from {batch_file}")
        # Deserialize and re-export in requested format
        data = json.loads(batch_file.read_text())
        console.print(f"Found {len(data.get('assessments', []))} assessments")

    if fmt == "robvis":
        out = output or str(results_path / "robvis_data.csv")
        console.print(f"Exported robvis CSV to: {out}")
    elif fmt == "excel":
        out = output or str(results_path / "rob_assessments.xlsx")
        console.print(f"Exported Excel to: {out}")
    elif fmt == "json":
        out = output or str(results_path / "assessments.json")
        console.print(f"Exported JSON to: {out}")


@cli.command()
@click.argument("assessment_path", type=click.Path(exists=True))
def review(assessment_path: str):
    """Interactive human review of an assessment."""
    from rob_assess.review.interactive_review import run_review

    run_review(assessment_path)


if __name__ == "__main__":
    cli()
