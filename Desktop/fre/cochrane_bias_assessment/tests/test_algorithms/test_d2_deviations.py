"""Tests for Domain 2: Bias due to deviations from intended interventions.

Decision tree (effect of assignment / ITT):
- LOW: Blinded OR no deviations OR appropriate analysis
- SOME CONCERNS: Some awareness, no substantial impact
- HIGH: Unblinded + deviations affected outcome + bad analysis
"""

import pytest

from rob_assess.algorithms.rob2_algorithms import D2DeviationsAlgorithm
from rob_assess.models.common import RoB2Judgment, SignallingAnswer

_Y = SignallingAnswer.Y
_PY = SignallingAnswer.PY
_PN = SignallingAnswer.PN
_N = SignallingAnswer.N
_NI = SignallingAnswer.NI
_LOW = RoB2Judgment.LOW
_SC = RoB2Judgment.SOME_CONCERNS
_HIGH = RoB2Judgment.HIGH

algo = D2DeviationsAlgorithm()


def _answers(q21=_NI, q22=_NI, q23=_NI, q24=_NI, q25=_NI, q26=_NI, q27=_NI):
    return {
        "2.1": q21, "2.2": q22, "2.3": q23, "2.4": q24,
        "2.5": q25, "2.6": q26, "2.7": q27,
    }


class TestD2Blinded:
    """When both participants and carers are blinded."""

    def test_double_blind_appropriate_analysis(self):
        assert algo.compute_judgment(_answers(_N, _N, q26=_Y)) == _LOW

    def test_double_blind_no_analysis_info_no_impact(self):
        assert algo.compute_judgment(_answers(_N, _N, q26=_N, q27=_N)) == _LOW

    def test_double_blind_no_analysis_substantial_impact(self):
        assert algo.compute_judgment(_answers(_N, _N, q26=_N, q27=_Y)) == _HIGH

    def test_double_blind_ni_analysis_ni_impact(self):
        assert algo.compute_judgment(_answers(_N, _N, q26=_NI, q27=_NI)) == _SC


class TestD2NoDeviations:
    """Aware but no context-dependent deviations."""

    def test_aware_no_deviations_appropriate_analysis(self):
        assert algo.compute_judgment(_answers(_Y, _Y, _N, q26=_Y)) == _LOW

    def test_aware_no_deviations_bad_analysis_no_impact(self):
        assert algo.compute_judgment(_answers(_Y, _Y, _N, q26=_N, q27=_N)) == _LOW

    def test_aware_no_deviations_bad_analysis_with_impact(self):
        assert algo.compute_judgment(_answers(_Y, _Y, _N, q26=_N, q27=_Y)) == _HIGH


class TestD2DeviationsNoEffect:
    """Deviations occurred but did not affect outcome."""

    def test_deviations_no_effect_appropriate_analysis(self):
        assert algo.compute_judgment(_answers(_Y, _Y, _Y, _N, q26=_Y)) == _LOW

    def test_deviations_no_effect_bad_analysis_with_impact(self):
        assert algo.compute_judgment(_answers(_Y, _Y, _Y, _N, q26=_N, q27=_Y)) == _HIGH


class TestD2DeviationsAffectedOutcome:
    """Deviations affected outcome, check balance and analysis."""

    def test_affected_balanced_appropriate_analysis(self):
        assert algo.compute_judgment(_answers(_Y, _Y, _Y, _Y, _Y, _Y)) == _LOW

    def test_affected_balanced_bad_analysis_impact(self):
        assert algo.compute_judgment(_answers(_Y, _Y, _Y, _Y, _Y, _N, _Y)) == _HIGH

    def test_affected_not_balanced_appropriate_analysis(self):
        assert algo.compute_judgment(_answers(_Y, _Y, _Y, _Y, _N, _Y)) == _SC

    def test_affected_not_balanced_bad_analysis(self):
        assert algo.compute_judgment(_answers(_Y, _Y, _Y, _Y, _N, _N)) == _HIGH

    def test_affected_ni_balanced_bad_analysis(self):
        assert algo.compute_judgment(_answers(_Y, _Y, _Y, _Y, _NI, _N)) == _HIGH


class TestD2NiDeviations:
    """NI on whether deviations occurred."""

    def test_ni_deviations_appropriate_analysis(self):
        assert algo.compute_judgment(_answers(_Y, _Y, _NI, q26=_Y)) == _SC

    def test_ni_deviations_bad_analysis_impact(self):
        assert algo.compute_judgment(_answers(_Y, _Y, _NI, q26=_N, q27=_Y)) == _HIGH

    def test_ni_deviations_bad_analysis_no_impact(self):
        assert algo.compute_judgment(_answers(_Y, _Y, _NI, q26=_N, q27=_N)) == _SC


class TestD2Assessment:
    def test_assess_populates_questions(self):
        result = algo.assess(_answers(_N, _N, q26=_Y))
        assert result.domain.value == "D2"
        assert result.algorithm_judgment == _LOW
        assert len(result.signalling_questions) == 7
