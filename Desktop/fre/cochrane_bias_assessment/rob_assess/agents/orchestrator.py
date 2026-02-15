"""Pipeline orchestrator: PDF -> Extraction -> Algorithms -> Assessment.

Runs 5 domain extraction agents in parallel, maps facts to signalling
question answers, runs deterministic algorithms, computes confidence.
"""

import asyncio
import dataclasses
import logging
import time
from pathlib import Path

from anthropic import AsyncAnthropic

from rob_assess.agents.base_agent import ExtractionResult
from rob_assess.agents.confidence_scorer import score_domain_confidence
from rob_assess.agents.domain_agents.d1_randomization import D1RandomizationAgent
from rob_assess.agents.domain_agents.d2_deviations import D2DeviationsAgent
from rob_assess.agents.domain_agents.d3_missing_data import D3MissingDataAgent
from rob_assess.agents.domain_agents.d4_outcome_measurement import D4OutcomeMeasurementAgent
from rob_assess.agents.domain_agents.d5_selective_reporting import D5SelectiveReportingAgent
from rob_assess.agents.fact_mapper import (
    map_d1_facts,
    map_d2_facts,
    map_d3_facts,
    map_d4_facts,
    map_d5_facts,
)
from rob_assess.algorithms.overall_judgment import compute_full_assessment
from rob_assess.algorithms.rob2_algorithms import DOMAIN_ALGORITHMS
from rob_assess.extraction.pdf_reader import extract_text
from rob_assess.extraction.text_chunker import (
    chunk_by_sections,
    get_domain_text,
)
from rob_assess.models.assessment import StudyAssessment
from rob_assess.models.common import RoB2Domain
from rob_assess.models.extraction import (
    DeviationFacts,
    ExtractedFact,
    MissingDataFacts,
    OutcomeMeasurementFacts,
    RandomizationFacts,
    SelectiveReportingFacts,
    StudyMethodologicalFacts,
)

logger = logging.getLogger(__name__)

# Maps domain enum -> (agent domain_name, facts class, fact mapper, attribute name)
_DOMAIN_REGISTRY = {
    RoB2Domain.D1: ("D1_randomization", RandomizationFacts, map_d1_facts, "randomization"),
    RoB2Domain.D2: ("D2_deviations", DeviationFacts, map_d2_facts, "deviations"),
    RoB2Domain.D3: ("D3_missing_data", MissingDataFacts, map_d3_facts, "missing_data"),
    RoB2Domain.D4: ("D4_outcome_measurement", OutcomeMeasurementFacts, map_d4_facts, "outcome_measurement"),
    RoB2Domain.D5: ("D5_selective_reporting", SelectiveReportingFacts, map_d5_facts, "selective_reporting"),
}


def _result_to_facts(result: ExtractionResult) -> list[ExtractedFact]:
    """Convert raw JSON extraction result to a list of ExtractedFact objects."""
    facts = []
    for key, val in result.facts_json.items():
        if isinstance(val, dict):
            facts.append(
                ExtractedFact(
                    value=val.get("value"),
                    quote=val.get("quote", ""),
                    confidence=val.get("confidence", 1.0),
                )
            )
    return facts


def _populate_domain_facts(facts_cls, result: ExtractionResult):
    """Populate a domain facts dataclass from extraction JSON."""
    instance = facts_cls()
    for field_info in dataclasses.fields(facts_cls):
        if field_info.name in result.facts_json:
            val = result.facts_json[field_info.name]
            if isinstance(val, dict):
                setattr(
                    instance,
                    field_info.name,
                    ExtractedFact(
                        value=val.get("value"),
                        quote=val.get("quote", ""),
                        confidence=val.get("confidence", 1.0),
                    ),
                )
    return instance


