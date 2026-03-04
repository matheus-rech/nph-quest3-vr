"""Split extracted PDF text into meaningful sections for targeted extraction."""

import re
from dataclasses import dataclass, field


@dataclass
class TextSection:
    """A labelled section of the paper."""

    heading: str
    content: str
    start_pos: int = 0


# Common section headings in medical papers (case-insensitive)
SECTION_PATTERNS = [
    r"(?i)^(abstract)\s*$",
    r"(?i)^(introduction|background)\s*$",
    r"(?i)^(methods?|materials?\s+and\s+methods?|patients?\s+and\s+methods?)\s*$",
    r"(?i)^(results?)\s*$",
    r"(?i)^(discussion)\s*$",
    r"(?i)^(conclusions?|summary)\s*$",
    r"(?i)^(references?|bibliography)\s*$",
    r"(?i)^(supplementary|appendix|supplemental)\s*",
    r"(?i)^(statistical\s+analysis|data\s+analysis)\s*$",
    r"(?i)^(study\s+design|trial\s+design)\s*$",
    r"(?i)^(randomization|randomisation)\s*$",
    r"(?i)^(blinding|masking)\s*$",
    r"(?i)^(outcomes?|endpoints?)\s*$",
    r"(?i)^(participants?|subjects?|eligibility|inclusion)\s*$",
    r"(?i)^(interventions?|treatments?)\s*$",
    r"(?i)^(sample\s+size|power\s+calculation)\s*$",
    r"(?i)^(follow[- ]up)\s*$",
]

COMPILED_PATTERNS = [re.compile(p, re.MULTILINE) for p in SECTION_PATTERNS]


def chunk_by_sections(text: str) -> list[TextSection]:
    """Split text into sections based on common paper headings.

    Falls back to fixed-size chunks if no sections are detected.
    """
    # Find all section heading positions
    headings: list[tuple[int, str]] = []
    for line_match in re.finditer(r"^(.+)$", text, re.MULTILINE):
        line = line_match.group(1).strip()
        if len(line) > 80:
            continue  # Not a heading
        for pattern in COMPILED_PATTERNS:
            if pattern.match(line):
                headings.append((line_match.start(), line))
                break

    if not headings:
        # No sections found — return as single chunk
        return [TextSection(heading="Full Text", content=text, start_pos=0)]

    # Build sections from heading positions
    sections = []
    for i, (pos, heading) in enumerate(headings):
        end_pos = headings[i + 1][0] if i + 1 < len(headings) else len(text)
        # Content starts after the heading line
        content_start = text.index("\n", pos) + 1 if "\n" in text[pos:end_pos] else pos
        content = text[content_start:end_pos].strip()
        if content:
            sections.append(TextSection(heading=heading, content=content, start_pos=pos))

    # Include text before the first heading (title, authors, abstract, etc.)
    if headings[0][0] > 0:
        preamble = text[: headings[0][0]].strip()
        if preamble:
            sections.insert(0, TextSection(heading="Preamble", content=preamble, start_pos=0))

    return sections


def get_methods_text(sections: list[TextSection]) -> str:
    """Extract methods-related sections (most relevant for RoB assessment)."""
    method_keywords = {
        "methods", "materials", "patients", "study design", "trial design",
        "randomization", "randomisation", "blinding", "masking",
        "statistical analysis", "data analysis", "sample size",
        "participants", "subjects", "eligibility", "inclusion",
        "interventions", "treatments", "follow-up",
    }
    parts = []
    for section in sections:
        heading_lower = section.heading.lower()
        if any(kw in heading_lower for kw in method_keywords):
            parts.append(f"## {section.heading}\n{section.content}")
    return "\n\n".join(parts)


def get_results_text(sections: list[TextSection]) -> str:
    """Extract results section."""
    parts = []
    for section in sections:
        if "result" in section.heading.lower():
            parts.append(f"## {section.heading}\n{section.content}")
    return "\n\n".join(parts)


def get_sections_by_keywords(
    sections: list[TextSection],
    keywords: set[str],
) -> str:
    """Extract sections whose headings match any of the given keywords.

    Args:
        sections: Parsed paper sections.
        keywords: Lowercase keywords to match against section headings.

    Returns:
        Concatenated text of matching sections.
    """
    parts = []
    for section in sections:
        heading_lower = section.heading.lower()
        if any(kw in heading_lower for kw in keywords):
            parts.append(f"## {section.heading}\n{section.content}")
    return "\n\n".join(parts)


# Domain-specific section keywords. Each domain agent only needs a subset
# of the paper — sending the full 50K to every agent wastes ~60% of tokens.
DOMAIN_SECTION_KEYWORDS: dict[str, set[str]] = {
    "D1_randomization": {
        "methods", "materials", "patients", "study design", "trial design",
        "randomization", "randomisation", "blinding", "masking",
        "participants", "subjects", "eligibility", "inclusion",
        "sample size", "result",  # Results for baseline Table 1
    },
    "D2_deviations": {
        "methods", "materials", "patients", "blinding", "masking",
        "interventions", "treatments", "statistical analysis",
        "data analysis", "follow-up", "result",
    },
    "D3_missing_data": {
        "result", "follow-up", "methods", "participants", "subjects",
        "statistical analysis", "data analysis",
    },
    "D4_outcome_measurement": {
        "methods", "outcomes", "endpoints", "blinding", "masking",
        "interventions", "treatments", "result",
    },
    "D5_selective_reporting": {
        "methods", "statistical analysis", "data analysis",
        "outcomes", "endpoints", "abstract", "introduction",
        "preamble",  # Often contains trial registration info
    },
}


def get_domain_text(
    domain_name: str,
    sections: list[TextSection],
    full_text: str,
    max_fallback_chars: int = 15000,
) -> str:
    """Get targeted text for a specific domain agent.

    Returns only the sections relevant to this domain. Falls back to
    truncated full text if no matching sections are found.

    Args:
        domain_name: Agent domain name (e.g., "D1_randomization").
        sections: Parsed paper sections.
        full_text: Full paper text (fallback).
        max_fallback_chars: Max chars for fallback truncation.

    Returns:
        Targeted text for the domain agent.
    """
    keywords = DOMAIN_SECTION_KEYWORDS.get(domain_name, set())
    if not keywords:
        return full_text[:max_fallback_chars]

    targeted = get_sections_by_keywords(sections, keywords)
    if targeted:
        return targeted

    # Fallback: no sections matched (unstructured paper)
    return full_text[:max_fallback_chars]
