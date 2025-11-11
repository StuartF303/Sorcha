# Testing Requirements

**Version:** 1.0.0
**Status:** MANDATORY
**Audience:** All developers and AI assistants

---

## Overview

This document defines the testing standards, strategies, and requirements for the Sorcha project. Comprehensive testing ensures code quality, reliability, and maintainability.

---

## 1. Testing Pyramid

### Test Distribution

```
        ┌──────────────────┐
        │   E2E Tests      │  10%  - Full system workflows
        │   (100 tests)    │
        ├──────────────────┤
        │ Integration Tests│  30%  - Component interactions
        │   (300 tests)    │
        ├──────────────────┤
        │   Unit Tests     │  60%  - Individual components
        │   (600 tests)    │
        └──────────────────┘
```

### Test Types

| Type | Purpose | Scope | Speed | Coverage Target |
|------|---------|-------|-------|-----------------|
| **Unit** | Test individual methods/classes | Single unit in isolation | Fast (<1ms) | 80%+ for Core |
| **Integration** | Test component interactions | Multiple units, real dependencies | Medium (10-100ms) | 70%+ for APIs |
| **E2E** | Test user workflows | Full system | Slow (seconds) | Critical paths |
| **Performance** | Test scalability and speed | Full system under load | Varies | Key scenarios |

---

## 2. Unit Testing

### ✅ REQUIRED: Test Structure

```csharp
// Test class naming: {ClassUnderTest}Tests
public class BlueprintExecutorTests
{
    private readonly BlueprintExecutor _executor;
    private readonly Mock<IBlueprintRepository> _mockRepository;
    private readonly Mock<IValidator<Blueprint>> _mockValidator;
    private readonly Mock<ILogger<BlueprintExecutor>> _mockLogger;

    public BlueprintExecutorTests()
    {
        // Arrange: Setup common dependencies
        _mockRepository = new Mock<IBlueprintRepository>();
        _mockValidator = new Mock<IValidator<Blueprint>>();
        _mockLogger = new Mock<ILogger<BlueprintExecutor>>();

        _executor = new BlueprintExecutor(
            _mockRepository.Object,
            _mockValidator.Object,
            _mockLogger.Object);
    }

    // Test naming: MethodName_StateUnderTest_ExpectedBehavior
    [Fact]
    public async Task ExecuteAsync_ValidBlueprint_ReturnsSuccessResult()
    {
        // Arrange
        var blueprint = CreateValidBlueprint();
        _mockValidator
            .Setup(v => v.ValidateAsync(blueprint, default))
            .ReturnsAsync(ValidationResult.Success);

        // Act
        var result = await _executor.ExecuteAsync(blueprint);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_InvalidBlueprint_ThrowsValidationException()
    {
        // Arrange
        var invalidBlueprint = CreateInvalidBlueprint();
        var validationFailure = new ValidationResult(new[]
        {
            new ValidationFailure("Title", "Title is required")
        });
        _mockValidator
            .Setup(v => v.ValidateAsync(invalidBlueprint, default))
            .ReturnsAsync(validationFailure);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BlueprintValidationException>(
            () => _executor.ExecuteAsync(invalidBlueprint));

        exception.Message.Should().Contain("Title is required");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task ExecuteAsync_InvalidActionCount_ThrowsException(int actionCount)
    {
        // Arrange
        var blueprint = CreateBlueprintWithActionCount(actionCount);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _executor.ExecuteAsync(blueprint));
    }

    // Helper methods
    private Blueprint CreateValidBlueprint() => new()
    {
        Id = "test-blueprint-1",
        Title = "Test Blueprint",
        Description = "Test description",
        Actions = new List<Action>
        {
            new() { Index = 0, Title = "Action 1" }
        }
    };

    private Blueprint CreateInvalidBlueprint() => new()
    {
        Id = "test-blueprint-2",
        Title = "", // Invalid: empty title
        Actions = new List<Action>()
    };

    private Blueprint CreateBlueprintWithActionCount(int count) => new()
    {
        Id = "test-blueprint-3",
        Title = "Test Blueprint",
        Actions = Enumerable.Range(0, count)
            .Select(i => new Action { Index = i, Title = $"Action {i}" })
            .ToList()
    };
}
```

