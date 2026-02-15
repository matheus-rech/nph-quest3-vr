"""Map extracted facts to signalling question answers.

This is the bridge between Phase 1 (extraction) and Phase 2 (algorithms).
Each mapper function translates raw extracted facts into SignallingAnswer
values for the domain's signalling questions.
"""

from rob_assess.models.common import SignallingAnswer
from rob_assess.models.extraction import (
    DeviationFacts,
    MissingDataFacts,
    OutcomeMeasurementFacts,
    RandomizationFacts,
    SelectiveReportingFacts,
)

_Y = SignallingAnswer.Y
_PY = SignallingAnswer.PY
_PN = SignallingAnswer.PN
_N = SignallingAnswer.N
_NI = SignallingAnswer.NI


def _bool_to_answer(value, positive_is_yes: bool = True) -> SignallingAnswer:
    """Convert an extracted boolean fact to a SignallingAnswer."""
    if value is None:
        return _NI
    if isinstance(value, bool):
        if positive_is_yes:
            return _Y if value else _N
        return _N if value else _Y
    # Handle string booleans from JSON
    if isinstance(value, str):
        lower = value.lower()
        if lower in ("true", "yes"):
            return _Y if positive_is_yes else _N
        if lower in ("false", "no"):
            return _N if positive_is_yes else _Y
    return _NI


def map_d1_facts(facts: RandomizationFacts) -> dict[str, SignallingAnswer]:
    """Map randomization facts to D1 signalling question answers."""
    return {
        "1.1": _bool_to_answer(facts.used_random_component.value),
        "1.2": _bool_to_answer(facts.concealment_adequate.value),
        "1.3": _bool_to_answer(facts.baseline_differences_problematic.value),
    }


def map_d2_facts(facts: DeviationFacts) -> dict[str, SignallingAnswer]:
    """Map deviation facts to D2 signalling question answers."""
    return {
        "2.1": _bool_to_answer(facts.participants_blinded.value, positive_is_yes=False),
        "2.2": _bool_to_answer(facts.carers_blinded.value, positive_is_yes=False),
        "2.3": _bool_to_answer(facts.context_dependent_deviations.value),
        "2.4": _bool_to_answer(facts.deviations_affected_outcome.value),
        "2.5": _bool_to_answer(facts.deviations_balanced.value),
        "2.6": _bool_to_answer(facts.appropriate_analysis.value),
        "2.7": _bool_to_answer(facts.substantial_impact_of_analysis_failure.value),
    }


def map_d3_facts(facts: MissingDataFacts) -> dict[str, SignallingAnswer]:
    """Map missing data facts to D3 signalling question answers."""
    return {
        "3.1": _bool_to_answer(facts.data_available_for_all.value),
        "3.2": _bool_to_answer(facts.evidence_result_not_biased.value),
        "3.3": _bool_to_answer(facts.missingness_depends_on_true_value.value),
        "3.4": _bool_to_answer(facts.missingness_likely_depends_on_true_value.value),
    }


def map_d4_facts(facts: OutcomeMeasurementFacts) -> dict[str, SignallingAnswer]:
    """Map outcome measurement facts to D4 signalling question answers."""
    return {
        "4.1": _bool_to_answer(facts.method_appropriate.value, positive_is_yes=False),
        "4.2": _bool_to_answer(facts.measurement_differed_between_groups.value),
        "4.3": _bool_to_answer(facts.assessors_blinded.value, positive_is_yes=False),
        "4.4": _bool_to_answer(facts.knowledge_could_influence.value),
        "4.5": _bool_to_answer(facts.knowledge_likely_influenced.value),
    }


def map_d5_facts(facts: SelectiveReportingFacts) -> dict[str, SignallingAnswer]:
    """Map selective reporting facts to D5 signalling question answers."""
    return {
        "5.1": _bool_to_answer(facts.plan_finalized_before_unblinding.value),
        "5.2": _bool_to_answer(facts.result_likely_selected.value),
        "5.3": _bool_to_answer(facts.multiple_analyses.value),
    }
