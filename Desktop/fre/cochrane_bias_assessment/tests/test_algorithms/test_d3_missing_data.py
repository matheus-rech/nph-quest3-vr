"""Tests for Domain 3: Bias due to missing outcome data.

Decision tree:
- LOW: Data available for all/nearly all, OR evidence not biased
- SOME CONCERNS: Missingness could depend on true value but unlikely
- HIGH: Missingness likely depends on true value
"""

import pytest

from rob_assess.algorithms.rob2_algorithms import D3MissingDataAlgorithm
from rob_assess.models.common import RoB2Judgment, SignallingAnswer

_Y = SignallingAnswer.Y
_PY = SignallingAnswer.PY
_PN = SignallingAnswer.PN
_N = SignallingAnswer.N
_NI = SignallingAnswer.NI
_LOW = RoB2Judgment.LOW
_SC = RoB2Judgment.SOME_CONCERNS
_HIGH = RoB2Judgment.HIGH

algo = D3MissingDataAlgorithm()


def _answers(q31=_NI, q32=_NI, q33=_NI, q34=_NI):
    return {"3.1": q31, "3.2": q32, "3.3": q33, "3.4": q34}


class TestD3Low:
    def test_data_available_all(self):
        assert algo.compute_judgment(_answers(_Y)) == _LOW

    def test_data_available_probably(self):
        assert algo.compute_judgment(_answers(_PY)) == _LOW

    def test_data_missing_but_not_biased(self):
        assert algo.compute_judgment(_answers(_N, _Y)) == _LOW

    def test_data_missing_probably_not_biased(self):
        assert algo.compute_judgment(_answers(_N, _PY)) == _LOW

    def test_missingness_cannot_depend_on_value(self):
        assert algo.compute_judgment(_answers(_N, _N, _N)) == _LOW

    def test_ni_data_but_evidence_not_biased(self):
        assert algo.compute_judgment(_answers(_NI, _Y)) == _LOW


class TestD3SomeConcerns:
    def test_could_depend_but_probably_not(self):
        assert algo.compute_judgment(_answers(_N, _N, _Y, _N)) == _SC

    def test_could_depend_ni_likelihood(self):
        assert algo.compute_judgment(_answers(_N, _N, _Y, _NI)) == _SC

    def test_ni_everything(self):
        assert algo.compute_judgment(_answers(_NI, _NI, _NI, _NI)) == _SC

    def test_ni_data_ni_evidence_could_depend_not_likely(self):
        assert algo.compute_judgment(_answers(_NI, _NI, _Y, _PN)) == _SC


class TestD3High:
    def test_missingness_likely_depends(self):
        assert algo.compute_judgment(_answers(_N, _N, _Y, _Y)) == _HIGH

    def test_missingness_probably_likely_depends(self):
        assert algo.compute_judgment(_answers(_N, _N, _PY, _PY)) == _HIGH

    def test_ni_evidence_could_and_likely_depends(self):
        assert algo.compute_judgment(_answers(_N, _NI, _Y, _Y)) == _HIGH


class TestD3Assessment:
    def test_assess_returns_domain(self):
        result = algo.assess(_answers(_Y))
        assert result.domain.value == "D3"
        assert result.algorithm_judgment == _LOW
        assert len(result.signalling_questions) == 4
