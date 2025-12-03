# Implementation Plan: Blueprint Service

**Feature Branch**: `blueprint-service`
**Created**: 2025-12-03
**Status**: 90% Complete (MVD Phase)

## Summary

The Blueprint Service provides the core workflow definition and execution capabilities for the Sorcha platform. It enables creation of multi-party declarative workflows with JSON Schema validation, JSON Logic routing, and privacy-preserving disclosures.

## Design Decisions

### Decision 1: JSON Schema for Data Validation

**Approach**: Use NJsonSchema for JSON Schema Draft 7 validation with schema caching.

**Rationale**: NJsonSchema provides robust validation with good error messages and supports `$ref` resolution for external schemas.

**Alternatives Considered**:
- JsonSchema.Net - Good performance but less mature ecosystem
- Custom validation - Would require significant implementation effort

### Decision 2: JSON Logic Implementation

**Approach**: Use JsonLogic.Net (or custom implementation based on json-logic-js spec).

**Rationale**: JSON Logic provides a declarative, sandboxed way to express routing and calculation logic that can be stored and audited.

**Alternatives Considered**:
- Jint (JavaScript execution) - Security concerns with arbitrary code execution
- Expression Trees - Less portable, harder to serialize

### Decision 3: Fluent Builder API

**Approach**: Provide a fluent C# API for programmatic blueprint construction.

**Rationale**: Type-safe builder pattern improves developer experience and catches errors at compile time.

**Alternatives Considered**:
- Direct JSON construction - Error-prone, no IntelliSense
- YAML DSL - Additional parsing complexity

### Decision 4: Schema Caching Strategy

**Approach**: In-memory caching with distributed Redis fallback.

**Rationale**: Schema resolution is expensive; caching improves validation performance by 10x.

**Implementation**: See `docs/schema-caching-implementation.md`

## Technical Approach

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│                 Sorcha.Blueprint.Api                     │
│                   (ASP.NET Core 10)                      │
├─────────────────────────────────────────────────────────┤
│  Endpoints/                                              │
│  ├── BlueprintEndpoints.cs      (CRUD operations)       │
│  ├── InstanceEndpoints.cs       (Workflow execution)    │
│  └── SchemaEndpoints.cs         (Schema management)     │
├─────────────────────────────────────────────────────────┤
│  Services/                                               │
│  ├── IBlueprintService.cs                               │
│  ├── BlueprintService.cs                                │
│  ├── IActionExecutor.cs                                 │
│  ├── ActionExecutor.cs                                  │
│  └── SchemaValidationService.cs                         │
├─────────────────────────────────────────────────────────┤
│  Repositories/                                           │
│  ├── IBlueprintRepository.cs                            │
│  └── InMemoryBlueprintRepository.cs (MVP)               │
│  └── PostgresBlueprintRepository.cs (Production)        │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│              Sorcha.Blueprint.Models                     │
│           (Domain Models, JSON-LD Support)              │
├─────────────────────────────────────────────────────────┤
│  Blueprint.cs, Participant.cs, Action.cs                │
│  Disclosure.cs, Control.cs, Condition.cs                │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│              Sorcha.Blueprint.Fluent                     │
│              (Fluent Builder API)                        │
├─────────────────────────────────────────────────────────┤
│  BlueprintBuilder.cs, ActionBuilder.cs                  │
│  ParticipantBuilder.cs, SchemaBuilder.cs                │
│  JsonLogicBuilder.cs, FormBuilder.cs                    │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│              Sorcha.Blueprint.Engine                     │
│         (Portable Execution Engine - 100%)              │
├─────────────────────────────────────────────────────────┤
│  ExecutionEngine.cs - Action execution logic            │
│  SchemaValidator.cs - JSON Schema validation            │
│  JsonLogicEvaluator.cs - Routing/calculations           │
│  DisclosureFilter.cs - Data access filtering            │
└─────────────────────────────────────────────────────────┘
```

### Component Status

| Component | Status | Notes |
|-----------|--------|-------|
| Blueprint.Models | 100% | Core domain models complete |
| Blueprint.Fluent | 100% | Full fluent builder API |
| Blueprint.Engine | 100% | Portable execution engine |
| Blueprint.Api | 90% | Needs database persistence |
| Blueprint.Api.Tests | 60% | Integration tests needed |

### API Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| POST | `/api/blueprints` | Create blueprint | Done |
| GET | `/api/blueprints` | List blueprints | Done |
| GET | `/api/blueprints/{id}` | Get blueprint | Done |
| PUT | `/api/blueprints/{id}` | Update blueprint | Done |
| DELETE | `/api/blueprints/{id}` | Delete blueprint | Done |
| POST | `/api/blueprints/{id}/instances` | Create instance | Done |
| POST | `/api/instances/{id}/actions/{actionId}` | Execute action | Done |
| GET | `/api/schemas` | List schemas | Done |
| POST | `/api/schemas/validate` | Validate data | Done |

## Dependencies

### Internal Dependencies

- `Sorcha.ServiceDefaults` - .NET Aspire configuration
- `Sorcha.Cryptography` - Signature verification
- `Sorcha.TransactionHandler` - Transaction building

### External Dependencies

- `NJsonSchema` - JSON Schema validation
- `JsonE.NET` - JSON-e template evaluation
- `System.Text.Json` - JSON serialization
- `PostgreSQL` - Database persistence (via EF Core)
- `Redis` - Distributed caching

### Service Dependencies

- Wallet Service - Participant wallet verification
- Register Service - Transaction storage
- Tenant Service - Multi-tenant isolation

## Migration/Integration Notes

### Database Migration

Blueprint persistence currently uses in-memory storage. Migration to PostgreSQL required:

1. Create EF Core DbContext with Blueprint entity mapping
2. Handle JSON column storage for schemas and JSON Logic expressions
3. Implement migration scripts for existing data

### Breaking Changes

- None for MVD phase
- Future: Blueprint schema v2 will require migration

## Open Questions

1. Should blueprint templates (JSON-e) be a separate entity or embedded?
2. How to handle long-running workflows with state persistence?
3. Should we support blueprint import/export in standardized format?
