"""Tests for Domain 1: Bias arising from the randomization process.

Tests every branch of the decision tree:
- LOW: random AND concealed AND baseline OK
- SOME CONCERNS: NI on randomization/concealment, no baseline issues
- HIGH: not random/concealed AND baseline problems
"""

import pytest

from rob_assess.algorithms.rob2_algorithms import D1RandomizationAlgorithm
from rob_assess.models.common import RoB2Judgment, SignallingAnswer

_Y = SignallingAnswer.Y
_PY = SignallingAnswer.PY
_PN = SignallingAnswer.PN
_N = SignallingAnswer.N
_NI = SignallingAnswer.NI
_LOW = RoB2Judgment.LOW
_SC = RoB2Judgment.SOME_CONCERNS
_HIGH = RoB2Judgment.HIGH

algo = D1RandomizationAlgorithm()


def _answers(q11, q12, q13):
    return {"1.1": q11, "1.2": q12, "1.3": q13}


class TestD1Low:
    """LOW: random AND concealed AND baseline compatible with chance."""

    def test_all_yes_no(self):
        assert algo.compute_judgment(_answers(_Y, _Y, _N)) == _LOW

    def test_probably_yes_random(self):
        assert algo.compute_judgment(_answers(_PY, _Y, _N)) == _LOW

    def test_probably_yes_concealed(self):
        assert algo.compute_judgment(_answers(_Y, _PY, _N)) == _LOW

    def test_both_probably_yes(self):
        assert algo.compute_judgment(_answers(_PY, _PY, _N)) == _LOW

    def test_probably_no_baseline(self):
        assert algo.compute_judgment(_answers(_Y, _Y, _PN)) == _LOW


class TestD1SomeConcerns:
    """SOME CONCERNS: NI on randomization/concealment, no baseline problems."""

    def test_ni_randomization(self):
        assert algo.compute_judgment(_answers(_NI, _Y, _N)) == _SC

    def test_ni_concealment(self):
        assert algo.compute_judgment(_answers(_Y, _NI, _N)) == _SC

    def test_both_ni(self):
        assert algo.compute_judgment(_answers(_NI, _NI, _N)) == _SC

    def test_ni_randomization_ni_baseline(self):
        assert algo.compute_judgment(_answers(_NI, _Y, _NI)) == _SC

    def test_ni_concealment_ni_baseline(self):
        assert algo.compute_judgment(_answers(_Y, _NI, _NI)) == _SC

    def test_random_but_not_concealed_no_baseline_issues(self):
        assert algo.compute_judgment(_answers(_Y, _N, _N)) == _SC

    def test_concealed_but_not_random_no_baseline_issues(self):
        assert algo.compute_judgment(_answers(_N, _Y, _N)) == _SC


class TestD1High:
    """HIGH: not random/concealed AND baseline problems."""

    def test_not_random_baseline_problems(self):
        assert algo.compute_judgment(_answers(_N, _Y, _Y)) == _HIGH

    def test_not_concealed_baseline_problems(self):
        assert algo.compute_judgment(_answers(_Y, _N, _Y)) == _HIGH

    def test_both_no_baseline_problems(self):
        assert algo.compute_judgment(_answers(_N, _N, _Y)) == _HIGH

    def test_ni_randomization_baseline_problems(self):
        assert algo.compute_judgment(_answers(_NI, _Y, _Y)) == _HIGH

    def test_ni_concealment_baseline_problems(self):
        assert algo.compute_judgment(_answers(_Y, _NI, _Y)) == _HIGH

    def test_baseline_problems_override(self):
        """Baseline problems alone → HIGH even if random+concealed."""
        assert algo.compute_judgment(_answers(_Y, _Y, _Y)) == _HIGH

    def test_not_random_not_concealed_no_baseline(self):
        """Not random AND not concealed → HIGH even without baseline issues."""
        assert algo.compute_judgment(_answers(_N, _N, _N)) == _HIGH

    def test_probably_not_random_baseline_problems(self):
        assert algo.compute_judgment(_answers(_PN, _Y, _Y)) == _HIGH

    def test_probably_not_concealed_baseline_problems(self):
        assert algo.compute_judgment(_answers(_Y, _PN, _Y)) == _HIGH


class TestD1Assessment:
    """Test the full assess() method."""

    def test_assess_returns_domain_assessment(self):
        result = algo.assess(_answers(_Y, _Y, _N))
        assert result.domain.value == "D1"
        assert result.algorithm_judgment == _LOW
        assert len(result.signalling_questions) == 3
        assert result.signalling_questions[0].answer == _Y
