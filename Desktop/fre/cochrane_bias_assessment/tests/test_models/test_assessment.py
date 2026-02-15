"""Tests for assessment model behavior."""

import pytest

from rob_assess.models.assessment import DomainConfidence, StudyAssessment
from rob_assess.models.common import RoB2Domain, RoB2Judgment
from rob_assess.models.rob2 import DomainAssessment, RoB2Assessment


class TestDomainConfidence:
    def test_high_confidence(self):
        conf = DomainConfidence(
            domain=RoB2Domain.D1,
            completeness=1.0,
            quote_coverage=1.0,
            agent_confidence=1.0,
            key_questions_answered=1.0,
        )
        assert conf.overall == 1.0
        assert not conf.needs_review

    def test_low_confidence_needs_review(self):
        conf = DomainConfidence(
            domain=RoB2Domain.D1,
            completeness=0.3,
            quote_coverage=0.2,
            agent_confidence=0.4,
            key_questions_answered=0.1,
        )
        assert conf.overall < 0.7
        assert conf.needs_review

    def test_weighted_average(self):
        conf = DomainConfidence(
            domain=RoB2Domain.D1,
            completeness=0.5,
            quote_coverage=0.5,
            agent_confidence=0.5,
            key_questions_answered=0.5,
        )
        assert conf.overall == pytest.approx(0.5)

    def test_key_questions_weighted_heavily(self):
        """Key questions have 40% weight, so they dominate."""
        conf = DomainConfidence(
            domain=RoB2Domain.D1,
            completeness=0.0,
            quote_coverage=0.0,
            agent_confidence=0.0,
            key_questions_answered=1.0,
        )
        assert conf.overall == pytest.approx(0.4)


class TestStudyAssessment:
    def test_overall_confidence_average(self):
        sa = StudyAssessment(study_id="test")
        sa.confidence = {
            RoB2Domain.D1: DomainConfidence(
                RoB2Domain.D1, 1.0, 1.0, 1.0, 1.0
            ),
            RoB2Domain.D2: DomainConfidence(
                RoB2Domain.D2, 0.0, 0.0, 0.0, 0.0
            ),
        }
        assert sa.overall_confidence == pytest.approx(0.5)

    def test_domains_needing_review(self):
        sa = StudyAssessment(study_id="test")
        sa.confidence = {
            RoB2Domain.D1: DomainConfidence(
                RoB2Domain.D1, 1.0, 1.0, 1.0, 1.0
            ),
            RoB2Domain.D2: DomainConfidence(
                RoB2Domain.D2, 0.0, 0.0, 0.0, 0.0
            ),
        }
        needs_review = sa.domains_needing_review
        assert RoB2Domain.D2 in needs_review
        assert RoB2Domain.D1 not in needs_review


class TestRoB2Assessment:
    def test_reviewer_override(self):
        da = DomainAssessment(
            domain=RoB2Domain.D1,
            algorithm_judgment=RoB2Judgment.LOW,
            reviewer_judgment=RoB2Judgment.HIGH,
        )
        assert da.judgment == RoB2Judgment.HIGH
        assert da.is_overridden

    def test_no_override(self):
        da = DomainAssessment(
            domain=RoB2Domain.D1,
            algorithm_judgment=RoB2Judgment.LOW,
        )
        assert da.judgment == RoB2Judgment.LOW
        assert not da.is_overridden

    def test_overall_with_reviewer_override(self):
        assessment = RoB2Assessment(
            study_id="test",
            overall_algorithm_judgment=RoB2Judgment.LOW,
            overall_reviewer_judgment=RoB2Judgment.HIGH,
        )
        assert assessment.overall_judgment == RoB2Judgment.HIGH
