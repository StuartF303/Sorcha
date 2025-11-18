# SORCHA Specification Kit

This directory contains the specification-driven development artifacts for the SORCHA project using the [Spec-Kit](https://github.com/github/spec-kit) methodology from GitHub.

**Last Updated:** 2025-11-17 (AI Code Documentation Policy added)

---

## 📋 Quick Start - Essential Documents

**For Active Development:**
1. ⭐ **[MASTER-PLAN.md](MASTER-PLAN.md)** - Unified implementation plan (START HERE)
2. ⭐ **[MASTER-TASKS.md](MASTER-TASKS.md)** - Consolidated task list with priorities
3. 📜 **[constitution.md](constitution.md)** - Project principles and standards
4. 📋 **[spec.md](spec.md)** - Requirements and architecture
5. 🤖 **[AI-CODE-DOCUMENTATION-POLICY.md](AI-CODE-DOCUMENTATION-POLICY.md)** - AI code documentation requirements (NEW)

---

## Overview

Spec-Kit is a specification-first development approach where specifications become executable and directly guide implementation. This directory organizes project intent, architecture, and requirements in a structured, version-controlled manner.

**Recent Update (2025-11-16):** Consolidated multiple planning documents into a unified master plan structure for clarity and MVD focus.

---

## Directory Structure

```
.specify/
├── README.md                           # This file - overview
├── MASTER-PLAN.md                      # ⭐ Unified implementation plan (NEW)
├── MASTER-TASKS.md                     # ⭐ Consolidated task list (NEW)
├── constitution.md                     # Project principles and standards
├── spec.md                            # Requirements, goals, and scenarios
├── plan.md                            # ⚠️  SUPERSEDED (see MASTER-PLAN.md)
├── UNIFIED-DESIGN-SUMMARY.md          # Blueprint service unified design
├── BLUEPRINT-SERVICE-UNIFIED-DESIGN.md # Detailed design specification
├── specs/                             # Service specifications
│   ├── sorcha-wallet-service.md
│   ├── sorcha-register-service.md
│   ├── sorcha-tenant-service.md
│   ├── sorcha-cryptography-rewrite.md
│   └── sorcha-transaction-handler.md
├── tasks/                             # Detailed task files (reference)
│   ├── TASK-OVERVIEW.md              # Cryptography tasks
│   ├── TX-OVERVIEW.md                # TransactionHandler tasks
│   ├── WALLET-OVERVIEW.md            # Wallet service tasks
│   └── [Individual task files...]
└── archive/                           # Historical documents
    └── pre-unified-plan-20251116/    # Archived planning documents
```

---

## Core Documents

### ⭐ Primary Planning Documents (Active)

#### MASTER-PLAN.md (NEW - 2025-11-16)
**Purpose:** Unified implementation plan consolidating all previous plans

**Contains:**
- Overall project strategy and phases
- MVD (Minimum Viable Deliverable) definition with 12-week timeline
- Three implementation phases (Blueprint-Action, Wallet, Register)
- Timeline, milestones, and success criteria
- Risk assessment and dependencies
- Post-MVD roadmap

**Replaces:** plan.md v2.0, BLUEPRINT-SERVICE-IMPLEMENTATION-PLAN.md, WALLET-PROGRESS.md, IMPLEMENTATION-SUMMARY.md

**When to update:**
- Phase completion
- Milestone achievements
- Major scope changes
- Risk materialization

**Review frequency:** Bi-weekly during active development
**Status:** ✅ Active, authoritative

---

#### MASTER-TASKS.md (NEW - 2025-11-16)
**Purpose:** Consolidated task list with priorities and status tracking

**Contains:**
- 138 tasks across 4 phases
- P0 (MVD Blocker) → P3 (Post-MVD) prioritization
- Task status, effort estimates, and dependencies
- Sprint breakdowns for Blueprint-Action service
- Wallet Service API tasks
- Register Service MVD tasks
- Post-MVD enhancement tasks

**Replaces:** Task tracking scattered across multiple documents

**When to update:**
- Task completion
- Status changes
- New tasks discovered
- Priority adjustments

**Review frequency:** Weekly during active development
**Status:** ✅ Active, authoritative

---

### 📜 Foundational Documents (Spec-Kit Core)

#### constitution.md
**Purpose:** Establishes non-negotiable principles and standards for the project

**Contains:**
- Core architectural principles (microservices, cloud-native, scalability)
- Security guidelines (zero trust, cryptographic standards)
- Development standards (code quality, .NET 10, testing)
- API documentation requirements (.NET 10 OpenAPI, Scalar UI)
- AI-generated code documentation requirements (NEW v1.2)
- Infrastructure as code principles
- Data management and observability standards

**When to update:**
- Major policy changes
- New architectural principles
- Security requirement updates
- Development standard changes

**Review frequency:** Quarterly
**Version:** 1.2 (Updated: 2025-11-17)
**Status:** ✅ Active

---

#### AI-CODE-DOCUMENTATION-POLICY.md 🤖 NEW
**Purpose:** Mandatory documentation requirements for AI-generated code

**Contains:**
- Required documentation updates for AI-generated code
- Documentation update workflow
- AI-specific requirements for coding assistants
- Examples and enforcement policies
- Quality standards and metrics

**When to apply:**
- Using AI coding assistants (Claude Code, Copilot, ChatGPT, etc.)
- Automated code generation tools
- Code scaffolding and templates
- ANY code not written entirely by humans

**Key Requirements:**
- README files must be updated
- Documentation in docs/ must be updated
- MASTER-TASKS.md must be updated
- Status files must be updated
- OpenAPI/XML documentation must be complete

**Review frequency:** Quarterly
**Version:** 1.0
**Status:** ✅ Active, Mandatory

---

#### spec.md
**Purpose:** Captures project goals, requirements, and user needs

**Contains:**
- Executive summary and vision
- Strategic goals (enterprise readiness, developer experience, security, performance, operational excellence)
- System architecture overview
- Core service descriptions (Wallet, Register, Blueprint, Peer, etc.)
- User scenarios and workflows
- Functional requirements (FR-1 through FR-6)
- Non-functional requirements (NFR-1 through NFR-7)
- Technical constraints
- Success metrics

**When to update:**
- New feature requests
- Requirement changes
- Architecture modifications
- New user scenarios

**Review frequency:** Monthly or per major release
**Version:** 1.0
**Status:** ✅ Active

---

#### plan.md ⚠️ SUPERSEDED
**Status:** SUPERSEDED by MASTER-PLAN.md as of 2025-11-16

**Original Purpose:** Described technical approach, architecture, and implementation strategy

**Why Superseded:**
- Multiple overlapping plans caused confusion
- Unclear prioritization across initiatives
- Difficult to track critical path for MVD
- Needed consolidated view

**Replacement:** [MASTER-PLAN.md](MASTER-PLAN.md)

**Historical Version:** Preserved in [archive/pre-unified-plan-20251116/plan.md](archive/pre-unified-plan-20251116/plan.md)

---

### 📐 Design Documents (Active)

#### UNIFIED-DESIGN-SUMMARY.md
**Purpose:** Blueprint-Action service unified design summary

**Contains:**
- Portable execution engine overview (client/server)
- Key architectural changes
- Unified service approach
- Technology stack updates
- Implementation plan summary
- Success criteria

**When to update:**
- Design changes
- Architecture decisions
- Implementation progress

**Status:** ✅ Active, approved 2025-11-15

---

#### BLUEPRINT-SERVICE-UNIFIED-DESIGN.md
**Purpose:** Detailed technical specification for Blueprint-Action service

**Contains:**
- Complete technical design
- 138-task implementation plan (8 sprints)
- Interface definitions
- Component architecture
- Integration patterns
- Testing strategy

**When to update:**
- Implementation discoveries
- Technical decisions
- Pattern refinements

**Status:** ✅ Active, in implementation (Sprint 3 of 8)

---

### 📝 Service Specifications

Located in **[specs/](specs/)** directory:

**Active Specifications:**
- **[sorcha-wallet-service.md](specs/sorcha-wallet-service.md)** - Complete wallet service specification
  - HD wallet support (BIP32/BIP39/BIP44)
  - Multi-algorithm cryptography (ED25519, SECP256K1, RSA)
  - Encrypted key storage
  - Transaction signing/verification
  - .NET Aspire integration

- **[sorcha-cryptography-rewrite.md](specs/sorcha-cryptography-rewrite.md)** - Cryptography library specification
  - Core cryptographic operations
  - Key management
  - Hash providers
  - Wallet utilities

- **[sorcha-transaction-handler.md](specs/sorcha-transaction-handler.md)** - Transaction handler specification
  - Transaction building and signing
  - Multi-recipient payload management
  - Serialization formats (binary, JSON)
  - Version compatibility (v1-v4)

**Stub Specifications (MVD Simplified):**
- **[sorcha-register-service.md](specs/sorcha-register-service.md)** - Register service (simplified for MVD)
- **[sorcha-tenant-service.md](specs/sorcha-tenant-service.md)** - Tenant service (simple provider for MVD)
- **[sorcha-action-service.md](specs/sorcha-action-service.md)** - Action service (merged into Blueprint service)

---

### 📋 Task Breakdowns

Located in **[tasks/](tasks/)** directory:

**Note:** Individual task files are now consolidated into **[MASTER-TASKS.md](MASTER-TASKS.md)** for easier tracking. The task directory is maintained for historical reference and detailed technical specifications.

**Reference Task Overviews:**
- **[TASK-OVERVIEW.md](tasks/TASK-OVERVIEW.md)** - Cryptography library tasks (25 tasks, reference)
- **[TX-OVERVIEW.md](tasks/TX-OVERVIEW.md)** - TransactionHandler tasks (19 tasks, reference)
- **[WALLET-OVERVIEW.md](tasks/WALLET-OVERVIEW.md)** - Wallet service tasks (32 tasks, reference)

**Individual Task Files:**
- Detailed specifications for specific implementation tasks
- Historical reference
- Technical deep-dives

---

## 🎯 Document Status Legend

- ✅ **Active** - Current, authoritative, use for development
- ⭐ **Primary** - Most important, start here
- ⚠️ **Superseded** - Replaced by newer document, see redirects
- 📦 **Archived** - Historical reference only, see archive/
- 🔄 **Under Review** - Being updated
- 📐 **Design** - Technical design specification

---

## Using Spec-Kit with AI Agents

This specification structure is designed to work with AI coding assistants like Claude Code, GitHub Copilot, and others. AI agents can reference these documents to:

### 🤖 AI Code Documentation Requirements

**IMPORTANT:** All AI-generated code MUST follow the [AI Code Documentation Policy](AI-CODE-DOCUMENTATION-POLICY.md).

**Before generating code:**
1. Identify which task you're working on in MASTER-TASKS.md
2. Review relevant specifications and design documents
3. Plan which documentation will need updates

**After generating code:**
1. Update MASTER-TASKS.md with task status
2. Update README files if APIs/features changed
3. Update docs/ files if architecture changed
4. Update status files with progress
5. Ensure OpenAPI/XML documentation is complete

See [AI-CODE-DOCUMENTATION-POLICY.md](AI-CODE-DOCUMENTATION-POLICY.md) for complete requirements.

---

### 1. Understand Project Context

**New Unified Approach:**
- **[MASTER-PLAN.md](MASTER-PLAN.md)** - Current implementation phase and priorities
- **[MASTER-TASKS.md](MASTER-TASKS.md)** - Specific tasks to work on
- **[constitution.md](constitution.md)** - Guardrails and principles
- **[spec.md](spec.md)** - What needs to be built
- **[AI-CODE-DOCUMENTATION-POLICY.md](AI-CODE-DOCUMENTATION-POLICY.md)** - Documentation requirements (MANDATORY)
- Design documents - How it's architected

### 2. Make Informed Decisions

- **MVD Focus:** Check task priority (P0, P1, P2, P3) before starting work
- **Architecture Choices:** Guided by design documents and spec.md
- **Feature Development:** Aligned with spec.md requirements
- **Code Style:** Consistent with constitution.md standards

### 3. Maintain Consistency

- All changes evaluated against constitutional principles
- Implementation follows established patterns from design docs
- Documentation stays synchronized across all documents
- Progress tracked in MASTER-TASKS.md

---

## Workflow

### Starting New Features

1. **Check MASTER-PLAN.md**
   - Confirm feature is in current phase
   - Understand MVD priority
   - Review dependencies

2. **Find Task in MASTER-TASKS.md**
   - Identify task ID and priority
   - Check prerequisites and dependencies
   - Estimate effort

3. **Consult constitution.md**
   - Ensure feature aligns with project principles
   - Check for constraints or requirements
   - Review coding standards

4. **Review spec.md**
   - Understand related requirements
   - Check for existing user scenarios
   - Identify affected functional areas

5. **Reference Design Documents**
   - Understand relevant architecture patterns
   - Identify affected services and components
   - Review technical constraints

6. **Implement**
   - Follow established patterns
   - Maintain test coverage requirements (>85%)
   - Document significant decisions
   - Update MASTER-TASKS.md status

### Making Architecture Changes

1. **Propose changes in design documents**
   - Document rationale
   - Explain alternatives considered
   - Impact analysis

2. **Verify constitutional compliance**
   - Check against architectural principles
   - Ensure security requirements met
   - Validate against quality standards

3. **Update related specifications**
   - Update spec.md if requirements change
   - Update design docs with new patterns
   - Update MASTER-PLAN.md if phase changes
   - Update MASTER-TASKS.md with new tasks

4. **Review and approve**
   - Technical review by team
   - Architecture review for major changes
   - Update version numbers

### Tracking Progress

1. **Daily/Weekly Updates**
   - Update task status in MASTER-TASKS.md
   - Mark completed tasks ✅
   - Flag blockers
   - Update effort estimates

2. **Sprint Reviews**
   - Review sprint completion in MASTER-PLAN.md
   - Update phase progress
   - Assess milestone achievement
   - Adjust priorities if needed

3. **Phase Completion**
   - Update MASTER-PLAN.md with completion date
   - Document lessons learned
   - Plan next phase
   - Archive superseded documents

---

## Version Control

All specification documents are version-controlled with the codebase:

- **Major version:** Breaking changes to principles or architecture
- **Minor version:** New features or requirements
- **Patch version:** Clarifications and corrections

Version numbers tracked in document headers.

---

## Document Maintenance

### Review Schedule

| Document | Review Frequency | Owner |
|----------|-----------------|--------|
| constitution.md | Quarterly | Architecture Team |
| spec.md | Monthly or per release | Product + Engineering |
| MASTER-PLAN.md | Bi-weekly during development | Engineering Team |
| MASTER-TASKS.md | Weekly during development | Development Team |
| Design documents | Per sprint | Engineering Team |
| Service specs | As needed | Service owners |

### Update Process

1. **Identify need for update**
   - Architecture change
   - New requirement
   - Process improvement
   - Clarification needed

2. **Draft changes**
   - Create feature branch
   - Update relevant documents
   - Maintain consistency across docs

3. **Review**
   - Technical review
   - Architecture review (if needed)
   - Stakeholder approval (if needed)

4. **Commit and merge**
   - Clear commit message
   - Reference related issues/PRs
   - Update version numbers

---

## Best Practices

### Writing Specifications

1. **Be Specific**
   - Use concrete examples
   - Avoid vague language
   - Define measurable criteria

2. **Stay Consistent**
   - Use consistent terminology
   - Reference other documents
   - Maintain cross-references

3. **Keep Current**
   - Update when changes occur
   - Remove obsolete information
   - Archive old decisions with context

4. **Make Testable**
   - Requirements should be verifiable
   - Include acceptance criteria
   - Define success metrics

### Using with AI

1. **Provide Context**
   - Start with MASTER-PLAN.md for current phase
   - Point to specific tasks in MASTER-TASKS.md
   - Reference relevant sections in spec.md
   - Cite constitutional principles

2. **Validate Output**
   - Check against specifications
   - Verify constitutional compliance
   - Test against requirements
   - Validate MVD priorities

3. **Update Documentation**
   - Document new patterns in design docs
   - Update task status in MASTER-TASKS.md
   - Keep specifications current
   - Archive superseded versions

---

## Getting Started

### For New Team Members

**Read in order:**
1. **README.md** (this file) - Understand the spec-kit structure
2. **[MASTER-PLAN.md](MASTER-PLAN.md)** - Current project status and strategy
3. **[constitution.md](constitution.md)** - Principles and standards
4. **[spec.md](spec.md)** - Requirements and architecture
5. **[MASTER-TASKS.md](MASTER-TASKS.md)** - Available tasks

**Understand:**
- Project goals and vision (spec.md)
- MVD definition and priorities (MASTER-PLAN.md)
- Architectural principles (constitution.md)
- Development standards (constitution.md)
- Testing requirements (constitution.md)

**Reference while coding:**
- Check MASTER-TASKS.md for your current task
- Verify MASTER-PLAN.md for context
- Follow constitution.md for standards
- Consult spec.md for requirements
- Review design docs for patterns

### For AI Agents

When working on SORCHA:

**Always reference:**
1. **[AI-CODE-DOCUMENTATION-POLICY.md](AI-CODE-DOCUMENTATION-POLICY.md)** - ⚠️ MANDATORY documentation requirements
2. **[MASTER-PLAN.md](MASTER-PLAN.md)** - Current phase and MVD priorities
3. **[MASTER-TASKS.md](MASTER-TASKS.md)** - Specific task details and status
4. **[constitution.md](constitution.md)** - Principles and standards
5. **[spec.md](spec.md)** - Requirements and features
6. **Design documents** - Architecture and patterns

**Validate decisions against:**
- MVD priorities (P0 > P1 > P2 > P3)
- Constitutional principles
- Functional requirements
- Non-functional requirements
- Architecture patterns

**Maintain consistency with:**
- Established code patterns
- Documentation standards (MUST update docs when generating code!)
- Testing requirements (>85% coverage)
- Security guidelines
- API documentation requirements (.NET 10 OpenAPI, Scalar UI)

**Documentation Requirements (MANDATORY):**
When you generate code, you MUST also:
- Update MASTER-TASKS.md task status
- Update README files if features/APIs changed
- Update docs/ files if architecture changed
- Update status files with completion progress
- Ensure OpenAPI/XML documentation is complete
- See [AI-CODE-DOCUMENTATION-POLICY.md](AI-CODE-DOCUMENTATION-POLICY.md) for details

---

## Support and Questions

- **Specification Issues:** Create issue with label `specification`
- **Architecture Questions:** Tag architecture team in PR
- **Clarifications:** Update relevant document with clear explanation
- **Conflicts:** Escalate through technical lead

---

## References

- [Spec-Kit GitHub Repository](https://github.com/github/spec-kit)
- [Spec-Kit Documentation](https://github.github.com/spec-kit/)
- [SORCHA Project README](../README.md)
- [MASTER-PLAN.md](MASTER-PLAN.md) - Unified implementation plan
- [MASTER-TASKS.md](MASTER-TASKS.md) - Consolidated task list

---

**Document Control**
- **Created:** 2025-11-11
- **Last Major Update:** 2025-11-17 (AI Code Documentation Policy added)
- **Owner:** SORCHA Architecture Team
- **Status:** Active
- **Next Review:** 2025-12-17
