"""Domain 4 extraction agent: Bias in measurement of the outcome."""

from rob_assess.agents.base_agent import BaseExtractionAgent


class D4OutcomeMeasurementAgent(BaseExtractionAgent):
    domain_name = "D4_outcome_measurement"

    def get_system_prompt(self) -> str:
        return """You are a methodological data extractor for systematic reviews.
Your task is to extract FACTUAL information about OUTCOME MEASUREMENT
from a randomized controlled trial paper.

IMPORTANT RULES:
- Extract ONLY facts. Do NOT make any judgments about risk of bias.
- For each fact, provide a DIRECT QUOTE from the paper.
- If information is not reported, use null.
- NEVER guess or infer — only extract what is explicitly stated.
- Remember: lack of blinding does NOT automatically mean bias.
  For truly objective outcomes (e.g., all-cause mortality), knowledge of
  assignment is unlikely to influence assessment.

Extract these specific facts:

1. measurement_method: What method was used to measure the outcome?
   (e.g., "standardized questionnaire", "laboratory test", "clinical assessment")

2. method_appropriate: Is the measurement method appropriate for this outcome?
   (true/false/null)

3. measurement_differed_between_groups: Could the measurement or ascertainment
   of the outcome have differed between intervention groups? (true/false/null)

4. assessors_blinded: Were outcome assessors blinded to the intervention received?
   (true/false/null)

5. outcome_type: Is the outcome objective or subjective?
   ("objective", "subjective", "mixed", null)
   Objective examples: mortality, lab values, imaging findings
   Subjective examples: pain scores, quality of life, symptom scales

6. knowledge_could_influence: Could knowledge of the intervention received have
   influenced the outcome assessment? (true/false/null)

7. knowledge_likely_influenced: Is it likely that knowledge of the intervention
   actually influenced the outcome assessment? (true/false/null)

Respond with JSON following this exact schema."""

    def get_extraction_schema(self) -> dict:
        return {
            "type": "object",
            "properties": {
                f: {"type": "object", "properties": {"value": {}, "quote": {"type": "string"}}}
                for f in [
                    "measurement_method", "method_appropriate",
                    "measurement_differed_between_groups", "assessors_blinded",
                    "outcome_type", "knowledge_could_influence",
                    "knowledge_likely_influenced",
                ]
            },
        }
