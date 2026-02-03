# Sorcha Platform - Master Implementation Plan

**Version:** 4.0 - VALIDATOR SERVICE UPDATE
**Last Updated:** 2026-02-03 (Validator Service & Transaction Storage Complete)
**Status:** Active - MVD Phase 98% Complete, Production Hardening In Progress
**Supersedes:** plan.md, BLUEPRINT-SERVICE-IMPLEMENTATION-PLAN.md, WALLET-PROGRESS.md, MASTER-PLAN v3.1
**Related:** [TASK-AUDIT-REPORT.md](.specify/TASK-AUDIT-REPORT.md), [MASTER-TASKS.md](.specify/MASTER-TASKS.md)

---

## Executive Summary

This master plan consolidates all Sorcha platform development efforts into a single, unified roadmap. The plan is organized around delivering a **Minimum Viable Deliverable (MVD)** solution that provides end-to-end functionality for blueprint-based workflows with secure wallet management, distributed ledger capabilities, and blockchain consensus validation.

**Current Overall Completion:**
- **MVD Core Functionality:** 98% ✅ (Updated 2026-02-03 - Validator Service operational, transaction storage complete)
- **Production Readiness:** 35% 🔧 (Significant progress in JWT authentication, Redis persistence, service-to-service auth)

**Recent Major Accomplishments (2026-02-03 Update):**

**NEW - Validator Service (Sprint 9F/10):**
- ✅ **Validator Service OPERATIONAL (95%)** - Complete blockchain validation and consensus
  - System wallet auto-initialization with `ISystemWalletProvider` pattern
  - Redis-backed memory pool (`IMemPoolManager`) for transaction persistence
  - Redis-backed register monitoring (`IRegisterMonitoringRegistry`) for docket build tracking
  - Genesis docket creation with Merkle tree computation and signing
  - Full transaction document storage integration with Register Service
  - Periodic docket build triggers (`DocketBuildTriggerService`)
  - JWT Bearer authentication for service-to-service communication
  - End-to-end flow: Register creation → Genesis transaction → Docket building → Transaction storage

**NEW - AI-Assisted Blueprint Design:**
- ✅ **Blueprint Chat Feature COMPLETE (100%)** - Interactive AI-assisted blueprint design
  - SignalR-based `ChatHub` with JWT authentication
  - Anthropic Claude AI provider integration with streaming responses
  - Real-time chat interface in UI for blueprint creation assistance
  - YARP routes configured for SignalR negotiation

**NEW - UI & Authentication:**
- ✅ **UI Authentication Improvements COMPLETE (100%)**
  - Automatic token refresh handling with refresh token support
  - Improved login flow with error handling and validation
  - Secure token storage using browser local storage
- ✅ **UI Consolidation COMPLETE (100%)** - Single unified Sorcha.UI application
- ✅ **UI Register Management COMPLETE (100%)** - Wallet selection wizard, transaction query
- ✅ **CLI Register Commands COMPLETE (100%)** - Two-phase creation, dockets, queries

**Previous Accomplishments:**
- ✅ Blueprint-Action Service Sprints 3-7 COMPLETE (96% - 54/56 tasks)
- ✅ Blueprint-Action Service SignalR integration tests COMPLETE (16 tests)
- ✅ Wallet Service API Phase 2 COMPLETE (100%)
- ✅ Portable Execution Engine remains at 100%
- ✅ SignalR real-time notifications operational (ActionsHub + RegisterHub + ChatHub)
- ✅ **Register Service COMPLETE (100% - all 15/15 tasks)**
- ✅ End-to-end integration COMPLETE (Blueprint → Wallet → Register → Validator flow verified)
- ✅ Comprehensive testing COMPLETE (1,113+ tests across 103+ test files)
- ✅ TransactionHandler regression testing COMPLETE (94 tests)

**⚠️ CRITICAL GAPS IDENTIFIED (Updated Status):**
1. ✅ **RESOLVED:** Production authentication/authorization - JWT Bearer implemented across services
2. ⚠️ **PARTIAL:** Database persistence - Redis persistence for validator, MongoDB for register (needs completion)
3. ⚠️ Security hardening INCOMPLETE - HTTPS partial, rate limiting pending, input validation partial
4. 📋 Deployment documentation MISSING - Docker compose functional, production deployment pending

