# Cochrane RoB 2 Assessment Tool (rob-assess)

## Overview

CLI tool for assessing risk of bias in RCTs using Cochrane RoB 2. Uses the **GEPA approach** (Guided Extraction + Programmatic Assessment): LLM extracts facts from papers, deterministic Python algorithms apply Cochrane decision trees.

## Architecture

```
PDF → [Extraction Agents (LLM)] → Facts → [Algorithms (Code)] → Assessment → [Export]
```

- **Phase 1**: 5 domain-specific Claude agents extract facts with direct quotes (no judgments)
  - Shared `AsyncAnthropic` client (single connection pool)
  - Domain-targeted text routing (each agent receives only relevant paper sections)
  - Exponential backoff retry on transient API errors (429, 500, JSON parse)
- **Phase 2**: Pure Python algorithms apply official Cochrane decision trees (no LLM)

## Project Structure

```
rob_assess/
├── models/          # Dataclasses: enums, facts, assessments, confidence
├── extraction/      # PDF reader (PyMuPDF + pdfplumber), text chunker + domain section routing
├── algorithms/      # MOST CRITICAL: deterministic D1-D5 decision trees + overall
├── agents/          # Anthropic SDK: base agent, 5 domain agents, orchestrator
│   └── domain_agents/  # D1-D5 extraction agents with focused prompts
├── output/          # robvis CSV, Excel (traffic-light), JSON exporters
├── review/          # Rich-based interactive human review terminal UI
└── cli.py           # Click CLI: single, batch, export, review
tests/
├── test_algorithms/ # 98 tests covering every branch of every domain algorithm
├── test_models/     # Enum behavior, confidence scoring, reviewer overrides
└── test_output/     # robvis CSV format validation
```

## Key Commands

```bash
source .venv/bin/activate
rob-assess single paper.pdf              # Assess one study
rob-assess batch ./papers/ -c 3          # Batch assess
rob-assess export ./results/ --format robvis  # Generate robvis CSV
rob-assess review results/assessment.json     # Interactive review
python -m pytest tests/ -v               # Run all 125 tests
```

## Development Rules

### Algorithms are the source of truth
- `rob_assess/algorithms/rob2_algorithms.py` implements exact Cochrane decision trees
- Reference: `cochrane_rob_tools_report.md` lines 60-63, 86-88, 108-111, 133-135, 155-157
- Any algorithm change MUST pass all 98 tests in `tests/test_algorithms/`
- Y/PY have identical implications for judgments; same for N/PN

### Extraction agents extract ONLY facts
- Agents in `rob_assess/agents/domain_agents/` must NEVER make judgments
- Every extracted fact requires a direct quote from the paper
- `null` for unreported information (never guess)

### Overall judgment rules
- LOW: All 5 domains Low
- SOME CONCERNS: Any domain Some Concerns, none High
- HIGH: Any domain High, OR 3+ domains with Some Concerns

### robvis compatibility
- CSV values must be exactly: `"Low"`, `"Some concerns"`, `"High"` (case-sensitive)
- Header: `Study,D1,D2,D3,D4,D5,Overall,Weight`

## Dependencies

`anthropic`, `pymupdf`, `pdfplumber`, `click`, `openpyxl`, `pandas`, `rich`
Dev: `pytest`, `pytest-asyncio`, `pytest-cov`, `ruff`

## Environment

- Python 3.12+ with venv at `.venv/`
- `ANTHROPIC_API_KEY` required for extraction agents
- `ROB_ASSESS_MODEL` env var controls model (default: claude-sonnet-4-5-20250929)

## Common Pitfalls (Cochrane guidance)

- Lack of blinding ≠ automatic bias (check if outcome is objective)
- Missing analysis plan ≠ automatic high risk
- Baseline imbalance alone ≠ randomization failure
- Do NOT set arbitrary missing data thresholds (e.g., 20%)
- RoB 2 assesses per **result**, not per study