### Test Organization

```
tests/
├── Sorcha.Blueprint.Models.Tests/
│   ├── BlueprintTests.cs
│   ├── ActionTests.cs
│   └── ParticipantTests.cs
├── Sorcha.Blueprint.Engine.Tests/
│   ├── BlueprintExecutorTests.cs
│   ├── ValidationServiceTests.cs
│   └── DisclosureServiceTests.cs
└── Sorcha.Blueprint.Fluent.Tests/
    ├── BlueprintBuilderTests.cs
    └── ActionBuilderTests.cs
```

### ✅ Required Patterns

#### Arrange-Act-Assert (AAA)

```csharp
[Fact]
public async Task GetByIdAsync_ExistingId_ReturnsBlueprint()
{
    // Arrange: Set up test data and mocks
    var blueprintId = "test-123";
    var expected = new Blueprint { Id = blueprintId, Title = "Test" };
    _mockRepository
        .Setup(r => r.GetByIdAsync(blueprintId))
        .ReturnsAsync(expected);

    // Act: Execute the method under test
    var actual = await _service.GetByIdAsync(blueprintId);

    // Assert: Verify the results
    actual.Should().NotBeNull();
    actual.Id.Should().Be(blueprintId);
    actual.Title.Should().Be("Test");
}
```

#### FluentAssertions

```csharp
// ✅ CORRECT: Use FluentAssertions
result.Should().NotBeNull();
result.Success.Should().BeTrue();
result.Errors.Should().BeEmpty();
result.Actions.Should().HaveCount(3);
result.Actions.Should().Contain(a => a.Index == 1);

// ❌ WRONG: Traditional assertions (less readable)
Assert.NotNull(result);
Assert.True(result.Success);
Assert.Empty(result.Errors);
Assert.Equal(3, result.Actions.Count);
```

#### Test Data Builders

```csharp
// ✅ CORRECT: Test data builders
public class BlueprintTestBuilder
{
    private string _id = "test-blueprint";
    private string _title = "Test Blueprint";
    private List<Action> _actions = new();

    public BlueprintTestBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public BlueprintTestBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public BlueprintTestBuilder WithAction(Action action)
    {
        _actions.Add(action);
        return this;
    }

    public Blueprint Build() => new()
    {
        Id = _id,
        Title = _title,
        Actions = _actions
    };
}

// Usage
var blueprint = new BlueprintTestBuilder()
    .WithId("bp-001")
    .WithTitle("Purchase Order")
    .WithAction(new Action { Index = 0, Title = "Submit" })
    .WithAction(new Action { Index = 1, Title = "Approve" })
    .Build();
```

### What to Test

#### ✅ DO Test

- Public methods and properties
- Business logic and validations
- Edge cases and boundary conditions
- Error handling and exceptions
- State changes and side effects

#### ❌ DON'T Test

- Private methods (test through public API)
- Framework code (e.g., ASP.NET Core)
- Third-party libraries
- Trivial getters/setters
- Generated code

---

## 3. Integration Testing

### ✅ REQUIRED: API Integration Tests

