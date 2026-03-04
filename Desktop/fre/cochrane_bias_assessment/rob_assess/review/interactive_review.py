"""Rich-based interactive terminal UI for human review of assessments.

Shows low-confidence domains first. Reviewers can Accept/Override/Edit
each domain with justification tracking.
"""

import json
from pathlib import Path

from rich.console import Console
from rich.panel import Panel
from rich.prompt import Confirm, Prompt
from rich.table import Table

from rob_assess.models.common import RoB2Domain, RoB2Judgment

console = Console()

DOMAIN_ORDER = [RoB2Domain.D1, RoB2Domain.D2, RoB2Domain.D3, RoB2Domain.D4, RoB2Domain.D5]

JUDGMENT_COLORS = {
    "Low": "green",
    "Some concerns": "yellow",
    "High": "red",
}

VALID_JUDGMENTS = {
    "L": RoB2Judgment.LOW,
    "S": RoB2Judgment.SOME_CONCERNS,
    "H": RoB2Judgment.HIGH,
}


def _load_assessment(path: str) -> dict:
    """Load assessment JSON."""
    with open(path) as f:
        data = json.load(f)
    if "assessments" in data:
        return data["assessments"][0] if data["assessments"] else {}
    return data


def _display_domain_detail(domain_data: dict, domain_name: str):
    """Show signalling questions and answers for a domain."""
    table = Table(title=f"Domain: {domain_name}", show_header=True)
    table.add_column("Q#", style="bold", width=5)
    table.add_column("Question", width=60)
    table.add_column("Answer", width=10)
    table.add_column("Justification", width=30)

    sqs = domain_data.get("signalling_questions", [])
    for sq in sqs:
        answer = sq.get("answer", "N/A")
        table.add_row(
            sq.get("number", ""),
            sq.get("text", ""),
            str(answer),
            sq.get("justification", "")[:30],
        )

    console.print(table)

    judgment = domain_data.get("algorithm_judgment") or domain_data.get("reviewer_judgment")
    if judgment:
        color = JUDGMENT_COLORS.get(judgment, "white")
        console.print(f"\nAlgorithm judgment: [{color}]{judgment}[/{color}]")


def _review_domain(domain_data: dict, domain_name: str) -> dict:
    """Interactive review of a single domain."""
    _display_domain_detail(domain_data, domain_name)

    action = Prompt.ask(
        "\n[bold]Action[/bold]",
        choices=["accept", "override", "skip"],
        default="accept",
    )

    if action == "accept":
        console.print("[green]Accepted[/green]")
        return domain_data

    if action == "override":
        new_judgment = Prompt.ask(
            "New judgment ([L]ow / [S]ome concerns / [H]igh)",
            choices=["L", "S", "H"],
        )
        justification = Prompt.ask("Justification for override")
        domain_data["reviewer_judgment"] = VALID_JUDGMENTS[new_judgment].value
        domain_data["reviewer_override_justification"] = justification
        color = JUDGMENT_COLORS.get(VALID_JUDGMENTS[new_judgment].value, "white")
        console.print(f"[{color}]Overridden to: {VALID_JUDGMENTS[new_judgment].value}[/{color}]")
        return domain_data

    return domain_data


def run_review(assessment_path: str):
    """Run the interactive review session."""
    data = _load_assessment(assessment_path)
    study_id = data.get("study_id", "Unknown")

    console.print(
        Panel(
            f"Reviewing assessment for: [bold]{study_id}[/bold]\n"
            f"Low-confidence domains shown first.",
            title="Human Review",
            border_style="blue",
        )
    )

    assessment = data.get("assessment", {})
    domains = assessment.get("domains", {})
    confidence = data.get("confidence", {})

    # Sort domains by confidence (low first)
    domain_items = []
    for d in DOMAIN_ORDER:
        d_key = d.value
        d_data = domains.get(d_key, {})
        conf = confidence.get(d_key, {})
        overall_conf = conf.get("overall", 1.0) if isinstance(conf, dict) else 0.5
        domain_items.append((d, d_data, overall_conf))

    domain_items.sort(key=lambda x: x[2])

    for domain, d_data, conf in domain_items:
        conf_color = "red" if conf < 0.7 else "yellow" if conf < 0.85 else "green"
        console.print(
            f"\n{'='*60}\n"
            f"[bold]{domain.full_name}[/bold] "
            f"(Confidence: [{conf_color}]{conf:.0%}[/{conf_color}])"
        )

        if not d_data:
            console.print("[dim]No data available for this domain[/dim]")
            continue

        d_data = _review_domain(d_data, domain.full_name)
        domains[domain.value] = d_data

    # Overall judgment review
    overall = assessment.get("overall_algorithm_judgment")
    if overall:
        color = JUDGMENT_COLORS.get(overall, "white")
        console.print(
            f"\n{'='*60}\n"
            f"[bold]Overall Algorithm Judgment[/bold]: [{color}]{overall}[/{color}]"
        )

        if Confirm.ask("Override overall judgment?", default=False):
            new_overall = Prompt.ask(
                "New overall judgment ([L]ow / [S]ome concerns / [H]igh)",
                choices=["L", "S", "H"],
            )
            justification = Prompt.ask("Justification")
            assessment["overall_reviewer_judgment"] = VALID_JUDGMENTS[new_overall].value
            assessment["overall_override_justification"] = justification

    # Save reviewed assessment
    data["assessment"] = assessment
    output_path = Path(assessment_path).with_stem(
        Path(assessment_path).stem + "_reviewed"
    )
    with open(output_path, "w") as f:
        json.dump(data, f, indent=2)

    console.print(f"\n[green]Review saved to: {output_path}[/green]")
