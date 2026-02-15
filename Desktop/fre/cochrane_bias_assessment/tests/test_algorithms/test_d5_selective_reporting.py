"""Tests for Domain 5: Bias in selection of the reported result.

Decision tree:
- LOW: Analysis per pre-specified plan
- SOME CONCERNS: No plan but no reason to suspect selection
- HIGH: Result likely selected from multiple measurements/analyses
"""

import pytest

from rob_assess.algorithms.rob2_algorithms import D5SelectiveReportingAlgorithm
from rob_assess.models.common import RoB2Judgment, SignallingAnswer

_Y = SignallingAnswer.Y
_PY = SignallingAnswer.PY
_PN = SignallingAnswer.PN
_N = SignallingAnswer.N
_NI = SignallingAnswer.NI
_LOW = RoB2Judgment.LOW
_SC = RoB2Judgment.SOME_CONCERNS
_HIGH = RoB2Judgment.HIGH

algo = D5SelectiveReportingAlgorithm()


def _answers(q51=_NI, q52=_NI, q53=_NI):
    return {"5.1": q51, "5.2": q52, "5.3": q53}


class TestD5Low:
    def test_pre_specified_plan(self):
        assert algo.compute_judgment(_answers(_Y)) == _LOW

    def test_probably_pre_specified(self):
        assert algo.compute_judgment(_answers(_PY)) == _LOW


class TestD5SomeConcerns:
    def test_no_plan_no_selection(self):
        assert algo.compute_judgment(_answers(_N, _N, _N)) == _SC

    def test_no_plan_probably_no_selection(self):
        assert algo.compute_judgment(_answers(_N, _PN, _PN)) == _SC

    def test_ni_plan_ni_selection(self):
        assert algo.compute_judgment(_answers(_NI, _NI, _NI)) == _SC

    def test_ni_plan_no_selection(self):
        assert algo.compute_judgment(_answers(_NI, _N, _N)) == _SC

    def test_no_plan_ni_measurement_no_analyses(self):
        assert algo.compute_judgment(_answers(_N, _NI, _N)) == _SC


class TestD5High:
    def test_selected_from_measurements(self):
        assert algo.compute_judgment(_answers(_N, _Y)) == _HIGH

    def test_selected_from_analyses(self):
        assert algo.compute_judgment(_answers(_N, _N, _Y)) == _HIGH

    def test_probably_selected_from_measurements(self):
        assert algo.compute_judgment(_answers(_N, _PY)) == _HIGH

    def test_probably_selected_from_analyses(self):
        assert algo.compute_judgment(_answers(_N, _N, _PY)) == _HIGH

    def test_ni_plan_selected_from_measurements(self):
        assert algo.compute_judgment(_answers(_NI, _Y)) == _HIGH


class TestD5Assessment:
    def test_assess_returns_domain(self):
        result = algo.assess(_answers(_Y))
        assert result.domain.value == "D5"
        assert result.algorithm_judgment == _LOW
        assert len(result.signalling_questions) == 3
