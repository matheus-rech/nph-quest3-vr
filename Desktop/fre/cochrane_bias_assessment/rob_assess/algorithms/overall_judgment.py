"""Overall RoB 2 judgment algorithm.

Cochrane rules (Section 1.4):
- LOW: Low risk of bias across ALL five domains
- SOME CONCERNS: Some concerns in at least one domain, but not high risk in any
- HIGH: High risk in at least one domain, OR some concerns for multiple domains
        in a way that substantially lowers confidence in the result
"""

from rob_assess.models.common import RoB2Domain, RoB2Judgment
from rob_assess.models.rob2 import DomainAssessment, RoB2Assessment

_LOW = RoB2Judgment.LOW
_SC = RoB2Judgment.SOME_CONCERNS
_HIGH = RoB2Judgment.HIGH

# Threshold: this many SOME_CONCERNS domains → HIGH overall
MULTI_CONCERN_THRESHOLD = 3


def compute_overall_judgment(
    domain_judgments: dict[RoB2Domain, RoB2Judgment],
) -> RoB2Judgment:
    """Apply the overall judgment algorithm.

    Args:
        domain_judgments: Judgment for each of the 5 domains.

    Returns:
        The overall RoB 2 judgment.
    """
    judgments = list(domain_judgments.values())

    if not judgments:
        return _SC

    # HIGH if any domain is HIGH
    if any(j == _HIGH for j in judgments):
        return _HIGH

    # HIGH if 3+ domains have SOME_CONCERNS (substantially lowers confidence)
    some_concerns_count = sum(1 for j in judgments if j == _SC)
    if some_concerns_count >= MULTI_CONCERN_THRESHOLD:
        return _HIGH

    # SOME CONCERNS if any domain has SOME_CONCERNS
    if any(j == _SC for j in judgments):
        return _SC

    # All LOW → LOW
    return _LOW


def compute_full_assessment(
    study_id: str,
    domain_assessments: dict[RoB2Domain, DomainAssessment],
    result_description: str = "",
) -> RoB2Assessment:
    """Build a complete RoB2Assessment from domain assessments."""
    domain_judgments = {}
    for domain, assessment in domain_assessments.items():
        if assessment.judgment is not None:
            domain_judgments[domain] = assessment.judgment

    overall = compute_overall_judgment(domain_judgments)

    return RoB2Assessment(
        study_id=study_id,
        result_description=result_description,
        domains=domain_assessments,
        overall_algorithm_judgment=overall,
    )
