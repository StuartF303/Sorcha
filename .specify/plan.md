# Sorcha Implementation Plan

**Version:** 2.0
**Last Updated:** 2025-11-13
**Status:** Active

## Overview

This document outlines the implementation plan for the Sorcha distributed ledger platform, including the newly specified Wallet Service and its integration with the existing architecture.

## Project Phases

### Phase 1: Foundation & Core Libraries (Completed)
**Status:** ✅ Complete
**Duration:** Completed

**Deliverables:**
- ✅ Sorcha.Cryptography - Cryptographic library with ED25519, SECP256K1, RSA support
- ✅ Sorcha.TransactionHandler - Transaction building and payload management
- ✅ Sorcha.Blueprint.Models - Domain models for blueprints
- ✅ Sorcha.ServiceDefaults - Shared service configurations with .NET Aspire
- ✅ .NET 10 migration complete
- ✅ .NET Aspire integration for orchestration

### Phase 2: Blueprint Services (In Progress)
**Status:** 🔄 In Progress
**Duration:** Weeks 1-8

**Completed:**
- ✅ Sorcha.Blueprint.Engine - Blueprint execution engine
- ✅ Sorcha.Blueprint.Fluent - Fluent API builders
- ✅ Sorcha.Blueprint.Schemas - Schema management with JSON-LD
- ✅ Sorcha.AppHost - .NET Aspire orchestration host
- ✅ Sorcha.ApiGateway - YARP-based API gateway

**In Progress:**
- 🔄 Sorcha.Blueprint.Service - REST API for blueprint management
- 🔄 Sorcha.Blueprint.Designer.Client - Blazor WASM designer UI
- 🔄 End-to-end testing
- 🔄 Performance optimization

**Next Steps:**
- Complete Blueprint Service API endpoints
- Finish Blueprint Designer UI components
- Performance testing and optimization
- Documentation and examples

### Phase 3: Wallet Service (Planned - Starting Week 9)
**Status:** 📋 Planned
**Duration:** Weeks 9-23 (15 weeks)
**Related Specification:** [sorcha-wallet-service.md](.specify/specs/sorcha-wallet-service.md)
**Task Breakdown:** [WALLET-OVERVIEW.md](.specify/tasks/WALLET-OVERVIEW.md)

#### Phase 3.1: Foundation (Weeks 9-10)
**Focus:** Project setup and domain models

**Tasks:**
- WALLET-001: Setup Sorcha.WalletService project structure
- WALLET-002: Implement domain models and enums
- WALLET-003: Define service interfaces
- WALLET-018: Setup test project structure

**Deliverables:**
- Sorcha.WalletService library project
- Sorcha.WalletService.Api minimal API project
- Domain models (Wallet, WalletAddress, WalletAccess, WalletTransaction)
- Service interfaces (IWalletService, IKeyManagementService)
- Test project infrastructure
- Integrated with Sorcha.AppHost

#### Phase 3.2: Core Services (Weeks 11-13)
**Focus:** Wallet management logic

**Tasks:**
- WALLET-004: Implement WalletManager
- WALLET-005: Implement KeyManager
- WALLET-006: Implement TransactionService
- WALLET-007: Implement DelegationManager
- WALLET-019: Unit tests for core services

**Deliverables:**
- HD wallet creation and recovery (BIP32/BIP39/BIP44)
- Multi-algorithm support (ED25519, SECP256K1, RSA)
- Transaction signing and verification
- Access control and delegation
- >90% unit test coverage

#### Phase 3.3: Storage Layer (Weeks 14-15)
**Focus:** Database integration

**Tasks:**
- WALLET-008: Repository abstractions
- WALLET-009: EF Core repository implementation
- WALLET-010: In-memory repository for testing
- WALLET-020: Unit tests for repositories
- WALLET-022: Integration tests with Testcontainers

**Deliverables:**
- IWalletRepository and ITransactionRepository interfaces
- EF Core implementation (PostgreSQL/MySQL)
- In-memory implementation for testing
- Database migrations
- Integration tests with real databases

#### Phase 3.4: Encryption Layer (Weeks 16-17)
**Focus:** Secure key storage

**Tasks:**
- WALLET-011: Encryption provider abstractions
- WALLET-012: Azure Key Vault provider
- WALLET-013: AWS KMS provider
- WALLET-014: Local AES-GCM provider
- WALLET-021: Unit tests for encryption

**Deliverables:**
- IEncryptionProvider interface
- Azure Key Vault integration
- AWS KMS integration
- Local AES-256-GCM fallback
- Key rotation support

#### Phase 3.5: Event System (Week 18)
**Focus:** Messaging integration

**Tasks:**
- WALLET-015: Event model definitions
- WALLET-016: .NET Aspire messaging integration
- WALLET-017: In-memory event bus for testing
- WALLET-023: Integration tests for events

**Deliverables:**
- Event models (WalletCreated, TransactionSigned, etc.)
- .NET Aspire messaging integration
- Event publishing and subscription
- Integration tests

#### Phase 3.6: API Layer (Weeks 19-20)
**Focus:** REST API with Minimal APIs

**Tasks:**
- WALLET-025: Sorcha.WalletService.Api project
- WALLET-026: Minimal API endpoints
- WALLET-027: Aspire integration
- WALLET-024: End-to-end tests

