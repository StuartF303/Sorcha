# Sorcha Project Specification Kit

**Version:** 1.1.0
**Last Updated:** 2025-11-12
**Project:** Sorcha - Blueprint Execution Engine and Designer

---

## Purpose

This Specification Kit (spec-kit) defines the complete set of architectural principles, coding standards, security requirements, and development guidelines for the Sorcha project. All contributors—human and AI—must read, understand, and comply with these definitions and rules.

---

## Quick Reference

| Document | Purpose | Priority |
|----------|---------|----------|
| [Architecture Rules](./architecture-rules.md) | System design, structure, and patterns | **CRITICAL** |
| [Coding Standards](./coding-standards.md) | Code style, conventions, and best practices | **CRITICAL** |
| [Security Guidelines](./security-guidelines.md) | Security requirements and practices | **CRITICAL** |
| [Testing Requirements](./testing-requirements.md) | Test coverage, strategies, and standards | **HIGH** |

---

## Project Overview

**Sorcha** is a modern .NET 10 blueprint execution engine and designer for data flow orchestration with multi-participant workflow capabilities.

### Core Technologies
- **.NET 10** (10.0.100) with C# 13
- **ASP.NET Core 10** (Minimal APIs)
- **Blazor Server & WebAssembly**
- **.NET Aspire 13.0.0** (Service orchestration & discovery)
- **gRPC 2.71.0** (Peer-to-peer communication)
- **YARP 2.2.0** (Reverse proxy & API Gateway)
- **Redis** (Distributed output caching with Aspire.StackExchange.Redis 13.0.0)
- **FluentValidation 11.10.0** (Input validation)
- **JsonSchema.Net 7.4.0** (Blueprint schema validation)
- **OpenTelemetry 1.12.0** (Metrics, tracing, logging)
- **Scalar 2.10.0** (API documentation UI, not Swagger UI)
- **Docker & Azure Container Apps** (Deployment)

### Architecture
- **Microservices**: Independent deployable services (Blueprint API, Peer Service, API Gateway)
- **API-First**: RESTful APIs with OpenAPI/Scalar documentation
- **Cloud-Native**: Built for containers and cloud platforms
- **Observable by Default**: Comprehensive OpenTelemetry telemetry
- **In-Memory Storage**: Currently using in-memory stores (no database persistence yet)

---

## Mandatory Compliance Rules

### 1. Architecture Compliance
- ✅ All services MUST follow the layered architecture (UI → Gateway → Core → Common → Infrastructure)
- ✅ Services MUST be independently deployable
- ✅ All inter-service communication MUST use service discovery
- ✅ All APIs MUST implement health check endpoints (`/health`, `/alive`)
- ✅ All services MUST integrate OpenTelemetry tracing

### 2. Code Quality Standards
- ✅ Nullable reference types MUST be enabled
- ✅ All public APIs MUST have XML documentation
- ✅ No warnings allowed in Release builds
- ✅ Code MUST pass all static analysis checks
- ✅ All async methods MUST follow proper async/await patterns

### 3. Security Requirements
- ✅ Input validation REQUIRED on all external boundaries (using DataAnnotations & FluentValidation)
- ✅ JSON Schema validation REQUIRED for all blueprints (using JsonSchema.Net)
- ✅ NO secrets in code or configuration files (use User Secrets for dev, Azure Key Vault for prod)
- ⚠️ Authentication/Authorization: NOT YET IMPLEMENTED (planned for future)
- ⚠️ Rate limiting: NOT YET IMPLEMENTED (planned for future)
- ⚠️ Security headers: Partially implemented (HTTPS redirect in Blazor, but not all services)
- ⚠️ CORS: Currently permissive in API Gateway (needs tightening for production)

### 4. Testing Requirements
- ✅ Unit tests REQUIRED for all business logic
- ✅ Integration tests REQUIRED for all APIs
- ✅ Minimum 80% code coverage for Core projects
- ✅ All tests MUST be deterministic and isolated
- ✅ Mock external dependencies in unit tests

### 5. Documentation Requirements
- ✅ All public APIs documented with XML comments
- ✅ Architecture decisions recorded in `docs/architecture.md`
- ✅ README updated for new features
- ✅ OpenAPI/Swagger specs auto-generated and current

---

## Project Structure

```
Sorcha/
├── src/
│   ├── Apps/                                      # Application layer
│   │   ├── Sorcha.AppHost/                        # .NET Aspire orchestration host
│   │   └── UI/
│   │       └── Sorcha.Blueprint.Designer.Client/  # Blazor WASM UI
│   ├── Common/                                    # Cross-cutting concerns
│   │   ├── Sorcha.Blueprint.Models/               # Domain models & DTOs
│   │   ├── Sorcha.Cryptography/                   # Cryptography library
│   │   └── Sorcha.ServiceDefaults/                # Service configuration utilities
│   ├── Core/                                      # Business logic layer
│   │   ├── Sorcha.Blueprint.Engine/               # Blueprint execution engine
│   │   ├── Sorcha.Blueprint.Fluent/               # Fluent API builders
│   │   └── Sorcha.Blueprint.Schemas/              # Schema management
│   └── Services/                                  # Service layer
│       ├── Sorcha.ApiGateway/                     # YARP-based API Gateway
│       ├── Sorcha.Blueprint.Service/              # Blueprint REST API
│       └── Sorcha.Peer.Service/                   # P2P networking service
├── tests/                                         # All test projects
│   ├── Sorcha.Blueprint.Engine.Tests/
│   ├── Sorcha.Blueprint.Fluent.Tests/
│   ├── Sorcha.Blueprint.Models.Tests/
│   ├── Sorcha.Blueprint.Schemas.Tests/
│   ├── Sorcha.Cryptography.Tests/
│   ├── Sorcha.Gateway.Integration.Tests/
│   ├── Sorcha.Integration.Tests/
│   ├── Sorcha.Peer.Service.Tests/
│   ├── Sorcha.Performance.Tests/
│   └── Sorcha.UI.E2E.Tests/
├── docs/                                          # Documentation
│   ├── architecture.md                            # System architecture
│   ├── project-structure.md                       # Detailed structure guide
│   └── ...
├── samples/                                       # Sample blueprints
│   └── blueprints/
│       ├── benefits/
│       ├── finance/
│       ├── healthcare/
│       └── supply-chain/
├── infra/                                         # Infrastructure as Code
├── scripts/                                       # Build and deployment scripts
└── .ai/                                           # AI assistance specifications
    └── spec-kit/                                  # THIS DIRECTORY
```

