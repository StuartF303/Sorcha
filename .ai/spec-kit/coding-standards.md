# Coding Standards

**Version:** 1.0.0
**Status:** MANDATORY
**Audience:** All developers and AI assistants

---

## Overview

This document defines the coding standards and conventions for the Sorcha project. These standards ensure code consistency, readability, maintainability, and quality across the entire codebase.

---

## 1. C# Language Standards

### Target Framework & Language Version
- **Framework**: .NET 10 (net10.0)
- **Language**: C# 13
- **Nullable Reference Types**: ENABLED (mandatory)
- **Implicit Usings**: ENABLED

### Project Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <WarningsAsErrors>$(WarningsAsErrors);nullable</WarningsAsErrors>
  </PropertyGroup>
</Project>
```

---

## 2. Naming Conventions

### Casing Rules

| Element | Casing | Example |
|---------|--------|---------|
| Namespace | PascalCase | `Sorcha.Blueprint.Engine` |
| Class | PascalCase | `BlueprintExecutor` |
| Interface | PascalCase with 'I' prefix | `IBlueprintRepository` |
| Method | PascalCase | `ExecuteAsync()` |
| Property | PascalCase | `BlueprintId` |
| Field (private) | camelCase with '_' prefix | `_repository` |
| Field (const) | PascalCase | `MaxRetryCount` |
| Parameter | camelCase | `blueprintId` |
| Local Variable | camelCase | `executionResult` |
| Type Parameter | PascalCase with 'T' prefix | `TEntity`, `TKey` |

### Naming Rules

```csharp
// ✅ CORRECT: Descriptive, clear names
public class BlueprintExecutionService { }
public interface IBlueprintRepository { }
public async Task<Blueprint> GetBlueprintByIdAsync(string blueprintId) { }
private readonly IBlueprintRepository _blueprintRepository;

// ❌ WRONG: Abbreviations, unclear names
public class BpExecSvc { } // VIOLATION
public interface IRepo { } // VIOLATION
public async Task<Blueprint> GetAsync(string id) { } // Too generic
private readonly IBlueprintRepository repo; // Missing prefix
```

### Async Method Naming

```csharp
// ✅ CORRECT: Async suffix
public async Task<Blueprint> CreateBlueprintAsync(BlueprintRequest request) { }
public async Task DeleteBlueprintAsync(string id) { }

// ❌ WRONG: Missing Async suffix
public async Task<Blueprint> CreateBlueprint(BlueprintRequest request) { } // VIOLATION
```

### Boolean Names

```csharp
// ✅ CORRECT: Use positive, question format
public bool IsValid { get; set; }
public bool HasActions { get; set; }
public bool CanExecute { get; set; }
public bool ShouldNotify { get; set; }

// ❌ WRONG: Negative or unclear
public bool NotValid { get; set; } // VIOLATION
public bool Flag { get; set; } // VIOLATION
```

---

## 3. Code Organization

### File Structure

```csharp
// 1. File header (if required)
// 2. Usings (organized by System, then third-party, then project)
// 3. Namespace
// 4. Type definition

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Sorcha.Blueprint.Models;

namespace Sorcha.Blueprint.Engine;

/// <summary>
/// Executes blueprint workflows with validation and error handling.
/// </summary>
public class BlueprintExecutor : IBlueprintExecutor
{
    // 1. Private fields
    // 2. Constructor(s)
    // 3. Public properties
    // 4. Public methods
    // 5. Private methods

    private readonly ILogger<BlueprintExecutor> _logger;
    private readonly IBlueprintValidator _validator;

    public BlueprintExecutor(
        ILogger<BlueprintExecutor> logger,
        IBlueprintValidator validator)
    {
        _logger = logger;
        _validator = validator;
    }

    public string CurrentBlueprintId { get; private set; } = string.Empty;

    public async Task<ExecutionResult> ExecuteAsync(Blueprint blueprint)
    {
        ValidateBlueprint(blueprint);
        return await ExecuteInternalAsync(blueprint);
    }

