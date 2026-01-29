// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Logging;
using Sorcha.McpServer.Infrastructure;

namespace Sorcha.McpServer.Tests.Infrastructure;

public class ServiceAvailabilityTrackerTests
{
    private readonly Mock<ILogger<ServiceAvailabilityTracker>> _loggerMock;
    private readonly ServiceAvailabilityTracker _tracker;

    public ServiceAvailabilityTrackerTests()
    {
        _loggerMock = new Mock<ILogger<ServiceAvailabilityTracker>>();
        _tracker = new ServiceAvailabilityTracker(_loggerMock.Object);
    }

    [Fact]
    public void IsServiceAvailable_NewService_ReturnsTrue()
    {
        // Act
        var result = _tracker.IsServiceAvailable("Blueprint");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RecordSuccess_Service_MarksAsAvailable()
    {
        // Arrange - simulate some failures first
        _tracker.RecordFailure("TestService");
        _tracker.RecordFailure("TestService");

        // Act
        _tracker.RecordSuccess("TestService");

        // Assert
        _tracker.IsServiceAvailable("TestService").Should().BeTrue();
        var status = _tracker.GetAllServiceStatus();
        status["TestService"].ConsecutiveFailures.Should().Be(0);
        status["TestService"].LastSuccessAt.Should().NotBeNull();
    }

    [Fact]
    public void RecordFailure_UnderThreshold_ServiceStaysAvailable()
    {
        // Act - 2 failures (threshold is 3)
        _tracker.RecordFailure("Blueprint");
        _tracker.RecordFailure("Blueprint");

        // Assert
        _tracker.IsServiceAvailable("Blueprint").Should().BeTrue();
        var status = _tracker.GetAllServiceStatus();
        status["Blueprint"].ConsecutiveFailures.Should().Be(2);
    }

    [Fact]
    public void RecordFailure_AtThreshold_ServiceBecomesUnavailable()
    {
        // Act - 3 failures (threshold reached)
        _tracker.RecordFailure("Blueprint");
        _tracker.RecordFailure("Blueprint");
        _tracker.RecordFailure("Blueprint");

        // Assert
        _tracker.IsServiceAvailable("Blueprint").Should().BeFalse();
        var status = _tracker.GetAllServiceStatus();
        status["Blueprint"].IsAvailable.Should().BeFalse();
        status["Blueprint"].CooldownEndsAt.Should().NotBeNull();
    }

    [Fact]
    public void RecordFailure_WithException_CapturesErrorMessage()
    {
        // Act
        _tracker.RecordFailure("Blueprint", new Exception("Connection refused"));

        // Assert
        var status = _tracker.GetAllServiceStatus();
        status["Blueprint"].LastErrorMessage.Should().Be("Connection refused");
    }

    [Fact]
    public void GetRequiredServices_KnownTool_ReturnsServices()
    {
        // Act
        var services = _tracker.GetRequiredServices("sorcha_action_submit");

        // Assert
        services.Should().Contain("Blueprint");
        services.Should().Contain("Register");
        services.Should().Contain("Wallet");
    }

    [Fact]
    public void GetRequiredServices_UnknownTool_ReturnsEmpty()
    {
        // Act
        var services = _tracker.GetRequiredServices("unknown_tool");

        // Assert
        services.Should().BeEmpty();
    }

    [Fact]
    public void AreToolServicesAvailable_AllAvailable_ReturnsTrue()
    {
        // Act
        var result = _tracker.AreToolServicesAvailable("sorcha_blueprint_list");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AreToolServicesAvailable_SomeUnavailable_ReturnsFalse()
    {
        // Arrange - make Blueprint service unavailable
        for (var i = 0; i < 3; i++)
        {
            _tracker.RecordFailure("Blueprint");
        }

        // Act
        var result = _tracker.AreToolServicesAvailable("sorcha_blueprint_list");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetUnavailableServicesForTool_SomeUnavailable_ReturnsUnavailableList()
    {
        // Arrange - make Blueprint service unavailable
        for (var i = 0; i < 3; i++)
        {
            _tracker.RecordFailure("Blueprint");
        }

        // Act
        var unavailable = _tracker.GetUnavailableServicesForTool("sorcha_workflow_instances");

        // Assert
        unavailable.Should().Contain("Blueprint");
        unavailable.Should().NotContain("Register");
    }

    [Fact]
    public void GetAllServiceStatus_ReturnsAllServices()
    {
        // Act
        var status = _tracker.GetAllServiceStatus();

        // Assert
        status.Should().ContainKey("Blueprint");
        status.Should().ContainKey("Register");
        status.Should().ContainKey("Wallet");
        status.Should().ContainKey("Tenant");
        status.Should().ContainKey("Validator");
        status.Should().ContainKey("Peer");
        status.Should().ContainKey("ApiGateway");
    }

    [Fact]
    public void RecordSuccess_AfterFailures_ResetsFailureCount()
    {
        // Arrange
        _tracker.RecordFailure("Blueprint");
        _tracker.RecordFailure("Blueprint");

        // Act
        _tracker.RecordSuccess("Blueprint");

        // Assert
        var status = _tracker.GetAllServiceStatus();
        status["Blueprint"].ConsecutiveFailures.Should().Be(0);
    }

    [Theory]
    [InlineData("sorcha_health_check", 7)] // Requires all 7 services
    [InlineData("sorcha_blueprint_list", 1)] // Just Blueprint
    [InlineData("sorcha_action_submit", 3)] // Blueprint, Register, Wallet
    public void GetRequiredServices_VariousTools_ReturnsCorrectCount(string toolName, int expectedCount)
    {
        // Act
        var services = _tracker.GetRequiredServices(toolName);

        // Assert
        services.Should().HaveCount(expectedCount);
    }
}
