# Moq Workflows Reference

## Contents
- Test Class Setup Workflow
- Integration Test Mocking
- Repository Helper Pattern
- Testing Async Operations
- Workflow Checklists

---

## Test Class Setup Workflow

### Standard Test Class Structure

```csharp
// From tests/Sorcha.Register.Service.Tests/Unit/RegisterCreationOrchestratorTests.cs
public class RegisterCreationOrchestratorTests
{
    // 1. Declare mocks as private readonly fields
    private readonly Mock<ILogger<RegisterCreationOrchestrator>> _mockLogger;
    private readonly Mock<RegisterManager> _mockRegisterManager;
    private readonly Mock<IWalletServiceClient> _mockWalletClient;
    private readonly RegisterCreationOrchestrator _orchestrator;

    public RegisterCreationOrchestratorTests()
    {
        // 2. Initialize mocks
        _mockLogger = new Mock<ILogger<RegisterCreationOrchestrator>>();
        _mockRegisterManager = new Mock<RegisterManager>(
            Mock.Of<IRegisterRepository>(),
            Mock.Of<IEventPublisher>(),
            Mock.Of<ILogger<RegisterManager>>());
        _mockWalletClient = new Mock<IWalletServiceClient>();

        // 3. Create SUT with mock dependencies
        _orchestrator = new RegisterCreationOrchestrator(
            _mockLogger.Object,
            _mockRegisterManager.Object,
            _mockWalletClient.Object);
    }

    [Fact]
    public async Task Method_Scenario_ExpectedBehavior()
    {
        // 4. Arrange - Setup specific mock behavior for this test
        _mockWalletClient.Setup(...).ReturnsAsync(...);

        // 5. Act
        var result = await _orchestrator.InitiateAsync(request);

        // 6. Assert - Verify interactions
        _mockWalletClient.Verify(..., Times.Once);
    }
}
```

### Workflow Checklist

Copy this checklist when creating a new test class:

- [ ] Declare mocks as `private readonly Mock<T>` fields
- [ ] Initialize mocks in constructor
- [ ] Use `Mock.Of<T>()` for loggers and simple deps
- [ ] Create SUT with `.Object` properties
- [ ] Add `SetupDefault*()` helper for common setups
- [ ] Implement `IDisposable` if using real resources

---

## Integration Test Mocking

### WebApplicationFactory Pattern

```csharp
// From tests/Sorcha.Tenant.Service.Tests/Infrastructure/TenantServiceWebApplicationFactory.cs
public class TenantServiceWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Replace real DbContext with InMemory
            services.RemoveAll<TenantDbContext>();
            services.AddDbContext<TenantDbContext>(options =>
                options.UseInMemoryDatabase("TenantServiceTests"));

            // Mock external services (Redis)
            var mockRedis = new Mock<IConnectionMultiplexer>();
            var mockDatabase = new Mock<IDatabase>();

            mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(mockDatabase.Object);

            mockDatabase.Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);

            services.AddSingleton(mockRedis.Object);
        });
    }
}
```

### Usage in Tests

```csharp
public class TenantIntegrationTests : IClassFixture<TenantServiceWebApplicationFactory>
{
    private readonly TenantServiceWebApplicationFactory _factory;

    public TenantIntegrationTests(TenantServiceWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateTenant_ReturnsCreated()
    {
        await _factory.SeedTestDataAsync();
        var client = _factory.CreateAdminClient();

        var response = await client.PostAsJsonAsync("/api/tenants", new { Name = "Test" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
```

---

## Repository Helper Pattern

### Private Helper for Complex Mock Setup

```csharp
// From tests/Sorcha.Register.Service.Tests/Unit/MongoSystemRegisterRepositoryTests.cs
private MongoSystemRegisterRepository CreateRepository()
{
    var mongoClientMock = new Mock<IMongoClient>();
    var mongoDatabaseMock = new Mock<IMongoDatabase>();
    var mongoCollectionMock = new Mock<IMongoCollection<SystemRegisterEntry>>();
    var indexManagerMock = new Mock<IMongoIndexManager<SystemRegisterEntry>>();

    mongoClientMock
        .Setup(c => c.GetDatabase(It.IsAny<string>(), It.IsAny<MongoDatabaseSettings>()))
        .Returns(mongoDatabaseMock.Object);

    mongoDatabaseMock
        .Setup(d => d.GetCollection<SystemRegisterEntry>(It.IsAny<string>(), It.IsAny<MongoCollectionSettings>()))
        .Returns(mongoCollectionMock.Object);

    mongoCollectionMock.Setup(c => c.Indexes).Returns(indexManagerMock.Object);

    indexManagerMock
        .Setup(i => i.CreateOneAsync(It.IsAny<CreateIndexModel<SystemRegisterEntry>>(), ...))
        .ReturnsAsync("mock_index");

    return new MongoSystemRegisterRepository(mongoClientMock.Object, "test_db", _loggerMock.Object);
}
```

---

## Testing Async Operations

### Setup Helper for Crypto Operations

```csharp
// From tests/Sorcha.Wallet.Service.Tests/Services/DelegationServiceTests.cs
private void SetupCryptoMocks(Mock<ICryptoModule> mockCrypto, Mock<IWalletUtilities> mockUtilities)
{
    mockCrypto
        .Setup(x => x.GenerateKeySetAsync(
            It.IsAny<WalletNetworks>(),
            It.IsAny<byte[]>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync((WalletNetworks network, byte[] seed, CancellationToken ct) =>
        {
            var privateKey = new byte[32];
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            Array.Copy(sha256.ComputeHash(seed), privateKey, 32);
            return CryptoResult<KeySet>.Success(new KeySet { ... });
        });
}
```

### Testing Exception Scenarios

```csharp
[Fact]
public async Task FinalizeAsync_WithInvalidSignature_ShouldThrowUnauthorizedException()
{
    // Arrange
    _mockCryptoModule
        .Setup(c => c.VerifyAsync(...))
        .ReturnsAsync(CryptoStatus.InvalidSignature);

    // Act & Assert
    var act = async () => await _orchestrator.FinalizeAsync(request);
    await act.Should().ThrowAsync<UnauthorizedAccessException>()
        .WithMessage("*Invalid signature*");
}
```

---

## Workflow Checklists

### New Unit Test File

Copy and track progress:

- [ ] Create test class with naming `{ClassName}Tests.cs`
- [ ] Add license header
- [ ] Declare mock fields with `_mock` prefix
- [ ] Initialize mocks in constructor
- [ ] Create SUT instance
- [ ] Add `SetupDefault*()` helper if needed
- [ ] Write first happy path test
- [ ] Write edge case tests
- [ ] Write error handling tests
- [ ] Verify no uncovered branches

### Integration Test with Mocked Dependencies

Copy and track progress:

- [ ] Create WebApplicationFactory subclass
- [ ] Replace database with InMemory
- [ ] Mock external service clients
- [ ] Add test authentication handler
- [ ] Create helper methods for authenticated clients
- [ ] Add seed data method
- [ ] Write test using IClassFixture

### Feedback Loop: Mock Setup Validation

1. Write mock setup
2. Run test: `dotnet test --filter "FullyQualifiedName~YourTestName"`
3. If mock not invoked, check:
   - Is `It.IsAny<T>()` matching the correct type?
   - Are all parameters accounted for?
   - Is the SUT using the mocked dependency?
4. Repeat until test passes

### Related Skills

- See the **xunit** skill for test structure patterns
- See the **fluent-assertions** skill for assertion syntax
- See the **entity-framework** skill for DbContext mocking patterns