    private void ValidateBlueprint(Blueprint blueprint)
    {
        // Implementation
    }

    private async Task<ExecutionResult> ExecuteInternalAsync(Blueprint blueprint)
    {
        // Implementation
        return await Task.FromResult(new ExecutionResult());
    }
}
```

### One Type Per File

```csharp
// ✅ CORRECT: Each class in separate file
// File: Blueprint.cs
public class Blueprint { }

// File: Action.cs
public class Action { }

// ❌ WRONG: Multiple types in one file
// File: Models.cs
public class Blueprint { }
public class Action { }
public class Participant { } // VIOLATION
```

**Exception**: Nested types and closely related helper types

---

## 4. Modern C# Features

### Records for DTOs and Value Objects

```csharp
// ✅ CORRECT: Use records for immutable data
public record Participant
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Organization { get; init; }
}

public record BlueprintRequest(
    string Title,
    string Description,
    List<Participant> Participants);
```

### Init-only Properties

```csharp
// ✅ CORRECT: Init for immutable after construction
public class Blueprint
{
    public required string Id { get; init; }
    public required string Title { get; set; } // Mutable
    public required DateTime CreatedAt { get; init; } // Immutable
}
```

### Required Members

```csharp
// ✅ CORRECT: Use required for mandatory properties
public class Action
{
    public required int Index { get; init; }
    public required string Title { get; set; }
    public string? Description { get; init; } // Optional
}

// Usage
var action = new Action
{
    Index = 1,
    Title = "Submit Order"
    // Description is optional
};
```

### Pattern Matching

```csharp
// ✅ CORRECT: Modern pattern matching
string GetStatusMessage(ExecutionResult result) => result switch
{
    { Success: true, Actions.Count: 0 } => "No actions executed",
    { Success: true } => $"Successfully executed {result.Actions.Count} actions",
    { Success: false, Error: not null } => $"Failed: {result.Error}",
    _ => "Unknown status"
};

// Type patterns
if (value is Blueprint { Actions.Count: > 0 } blueprint)
{
    ProcessBlueprint(blueprint);
}
```

### Null Handling

```csharp
// ✅ CORRECT: Null-coalescing and conditional operators
string name = participant.Name ?? "Unknown";
int actionCount = blueprint?.Actions?.Count ?? 0;
string? organizationName = participant?.Organization;

// Null-forgiving operator (use sparingly)
string id = context!.BlueprintId; // Only when you're certain it's not null

// ❌ WRONG: Explicit null checks everywhere
if (participant != null)
{
    if (participant.Name != null)
    {
        string name = participant.Name; // VERBOSE
    }
}
```

### Collection Expressions (C# 12+)

```csharp
// ✅ CORRECT: Collection expressions
List<string> ids = ["id1", "id2", "id3"];
string[] names = ["Alice", "Bob"];
int[] numbers = [1, 2, 3, 4, 5];

// Spread operator
List<string> combined = [..existingIds, "newId"];
```

### Primary Constructors (C# 12+)

```csharp
// ✅ CORRECT: Primary constructors for simple classes
public class BlueprintService(
    IBlueprintRepository repository,
    ILogger<BlueprintService> logger) : IBlueprintService
{
    public async Task<Blueprint> GetByIdAsync(string id)
    {
        logger.LogInformation("Fetching blueprint {Id}", id);
        return await repository.GetByIdAsync(id);
    }
}
```

---

## 5. Async/Await Best Practices

### Async All the Way

```csharp
// ✅ CORRECT: Async all the way down
public async Task<Blueprint> GetBlueprintAsync(string id)
{
    var blueprint = await _repository.GetByIdAsync(id);
    await ValidateAsync(blueprint);
    return blueprint;
}

// ❌ WRONG: Blocking on async code
public Blueprint GetBlueprint(string id)
{
    return _repository.GetByIdAsync(id).Result; // DEADLOCK RISK
}

