# Medical Equipment Refurbishment

**Purpose:** Multi-org healthcare workflow demonstrating conditional routing, calculations, participant publishing to register, and verifiable credential issuance across three organisations
**Date Created:** 2026-02-20
**Status:** ✅ Complete
**Prerequisites:** Docker Desktop, PowerShell 7+, Sorcha services running

---

## Overview

This walkthrough demonstrates a realistic medical equipment refurbishment process involving three organisations and four participants. A hospital's biomedical engineer submits a defective device, the department head approves the budget, a refurbishment company quotes and performs the work, and — for safety-critical devices — a health authority compliance officer reviews regulatory compliance before return to service.

This is the **first walkthrough to exercise participant publishing** (spec 001), where all four participants are published to the register before blueprint execution begins.

The scenario exercises every major Sorcha capability:
- **Participant publishing** — 4 participants published to register via Tenant Service pipeline
- **Multi-organization participation** — 3 separate orgs with independent wallets
- **Multi-user within one org** — Hospital has both a Biomedical Engineer and Department Head
- **Conditional routing** — safety-critical devices go through regulatory review, routine devices skip it
- **Calculations** — risk category from device class + failure type, estimated cost from class + complexity
- **Rejection paths** — Department Head, Lead Technician, and Compliance Officer can reject back
- **Verifiable credential issuance** — completed refurbishments produce a signed certificate VC

---

## Scenario

### The Story

**City General Hospital** has a defective medical device that needs refurbishment. Their biomedical engineer submits it through Sorcha, which routes it through departmental budget approval, the refurbishment company's quote and acceptance, an optional regulatory compliance review (for safety-critical devices), and finally back to the refurbishment company for completion and certification.

Three test runs demonstrate the branching:
1. **Routine** — Class IIa patient monitor with electrical fault, skips regulatory review (4 actions)
2. **Safety-Critical** — Class III defibrillator with safety failure, triggers regulatory review (5 actions)
3. **Rejection** — Class IIb device rejected at quote stage as beyond economical repair (3 actions)

### Organisations

| Organisation | Role | Description |
|---|---|---|
| **City General Hospital** | Healthcare Provider | Two participants: Biomedical Engineer (submits device) and Department Head (approves budget) |
| **MedTech Refurbishment Ltd** | Refurbishment Company | Lead Technician quotes, accepts, and performs the refurbishment work |
| **Regional Health Authority** | Regulatory Body | Compliance Officer performs regulatory review for safety-critical devices (conditional) |

### Participants

| Participant ID | Display Name | Organisation | Role in Workflow |
|---|---|---|---|
| `biomedical-engineer` | Biomedical Engineer | City General Hospital | Submits refurbishment request (Action 1) |
| `department-head` | Department Head | City General Hospital | Budget approval (Action 2) |
| `lead-technician` | Lead Technician | MedTech Refurbishment Ltd | Quote, accept, complete, certify (Actions 3, 5) |
| `compliance-officer` | Compliance Officer | Regional Health Authority | Regulatory review for safety-critical devices (Action 4, conditional) |

---

## Workflow

### Flow Diagram

```
                          ┌──────────────────────────┐
                          │  [1] Submit Refurbishment │
                          │  Request                  │
                          │  (biomedical-engineer)    │
                          └────────────┬──────────────┘
                                       │
                          ┌────────────▼──────────────┐
                          │  [2] Departmental Approval │
                     ┌────│  (department-head)         │
                     │    │                            │
                     │    │  Can REJECT -> [1]         │
                     │    └────────────┬───────────────┘
                     │                 │
                     │    ┌────────────▼──────────────┐
                     │    │  [3] Quote & Accept Job    │
                     │ ┌──│  (lead-technician)         │──┐
                     │ │  │                            │  │
                     │ │  │  Calculates:               │  │
                     │ │  │  - riskCategory             │  │
                     │ │  │  - estimatedCost            │  │
                     │ │  │  Can REJECT -> [1]         │  │
                     │ │  └────────────────────────────┘  │
                     │ │                                   │
                     │ │  riskCategory ==               riskCategory ==
                     │ │  "safety-critical"              "routine"
                     │ │         │                          │
                     │ │  ┌──────▼──────────────────┐      │
                     │ │  │  [4] Regulatory Hold    │      │
                     │ │  │  & Review               │      │
                     │ │  │  (compliance-officer)   │      │
                     │ │  │                         │      │
                     │ │  │  Can REJECT -> [1]      │      │
                     │ │  └──────────┬──────────────┘      │
                     │ │             │                      │
                     │ │             └────────┬─────────────┘
                     │ │                      │
                     │ │         ┌────────────▼──────────────┐
                     │ │         │  [5] Complete & Certify   │
                     │ │         │  (lead-technician)        │
                     │ │         │                           │
                     │ │         │  Issues:                  │
                     │ │         │  Refurbishment Cert VC    │
                     │ │         └───────────────────────────┘
```

