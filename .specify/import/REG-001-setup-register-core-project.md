# Task: Setup RegisterService Core Library Project

**ID:** REG-001
**Status:** Not Started
**Priority:** Critical
**Estimate:** 4 hours
**Created:** 2025-11-13
**Specification:** [previous-codebase-register-service.md](../specs/previous-codebase-register-service.md)

## Objective

Create the foundational project structure for the new Siccar.RegisterService core library, establishing the base architecture for portable register management functionality.

## Tasks

### Project Creation
- [ ] Create `Siccar.RegisterService` class library project (.NET 9.0)
- [ ] Configure project properties (nullable enabled, implicit usings disabled)
- [ ] Set root namespace to `Siccar.RegisterService`
- [ ] Create solution folder structure for register-related projects
- [ ] Add project to main solution file

### Directory Structure
- [ ] Create `Services/` directory for business logic
- [ ] Create `Storage/` directory for repository abstractions
- [ ] Create `Events/` directory for event system
- [ ] Create `Models/` directory for domain entities
- [ ] Create `Authorization/` directory for access control
- [ ] Create `Exceptions/` directory for custom exceptions
- [ ] Create `Configuration/` directory for settings

### NuGet Dependencies
- [ ] Add `System.Linq.Async` (for IQueryable support)
- [ ] Add `System.ComponentModel.Annotations` (for validation)
- [ ] Add `Microsoft.Extensions.DependencyInjection.Abstractions`
- [ ] Add `Microsoft.Extensions.Logging.Abstractions`
- [ ] Add `Microsoft.Extensions.Configuration.Abstractions`

### Project References
- [ ] Reference `Siccar.Platform` (for Register, TransactionModel, Docket models)
- [ ] Reference `Siccar.Common` (for Topics, Constants)
- [ ] Verify models are accessible from Platform namespace

### Documentation Files
- [ ] Create README.md with project overview
- [ ] Create CHANGELOG.md for version tracking
- [ ] Add XML documentation generation to csproj
- [ ] Create LICENSE file (if applicable)

## Acceptance Criteria

- [ ] Project builds successfully without errors
- [ ] All required NuGet packages installed
- [ ] Directory structure matches specification
- [ ] XML documentation generation enabled
- [ ] Project integrated into solution

## Definition of Done

- Project compiles successfully
- No build warnings
- README.md documents purpose and structure
- All directories created and organized
- Git repository updated with new project

---

**Dependencies:** None
**Blocks:** REG-002, REG-003, REG-004
