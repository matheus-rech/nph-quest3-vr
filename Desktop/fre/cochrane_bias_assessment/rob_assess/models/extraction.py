"""Fact dataclasses for LLM-extracted methodological information.

These contain ONLY factual information extracted from papers — no judgments.
Each field includes an optional quote for provenance tracking.
"""

from dataclasses import dataclass, field


@dataclass
class ExtractedFact:
    """A single extracted fact with its supporting quote."""

    value: str | bool | float | None = None
    quote: str = ""
    confidence: float = 1.0  # Agent's confidence in extraction (0-1)


@dataclass
class RandomizationFacts:
    """Domain 1: Facts about the randomization process."""

    random_sequence_method: ExtractedFact = field(default_factory=ExtractedFact)
    used_random_component: ExtractedFact = field(default_factory=ExtractedFact)
    allocation_concealment_method: ExtractedFact = field(default_factory=ExtractedFact)
    concealment_adequate: ExtractedFact = field(default_factory=ExtractedFact)
    baseline_differences_reported: ExtractedFact = field(default_factory=ExtractedFact)
    baseline_differences_problematic: ExtractedFact = field(default_factory=ExtractedFact)
    group_size_imbalance: ExtractedFact = field(default_factory=ExtractedFact)
    excess_significant_differences: ExtractedFact = field(default_factory=ExtractedFact)


@dataclass
class DeviationFacts:
    """Domain 2: Facts about deviations from intended interventions."""

    participants_blinded: ExtractedFact = field(default_factory=ExtractedFact)
    carers_blinded: ExtractedFact = field(default_factory=ExtractedFact)
    context_dependent_deviations: ExtractedFact = field(default_factory=ExtractedFact)
    deviations_affected_outcome: ExtractedFact = field(default_factory=ExtractedFact)
    deviations_balanced: ExtractedFact = field(default_factory=ExtractedFact)
    appropriate_analysis: ExtractedFact = field(default_factory=ExtractedFact)
    analysis_type: ExtractedFact = field(default_factory=ExtractedFact)  # ITT, per-protocol, etc.
    substantial_impact_of_analysis_failure: ExtractedFact = field(default_factory=ExtractedFact)


@dataclass
class MissingDataFacts:
    """Domain 3: Facts about missing outcome data."""

    data_available_for_all: ExtractedFact = field(default_factory=ExtractedFact)
    proportion_missing: ExtractedFact = field(default_factory=ExtractedFact)
    reasons_for_missingness: ExtractedFact = field(default_factory=ExtractedFact)
    missingness_balanced: ExtractedFact = field(default_factory=ExtractedFact)
    evidence_result_not_biased: ExtractedFact = field(default_factory=ExtractedFact)
    sensitivity_analysis_done: ExtractedFact = field(default_factory=ExtractedFact)
    missingness_depends_on_true_value: ExtractedFact = field(default_factory=ExtractedFact)
    missingness_likely_depends_on_true_value: ExtractedFact = field(default_factory=ExtractedFact)


@dataclass
class OutcomeMeasurementFacts:
    """Domain 4: Facts about measurement of the outcome."""

    measurement_method: ExtractedFact = field(default_factory=ExtractedFact)
    method_appropriate: ExtractedFact = field(default_factory=ExtractedFact)
    measurement_differed_between_groups: ExtractedFact = field(default_factory=ExtractedFact)
    assessors_blinded: ExtractedFact = field(default_factory=ExtractedFact)
    outcome_type: ExtractedFact = field(default_factory=ExtractedFact)  # objective vs subjective
    knowledge_could_influence: ExtractedFact = field(default_factory=ExtractedFact)
    knowledge_likely_influenced: ExtractedFact = field(default_factory=ExtractedFact)


@dataclass
class SelectiveReportingFacts:
    """Domain 5: Facts about selection of the reported result."""

    protocol_available: ExtractedFact = field(default_factory=ExtractedFact)
    trial_registered: ExtractedFact = field(default_factory=ExtractedFact)
    registration_id: ExtractedFact = field(default_factory=ExtractedFact)
    analysis_plan_pre_specified: ExtractedFact = field(default_factory=ExtractedFact)
    plan_finalized_before_unblinding: ExtractedFact = field(default_factory=ExtractedFact)
    multiple_outcome_measurements: ExtractedFact = field(default_factory=ExtractedFact)
    multiple_analyses: ExtractedFact = field(default_factory=ExtractedFact)
    result_likely_selected: ExtractedFact = field(default_factory=ExtractedFact)


@dataclass
class StudyMethodologicalFacts:
    """All extracted methodological facts for one study."""

    study_id: str
    title: str = ""
    authors: str = ""
    year: int | None = None
    journal: str = ""
    randomization: RandomizationFacts = field(default_factory=RandomizationFacts)
    deviations: DeviationFacts = field(default_factory=DeviationFacts)
    missing_data: MissingDataFacts = field(default_factory=MissingDataFacts)
    outcome_measurement: OutcomeMeasurementFacts = field(default_factory=OutcomeMeasurementFacts)
    selective_reporting: SelectiveReportingFacts = field(default_factory=SelectiveReportingFacts)
