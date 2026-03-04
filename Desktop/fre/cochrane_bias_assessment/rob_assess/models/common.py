"""Core enums and types shared across the RoB assessment framework."""

from enum import Enum


class ToolType(str, Enum):
    """Which Cochrane risk-of-bias tool is being used."""

    ROB2 = "ROB2"
    ROBINS_I = "ROBINS-I"
    ROBINS_E = "ROBINS-E"


class EffectOfInterest(str, Enum):
    """The causal estimand being assessed (determines Domain 2 questions)."""

    ASSIGNMENT = "effect_of_assignment"  # Intention-to-treat
    ADHERENCE = "effect_of_adherence"  # Per-protocol


class SignallingAnswer(str, Enum):
    """Response options for RoB 2 signalling questions.

    Y/PY have identical implications for risk-of-bias judgments,
    as do N/PN. NI indicates the information was not reported.
    """

    Y = "Y"  # Yes
    PY = "PY"  # Probably yes
    PN = "PN"  # Probably no
    N = "N"  # No
    NI = "NI"  # No information

    @property
    def is_yes(self) -> bool:
        return self in (SignallingAnswer.Y, SignallingAnswer.PY)

    @property
    def is_no(self) -> bool:
        return self in (SignallingAnswer.N, SignallingAnswer.PN)

    @property
    def is_yes_or_ni(self) -> bool:
        return self.is_yes or self == SignallingAnswer.NI

    @property
    def is_no_or_ni(self) -> bool:
        return self.is_no or self == SignallingAnswer.NI


class RoB2Judgment(str, Enum):
    """Domain-level and overall risk-of-bias judgments for RoB 2."""

    LOW = "Low"
    SOME_CONCERNS = "Some concerns"
    HIGH = "High"


class RoB2Domain(str, Enum):
    """The five bias domains in RoB 2."""

    D1 = "D1"  # Randomization process
    D2 = "D2"  # Deviations from intended interventions
    D3 = "D3"  # Missing outcome data
    D4 = "D4"  # Measurement of the outcome
    D5 = "D5"  # Selection of the reported result

    @property
    def full_name(self) -> str:
        names = {
            "D1": "Bias arising from the randomization process",
            "D2": "Bias due to deviations from intended interventions",
            "D3": "Bias due to missing outcome data",
            "D4": "Bias in measurement of the outcome",
            "D5": "Bias in selection of the reported result",
        }
        return names[self.value]