**Updated Strategic Focus:**
1. **P0 - IMMEDIATE:** Implement JWT authentication and RBAC authorization (AUTH-001, AUTH-002)
2. **P0 - IMMEDIATE:** Security hardening (SEC-001, HTTPS enforcement)
3. **P1 - SHORT-TERM:** Database persistence (Wallet EF Core, Register MongoDB, Blueprint EF Core)
4. **P1 - SHORT-TERM:** Azure Key Vault integration for production secrets
5. **P1 - DEFERRED:** Resolve Register Service code duplication (REG-CODE-DUP)

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
- **Sorcha.Cryptography** (95%) - ED25519, NIST P-256, RSA-4096 support, HD wallet operations
- **Sorcha.TransactionHandler** (68% core, pending integration) - Transaction building and serialization
- **Sorcha.ServiceDefaults** (100%) - .NET Aspire service configuration

#### Infrastructure (95% Complete)
- **Sorcha.AppHost** (100%) - .NET Aspire orchestration
- **Sorcha.ApiGateway** (95%) - YARP-based gateway with health aggregation, SignalR routing
- **CI/CD Pipeline** (95%) - Advanced GitHub Actions with Azure deployment
- **Containerization** (95%) - Docker support for all services

#### Services (85% Overall - Updated 2026-02-03)
- **Sorcha.Blueprint.Service** (100%) - Complete with SignalR ActionsHub and ChatHub, AI-assisted design
- **Sorcha.Wallet.Service** (95%) - Core API complete, HD wallets operational, EF Core pending
- **Sorcha.Register.Service** (100%) - Complete distributed ledger with MongoDB, OData, SignalR
- **Sorcha.Validator.Service** (95%) - Operational consensus engine, memory pool, genesis docket creation
- **Sorcha.Tenant.Service** (90%) - JWT authentication, multi-tenant management, participant identity API
- **Sorcha.Peer.Service** (70%) - P2P discovery operational, full replication pending

### 🚧 In Progress Components

#### Services
- **Sorcha.Peer.Service** (70%) - P2P discovery complete, transaction replication pending
- **Sorcha.Wallet.Service** (95%) - EF Core repository pending for production persistence

### 📋 Planned Components

#### Post-MVD Enhancements
- Complete database persistence for all services (Blueprint, Wallet EF Core)
- Azure Key Vault integration for production secrets
- Full P2P transaction replication and consensus
- Advanced analytics and reporting dashboard

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
- ✅ Core wallet management (95% complete)
- ✅ Minimal API endpoints (14/15 operational)
- ✅ Integration with .NET Aspire
- ✅ Integration with Blueprint Service
- ✅ Multi-algorithm support (ED25519, P-256, RSA-4096)
- 🚧 EF Core repository for production persistence

**3. Register Service**
- ✅ Transaction submission endpoint (COMPLETE)
- ✅ Transaction retrieval by ID (COMPLETE)
- ✅ MongoDB block storage with full transaction documents (COMPLETE)
- ✅ Transaction history queries with OData (COMPLETE)
- ✅ SignalR real-time notifications (RegisterHub) (COMPLETE)
- ✅ Docket (block) management and chain validation (COMPLETE)

**4. Validator Service**
- ✅ System wallet initialization (`ISystemWalletProvider`) (COMPLETE)
- ✅ Memory pool for pending transactions (Redis-backed) (COMPLETE)
- ✅ Register monitoring for docket build triggers (COMPLETE)
- ✅ Genesis docket creation with Merkle tree computation (COMPLETE)
- ✅ Full transaction document storage integration (COMPLETE)
- ✅ JWT authentication for service-to-service calls (COMPLETE)
- ✅ Periodic docket building (`DocketBuildTriggerService`) (COMPLETE)

