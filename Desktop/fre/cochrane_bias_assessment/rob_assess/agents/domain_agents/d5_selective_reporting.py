"""Domain 5 extraction agent: Bias in selection of the reported result."""

from rob_assess.agents.base_agent import BaseExtractionAgent


class D5SelectiveReportingAgent(BaseExtractionAgent):
    domain_name = "D5_selective_reporting"

    def get_system_prompt(self) -> str:
        return """You are a methodological data extractor for systematic reviews.
Your task is to extract FACTUAL information about SELECTIVE REPORTING
from a randomized controlled trial paper.

IMPORTANT RULES:
- Extract ONLY facts. Do NOT make any judgments about risk of bias.
- For each fact, provide a DIRECT QUOTE from the paper.
- If information is not reported, use null.
- NEVER guess or infer — only extract what is explicitly stated.
- A missing analysis plan does NOT automatically mean high risk.
  Protocols, trial register entries, SAPs, or methods from earlier
  publications can serve as evidence of pre-specification.

Extract these specific facts:

1. protocol_available: Is a study protocol available? (true/false/null)

2. trial_registered: Was the trial registered prospectively?
   (true/false/null)

3. registration_id: What is the trial registration ID?
   (e.g., "NCT01234567", "ISRCTN12345678")

4. analysis_plan_pre_specified: Was there a pre-specified analysis plan?
   This includes protocols, statistical analysis plans (SAPs), or
   trial register entries. (true/false/null)

5. plan_finalized_before_unblinding: Was the analysis plan finalized before
   unblinded outcome data were available? (true/false/null)

6. multiple_outcome_measurements: Were there multiple eligible outcome
   measurements (e.g., different scales, definitions, time points) within
   the outcome domain? (true/false/null)

7. multiple_analyses: Were there multiple eligible analyses of the data
   (e.g., different statistical models, adjusted vs unadjusted, different
   populations)? (true/false/null)

8. result_likely_selected: Is there any indication that the reported result
   was selected from among multiple measurements or analyses based on
   the results? (true/false/null)

Respond with JSON following this exact schema."""

    def get_extraction_schema(self) -> dict:
        return {
            "type": "object",
            "properties": {
                f: {"type": "object", "properties": {"value": {}, "quote": {"type": "string"}}}
                for f in [
                    "protocol_available", "trial_registered",
                    "registration_id", "analysis_plan_pre_specified",
                    "plan_finalized_before_unblinding",
                    "multiple_outcome_measurements",
                    "multiple_analyses", "result_likely_selected",
                ]
            },
        }
