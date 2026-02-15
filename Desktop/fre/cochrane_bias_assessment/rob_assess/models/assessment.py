"""Final study assessment combining extracted facts, algorithmic judgments, and confidence."""

from dataclasses import dataclass, field
from datetime import datetime

from .common import RoB2Domain, RoB2Judgment
from .extraction import StudyMethodologicalFacts
from .rob2 import RoB2Assessment


@dataclass
class DomainConfidence:
    """Confidence metrics for a single domain's extraction."""

    domain: RoB2Domain
    completeness: float = 0.0  # Fraction of fields with non-null values
    quote_coverage: float = 0.0  # Fraction of facts backed by quotes
    agent_confidence: float = 0.0  # Mean of individual fact confidences
    key_questions_answered: float = 0.0  # Whether critical SQs could be answered

    @property
    def overall(self) -> float:
        """Weighted average: key questions matter most."""
        return (
            self.completeness * 0.2
            + self.quote_coverage * 0.2
            + self.agent_confidence * 0.2
            + self.key_questions_answered * 0.4
        )

    @property
    def needs_review(self) -> bool:
        return self.overall < 0.7


@dataclass
class StudyAssessment:
    """Complete assessment output for a single study: facts + judgments + confidence."""

    study_id: str
    facts: StudyMethodologicalFacts | None = None
    assessment: RoB2Assessment | None = None
    confidence: dict[RoB2Domain, DomainConfidence] = field(default_factory=dict)
    pdf_path: str = ""
    assessed_at: str = field(default_factory=lambda: datetime.now().isoformat())
    model_used: str = ""
    tool_version: str = "0.1.0"

    @property
    def overall_confidence(self) -> float:
        if not self.confidence:
            return 0.0
        return sum(c.overall for c in self.confidence.values()) / len(self.confidence)

    @property
    def domains_needing_review(self) -> list[RoB2Domain]:
        return [d for d, c in self.confidence.items() if c.needs_review]

    @property
    def overall_judgment(self) -> RoB2Judgment | None:
        if self.assessment:
            return self.assessment.overall_judgment
        return None