**5. End-to-End Integration**
- ✅ Blueprint → Action → Sign → Register flow (COMPLETE)
- ✅ Register creation → Genesis transaction → Validator → Docket (COMPLETE)
- ✅ E2E tests covering full workflow (COMPLETE - 27 tests)
- ✅ UI integration with register management (COMPLETE)
- ✅ CLI integration with register commands (COMPLETE)

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
**Status:** ✅ **COMPLETE** (Post-Audit)
**Completion:** 96% (54/56 tasks - Sprints 1-7 complete)

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

#### Sprint 5: Execution Helpers & SignalR ✅ SERVER COMPLETE (88%)
**Goal:** Add validation helpers and real-time notifications

**Completed Tasks:**
- ✅ 5.1: POST /api/execution/validate endpoint
- ✅ 5.2: POST /api/execution/calculate endpoint
- ✅ 5.3: POST /api/execution/route endpoint
- ✅ 5.4: POST /api/execution/disclose endpoint
- ✅ 5.5: Implement SignalR ActionsHub
- ✅ 5.6: Redis backplane for SignalR
- ✅ 5.7: SignalR integration tests (COMPLETE - audit found 16 tests in SignalRIntegrationTests.cs)
- ❌ 5.8: Client-side SignalR integration (NOT STARTED - no Blazor client code found)

**Delivered:**
- ✅ Execution helper endpoints for client-side validation
- ✅ Real-time notification hub operational (ActionsHub + RegisterHub)
- ✅ Scalable SignalR with Redis backplane
- ✅ Comprehensive SignalR integration tests (16 tests covering subscription, notifications, multi-client)

**Deferred to P3:**
- ❌ Client-side SignalR integration (BP-5.8) - Not required for MVD server-side functionality

### Phase 2: Wallet Service API & Integration (Weeks 7-9)
**Status:** ✅ **COMPLETE** (Post-Audit)
**Completion:** 100% (32/32 tasks - API and integration complete)

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
- ✅ WALLET-027: .NET Aspire integration (COMPLETE)
- ✅ API tests - Comprehensive unit and integration tests (WS-030, WS-031)

**Delivered:**
- ✅ Wallet REST API with OpenAPI docs (14/15 endpoints)
- ✅ Core implementation (90% complete)
- ✅ Comprehensive unit and integration tests (60+ tests)
- ✅ HD wallet support (BIP32/BIP39/BIP44)
- ✅ Multi-algorithm support (ED25519, NIST P-256, RSA-4096)
- ✅ .NET Aspire integration with health checks
- ✅ API Gateway routing configured

**Pending (10%):**
- 🚧 EF Core repository implementation (PostgreSQL/SQL Server)
- 🚧 Azure Key Vault encryption provider
- 🚧 Production authentication/authorization
- 🚧 GenerateAddress endpoint (requires mnemonic storage design)

#### Week 9: Integration Testing ✅ COMPLETE
**Goal:** Integrate Wallet Service with Blueprint Service

**Completed Tasks:**
- ✅ Blueprint Service integrated with Wallet Service (WalletServiceClient implemented)
- ✅ Encryption/decryption integration complete (PayloadResolverService updated)
- ✅ End-to-end integration tests complete (27 E2E tests found)
- ✅ Performance testing complete (NBomber tests with 1000+ req/s scenarios)

**Delivered:**
- ✅ Blueprint Service calling Wallet Service for crypto operations
- ✅ E2E tests for Blueprint → Wallet integration (13+ tests in WalletRegisterIntegrationTests)
- ✅ Integration working in development environment
- ✅ Performance benchmarks established (load testing with ramp-up/ramp-down)

**Audit Finding:** WS-INT-1 through WS-INT-4 completed under Sprint 6 & 7 task IDs (BP-6.x, BP-7.x)

### Phase 3: Register Service (MVD Version) (Weeks 10-12)
**Status:** ✅ **COMPLETE**
**Completion:** 100% (15/15 tasks - Core, API, integration, transaction storage, and comprehensive testing complete)

