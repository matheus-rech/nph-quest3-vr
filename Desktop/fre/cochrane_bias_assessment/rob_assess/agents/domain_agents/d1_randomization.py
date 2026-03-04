"""Domain 1 extraction agent: Bias arising from the randomization process."""

from rob_assess.agents.base_agent import BaseExtractionAgent


class D1RandomizationAgent(BaseExtractionAgent):
    domain_name = "D1_randomization"

    def get_system_prompt(self) -> str:
        return """You are a methodological data extractor for systematic reviews.
Your task is to extract FACTUAL information about the RANDOMIZATION PROCESS
from a randomized controlled trial (RCT) paper.

IMPORTANT RULES:
- Extract ONLY facts. Do NOT make any judgments about risk of bias.
- For each fact, provide a DIRECT QUOTE from the paper.
- If information is not reported, use null.
- NEVER guess or infer — only extract what is explicitly stated.

Extract these specific facts:

1. random_sequence_method: What method was used to generate the random allocation sequence?
   (e.g., "computer-generated random numbers", "random number table", "coin toss", etc.)
   Non-random methods include: date of birth, day of the week, medical record number.

2. used_random_component: Was a truly random component used? (true/false/null)

3. allocation_concealment_method: How was allocation concealment implemented?
   (e.g., "sealed opaque envelopes", "central randomization service", "pharmacy-controlled", etc.)

4. concealment_adequate: Was the concealment method adequate to prevent foreknowledge?
   (true/false/null)

5. baseline_differences_reported: Were baseline characteristics compared between groups?
   (true/false/null)

6. baseline_differences_problematic: Were there problematic baseline differences?
   Look for: unusually large group size imbalances, many statistically significant
   baseline differences, or key prognostic factor imbalances.
   (true/false/null)

7. group_size_imbalance: Description of any group size imbalance.

8. excess_significant_differences: Were there more statistically significant baseline
   differences than expected by chance alone?

Respond with JSON following this exact schema."""

    def get_extraction_schema(self) -> dict:
        return {
            "type": "object",
            "properties": {
                "random_sequence_method": {
                    "type": "object",
                    "properties": {"value": {"type": ["string", "null"]}, "quote": {"type": "string"}},
                },
                "used_random_component": {
                    "type": "object",
                    "properties": {"value": {"type": ["boolean", "null"]}, "quote": {"type": "string"}},
                },
                "allocation_concealment_method": {
                    "type": "object",
                    "properties": {"value": {"type": ["string", "null"]}, "quote": {"type": "string"}},
                },
                "concealment_adequate": {
                    "type": "object",
                    "properties": {"value": {"type": ["boolean", "null"]}, "quote": {"type": "string"}},
                },
                "baseline_differences_reported": {
                    "type": "object",
                    "properties": {"value": {"type": ["boolean", "null"]}, "quote": {"type": "string"}},
                },
                "baseline_differences_problematic": {
                    "type": "object",
                    "properties": {"value": {"type": ["boolean", "null"]}, "quote": {"type": "string"}},
                },
                "group_size_imbalance": {
                    "type": "object",
                    "properties": {"value": {"type": ["string", "null"]}, "quote": {"type": "string"}},
                },
                "excess_significant_differences": {
                    "type": "object",
                    "properties": {"value": {"type": ["string", "null"]}, "quote": {"type": "string"}},
                },
            },
        }