```csharp
public class BlueprintApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public BlueprintApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetBlueprints_ReturnsOkWithBlueprints()
    {
        // Act
        var response = await _client.GetAsync("/api/blueprints");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var blueprints = await response.Content
            .ReadFromJsonAsync<List<Blueprint>>();

        blueprints.Should().NotBeNull();
        blueprints.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateBlueprint_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new BlueprintRequest(
            Title: "Test Blueprint",
            Description: "Integration test",
            Participants: new List<Participant>
            {
                new() { Id = "p1", Name = "Participant 1" }
            });

        // Act
        var response = await _client.PostAsJsonAsync("/api/blueprints", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var blueprint = await response.Content.ReadFromJsonAsync<Blueprint>();
        blueprint.Should().NotBeNull();
        blueprint!.Title.Should().Be("Test Blueprint");
    }

    [Fact]
    public async Task CreateBlueprint_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var invalidRequest = new BlueprintRequest(
            Title: "", // Invalid: empty title
            Description: "Test",
            Participants: new List<Participant>());

        // Act
        var response = await _client.PostAsJsonAsync("/api/blueprints", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }
}
```

### Test Fixtures and Shared Context

```csharp
// Custom WebApplicationFactory for testing
public class SorchaWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove real database
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add in-memory database
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDb");
            });

            // Replace external services with test doubles
            services.AddSingleton<IEmailService, FakeEmailService>();
        });
    }
}
```

### Database Integration Tests

```csharp
public class BlueprintRepositoryIntegrationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly BlueprintRepository _repository;

    public BlueprintRepositoryIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new BlueprintRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ValidBlueprint_PersistsToDatabase()
    {
        // Arrange
        var blueprint = new Blueprint
        {
            Id = "bp-001",
            Title = "Test Blueprint"
        };

        // Act
        await _repository.AddAsync(blueprint);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _repository.GetByIdAsync("bp-001");
        saved.Should().NotBeNull();
        saved!.Title.Should().Be("Test Blueprint");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
```

---

## 4. End-to-End (E2E) Testing

### ✅ Playwright E2E Tests

```csharp
[TestClass]
public class BlueprintDesignerE2ETests : PageTest
{
    [TestMethod]
    public async Task CreateBlueprint_CompleteWorkflow_Success()
    {
        // Navigate to designer
        await Page.GotoAsync("https://localhost:5001/designer");

        // Create new blueprint
        await Page.ClickAsync("button:has-text('New Blueprint')");

        // Fill in blueprint details
        await Page.FillAsync("input[name='title']", "E2E Test Blueprint");
        await Page.FillAsync("textarea[name='description']", "Created via E2E test");

        // Add participant
        await Page.ClickAsync("button:has-text('Add Participant')");
        await Page.FillAsync("input[name='participantName']", "Test Participant");
        await Page.ClickAsync("button:has-text('Save Participant')");

        // Add action
        await Page.ClickAsync("button:has-text('Add Action')");
        await Page.FillAsync("input[name='actionTitle']", "Submit Order");
        await Page.SelectOptionAsync("select[name='sentBy']", "Test Participant");
        await Page.ClickAsync("button:has-text('Save Action')");

        // Save blueprint
        await Page.ClickAsync("button:has-text('Save Blueprint')");

        // Assert success message
        var successMessage = await Page.TextContentAsync(".alert-success");
        successMessage.Should().Contain("Blueprint saved successfully");

        // Verify redirect to blueprint list
        await Expect(Page).ToHaveURLAsync(new Regex("/blueprints$"));
    }

    [TestMethod]
    public async Task ExecuteBlueprint_ValidBlueprint_ShowsResults()
    {
        // Navigate to blueprint
        await Page.GotoAsync("https://localhost:5001/blueprints/test-blueprint-1");

        // Execute blueprint
        await Page.ClickAsync("button:has-text('Execute')");

        // Wait for execution to complete
        await Page.WaitForSelectorAsync(".execution-complete");

        // Verify results
        var status = await Page.TextContentAsync(".execution-status");
        status.Should().Contain("Success");

        var actionCount = await Page.Locator(".action-result").CountAsync();
        actionCount.Should().BeGreaterThan(0);
    }
}
```

---

## 5. Performance Testing

### Load Testing with NBomber