// ❌ WRONG: Sync over async
public Blueprint GetBlueprint(string id)
{
    return _repository.GetByIdAsync(id).GetAwaiter().GetResult(); // VIOLATION
}
```

### ConfigureAwait

```csharp
// In library code (NOT in ASP.NET Core)
public async Task<Blueprint> GetBlueprintAsync(string id)
{
    var blueprint = await _repository
        .GetByIdAsync(id)
        .ConfigureAwait(false);

    return blueprint;
}

// In ASP.NET Core: ConfigureAwait(false) NOT needed
public async Task<IResult> GetBlueprint(string id)
{
    var blueprint = await _repository.GetByIdAsync(id);
    return Results.Ok(blueprint);
}
```

### ValueTask for Hot Paths

```csharp
// ✅ Use ValueTask when result might be synchronous
public ValueTask<Blueprint?> GetCachedBlueprintAsync(string id)
{
    if (_cache.TryGetValue(id, out var cached))
    {
        return ValueTask.FromResult(cached); // Synchronous
    }

    return new ValueTask<Blueprint?>(LoadFromDatabaseAsync(id)); // Async
}

private async Task<Blueprint?> LoadFromDatabaseAsync(string id)
{
    return await _repository.GetByIdAsync(id);
}
```

### Cancellation Tokens

```csharp
// ✅ CORRECT: Always accept CancellationToken
public async Task<Blueprint> ExecuteBlueprintAsync(
    string blueprintId,
    CancellationToken cancellationToken = default)
{
    var blueprint = await _repository
        .GetByIdAsync(blueprintId, cancellationToken);

    await _executor.ExecuteAsync(blueprint, cancellationToken);

    return blueprint;
}

// Pass through to all async calls
await Task.Delay(1000, cancellationToken);
```

---

## 6. LINQ Best Practices

### Readable LINQ

```csharp
// ✅ CORRECT: Clear, readable LINQ
var activeBlueprints = blueprints
    .Where(b => b.IsActive)
    .OrderBy(b => b.CreatedAt)
    .Take(10)
    .ToList();

// ✅ CORRECT: Method syntax for complex queries
var result = blueprints
    .Join(executions,
        b => b.Id,
        e => e.BlueprintId,
        (b, e) => new { Blueprint = b, Execution = e })
    .Where(x => x.Execution.Status == ExecutionStatus.Completed)
    .ToList();

// ❌ WRONG: Overly complex single-line LINQ
var result = blueprints.Where(b => b.IsActive).SelectMany(b => b.Actions).Where(a => a.Status == ActionStatus.Completed).GroupBy(a => a.ParticipantId).Select(g => new { ParticipantId = g.Key, Count = g.Count() }).ToList(); // VIOLATION
```

### Materialization

```csharp
// ✅ CORRECT: Materialize when needed
var activeBlueprints = await context.Blueprints
    .Where(b => b.IsActive)
    .ToListAsync(); // Materialize once

foreach (var blueprint in activeBlueprints)
{
    // Work with materialized list
}

// ❌ WRONG: Multiple enumerations
var query = context.Blueprints.Where(b => b.IsActive);
var count = query.Count(); // Query 1
var first = query.First(); // Query 2 - VIOLATION
```

### Async LINQ (Entity Framework)

```csharp
// ✅ CORRECT: Use async methods
var blueprints = await context.Blueprints
    .Where(b => b.IsActive)
    .ToListAsync();

var count = await context.Blueprints
    .CountAsync(b => b.IsActive);

// ❌ WRONG: Sync enumeration of IQueryable
var blueprints = context.Blueprints
    .Where(b => b.IsActive)
    .ToList(); // VIOLATION - Use ToListAsync()
```

---

## 7. Exception Handling

### Custom Exceptions

```csharp
// ✅ CORRECT: Domain-specific exceptions
public class BlueprintValidationException : Exception
{
    public BlueprintValidationException(string message)
        : base(message)
    {
    }