#### ✅ Completed: Phase 1-2 Core Implementation (100%)
**What exists:**
- ✅ Complete domain models (Register, TransactionModel, Docket, PayloadModel)
- ✅ RegisterManager - CRUD operations for registers (204 lines)
- ✅ TransactionManager - Transaction storage/retrieval (225 lines)
- ✅ DocketManager - Block creation and sealing (255 lines)
- ✅ QueryManager - Advanced queries with pagination (233 lines)
- ✅ ChainValidator - Chain integrity validation (268 lines)
- ✅ IRegisterRepository abstraction (214 lines, 20+ methods)
- ✅ InMemoryRegisterRepository implementation (265 lines)
- ✅ Event system (IEventPublisher, RegisterEvents)
- ✅ ~3,500 lines of production code

#### ✅ Completed: Phase 5 API Layer (100%)
**Status:** API fully integrated with Phase 1-2 core

**Achievements:**
- ✅ REG-INT-1: API fully integrated with core managers (RegisterManager, TransactionManager, QueryManager)
- ✅ REG-003-007: 20 REST endpoints operational
- ✅ REG-008: .NET Aspire integration complete
- ✅ REG-009: Comprehensive unit and integration tests (112 tests, ~2,459 LOC)
- ✅ SignalR real-time notifications with RegisterHub
- ✅ OData V4 support for flexible queries
- ✅ OpenAPI/Swagger documentation with Scalar UI

**Deliverables Complete:**
- ✅ API service fully integrated with Phase 1-2 core
- ✅ Comprehensive automated testing (112 test methods)
- ✅ .NET Aspire integration operational
- ⚠️ Code duplication issue remains (DocketManager/ChainValidator in Validator.Service)

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

### Phase 4: Validator Service & Blockchain Consensus (Sprint 9F/10)
**Status:** ✅ **COMPLETE**
**Completion:** 95% (Core validation operational, production hardening pending)
**Timeline:** Completed 2026-02-03

#### Sprint 9F: System Wallet & Memory Pool ✅ COMPLETE
**Goal:** Establish foundation for transaction validation and docket building

**Completed Tasks:**
- ✅ System wallet provider pattern implementation (`ISystemWalletProvider`)
- ✅ Auto-initialization of validator signing wallet on startup
- ✅ Memory pool manager with Redis persistence (`IMemPoolManager`)
- ✅ Transaction priority queuing and TTL management
- ✅ Cross-restart persistence for pending transactions
- ✅ Integration with Wallet Service for cryptographic operations

**Delivered:**
- ✅ System wallet initialized and operational for docket signing
- ✅ Memory pool survives service restarts via Redis backing
- ✅ Priority-based transaction ordering
- ✅ Automatic expiration of stale transactions

#### Sprint 10: Genesis Docket & Transaction Storage ✅ COMPLETE
**Goal:** Complete genesis docket creation and integrate transaction storage

**Completed Tasks:**
- ✅ Genesis docket creation (`GenesisManager`) with Merkle tree computation
- ✅ Register monitoring registry with Redis persistence (`IRegisterMonitoringRegistry`)
- ✅ Docket build trigger service for periodic docket creation
- ✅ Full transaction document storage in Register Service
- ✅ Transaction-to-TransactionModel mapping in docket building
- ✅ JWT Bearer authentication configuration for validator-to-register calls
- ✅ End-to-end flow: Register creation → Genesis tx → Memory pool → Docket → Storage

**Delivered:**
- ✅ Genesis dockets created and signed for new registers
- ✅ Full transaction documents stored (not just IDs) in MongoDB
- ✅ Merkle root computation and docket hash signing operational
- ✅ Register monitoring persists across restarts
- ✅ Periodic docket building triggers operational
- ✅ Complete architectural fix for transaction storage

**Key Architectural Achievements:**
- **Redis Persistence:** Both memory pool and register monitoring survive container restarts
- **System Wallet Pattern:** Singleton provider ensures consistent validator identity across services
- **Transaction Storage:** Full document storage enables complete transaction history and queries
- **Service Integration:** Validator ↔ Register ↔ Wallet communication fully authenticated via JWT

**Pending (5%):**
- 🚧 Production consensus algorithm (currently simplified for MVD)
- 🚧 Advanced docket validation rules
- 🚧 Performance optimization for high-throughput scenarios

---

## Timeline & Milestones

### Overall Timeline: Extended MVD (Updated 2026-02-03)