```csharp
public class BlueprintApiPerformanceTests
{
    [Fact]
    public void GetBlueprintsEndpoint_Under1000RPS_MeetsLatencyRequirements()
    {
        var scenario = Scenario.Create("get_blueprints", async context =>
        {
            var client = new HttpClient();
            var response = await client.GetAsync("https://localhost:8080/api/blueprints");

            return response.IsSuccessStatusCode
                ? Response.Ok()
                : Response.Fail();
        })
        .WithLoadSimulations(
            Simulation.RampingInject(
                rate: 1000,
                interval: TimeSpan.FromSeconds(1),
                during: TimeSpan.FromMinutes(5))
        );

        var stats = NBomberRunner
            .RegisterScenarios(scenario)
            .Run();

        // Assert latency requirements
        var percentile99 = stats.ScenarioStats[0]
            .StepStats[0]
            .Ok.Latency.Percent99;

        percentile99.Should().BeLessThan(200); // < 200ms at 99th percentile
    }
}
```

### Benchmark Tests with BenchmarkDotNet

```csharp
[MemoryDiagnoser]
public class BlueprintExecutionBenchmarks
{
    private Blueprint _blueprint = null!;
    private BlueprintExecutor _executor = null!;

    [GlobalSetup]
    public void Setup()
    {
        _blueprint = CreateBlueprintWith100Actions();
        _executor = new BlueprintExecutor(
            Mock.Of<IBlueprintRepository>(),
            Mock.Of<IValidator<Blueprint>>(),
            Mock.Of<ILogger<BlueprintExecutor>>());
    }

    [Benchmark]
    public async Task<ExecutionResult> ExecuteBlueprint()
    {
        return await _executor.ExecuteAsync(_blueprint);
    }

    [Benchmark]
    public async Task<ValidationResult> ValidateBlueprint()
    {
        return await _executor.ValidateAsync(_blueprint);
    }

    private Blueprint CreateBlueprintWith100Actions()
    {
        // Create test blueprint
        return new Blueprint();
    }
}
```

---

## 6. Test Coverage Requirements

### Minimum Coverage

| Project Type | Minimum Coverage | Target Coverage |
|--------------|------------------|-----------------|
| **Core (Models, Engine, Fluent)** | 80% | 90% |
| **API Services** | 70% | 80% |
| **UI (Blazor)** | 60% | 70% |
| **Infrastructure** | 50% | 60% |

### Measuring Coverage

```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate coverage report
reportgenerator \
    -reports:"**/coverage.cobertura.xml" \
    -targetdir:"coverage-report" \
    -reporttypes:Html

# View report
open coverage-report/index.html
```

### Coverage Configuration

```xml
<!-- Directory.Build.props -->
<PropertyGroup>
  <CoverletOutputFormat>cobertura</CoverletOutputFormat>
  <ExcludeByAttribute>GeneratedCodeAttribute,ExcludeFromCodeCoverage</ExcludeByAttribute>
  <ExcludeByFile>**/Migrations/*.cs</ExcludeByFile>
</PropertyGroup>
```

---

## 7. Test Isolation and Determinism

### ✅ REQUIRED: Isolated Tests

```csharp
// ✅ CORRECT: Each test is independent
[Fact]
public async Task Test1_IndependentState()
{
    var blueprint = CreateBlueprint("bp-1");
    var result = await _executor.ExecuteAsync(blueprint);
    result.Success.Should().BeTrue();
}

[Fact]
public async Task Test2_IndependentState()
{
    var blueprint = CreateBlueprint("bp-2");
    var result = await _executor.ExecuteAsync(blueprint);
    result.Success.Should().BeTrue();
}

// ❌ WRONG: Tests depend on shared state
private static int _counter = 0; // VIOLATION - Shared state

[Fact]
public void Test1_SharedState()
{
    _counter++;
    _counter.Should().Be(1); // Fails if Test2 runs first
}

[Fact]
public void Test2_SharedState()
{
    _counter++;
    _counter.Should().Be(1); // Fails if Test1 runs first
}
```

