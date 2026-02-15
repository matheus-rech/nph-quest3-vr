"""Tests for robvis CSV export format validation.

robvis expects exact strings: "Low", "Some concerns", "High"
in columns: Study, D1, D2, D3, D4, D5, Overall, Weight
"""

import csv
import io

import pytest

from rob_assess.models.assessment import StudyAssessment
from rob_assess.models.common import RoB2Domain, RoB2Judgment
from rob_assess.models.rob2 import DomainAssessment, RoB2Assessment
from rob_assess.output.robvis_csv import (
    ROBVIS_HEADER,
    assessment_to_robvis_row,
    export_robvis_csv,
)


def _make_assessment(study_id: str, judgments: dict[RoB2Domain, RoB2Judgment]) -> StudyAssessment:
    """Helper to build a StudyAssessment with given judgments."""
    domains = {}
    for domain, judgment in judgments.items():
        domains[domain] = DomainAssessment(domain=domain, algorithm_judgment=judgment)

    # Compute overall
    from rob_assess.algorithms.overall_judgment import compute_overall_judgment

    overall = compute_overall_judgment(judgments)

    rob2 = RoB2Assessment(
        study_id=study_id,
        domains=domains,
        overall_algorithm_judgment=overall,
    )
    return StudyAssessment(study_id=study_id, assessment=rob2)


class TestRobvisHeader:
    def test_header_format(self):
        assert ROBVIS_HEADER == ["Study", "D1", "D2", "D3", "D4", "D5", "Overall", "Weight"]


class TestRobvisRow:
    def test_all_low(self):
        sa = _make_assessment(
            "Smith 2024",
            {d: RoB2Judgment.LOW for d in RoB2Domain},
        )
        row = assessment_to_robvis_row(sa)
        assert row == ["Smith 2024", "Low", "Low", "Low", "Low", "Low", "Low", "1.0"]

    def test_all_high(self):
        sa = _make_assessment(
            "Jones 2023",
            {d: RoB2Judgment.HIGH for d in RoB2Domain},
        )
        row = assessment_to_robvis_row(sa)
        assert row == ["Jones 2023", "High", "High", "High", "High", "High", "High", "1.0"]

    def test_mixed_judgments(self):
        sa = _make_assessment(
            "Lee 2025",
            {
                RoB2Domain.D1: RoB2Judgment.LOW,
                RoB2Domain.D2: RoB2Judgment.SOME_CONCERNS,
                RoB2Domain.D3: RoB2Judgment.LOW,
                RoB2Domain.D4: RoB2Judgment.HIGH,
                RoB2Domain.D5: RoB2Judgment.LOW,
            },
        )
        row = assessment_to_robvis_row(sa)
        assert row[0] == "Lee 2025"
        assert row[1] == "Low"
        assert row[2] == "Some concerns"
        assert row[4] == "High"

    def test_custom_weight(self):
        sa = _make_assessment(
            "Test",
            {d: RoB2Judgment.LOW for d in RoB2Domain},
        )
        row = assessment_to_robvis_row(sa, weight=2.5)
        assert row[-1] == "2.5"

    def test_exact_robvis_strings(self):
        """Verify judgment values are exactly what robvis expects."""
        assert RoB2Judgment.LOW.value == "Low"
        assert RoB2Judgment.SOME_CONCERNS.value == "Some concerns"
        assert RoB2Judgment.HIGH.value == "High"


class TestRobvisCsvExport:
    def test_export_single(self):
        sa = _make_assessment(
            "Smith 2024",
            {d: RoB2Judgment.LOW for d in RoB2Domain},
        )
        csv_str = export_robvis_csv([sa])
        reader = csv.reader(io.StringIO(csv_str))
        rows = list(reader)
        assert rows[0] == ROBVIS_HEADER
        assert len(rows) == 2

    def test_export_multiple(self):
        assessments = [
            _make_assessment(f"Study {i}", {d: RoB2Judgment.LOW for d in RoB2Domain})
            for i in range(5)
        ]
        csv_str = export_robvis_csv(assessments)
        reader = csv.reader(io.StringIO(csv_str))
        rows = list(reader)
        assert len(rows) == 6  # header + 5 studies

    def test_export_to_file(self, tmp_path):
        sa = _make_assessment(
            "Test",
            {d: RoB2Judgment.SOME_CONCERNS for d in RoB2Domain},
        )
        out = tmp_path / "robvis.csv"
        export_robvis_csv([sa], out)
        assert out.exists()
        content = out.read_text()
        assert "Some concerns" in content

    def test_csv_parseable(self):
        """Verify the CSV is well-formed and parseable."""
        sa = _make_assessment(
            'Study "with" special, chars',
            {d: RoB2Judgment.LOW for d in RoB2Domain},
        )
        csv_str = export_robvis_csv([sa])
        reader = csv.reader(io.StringIO(csv_str))
        rows = list(reader)
        assert rows[1][0] == 'Study "with" special, chars'

    def test_export_with_weights(self):
        assessments = [
            _make_assessment("A", {d: RoB2Judgment.LOW for d in RoB2Domain}),
            _make_assessment("B", {d: RoB2Judgment.HIGH for d in RoB2Domain}),
        ]
        weights = {"A": 1.5, "B": 0.8}
        csv_str = export_robvis_csv(assessments, weights=weights)
        reader = csv.reader(io.StringIO(csv_str))
        rows = list(reader)
        assert rows[1][-1] == "1.5"
        assert rows[2][-1] == "0.8"
