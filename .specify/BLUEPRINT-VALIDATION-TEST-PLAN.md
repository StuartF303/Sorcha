# Blueprint Validation Test Plan

**Created:** 2025-11-18
**Purpose:** Define comprehensive tests to validate Blueprint functionality against project goals
**Status:** Active Development Plan

---

## Executive Summary

This plan defines the test cases needed to ensure Blueprint functionality matches the Sorcha Platform's workflow execution requirements. Since authentication/authorization is handled by the tenant service (external identity), we focus on validating the **Blueprint Schema**, **workflow integrity**, and **execution logic**.

---

## Blueprint Schema Requirements

Based on `Sorcha.Blueprint.Models.Blueprint`:

### Core Requirements
- ✅ ID: Unique identifier (max 64 chars)
- ✅ Title: 3-200 chars, required
- ✅ Description: 5-2000 chars, required
- ✅ Version: Integer, defaults to 1
- ✅ Participants: Minimum 2 required
- ✅ Actions: Minimum 1 required
- ⚠️ DataSchemas: Optional embedded JSON schemas
- ⚠️ Metadata: Optional key-value pairs
- ✅ Timestamps: CreatedAt, UpdatedAt

### Participant Requirements
- ✅ ID: Required, max 64 chars
- ✅ Name: Required, 1-100 chars
- ✅ Organisation: Required, 1-200 chars
- ⚠️ **WalletAddress: Max 100 chars** (critical for cryptography)
- ⚠️ DidUri: Optional DID identifier
- ⚠️ VerifiableCredential: Optional JSON-LD credential
- ⚠️ UseStealthAddress: Boolean for privacy