### Action Details

#### Action 1: Submit Refurbishment Request
**Participant:** `biomedical-engineer` (City General Hospital)
**Purpose:** Submit a defective medical device for refurbishment assessment

| Field | Type | Validation | Description |
|---|---|---|---|
| `deviceName` | string | required, min 3 chars | Name of the medical device |
| `deviceClass` | enum | Class I, IIa, IIb, III | EU MDR device classification |
| `manufacturer` | string | required | Device manufacturer |
| `serialNumber` | string | required | Serial number |
| `failureType` | enum | electrical, mechanical, software, calibration, safety, cosmetic | Type of failure |
| `failureDescription` | string | required | Detailed failure description |
| `department` | string | required | Originating hospital department |
| `complexityFactor` | number | 1-10 | Estimated repair complexity |
| `urgency` | enum | routine, urgent, critical | Repair urgency |

**Disclosures:**
- `biomedical-engineer` sees all submitted data
- `department-head` sees: deviceName, deviceClass, manufacturer, failureType, department, urgency (summary for budget decision — not technical details)

---

#### Action 2: Departmental Approval
**Participant:** `department-head` (City General Hospital)
**Purpose:** Review and approve the refurbishment budget

| Field | Type | Description |
|---|---|---|
| `budgetApproved` | boolean | Budget approved for this refurbishment |
| `budgetCode` | string | Internal budget code (e.g. MED-20260042) |
| `approvalNotes` | string | Approval notes and conditions |

**Rejection:** Can reject back to Action 1 (biomedical engineer must resubmit)

**Disclosures:**
- `department-head` sees all accumulated data
- `lead-technician` sees: deviceName, deviceClass, manufacturer, serialNumber, failureType, failureDescription, complexityFactor, urgency (full technical details needed for quote)

---

#### Action 3: Quote & Accept Job
**Participant:** `lead-technician` (MedTech Refurbishment Ltd)
**Purpose:** Assess device, calculate risk and cost, provide quote, accept/reject job

| Field | Type | Description |
|---|---|---|
| `initialAssessment` | string | Technical assessment of device condition |
| `partsRequired` | string | List of parts needed for repair |
| `estimatedDays` | integer | Estimated working days to complete |
| `jobAccepted` | boolean | Whether the job is accepted |

**Calculations (JSON Logic):**

Risk category:
```json
{
  "riskCategory": {
    "if": [
      { "or": [
        { "==": [{"var":"deviceClass"}, "Class III"] },
        { "and": [
          { "in": [{"var":"deviceClass"}, ["Class IIa","Class IIb"]] },
          { "==": [{"var":"failureType"}, "safety"] }
        ]}
      ]},
      "safety-critical",
      "routine"
    ]
  }
}
```

Estimated cost:
```json
{
  "estimatedCost": {
    "max": [500, {"*": [
      {"if": [
        {"==": [{"var":"deviceClass"}, "Class III"]}, 4,
        {"==": [{"var":"deviceClass"}, "Class IIb"]}, 3,
        {"==": [{"var":"deviceClass"}, "Class IIa"]}, 2,
        1
      ]},
      {"var": "complexityFactor"},
      250
    ]}]
  }
}
```

**Risk category examples:**
| Device Class | Failure Type | Risk Category |
|---|---|---|
| Class III | any | safety-critical |
| Class IIa/IIb | safety | safety-critical |
| Class IIa | electrical | routine |
| Class I | any | routine |

