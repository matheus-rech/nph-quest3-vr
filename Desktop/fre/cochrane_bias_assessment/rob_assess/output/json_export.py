"""Export full assessment to JSON for archival and machine consumption."""

import json
from dataclasses import asdict
from pathlib import Path

from rob_assess.models.assessment import StudyAssessment
from rob_assess.models.common import RoB2Domain, RoB2Judgment, SignallingAnswer


class AssessmentEncoder(json.JSONEncoder):
    """Custom JSON encoder for assessment dataclasses."""

    def default(self, obj):
        if isinstance(obj, (RoB2Judgment, SignallingAnswer, RoB2Domain)):
            return obj.value
        return super().default(obj)


def assessment_to_dict(assessment: StudyAssessment) -> dict:
    """Convert a StudyAssessment to a JSON-serializable dict."""
    data = asdict(assessment)

    # Post-process enum values to strings
    def _convert_enums(obj):
        if isinstance(obj, dict):
            new_dict = {}
            for k, v in obj.items():
                # Convert enum keys
                key = k.value if isinstance(k, (RoB2Domain,)) else k
                new_dict[key] = _convert_enums(v)
            return new_dict
        if isinstance(obj, list):
            return [_convert_enums(item) for item in obj]
        if isinstance(obj, (RoB2Judgment, SignallingAnswer, RoB2Domain)):
            return obj.value
        return obj

    return _convert_enums(data)


def export_json(
    assessments: list[StudyAssessment],
    output_path: str | Path | None = None,
    indent: int = 2,
) -> str:
    """Export assessments to JSON.

    Args:
        assessments: List of completed assessments.
        output_path: If provided, write to file.
        indent: JSON indentation level.

    Returns:
        JSON string.
    """
    data = {
        "tool": "rob-assess",
        "version": "0.1.0",
        "framework": "RoB 2",
        "assessments": [assessment_to_dict(a) for a in assessments],
    }

    json_str = json.dumps(data, indent=indent, cls=AssessmentEncoder)

    if output_path:
        Path(output_path).write_text(json_str)

    return json_str
