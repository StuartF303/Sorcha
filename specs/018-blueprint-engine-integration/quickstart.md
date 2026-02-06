# Quickstart: Blueprint Engine Integration

**Feature**: 018-blueprint-engine-integration
**Date**: 2026-02-06

## Prerequisites

- .NET 10 SDK
- Existing Sorcha solution builds successfully (`dotnet build`)
- All 319 Blueprint Engine tests pass (`dotnet test --filter "FullyQualifiedName~Blueprint.Engine"`)

## Implementation Order

Work should proceed in this order to minimize inter-task dependencies:

### Phase A: Engine Core Changes (no service dependencies)

1. **Extend RoutingResult** for parallel branch support (`RoutingResult.cs`)
2. **Implement Route-based routing** in `RoutingEngine.cs`
3. **Add ExecutionMode check** in `ActionProcessor.cs`
4. **Wire JsonLogicCache** into `JsonLogicEvaluator.cs`
5. **Write Engine tests** for all of the above

### Phase B: Fluent API (depends on Phase A models)

6. **Create RouteBuilder** (`RouteBuilder.cs`)
7. **Create RejectionConfigBuilder** (`RejectionConfigBuilder.cs`)
8. **Extend ActionBuilder** with new methods
9. **Write Fluent tests** for all builders

### Phase C: Service Integration (depends on Phase A)

10. **Wire IExecutionEngine** into ActionExecutionService
11. **Fix TransactionBuilderServiceExtensions** stubs
12. **Implement full disclosure** in POST /api/actions endpoint
13. **Implement cycle detection** in publish validation
14. **Register JsonLogicCache** in DI (Program.cs)
15. **Write Service tests** for integration and cycle detection

## Verification

After each phase, run the relevant test suite:

```bash
# Phase A: Engine tests (should be ~340+ after new tests)
dotnet test tests/Sorcha.Blueprint.Engine.Tests

# Phase B: Fluent tests (should be ~50+ after new tests)
dotnet test tests/Sorcha.Blueprint.Fluent.Tests

# Phase C: Service tests (should be ~220+ after new tests)
dotnet test tests/Sorcha.Blueprint.Service.Tests

# Full suite: all tests must pass
dotnet test
```

## Key Files Quick Reference

| File | Change Type | Purpose |
|------|------------|---------|
| `src/Core/Sorcha.Blueprint.Engine/Models/RoutingResult.cs` | MODIFY | Add NextActions list, IsParallel, RoutedAction |
| `src/Core/Sorcha.Blueprint.Engine/Implementation/RoutingEngine.cs` | MODIFY | Route-based routing + legacy fallback |
| `src/Core/Sorcha.Blueprint.Engine/Implementation/ActionProcessor.cs` | MODIFY | ExecutionMode.ValidationOnly check |
| `src/Core/Sorcha.Blueprint.Engine/Implementation/JsonLogicEvaluator.cs` | MODIFY | Wire JsonLogicCache |
| `src/Core/Sorcha.Blueprint.Fluent/RouteBuilder.cs` | NEW | Route fluent builder |
| `src/Core/Sorcha.Blueprint.Fluent/RejectionConfigBuilder.cs` | NEW | Rejection fluent builder |
| `src/Core/Sorcha.Blueprint.Fluent/ActionBuilder.cs` | MODIFY | Add Route/Rejection/Starting methods |
| `src/Services/Sorcha.Blueprint.Service/Services/Implementation/ActionExecutionService.cs` | MODIFY | Wire Engine components |
| `src/Services/Sorcha.Blueprint.Service/Services/Interfaces/ITransactionBuilderService.cs` | MODIFY | Fix stub extensions |
| `src/Services/Sorcha.Blueprint.Service/Program.cs` | MODIFY | Cycle detection + disclosure + DI |
