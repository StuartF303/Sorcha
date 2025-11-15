# Sorcha Action Service - Implementation Plan

**Version:** 1.0
**Date:** 2025-11-15
**Status:** Proposed
**Related Specification:** [sorcha-action-service.md](specs/sorcha-action-service.md)

## Overview

This document outlines the phased implementation plan for the Sorcha Action Service. The implementation is divided into 6 phases over approximately 12-14 weeks, with each phase delivering incrementally valuable functionality.

## Implementation Phases

### Phase 1: Foundation & Project Setup (Week 1)
**Goal:** Establish project structure, dependencies, and basic service skeleton

**Tasks:**
- ACT-001: Create project structure and solution integration
- ACT-002: Configure dependencies and NuGet packages
- ACT-003: Implement service defaults and health checks
- ACT-004: Setup OpenAPI and Scalar documentation
- ACT-005: Configure logging and observability
- ACT-006: Setup test projects (unit + integration)

**Deliverables:**
- ✅ Sorcha.Action.Service project created
- ✅ Test projects configured
- ✅ Health endpoints functional (/health, /alive)
- ✅ OpenAPI documentation accessible at /scalar/v1
- ✅ Aspire integration configured
- ✅ Build pipeline passing

**Dependencies:** None (foundation work)

---

### Phase 2: Core Models & Validation (Week 2)
**Goal:** Implement request/response models and validation logic

**Tasks:**
- ACT-007: Implement DTOs (ActionSubmission, ActionResponse, etc.)
- ACT-008: Implement FluentValidation validators
- ACT-009: Implement custom exceptions
- ACT-010: Add JSON Schema validation integration
- ACT-011: Write unit tests for models and validators
- ACT-012: Document models with XML comments

**Deliverables:**
- ✅ All DTOs implemented and documented
- ✅ Validation logic complete with >90% coverage
- ✅ JSON Schema validation functional
- ✅ Custom exceptions defined
- ✅ Unit tests passing

**Dependencies:** Phase 1 complete

---

### Phase 3: Action Resolution & Retrieval (Weeks 3-4)
**Goal:** Implement action retrieval endpoints and business logic

**Tasks:**
- ACT-013: Implement IActionResolver interface and service
- ACT-014: Implement IActionService interface
- ACT-015: Implement Blueprint caching with Redis
- ACT-016: Implement GET /api/actions/{wallet}/{register}/blueprints endpoint
- ACT-017: Implement GET /api/actions/{wallet}/{register} endpoint
- ACT-018: Implement GET /api/actions/{wallet}/{register}/{tx} endpoint
- ACT-019: Implement data aggregation logic
- ACT-020: Write unit tests for ActionResolver
- ACT-021: Write unit tests for ActionService
- ACT-022: Write integration tests for action retrieval endpoints
- ACT-023: Implement pagination and filtering

**Deliverables:**
- ✅ Action retrieval endpoints functional
- ✅ Blueprint resolution and caching working
- ✅ Data aggregation from previous transactions
- ✅ Pagination and filtering implemented
- ✅ Integration with BlueprintService and RegisterService
- ✅ >80% test coverage
- ✅ OpenAPI documentation complete

**Dependencies:**
- Phase 2 complete
- BlueprintService available
- RegisterService available

---

### Phase 4: Payload Management & Encryption (Week 5)
**Goal:** Implement payload encryption, selective disclosure, and decryption

**Tasks:**
- ACT-024: Implement IPayloadResolver interface
- ACT-025: Implement payload encryption logic
- ACT-026: Implement selective disclosure (JSON Pointer filtering)
- ACT-027: Implement payload decryption logic
- ACT-028: Integrate with WalletService for encryption keys
- ACT-029: Implement tracking data separation
- ACT-030: Write unit tests for PayloadResolver
- ACT-031: Write integration tests for encryption/decryption
- ACT-032: Test multi-recipient payload encryption

**Deliverables:**
- ✅ Payload encryption/decryption functional
- ✅ Selective disclosure working correctly
- ✅ Integration with WalletService complete
- ✅ Tracking data handled separately
- ✅ >80% test coverage
- ✅ Security review passed

**Dependencies:**
- Phase 2 complete
- WalletService available
- Sorcha.Cryptography library complete

---

### Phase 5: Transaction Construction & Submission (Weeks 6-7)
**Goal:** Implement action submission, transaction building, and integration

