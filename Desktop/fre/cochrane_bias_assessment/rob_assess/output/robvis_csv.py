"""Export assessments to robvis-compatible CSV format.

robvis expects:
Study, D1, D2, D3, D4, D5, Overall, Weight

Values must be exactly: "Low", "Some concerns", "High"
(matching RoB2Judgment.value strings).

Reference: https://github.com/mcguinlu/robvis
"""

import csv
import io
from pathlib import Path

from rob_assess.models.assessment import StudyAssessment
from rob_assess.models.common import RoB2Domain, RoB2Judgment

ROBVIS_HEADER = ["Study", "D1", "D2", "D3", "D4", "D5", "Overall", "Weight"]
DOMAIN_ORDER = [RoB2Domain.D1, RoB2Domain.D2, RoB2Domain.D3, RoB2Domain.D4, RoB2Domain.D5]


def _judgment_to_robvis(judgment: RoB2Judgment | None) -> str:
    """Convert judgment to the exact string robvis expects."""
    if judgment is None:
        return ""
    return judgment.value  # "Low", "Some concerns", "High"


def assessment_to_robvis_row(assessment: StudyAssessment, weight: float = 1.0) -> list[str]:
    """Convert a single StudyAssessment to a robvis CSV row."""
    if assessment.assessment is None:
        raise ValueError(f"No assessment data for study {assessment.study_id}")

    row = [assessment.study_id]

    for domain in DOMAIN_ORDER:
        domain_assessment = assessment.assessment.domains.get(domain)
        if domain_assessment:
            row.append(_judgment_to_robvis(domain_assessment.judgment))
        else:
            row.append("")

    row.append(_judgment_to_robvis(assessment.assessment.overall_judgment))
    row.append(str(weight))

    return row


def export_robvis_csv(
    assessments: list[StudyAssessment],
    output_path: str | Path | None = None,
    weights: dict[str, float] | None = None,
) -> str:
    """Export multiple assessments to robvis CSV format.

    Args:
        assessments: List of completed study assessments.
        output_path: If provided, write to file. Otherwise return string.
        weights: Optional dict mapping study_id to weight.

    Returns:
        The CSV content as a string.
    """
    output = io.StringIO()
    writer = csv.writer(output)
    writer.writerow(ROBVIS_HEADER)

    for sa in assessments:
        weight = (weights or {}).get(sa.study_id, 1.0)
        row = assessment_to_robvis_row(sa, weight)
        writer.writerow(row)

    csv_content = output.getvalue()

    if output_path:
        Path(output_path).write_text(csv_content)

    return csv_content
