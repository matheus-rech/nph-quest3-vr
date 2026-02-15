"""Per-domain confidence scoring based on extraction quality."""

from rob_assess.models.assessment import DomainConfidence
from rob_assess.models.common import RoB2Domain, SignallingAnswer
from rob_assess.models.extraction import ExtractedFact
from rob_assess.models.rob2 import DomainAssessment

# Key signalling questions per domain (most important for judgment)
KEY_QUESTIONS = {
    RoB2Domain.D1: ["1.1", "1.2", "1.3"],
    RoB2Domain.D2: ["2.1", "2.2", "2.6"],
    RoB2Domain.D3: ["3.1"],
    RoB2Domain.D4: ["4.1", "4.2", "4.3"],
    RoB2Domain.D5: ["5.1"],
}


def score_domain_confidence(
    domain: RoB2Domain,
    domain_assessment: DomainAssessment,
    facts: list[ExtractedFact],
) -> DomainConfidence:
    """Score confidence for a domain based on extraction quality.

    Args:
        domain: Which domain.
        domain_assessment: The assessment with signalling question answers.
        facts: The extracted facts for this domain.

    Returns:
        DomainConfidence with sub-scores.
    """
    # Completeness: fraction of facts with non-null values
    non_null = sum(1 for f in facts if f.value is not None)
    completeness = non_null / len(facts) if facts else 0.0

    # Quote coverage: fraction of non-null facts backed by quotes
    quoted = sum(1 for f in facts if f.value is not None and f.quote)
    quote_coverage = quoted / non_null if non_null else 0.0

    # Agent confidence: mean of individual fact confidences
    confidences = [f.confidence for f in facts if f.value is not None]
    agent_confidence = sum(confidences) / len(confidences) if confidences else 0.0

    # Key questions answered: fraction of key SQs with definitive answers (not NI)
    key_qs = KEY_QUESTIONS.get(domain, [])
    answers = domain_assessment.answers_dict()
    answered = sum(
        1 for q in key_qs
        if answers.get(q) is not None and answers[q] != SignallingAnswer.NI
    )
    key_questions_answered = answered / len(key_qs) if key_qs else 0.0

    return DomainConfidence(
        domain=domain,
        completeness=completeness,
        quote_coverage=quote_coverage,
        agent_confidence=agent_confidence,
        key_questions_answered=key_questions_answered,
    )
