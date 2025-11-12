# Sorcha Project Specification Kit

**Version:** 1.0.0
**Last Updated:** 2025-11-11
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
- **.NET Aspire 9.5.2** (Orchestration)
- **gRPC** (Service communication)
- **OpenTelemetry** (Observability)
- **Docker & Azure Container Apps** (Deployment)

### Architecture
- **Microservices**: Independent deployable services
- **API-First**: RESTful APIs with OpenAPI/Swagger
- **Cloud-Native**: Built for containers and cloud platforms
- **Observable by Default**: Comprehensive telemetry

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
- ✅ Input validation REQUIRED on all external boundaries
- ✅ JSON Schema validation REQUIRED for all blueprints
- ✅ NO secrets in code or configuration files
- ✅ All APIs MUST implement rate limiting
- ✅ Security headers MUST be configured on all web services

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
│   ├── Common/           # Shared libraries (Models, ServiceDefaults)
│   ├── Core/             # Core business logic (Engine, Fluent, Schemas)
│   ├── Apps/
│   │   ├── Hosting/      # Orchestration (AppHost, ServiceDefaults)
│   │   ├── Services/     # Microservices (API, Gateway, Peer)
│   │   └── UI/           # User interfaces (Designer Client & Server)
│   └── Services/         # Background services
├── tests/                # Test projects (mirrors src structure)
├── docs/                 # Documentation
├── infra/                # Infrastructure as Code (Azure Bicep)
├── scripts/              # Utility scripts
└── .ai/                  # AI assistance specifications (THIS DIRECTORY)
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
- **Blueprint**: Root workflow definition with title, description, participants, and actions
- **Action**: Workflow step with data schemas, routing, disclosures, and calculations
- **Participant**: Workflow party with ID, name, organization, and DID
- **Disclosure**: Data visibility rule using JSON Pointers
- **Condition**: Routing logic using JSON Logic expressions
- **Calculation**: Computed field with JSON Logic expressions

### Ubiquitous Language
Use these terms consistently across code, docs, and discussions:
- **Blueprint** (not "workflow" or "process")
- **Action** (not "step" or "task")
- **Participant** (not "user" or "party")
- **Disclosure** (not "visibility" or "access control")
- **Execution Context** (not "runtime" or "session")

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
