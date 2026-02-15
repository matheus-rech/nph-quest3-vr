"""Tests for common enum and model behavior."""

import pytest

from rob_assess.models.common import RoB2Domain, RoB2Judgment, SignallingAnswer


class TestSignallingAnswer:
    def test_yes_group(self):
        assert SignallingAnswer.Y.is_yes
        assert SignallingAnswer.PY.is_yes
        assert not SignallingAnswer.N.is_yes
        assert not SignallingAnswer.PN.is_yes
        assert not SignallingAnswer.NI.is_yes

    def test_no_group(self):
        assert SignallingAnswer.N.is_no
        assert SignallingAnswer.PN.is_no
        assert not SignallingAnswer.Y.is_no
        assert not SignallingAnswer.PY.is_no
        assert not SignallingAnswer.NI.is_no

    def test_yes_or_ni(self):
        assert SignallingAnswer.Y.is_yes_or_ni
        assert SignallingAnswer.PY.is_yes_or_ni
        assert SignallingAnswer.NI.is_yes_or_ni
        assert not SignallingAnswer.N.is_yes_or_ni
        assert not SignallingAnswer.PN.is_yes_or_ni

    def test_no_or_ni(self):
        assert SignallingAnswer.N.is_no_or_ni
        assert SignallingAnswer.PN.is_no_or_ni
        assert SignallingAnswer.NI.is_no_or_ni
        assert not SignallingAnswer.Y.is_no_or_ni
        assert not SignallingAnswer.PY.is_no_or_ni


class TestRoB2Judgment:
    def test_values(self):
        assert RoB2Judgment.LOW.value == "Low"
        assert RoB2Judgment.SOME_CONCERNS.value == "Some concerns"
        assert RoB2Judgment.HIGH.value == "High"


class TestRoB2Domain:
    def test_domain_names(self):
        assert "randomization" in RoB2Domain.D1.full_name.lower()
        assert "deviation" in RoB2Domain.D2.full_name.lower()
        assert "missing" in RoB2Domain.D3.full_name.lower()
        assert "measurement" in RoB2Domain.D4.full_name.lower()
        assert "selection" in RoB2Domain.D5.full_name.lower()

    def test_five_domains(self):
        assert len(RoB2Domain) == 5
