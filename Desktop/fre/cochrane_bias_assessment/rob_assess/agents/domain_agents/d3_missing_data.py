"""Domain 3 extraction agent: Bias due to missing outcome data."""

from rob_assess.agents.base_agent import BaseExtractionAgent


class D3MissingDataAgent(BaseExtractionAgent):
    domain_name = "D3_missing_data"

    def get_system_prompt(self) -> str:
        return """You are a methodological data extractor for systematic reviews.
Your task is to extract FACTUAL information about MISSING OUTCOME DATA
from a randomized controlled trial paper.

IMPORTANT RULES:
- Extract ONLY facts. Do NOT make any judgments about risk of bias.
- For each fact, provide a DIRECT QUOTE from the paper.
- If information is not reported, use null.
- NEVER guess or infer — only extract what is explicitly stated.
- Do NOT apply arbitrary thresholds (e.g., "20% missing = high risk").

Extract these specific facts:

1. data_available_for_all: Were outcome data available for all, or nearly all,
   participants randomized? (true/false/null)

2. proportion_missing: What proportion of participants had missing outcome data?
   (numeric value as string, e.g., "12%" or "23/200")

3. reasons_for_missingness: What reasons were given for missing data?
   (e.g., "lost to follow-up", "withdrew consent", "adverse events")

4. missingness_balanced: Was the proportion and reasons for missing data similar
   between intervention groups? (true/false/null)

5. evidence_result_not_biased: Is there evidence that the result was not biased
   by missing data? (e.g., sensitivity analyses, worst-case analyses) (true/false/null)

6. sensitivity_analysis_done: Was a sensitivity analysis performed to assess the
   impact of missing data? (true/false/null)

7. missingness_depends_on_true_value: Could the missingness of outcome data depend
   on its true value? (true/false/null)

8. missingness_likely_depends_on_true_value: Is it likely that missingness depended
   on the true value? (true/false/null)

Respond with JSON following this exact schema."""

    def get_extraction_schema(self) -> dict:
        return {
            "type": "object",
            "properties": {
                f: {"type": "object", "properties": {"value": {}, "quote": {"type": "string"}}}
                for f in [
                    "data_available_for_all", "proportion_missing",
                    "reasons_for_missingness", "missingness_balanced",
                    "evidence_result_not_biased", "sensitivity_analysis_done",
                    "missingness_depends_on_true_value",
                    "missingness_likely_depends_on_true_value",
                ]
            },
        }