```
Week 1-2:   Sprint 3 - Service Layer Foundation ✅
Week 3-4:   Sprint 4 - Action API Endpoints ✅
Week 5-6:   Sprint 5 - Execution Helpers & SignalR ✅
Week 7-8:   Wallet Service API ✅
Week 9:     Wallet Integration & Testing ✅
Week 10-11: Register Service (MVD) ✅
Week 12:    Full Integration & E2E Testing ✅
Week 13-14: Sprint 9F/10 - Validator Service ✅
Week 15:    AI-Assisted Blueprint Design ✅
```

### Key Milestones

| Milestone | Week | Deliverable | Success Criteria | Status |
|-----------|------|-------------|------------------|--------|
| **M1: Blueprint Service Complete** | 6 | Unified Blueprint-Action Service | All API endpoints functional, >85% test coverage | ✅ COMPLETE |
| **M2: Wallet Service Live** | 8 | Wallet Service API | REST API functional, integrated with Aspire | ✅ COMPLETE |
| **M3: Wallet Integration** | 9 | Wallet ↔ Blueprint Integration | E2E encryption/signing working | ✅ COMPLETE |
| **M4: Register Service MVD** | 11 | Complete Register Service | Transaction storage and retrieval working | ✅ COMPLETE |
| **M5: MVD E2E Complete** | 12 | Full E2E Workflow | Blueprint → Action → Sign → Register flow functional | ✅ COMPLETE |
| **M6: Validator Service Live** | 14 | Blockchain Consensus | Genesis dockets, memory pool, transaction storage | ✅ COMPLETE |
| **M7: AI-Assisted Design** | 15 | Blueprint Chat Feature | Claude AI integration with streaming | ✅ COMPLETE |

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

### Phase 5: Production Hardening (Next Priority)
**Status:** 🚧 IN PROGRESS (35% Complete)
**Focus:** Security, persistence, and operational readiness

**Deliverables:**
- ✅ JWT Bearer authentication (COMPLETE)
- 🚧 Database persistence for Blueprint Service (EF Core PostgreSQL)
- 🚧 EF Core repository for Wallet Service (PostgreSQL)
- 🚧 Azure Key Vault integration for production secrets
- 🚧 HTTPS enforcement across all services
- 🚧 Rate limiting and input validation
- 🚧 Comprehensive security audit
- 🚧 Production deployment documentation
- 📋 Backup and disaster recovery procedures

**Estimated Timeline:** 4-6 weeks

### Phase 6: Enterprise Features (Future)
**Focus:** Multi-tenancy enhancements, compliance, and advanced consensus

**Deliverables:**
- Enhanced Tenant Service with RBAC
- Advanced Validator Service with production consensus algorithm
- P2P transaction distribution and replication
- Audit logging and compliance reporting
- Load balancing and auto-scaling strategies
- Production monitoring and alerting dashboards
- Performance optimization for high-throughput scenarios

**Estimated Timeline:** 6-8 weeks

### Phase 7: Platform Expansion (Future)
**Focus:** Developer ecosystem and platform growth

**Deliverables:**
- SDK for external developers
- Additional encryption providers (AWS KMS, GCP KMS)
- Advanced blueprint features (versioning, templates, branching)
- Marketplace for blueprints and reusable components
- Community documentation and tutorials
- Developer portal with interactive examples

**Estimated Timeline:** 8-12 weeks

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
- 2026-02-03: Version 4.0 - Validator Service completion, transaction storage architectural fix, AI-assisted blueprint design
  - Added Phase 4 (Validator Service) documentation with Sprint 9F/10 details
  - Updated service completion percentages (Register 100%, Validator 95%, Blueprint 100%)
  - Added AI-assisted blueprint chat feature documentation
  - Updated MVD completion to 98%, Production Readiness to 35%
  - Added 7 new milestones (M6: Validator Service, M7: AI-Assisted Design)
  - Documented Redis persistence for memory pool and register monitoring
  - Documented system wallet provider pattern and JWT authentication
- 2025-11-18: Version 3.1 - Post-audit updates with comprehensive test findings
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
