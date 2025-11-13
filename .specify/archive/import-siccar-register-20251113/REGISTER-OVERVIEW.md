# Siccar Register Service Import Overview

**Import Date:** 2025-11-13
**Source Repository:** https://github.com/StuartF303/SICCARV3.git
**Import Purpose:** Extract Register service specifications for upgrade to Sorcha platform

## Imported Files

### Specification Document
- **siccar-register-service.md** - Complete Register service specification including:
  - Background and current state analysis
  - Architecture design and data models
  - Storage abstraction layer
  - Event system design
  - Integration points with other services
  - Implementation task breakdown
  - Success criteria and non-functional requirements

### Implementation Task Files
- **REG-001-setup-register-core-project.md** - Project setup task
- **REG-002-implement-exceptions-enums.md** - Core exceptions and enums
- **REG-003-define-storage-abstractions.md** - Storage interface definitions
- **REG-004-implement-register-service.md** - Register business logic
- **REG-005-implement-transaction-service.md** - Transaction management
- **REG-006-implement-docket-service.md** - Docket/block management
- **REG-008-implement-mongodb-repository.md** - MongoDB storage implementation
- **REG-009-implement-inmemory-repository.md** - In-memory storage for testing
- **REG-011-implement-event-abstractions.md** - Event system interfaces
- **REG-012-implement-dapr-event-bus.md** - Dapr pub/sub implementation
- **REG-022-unit-tests-register-operations.md** - Unit testing guidelines
- **TASK-023-integrate-register-service.md** - Service integration task

### README Documentation
- **RegisterCore-Readme.md** - Core library documentation
- **RegisterService-README.md** - Service README
- **RegisterService-RELEASENOTES.md** - Release notes

## Key Components Identified

### Register Service Architecture (Siccar V3)

**Core Projects:**
1. **RegisterService** - ASP.NET Core Web API with controllers and SignalR hubs
2. **RegisterCore** - Domain models, interfaces, and core logic
3. **RegisterCoreMongoDBStorage** - MongoDB repository implementation
4. **RegisterTests** - Unit test suite
5. **RegisterService.IntegrationTests** - Integration test suite

**Key Features:**
- Register CRUD operations with OData query support
- Transaction storage and retrieval
- Docket (block) creation and sealing
- Multi-tenant isolation via TenantService integration
- Real-time updates via SignalR
- Event-driven architecture with Dapr pub/sub
- MongoDB-based persistence with dynamic collections per register
- JWT authentication and role-based access control

**Key Models:**
- **Register** - Ledger entity with height tracking, status, and metadata
- **TransactionModel** - Signed transaction with encrypted payloads
- **Docket** - Sealed collection of transactions (blockchain block)
- **PayloadModel** - Encrypted data with wallet-based access control
- **TransactionMetaData** - Blueprint workflow tracking

**Integration Points:**
- **TenantService** - Authorization and multi-tenant filtering
- **ValidatorService** - Transaction validation and consensus
- **WalletService** - Transaction signing and address management
- **PeerService** - Network synchronization (future)

**Technologies:**
- ASP.NET Core with OData v4
- Dapr for pub/sub and service-to-service calls
- MongoDB for distributed storage
- SignalR for real-time notifications
- Serilog for structured logging
- Application Insights for telemetry

## Upgrade Requirements for Sorcha

### Architectural Changes

1. **Service Orchestration**
   - Replace Dapr with .NET Aspire for service orchestration
   - Use Aspire messaging for pub/sub instead of Dapr topics
   - Implement Aspire service discovery
   - Add health checks and observability via ServiceDefaults

2. **Naming and Branding**
   - Replace all "Siccar" references with "Sorcha"
   - Update namespaces: `Siccar.*` → `Sorcha.*`
   - Update project names and directory structure
   - Align with Sorcha naming conventions

3. **Technology Stack Alignment**
   - Target .NET 10 (current: .NET 8/9)
   - Use modern C# 13 features
   - Align with Sorcha's 4-layer architecture pattern
   - Follow Sorcha project structure conventions

4. **Data Storage**
   - Evaluate MongoDB vs PostgreSQL vs hybrid approach
   - Consider EF Core for relational data (matching Wallet service)
   - Maintain storage abstraction layer
   - Support for distributed caching with Redis

5. **Event System**
   - Design event bus abstraction compatible with Aspire
   - Support multiple event providers (Aspire, RabbitMQ, Azure Service Bus)
   - Implement event versioning and backward compatibility
   - Add idempotent event handlers

6. **Authentication & Authorization**
   - Align with Sorcha's authentication strategy
   - Remove Dapr-specific auth schemes
   - Implement unified JWT validation
   - Support multi-tenant access control

7. **API Design**
   - Follow Sorcha's Minimal API patterns
   - Maintain OData support for advanced queries
   - Add OpenAPI/Swagger documentation
   - Consider gRPC for service-to-service calls

8. **Real-time Communication**
   - Evaluate SignalR vs alternatives
   - Consider server-sent events or WebSockets
   - Implement connection management and scaling
   - Add Redis backplane for multi-instance deployments

### Testing Strategy

1. **Unit Tests**
   - Target >90% code coverage
   - Use xUnit, Moq, FluentAssertions
   - In-memory repository for fast tests
   - Test all business logic independently

2. **Integration Tests**
   - Test with real storage backends
   - Test Aspire messaging integration
   - Test multi-service scenarios
   - Use Testcontainers for dependencies

3. **Performance Tests**
   - Benchmark transaction throughput
   - Test query performance at scale
   - Load testing for SignalR connections
   - Storage performance benchmarks

### Documentation Requirements

1. **Specification**
   - Updated architecture diagrams
   - Sorcha-specific integration points
   - Technology stack documentation
   - Migration guide from Siccar

2. **Implementation Tasks**
   - Break down into Sorcha-compatible sprints
   - Align with Sorcha development workflow
   - Define clear acceptance criteria
   - Estimate effort and dependencies

3. **API Documentation**
   - OpenAPI/Swagger specs
   - Usage examples with Sorcha SDK
   - Integration patterns
   - Best practices guide

## Next Steps

1. Create comprehensive Sorcha Register Service specification
2. Update all task files with Sorcha-specific requirements
3. Design integration with existing Sorcha services
4. Create implementation roadmap aligned with project priorities
5. Review and validate with Sorcha architecture team

## Notes

- The Siccar Register Service is well-architected with good separation of concerns
- Storage abstraction layer is a strong foundation for Sorcha
- Event-driven architecture aligns well with distributed system requirements
- Multi-tenant support is critical and well-implemented
- SignalR real-time updates are valuable for user experience
- OData support provides powerful query capabilities
- Consider whether to maintain full backward compatibility or modernize architecture

## References

- [Sorcha Constitution](../../constitution.md)
- [Sorcha Architecture](../../../docs/architecture.md)
- [Wallet Service Specification](../../specs/sorcha-wallet-service.md)
- [Transaction Handler Specification](../../specs/sorcha-transaction-handler.md)
