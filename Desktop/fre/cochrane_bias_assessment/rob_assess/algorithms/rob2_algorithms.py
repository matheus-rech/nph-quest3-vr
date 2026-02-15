"""Deterministic implementations of all 5 RoB 2 domain algorithms.

Each algorithm implements the exact Cochrane decision tree from the
official RoB 2 guidance document. These are pure functions over
SignallingAnswer inputs — no LLM calls, fully testable and reproducible.

Source of truth: cochrane_rob_tools_report.md (domain algorithms)
"""

from rob_assess.models.common import RoB2Domain, RoB2Judgment, SignallingAnswer
from rob_assess.models.rob2 import SignallingQuestion

from .base_algorithm import DomainAlgorithm

# Convenience aliases
_Y = SignallingAnswer.Y
_PY = SignallingAnswer.PY
_PN = SignallingAnswer.PN
_N = SignallingAnswer.N
_NI = SignallingAnswer.NI
_LOW = RoB2Judgment.LOW
_SC = RoB2Judgment.SOME_CONCERNS
_HIGH = RoB2Judgment.HIGH


class D1RandomizationAlgorithm(DomainAlgorithm):
    """Domain 1: Bias arising from the randomization process.

    Decision tree:
    - LOW: Allocation was random AND concealed AND baseline differences
           are compatible with chance
    - SOME CONCERNS: No information on randomization or concealment,
           but no baseline imbalances suggesting problems
    - HIGH: Allocation was not random OR not concealed, AND baseline
           differences suggest a problem with randomization
    """

    domain = RoB2Domain.D1

    def get_signalling_questions(self) -> list[SignallingQuestion]:
        return [
            SignallingQuestion("1.1", "Was the allocation sequence random?"),
            SignallingQuestion(
                "1.2",
                "Was the allocation sequence concealed until participants were "
                "enrolled and assigned to interventions?",
            ),
            SignallingQuestion(
                "1.3",
                "Did baseline differences between intervention groups suggest "
                "a problem with the randomization process?",
            ),
        ]

    def compute_judgment(self, answers: dict[str, SignallingAnswer]) -> RoB2Judgment:
        q11 = self._get(answers, "1.1")
        q12 = self._get(answers, "1.2")
        q13 = self._get(answers, "1.3")

        # LOW: random AND concealed AND baseline OK
        if q11.is_yes and q12.is_yes and q13.is_no:
            return _LOW

        # HIGH: (not random OR not concealed) AND baseline problems
        if (q11.is_no or q12.is_no) and q13.is_yes:
            return _HIGH

        # HIGH: baseline clearly problematic regardless
        if q13.is_yes:
            return _HIGH

        # SOME CONCERNS: missing info but no baseline red flags
        if q11 == _NI or q12 == _NI:
            if q13.is_no or q13 == _NI:
                return _SC
            return _HIGH

        # Partial info: random but not concealed (or vice versa), no baseline issues
        if (q11.is_yes and q12.is_no) or (q11.is_no and q12.is_yes):
            if q13.is_no or q13 == _NI:
                return _SC
            return _HIGH

        # Not random and not concealed, but baseline OK
        if q11.is_no and q12.is_no and (q13.is_no or q13 == _NI):
            return _HIGH

        return _SC


