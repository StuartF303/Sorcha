# Task: Unit Tests - Register Operations

**ID:** REG-022
**Status:** Not Started
**Priority:** High
**Estimate:** 8 hours
**Created:** 2025-11-13
**Specification:** [siccar-register-service.md](../specs/siccar-register-service.md)

## Objective

Create comprehensive unit tests for RegisterService operations to ensure correctness, error handling, and edge cases are covered.

## Tasks

### Test Project Setup
- [ ] Create `Siccar.RegisterService.Tests` project
- [ ] Add NuGet package `xunit` (2.9.2)
- [ ] Add NuGet package `xunit.runner.visualstudio`
- [ ] Add NuGet package `Moq` for mocking
- [ ] Add NuGet package `FluentAssertions` for readable assertions
- [ ] Add NuGet package `Microsoft.Extensions.Logging.Abstractions`
- [ ] Reference `Siccar.RegisterService` project
- [ ] Reference `Siccar.RegisterService.Storage.InMemory` project

### RegisterService Tests
- [ ] Create `Services/RegisterServiceTests.cs`
- [ ] Test `CreateRegisterAsync` success case
- [ ] Test `CreateRegisterAsync` with duplicate ID (should throw)
- [ ] Test `CreateRegisterAsync` with max registers reached (should throw)
- [ ] Test `CreateRegisterAsync` with invalid name (should throw)
- [ ] Test `CreateRegisterAsync` publishes event
- [ ] Test `GetRegisterAsync` returns correct register
- [ ] Test `GetRegisterAsync` with non-existent ID returns null
- [ ] Test `GetRegistersAsync` returns all registers
- [ ] Test `GetRegistersByTenantAsync` filters by tenant
- [ ] Test `UpdateRegisterAsync` success case
- [ ] Test `UpdateRegisterAsync` with non-existent register (should throw)
- [ ] Test `UpdateRegisterStatusAsync` changes status
- [ ] Test `UpdateRegisterStatusAsync` publishes event
- [ ] Test `IncrementRegisterHeightAsync` increments atomically
- [ ] Test `DeleteRegisterAsync` success case
- [ ] Test `DeleteRegisterAsync` publishes event

### Validation Tests
- [ ] Create `Services/RegisterValidationTests.cs`
- [ ] Test register ID format validation (guid without hyphens)
- [ ] Test register name validation (max 38 chars)
- [ ] Test register name required
- [ ] Test tenant ID required
- [ ] Test invalid enum values rejected

### Authorization Tests
- [ ] Create `Authorization/RegisterAuthorizationTests.cs`
- [ ] Test tenant-based register filtering
- [ ] Test installation admin access (all registers)
- [ ] Test unauthorized access throws exception
- [ ] Test role-based access control

### Mock Setup
- [ ] Create test fixtures for common scenarios
- [ ] Mock `IRegisterRepository` for isolation
- [ ] Mock `IEventPublisher` for event verification
- [ ] Mock `ILogger` for log verification
- [ ] Create test data builders

### Edge Cases
- [ ] Test concurrent register creation
- [ ] Test null/empty parameters
- [ ] Test very long register names
- [ ] Test special characters in names
- [ ] Test register count at exactly maximum
- [ ] Test register count over maximum

### Event Verification
- [ ] Verify `RegisterCreated` event published with correct data
- [ ] Verify `RegisterDeleted` event published
- [ ] Verify `RegisterUpdated` event published
- [ ] Verify `RegisterHeightUpdated` event published
- [ ] Verify event correlation IDs

## Test Example

```csharp
public class RegisterServiceTests
{
    private readonly Mock<IRegisterRepository> _mockRepository;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<ILogger<RegisterService>> _mockLogger;
    private readonly RegisterService _service;

    public RegisterServiceTests()
    {
        _mockRepository = new Mock<IRegisterRepository>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockLogger = new Mock<ILogger<RegisterService>>();
        _service = new RegisterService(
            _mockRepository.Object,
            _mockEventPublisher.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task CreateRegisterAsync_ValidRegister_ReturnsCreatedRegister()
    {
        // Arrange
        var newRegister = new Register
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Test Register",
            Advertise = true,
            IsFullReplica = true
        };

        _mockRepository
            .Setup(r => r.CountRegisters(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockRepository
            .Setup(r => r.InsertRegisterAsync(
                It.IsAny<Register>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(newRegister);

        // Act
        var result = await _service.CreateRegisterAsync(
            newRegister,
            "tenant-123");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(newRegister.Id);
        result.TenantId.Should().Be("tenant-123");
        result.Height.Should().Be(0);
        result.Status.Should().Be(RegisterStatusTypes.ONLINE);

        _mockEventPublisher.Verify(
            p => p.PublishAsync(
                Topics.RegisterCreatedTopicName,
                It.Is<RegisterCreated>(e =>
                    e.Id == newRegister.Id &&
                    e.TenantId == "tenant-123"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateRegisterAsync_MaxRegistersReached_ThrowsException()
    {
        // Arrange
        var newRegister = new Register
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "Test Register"
        };

        _mockRepository
            .Setup(r => r.CountRegisters(It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);

        // Act & Assert
        await Assert.ThrowsAsync<RegisterLimitExceededException>(
            () => _service.CreateRegisterAsync(newRegister, "tenant-123"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateRegisterAsync_InvalidName_ThrowsException(
        string invalidName)
    {
        // Arrange
        var newRegister = new Register
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = invalidName
        };

        _mockRepository
            .Setup(r => r.CountRegisters(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateRegisterAsync(newRegister, "tenant-123"));
    }

    [Fact]
    public async Task CreateRegisterAsync_NameTooLong_ThrowsException()
    {
        // Arrange
        var newRegister = new Register
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = new string('a', 39) // 39 chars, max is 38
        };

        _mockRepository
            .Setup(r => r.CountRegisters(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateRegisterAsync(newRegister, "tenant-123"));
    }

    [Fact]
    public async Task DeleteRegisterAsync_ExistingRegister_DeletesAndPublishesEvent()
    {
        // Arrange
        var registerId = Guid.NewGuid().ToString("N");

        _mockRepository
            .Setup(r => r.GetRegisterAsync(registerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Register { Id = registerId, Name = "Test" });

        _mockRepository
            .Setup(r => r.DeleteRegisterAsync(registerId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteRegisterAsync(registerId);

        // Assert
        _mockRepository.Verify(
            r => r.DeleteRegisterAsync(registerId, It.IsAny<CancellationToken>()),
            Times.Once);

        _mockEventPublisher.Verify(
            p => p.PublishAsync(
                Topics.RegisterDeletedTopicName,
                It.Is<RegisterDeleted>(e => e.Id == registerId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

## Acceptance Criteria

- [ ] All RegisterService methods have unit tests
- [ ] >90% code coverage for RegisterService
- [ ] All edge cases covered
- [ ] Event publishing verified
- [ ] Error cases tested
- [ ] Tests use mocks for isolation
- [ ] Tests run fast (<100ms each)
- [ ] All tests passing

## Definition of Done

- All tests implemented
- All tests passing
- Code coverage >90%
- Code review approved
- CI/CD pipeline runs tests
- Test documentation complete

---

**Dependencies:** REG-004, REG-009
**Blocks:** None