### Action Requirements
- ✅ Id: Required (integer)
- ✅ Title: Required, 1-100 chars
- ⚠️ **Sender: Participant ID reference** (must exist)
- ⚠️ **Participants: Routing conditions** (JSON Logic)
- ⚠️ **Disclosures: Minimum 1 required** (data visibility rules)
- ⚠️ **DataSchemas: Optional JSON schemas for action data**
- ⚠️ **Condition: Routing logic** (JSON Logic, defaults to {\"==\":[0,0]})
- ⚠️ **Calculations: Optional data transformations** (JSON Logic)
- ⚠️ Form: UI layout specification

---

## Test Categories

### ✅ Category 1: Model Validation Tests (EXISTS)

**Status:** Implemented in `Sorcha.Blueprint.Models.Tests`

- [x] Blueprint constructor initializes defaults
- [x] ID uniqueness
- [x] Title validation (min/max length, required)
- [x] Description validation (min/max length, required)
- [x] Version defaults and custom values
- [x] Participants/Actions empty by default
- [x] Metadata key-value pairs
- [x] Equality and hash code
- [x] Timestamps on creation
- [x] Complete data passes validation

### ⚠️ Category 2: Blueprint Structural Validation (MISSING)

**Priority:** P0 (MVD Blocker)
**Location:** Create `Sorcha.Blueprint.Models.Tests/BlueprintStructuralValidationTests.cs`

**Tests Needed:**

#### 2.1 Participant Count Validation
- [ ] Blueprint with 0 participants should fail validation
- [ ] Blueprint with 1 participant should fail validation
- [ ] Blueprint with 2 participants should pass validation
- [ ] Blueprint with 10 participants should pass validation

#### 2.2 Action Count Validation
- [ ] Blueprint with 0 actions should fail validation
- [ ] Blueprint with 1 action should pass validation
- [ ] Blueprint with multiple actions should pass validation

#### 2.3 Participant Reference Integrity
- [ ] Action sender must reference existing participant ID
- [ ] Action sender referencing non-existent participant should fail
- [ ] Action target must reference existing participant (if specified)
- [ ] Action additionalRecipients must reference existing participants
- [ ] All participant references validated across all actions

#### 2.4 Wallet Address Validation
- [ ] Participant with empty wallet address is allowed (assigned later)
- [ ] Participant with wallet address max length (100 chars) passes
- [ ] Participant with wallet address >100 chars fails
- [ ] Multiple participants can share same organization but need unique IDs

### ⚠️ Category 3: Blueprint Workflow Validation (MISSING)

**Priority:** P0 (MVD Blocker)
**Location:** Create `Sorcha.Blueprint.Engine.Tests/BlueprintWorkflowValidationTests.cs`

#### 3.1 Action Routing Validation
- [ ] Action condition must be valid JSON Logic
- [ ] Action condition that evaluates to non-existent action ID should fail
- [ ] Action routing must not create unreachable actions
- [ ] Action routing graph must be valid (no orphaned actions except terminal)

#### 3.2 Action Sequence Validation
- [ ] Action ID 0 must exist (starting action)
- [ ] Action IDs must be sequential integers starting from 0
- [ ] Duplicate action IDs should fail validation
- [ ] Negative action IDs should fail validation

#### 3.3 Graph Cycle Detection
- [ ] **CRITICAL:** Detect simple cycles (A → B → A)
- [ ] **CRITICAL:** Detect complex cycles (A → B → C → A)
- [ ] **CRITICAL:** Self-referencing actions should fail (A → A)
- [ ] Valid linear workflow (A → B → C) should pass
- [ ] Valid branching workflow (A → B or C → D) should pass
- [ ] Terminal actions (no next action) should be allowed

### ⚠️ Category 4: Data Schema Validation (PARTIAL)

**Priority:** P1 (Core MVD)
**Location:** Extend `Sorcha.Blueprint.Engine.Tests/SchemaValidatorTests.cs`

**Existing:**
- [x] Valid data against schema passes
- [x] Missing required fields fail
- [x] Wrong type fails
- [x] Nested object validation
- [x] Pattern validation

**Missing:**

#### 4.1 Blueprint DataSchemas
- [ ] Blueprint with embedded DataSchemas validates correctly
- [ ] DataSchemas can be referenced by actions
- [ ] Invalid JSON Schema in Blueprint.DataSchemas fails
- [ ] Empty DataSchemas array is allowed

#### 4.2 Action DataSchemas
- [ ] Action with valid DataSchemas passes validation
- [ ] Action DataSchemas must be valid JSON Schema Draft 2020-12
- [ ] Action data submitted must validate against Action.DataSchemas
- [ ] Multiple DataSchemas in single action validates correctly

#### 4.3 PreviousData Validation
- [ ] Action.PreviousData must conform to previous action's DataSchemas
- [ ] First action (ID 0) can have null PreviousData
- [ ] Non-first action with null PreviousData should be validated based on workflow
- [ ] PreviousData schema mismatch should fail validation

### ⚠️ Category 5: Disclosure Rules Validation (MISSING)

**Priority:** P1 (Core MVD)
**Location:** Create `Sorcha.Blueprint.Engine.Tests/DisclosureValidationTests.cs`

#### 5.1 Disclosure Requirement
- [ ] Action must have minimum 1 disclosure
- [ ] Action with 0 disclosures should fail validation
- [ ] Action with multiple disclosures should pass

#### 5.2 Disclosure Target Validation
- [ ] Disclosure recipients must reference existing participants
- [ ] Disclosure recipient referencing non-existent participant fails
- [ ] Disclosure with empty recipients list validation
- [ ] Disclosure with "all" participants is valid

#### 5.3 Disclosure Data Fields
- [ ] Disclosure dataPointers must reference valid action data fields
- [ ] Disclosure with non-existent dataPointer should fail
- [ ] Disclosure with JSONPath expressions validates correctly
- [ ] Empty disclosure dataPointers is allowed (disclose all)

### ⚠️ Category 6: JSON Logic Validation (MISSING)

**Priority:** P1 (Core MVD)
**Location:** Create `Sorcha.Blueprint.Engine.Tests/JsonLogicValidationTests.cs`

#### 6.1 Condition Validation
- [ ] Action condition must be valid JSON Logic syntax
- [ ] Action condition with invalid JSON Logic fails
- [ ] Default condition {\"==\":[0,0]} is valid
- [ ] Condition that always evaluates to same action works
- [ ] Condition using action data fields validates correctly

#### 6.2 Participant Routing Conditions
- [ ] Action.Participants conditions must be valid JSON Logic
- [ ] Participant routing with invalid condition fails
- [ ] Participant routing evaluates to valid participant ID
- [ ] Multiple participant routing conditions validated

#### 6.3 Calculations Validation
- [ ] Action.Calculations must be valid JSON Logic
- [ ] Calculation with invalid JSON Logic fails
- [ ] Calculation referencing non-existent data field fails
- [ ] Multiple calculations in single action validates correctly
- [ ] Calculation results can be used in subsequent actions

### ⚠️ Category 7: Form Validation (MISSING - P2)

**Priority:** P2 (Enhanced MVD)
**Location:** Create `Sorcha.Blueprint.Models.Tests/FormValidationTests.cs`

#### 7.1 Control Type Validation
- [ ] Valid ControlTypes (Layout, Input, etc.) pass validation
- [ ] Invalid ControlType fails validation
- [ ] Default VerticalLayout is valid

#### 7.2 Form-DataSchema Alignment
- [ ] Form fields must align with action DataSchemas
- [ ] Form field referencing non-existent schema property fails
- [ ] Form validation rules match schema validation rules

### ⚠️ Category 8: JSON-LD Validation (MISSING - P3)

**Priority:** P3 (Post-MVD)
**Location:** Extend `Sorcha.Blueprint.Fluent.Tests/JsonLd/BlueprintBuilderJsonLdTests.cs`

#### 8.1 Context Validation
- [ ] Blueprint @context is valid JSON-LD
- [ ] Participant @type (Person/Organization) validates
- [ ] Action @type (ActivityStreams Activity) validates
- [ ] VerifiableCredential format validates as W3C VC

### ⚠️ Category 9: Transaction Chain Validation (MISSING)

**Priority:** P0 (MVD Blocker)
**Location:** Create `Sorcha.Blueprint.Engine.Tests/TransactionChainValidationTests.cs`

**Context:** Transaction chains track blueprint execution instances via previousId references.
No separate blueprintExecutionInstanceId is needed - each chain branch represents a unique instance.

#### 9.1 previousId Reference Validation
- [ ] Transaction previousId must reference valid transaction
- [ ] Transaction with null previousId fails validation (except genesis block)
- [ ] Transaction with non-existent previousId fails validation
- [ ] Transaction previousId referencing transaction from different blueprint fails

#### 9.2 Chain Continuity Validation
- [ ] Action 0 transaction must reference Blueprint publication transaction
- [ ] Action N transaction must reference previous action in same instance
- [ ] Chain with broken continuity (skipped action) should fail
- [ ] Chain with correct sequence (0→1→2→3) should pass

#### 9.3 Chain Branching and Instance Isolation
- [ ] Multiple Action 0 transactions from same Blueprint create separate instances
- [ ] Each branch maintains independent chain (txid1→txid2→txid3 vs txid1→txid4)
- [ ] Chain merge detection (two chains converging) should fail
- [ ] Parallel instances from same Blueprint should be valid

#### 9.4 previousData Chain Validation
- [ ] Action N previousData must match Action N-1 current data
- [ ] previousData mismatch in chain should fail validation
- [ ] First action (Action 0) previousData validation rules
- [ ] previousData references resolved through chain walk

#### 9.5 Chain Integrity
- [ ] Chain with valid signatures at each step passes
- [ ] Chain with tampered previousId fails validation
- [ ] Chain timestamp sequence must be chronological
- [ ] Chain nonce values must be unique per participant

### ⚠️ Category 10: Multi-Participant Workflow Tests (MISSING)

**Priority:** P1 (Core MVD)
**Location:** Create `Sorcha.Blueprint.Engine.Tests/MultiParticipantWorkflowTests.cs`

#### 10.1 Simple Linear Workflow (2 participants)
- [ ] Blueprint: Participant A → Action → Participant B
- [ ] Action sender must match previous action target
- [ ] Workflow completes with all participants involved

#### 10.2 Branching Workflow (3+ participants)
- [ ] Blueprint: A → (B or C based on condition) → D
- [ ] Conditional routing selects correct participant
- [ ] All branches validated for participant references

#### 10.3 Round-Robin Workflow
- [ ] Blueprint: A → B → C → A (endorsement loop)
- [ ] Cycle detection allows controlled loops
- [ ] Maximum iteration limits prevent infinite loops

### ⚠️ Category 11: Blueprint Template Validation (PARTIAL)

**Priority:** P2 (Enhanced MVD)
**Location:** Extend `Sorcha.Blueprint.Engine.Tests/BlueprintTemplateServiceTests.cs`

**Existing:** Basic template service tests exist

**Missing:**
- [ ] Template parameter substitution validates correctly
- [ ] Template with missing required parameters fails
- [ ] Template generates valid blueprint instance
- [ ] Template validation catches errors before instantiation

---

## Implementation Priority

### Sprint 1: Critical Validations (P0)
**Focus:** Structural integrity and workflow validity

1. **BlueprintStructuralValidationTests.cs** (2 days)
   - Participant/action count validation
   - Participant reference integrity
   - Wallet address validation

2. **BlueprintWorkflowValidationTests.cs** (3 days)
   - Action routing validation
   - Action sequence validation
   - **Graph cycle detection** (CRITICAL)

3. **TransactionChainValidationTests.cs** (2.5 days) **NEW**
   - previousId reference validation
   - Chain continuity and branching
   - previousData chain validation
   - Chain integrity (signatures, timestamps, nonces)

### Sprint 2: Core Execution Validation (P1)
**Focus:** Data and disclosure validation

4. **DisclosureValidationTests.cs** (2 days)
   - Disclosure requirements
   - Target validation
   - Data field validation

5. **Extend SchemaValidatorTests.cs** (2 days)
   - Blueprint DataSchemas
   - Action DataSchemas
   - PreviousData validation

### Sprint 3: Logic Validation (P1)
**Focus:** JSON Logic correctness

6. **JsonLogicValidationTests.cs** (3 days)
   - Condition validation
   - Participant routing conditions
   - Calculations validation

7. **MultiParticipantWorkflowTests.cs** (2 days)
   - Linear workflows
   - Branching workflows
   - Round-robin patterns

### Sprint 4: Enhanced Validation (P2)
**Focus:** Forms and templates

8. **FormValidationTests.cs** (1 day)
   - Control type validation
   - Form-schema alignment

9. **Extend BlueprintTemplateServiceTests.cs** (2 days)
   - Template parameter validation
   - Template instantiation

### Sprint 5: Semantic Web (P3)
**Focus:** JSON-LD compliance

10. **Extend JsonLd tests** (1 day)
   - Context validation
   - Type validation
   - Verifiable Credentials

---

## Test File Structure

```
tests/
├── Sorcha.Blueprint.Models.Tests/
│   ├── BlueprintTests.cs ✅ (EXISTS)
│   ├── BlueprintStructuralValidationTests.cs ⚠️ (CREATE)
│   ├── FormValidationTests.cs ⚠️ (CREATE)
│   └── ParticipantTests.cs ✅ (EXISTS)
│
├── Sorcha.Blueprint.Engine.Tests/
│   ├── SchemaValidatorTests.cs ✅ (EXISTS - EXTEND)
│   ├── BlueprintWorkflowValidationTests.cs ⚠️ (CREATE)
│   ├── TransactionChainValidationTests.cs ⚠️ (CREATE - NEW)
│   ├── DisclosureValidationTests.cs ⚠️ (CREATE)
│   ├── JsonLogicValidationTests.cs ⚠️ (CREATE)
│   ├── MultiParticipantWorkflowTests.cs ⚠️ (CREATE)
│   └── BlueprintTemplateServiceTests.cs ✅ (EXISTS - EXTEND)
│
└── Sorcha.Blueprint.Fluent.Tests/
    └── JsonLd/
        └── BlueprintBuilderJsonLdTests.cs ✅ (EXISTS - EXTEND)
```

---

## Success Criteria

### Minimum Acceptance (MVD)
- ✅ All Category 1 tests pass (basic model validation)
- ⚠️ All Category 2 tests pass (structural validation)
- ⚠️ All Category 3 tests pass (workflow validation)
- ⚠️ All Category 5 tests pass (disclosure validation)
- ⚠️ All Category 9 tests pass (transaction chain validation) **NEW**

### Production Ready
- All P0 and P1 test categories implemented
- Test coverage >90% for Blueprint validation logic
- Integration tests validate full Blueprint → Action → Register flow

### Complete
- All 11 test categories fully implemented
- JSON-LD compliance verified
- Template system validated
- Transaction chain validation complete

---

## Related Tasks

- **BS-045**: Implement blueprint validation (participant references)
- **BS-046**: Implement graph cycle detection for blueprints
- **BP-3.5**: Unit tests for service layer
- **BP-7.1**: E2E tests for full workflow

---

## Notes

- **Authentication:** Not covered here - handled by Tenant Service with external identity
- **Cryptography:** Wallet addresses validated but signing tested separately in Wallet Service tests
- **Register Integration:** Transaction submission tested in integration tests, not blueprint validation
- **UI Validation:** Client-side validation tested separately in Designer.Client tests

---

**Next Steps:**
1. Review and approve this test plan
2. Start with Sprint 1 (Critical Validations)
3. Implement tests in order of priority
4. Update MASTER-TASKS.md as tests are completed
