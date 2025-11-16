# Sorcha Platform - Master Implementation Plan

**Version:** 3.0 - UNIFIED
**Last Updated:** 2025-11-16 (Post-Sprints 3-5 Update)
**Status:** Active - MVD Phase
**Supersedes:** plan.md, BLUEPRINT-SERVICE-IMPLEMENTATION-PLAN.md, WALLET-PROGRESS.md

---

## Executive Summary

This master plan consolidates all Sorcha platform development efforts into a single, unified roadmap. The plan is organized around delivering a **Minimum Viable Deliverable (MVD)** solution that provides end-to-end functionality for blueprint-based workflows with secure wallet management and distributed ledger capabilities.

**Current Overall Completion:** 80% (Updated from 70%)

**Recent Major Accomplishments:**
- ✅ Blueprint-Action Service Sprints 3, 4, 5 COMPLETE
- ✅ Wallet Service API Phase 2 COMPLETE (90% overall)
- ✅ Portable Execution Engine remains at 100%
- ✅ SignalR real-time notifications operational

**Strategic Focus:** Complete Register Service implementation and final MVD integration to enable end-to-end workflows.

---

## Table of Contents

1. [Project Vision & Goals](#project-vision--goals)
2. [Current Status](#current-status)
3. [Minimum Viable Deliverable (MVD)](#minimum-viable-deliverable-mvd)
4. [Implementation Phases](#implementation-phases)
5. [Timeline & Milestones](#timeline--milestones)
6. [Success Criteria](#success-criteria)
7. [Risk Assessment](#risk-assessment)

---

## Project Vision & Goals

### Vision
Create a production-grade distributed ledger platform that combines blockchain technology with enterprise-scale performance, security, and operational requirements through a microservices architecture.

### Strategic Goals

1. **MVP First:** Deliver functional end-to-end workflows before expanding features
2. **Quality Over Speed:** Maintain >85% test coverage and comprehensive documentation
3. **Cloud-Native:** Leverage .NET Aspire for modern, scalable deployments
4. **Security First:** Implement cryptographic best practices and secure key management
5. **Developer Experience:** Provide clear APIs, SDKs, and comprehensive documentation

---

## Current Status

### ✅ Completed Components (Production Ready)

#### Core Libraries (95% Complete)
- **Sorcha.Blueprint.Models** (100%) - Complete domain models with JSON-LD support
- **Sorcha.Blueprint.Fluent** (95%) - Fluent API for blueprint construction
- **Sorcha.Blueprint.Schemas** (95%) - Schema management with caching
- **Sorcha.Blueprint.Engine** (Portable, 100%) - Client/server execution engine with 102 tests
- **Sorcha.Cryptography** (90%) - ED25519, NIST P-256, RSA-4096 support
- **Sorcha.TransactionHandler** (68% core, pending integration) - Transaction building and serialization
- **Sorcha.ServiceDefaults** (100%) - .NET Aspire service configuration

#### Infrastructure (95% Complete)
- **Sorcha.AppHost** (100%) - .NET Aspire orchestration
- **Sorcha.ApiGateway** (95%) - YARP-based gateway with health aggregation
- **CI/CD Pipeline** (95%) - Advanced GitHub Actions with Azure deployment
- **Containerization** (95%) - Docker support for all services

### 🚧 In Progress Components

#### Services (60% Overall)
- **Sorcha.Blueprint.Service** (90%) - CRUD API functional, action endpoints pending
- **Sorcha.WalletService** (90%) - Core implementation complete, API pending
- **Sorcha.Peer.Service** (65%) - Discovery complete, transaction processing pending
- **Sorcha.Blueprint.Designer.Client** (85%) - Blazor WASM UI functional

### 📋 Planned Components

#### Services (Not Started)
- **Register Service** (Stub only) - Distributed ledger implementation
- **Tenant Service** (Stub only) - Multi-tenant management

---

## Minimum Viable Deliverable (MVD)

The MVD focuses on delivering a working end-to-end system that can:
1. Create and manage blueprints (workflow definitions)
2. Execute actions through the portable execution engine
3. Sign transactions with secure wallets
4. Store transactions on a distributed ledger
5. Provide a user interface for blueprint design and interaction

### MVD Scope

#### 🎯 MUST HAVE (Core MVD)

**1. Blueprint Execution Pipeline**
- ✅ Portable execution engine (COMPLETE)
- 🚧 Action submission API endpoints
- 🚧 Integration with Blueprint Service
- 🚧 SignalR real-time notifications
- 🚧 File handling for attachments

**2. Wallet Service**
- ✅ Core wallet management (90% complete)
- 🚧 Minimal API endpoints
- 🚧 Integration with .NET Aspire
- 🚧 Integration with Blueprint Service
- 🚧 Basic encryption provider (local AES-GCM)

**3. Register Service (Simplified MVD Version)**
- 📋 Transaction submission endpoint
- 📋 Transaction retrieval by ID
- 📋 Basic block storage (in-memory or MongoDB)
- 📋 Transaction history queries

**4. End-to-End Integration**
- 📋 Blueprint → Action → Sign → Register flow
- 📋 E2E tests covering full workflow
- 📋 Basic UI integration in Designer

#### ✨ SHOULD HAVE (Enhanced MVD)

**5. Enhanced Features**
- Database persistence for Blueprint Service (currently in-memory)
- EF Core repository for Wallet Service
- Azure Key Vault encryption provider
- Graph cycle detection in blueprints
- Performance optimizations

#### 💡 NICE TO HAVE (Post-MVD)

**6. Advanced Features**
- P2P transaction distribution
- Tenant Service with multi-tenancy
- Advanced Register Service with consensus
- AWS KMS encryption provider
- Complete backward compatibility (v1-v4)

### MVD Timeline: 12 Weeks

---

## Implementation Phases

### Phase 1: Complete Blueprint-Action Service (Weeks 1-6)
**Status:** ✅ **COMPLETE**
**Completion:** 100% (3 of 3 sprints complete)

#### Sprint 3: Service Layer Foundation ✅ COMPLETE
**Goal:** Build service layer components for action management

**Completed Tasks:**
- ✅ 3.1: Implement ActionResolverService
- ✅ 3.2: Implement PayloadResolverService with stub Wallet/Register
- ✅ 3.3: Implement TransactionBuilderService
- ✅ 3.4: Add caching layer (Redis integration)
- ✅ 3.5: Unit tests for service layer
- ✅ 3.6: Integration tests

**Delivered:**
- ✅ Action resolution from blueprints
- ✅ Payload encryption/decryption (integrated with Wallet Service)
- ✅ Transaction building orchestration
- ✅ Redis caching for blueprints and actions
- ✅ >85% test coverage achieved

#### Sprint 4: Action API Endpoints ✅ COMPLETE
**Goal:** Implement REST API endpoints for action operations

**Completed Tasks:**
- ✅ 4.1: GET /api/actions/{wallet}/{register}/blueprints
- ✅ 4.2: GET /api/actions/{wallet}/{register} (paginated)
- ✅ 4.3: GET /api/actions/{wallet}/{register}/{tx}
- ✅ 4.4: POST /api/actions (submit action)
- ✅ 4.5: POST /api/actions/reject
- ✅ 4.6: GET /api/files/{wallet}/{register}/{tx}/{fileId}
- ✅ 4.7: API integration tests
- ✅ 4.8: OpenAPI documentation

**Delivered:**
- ✅ Complete action management API
- ✅ File upload/download support
- ✅ API documentation with Scalar UI
- ✅ Integration tests passing

#### Sprint 5: Execution Helpers & SignalR ✅ COMPLETE
**Goal:** Add validation helpers and real-time notifications

**Completed Tasks:**
- ✅ 5.1: POST /api/execution/validate endpoint
- ✅ 5.2: POST /api/execution/calculate endpoint
- ✅ 5.3: POST /api/execution/route endpoint
- ✅ 5.4: POST /api/execution/disclose endpoint
- ✅ 5.5: Implement SignalR ActionsHub
- ✅ 5.6: Redis backplane for SignalR
- ✅ 5.7: SignalR integration tests
- ✅ 5.8: Client-side SignalR integration

**Delivered:**
- ✅ Execution helper endpoints for client-side validation
- ✅ Real-time notification hub operational
- ✅ Scalable SignalR with Redis backplane
- ✅ Integration tests passing

### Phase 2: Wallet Service API & Integration (Weeks 7-9)
**Status:** ✅ **MOSTLY COMPLETE** (90%)
**Completion:** 90% (API complete, deployment pending)

#### Week 7-8: Wallet Service API ✅ COMPLETE
**Goal:** Create REST API for wallet operations

**Completed Tasks:**
- ✅ WALLET-025: Setup Sorcha.WalletService.Api project
- ✅ WALLET-026: Implement minimal API endpoints (WS-030, WS-031)
  - ✅ POST /api/wallets (create wallet)
  - ✅ GET /api/wallets/{id} (get wallet)
  - ✅ POST /api/wallets/{id}/sign (sign transaction)
  - ✅ POST /api/wallets/{id}/decrypt (decrypt payload)
  - ✅ POST /api/wallets/{id}/addresses (generate address)
- 🚧 WALLET-027: .NET Aspire integration (partial)
- ✅ API tests - Comprehensive unit and integration tests

**Delivered:**
- ✅ Wallet REST API with OpenAPI docs
- ✅ Core implementation (90% complete)
- ✅ Comprehensive unit and integration tests
- ✅ HD wallet support (BIP32/BIP39/BIP44)
- ✅ Multi-algorithm support (ED25519, NIST P-256, RSA-4096)

**Pending (10%):**
- 🚧 Full .NET Aspire deployment
- 🚧 EF Core repository implementation
- 🚧 Azure Key Vault encryption provider
- 🚧 Production deployment

#### Week 9: Integration Testing ✅ MOSTLY COMPLETE
**Goal:** Integrate Wallet Service with Blueprint Service

**Completed Tasks:**
- ✅ Blueprint Service integrated with Wallet Service
- ✅ Encryption/decryption integration complete
- ✅ End-to-end integration tests passing
- 🚧 Performance testing (partial)

**Delivered:**
- ✅ Blueprint Service calling Wallet Service for crypto operations
- ✅ E2E tests for Blueprint → Wallet integration
- ✅ Integration working in development environment

**Pending:**
- 🚧 Production performance benchmarks
- 🚧 Load testing at scale

### Phase 3: Register Service (MVD Version) (Weeks 10-12)
**Status:** Not Started
**Completion:** 0%

#### Week 10-11: Core Register Service
**Goal:** Build simplified register service for MVD

**Tasks:**
- REG-001: Setup Register Service project (6h)
- REG-002: Implement transaction storage (MongoDB or in-memory) (12h)
- REG-003: POST /api/register/transactions (submit) (8h)
- REG-004: GET /api/register/transactions/{id} (retrieve) (6h)
- REG-005: GET /api/register/transactions/history/{wallet} (8h)
- REG-006: Basic block creation (simplified, no consensus) (10h)
- REG-007: .NET Aspire integration (8h)
- Unit and integration tests (12h)

**Deliverables:**
- Functional Register Service API
- Transaction storage and retrieval
- Basic block management
- Integration with .NET Aspire

#### Week 12: Full Integration & E2E Testing
**Goal:** Complete end-to-end workflow

**Tasks:**
- Integrate Register Service with Blueprint Service (8h)
- Update transaction submission flow (6h)
- Complete E2E test suite (16h)
- Performance testing (8h)
- Security testing (8h)
- Documentation updates (8h)

**Deliverables:**
- End-to-end workflow functional: Blueprint → Action → Sign → Register
- Complete E2E test coverage
- Performance and security validation
- Updated documentation

---

## Timeline & Milestones

### Overall Timeline: 12 Weeks (3 Months)

```
Week 1-2:   Sprint 3 - Service Layer Foundation
Week 3-4:   Sprint 4 - Action API Endpoints
Week 5-6:   Sprint 5 - Execution Helpers & SignalR
Week 7-8:   Wallet Service API
Week 9:     Wallet Integration & Testing
Week 10-11: Register Service (MVD)
Week 12:    Full Integration & E2E Testing
```

### Key Milestones

| Milestone | Week | Deliverable | Success Criteria |
|-----------|------|-------------|------------------|
| **M1: Blueprint Service Complete** | 6 | Unified Blueprint-Action Service | All API endpoints functional, >85% test coverage |
| **M2: Wallet Service Live** | 8 | Wallet Service API | REST API functional, integrated with Aspire |
| **M3: Wallet Integration** | 9 | Wallet ↔ Blueprint Integration | E2E encryption/signing working |
| **M4: Register Service MVD** | 11 | Basic Register Service | Transaction storage and retrieval working |
| **M5: MVD Complete** | 12 | Full E2E Workflow | Blueprint → Action → Sign → Register flow functional |

---

## Success Criteria

### Technical Metrics

**Code Quality:**
- ✅ Test coverage >85% for all core libraries
- ✅ Zero critical security vulnerabilities
- ✅ Build success rate >95%
- 🎯 API response time <200ms (p95) for GET operations
- 🎯 API response time <500ms (p95) for POST operations

**Functionality:**
- 🎯 Complete blueprint lifecycle (create, publish, execute)
- 🎯 Secure wallet operations (create, sign, encrypt/decrypt)
- 🎯 Transaction submission and retrieval
- 🎯 Real-time notifications via SignalR
- 🎯 File upload/download support

**Integration:**
- 🎯 All services integrated via .NET Aspire
- 🎯 API Gateway routing to all services
- 🎯 Health checks and monitoring functional
- 🎯 E2E tests covering complete workflows

### Business Metrics

**Developer Experience:**
- 🎯 Complete API documentation (OpenAPI/Scalar)
- 🎯 Integration guides for all services
- 🎯 Sample applications and code examples
- 🎯 Clear troubleshooting documentation

**Operational Metrics:**
- 🎯 Successful Docker Compose deployment
- 🎯 Azure deployment via Bicep templates
- 🎯 CI/CD pipeline with automated testing
- 🎯 Monitoring and logging functional

---

## Risk Assessment

### High Priority Risks

| Risk | Impact | Probability | Mitigation | Owner |
|------|--------|-------------|------------|-------|
| **Register Service complexity underestimated** | High | Medium | Use simplified MVD version, defer consensus | Architecture |
| **Wallet-Blueprint integration issues** | High | Medium | Stub interfaces early, comprehensive integration tests | Dev Team |
| **Performance not meeting SLAs** | High | Low | Regular performance testing, optimize as needed | Dev Team |
| **Security vulnerabilities in encryption** | Critical | Low | Security audit, use proven libraries (Sorcha.Cryptography) | Security |

### Medium Priority Risks

| Risk | Impact | Probability | Mitigation | Owner |
|------|--------|-------------|------------|-------|
| **SignalR scaling challenges** | Medium | Medium | Use Redis backplane, load testing | DevOps |
| **Test coverage insufficient** | Medium | Low | TDD approach, coverage enforcement | QA |
| **Documentation incomplete** | Medium | Medium | Document as we build, review gates | Tech Writer |

### Low Priority Risks

| Risk | Impact | Probability | Mitigation | Owner |
|------|--------|-------------|------------|-------|
| **P2P service delays** | Low | Low | Not critical for MVD, defer if needed | Architecture |
| **Tenant service delays** | Low | Low | Use simple tenant provider for MVD | Architecture |

---

## Post-MVD Roadmap

### Phase 4: Enhanced Features (Weeks 13-18)
**Focus:** Performance, scalability, and advanced features

**Deliverables:**
- Database persistence for Blueprint Service
- EF Core repository for Wallet Service
- Azure Key Vault integration
- P2P transaction distribution
- Advanced caching strategies
- Performance optimization

### Phase 5: Enterprise Features (Weeks 19-24)
**Focus:** Multi-tenancy, compliance, and production hardening

**Deliverables:**
- Full Tenant Service implementation
- Advanced Register Service with consensus
- Audit logging and compliance reporting
- Backup and disaster recovery
- Load balancing and auto-scaling
- Production monitoring and alerting

### Phase 6: Platform Expansion (Weeks 25+)
**Focus:** Developer ecosystem and platform growth

**Deliverables:**
- SDK for external developers
- Additional encryption providers (AWS KMS, GCP KMS)
- Advanced blueprint features (versioning, templates)
- Marketplace for blueprints
- Community documentation and examples

---

## Dependencies

### External Dependencies
- .NET 10 SDK (10.0.100+)
- Redis (for caching and SignalR backplane)
- MongoDB (for Register Service)
- PostgreSQL (for Wallet Service, optional for Blueprint Service)
- Azure services (optional for Key Vault, Container Apps)

### Internal Dependencies
- Sorcha.Cryptography v2.0+ (complete)
- Sorcha.TransactionHandler v1.0+ (complete)
- Sorcha.Blueprint.Engine v1.0+ (complete)
- .NET Aspire orchestration

---

## Review & Updates

**Review Frequency:** Bi-weekly during active development
**Next Review:** Week 3 (after Sprint 3 completion)
**Document Owner:** Sorcha Architecture Team

**Change Log:**
- 2025-11-16: Version 3.0 - Unified master plan created
- Supersedes: plan.md v2.0, BLUEPRINT-SERVICE-IMPLEMENTATION-PLAN.md v2.1

---

**Related Documents:**
- [Master Task List](MASTER-TASKS.md) - Detailed task breakdown
- [Project Specification](spec.md) - Requirements and architecture
- [Project Constitution](constitution.md) - Principles and standards
- [Development Status](../docs/development-status.md) - Current completion status
- [Architecture Documentation](../docs/architecture.md) - System architecture

---

**Status Legend:**
- ✅ Complete
- 🚧 In Progress
- 📋 Planned
- 🎯 Target/Goal
