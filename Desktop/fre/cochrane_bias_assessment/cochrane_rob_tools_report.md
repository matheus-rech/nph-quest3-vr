# Cochrane Risk of Bias (RoB) Tools: Comprehensive Report

*Report date: February 15, 2026*

---

## Table of Contents

1. [RoB 2 -- Randomized Trials](#1-rob-2--revised-cochrane-risk-of-bias-tool-for-randomized-trials)
2. [ROBINS-I -- Non-Randomized Studies of Interventions](#2-robins-i--risk-of-bias-in-non-randomized-studies-of-interventions)
3. [ROBINS-E -- Non-Randomized Studies of Exposures](#3-robins-e--risk-of-bias-in-non-randomized-studies-of-exposures)
4. [Assessment Workflow](#4-assessment-workflow)
5. [Output Formats and Visualization](#5-output-formats-and-visualization)
6. [Existing Software Tools](#6-existing-software-tools)
7. [AI and Automation Efforts](#7-aiautomation-efforts)

---

## 1. RoB 2 -- Revised Cochrane Risk-of-Bias Tool for Randomized Trials

### 1.1 Overview

RoB 2, released on August 22, 2019, is the recommended tool for assessing risk of bias in randomized controlled trials (RCTs) included in Cochrane Reviews. It replaced the original Cochrane RoB tool and was developed by the Cochrane Bias Methods Group, led by researchers at the University of Bristol. The tool is licensed under Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.

RoB 2 evaluates bias at the **result level** (not the study level), meaning each specific numerical result contributing to a review outcome is assessed independently. This is a critical distinction from the original RoB tool, which assessed bias at the study level.

### 1.2 Two Effects of Interest

Before beginning an assessment, reviewers must specify which effect they are interested in:

- **Effect of assignment to intervention (intention-to-treat effect)**: The effect of being assigned to interventions at baseline, regardless of whether participants actually received the intervention as intended. This is the default in most Cochrane Reviews.
- **Effect of adhering to intervention (per-protocol effect)**: The effect of actually adhering to the interventions as specified in the trial protocol.

The choice of effect determines which signalling questions appear in Domain 2 (deviations from intended interventions).

### 1.3 The Five Bias Domains

The RoB 2 tool comprises **five domains** with a total of **22 signalling questions** across all domains. Each signalling question has response options: **Yes**, **Probably yes**, **Probably no**, **No**, and **No information**. The responses "Yes" and "Probably yes" have the same implications for risk of bias judgments, as do "No" and "Probably no."

---

#### Domain 1: Bias Arising from the Randomization Process

This domain addresses whether the randomization process was adequate and whether any baseline differences between groups suggest problems.

**Signalling Questions:**

| Question | Text |
|----------|------|
| 1.1 | Was the allocation sequence random? |
| 1.2 | Was the allocation sequence concealed until participants were enrolled and assigned to interventions? |
| 1.3 | Did baseline differences between intervention groups suggest a problem with the randomization process? |

**Guidance for Key Questions:**

- **Q1.1**: Answer "Yes" if a random component was used (e.g., computer-generated random numbers, random number table, coin tossing, shuffling cards/envelopes, throwing dice, drawing lots). Answer "No" if a non-random method was used (e.g., allocation by date of birth, day of the week, medical record number, sequence of enrollment).
- **Q1.3**: Answer "Yes" if there are imbalances indicating problems with randomization, including: (1) unusually large differences between group sizes; (2) substantial excess in statistically significant baseline differences beyond what chance alone would predict; (3) imbalance in key prognostic factors unlikely due to chance. Important: random variation causes some imbalance; only extreme imbalance suggests compromised randomization.

**Domain-Level Algorithm:**
- **Low risk**: Allocation was random AND concealed AND baseline differences are compatible with chance
- **Some concerns**: No information on randomization or concealment, but no baseline imbalances suggesting problems
- **High risk**: Allocation was not random OR not concealed, AND baseline differences suggest a problem with randomization

---

#### Domain 2: Bias Due to Deviations from Intended Interventions

This domain assesses whether there were deviations from intended interventions that could bias the result, and whether appropriate analysis was used. The signalling questions differ depending on the effect of interest.

**Signalling Questions (Effect of Assignment to Intervention):**

| Question | Text |
|----------|------|
| 2.1 | Were participants aware of their assigned intervention during the trial? |
| 2.2 | Were carers and people delivering the interventions aware of participants' assigned intervention during the trial? |
| 2.3 | If Y/PY/NI to 2.1 or 2.2: Were there deviations from the intended intervention that arose because of the trial context? |
| 2.4 | If Y/PY to 2.3: Were these deviations likely to have affected the outcome? |
| 2.5 | If Y/PY/NI to 2.4: Were these deviations from intended intervention balanced between groups? |
| 2.6 | Was an appropriate analysis used to estimate the effect of assignment to intervention? |
| 2.7 | If N/PN/NI to 2.6: Was there potential for a substantial impact (on the result) of the failure to analyse participants in the group to which they were randomized? |

**Additional Questions for Effect of Adhering to Intervention:**
Questions 2.5 and 2.6 are replaced with questions about whether important co-interventions were balanced, whether the study used an appropriate analysis to estimate the effect of adhering to intervention, and the potential impact of deviations.

**Domain-Level Algorithm:**
- **Low risk**: Participants and carers were blinded, OR deviations were not context-dependent, OR analysis was appropriate (ITT)
- **Some concerns**: Some knowledge of assignment, but no substantial impact expected
- **High risk**: Unblinded, context-dependent deviations likely affected the outcome, and analysis did not correct for this

---

#### Domain 3: Bias Due to Missing Outcome Data

This domain addresses whether outcome data were available for all or nearly all participants.

**Signalling Questions:**

| Question | Text |
|----------|------|
| 3.1 | Were data for this outcome available for all, or nearly all, participants randomized? |
| 3.2 | If N/PN/NI to 3.1: Is there evidence that the result was not biased by missing outcome data? |
| 3.3 | If N/PN to 3.2: Could missingness in the outcome depend on its true value? |
| 3.4 | If Y/PY/NI to 3.3: Is it likely that missingness in the outcome depended on its true value? |

**Guidance:**
- The importance of missing data depends on the frequency, variability, and reasons for missingness. Do not set arbitrary thresholds (e.g., "20% missing = high risk"). Instead, consider whether the proportion and reasons for missingness could have biased the result.

**Domain-Level Algorithm:**
- **Low risk**: Data available for all/nearly all participants, OR evidence that result was not biased by missing data
- **Some concerns**: Missingness could depend on true value, but unlikely to have done so
- **High risk**: Missingness likely depends on its true value

---

#### Domain 4: Bias in Measurement of the Outcome

This domain addresses whether the outcome was measured appropriately and whether measurement could have been influenced by knowledge of intervention assignment.

**Signalling Questions:**

| Question | Text |
|----------|------|
| 4.1 | Was the method of measuring the outcome inappropriate? |
| 4.2 | Could measurement or ascertainment of the outcome have differed between intervention groups? |
| 4.3 | If N/PN/NI to 4.1 and 4.2: Were outcome assessors aware of the intervention received by study participants? |
| 4.4 | If Y/PY/NI to 4.3: Could assessment of the outcome have been influenced by knowledge of intervention received? |
| 4.5 | If Y/PY/NI to 4.4: Is it likely that assessment of the outcome was influenced by knowledge of intervention received? |

**Guidance:**
- Lack of blinding of outcome assessors does NOT automatically indicate bias. For truly objective outcomes (e.g., all-cause mortality), knowledge of assignment is unlikely to influence assessment. The downstream questions (4.4 and 4.5) determine whether knowledge of assignment actually mattered.

**Domain-Level Algorithm:**
- **Low risk**: Method was appropriate AND measurement did not differ between groups, AND assessors were blinded OR knowledge of assignment could not influence assessment
- **Some concerns**: Assessors were aware, and knowledge could (but probably did not) influence assessment
- **High risk**: Measurement was inappropriate, OR knowledge of assignment likely influenced assessment

---

#### Domain 5: Bias in Selection of the Reported Result

This domain addresses whether the reported result was selected from multiple possible results.

**Signalling Questions:**

| Question | Text |
|----------|------|
| 5.1 | Were the data that produced this result analysed in accordance with a pre-specified analysis plan that was finalized before unblinded outcome data were available for analysis? |
| 5.2 | If N/PN/NI to 5.1: Is the numerical result being assessed likely to have been selected, on the basis of the results, from multiple eligible outcome measurements (e.g., scales, definitions, time points) within the outcome domain? |
| 5.3 | If N/PN/NI to 5.1: Is the numerical result being assessed likely to have been selected, on the basis of the results, from multiple eligible analyses of the data? |

**Guidance:**
- A missing or unavailable analysis plan does NOT automatically indicate high risk. Protocols, trial register entries (e.g., ClinicalTrials.gov), statistical analysis plans, or methods sections from earlier publications can serve as evidence of pre-specification. The key is whether the plan predated the availability of unblinded outcome data.

**Domain-Level Algorithm:**
- **Low risk**: Data analyzed per pre-specified plan, OR no evidence of selection among multiple measurements or analyses
- **Some concerns**: Analysis plan was not pre-specified, but there is no reason to suspect selection
- **High risk**: Numerical result likely selected based on results from multiple measurements or analyses

---

### 1.4 Overall Risk-of-Bias Judgment

| Overall Judgment | Criteria |
|-----------------|----------|
| **Low risk of bias** | Low risk of bias across ALL five domains |
| **Some concerns** | Some concerns in at least one domain, but not judged to be at high risk of bias for any domain |
| **High risk of bias** | High risk of bias in at least one domain, OR some concerns for multiple domains in a way that substantially lowers confidence in the result |

The overall risk-of-bias judgment is effectively the **least favorable** assessment across all domains. Algorithms generate proposed judgments, but reviewers may override these with justification.

### 1.5 Specialized Variants

- **RoB 2 for cluster-randomized trials**: Includes an additional domain (Domain 1b) addressing bias arising from identification or recruitment of individual participants within clusters
- **RoB 2 for crossover trials**: Modified to account for the within-participant design, including considerations of carry-over effects and period-specific biases

---

## 2. ROBINS-I -- Risk of Bias in Non-Randomized Studies of Interventions

### 2.1 Overview

ROBINS-I (Risk Of Bias In Non-randomized Studies -- of Interventions) was first published in 2016. **Version 2 (ROBINS-I V2)** was released in November 2025, with major updates including the addition of formal algorithms (mirroring RoB 2) that map answers to signalling questions onto proposed risk-of-bias judgments.

The tool is designed to assess risk of bias in a specific result from an individual non-randomized study that examines the effect of an intervention on an outcome. It uses the concept of a **target trial** -- an idealized randomized trial that the non-randomized study is attempting to emulate -- as the basis for judgment.

### 2.2 Judgment Levels

ROBINS-I uses a five-level judgment scale:

| Judgment | Interpretation |
|----------|---------------|
| **Low risk of bias** | The study is comparable to a well-performed randomized trial with respect to this domain |
| **Moderate risk of bias** | The study provides sound evidence for a non-randomized study but cannot be considered comparable to a well-performed randomized trial |
| **Serious risk of bias** | The study has some important problems in this domain |
| **Critical risk of bias** | The study is too problematic in this domain to provide any useful evidence on the effects being studied |
| **No information** | Insufficient information to make a judgment |

### 2.3 The Seven Bias Domains

The seven domains are organized chronologically relative to the intervention:

---

#### Pre-Intervention Domains

**Domain 1: Bias Due to Confounding**

This is often the most critical domain for non-randomized studies. Confounding occurs when baseline prognostic variables predict both the intervention received and the outcome. ROBINS-I V2 now specifically addresses **baseline confounding** (the primary concern) while also considering time-varying confounding when individuals switch interventions after baseline.

Signalling questions address:
- Whether important confounding domains were identified and appropriately measured
- Whether confounders were controlled for in the analysis using appropriate methods
- Whether there were important confounders not controlled for
- Whether time-varying confounding was addressed if applicable

**Domain 2: Bias in Selection of Participants into the Study**

Selection bias occurs when excluding eligible participants or selecting the start of follow-up creates spurious associations between intervention and outcome, independent of the true intervention effect.

Signalling questions address:
- Whether start of follow-up and start of intervention coincide
- Whether selection into the study was related to both intervention and outcome
- Whether adjustment was made for selection differences

---

#### At-Intervention Domain

**Domain 3: Bias in Classification of Interventions**

This domain assesses whether intervention status was correctly defined and recorded. Misclassification can be:
- **Non-differential**: Misclassification unrelated to the outcome (typically biases toward the null)
- **Differential**: Misclassification related to the outcome (produces unpredictable bias)

Signalling questions address:
- Whether intervention groups were clearly defined
- Whether information used to define intervention groups was recorded at the start of the intervention
- Whether classification of intervention status could have been affected by knowledge of the outcome

---

#### Post-Intervention Domains

**Domain 4: Bias Due to Deviations from Intended Interventions**

This domain mirrors Domain 2 in RoB 2. It considers systematic differences in care or exposure between groups that represent deviations from intended interventions. Assessment depends on whether the effect of interest is assignment to or adherence to the intervention.

Signalling questions address:
- Whether there were deviations from intended intervention
- Whether they were balanced between groups
- Whether an appropriate analysis was used
- Whether investigators were aware of participants' assigned intervention

**Domain 5: Bias Due to Missing Data**

Bias arises from differential loss to follow-up affecting prognostic factors or exclusion of individuals with missing data on intervention, confounder, or outcome variables.

Signalling questions address:
- Whether outcome data were available for all or nearly all participants
- Whether participants excluded from analysis differed from those included
- Whether appropriate methods were used to account for missing data

**Domain 6: Bias in Measurement of Outcomes**

This domain assesses measurement error in the outcome, particularly when it differs between intervention groups or when assessors are aware of intervention status.

Signalling questions address:
- Whether the outcome measure was appropriate
- Whether outcome assessment methods were comparable across groups
- Whether outcome assessors were blinded to intervention status

**Domain 7: Bias in Selection of the Reported Result**

Selective reporting occurs when results are reported selectively based on findings, preventing appropriate inclusion in meta-analysis.

Signalling questions address:
- Whether the reported result was pre-specified
- Whether multiple outcome measurements, analyses, or subgroups were available
- Whether there is evidence of selection based on the direction or significance of results

---

### 2.4 Overall Risk-of-Bias Judgment

| Overall Judgment | Criteria |
|-----------------|----------|
| **Low risk** | Low risk of bias across ALL seven domains |
| **Moderate risk** | Low or moderate risk of bias in all domains |
| **Serious risk** | Serious risk of bias in at least one domain, but not critical in any |
| **Critical risk** | Critical risk of bias in at least one domain |
| **No information** | No information available in one or more domains, preventing a judgment |

### 2.5 Key Changes in ROBINS-I V2 (November 2025)

- **Addition of algorithms**: Like RoB 2, formal algorithms now map signalling question responses to proposed domain-level judgments
- **Graded response options**: Some questions now have "strong yes/no" versus "weak yes/no" responses to better discriminate between risk levels
- **Domain 1 revision**: Now specifically addresses baseline confounding rather than general confounding
- **Improved usability**: Reorganized structure and clearer guidance for signalling questions

---

## 3. ROBINS-E -- Risk of Bias in Non-Randomized Studies of Exposures

### 3.1 Overview

ROBINS-E was published in *Environment International* on March 24, 2024, making it the newest of the three Cochrane RoB tools. The launch version was first posted on riskofbias.info in June 2022, with the formal publication following. It was developed to assess risk of bias in estimates of the causal effect of an **exposure** (rather than a deliberate intervention) on an outcome.

ROBINS-E targets **cohort (follow-up) studies** specifically, with future variants planned for case-control and other observational designs.

### 3.2 The Seven ROBINS-E Domains

| Domain | Description |
|--------|-------------|
| 1. Bias due to confounding | Whether confounding factors (prognostic for outcome AND predictive of exposure) were adequately controlled |
| 2. Bias arising from measurement of the exposure | Whether the exposure was measured accurately and whether misclassification occurred |
| 3. Bias in selection of participants into the study | Whether selection was related to both exposure and outcome |
| 4. Bias due to post-exposure interventions | Whether post-exposure interventions influenced by prior exposure affected the outcome |
| 5. Bias due to missing data | Whether missing data on exposure, outcome, or confounders biased results |
| 6. Bias in measurement of the outcome | Whether outcome measurement differed between exposure groups |
| 7. Bias in selection of the reported result | Whether results were selectively reported based on their direction or significance |

### 3.3 Key Differences from ROBINS-I

| Feature | ROBINS-I | ROBINS-E |
|---------|----------|----------|
| **Study focus** | Non-randomized studies of deliberate health **interventions** | Non-randomized studies of **exposures** (environmental, occupational, behavioral) |
| **Domain 2** | Bias in **selection of participants** | Bias in **measurement of the exposure** |
| **Domain 3** | Bias in **classification of interventions** | Bias in **selection of participants** |
| **Domain 4** | Bias due to **deviations from intended interventions** | Bias due to **post-exposure interventions** |
| **Judgment scale** | Low / Moderate / Serious / Critical / No information | Low / Some concerns / High / Very high risk of bias |
| **Confounding (Domain 1)** | Best achievable: Low risk | Best achievable: "Low risk of bias (except for concerns about uncontrolled confounding)" -- acknowledges that uncontrolled confounding can never be fully ruled out in exposure studies |
| **Target concept** | Target trial (hypothetical RCT the study emulates) | Target experiment (hypothetical randomized experiment, though randomization is usually infeasible for exposures) |
| **Signalling question responses** | Standard: Y/PY/PN/N/NI | Includes additional graded responses (e.g., "weak no" vs "strong no") for some questions |

### 3.4 Confounding Domain Details

The confounding domain in ROBINS-E includes a particularly detailed assessment. The first signalling question asks: "Did the authors control for all the important confounding factors for which this was necessary?" with graded response options:
- **Yes** / **Probably yes**
- **Weak No**: "No, but uncontrolled confounding was probably not substantial"
- **Strong No**: "No, and uncontrolled confounding was probably substantial"

This graded response structure helps discriminate between moderate and serious/critical risk of bias from confounding.

### 3.5 Domain-Level and Overall Judgments

Each domain generates three summary assessments:
1. Risk of bias in the result
2. Predicted direction of bias
3. Threats to conclusions

The overall risk-of-bias judgment defaults to the **domain with the greatest risk of bias**, though assessors can override this with justification.

---

## 4. Assessment Workflow

### 4.1 Step-by-Step Process

The workflow for conducting a risk-of-bias assessment using any of the Cochrane tools follows a structured process:

**Step 1: Protocol Specification**

Before any assessment begins, the review protocol should specify:
- Which outcomes will be assessed for bias
- Which version of the tool will be used (RoB 2, ROBINS-I V2, or ROBINS-E)
- The effect of interest (effect of assignment vs. effect of adherence for RoB 2)
- The process for resolving disagreements between reviewers
- Any review-specific guidance for answering signalling questions

**Step 2: Piloting**

Before assessing all included studies, the review team should pilot the tool:
- Select 3-6 representative studies covering a range of study quality
- Each reviewer independently assesses these studies
- Compare results and discuss discrepancies
- Develop review-specific supplementary guidance based on piloting experience
- This step significantly improves inter-rater reliability

**Step 3: Independent Assessment**

- At least **two reviewers** independently assess each result
- Each reviewer reads the full study report (including any supplementary materials, protocols, or trial register entries)
- For each domain, reviewers answer all signalling questions sequentially
- Each answer must be supported by a **free-text justification** citing specific evidence from the study
- The algorithm generates a proposed domain-level judgment

**Step 4: Domain-Level Judgments**

- Review the algorithm's proposed judgment
- The proposed judgment can be overridden if the reviewer believes the algorithm does not capture the nuance of the situation -- but overrides must be justified
- Record the domain-level judgment: Low risk / Some concerns / High risk (for RoB 2) or Low / Moderate / Serious / Critical (for ROBINS-I)

**Step 5: Overall Judgment**

- The overall judgment is derived from domain-level judgments
- The algorithm proposes a judgment based on the worst domain-level assessment
- As with domain-level judgments, the overall judgment can be overridden with justification

**Step 6: Reconciliation**

- Both reviewers compare their independent assessments
- Disagreements are resolved through discussion
- If consensus cannot be reached, a third reviewer (or senior author) adjudicates
- The final consensus judgment is recorded along with supporting justifications

**Step 7: Documentation**

- All signalling question responses, domain judgments, overall judgments, and free-text justifications are recorded
- These are typically entered into systematic review software (RevMan, Covidence, etc.)
- The documentation enables transparency and reproducibility

### 4.2 Practical Considerations

**Assessment Granularity:**
- RoB 2 assesses bias per **result** (not per study). If a trial contributes data to multiple outcomes in a review, a separate RoB 2 assessment may be needed for each result.
- ROBINS-I and ROBINS-E similarly assess bias at the result level.

**Information Sources:**
- Full text of the primary publication
- Supplementary materials and appendices
- Trial registration records (e.g., ClinicalTrials.gov, ISRCTN)
- Published protocols or statistical analysis plans
- Previous publications from the same trial
- Contact with trial authors (when information is unclear)

**Common Pitfalls (from Cochrane's Ten Tips):**
1. Assessing entire studies rather than specific results
2. Failing to specify the effect of interest in the protocol
3. Assuming baseline imbalance always indicates randomization failure
4. Assuming lack of blinding always causes bias
5. Assuming intervention switching always introduces bias
6. Setting arbitrary thresholds for missing data
7. Assuming a missing analysis plan automatically means high risk
8. Skipping signalling questions or not using the algorithms
9. Not piloting the tool before full assessment
10. Insufficient free-text justification for judgments

---

## 5. Output Formats and Visualization

### 5.1 Traffic Light Plots

The most common visualization for individual study-level assessments. Each row represents one study, and each column represents one bias domain. Cells are colored:

**RoB 2 color scheme:**
- Green (+): Low risk of bias
- Yellow (?): Some concerns
- Red (-): High risk of bias

**ROBINS-I color scheme:**
- Green: Low risk
- Yellow: Moderate risk
- Orange: Serious risk
- Red: Critical risk
- White/Gray: No information

Traffic light plots display the specific domain-level judgments for each study, providing a detailed view of where bias may arise.

### 5.2 Summary Bar Plots (Weighted)

Summary plots show the **distribution of risk-of-bias judgments** across all studies for each domain as a weighted bar chart. Each bar represents one domain, and the segments show the proportion of studies judged as low risk, some concerns, and high risk (or the equivalent categories for ROBINS-I/E). These plots can be weighted by the inverse variance weight of each study in the meta-analysis.

### 5.3 Standard Table Formats

Risk-of-bias assessments are also commonly presented as structured tables:
- **Summary of findings tables**: Incorporate the overall risk-of-bias judgment alongside effect estimates
- **Characteristics of included studies tables**: Include domain-level judgments for each study
- **Detailed assessment tables**: Show signalling question responses, domain judgments, and supporting text

### 5.4 Forest Plots with RoB Integration

Modern forest plots can include risk-of-bias information as colored symbols (traffic lights) adjacent to each study's effect estimate, allowing readers to see bias judgments alongside the quantitative results.

---

## 6. Existing Software Tools

### 6.1 robvis (R Package)

**Developer**: Luke McGuinness (University of Bristol / MRC Integrative Epidemiology Unit)

**Overview**: robvis is the most widely used dedicated visualization package for risk-of-bias assessments, cited in over **1,500 academic articles** as of 2025.

**Key Features:**
- Generates publication-quality traffic light plots and summary bar plots
- Supports templates for: **RoB 2, RoB 2 (Cluster), ROBINS-I, ROBINS-E, QUADAS-2, QUIPS**, and a generic template
- Two predefined color schemes: "cochrane" (standard green/yellow/red) and "colourblind" (accessible palette)
- Custom color palettes via hex codes
- Available both as an R package (`install.packages("robvis")`) and a **web application** for users unfamiliar with R

**Usage:**
```r
library(robvis)
rob_summary(data, tool = "ROB2", colour = "cochrane")
rob_traffic_light(data, tool = "ROB2", colour = "cochrane")
```

**Web App**: https://mcguinlu.shinyapps.io/robvis/ (also accessible via riskofbias.info)

### 6.2 RevMan Web (Review Manager)

**Developer**: Cochrane

**Overview**: RevMan Web is Cochrane's official systematic review authoring and management software. As of June 2024, all intervention reviews have study-centric data management enabled by default.

**RoB Features:**
- Built-in RoB 2 assessment module (can be enabled per review)
- Enter signalling question responses directly in the platform
- Automatically generates forest plots with integrated traffic light symbols
- Generates risk-of-bias tables and summary figures
- Supports both original RoB (Version 1) and RoB 2
- In January 2025, new random-effects methods were added

**Limitations**: Primarily designed for Cochrane Reviews; less flexibility for non-Cochrane projects

### 6.3 Covidence

**Overview**: A web-based systematic review management platform widely used by both Cochrane and non-Cochrane review teams.

**RoB Features:**
- Customizable quality assessment templates
- Supports RoB 2, ROBINS-I, and other critical appraisal tools
- Risk-of-bias tables with data extraction
- Conflict resolution workflow for disagreements between reviewers
- Export to RevMan format

### 6.4 EPPI-Reviewer

**Developer**: EPPI-Centre, University College London

**Overview**: A comprehensive systematic review software platform used for all stages of evidence synthesis.

**RoB Features:**
- Built-in coding tools for risk-of-bias assessment
- Integration with RobotReviewer for semi-automated assessment
- Customizable assessment templates
- Support for multiple review types and bias tools

### 6.5 JBI SUMARI

**Developer**: Joanna Briggs Institute (JBI)

**Overview**: System for the Unified Management, Assessment, and Review of Information.

**RoB Features:**
- Includes 13 different study design-based critical appraisal forms
- Supports Cochrane Risk of Bias tool
- Integrated quality assessment within the review workflow
- JBI is currently undertaking a comprehensive revision of its critical appraisal tools to align with recent advances in risk-of-bias assessment methodology

### 6.6 DistillerSR

**Overview**: AI-augmented systematic review software with built-in risk-of-bias assessment capabilities.

### 6.7 meta R Package

The `meta` R package also includes functions for risk-of-bias visualization:
```r
library(meta)
traffic_light(rob_data)
```

### 6.8 Online Tools at riskofbias.info

The official Cochrane risk-of-bias tools website provides:
- Downloadable Word/Excel templates for all three tools (RoB 2, ROBINS-I V2, ROBINS-E)
- The robvis web app for visualization
- Guidance documents and cribsheets
- Links to training resources

---

## 7. AI/Automation Efforts

### 7.1 RobotReviewer

**Developer**: Iain Marshall et al. (King's College London)

**Architecture**: RobotReviewer uses a multi-task machine learning model trained on algorithmically annotated data from 12,808 trial PDFs drawn from the Cochrane Database of Systematic Reviews. The system makes both article-level predictions (risk-of-bias judgments) and sentence-level predictions (identifying supporting text) using a joint learning approach that borrows strength across related bias assessment tasks.

**Performance Metrics:**
- Reviewers accepted **83%** of RobotReviewer's assessments vs. **81%** of peer (human) assessments (risk ratio 1.02, p=0.33, no statistically significant difference)
- Full agreement between RobotReviewer, one human reviewer, and final consensus: **79%** of questions
- By domain reliability (Cohen's kappa):
  - Random sequence generation: 0.48 (moderate)
  - Allocation concealment: 0.45 (moderate)
  - Blinding of participants/personnel: 0.42 (moderate)
  - Overall risk of bias: 0.34 (fair)
  - Incomplete outcome data: 0.14 (slight)
  - Blinding of outcome assessors: 0.10 (slight)
  - Selective reporting: 0.02 (slight)

**Time Savings**: Prior research demonstrated 25% faster completion when combining one reviewer with RobotReviewer versus fully manual assessment.

**Current Status**: RobotReviewer is recommended as a **supplementary third assessment** (alongside two human reviewers) rather than a replacement. It assesses the original Cochrane RoB tool domains (not yet updated for RoB 2).

**URL**: https://www.robotreviewer.net/

### 7.2 Large Language Models (LLMs) for RoB Assessment

#### Claude 2 Study (2024-2025)

**Publication**: Published in *Research Synthesis Methods* (Cambridge Core), originally preprinted on medRxiv (July 2024).

**Methodology**: Assessed 100 two-arm parallel RCTs from 78 Cochrane Reviews using RoB 2. Claude received full article text plus compressed protocols/register entries, with automated prompt engineering and three iterations per trial.

**Results:**
- Overall agreement (Cohen's kappa): **0.22** (fair agreement)
- Domain-specific kappa values ranged from 0.10 (slight) to 0.31 (fair)
- Domain 3 (missing data): kappa = 0.31 (best performing domain)
- Domain 5 (selective reporting): kappa = 0.10 (worst performing domain)
- Domain 4 (outcome measurement): highest observed agreement at 71%
- Overall judgment observed agreement: 41%

**Systematic Biases**: Claude tended to underestimate risk of bias, frequently assuming adequate blinding and allocation concealment when information was absent. The model occasionally provided incorrect supporting justifications.

**Conclusion**: "Claude's RoB 2 judgements cannot replace human risk of bias assessment, and its use within systematic reviews without further human validation cannot be recommended."

#### GPT-4 Studies (2024-2025)

**ChatGPT-4 with RoB 2** (Pitre et al.):
- Assessed 157 RCTs using RoB 2
- Cohen's kappa for overall assessment: **0.16** (slight agreement)

**ChatGPT-4 with ROBINS-I** (Hasan et al.):
- Cohen's kappa for overall assessment: **0.13** (slight agreement)

#### JMIR Evaluation Study (2025)

Published in the *Journal of Medical Internet Research*, this study evaluated LLM-assisted RoB 2 assessment. Key finding: LLMs achieved better accuracy when guided by **structured prompts** that process methodological details through structured reasoning frameworks. While not replacing human assessment, LLMs showed "strong potential for assisting RoB 2 evaluations" when used as decision support tools.

#### GEPA Framework (2025)

Published on arXiv (December 2025), this study proposed an automated RoB assessment framework using programmatic prompting. The Guided Extraction and Programmatic Assessment (GEPA) approach structures the LLM's reasoning process to systematically extract methodological information and apply the RoB 2 algorithm.

### 7.3 NLP-Based Approaches

A comprehensive review of 52 papers on systematic review automation found that **Natural Language Processing (NLP)** is the predominant technique used across all stages of systematic review automation. For risk-of-bias assessment specifically:
- **Text classification** is the most common NLP task -- categorizing study methods as having high/low bias
- NLP approaches achieve accuracy rates of approximately **71-78%** for identifying relevant text and classifying bias

### 7.4 Summary of AI Readiness

| Tool/Approach | Agreement with Humans | Readiness Level |
|---------------|----------------------|-----------------|
| RobotReviewer (ML) | 72-83% acceptance rate; kappa 0.02-0.48 by domain | Semi-operational (supplementary tool) |
| Claude 2 (LLM) | Kappa 0.10-0.31 by domain; 0.22 overall | Research stage -- not recommended for standalone use |
| GPT-4 (LLM) | Kappa 0.13-0.16 overall | Research stage -- not recommended for standalone use |
| Structured prompting (LLM) | Improved over naive prompting | Promising research direction |
| NLP text classification | 71-78% accuracy | Operational for text extraction; limited for judgment |

**Current Consensus**: As of early 2026, no AI tool is ready to replace human risk-of-bias assessment. The recommended approach is **human-AI collaboration**, where AI tools serve as a supplementary assessment alongside two independent human reviewers. AI tools show the most promise for:
1. Pre-populating signalling question responses for human review
2. Extracting relevant text passages that support bias judgments
3. Flagging studies that may have high risk of bias for priority review
4. Reducing the time burden of the initial assessment pass

A scoping review of LLMs for systematic reviews (published in the *Journal of Clinical Epidemiology*, 2025) concluded that LLM approaches covered 10 of 13 defined systematic review steps, with the most frequent applications being literature search (41%), study selection (38%), and data extraction (30%). Risk-of-bias assessment remained one of the more challenging steps for automation.

---

## Summary Comparison of the Three Tools

| Feature | RoB 2 | ROBINS-I (V2) | ROBINS-E |
|---------|-------|---------------|----------|
| **Year released** | 2019 | 2016 (V2: Nov 2025) | 2022 (published: Mar 2024) |
| **Study type** | Randomized trials | Non-randomized studies of interventions | Non-randomized studies of exposures |
| **Number of domains** | 5 | 7 | 7 |
| **Signalling questions** | 22 total | Variable per domain | Variable per domain |
| **Judgment levels** | Low / Some concerns / High | Low / Moderate / Serious / Critical / NI | Low / Some concerns / High / Very high |
| **Algorithms** | Yes (since 2019) | Yes (added in V2, 2025) | Yes |
| **Assessment unit** | Per result | Per result | Per result |
| **Reference standard** | Well-performed RCT | Hypothetical target trial | Hypothetical target experiment |
| **Specialized variants** | Cluster, Crossover | -- | Future: case-control |
| **Key unique feature** | Two effect-of-interest options | Target trial concept | Graded confounding responses |

---

## Key Resources

- **Official tool website**: https://www.riskofbias.info/
- **RoB 2 resources (Cochrane)**: https://methods.cochrane.org/bias/resources/rob-2-revised-cochrane-risk-bias-tool-randomized-trials
- **ROBINS-I V2**: https://www.riskofbias.info/welcome/robins-i-v2
- **ROBINS-E**: https://www.riskofbias.info/welcome/robins-e-tool
- **robvis R package**: https://github.com/mcguinlu/robvis
- **robvis web app**: https://mcguinlu.shinyapps.io/robvis/
- **Cochrane Handbook Chapter 8**: https://www.cochrane.org/authors/handbooks-and-manuals/handbook/current/chapter-08
- **RobotReviewer**: https://www.robotreviewer.net/
- **RevMan Web**: https://revman.cochrane.org/
- **Covidence**: https://www.covidence.org/
- **EPPI-Reviewer**: https://eppi.ioe.ac.uk/cms/