**Tasks:**
- ACT-033: Implement ITransactionRequestBuilder interface
- ACT-034: Implement new instance transaction building
- ACT-035: Implement continuation transaction building
- ACT-036: Implement rejection transaction building
- ACT-037: Implement POST /api/actions endpoint
- ACT-038: Implement POST /api/actions/reject endpoint
- ACT-039: Integrate with RegisterService for submission
- ACT-040: Implement retry logic with exponential backoff
- ACT-041: Implement ICalculationService interface
- ACT-042: Implement JSON Logic evaluation
- ACT-043: Implement routing condition evaluation
- ACT-044: Write unit tests for TransactionRequestBuilder
- ACT-045: Write unit tests for CalculationService
- ACT-046: Write integration tests for action submission
- ACT-047: Write integration tests for action rejection
- ACT-048: Test end-to-end workflow (retrieve → submit → confirm)

**Deliverables:**
- ✅ Action submission endpoint functional
- ✅ Rejection endpoint functional
- ✅ Transaction construction working
- ✅ JSON Logic calculations implemented
- ✅ Routing conditions evaluated
- ✅ Integration with RegisterService complete
- ✅ End-to-end workflow tested
- ✅ >80% test coverage

**Dependencies:**
- Phase 3 complete
- Phase 4 complete
- RegisterService available

---

### Phase 6: File Management (Week 8)
**Goal:** Implement file attachment support

**Tasks:**
- ACT-049: Implement file validation logic
- ACT-050: Implement file transaction building
- ACT-051: Implement file upload handling in POST /api/actions
- ACT-052: Implement GET /api/files/{wallet}/{register}/{tx}/{fileId} endpoint
- ACT-053: Implement file streaming for large files
- ACT-054: Write unit tests for file validation
- ACT-055: Write integration tests for file upload
- ACT-056: Write integration tests for file download
- ACT-057: Test file size limits and type restrictions
- ACT-058: Test multiple files per action

**Deliverables:**
- ✅ File upload support in action submission
- ✅ File download endpoint functional
- ✅ File validation working
- ✅ Streaming for large files
- ✅ >80% test coverage
- ✅ Security review passed

**Dependencies:**
- Phase 5 complete
- Transaction storage supports file payloads

---

### Phase 7: Real-Time Notifications (Weeks 9-10)
**Goal:** Implement SignalR hub and real-time notification system

**Tasks:**
- ACT-059: Implement ActionsHub SignalR hub
- ACT-060: Implement JWT authentication for SignalR
- ACT-061: Implement connection group management
- ACT-062: Implement notification broadcasting logic
- ACT-063: Implement POST /api/actions/notify internal endpoint
- ACT-064: Integrate with RegisterService event stream
- ACT-065: Implement Redis backplane for SignalR scale-out
- ACT-066: Implement reconnection handling
- ACT-067: Write unit tests for hub logic
- ACT-068: Write integration tests for SignalR connections
- ACT-069: Test notification delivery
- ACT-070: Test scale-out with multiple instances
- ACT-071: Load test with 10,000 concurrent connections

**Deliverables:**
- ✅ SignalR hub functional
- ✅ Real-time notifications working
- ✅ JWT authentication enforced
- ✅ Connection groups managed correctly
- ✅ Redis backplane configured
- ✅ Scale-out tested
- ✅ Performance targets met

**Dependencies:**
- Phase 5 complete
- Redis available
- RegisterService event stream available

---

### Phase 8: Security Hardening & Performance (Week 11)
**Goal:** Implement security measures and optimize performance

**Tasks:**
- ACT-072: Implement rate limiting
- ACT-073: Implement request size limits
- ACT-074: Implement wallet address validation
- ACT-075: Implement authorization checks for all endpoints
- ACT-076: Implement audit logging
- ACT-077: Security penetration testing
- ACT-078: Optimize caching strategies
- ACT-079: Optimize database queries
- ACT-080: Implement connection pooling
- ACT-081: Performance testing (load, stress, spike)
- ACT-082: Identify and fix performance bottlenecks
- ACT-083: Review and optimize memory usage

**Deliverables:**
- ✅ Rate limiting implemented
- ✅ Authorization enforced on all endpoints
- ✅ Audit logging complete
- ✅ Security vulnerabilities addressed
- ✅ Performance targets met (< 200ms p95 for GET, < 500ms for POST)
- ✅ Load testing passed (1000 req/s)
- ✅ Memory leaks fixed

**Dependencies:**
- All previous phases complete

---

### Phase 9: Integration & End-to-End Testing (Week 12)
**Goal:** Complete system integration and comprehensive testing

**Tasks:**
- ACT-084: Integration testing with WalletService
- ACT-085: Integration testing with RegisterService
- ACT-086: Integration testing with BlueprintService
- ACT-087: End-to-end workflow testing (complete Blueprint execution)
- ACT-088: Multi-participant workflow testing
- ACT-089: File attachment workflow testing
- ACT-090: Rejection workflow testing
- ACT-091: Error handling and recovery testing
- ACT-092: Chaos engineering tests (service failures)
- ACT-093: Data consistency validation
- ACT-094: Cross-browser testing (SignalR)