**Deliverables:**
- Minimal API endpoints for wallet operations
- OpenAPI/Swagger documentation
- Integrated with Sorcha.AppHost
- Integrated with Sorcha.ApiGateway
- E2E tests

#### Phase 3.7: Testing & Performance (Week 21)
**Focus:** Quality assurance

**Tasks:**
- WALLET-028: Performance benchmarks
- WALLET-029: Security tests and audit

**Deliverables:**
- BenchmarkDotNet performance tests
- Load tests (10,000+ wallets, 1,000 concurrent ops)
- Security audit (OWASP Top 10)
- Performance report

#### Phase 3.8: Documentation (Week 22)
**Focus:** Developer experience

**Tasks:**
- WALLET-030: XML documentation and API reference
- WALLET-031: Integration guide

**Deliverables:**
- Complete XML documentation
- Integration guide with examples
- Security best practices guide

#### Phase 3.9: Deployment (Week 23)
**Focus:** Production readiness

**Tasks:**
- WALLET-032: Deployment and rollout

**Deliverables:**
- NuGet package publishing
- Aspire orchestration configuration
- CI/CD pipeline updates
- Monitoring and alerting
- Production deployment

### Phase 4: Register Service (Future)
**Status:** 📅 Future
**Duration:** To Be Determined
**Related Specification:** [sorcha-register-service.md](.specify/specs/sorcha-register-service.md)

**Planned Features:**
- Distributed ledger implementation
- Block creation and validation
- Transaction registry
- Merkle tree management
- Query capabilities
- Integration with Wallet Service for transaction history

**Current Status:**
- Boilerplate specification created
- Stub implementation for Wallet Service integration
- Full specification to be developed based on priority

### Phase 5: Tenant Service (Future)
**Status:** 📅 Future
**Duration:** To Be Determined
**Related Specification:** [sorcha-tenant-service.md](.specify/specs/sorcha-tenant-service.md)

**Planned Features:**
- Multi-tenant management
- Identity provider integration (Azure AD, B2C)
- Tenant-specific policies
- Billing and metering

**Current Status:**
- Boilerplate specification created
- Simple tenant provider for Wallet Service
- Full specification to be developed based on priority

## Cross-Cutting Concerns

### Testing Strategy
**Target:** >90% code coverage for core libraries, >80% for services

**Approach:**
- Unit tests for all business logic
- Integration tests with Testcontainers
- E2E tests with Playwright (UI)
- Performance tests with NBomber
- Security testing (OWASP Top 10)

### CI/CD Pipeline
**Tools:** GitHub Actions

**Stages:**
1. Build & compile
2. Run unit tests
3. Run integration tests (with Docker)
4. Code coverage analysis
5. Security scanning
6. NuGet package creation
7. Docker image creation
8. Deployment to staging
9. E2E tests on staging
10. Production deployment (manual approval)

### Security
**Focus Areas:**
- Private key encryption (AES-256-GCM)
- Secure key management (Azure KV, AWS KMS)
- OWASP Top 10 compliance
- Regular security audits
- Dependency vulnerability scanning
- Secrets management (never in code)

### Observability
**Stack:**
- OpenTelemetry for distributed tracing
- Structured logging with Serilog
- Health checks at all levels
- .NET Aspire dashboard for development
- Application Insights for production (optional)

## Success Metrics

### Phase 2 (Blueprint Services) Success Criteria
- [ ] All Blueprint Service endpoints functional
- [ ] Blueprint Designer UI operational
- [ ] >80% test coverage
- [ ] Performance: <100ms API response time (p95)
- [ ] Documentation complete

### Phase 3 (Wallet Service) Success Criteria
- [ ] All 32 wallet tasks completed
- [ ] >90% unit test coverage
- [ ] <50ms transaction signing latency (p95)
- [ ] <100ms payload decryption latency (p95)
- [ ] Support 10,000+ wallets per tenant
- [ ] Security audit passed
- [ ] Complete documentation
- [ ] Successful Aspire integration
- [ ] Production ready

## Timeline Summary

```
Weeks 1-8:   Phase 2 - Blueprint Services (In Progress)
Weeks 9-23:  Phase 3 - Wallet Service (Planned)
Weeks 24+:   Phase 4 - Register Service (Future)
Weeks 24+:   Phase 5 - Tenant Service (Future)
```

## Next Actions

### Immediate (This Week)
1. ✅ Complete Wallet Service specification
2. ✅ Create task breakdown
3. ✅ Update constitution and spec documents
4. 🔄 Complete Blueprint Service API implementation
5. 🔄 Finish Blueprint Designer UI

### Short Term (Weeks 1-4)
1. Complete Phase 2 (Blueprint Services)
2. Prepare for Phase 3 (Wallet Service)
3. Review and approve Wallet Service architecture
4. Setup development environment for Wallet Service
5. Begin WALLET-001 (Project Setup)

### Medium Term (Weeks 5-23)
1. Execute Phase 3 (Wallet Service) tasks
2. Regular progress reviews and adjustments
3. Continuous testing and quality assurance
4. Documentation as we build
5. Prepare for Register Service specification

### Long Term (Weeks 24+)
1. Specify and implement Register Service
2. Specify and implement Tenant Service
3. Enhanced Peer Service integration
4. Platform optimization and scaling
5. Additional features and integrations

---

**Document Control**
- **Last Updated:** 2025-11-13
- **Next Review:** Weekly during active development
- **Owner:** Sorcha Architecture Team