---

## Key Design Principles

1. **Cloud-Native First**: Designed for containerization and orchestration
2. **API-First**: All functionality exposed via well-documented APIs
3. **Observable by Default**: Comprehensive telemetry from day one
4. **Security by Design**: Input validation, schema validation, audit logging
5. **Domain-Driven Design**: Rich domain models with ubiquitous language
6. **Fail Fast**: Validate early, throw meaningful exceptions
7. **Immutability Preferred**: Use immutable objects where possible
8. **Explicit Over Implicit**: Clear, readable code over clever tricks

---

## Domain Model

### Core Entities
- **Blueprint**: Root workflow definition with title, description, version, participants, actions, and optional JSON-LD context
- **Action**: Workflow step with sender, target, data schemas, routing conditions, disclosures, calculations, and optional UI form controls
- **Participant**: Workflow party with ID, name, organization, wallet address, optional DID (Decentralized Identifier), optional Verifiable Credential, and privacy features (stealth address support)
- **Disclosure**: Data visibility rule using JSON Pointers to control what data participants can see
- **Condition**: Routing logic using JSON Logic expressions for conditional workflow branching
- **Calculation**: Computed field with JSON Logic expressions for deriving values
- **PublishedBlueprint**: Immutable versioned snapshot of a blueprint with publish metadata
- **SchemaDocument**: JSON Schema with metadata (source, category, usage statistics)

### Ubiquitous Language
Use these terms consistently across code, docs, and discussions:
- **Blueprint** (not "workflow" or "process")
- **Action** (not "step" or "task")
- **Participant** (not "user" or "party")
- **Disclosure** (not "visibility" or "access control")
- **Publish** (not "deploy" or "release") - creating immutable blueprint versions
- **JSON-LD Context** - semantic web metadata for blueprints
- **Fluent Builder** - API pattern for constructing blueprints programmatically

---

## Development Workflow

### 1. Before Writing Code
- Read relevant spec-kit documents
- Review existing patterns in the codebase
- Check for existing implementations
- Verify architectural fit

### 2. During Development
- Follow coding standards strictly
- Write tests alongside code (TDD encouraged)
- Document public APIs with XML comments
- Use meaningful commit messages

### 3. Before Committing
- Run full test suite (`dotnet test`)
- Check code coverage
- Run static analysis
- Verify no build warnings
- Update documentation if needed

### 4. CI/CD
- All PRs trigger automated builds and tests
- Code coverage reports generated
- Security scanning via CodeQL
- Multi-platform validation (Linux, Windows, macOS)

---

## AI Assistant Guidelines

When assisting with the Sorcha project, AI assistants MUST:

1. **Read the Spec-Kit First**
   - Review all documents in `.ai/spec-kit/` before making changes
   - Understand the architecture before proposing solutions
   - Follow established patterns and conventions

2. **Maintain Consistency**
   - Use existing patterns and styles
   - Match naming conventions
   - Follow project structure
   - Respect architectural boundaries

3. **Prioritize Quality**
   - Write tests for all new code
   - Document public APIs
   - Validate inputs
   - Handle errors gracefully
   - Follow security guidelines

4. **Communicate Clearly**
   - Explain architectural decisions
   - Reference spec-kit sections when applicable
   - Ask for clarification on ambiguous requirements
   - Document assumptions

5. **Be Proactive**
   - Identify potential issues early
   - Suggest improvements
   - Flag security concerns
   - Recommend refactoring when appropriate

---

## Breaking Changes

Changes that require spec-kit updates:

- New architectural patterns or layers
- Technology stack changes
- New security requirements
- Changes to coding standards
- New mandatory practices

**Process**: Update spec-kit documents BEFORE implementing breaking changes.

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.1.0 | 2025-11-12 | Updated to reflect actual codebase implementation, added accurate technology stack, clarified security status |
| 1.0.0 | 2025-11-11 | Initial spec-kit creation |

---

## References

- [Project README](../../README.md)
- [Architecture Documentation](../../docs/architecture.md)
- [Getting Started Guide](../../docs/getting-started.md)
- [Contributing Guidelines](../../CONTRIBUTING.md)
- [Blueprint Schema Specification](../../docs/blueprint-schema.md)

---

## Feedback and Updates

This spec-kit is a living document. To suggest updates:

1. Review existing documentation
2. Discuss in issue or PR
3. Update spec-kit documents
4. Update version number and history
5. Commit with clear message

---

**Remember**: This spec-kit exists to maintain consistency, quality, and architectural integrity across the Sorcha project. Compliance is not optional—it ensures long-term maintainability and success.