**Deliverables:**
- ✅ All integration tests passing
- ✅ End-to-end workflows validated
- ✅ Error scenarios handled gracefully
- ✅ Service resilience tested
- ✅ Data consistency verified
- ✅ Browser compatibility confirmed

**Dependencies:**
- All previous phases complete
- All dependent services available

---

### Phase 10: Documentation & Deployment (Weeks 13-14)
**Goal:** Complete documentation and prepare for production deployment

**Tasks:**
- ACT-095: Complete XML documentation for all public APIs
- ACT-096: Write README.md for service
- ACT-097: Write deployment guide
- ACT-098: Write troubleshooting guide
- ACT-099: Create API usage examples
- ACT-100: Create architecture diagrams
- ACT-101: Write migration guide (if applicable)
- ACT-102: Configure Docker image
- ACT-103: Configure Kubernetes manifests
- ACT-104: Setup CI/CD pipeline
- ACT-105: Configure monitoring and alerting
- ACT-106: Perform security audit
- ACT-107: Code review and cleanup
- ACT-108: Performance baseline documentation
- ACT-109: Production readiness review
- ACT-110: Deploy to staging environment
- ACT-111: Staging validation
- ACT-112: Production deployment
- ACT-113: Post-deployment validation
- ACT-114: Handoff to operations team

**Deliverables:**
- ✅ Complete documentation
- ✅ Deployment automation
- ✅ Monitoring configured
- ✅ Production deployment successful
- ✅ Service operational

**Dependencies:**
- All previous phases complete

---

## Task Breakdown by Category

### Project Setup (6 tasks)
- ACT-001 through ACT-006

### Models & Validation (6 tasks)
- ACT-007 through ACT-012

### Action Retrieval (11 tasks)
- ACT-013 through ACT-023

### Payload Management (9 tasks)
- ACT-024 through ACT-032

### Transaction Building (16 tasks)
- ACT-033 through ACT-048

### File Management (10 tasks)
- ACT-049 through ACT-058

### Real-Time Notifications (13 tasks)
- ACT-059 through ACT-071

### Security & Performance (12 tasks)
- ACT-072 through ACT-083

### Integration Testing (11 tasks)
- ACT-084 through ACT-094

### Documentation & Deployment (20 tasks)
- ACT-095 through ACT-114

**Total: 114 tasks**

---

## Timeline Summary

| Phase | Duration | Tasks | Key Deliverable |
|-------|----------|-------|-----------------|
| 1. Foundation | Week 1 | 6 | Project structure and health checks |
| 2. Models | Week 2 | 6 | DTOs and validation |
| 3. Action Retrieval | Weeks 3-4 | 11 | GET endpoints functional |
| 4. Payload Management | Week 5 | 9 | Encryption and selective disclosure |
| 5. Transaction Building | Weeks 6-7 | 16 | POST endpoints functional |
| 6. File Management | Week 8 | 10 | File upload/download |
| 7. Real-Time | Weeks 9-10 | 13 | SignalR notifications |
| 8. Security | Week 11 | 12 | Production-ready security |
| 9. Integration | Week 12 | 11 | End-to-end testing |
| 10. Deployment | Weeks 13-14 | 20 | Production deployment |

**Total Duration:** 14 weeks (3.5 months)

---

## Resource Requirements

### Development Team
- 1 Senior Backend Developer (full-time)
- 1 Mid-level Backend Developer (full-time)
- 1 QA Engineer (weeks 8-14)
- 1 DevOps Engineer (weeks 13-14)
- 1 Security Engineer (review in weeks 11-12)

### Infrastructure
- Development environment (Aspire local)
- Staging environment (Kubernetes cluster)
- Redis instance (development + staging)
- Test databases (PostgreSQL/MongoDB)
- CI/CD pipeline (GitHub Actions)

### External Dependencies
- Sorcha.WalletService (by Week 4)
- Sorcha.RegisterService (by Week 3)
- Sorcha.Blueprint.Service (already available)
- Sorcha.Cryptography (already available)
- Sorcha.TransactionHandler (already available)

---

## Risk Management

### High Risks

1. **WalletService Availability**
   - **Risk:** WalletService not ready by Week 4
   - **Mitigation:** Use mock WalletService for development, parallel track WalletService development
   - **Impact:** Phase 4 delayed

2. **RegisterService Availability**
   - **Risk:** RegisterService not ready by Week 3
   - **Mitigation:** Use in-memory repository for testing, parallel track RegisterService development
   - **Impact:** Phase 3 integration delayed

3. **Performance Requirements**
   - **Risk:** Cannot meet response time targets (< 200ms)
   - **Mitigation:** Early performance testing, caching strategy, horizontal scaling
   - **Impact:** Phase 8 extended

### Medium Risks