    public BlueprintValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
```

### Exception Handling Patterns

```csharp
// ✅ CORRECT: Specific exceptions first
try
{
    await ExecuteBlueprintAsync(blueprintId);
}
catch (BlueprintValidationException ex)
{
    _logger.LogWarning(ex, "Blueprint validation failed");
    throw; // Re-throw to preserve stack trace
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error executing blueprint");
    throw; // Or wrap in new exception
}

// ❌ WRONG: Catching Exception first
try
{
    await ExecuteBlueprintAsync(blueprintId);
}
catch (Exception ex) // VIOLATION - Too broad
{
    // Specific exceptions never reached
}

// ❌ WRONG: Swallowing exceptions
try
{
    await ExecuteBlueprintAsync(blueprintId);
}
catch
{
    // VIOLATION - Exception lost
}
```

### Guard Clauses

```csharp
// ✅ CORRECT: Guard clauses at start of method
public async Task<Blueprint> GetBlueprintAsync(string blueprintId)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(blueprintId);

    var blueprint = await _repository.GetByIdAsync(blueprintId);

    if (blueprint == null)
    {
        throw new NotFoundException($"Blueprint {blueprintId} not found");
    }

    return blueprint;
}

// Using ArgumentNullException.ThrowIfNull (C# 11+)
public void ProcessBlueprint(Blueprint blueprint)
{
    ArgumentNullException.ThrowIfNull(blueprint);

    // Process blueprint
}
```

---

## 8. Documentation Standards

### XML Documentation

```csharp
/// <summary>
/// Executes a blueprint workflow with the specified execution context.
/// </summary>
/// <param name="blueprint">The blueprint to execute.</param>
/// <param name="context">The execution context containing runtime parameters.</param>
/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
/// <returns>The execution result containing status and output data.</returns>
/// <exception cref="BlueprintValidationException">
/// Thrown when the blueprint fails validation.
/// </exception>
/// <exception cref="ExecutionException">
/// Thrown when execution fails due to runtime errors.
/// </exception>
/// <remarks>
/// This method validates the blueprint before execution and applies
/// all configured disclosure and routing rules during execution.
/// </remarks>
public async Task<ExecutionResult> ExecuteAsync(
    Blueprint blueprint,
    ExecutionContext context,
    CancellationToken cancellationToken = default)
{
    // Implementation
}
```

### Required Documentation

- ✅ All public types (classes, interfaces, enums)
- ✅ All public members (methods, properties, events)
- ✅ All parameters with `<param>`
- ✅ Return values with `<returns>`
- ✅ Exceptions with `<exception>`
- ✅ Complex logic with `<remarks>`

### Code Comments

```csharp
// ✅ CORRECT: Explain WHY, not WHAT
// Apply disclosure rules to ensure participants only see authorized data
var visibleData = ApplyDisclosureRules(data, participant);

// Add retry logic because external API is occasionally unstable
await RetryAsync(() => CallExternalApiAsync());

// ❌ WRONG: Obvious comments
// Get the blueprint by ID
var blueprint = await GetByIdAsync(id); // VIOLATION

// Loop through actions
foreach (var action in blueprint.Actions) // VIOLATION
{
}
```

---

## 9. Performance Best Practices

### String Handling

```csharp
// ✅ CORRECT: StringBuilder for concatenation
var builder = new StringBuilder();
foreach (var action in blueprint.Actions)
{
    builder.AppendLine($"Action {action.Index}: {action.Title}");
}
string result = builder.ToString();

// ❌ WRONG: String concatenation in loop
string result = "";
foreach (var action in blueprint.Actions)
{
    result += $"Action {action.Index}: {action.Title}\n"; // VIOLATION
}
```

### Collection Pre-allocation

```csharp
// ✅ CORRECT: Pre-allocate known capacity
var results = new List<ExecutionResult>(blueprint.Actions.Count);
foreach (var action in blueprint.Actions)
{
    results.Add(await ExecuteActionAsync(action));
}

// ❌ WRONG: Default capacity with many items
var results = new List<ExecutionResult>(); // Will resize multiple times
```

### Span<T> for Performance-Critical Code

```csharp
// ✅ CORRECT: Span for stack-allocated arrays
Span<int> numbers = stackalloc int[10];
for (int i = 0; i < numbers.Length; i++)
{
    numbers[i] = i;
}

