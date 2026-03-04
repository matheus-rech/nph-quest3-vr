"""Domain 2 extraction agent: Bias due to deviations from intended interventions."""

from rob_assess.agents.base_agent import BaseExtractionAgent


class D2DeviationsAgent(BaseExtractionAgent):
    domain_name = "D2_deviations"

    def get_system_prompt(self) -> str:
        return """You are a methodological data extractor for systematic reviews.
Your task is to extract FACTUAL information about BLINDING AND DEVIATIONS
FROM INTENDED INTERVENTIONS from a randomized controlled trial paper.

IMPORTANT RULES:
- Extract ONLY facts. Do NOT make any judgments about risk of bias.
- For each fact, provide a DIRECT QUOTE from the paper.
- If information is not reported, use null.
- NEVER guess or infer — only extract what is explicitly stated.

Extract these specific facts:

1. participants_blinded: Were participants blinded to their assigned intervention?
   (true/false/null)

2. carers_blinded: Were carers and people delivering interventions blinded?
   (true/false/null)

3. context_dependent_deviations: Were there deviations from intended interventions
   that arose because of the trial context (e.g., crossovers, contamination,
   switches prompted by knowledge of assignment)? (true/false/null)

4. deviations_affected_outcome: If deviations occurred, were they likely to have
   affected the outcome? (true/false/null)

5. deviations_balanced: Were deviations balanced between intervention groups?
   (true/false/null)

6. appropriate_analysis: Was an appropriate analysis used (e.g., intention-to-treat)?
   (true/false/null)

7. analysis_type: What type of analysis was used?
   (e.g., "intention-to-treat", "modified ITT", "per-protocol", "as-treated")

8. substantial_impact_of_analysis_failure: If analysis was not ITT, was there
   potential for substantial impact on the result? (true/false/null)

Respond with JSON following this exact schema."""

    def get_extraction_schema(self) -> dict:
        return {
            "type": "object",
            "properties": {
                f: {"type": "object", "properties": {"value": {}, "quote": {"type": "string"}}}
                for f in [
                    "participants_blinded", "carers_blinded",
                    "context_dependent_deviations", "deviations_affected_outcome",
                    "deviations_balanced", "appropriate_analysis",
                    "analysis_type", "substantial_impact_of_analysis_failure",
                ]
            },
        }
