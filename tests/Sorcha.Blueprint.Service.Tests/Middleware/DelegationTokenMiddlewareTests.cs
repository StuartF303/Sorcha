// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.Blueprint.Service.Middleware;

namespace Sorcha.Blueprint.Service.Tests.Middleware;

public class DelegationTokenMiddlewareTests
{
    private readonly Mock<ILogger<DelegationTokenMiddleware>> _logger = new();

    private DelegationTokenMiddleware CreateMiddleware(RequestDelegate next)
        => new(next, _logger.Object);

    [Fact]
    public async Task InvokeAsync_WithTokenHeader_StoresTokenInItems()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers[DelegationTokenMiddleware.DelegationTokenHeader] = "test-token-123";
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
        context.Items.Should().ContainKey(DelegationTokenMiddleware.DelegationTokenKey);
        context.Items[DelegationTokenMiddleware.DelegationTokenKey].Should().Be("test-token-123");
    }

    [Fact]
    public async Task InvokeAsync_WithoutTokenHeader_DoesNotStoreToken()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Items.Should().NotContainKey(DelegationTokenMiddleware.DelegationTokenKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task InvokeAsync_WithEmptyOrWhitespaceToken_DoesNotStoreToken(string token)
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers[DelegationTokenMiddleware.DelegationTokenHeader] = token;
        var middleware = CreateMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Items.Should().NotContainKey(DelegationTokenMiddleware.DelegationTokenKey);
    }

    [Fact]
    public async Task InvokeAsync_AlwaysCallsNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var nextCalled = false;
        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public void GetDelegationToken_WhenTokenPresent_ReturnsToken()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Items[DelegationTokenMiddleware.DelegationTokenKey] = "my-token";

        // Act
        var token = context.GetDelegationToken();

        // Assert
        token.Should().Be("my-token");
    }

    [Fact]
    public void GetDelegationToken_WhenTokenAbsent_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var token = context.GetDelegationToken();

        // Assert
        token.Should().BeNull();
    }

    [Fact]
    public void GetRequiredDelegationToken_WhenTokenPresent_ReturnsToken()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Items[DelegationTokenMiddleware.DelegationTokenKey] = "required-token";

        // Act
        var token = context.GetRequiredDelegationToken();

        // Assert
        token.Should().Be("required-token");
    }

    [Fact]
    public void GetRequiredDelegationToken_WhenTokenAbsent_ThrowsInvalidOperationException()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        var act = () => context.GetRequiredDelegationToken();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*X-Delegation-Token*");
    }
}