1. **JSON Logic Complexity**
   - **Risk:** JSON Logic evaluation more complex than anticipated
   - **Mitigation:** Use existing JsonLogic.Net library, comprehensive testing
   - **Impact:** Phase 5 extended by 1-2 days

2. **SignalR Scale-Out**
   - **Risk:** SignalR doesn't scale to 10,000 connections
   - **Mitigation:** Redis backplane, load testing, Azure SignalR Service as fallback
   - **Impact:** Phase 7 extended, potential architecture change

3. **File Storage Size**
   - **Risk:** Transaction payloads too large for file storage
   - **Mitigation:** Monitor transaction sizes, prepare blob storage migration plan
   - **Impact:** Future phase needed for blob storage

### Low Risks

1. **API Documentation**
   - **Risk:** OpenAPI documentation incomplete
   - **Mitigation:** Document as you code, automated validation
   - **Impact:** Phase 10 extended by 1-2 days

2. **Test Coverage**
   - **Risk:** Don't reach 80% coverage target
   - **Mitigation:** Write tests alongside features, coverage gates in CI
   - **Impact:** Phase completion delayed until coverage met

---

## Success Criteria

### Phase Completion Criteria

Each phase is considered complete when:
- ✅ All tasks completed
- ✅ All tests passing (unit + integration)
- ✅ Code coverage target met (>80%)
- ✅ Code reviewed and approved
- ✅ OpenAPI documentation updated
- ✅ No critical bugs
- ✅ Performance targets met (if applicable)

### Overall Project Success

The Action Service project is successful when:
1. ✅ All 114 tasks completed
2. ✅ All REST endpoints functional with OpenAPI docs
3. ✅ SignalR hub operational
4. ✅ >80% unit test coverage
5. ✅ All integration tests passing
6. ✅ Performance targets met:
   - GET endpoints: < 200ms (p95)
   - POST endpoints: < 500ms (p95)
   - 1000 requests/second throughput
   - 10,000 concurrent SignalR connections
7. ✅ Security review passed
8. ✅ Production deployment successful
9. ✅ Zero critical bugs in first 2 weeks of production
10. ✅ Documentation complete and accessible

---

## Milestones

### M1: Foundation Complete (End of Week 1)
- Project structure established
- Health checks working
- OpenAPI documentation accessible
- Aspire integration configured

### M2: Read-Only Operations (End of Week 4)
- All GET endpoints functional
- Blueprint resolution working
- Data aggregation implemented
- Integration with BlueprintService and RegisterService

### M3: Write Operations (End of Week 7)
- Action submission working
- Transaction construction complete
- Calculations and conditions evaluated
- Integration with RegisterService

### M4: Complete Feature Set (End of Week 10)
- File management functional
- Real-time notifications working
- All core features implemented

### M5: Production Ready (End of Week 12)
- Security hardening complete
- Performance optimized
- Integration testing complete

### M6: Deployed (End of Week 14)
- Documentation complete
- Production deployment successful
- Operations handoff complete

---

## Dependencies Graph

```
Phase 1 (Foundation)
    ↓
Phase 2 (Models) ────────────┐
    ↓                        │
Phase 3 (Retrieval) ────┐    │
    ↓                   │    │
Phase 4 (Payloads) ─────┤    │
    ↓                   │    │
Phase 5 (Transactions) ─┴────┤
    ↓                        │
Phase 6 (Files) ─────────────┤
    ↓                        │
Phase 7 (SignalR) ───────────┤
    ↓                        │
Phase 8 (Security) ──────────┤
    ↓                        │
Phase 9 (Integration) ───────┘
    ↓
Phase 10 (Deployment)
```

---

## Progress Tracking

Progress will be tracked using:
1. **GitHub Projects** - Kanban board with all tasks
2. **GitHub Issues** - One issue per task
3. **GitHub Milestones** - One milestone per phase
4. **Weekly Status Reports** - Progress, blockers, next steps
5. **Code Coverage Dashboard** - Track test coverage trends
6. **Performance Dashboard** - Track response times and throughput

---

## Review & Approval

| Role | Name | Approval | Date |
|------|------|----------|------|
| Technical Lead | TBD | ☐ | |
| Product Owner | TBD | ☐ | |
| Security Lead | TBD | ☐ | |
| DevOps Lead | TBD | ☐ | |

---

## References

1. [Action Service Specification](specs/sorcha-action-service.md)
2. [Sorcha Architecture](../docs/architecture.md)
3. [Sorcha Constitution](.specify/constitution.md)
4. [Wallet Service Specification](specs/sorcha-wallet-service.md)
5. [Register Service Specification](specs/sorcha-register-service.md)

---

**Document Control**
- **Created:** 2025-11-15
- **Author:** Sorcha Architecture Team
- **Review Frequency:** Weekly during implementation
- **Next Review:** 2025-11-22
