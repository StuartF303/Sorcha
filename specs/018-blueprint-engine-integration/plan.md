# Implementation Plan: Blueprint Engine Integration

**Branch**: `018-blueprint-engine-integration` | **Date**: 2026-02-06 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/018-blueprint-engine-integration/spec.md`

## Summary

Wire the existing, well-tested Blueprint Engine components (ISchemaValidator, IJsonLogicEvaluator, IDisclosureProcessor, IRoutingEngine) into the Blueprint Service's ActionExecutionService, replacing stub/placeholder implementations. Additionally, implement Route-based routing in the Engine, add Fluent API builders for Routes and RejectionConfig, wire JsonLogicCache into the evaluator, implement ExecutionMode.ValidationOnly support, fix TransactionBuilderServiceExtensions stubs, implement full disclosure in the action submission endpoint, and add graph cycle detection for blueprint publish validation.

## Technical Context

**Language/Version**: C# 13 / .NET 10.0
**Primary Dependencies**: JsonSchema.Net 8.0.5, JsonLogic 5.5.0, JsonPath.Net 2.2.0, JsonE.Net 2.5.1, Microsoft.Extensions.Caching.Memory 10.0.2, FluentValidation 12.1.1
**Storage**: N/A (no new storage; uses existing in-memory stores)
**Testing**: xUnit 3.2.2, FluentAssertions 8.8.0, Moq 4.20.72
**Target Platform**: .NET 10 server + Blazor WASM (Engine is portable)
**Project Type**: Microservices (existing)
**Performance Goals**: JsonLogicCache should reduce repeated expression parsing; no new latency introduced
**Constraints**: Engine must remain WASM-compatible (no server-only dependencies); backward compatibility with legacy Condition-based routing
**Scale/Scope**: Changes span 4 existing projects, 2 new Fluent API files, ~15 modified files, ~100 new tests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Microservices-First | PASS | No new services; changes within existing service boundaries. Engine remains a portable library. |
| II. Security First | PASS | Disclosure processing enforces DAD model. Schema validation on all external boundaries. |
| III. API Documentation | PASS | No new public API endpoints; existing endpoints gain correct behavior. |
| IV. Testing Requirements | PASS | Target >85% coverage on changed files. All 319 existing tests must continue passing. |
| V. Code Quality | PASS | async/await, DI, nullable reference types, no warnings. |
| VI. Blueprint Creation Standards | PASS | Fluent API additions are for developer scenarios; JSON/YAML remains primary. |
| VII. Domain-Driven Design | PASS | Uses correct domain terms: Blueprint, Action, Participant, Disclosure. |
| VIII. Observability | PASS | Existing ActivitySource tracing in ActionExecutionService preserved. |

**Gate Result: PASS** -- No violations. Proceeding to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/018-blueprint-engine-integration/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (internal contracts)
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── Core/
│   ├── Sorcha.Blueprint.Engine/
│   │   ├── Implementation/
│   │   │   ├── RoutingEngine.cs          # MODIFY: Add Route-based routing
│   │   │   ├── ActionProcessor.cs        # MODIFY: Add ExecutionMode check
│   │   │   └── JsonLogicEvaluator.cs     # MODIFY: Wire JsonLogicCache
│   │   ├── Caching/
│   │   │   └── JsonLogicCache.cs         # EXISTING: Already implemented
│   │   └── Models/
│   │       └── RoutingResult.cs          # MODIFY: Add parallel branch support
│   └── Sorcha.Blueprint.Fluent/
│       ├── ActionBuilder.cs              # MODIFY: Add Route/Rejection methods
│       ├── RouteBuilder.cs               # NEW: Route configuration builder
│       └── RejectionConfigBuilder.cs     # NEW: Rejection configuration builder
├── Services/
│   └── Sorcha.Blueprint.Service/
│       ├── Services/
│       │   ├── Implementation/
│       │   │   └── ActionExecutionService.cs  # MODIFY: Wire Engine components
│       │   └── Interfaces/
│       │       └── ITransactionBuilderService.cs  # MODIFY: Fix stub extensions
│       └── Program.cs                    # MODIFY: Cycle detection + disclosure

tests/
├── Sorcha.Blueprint.Engine.Tests/
│   ├── RoutingEngineTests.cs             # MODIFY: Add Route-based tests
│   ├── ActionProcessorTests.cs           # MODIFY: Add ValidationOnly tests
│   └── JsonLogicCacheIntegrationTests.cs # NEW: Cache integration tests
├── Sorcha.Blueprint.Fluent.Tests/
│   ├── RouteBuilderTests.cs              # NEW: Route builder tests
│   └── RejectionConfigBuilderTests.cs    # NEW: Rejection builder tests
└── Sorcha.Blueprint.Service.Tests/
    ├── ActionExecutionServiceTests.cs    # MODIFY: Test engine delegation
    └── CycleDetectionTests.cs            # NEW: Graph cycle detection tests
```

**Structure Decision**: Existing microservices structure. No new projects -- only modifications to existing projects and new files within existing projects. Two new builder files in Fluent project, one new test file per project.

## Complexity Tracking

> No violations to justify. All changes fit within existing project structure.