async def assess_pdf(
    pdf_path: str | Path,
    study_id: str | None = None,
    model: str | None = None,
) -> StudyAssessment:
    """Run the full GEPA pipeline on a single PDF.

    Phase 1: Extract facts with 5 parallel domain agents (LLM)
    Phase 2: Map facts -> signalling answers -> algorithms (deterministic)

    Args:
        pdf_path: Path to the RCT paper PDF.
        study_id: Identifier for this study (defaults to filename).
        model: Anthropic model to use.

    Returns:
        Complete StudyAssessment with timing metrics.
    """
    pdf_path = Path(pdf_path)
    study_id = study_id or pdf_path.stem
    pipeline_start = time.monotonic()

    # === Phase 0: Extract and chunk text ===
    logger.info(f"Extracting text from {pdf_path}")
    t0 = time.monotonic()
    full_text = extract_text(pdf_path)
    sections = chunk_by_sections(full_text)
    text_extraction_time = time.monotonic() - t0
    logger.info(f"Text extraction: {text_extraction_time:.2f}s, {len(full_text)} chars, {len(sections)} sections")

    # === Phase 1: Parallel extraction with shared client ===
    client = AsyncAnthropic()
    agents = [
        D1RandomizationAgent(model=model, client=client),
        D2DeviationsAgent(model=model, client=client),
        D3MissingDataAgent(model=model, client=client),
        D4OutcomeMeasurementAgent(model=model, client=client),
        D5SelectiveReportingAgent(model=model, client=client),
    ]

    # Route only relevant sections to each agent
    domain_texts = {
        agent.domain_name: get_domain_text(agent.domain_name, sections, full_text)
        for agent in agents
    }

    logger.info("Running 5 domain extraction agents in parallel (targeted sections)")
    t1 = time.monotonic()

    extraction_results: list[ExtractionResult | BaseException] = await asyncio.gather(
        *[agent.extract(domain_texts[agent.domain_name]) for agent in agents],
        return_exceptions=True,
    )

    extraction_time = time.monotonic() - t1

    # Collect results and track metrics
    results_by_domain: dict[str, ExtractionResult] = {}
    total_input_tokens = 0
    total_output_tokens = 0
    for result in extraction_results:
        if isinstance(result, BaseException):
            logger.error(f"Extraction failed: {result}")
            continue
        results_by_domain[result.domain] = result
        total_input_tokens += result.input_tokens
        total_output_tokens += result.output_tokens

    logger.info(
        f"Extraction: {extraction_time:.2f}s, "
        f"{total_input_tokens} input tokens, {total_output_tokens} output tokens, "
        f"{len(results_by_domain)}/5 domains succeeded"
    )

    # === Phase 1.5: Build facts dataclasses ===
    study_facts = StudyMethodologicalFacts(study_id=study_id)

    for domain, (agent_key, facts_cls, _, attr_name) in _DOMAIN_REGISTRY.items():
        if agent_key in results_by_domain:
            facts_instance = _populate_domain_facts(facts_cls, results_by_domain[agent_key])
            setattr(study_facts, attr_name, facts_instance)

    # === Phase 2: Map facts -> signalling answers -> algorithms ===
    logger.info("Applying deterministic algorithms")
    t2 = time.monotonic()

    domain_assessments = {}
    confidence_scores = {}

    for domain, (agent_key, _, mapper_fn, attr_name) in _DOMAIN_REGISTRY.items():
        facts_instance = getattr(study_facts, attr_name)

        # Map facts to signalling question answers
        sq_answers = mapper_fn(facts_instance)

        # Run the deterministic algorithm
        algorithm = DOMAIN_ALGORITHMS[domain]()
        domain_assessment = algorithm.assess(sq_answers)
        domain_assessments[domain] = domain_assessment

        # Compute confidence from extracted facts
        extracted_facts = (
            _result_to_facts(results_by_domain[agent_key])
            if agent_key in results_by_domain
            else []
        )
        confidence_scores[domain] = score_domain_confidence(
            domain, domain_assessment, extracted_facts
        )

    algorithm_time = time.monotonic() - t2

    # === Compute overall judgment ===
    rob2_assessment = compute_full_assessment(study_id, domain_assessments)

    total_time = time.monotonic() - pipeline_start
    logger.info(
        f"Pipeline complete: {total_time:.2f}s total "
        f"(text: {text_extraction_time:.2f}s, "
        f"extraction: {extraction_time:.2f}s, "
        f"algorithms: {algorithm_time:.4f}s)"
    )

    model_used = model or agents[0].model

    return StudyAssessment(
        study_id=study_id,
        facts=study_facts,
        assessment=rob2_assessment,
        confidence=confidence_scores,
        pdf_path=str(pdf_path),
        model_used=model_used,
    )


async def assess_batch(
    pdf_paths: list[str | Path],
    concurrency: int = 3,
    model: str | None = None,
) -> list[StudyAssessment]:
    """Assess multiple PDFs with controlled concurrency.

    Args:
        pdf_paths: List of PDF file paths.
        concurrency: Max number of PDFs to process simultaneously.
        model: Anthropic model to use.

    Returns:
        List of StudyAssessments.
    """
    semaphore = asyncio.Semaphore(concurrency)

    async def _limited_assess(pdf_path):
        async with semaphore:
            try:
                return await assess_pdf(pdf_path, model=model)
            except Exception as e:
                logger.error(f"Failed to assess {pdf_path}: {e}")
                return StudyAssessment(
                    study_id=Path(pdf_path).stem,
                    pdf_path=str(pdf_path),
                )

    return await asyncio.gather(*[_limited_assess(p) for p in pdf_paths])