**Cost examples:**
| Device Class | Complexity | Formula | Estimated Cost |
|---|---|---|---|
| Class IIa | 4 | max(500, 2 * 4 * 250) | 2,000 |
| Class III | 7 | max(500, 4 * 7 * 250) | 7,000 |
| Class IIb | 8 | max(500, 3 * 8 * 250) | 6,000 |
| Class I | 2 | max(500, 1 * 2 * 250) | 500 (minimum) |

**Routing (conditional):**
- If `riskCategory == "safety-critical"` -> Route to **Action 4** (Regulatory Hold)
- If `riskCategory == "routine"` -> Route to **Action 5** (Complete & Certify)

**Rejection:** Can reject back to Action 1 (device beyond economical repair)

---

#### Action 4: Regulatory Hold & Review (Conditional)
**Participant:** `compliance-officer` (Regional Health Authority)
**Purpose:** Regulatory compliance review for safety-critical devices

*This action is only reached when the risk category is "safety-critical".*

| Field | Type | Description |
|---|---|---|
| `regulatoryStandard` | enum | IEC 62353, IEC 60601, ISO 13485, MDR 2017/745 |
| `complianceStatus` | enum | compliant, conditionally-compliant, non-compliant |
| `regulatoryConditions` | string | Conditions that must be met before return to service |
| `reinspectionRequired` | boolean | Whether post-refurbishment reinspection is required |

**Rejection:** Can reject back to Action 1 if device cannot be safely refurbished

**Disclosures:**
- `compliance-officer` sees: deviceName, deviceClass, manufacturer, serialNumber, failureType, failureDescription, riskCategory, initialAssessment, partsRequired (no budget/cost data)
- `lead-technician` sees: regulatoryStandard, complianceStatus, regulatoryConditions, reinspectionRequired

---

#### Action 5: Complete & Certify
**Participant:** `lead-technician` (MedTech Refurbishment Ltd)
**Purpose:** Complete refurbishment and issue Refurbishment Certificate VC

| Field | Type | Description |
|---|---|---|
| `workPerformed` | string | Description of work completed |
| `partsReplaced` | string | Detailed list of replaced parts with part numbers |
| `testResults` | string | Post-refurbishment test results |
| `safetyTestPassed` | boolean | Whether all safety tests passed |
| `certificateNumber` | string | Refurbishment certificate number (e.g. RC-2026-00142) |
| `returnToServiceDate` | date | Date device can return to clinical use |

**Credential Issuance:**
On completion, a **Refurbishment Certificate Verifiable Credential** is minted:

```json
{
  "type": ["VerifiableCredential", "RefurbishmentCertificateCredential"],
  "issuer": "did:sorcha:w:{lead-technician-wallet}",
  "credentialSubject": {
    "certificateNumber": "RC-2026-00142",
    "deviceName": "Philips IntelliVue MX800 Patient Monitor",
    "deviceClass": "Class IIa",
    "manufacturer": "Philips Healthcare",
    "serialNumber": "PHM-2021-44821",
    "riskCategory": "routine",
    "workPerformed": "Replaced three failed capacitors...",
    "safetyTestPassed": true,
    "returnToServiceDate": "2026-03-01"
  },
  "expirationDate": "2027-03-01"
}
```

**Disclosures:**
- `lead-technician` sees all accumulated data
- `biomedical-engineer` sees: certificateNumber, workPerformed, partsReplaced, testResults, safetyTestPassed, returnToServiceDate, estimatedCost

---

## Test Scenarios

### Scenario A: Routine Refurbishment (Happy Path)

A Class IIa patient monitor with an electrical fault — routine risk, skips regulatory review.

| Step | Action | Participant | Key Input |
|---|---|---|---|
| 1 | Submit Request | biomedical-engineer | Class IIa, electrical, complexity 4 |
| 2 | Departmental Approval | department-head | budgetApproved: true |
| 3 | Quote & Accept | lead-technician | 5 days, job accepted |
| -- | *riskCategory = "routine", routes to Action 5 (skips regulatory)* | | |
| 4 | Complete & Certify | lead-technician | safety tests passed, certificate issued |

