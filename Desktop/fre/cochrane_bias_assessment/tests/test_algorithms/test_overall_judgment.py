"""Tests for overall RoB 2 judgment algorithm.

Rules:
- LOW: All domains LOW
- SOME CONCERNS: At least one domain SC, none HIGH
- HIGH: Any domain HIGH, OR 3+ domains with SOME_CONCERNS
"""

import pytest

from rob_assess.algorithms.overall_judgment import compute_overall_judgment
from rob_assess.models.common import RoB2Domain, RoB2Judgment

_LOW = RoB2Judgment.LOW
_SC = RoB2Judgment.SOME_CONCERNS
_HIGH = RoB2Judgment.HIGH

D1, D2, D3, D4, D5 = RoB2Domain.D1, RoB2Domain.D2, RoB2Domain.D3, RoB2Domain.D4, RoB2Domain.D5


def _judgments(d1=_LOW, d2=_LOW, d3=_LOW, d4=_LOW, d5=_LOW):
    return {D1: d1, D2: d2, D3: d3, D4: d4, D5: d5}


class TestOverallLow:
    def test_all_low(self):
        assert compute_overall_judgment(_judgments()) == _LOW

    def test_explicit_all_low(self):
        assert compute_overall_judgment(_judgments(_LOW, _LOW, _LOW, _LOW, _LOW)) == _LOW


class TestOverallSomeConcerns:
    def test_one_domain_sc(self):
        assert compute_overall_judgment(_judgments(d1=_SC)) == _SC

    def test_two_domains_sc(self):
        assert compute_overall_judgment(_judgments(d1=_SC, d3=_SC)) == _SC

    def test_each_domain_sc_individually(self):
        for d in [D1, D2, D3, D4, D5]:
            j = _judgments()
            j[d] = _SC
            assert compute_overall_judgment(j) == _SC


class TestOverallHigh:
    def test_one_domain_high(self):
        assert compute_overall_judgment(_judgments(d1=_HIGH)) == _HIGH

    def test_any_domain_high(self):
        for d in [D1, D2, D3, D4, D5]:
            j = _judgments()
            j[d] = _HIGH
            assert compute_overall_judgment(j) == _HIGH

    def test_three_sc_becomes_high(self):
        """3+ SOME_CONCERNS → HIGH (substantially lowers confidence)."""
        assert compute_overall_judgment(_judgments(d1=_SC, d2=_SC, d3=_SC)) == _HIGH

    def test_four_sc_is_high(self):
        assert compute_overall_judgment(_judgments(d1=_SC, d2=_SC, d3=_SC, d4=_SC)) == _HIGH

    def test_five_sc_is_high(self):
        assert compute_overall_judgment(
            _judgments(d1=_SC, d2=_SC, d3=_SC, d4=_SC, d5=_SC)
        ) == _HIGH

    def test_high_overrides_sc(self):
        assert compute_overall_judgment(_judgments(d1=_HIGH, d2=_SC)) == _HIGH

    def test_high_plus_many_sc(self):
        assert compute_overall_judgment(
            _judgments(d1=_HIGH, d2=_SC, d3=_SC, d4=_SC)
        ) == _HIGH


class TestOverallEdgeCases:
    def test_empty_judgments(self):
        assert compute_overall_judgment({}) == _SC

    def test_partial_judgments_all_low(self):
        assert compute_overall_judgment({D1: _LOW, D2: _LOW}) == _LOW

    def test_partial_judgments_one_high(self):
        assert compute_overall_judgment({D1: _LOW, D3: _HIGH}) == _HIGH

    def test_exactly_two_sc_not_high(self):
        """Two SC should be SOME_CONCERNS, not HIGH (threshold is 3)."""
        assert compute_overall_judgment(_judgments(d1=_SC, d2=_SC)) == _SC