class D2DeviationsAlgorithm(DomainAlgorithm):
    """Domain 2: Bias due to deviations from intended interventions.

    This implements the "effect of assignment" (ITT) variant.

    Decision tree:
    - LOW: Blinded OR no context-dependent deviations OR appropriate analysis (ITT)
    - SOME CONCERNS: Some knowledge of assignment, but no substantial impact expected
    - HIGH: Unblinded, context-dependent deviations likely affected outcome,
            and analysis did not correct for this
    """

    domain = RoB2Domain.D2

    def get_signalling_questions(self) -> list[SignallingQuestion]:
        return [
            SignallingQuestion(
                "2.1",
                "Were participants aware of their assigned intervention during the trial?",
            ),
            SignallingQuestion(
                "2.2",
                "Were carers and people delivering the interventions aware of "
                "participants' assigned intervention during the trial?",
            ),
            SignallingQuestion(
                "2.3",
                "If Y/PY/NI to 2.1 or 2.2: Were there deviations from the intended "
                "intervention that arose because of the trial context?",
            ),
            SignallingQuestion(
                "2.4",
                "If Y/PY to 2.3: Were these deviations likely to have affected the outcome?",
            ),
            SignallingQuestion(
                "2.5",
                "If Y/PY/NI to 2.4: Were these deviations from intended intervention "
                "balanced between groups?",
            ),
            SignallingQuestion(
                "2.6",
                "Was an appropriate analysis used to estimate the effect of "
                "assignment to intervention?",
            ),
            SignallingQuestion(
                "2.7",
                "If N/PN/NI to 2.6: Was there potential for a substantial impact "
                "(on the result) of the failure to analyse participants in the group "
                "to which they were randomized?",
            ),
        ]

    def compute_judgment(self, answers: dict[str, SignallingAnswer]) -> RoB2Judgment:
        q21 = self._get(answers, "2.1")
        q22 = self._get(answers, "2.2")
        q23 = self._get(answers, "2.3")
        q24 = self._get(answers, "2.4")
        q25 = self._get(answers, "2.5")
        q26 = self._get(answers, "2.6")
        q27 = self._get(answers, "2.7")

        # Both participants and carers blinded → LOW (regardless of other answers)
        if q21.is_no and q22.is_no:
            # Still need appropriate analysis
            if q26.is_yes:
                return _LOW
            if q26.is_no_or_ni:
                if q27.is_yes:
                    return _HIGH
                if q27.is_no:
                    return _LOW
                return _SC

        # Someone was aware (or NI) — check if deviations occurred
        aware = q21.is_yes_or_ni or q22.is_yes_or_ni

        if aware:
            # No context-dependent deviations
            if q23.is_no:
                if q26.is_yes:
                    return _LOW
                if q26.is_no_or_ni:
                    if q27.is_yes:
                        return _HIGH
                    if q27.is_no:
                        return _LOW
                    return _SC

            # Deviations occurred (or NI)
            if q23.is_yes:
                # Deviations did not affect outcome
                if q24.is_no:
                    if q26.is_yes:
                        return _LOW
                    if q26.is_no_or_ni:
                        if q27.is_yes:
                            return _HIGH
                        if q27.is_no:
                            return _LOW
                        return _SC

                # Deviations may have affected outcome
                if q24.is_yes_or_ni:
                    # Balanced between groups
                    if q25.is_yes:
                        if q26.is_yes:
                            return _LOW
                        if q26.is_no_or_ni:
                            if q27.is_yes:
                                return _HIGH
                            if q27.is_no:
                                return _SC
                            return _SC

                    # Not balanced or NI
                    if q25.is_no_or_ni:
                        if q26.is_yes:
                            return _SC
                        if q26.is_no_or_ni:
                            return _HIGH

            # NI on deviations
            if q23 == _NI:
                if q26.is_yes:
                    return _SC
                if q26.is_no_or_ni:
                    if q27.is_yes:
                        return _HIGH
                    return _SC

        return _SC


class D3MissingDataAlgorithm(DomainAlgorithm):
    """Domain 3: Bias due to missing outcome data.

    Decision tree:
    - LOW: Data available for all/nearly all, OR evidence result not biased
    - SOME CONCERNS: Missingness could depend on true value but unlikely
    - HIGH: Missingness likely depends on its true value
    """

    domain = RoB2Domain.D3

    def get_signalling_questions(self) -> list[SignallingQuestion]:
        return [
            SignallingQuestion(
                "3.1",
                "Were data for this outcome available for all, or nearly all, "
                "participants randomized?",
            ),
            SignallingQuestion(
                "3.2",
                "If N/PN/NI to 3.1: Is there evidence that the result was not "
                "biased by missing outcome data?",
            ),
            SignallingQuestion(
                "3.3",
                "If N/PN to 3.2: Could missingness in the outcome depend on "
                "its true value?",
            ),
            SignallingQuestion(
                "3.4",
                "If Y/PY/NI to 3.3: Is it likely that missingness in the outcome "
                "depended on its true value?",
            ),
        ]

    def compute_judgment(self, answers: dict[str, SignallingAnswer]) -> RoB2Judgment:
        q31 = self._get(answers, "3.1")
        q32 = self._get(answers, "3.2")
        q33 = self._get(answers, "3.3")
        q34 = self._get(answers, "3.4")

        # Data available for all/nearly all → LOW
        if q31.is_yes:
            return _LOW

        # Data not available (or NI): check if evidence of no bias
        if q31.is_no_or_ni:
            if q32.is_yes:
                return _LOW

            if q32.is_no_or_ni:
                # Could missingness depend on true value?
                if q33.is_no:
                    return _LOW

                if q33.is_yes_or_ni:
                    # Is it likely?
                    if q34.is_yes:
                        return _HIGH
                    if q34.is_no:
                        return _SC
                    # NI on likelihood
                    return _SC

        return _SC


