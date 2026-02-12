// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.Logging;
using Sorcha.McpServer.Infrastructure;

namespace Sorcha.McpServer.Tests.Infrastructure;

public class McpErrorHandlerTests
{
    private readonly Mock<ILogger<McpErrorHandler>> _loggerMock;
    private readonly McpErrorHandler _handler;

    public McpErrorHandlerTests()
    {
        _loggerMock = new Mock<ILogger<McpErrorHandler>>();
        _handler = new McpErrorHandler(_loggerMock.Object);
    }

    [Fact]
    public void CreateError_AuthenticationError_ReturnsCorrectResponse()
    {
        // Act
        var result = _handler.CreateError(McpErrorCategory.Authentication, "Token expired");

        // Assert
        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("AUTHENTICATION_REQUIRED");
        result.Message.Should().Be("Token expired");
        result.SuggestedAction.Should().Contain("JWT token");
    }

    [Fact]
    public void CreateError_AuthorizationError_ReturnsCorrectResponse()
    {
        // Act
        var result = _handler.CreateError(McpErrorCategory.Authorization, "Access denied");

        // Assert
        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("ACCESS_DENIED");
        result.Message.Should().Be("Access denied");
        result.SuggestedAction.Should().Contain("administrator");
    }

    [Fact]
    public void CreateError_ValidationError_IncludesExceptionDetails()
    {
        // Arrange
        var exception = new ArgumentException("Invalid parameter 'name'");

        // Act
        var result = _handler.CreateError(McpErrorCategory.Validation, "Validation failed", exception);

        // Assert
        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("VALIDATION_ERROR");
        result.Details.Should().Contain("Invalid parameter");
    }

    [Fact]
    public void CreateError_ServiceUnavailable_ReturnsCorrectResponse()
    {
        // Act
        var result = _handler.CreateError(McpErrorCategory.ServiceUnavailable, "Blueprint service down");

        // Assert
        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("SERVICE_UNAVAILABLE");
        result.SuggestedAction.Should().Contain("try again later");
    }

    [Fact]
    public void CreateError_RateLimited_IncludesRetryAfter()
    {
        // Act
        var result = _handler.CreateError(McpErrorCategory.RateLimited, "Rate limit exceeded");

        // Assert
        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("RATE_LIMITED");
        result.RetryAfterSeconds.Should().Be(60);
    }

    [Fact]
    public void CreateError_NotFound_ReturnsCorrectResponse()
    {
        // Act
        var result = _handler.CreateError(McpErrorCategory.NotFound, "Blueprint not found");

        // Assert
        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("NOT_FOUND");
    }

    [Fact]
    public void CreateError_Conflict_ReturnsCorrectResponse()
    {
        // Act
        var result = _handler.CreateError(McpErrorCategory.Conflict, "Version conflict");

        // Assert
        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("CONFLICT");
        result.SuggestedAction.Should().Contain("refresh");
    }

    [Fact]
    public void CreateError_Internal_DoesNotLeakDetails()
    {
        // Arrange
        var exception = new Exception("Database connection string: server=secret...");

        // Act
        var result = _handler.CreateError(McpErrorCategory.Internal, "Something went wrong", exception);

        // Assert
        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("INTERNAL_ERROR");
        result.Message.Should().NotContain("Database");
        result.Message.Should().NotContain("secret");
        result.Details.Should().BeNull();
    }

    [Fact]
    public void AuthenticationError_Extension_CreatesCorrectError()
    {
        // Act
        var result = _handler.AuthenticationError("Custom auth message");

        // Assert
        result.ErrorCode.Should().Be("AUTHENTICATION_REQUIRED");
        result.Message.Should().Be("Custom auth message");
    }

    [Fact]
    public void AuthorizationError_Extension_IncludesToolName()
    {
        // Act
        var result = _handler.AuthorizationError("sorcha_health_check");

        // Assert
        result.ErrorCode.Should().Be("ACCESS_DENIED");
        result.Message.Should().Contain("sorcha_health_check");
    }

    [Fact]
    public void ServiceUnavailableError_Extension_IncludesServiceName()
    {
        // Act
        var result = _handler.ServiceUnavailableError("Blueprint");

        // Assert
        result.ErrorCode.Should().Be("SERVICE_UNAVAILABLE");
        result.Message.Should().Contain("Blueprint");
    }

    [Fact]
    public void NotFoundError_Extension_FormatsCorrectly()
    {
        // Act
        var result = _handler.NotFoundError("Blueprint", "BP-123");

        // Assert
        result.ErrorCode.Should().Be("NOT_FOUND");
        result.Message.Should().Contain("Blueprint");
        result.Message.Should().Contain("BP-123");
    }
}
