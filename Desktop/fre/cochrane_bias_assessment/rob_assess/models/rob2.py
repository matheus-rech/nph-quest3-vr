"""RoB 2 assessment dataclasses — signalling questions, domain assessments, full assessments."""

from dataclasses import dataclass, field

from .common import RoB2Domain, RoB2Judgment, SignallingAnswer


@dataclass
class SignallingQuestion:
    """A single signalling question with its response and justification."""

    number: str  # e.g. "1.1", "2.3"
    text: str
    answer: SignallingAnswer | None = None
    justification: str = ""
    quotes: list[str] = field(default_factory=list)


@dataclass
class DomainAssessment:
    """Assessment for one RoB 2 domain."""

    domain: RoB2Domain
    signalling_questions: list[SignallingQuestion] = field(default_factory=list)
    algorithm_judgment: RoB2Judgment | None = None
    reviewer_judgment: RoB2Judgment | None = None
    reviewer_override_justification: str = ""

    @property
    def judgment(self) -> RoB2Judgment | None:
        """Final judgment: reviewer override takes precedence."""
        return self.reviewer_judgment or self.algorithm_judgment

    @property
    def is_overridden(self) -> bool:
        return (
            self.reviewer_judgment is not None
            and self.algorithm_judgment is not None
            and self.reviewer_judgment != self.algorithm_judgment
        )

    def answers_dict(self) -> dict[str, SignallingAnswer | None]:
        """Map question numbers to answers for algorithm consumption."""
        return {q.number: q.answer for q in self.signalling_questions}


@dataclass
class RoB2Assessment:
    """Complete RoB 2 assessment for a single result from a single study."""

    study_id: str
    result_description: str = ""
    domains: dict[RoB2Domain, DomainAssessment] = field(default_factory=dict)
    overall_algorithm_judgment: RoB2Judgment | None = None
    overall_reviewer_judgment: RoB2Judgment | None = None
    overall_override_justification: str = ""

    @property
    def overall_judgment(self) -> RoB2Judgment | None:
        return self.overall_reviewer_judgment or self.overall_algorithm_judgment

    def domain_judgments(self) -> dict[RoB2Domain, RoB2Judgment | None]:
        return {domain: assessment.judgment for domain, assessment in self.domains.items()}