**Expected:** 4 actions executed, risk category "routine", estimated cost 2,000, Refurbishment Certificate VC issued.

### Scenario B: Safety-Critical Refurbishment

A Class III defibrillator with a safety failure — triggers the regulatory review branch.

| Step | Action | Participant | Key Input |
|---|---|---|---|
| 1 | Submit Request | biomedical-engineer | Class III, safety, complexity 7 |
| 2 | Departmental Approval | department-head | budgetApproved: true, priority |
| 3 | Quote & Accept | lead-technician | 14 days, job accepted |
| -- | *riskCategory = "safety-critical", routes to Action 4 (regulatory review)* | | |
| 4 | Regulatory Hold | compliance-officer | IEC 60601, conditionally-compliant |
| 5 | Complete & Certify | lead-technician | accredited lab testing, certificate issued |

**Expected:** 5 actions executed, risk category "safety-critical", estimated cost 7,000, regulatory conditions attached, Refurbishment Certificate VC issued.

### Scenario C: Rejection (Beyond Economical Repair)

A Class IIb patient monitor with extensive water damage — rejected at quote stage.

| Step | Action | Participant | Key Input |
|---|---|---|---|
| 1 | Submit Request | biomedical-engineer | Class IIb, mechanical, complexity 8 |
| 2 | Departmental Approval | department-head | budgetApproved: true (assessment only) |
| 3 | Quote & Accept | lead-technician | jobAccepted: false, REJECT |

**Expected:** Workflow rejected at action 3, routes back to biomedical engineer with BER assessment.

---

## Files in This Walkthrough

| File | Description |
|---|---|
| `README.md` | This file — scenario overview and action specifications |
| `medical-equipment-refurb-template.json` | Blueprint template with parameterised participants |
| `test-medical-equipment-refurb.ps1` | Main walkthrough script (all 3 scenarios) |
| `data/scenario-a-routine.json` | Input data for routine refurbishment scenario |
| `data/scenario-b-safety-critical.json` | Input data for safety-critical scenario |
| `data/scenario-c-rejection.json` | Input data for rejection scenario |

---

## Quick Start

```powershell
# 1. Ensure Docker services are running
docker-compose up -d

# 2. Run the walkthrough
./walkthroughs/MedicalEquipmentRefurb/test-medical-equipment-refurb.ps1

# Or run a specific scenario
./walkthroughs/MedicalEquipmentRefurb/test-medical-equipment-refurb.ps1 -Scenario A

# Run with verbose JSON output
./walkthroughs/MedicalEquipmentRefurb/test-medical-equipment-refurb.ps1 -Scenario B -ShowJson

# Run with Aspire profile
./walkthroughs/MedicalEquipmentRefurb/test-medical-equipment-refurb.ps1 -Scenario All -Profile aspire
```

---

## Key Concepts Demonstrated

| Concept | Where |
|---|---|
| Participant publishing to register | Phase 4b — all 4 participants published before blueprint execution |
| Template parameterisation | Participant wallet addresses injected at instance creation |
| Conditional routing | Action 3 branches on riskCategory (safety-critical or routine) |
| Rejection routing | Actions 2, 3, 4 can reject back to Action 1 |
| Chained calculations | Risk category (Action 3) feeds routing; estimated cost feeds disclosure |
| Selective disclosure | Each participant sees only what they need (no cost data for compliance) |
| Multi-user same org | Biomedical Engineer and Department Head are both City General Hospital |
| Verifiable credential | Refurbishment Certificate VC issued on completion |
| Credential downstream use | Hospital can present the certificate to regulators/insurers |

---

## Related Documentation

- [Blueprint Builder Skill](../../.claude/skills/blueprint-builder/)
- [Participant Identity API](../../CLAUDE.md#participant-identity-api)
- [ConstructionPermit Walkthrough](../ConstructionPermit/) — similar multi-org example without participant publishing
- [OrganizationPingPong Walkthrough](../OrganizationPingPong/) — simpler 2-participant example
- [CLAUDE.md](../../CLAUDE.md) — project conventions