// ✅ ReadOnlySpan for slicing
ReadOnlySpan<char> trimmed = input.AsSpan().Trim();
```

---

## 10. Testing Standards

### Test Method Naming

```csharp
// ✅ CORRECT: MethodName_StateUnderTest_ExpectedBehavior
[Fact]
public async Task ExecuteAsync_ValidBlueprint_ReturnsSuccess()
{
    // Arrange
    var blueprint = CreateValidBlueprint();

    // Act
    var result = await _executor.ExecuteAsync(blueprint);

    // Assert
    result.Success.Should().BeTrue();
}

[Fact]
public async Task ExecuteAsync_InvalidBlueprint_ThrowsValidationException()
{
    // Arrange
    var invalidBlueprint = CreateInvalidBlueprint();

    // Act & Assert
    await Assert.ThrowsAsync<BlueprintValidationException>(
        () => _executor.ExecuteAsync(invalidBlueprint));
}
```

### Arrange-Act-Assert Pattern

```csharp
[Fact]
public async Task GetByIdAsync_ExistingId_ReturnsBlueprint()
{
    // Arrange
    var blueprintId = "test-id";
    var expected = new Blueprint { Id = blueprintId, Title = "Test" };
    _mockRepository
        .Setup(r => r.GetByIdAsync(blueprintId))
        .ReturnsAsync(expected);

    // Act
    var actual = await _service.GetByIdAsync(blueprintId);

    // Assert
    actual.Should().NotBeNull();
    actual.Id.Should().Be(blueprintId);
    actual.Title.Should().Be("Test");
}
```

---

## 11. Code Quality Tools

### Static Analysis

```bash
# Run before committing
dotnet build -warnaserror
dotnet format --verify-no-changes
```

### Code Coverage

```bash
# Required: 80% minimum for Core projects
dotnet test --collect:"XPlat Code Coverage"
```

### Required Analyzers

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.CodeAnalysis.NetAnalyzers" Version="9.0.0" />
  <PackageReference Include="StyleCop.Analyzers" Version="1.2.0-beta.556" />
</ItemGroup>
```

---

## 12. Anti-Patterns to Avoid

### ❌ Magic Numbers

```csharp
// WRONG
if (blueprint.Actions.Count > 100) { } // What is 100?

// CORRECT
private const int MaxActionsPerBlueprint = 100;
if (blueprint.Actions.Count > MaxActionsPerBlueprint) { }
```

### ❌ Large Methods

```csharp
// WRONG: Method > 50 lines
public async Task<ExecutionResult> ExecuteAsync(Blueprint blueprint)
{
    // 150 lines of code... VIOLATION
}

// CORRECT: Extract to smaller methods
public async Task<ExecutionResult> ExecuteAsync(Blueprint blueprint)
{
    ValidateBlueprint(blueprint);
    var context = CreateExecutionContext(blueprint);
    return await ExecuteActionsAsync(context);
}
```

### ❌ God Classes

```csharp
// WRONG: Class with > 20 methods or > 500 lines
public class BlueprintManager // VIOLATION
{
    // 50 methods, 1000 lines
}

// CORRECT: Split responsibilities
public class BlueprintValidator { }
public class BlueprintExecutor { }
public class BlueprintRepository { }
```

---

## Compliance Checklist

Before committing code:

- [ ] Nullable reference types enabled
- [ ] All public APIs have XML documentation
- [ ] Async methods have 'Async' suffix
- [ ] No compiler warnings
- [ ] Naming conventions followed
- [ ] Modern C# features used appropriately
- [ ] LINQ queries are readable
- [ ] Exceptions handled properly
- [ ] Tests follow naming conventions
- [ ] Code coverage meets minimum (80%)

---

## References

- [Spec-Kit Main](./spec-kit.md)
- [C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [Framework Design Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/)
- [Async Best Practices](https://learn.microsoft.com/en-us/archive/msdn-magazine/2013/march/async-await-best-practices-in-asynchronous-programming)
