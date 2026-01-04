# CLAUDE.md - AI Assistant Guide for Sorcha Project

**Version:** 1.1
**Last Updated:** 2025-12-07
**Status:** Active
**Purpose:** Quick reference guide for AI coding assistants working on the Sorcha project

---

## Welcome, AI Assistant! 👋

This document is your starting point for understanding and working effectively with the Sorcha codebase. Sorcha is a distributed ledger platform built on .NET 10, using a microservices architecture with .NET Aspire orchestration.

**Current Project Status:** MVD Phase - 97% Complete (Production Readiness: 10%)
**Active Development Branch:** See Git context for current branch
**Documentation Standard:** [Spec-Kit](https://github.com/github/spec-kit) methodology

---

## 🚀 Quick Start - What You Need to Know

### 1. **READ THESE FIRST** (In Order)

Before making any code changes, familiarize yourself with these key documents:

| Priority | Document | Purpose | Link |
|----------|----------|---------|------|
| ⭐⭐⭐ | **Constitution** | Non-negotiable principles, standards, and architectural guidelines | [.specify/constitution.md](.specify/constitution.md) |
| ⭐⭐⭐ | **AI Documentation Policy** | **MANDATORY** requirements for AI-generated code | [.specify/AI-CODE-DOCUMENTATION-POLICY.md](.specify/AI-CODE-DOCUMENTATION-POLICY.md) |
| ⭐⭐ | **Master Plan** | Current development phase, MVD priorities, and timeline | [.specify/MASTER-PLAN.md](.specify/MASTER-PLAN.md) |
| ⭐⭐ | **Master Tasks** | Consolidated task list with P0-P3 priorities | [.specify/MASTER-TASKS.md](.specify/MASTER-TASKS.md) |
| ⭐ | **Specification** | Requirements, architecture, and user scenarios | [.specify/spec.md](.specify/spec.md) |
| ⭐ | **README** | Project overview and getting started | [README.md](README.md) |

### 2. **Understand the Architecture**

**Sorcha Platform Components:**
- **Blueprint Service** - Workflow definition and execution (100% complete)
- **Wallet Service** - Cryptographic wallet management (90% complete)
- **Register Service** - Distributed ledger for transactions (100% complete)
- **Peer Service** - P2P networking (65% complete)
- **API Gateway** - YARP-based routing (95% complete)

**Core Libraries:**
- `Sorcha.Blueprint.Models` - Domain models with JSON-LD support
- `Sorcha.Blueprint.Engine` - Portable execution engine (100% complete)
- `Sorcha.Cryptography` - Multi-algorithm crypto operations (ED25519, NIST P-256, RSA-4096)
- `Sorcha.TransactionHandler` - Transaction building and serialization
- `Sorcha.ServiceDefaults` - Shared .NET Aspire configurations

**Technology Stack:**
- **.NET 10** (target framework)
- **.NET Aspire** (service orchestration)
- **SignalR** (real-time notifications with Redis backplane)
- **Minimal APIs** (modern API design)
- **PostgreSQL/MongoDB/Redis** (data storage)

---

## 📋 AI Code Documentation Policy - CRITICAL

### ⚠️ MANDATORY: Documentation Updates

**When you generate ANY code, you MUST update documentation.** This is not optional.

#### Required Updates for AI-Generated Code:

1. **✅ MASTER-TASKS.md** - Update task status (📋 → 🚧 → ✅)
2. **✅ README files** - Update if features/APIs changed
3. **✅ docs/ files** - Update architecture/API reference if changed
4. **✅ Status files** - Update completion percentages
5. **✅ OpenAPI/XML docs** - Ensure all endpoints documented
6. **✅ Service specs** - Update if implementation deviates from spec

**Read the full policy:** [.specify/AI-CODE-DOCUMENTATION-POLICY.md](.specify/AI-CODE-DOCUMENTATION-POLICY.md)

**Pull Requests will NOT be approved without documentation updates.**

---

## 🎯 Current Development Focus (MVD Phase)

### What is the MVD (Minimum Viable Deliverable)?

The MVD delivers a working end-to-end system:
1. Create and manage blueprints (workflow definitions)
2. Execute actions through the portable execution engine
3. Sign transactions with secure wallets
4. Store transactions on a distributed ledger
5. Provide a user interface for blueprint design

### Current Priorities (P0 = Highest)

**P0 Tasks (MVD Blockers):**
- End-to-end integration testing (Blueprint → Wallet → Register)
- Wallet Service EF Core repository implementation
- Register Service MongoDB repository
- Production authentication/authorization

**P1 Tasks (Core MVD):**
- Azure Key Vault integration for Wallet Service
- Database persistence for Blueprint Service
- Performance testing and optimization
- Security hardening

**Use [MASTER-TASKS.md](.specify/MASTER-TASKS.md) to find your assigned tasks.**

---

## 📐 Architectural Principles (From Constitution)

### Non-Negotiable Standards

1. **Microservices-First Architecture**
   - Each service must be independently deployable
   - Use .NET Aspire for orchestration
   - Minimal coupling between services

2. **Security First**
   - Zero trust security model
   - All sensitive data encrypted at rest (AES-256-GCM)
   - Support for Azure Key Vault, AWS KMS
   - Never commit secrets to source control

3. **API Documentation**
   - ⚠️ **Use .NET 10 built-in OpenAPI** (NOT Swagger/Swashbuckle)
   - Use `Scalar.AspNetCore` for interactive API docs
   - All public APIs must have XML documentation
   - Include examples for complex payloads

4. **Testing Requirements**
   - Minimum 80% unit test coverage for core libraries
   - Target >85% for new code
   - Use xUnit as primary testing framework
   - Integration tests for all service APIs

5. **Code Quality**
   - Follow C# coding conventions
   - Use async/await for I/O operations
   - Leverage dependency injection
   - Target .NET 10 framework

6. **Blueprint Creation Standards**
   - ⚠️ **Always create blueprints as JSON or YAML documents** (primary format)
   - Use YAML for improved readability when appropriate
   - Fluent API is ONLY for rare developer scenarios requiring runtime generation
   - Use JSON-e (JsonE.NET) for runtime variable replacement (e.g., wallet addresses)
   - Store blueprint templates as JSON/YAML files, not C# code
   - AI agents must create blueprint demos as JSON/YAML, not Fluent API

7. **Service Client Pattern**
   - ⚠️ **Always use consolidated service clients from `Sorcha.ServiceClients`**
   - NEVER create duplicate client implementations in service projects
   - Use `builder.Services.AddServiceClients(builder.Configuration)` for DI registration
   - All inter-service communication uses clients from `src/Common/Sorcha.ServiceClients/`
   - Client interfaces include ALL methods needed by ANY consumer
   - See: [Sorcha.ServiceClients README](src/Common/Sorcha.ServiceClients/README.md)

**Full details:** [.specify/constitution.md](.specify/constitution.md)

---

## 🛠️ Working with the Codebase

### Before You Start Coding

1. **Identify the Task**
   - Check [MASTER-TASKS.md](.specify/MASTER-TASKS.md) for task details
   - Note the priority (P0, P1, P2, P3)
   - Review acceptance criteria

2. **Review Relevant Specs**
   - Service specifications: [.specify/specs/](.specify/specs/)
   - Architecture: [docs/architecture.md](docs/architecture.md)
   - Current status: [docs/development-status.md](docs/development-status.md)

3. **Check Dependencies**
   - Review task dependencies in MASTER-TASKS.md
   - Ensure prerequisite tasks are complete
   - Identify affected services/components

### While Coding

1. **Follow Established Patterns**
   - Review existing code in the same service
   - Use consistent naming conventions
   - Follow dependency injection patterns

2. **Write Tests as You Go**
   - Create test file alongside code
   - Aim for >85% coverage
   - Test edge cases and error conditions

3. **Document Your Code**
   - Add XML documentation to public APIs
   - Include OpenAPI annotations for endpoints
   - Add inline comments for complex logic
   - Update examples if APIs change

### After Generating Code

**⚠️ CRITICAL: Update Documentation (Per AI Policy)**

1. **Update MASTER-TASKS.md**
   - Change task status to ✅ Complete
   - Add completion date
   - Note any implementation decisions

2. **Update README Files**
   - Project README if major features added
   - Service README if APIs changed
   - Update feature lists and examples

3. **Update Technical Docs**
   - docs/architecture.md if architecture changed
   - docs/API-DOCUMENTATION.md if endpoints added
   - docs/development-status.md with new completion %

4. **Update Spec Files**
   - Service specs if implementation differs from plan
   - MASTER-PLAN.md if phase milestones reached

5. **Commit Message**
   - Reference task ID (e.g., "feat: WS-030 - Implement wallet creation endpoint")
   - List documentation files updated
   - Follow conventional commits format

---

## 📚 Key Reference Documents

### Specification Files (.specify/)

| File | Purpose | When to Update |
|------|---------|----------------|
| [constitution.md](.specify/constitution.md) | Project principles and standards | New patterns, security changes |
| [spec.md](.specify/spec.md) | Requirements and architecture | New features, requirement changes |
| [MASTER-PLAN.md](.specify/MASTER-PLAN.md) | Implementation strategy | Phase completion, milestone changes |
| [MASTER-TASKS.md](.specify/MASTER-TASKS.md) | Task tracking | Task status changes (ALWAYS) |
| [README.md](.specify/README.md) | Spec-Kit guide | Changes to documentation structure |

### Service Specifications (.specify/specs/)

| Service | Specification | Status |
|---------|---------------|--------|
| Wallet Service | [sorcha-wallet-service.md](.specify/specs/sorcha-wallet-service.md) | 90% complete |
| Register Service | [sorcha-register-service.md](.specify/specs/sorcha-register-service.md) | 100% complete |
| Cryptography | [sorcha-cryptography-rewrite.md](.specify/specs/sorcha-cryptography-rewrite.md) | 90% complete |
| Transaction Handler | [sorcha-transaction-handler.md](.specify/specs/sorcha-transaction-handler.md) | 68% complete |
| Tenant Service | [sorcha-tenant-service.md](.specify/specs/sorcha-tenant-service.md) | Stub only |

### Documentation (docs/)

| File | Purpose |
|------|---------|
| [development-status.md](docs/development-status.md) | Current completion status |
| [wallet-service-status.md](docs/wallet-service-status.md) | Detailed Wallet Service status |
| [architecture.md](docs/architecture.md) | System architecture |
| [API-DOCUMENTATION.md](docs/API-DOCUMENTATION.md) | API documentation |

---

## 🧪 Testing Guidelines

### Test Coverage Requirements

- **Core Libraries:** >85% coverage (constitutional requirement)
- **Services:** >80% coverage
- **APIs:** Integration tests for all endpoints
- **E2E:** Critical user workflows

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Sorcha.Blueprint.Api.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Watch mode (auto-rerun on changes)
dotnet watch test --project tests/Sorcha.Blueprint.Api.Tests
```

### Test Frameworks

- **xUnit** - Primary testing framework
- **FluentAssertions** - Assertion library
- **Moq** - Mocking framework
- **Testcontainers** - Integration test dependencies
- **NBomber** - Performance testing
- **Playwright** - E2E browser testing

---

## 🔐 Security Guidelines

### Cryptography

- Use `Sorcha.Cryptography` library for all crypto operations
- Supported algorithms: ED25519, NIST P-256, RSA-4096
- HD wallet support: BIP32, BIP39, BIP44
- Encryption: AES-256-GCM for data at rest
- Key management: Azure Key Vault, AWS KMS, local DPAPI

### Never Do This

- ❌ Commit secrets, keys, or credentials
- ❌ Store mnemonics (user responsibility to backup)
- ❌ Use weak encryption algorithms
- ❌ Skip authentication/authorization
- ❌ Log sensitive data (keys, mnemonics, passwords)

### Always Do This

- ✅ Use environment variables for secrets
- ✅ Encrypt sensitive data at rest
- ✅ Use TLS for all communication
- ✅ Implement proper access control
- ✅ Audit log all sensitive operations

---

## 🚢 Development Workflow

### 1. Start New Feature

```bash
# Check current branch
git status

# Create feature branch (if needed)
git checkout -b feature/your-feature-name

# Pull latest changes
git pull origin main
```

### 2. Build and Test

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test
```

### 3. Run Services Locally

```bash
# Using .NET Aspire (recommended)
dotnet run --project src/Apps/Sorcha.AppHost

# Aspire Dashboard: http://localhost:15888
# API Gateway: https://localhost:7082
# Blueprint Designer: https://localhost:7083
```

### 4. Make Changes

- Follow architectural principles
- Write tests alongside code
- Update documentation as you go
- Run tests frequently

### 5. Commit Changes

```bash
# Format code
dotnet format

# Stage changes
git add .

# Commit with descriptive message
git commit -m "feat: [TASK-ID] - Brief description

- Implementation details
- Documentation updated: README.md, MASTER-TASKS.md
- Tests added/updated"

# Push to branch
git push -u origin feature/your-feature-name
```

### 6. Create Pull Request

- Reference task ID in PR title
- List documentation files updated
- Include test results
- Request review from team

---

## 🎨 Code Style and Conventions

### Naming Conventions

- **PascalCase:** Classes, methods, properties, public fields
- **camelCase:** Local variables, parameters, private fields
- **UPPER_CASE:** Constants
- **Interfaces:** Prefix with `I` (e.g., `IWalletRepository`)

### File Organization

```
ServiceName/
├── Controllers/         # API controllers (if using controllers)
├── Services/           # Business logic services
├── Models/             # Domain models
├── Repositories/       # Data access
├── Interfaces/         # Service abstractions
├── Extensions/         # Extension methods
└── README.md          # Service documentation
```

### API Endpoint Patterns

```csharp
// ✅ Good: Minimal API with OpenAPI docs
app.MapPost("/api/wallets", async (CreateWalletRequest request) =>
{
    // Implementation
})
.WithName("CreateWallet")
.WithOpenApi(operation => new(operation)
{
    Summary = "Create a new wallet",
    Description = "Creates a new HD wallet with the specified algorithm"
});

// ❌ Bad: No documentation
app.MapPost("/api/wallets", async (CreateWalletRequest request) => { });
```

---

## 📊 Project Metrics

### Current Completion Status

| Component | Completion | Lines of Code | Tests |
|-----------|-----------|---------------|-------|
| Blueprint Engine | 100% | ~4,500 | 102 |
| Blueprint Service | 100% | ~6,000 | 123 |
| Wallet Service | 90% | ~8,000 | 111 |
| Register Service | 100% | ~3,500 | 112 |
| Cryptography | 90% | ~3,200 | 85+ |

**Overall MVD Progress:** 97% (Production Readiness: 10%)

---

## 🆘 Common Tasks and How-Tos

### Add a New REST API Endpoint

1. Add endpoint method to service
2. Add OpenAPI documentation with `.WithOpenApi()`
3. Add unit tests for endpoint logic
4. Add integration tests for HTTP calls
5. Update service README with endpoint details
6. Update docs/API-DOCUMENTATION.md
7. Update MASTER-TASKS.md task status

### Implement a New Service

1. Review service specification in .specify/specs/
2. Create project structure following existing patterns
3. Implement domain models
4. Implement service layer with interfaces
5. Implement repository with abstraction
6. Add .NET Aspire integration
7. Create comprehensive tests (unit + integration)
8. Update MASTER-PLAN.md and MASTER-TASKS.md
9. Create service README
10. Update docs/architecture.md

### Integrate Services

1. Define service interfaces
2. Use .NET Aspire service discovery
3. Add HTTP client with resilience policies
4. Implement error handling and retries
5. Add integration tests
6. Update service diagrams in docs/

### Add Database Repository

1. Define `IRepository` interface
2. Implement EF Core DbContext
3. Add migrations if using EF Core
4. Implement repository with CRUD operations
5. Add unit tests with in-memory database
6. Add integration tests with Testcontainers
7. Update service configuration for connection strings
8. Document in service README

### Use Service Clients for Inter-Service Communication

1. Add reference to `Sorcha.ServiceClients` project
2. Register clients in Program.cs: `builder.Services.AddServiceClients(builder.Configuration)`
3. Inject client interface (e.g., `IWalletServiceClient`) in constructor
4. Configure endpoints in appsettings.json under `ServiceClients:*`
5. NEVER create duplicate client implementations in your service
6. See: [Sorcha.ServiceClients README](src/Common/Sorcha.ServiceClients/README.md)

---

## 🔗 Important Links

### External Resources

- **.NET 10 Documentation:** https://learn.microsoft.com/dotnet/
- **.NET Aspire:** https://learn.microsoft.com/dotnet/aspire/
- **Spec-Kit Methodology:** https://github.com/github/spec-kit
- **BIP Standards:** https://github.com/bitcoin/bips

### Internal Resources

- **Spec-Kit Guide:** [.specify/README.md](.specify/README.md)
- **Contributing Guide:** [CONTRIBUTING.md](CONTRIBUTING.md)
- **Troubleshooting:** [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
- **License:** [LICENSE](LICENSE)

---

## 💡 Tips for AI Assistants

### DO

- ✅ Always read the constitution and AI documentation policy first
- ✅ Check MASTER-TASKS.md for task priorities
- ✅ Follow established code patterns in the service you're modifying
- ✅ Write tests alongside code (TDD approach)
- ✅ Update documentation proactively
- ✅ Use .NET 10 built-in OpenAPI (not Swagger/Swashbuckle)
- ✅ Reference task IDs in commits and PRs
- ✅ Ask clarifying questions if requirements are unclear

### DON'T

- ❌ Generate code without updating documentation
- ❌ Skip writing tests
- ❌ Ignore the constitution's architectural principles
- ❌ Use deprecated patterns or libraries
- ❌ Commit secrets or sensitive data
- ❌ Make breaking changes without documentation
- ❌ Assume requirements - verify in specs
- ❌ Use Swagger/Swashbuckle (use .NET 10 OpenAPI)

### Best Practices

1. **Always provide context** when asking questions
2. **Reference specific documents** when making architectural decisions
3. **Validate against specs** before implementing
4. **Test thoroughly** - unit, integration, and E2E
5. **Document as you go** - don't batch documentation updates
6. **Follow the MVD priorities** - P0 first, then P1, P2, P3
7. **Maintain consistency** with existing code patterns

### Creating Walkthroughs

When creating test scripts, demos, or exploration work:

**✅ DO:**
- Create a dedicated subdirectory in `walkthroughs/YourWalkthroughName/`
- Include a README.md explaining the purpose and how to use the scripts
- Use clear naming: PascalCase for directories, lowercase-hyphen for scripts
- Document actual results, limitations, and next steps
- Make scripts runnable from the repository root
- Include error handling and clear output

**❌ DON'T:**
- Put test scripts or results directly in the repository root
- Create undocumented scripts without context
- Use hardcoded credentials (use variables)
- Leave scripts without error handling

**Structure:**
```
walkthroughs/
└── YourWalkthroughName/
    ├── README.md              # Overview and instructions
    ├── test-*.ps1             # Test scripts
    ├── demo-*.ps1             # Demo scripts
    └── *-RESULTS.md           # Results and findings
```

See [walkthroughs/README.md](walkthroughs/README.md) for detailed guidelines and [walkthroughs/BlueprintStorageBasic/](walkthroughs/BlueprintStorageBasic/) for an example.

---

## 🏁 Quick Checklist for Code Changes

Before submitting code, verify:

- [ ] Code follows architectural principles from constitution
- [ ] Tests written with >85% coverage
- [ ] All public APIs have XML documentation
- [ ] REST endpoints have OpenAPI documentation
- [ ] MASTER-TASKS.md updated with task status
- [ ] README files updated if features changed
- [ ] docs/ files updated if architecture changed
- [ ] Service specs updated if implementation differs
- [ ] Commit message references task ID
- [ ] No secrets or sensitive data in code
- [ ] Code formatted with `dotnet format`
- [ ] All tests passing locally

---

## 📞 Getting Help

### For AI Assistants

- **Unclear Requirements:** Reference spec files and ask specific questions
- **Architectural Decisions:** Cite constitution.md principles
- **Implementation Patterns:** Review existing code in similar services
- **Testing Strategy:** Check existing test projects for examples

### For Human Developers

- **Documentation:** Check [.specify/README.md](.specify/README.md)
- **Issues:** Create GitHub issue with appropriate label
- **Architecture Questions:** Tag architecture team in PR
- **Clarifications:** Update relevant spec document with explanation

---

## 📝 Document Maintenance

**This Document:**
- **Created:** 2025-11-17
- **Last Updated:** 2025-12-07
- **Version:** 1.1
- **Owner:** Sorcha Architecture Team
- **Review Frequency:** Monthly or per major milestone

**When to Update:**
- New AI coding policies adopted
- Major project milestones reached
- Architecture patterns change
- New services added
- Documentation structure changes

---

## 🎯 Current Development Focus

**Active Phase:** Production Hardening

**MVD Core Status:** 97% Complete (Sprint 10 completed)

**Current Focus Areas:**
- Production authentication/authorization implementation
- Database persistence (EF Core, MongoDB)
- Security hardening
- Performance optimization

**Next Steps:**
- Review MASTER-PLAN.md for priorities
- Check MASTER-TASKS.md for available tasks
- Focus on P0 production readiness tasks
- Read relevant service specifications

---

**Welcome to the Sorcha project! 🚀**

Remember: Quality over speed. Follow the constitution, update documentation, write comprehensive tests, and maintain the architectural vision. When in doubt, reference the specification files!

**Happy Coding!** 💻
- Sorcha is based on the idea that modern IT systems if properly designed have a security model based on CIA (confidentiality, integrity and availability). For historical reasons MPS began Iso 27001 and ISMS look to manage technical and procedural controls to maintain corporate data governance - yet the data leaks, has no provenance and can’t be trusted for high assurance needs. The idea of decentralised digital cryptographic ledgers as exemplified by Bitcoin demonstrate a new design pattern that allows to look at the opposite view of system security architecture – DAD (Disclosure, Alteration and Destruction). Taking a system design where we are looking for the simplest, most robust and where we look to judge its capabilities against DAD, building data ‘Registers’ that are cryptographically secured data stores in the most simple, trustable and durable form. Decentralised registers where a peer network recording the changes in data and state over time (alteration) become, conditions apply, the most robust cybersecure data store making destruction risk and issues negligible. That leaves ‘disclosure’ – the request for attested data and the production of verifiable and validated data being managed through defined data objects (schema), user principals as viewed in the cryptographic keys maintained in a digital wallet.
- do not re introduce this problem: JsonSchema.Net's Evaluate() method expects JsonElement, not JsonNode
  - Fix: Convert JsonNode to JsonElement before validation
- the main services should communicate internally with each other using gRPC
- Aspire launches services on the ports described in each projects Properties/launchsettings.json
