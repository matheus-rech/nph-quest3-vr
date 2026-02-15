"""Base agent wrapping AsyncAnthropic for structured fact extraction."""

import asyncio
import json
import logging
import os
from abc import ABC, abstractmethod
from dataclasses import dataclass

from anthropic import AsyncAnthropic

DEFAULT_MODEL = "claude-sonnet-4-5-20250929"

logger = logging.getLogger(__name__)

# Retry settings for transient API errors
MAX_RETRIES = 3
RETRY_BASE_DELAY = 1.0  # seconds
RETRYABLE_STATUS_CODES = {429, 500, 502, 503, 529}


@dataclass
class ExtractionResult:
    """Raw extraction result from an agent."""

    domain: str
    facts_json: dict
    raw_response: str
    model: str
    input_tokens: int = 0
    output_tokens: int = 0


class BaseExtractionAgent(ABC):
    """Base class for domain-specific extraction agents.

    Each agent has a focused system prompt and returns structured JSON
    matching its domain's Facts dataclass schema.
    """

    def __init__(
        self,
        model: str | None = None,
        client: AsyncAnthropic | None = None,
    ):
        self.model = model or os.environ.get("ROB_ASSESS_MODEL", DEFAULT_MODEL)
        self.client = client or AsyncAnthropic()

    @property
    @abstractmethod
    def domain_name(self) -> str: ...

    @abstractmethod
    def get_system_prompt(self) -> str: ...

    @abstractmethod
    def get_extraction_schema(self) -> dict:
        """JSON schema describing the expected output."""
        ...

    def _build_user_prompt(self, targeted_text: str) -> str:
        return (
            f"Below is text from a randomized controlled trial paper, "
            f"containing the sections most relevant to your domain. "
            f"Extract the factual information requested in your instructions.\n\n"
            f"=== PAPER TEXT ===\n{targeted_text}\n\n"
            f"Respond ONLY with valid JSON matching the schema. "
            f'Use null for any information that is not reported. '
            f"Include a direct quote from the paper for each fact you extract."
        )

    async def extract(self, targeted_text: str) -> ExtractionResult:
        """Run extraction on domain-targeted text with retry logic.

        Args:
            targeted_text: Pre-filtered text containing only sections
                relevant to this domain.

        Returns:
            ExtractionResult with parsed JSON facts.
        """
        user_prompt = self._build_user_prompt(targeted_text)
        last_error = None

        for attempt in range(MAX_RETRIES):
            try:
                response = await self.client.messages.create(
                    model=self.model,
                    max_tokens=4096,
                    temperature=0.0,
                    system=self.get_system_prompt(),
                    messages=[{"role": "user", "content": user_prompt}],
                )

                raw_text = response.content[0].text

                # Extract JSON from response (handle markdown code blocks)
                json_str = raw_text
                if "```json" in json_str:
                    json_str = json_str.split("```json")[1].split("```")[0]
                elif "```" in json_str:
                    json_str = json_str.split("```")[1].split("```")[0]

                facts_json = json.loads(json_str.strip())

                return ExtractionResult(
                    domain=self.domain_name,
                    facts_json=facts_json,
                    raw_response=raw_text,
                    model=self.model,
                    input_tokens=response.usage.input_tokens,
                    output_tokens=response.usage.output_tokens,
                )

            except json.JSONDecodeError as e:
                last_error = e
                logger.warning(
                    f"{self.domain_name}: JSON parse failed (attempt {attempt + 1}/{MAX_RETRIES}): {e}"
                )
            except Exception as e:
                last_error = e
                status_code = getattr(e, "status_code", None)
                if status_code in RETRYABLE_STATUS_CODES:
                    logger.warning(
                        f"{self.domain_name}: API error {status_code} "
                        f"(attempt {attempt + 1}/{MAX_RETRIES})"
                    )
                else:
                    raise  # Non-retryable error — fail immediately

            if attempt < MAX_RETRIES - 1:
                delay = RETRY_BASE_DELAY * (2**attempt)
                logger.info(f"{self.domain_name}: retrying in {delay:.1f}s")
                await asyncio.sleep(delay)

        raise RuntimeError(
            f"{self.domain_name}: extraction failed after {MAX_RETRIES} attempts"
        ) from last_error