class D4OutcomeMeasurementAlgorithm(DomainAlgorithm):
    """Domain 4: Bias in measurement of the outcome.

    Decision tree:
    - LOW: Method appropriate AND measurement same across groups AND
           assessors blinded OR knowledge couldn't influence
    - SOME CONCERNS: Assessors aware, knowledge could but probably didn't influence
    - HIGH: Method inappropriate OR knowledge likely influenced assessment
    """

    domain = RoB2Domain.D4

    def get_signalling_questions(self) -> list[SignallingQuestion]:
        return [
            SignallingQuestion(
                "4.1", "Was the method of measuring the outcome inappropriate?"
            ),
            SignallingQuestion(
                "4.2",
                "Could measurement or ascertainment of the outcome have differed "
                "between intervention groups?",
            ),
            SignallingQuestion(
                "4.3",
                "If N/PN/NI to 4.1 and 4.2: Were outcome assessors aware of the "
                "intervention received by study participants?",
            ),
            SignallingQuestion(
                "4.4",
                "If Y/PY/NI to 4.3: Could assessment of the outcome have been "
                "influenced by knowledge of intervention received?",
            ),
            SignallingQuestion(
                "4.5",
                "If Y/PY/NI to 4.4: Is it likely that assessment of the outcome "
                "was influenced by knowledge of intervention received?",
            ),
        ]

    def compute_judgment(self, answers: dict[str, SignallingAnswer]) -> RoB2Judgment:
        q41 = self._get(answers, "4.1")
        q42 = self._get(answers, "4.2")
        q43 = self._get(answers, "4.3")
        q44 = self._get(answers, "4.4")
        q45 = self._get(answers, "4.5")

        # Method inappropriate → HIGH
        if q41.is_yes:
            return _HIGH

        # Measurement differed between groups → HIGH
        if q42.is_yes:
            return _HIGH

        # Method appropriate AND measurement same (or NI)
        if q41.is_no_or_ni and q42.is_no_or_ni:
            # Assessors blinded → LOW
            if q43.is_no:
                return _LOW

            # Assessors aware (or NI)
            if q43.is_yes_or_ni:
                # Knowledge could not influence → LOW
                if q44.is_no:
                    return _LOW

                if q44.is_yes_or_ni:
                    # Likely influenced → HIGH
                    if q45.is_yes:
                        return _HIGH
                    # Probably not influenced → SOME CONCERNS
                    if q45.is_no:
                        return _SC
                    # NI on likelihood
                    return _SC

        return _SC


class D5SelectiveReportingAlgorithm(DomainAlgorithm):
    """Domain 5: Bias in selection of the reported result.

    Decision tree:
    - LOW: Analysis per pre-specified plan, OR no evidence of selection
    - SOME CONCERNS: No pre-specified plan but no reason to suspect selection
    - HIGH: Result likely selected based on results from multiple measurements/analyses
    """

    domain = RoB2Domain.D5

    def get_signalling_questions(self) -> list[SignallingQuestion]:
        return [
            SignallingQuestion(
                "5.1",
                "Were the data that produced this result analysed in accordance with "
                "a pre-specified analysis plan that was finalized before unblinded "
                "outcome data were available for analysis?",
            ),
            SignallingQuestion(
                "5.2",
                "If N/PN/NI to 5.1: Is the numerical result being assessed likely "
                "to have been selected, on the basis of the results, from multiple "
                "eligible outcome measurements (e.g., scales, definitions, time points) "
                "within the outcome domain?",
            ),
            SignallingQuestion(
                "5.3",
                "If N/PN/NI to 5.1: Is the numerical result being assessed likely "
                "to have been selected, on the basis of the results, from multiple "
                "eligible analyses of the data?",
            ),
        ]

    def compute_judgment(self, answers: dict[str, SignallingAnswer]) -> RoB2Judgment:
        q51 = self._get(answers, "5.1")
        q52 = self._get(answers, "5.2")
        q53 = self._get(answers, "5.3")

        # Pre-specified plan → LOW
        if q51.is_yes:
            return _LOW

        # No pre-specified plan (or NI)
        if q51.is_no_or_ni:
            # Result likely selected from multiple measurements → HIGH
            if q52.is_yes:
                return _HIGH
            # Result likely selected from multiple analyses → HIGH
            if q53.is_yes:
                return _HIGH

            # NI on selection → SOME CONCERNS
            if q51 == _NI:
                return _SC

            # Plan not pre-specified but no selection suspected
            if q52.is_no and q53.is_no:
                return _SC

            # Mixed NI
            return _SC

        return _SC


# Registry of all domain algorithms
DOMAIN_ALGORITHMS: dict[RoB2Domain, type[DomainAlgorithm]] = {
    RoB2Domain.D1: D1RandomizationAlgorithm,
    RoB2Domain.D2: D2DeviationsAlgorithm,
    RoB2Domain.D3: D3MissingDataAlgorithm,
    RoB2Domain.D4: D4OutcomeMeasurementAlgorithm,
    RoB2Domain.D5: D5SelectiveReportingAlgorithm,
}
