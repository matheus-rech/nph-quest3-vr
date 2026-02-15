"""Abstract base for RoB 2 domain algorithms."""

from abc import ABC, abstractmethod

from rob_assess.models.common import RoB2Domain, RoB2Judgment, SignallingAnswer
from rob_assess.models.rob2 import DomainAssessment, SignallingQuestion


class DomainAlgorithm(ABC):
    """Base class for a domain-level RoB 2 algorithm.

    Subclasses implement the exact Cochrane decision tree for their domain.
    The algorithm takes extracted facts, maps them to signalling question
    answers, then applies the decision tree to produce a judgment.
    """

    domain: RoB2Domain

    @abstractmethod
    def get_signalling_questions(self) -> list[SignallingQuestion]:
        """Return the signalling questions for this domain."""
        ...

    @abstractmethod
    def compute_judgment(self, answers: dict[str, SignallingAnswer]) -> RoB2Judgment:
        """Apply the Cochrane decision tree to produce a domain judgment.

        Args:
            answers: Map of question number (e.g. "1.1") to SignallingAnswer.

        Returns:
            The algorithmically determined RoB2Judgment.
        """
        ...

    def assess(self, answers: dict[str, SignallingAnswer]) -> DomainAssessment:
        """Run the full assessment: populate questions, compute judgment."""
        questions = self.get_signalling_questions()
        for q in questions:
            q.answer = answers.get(q.number)

        judgment = self.compute_judgment(answers)

        return DomainAssessment(
            domain=self.domain,
            signalling_questions=questions,
            algorithm_judgment=judgment,
        )

    @staticmethod
    def _get(answers: dict[str, SignallingAnswer], key: str) -> SignallingAnswer:
        """Get an answer, defaulting to NI if missing."""
        return answers.get(key, SignallingAnswer.NI)