### Deterministic Tests

```csharp
// ✅ CORRECT: Deterministic datetime
public class TimeProviderTests
{
    private readonly FakeTimeProvider _timeProvider;

    public TimeProviderTests()
    {
        _timeProvider = new FakeTimeProvider();
        _timeProvider.SetUtcNow(new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void GetCurrentTime_ReturnsFakeTime()
    {
        var time = _timeProvider.GetUtcNow();
        time.Should().Be(new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc));
    }
}

// ❌ WRONG: Non-deterministic datetime
[Fact]
public void CreateBlueprint_SetsCreatedDate()
{
    var blueprint = new Blueprint();
    blueprint.CreatedAt.Should().Be(DateTime.UtcNow); // VIOLATION - Flaky
}
```

---

## 8. Mocking Best Practices

### Mock Setup

```csharp
// ✅ CORRECT: Explicit mock setup
_mockRepository
    .Setup(r => r.GetByIdAsync("bp-001"))
    .ReturnsAsync(new Blueprint { Id = "bp-001", Title = "Test" });

_mockRepository
    .Setup(r => r.GetByIdAsync("bp-002"))
    .ThrowsAsync(new NotFoundException());

// Verify mock interactions
_mockRepository.Verify(
    r => r.GetByIdAsync("bp-001"),
    Times.Once);

// ❌ WRONG: Over-mocking
_mockLogger.Setup(l => l.LogInformation(It.IsAny<string>())); // Unnecessary
```

### Avoid Over-Mocking

```csharp
// ✅ CORRECT: Use real objects when simple
var blueprint = new Blueprint
{
    Id = "bp-001",
    Title = "Test"
};

// ❌ WRONG: Mocking simple objects
var mockBlueprint = new Mock<IBlueprint>();
mockBlueprint.Setup(b => b.Id).Returns("bp-001");
mockBlueprint.Setup(b => b.Title).Returns("Test");
```

---

## 9. CI/CD Integration

### GitHub Actions Test Workflow

```yaml
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore

      - name: Run unit tests
        run: dotnet test --no-build --verbosity normal --filter "Category=Unit"

      - name: Run integration tests
        run: dotnet test --no-build --verbosity normal --filter "Category=Integration"

      - name: Generate coverage report
        run: |
          dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
          reportgenerator -reports:./coverage/**/coverage.cobertura.xml -targetdir:./coverage-report -reporttypes:Html

      - name: Upload coverage
        uses: codecov/codecov-action@v3
        with:
          files: ./coverage/**/coverage.cobertura.xml
```

---

## 10. Test Categories

### Category Attributes

```csharp
[Trait("Category", "Unit")]
public class BlueprintExecutorTests { }

[Trait("Category", "Integration")]
public class BlueprintApiIntegrationTests { }

[Trait("Category", "E2E")]
public class BlueprintDesignerE2ETests { }

[Trait("Category", "Performance")]
public class BlueprintPerformanceTests { }
```

### Run Specific Categories

```bash
# Run only unit tests
dotnet test --filter "Category=Unit"

# Run integration and E2E tests
dotnet test --filter "Category=Integration|Category=E2E"

# Exclude performance tests
dotnet test --filter "Category!=Performance"
```

---

## Testing Checklist

Before committing code:

- [ ] Unit tests written for all business logic
- [ ] Integration tests for API endpoints
- [ ] Tests follow Arrange-Act-Assert pattern
- [ ] Test naming convention followed
- [ ] FluentAssertions used for readability
- [ ] Tests are isolated and deterministic
- [ ] Mocks used appropriately (not over-mocked)
- [ ] Code coverage meets minimum requirements
- [ ] All tests pass locally
- [ ] No flaky or intermittent failures

---

## References

- [Spec-Kit Main](./spec-kit.md)
- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Moq Documentation](https://github.com/moq/moq4)
- [Testing ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/)
