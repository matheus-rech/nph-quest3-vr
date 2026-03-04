"""Tests for Domain 4: Bias in measurement of the outcome.

Decision tree:
- LOW: Method appropriate AND same across groups AND blinded/can't influence
- SOME CONCERNS: Aware, could but probably didn't influence
- HIGH: Method inappropriate OR knowledge likely influenced
"""

import pytest

from rob_assess.algorithms.rob2_algorithms import D4OutcomeMeasurementAlgorithm
from rob_assess.models.common import RoB2Judgment, SignallingAnswer

_Y = SignallingAnswer.Y
_PY = SignallingAnswer.PY
_PN = SignallingAnswer.PN
_N = SignallingAnswer.N
_NI = SignallingAnswer.NI
_LOW = RoB2Judgment.LOW
_SC = RoB2Judgment.SOME_CONCERNS
_HIGH = RoB2Judgment.HIGH

algo = D4OutcomeMeasurementAlgorithm()


def _answers(q41=_NI, q42=_NI, q43=_NI, q44=_NI, q45=_NI):
    return {"4.1": q41, "4.2": q42, "4.3": q43, "4.4": q44, "4.5": q45}


class TestD4Low:
    def test_appropriate_same_blinded(self):
        assert algo.compute_judgment(_answers(_N, _N, _N)) == _LOW

    def test_appropriate_same_aware_cant_influence(self):
        assert algo.compute_judgment(_answers(_N, _N, _Y, _N)) == _LOW

    def test_probably_appropriate_same_blinded(self):
        assert algo.compute_judgment(_answers(_PN, _PN, _N)) == _LOW

    def test_ni_method_same_blinded(self):
        """NI on method appropriateness still falls through to assessor check."""
        assert algo.compute_judgment(_answers(_NI, _N, _N)) == _LOW


class TestD4SomeConcerns:
    def test_aware_could_but_didnt_influence(self):
        assert algo.compute_judgment(_answers(_N, _N, _Y, _Y, _N)) == _SC

    def test_aware_ni_influence_ni_likely(self):
        assert algo.compute_judgment(_answers(_N, _N, _Y, _NI, _NI)) == _SC

    def test_ni_assessor_awareness_could_influence_not_likely(self):
        assert algo.compute_judgment(_answers(_N, _N, _NI, _Y, _N)) == _SC

    def test_aware_could_influence_probably_not(self):
        assert algo.compute_judgment(_answers(_N, _N, _Y, _PY, _PN)) == _SC


class TestD4High:
    def test_method_inappropriate(self):
        assert algo.compute_judgment(_answers(_Y)) == _HIGH

    def test_measurement_differed(self):
        assert algo.compute_judgment(_answers(_N, _Y)) == _HIGH

    def test_knowledge_likely_influenced(self):
        assert algo.compute_judgment(_answers(_N, _N, _Y, _Y, _Y)) == _HIGH

    def test_probably_inappropriate(self):
        assert algo.compute_judgment(_answers(_PY)) == _HIGH

    def test_measurement_probably_differed(self):
        assert algo.compute_judgment(_answers(_N, _PY)) == _HIGH

    def test_probably_likely_influenced(self):
        assert algo.compute_judgment(_answers(_N, _N, _Y, _Y, _PY)) == _HIGH


class TestD4Assessment:
    def test_assess_returns_domain(self):
        result = algo.assess(_answers(_N, _N, _N))
        assert result.domain.value == "D4"
        assert result.algorithm_judgment == _LOW
        assert len(result.signalling_questions) == 5
