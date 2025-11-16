// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Register.Core.Events;
using Sorcha.Register.Core.Managers;
using Sorcha.Register.Models.Enums;
using Sorcha.Register.Storage.InMemory;
using Xunit;

namespace Sorcha.Register.Core.Tests.Managers;

public class RegisterManagerTests
{
    private readonly InMemoryRegisterRepository _repository;
    private readonly InMemoryEventPublisher _eventPublisher;
    private readonly RegisterManager _manager;

    public RegisterManagerTests()
    {
        _repository = new InMemoryRegisterRepository();
        _eventPublisher = new InMemoryEventPublisher();
        _manager = new RegisterManager(_repository, _eventPublisher);
    }

    [Fact]
    public async Task CreateRegisterAsync_WithValidData_ShouldCreateRegister()
    {
        // Arrange
        var name = "TestRegister";
        var tenantId = "tenant-123";

        // Act
        var result = await _manager.CreateRegisterAsync(name, tenantId);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(name);
        result.TenantId.Should().Be(tenantId);
        result.Id.Should().NotBeEmpty();
        result.Id.Should().HaveLength(32); // GUID without hyphens
        result.Height.Should().Be(0u);
        result.Status.Should().Be(RegisterStatus.Offline);
        result.Advertise.Should().BeFalse();
        result.IsFullReplica.Should().BeTrue();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CreateRegisterAsync_ShouldPublishRegisterCreatedEvent()
    {
        // Arrange
        var name = "EventTest";
        var tenantId = "tenant-456";

        // Act
        var result = await _manager.CreateRegisterAsync(name, tenantId);

        // Assert
        var events = _eventPublisher.GetPublishedEvents<RegisterCreatedEvent>();
        events.Should().ContainSingle();
        var createdEvent = events.First();
        createdEvent.RegisterId.Should().Be(result.Id);
        createdEvent.Name.Should().Be(name);
        createdEvent.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task CreateRegisterAsync_WithAdvertise_ShouldSetCorrectly()
    {
        // Arrange
        var name = "AdvertisedRegister";
        var tenantId = "tenant-789";

        // Act
        var result = await _manager.CreateRegisterAsync(name, tenantId, advertise: true, isFullReplica: false);

        // Assert
        result.Advertise.Should().BeTrue();
        result.IsFullReplica.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CreateRegisterAsync_WithInvalidName_ShouldThrowException(string? invalidName)
    {
        // Act
        var act = () => _manager.CreateRegisterAsync(invalidName!, "tenant-123");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public async Task CreateRegisterAsync_WithNameTooLong_ShouldThrowException()
    {
        // Arrange
        var longName = new string('a', 39); // Max is 38

        // Act
        var act = () => _manager.CreateRegisterAsync(longName, "tenant-123");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*between 1 and 38 characters*")
            .WithParameterName("name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CreateRegisterAsync_WithInvalidTenantId_ShouldThrowException(string? invalidTenantId)
    {
        // Act
        var act = () => _manager.CreateRegisterAsync("ValidName", invalidTenantId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("tenantId");
    }

    [Fact]
    public async Task GetRegisterAsync_WithExistingId_ShouldReturnRegister()
    {
        // Arrange
        var created = await _manager.CreateRegisterAsync("TestRegister", "tenant-123");

        // Act
        var result = await _manager.GetRegisterAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.Name.Should().Be("TestRegister");
    }

    [Fact]
    public async Task GetRegisterAsync_WithNonExistingId_ShouldReturnNull()
    {
        // Act
        var result = await _manager.GetRegisterAsync("non-existing-id");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetRegisterAsync_WithInvalidId_ShouldThrowException(string? invalidId)
    {
        // Act
        var act = () => _manager.GetRegisterAsync(invalidId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("registerId");
    }

    [Fact]
    public async Task GetAllRegistersAsync_WithMultipleRegisters_ShouldReturnAll()
    {
        // Arrange
        await _manager.CreateRegisterAsync("Register1", "tenant-1");
        await _manager.CreateRegisterAsync("Register2", "tenant-2");
        await _manager.CreateRegisterAsync("Register3", "tenant-3");

        // Act
        var result = await _manager.GetAllRegistersAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Select(r => r.Name).Should().Contain(new[] { "Register1", "Register2", "Register3" });
    }

    [Fact]
    public async Task GetAllRegistersAsync_WithNoRegisters_ShouldReturnEmpty()
    {
        // Act
        var result = await _manager.GetAllRegistersAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRegistersByTenantAsync_ShouldReturnOnlyTenantRegisters()
    {
        // Arrange
        await _manager.CreateRegisterAsync("Tenant1Reg1", "tenant-1");
        await _manager.CreateRegisterAsync("Tenant1Reg2", "tenant-1");
        await _manager.CreateRegisterAsync("Tenant2Reg1", "tenant-2");
        await _manager.CreateRegisterAsync("Tenant2Reg2", "tenant-2");

        // Act
        var tenant1Registers = await _manager.GetRegistersByTenantAsync("tenant-1");

        // Assert
        tenant1Registers.Should().HaveCount(2);
        tenant1Registers.Should().AllSatisfy(r => r.TenantId.Should().Be("tenant-1"));
        tenant1Registers.Select(r => r.Name).Should().Contain(new[] { "Tenant1Reg1", "Tenant1Reg2" });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetRegistersByTenantAsync_WithInvalidTenantId_ShouldThrowException(string? invalidTenantId)
    {
        // Act
        var act = () => _manager.GetRegistersByTenantAsync(invalidTenantId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("tenantId");
    }

    [Fact]
    public async Task UpdateRegisterAsync_WithValidRegister_ShouldUpdate()
    {
        // Arrange
        var created = await _manager.CreateRegisterAsync("Original", "tenant-123");
        created.Name = "Updated";
        created.Advertise = true;
        created.Height = 10;

        // Act
        var updated = await _manager.UpdateRegisterAsync(created);

        // Assert
        updated.Name.Should().Be("Updated");
        updated.Advertise.Should().BeTrue();
        updated.Height.Should().Be(10u);
    }

    [Fact]
    public async Task UpdateRegisterAsync_WithNonExistingRegister_ShouldThrowException()
    {
        // Arrange
        var nonExisting = new Sorcha.Register.Models.Register
        {
            Id = "non-existing-id",
            Name = "Test",
            TenantId = "tenant-123"
        };

        // Act
        var act = () => _manager.UpdateRegisterAsync(nonExisting);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task UpdateRegisterAsync_WithNull_ShouldThrowException()
    {
        // Act
        var act = () => _manager.UpdateRegisterAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData(RegisterStatus.Offline)]
    [InlineData(RegisterStatus.Online)]
    [InlineData(RegisterStatus.Checking)]
    [InlineData(RegisterStatus.Recovery)]
    public async Task UpdateRegisterStatusAsync_WithValidStatus_ShouldUpdate(RegisterStatus newStatus)
    {
        // Arrange
        var created = await _manager.CreateRegisterAsync("TestRegister", "tenant-123");

        // Act
        var updated = await _manager.UpdateRegisterStatusAsync(created.Id, newStatus);

        // Assert
        updated.Status.Should().Be(newStatus);
        updated.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task UpdateRegisterStatusAsync_WithNonExistingId_ShouldThrowException()
    {
        // Act
        var act = () => _manager.UpdateRegisterStatusAsync("non-existing", RegisterStatus.Online);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task DeleteRegisterAsync_WithValidTenant_ShouldDelete()
    {
        // Arrange
        var created = await _manager.CreateRegisterAsync("ToDelete", "tenant-123");
        _eventPublisher.Clear(); // Clear creation event

        // Act
        await _manager.DeleteRegisterAsync(created.Id, "tenant-123");

        // Assert
        var retrieved = await _manager.GetRegisterAsync(created.Id);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task DeleteRegisterAsync_ShouldPublishRegisterDeletedEvent()
    {
        // Arrange
        var created = await _manager.CreateRegisterAsync("ToDelete", "tenant-456");
        _eventPublisher.Clear();

        // Act
        await _manager.DeleteRegisterAsync(created.Id, "tenant-456");

        // Assert
        var events = _eventPublisher.GetPublishedEvents<RegisterDeletedEvent>();
        events.Should().ContainSingle();
        var deletedEvent = events.First();
        deletedEvent.RegisterId.Should().Be(created.Id);
        deletedEvent.TenantId.Should().Be("tenant-456");
    }

    [Fact]
    public async Task DeleteRegisterAsync_WithWrongTenant_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var created = await _manager.CreateRegisterAsync("Protected", "tenant-123");

        // Act
        var act = () => _manager.DeleteRegisterAsync(created.Id, "wrong-tenant");

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*does not belong to tenant*");
    }

    [Fact]
    public async Task DeleteRegisterAsync_WithNonExistingId_ShouldThrowException()
    {
        // Act
        var act = () => _manager.DeleteRegisterAsync("non-existing", "tenant-123");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task RegisterExistsAsync_WithExistingRegister_ShouldReturnTrue()
    {
        // Arrange
        var created = await _manager.CreateRegisterAsync("Existing", "tenant-123");

        // Act
        var exists = await _manager.RegisterExistsAsync(created.Id);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterExistsAsync_WithNonExistingRegister_ShouldReturnFalse()
    {
        // Act
        var exists = await _manager.RegisterExistsAsync("non-existing-id");

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task GetRegisterCountAsync_WithMultipleRegisters_ShouldReturnCorrectCount()
    {
        // Arrange
        await _manager.CreateRegisterAsync("Reg1", "tenant-1");
        await _manager.CreateRegisterAsync("Reg2", "tenant-2");
        await _manager.CreateRegisterAsync("Reg3", "tenant-3");

        // Act
        var count = await _manager.GetRegisterCountAsync();

        // Assert
        count.Should().Be(3);
    }

    [Fact]
    public async Task GetRegisterCountAsync_WithNoRegisters_ShouldReturnZero()
    {
        // Act
        var count = await _manager.GetRegisterCountAsync();

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void Constructor_WithNullRepository_ShouldThrowException()
    {
        // Act
        var act = () => new RegisterManager(null!, _eventPublisher);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("repository");
    }

    [Fact]
    public void Constructor_WithNullEventPublisher_ShouldThrowException()
    {
        // Act
        var act = () => new RegisterManager(_repository, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("eventPublisher");
    }
}
