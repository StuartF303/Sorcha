// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Sorcha.ServiceDefaults.Tests;

/// <summary>
/// Tests for <see cref="CorsExtensions"/> which configure a permissive CORS policy
/// for development use across Sorcha services.
/// </summary>
public class CorsExtensionsTests
{
    [Fact]
    public void AddSorchaCors_ReturnsBuilder_ForChaining()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        var result = builder.AddSorchaCors();

        // Assert
        result.Should().BeSameAs(builder, "the method should return the builder for chaining");
    }

    [Fact]
    public void AddSorchaCors_RegistersCorsServices()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.AddSorchaCors();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        var corsService = serviceProvider.GetService<ICorsService>();
        corsService.Should().NotBeNull("AddCors should register ICorsService in the service collection");
    }

    [Fact]
    public void AddSorchaCors_RegistersCorsPolicyProvider()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        builder.AddSorchaCors();

        // Assert
        var serviceProvider = builder.Services.BuildServiceProvider();
        var policyProvider = serviceProvider.GetService<ICorsPolicyProvider>();
        policyProvider.Should().NotBeNull("AddCors should register ICorsPolicyProvider");
    }

    [Fact]
    public void AddSorchaCors_DefaultPolicy_AllowsAnyOrigin()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.AddSorchaCors();
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Act
        var corsOptions = serviceProvider.GetRequiredService<IOptions<CorsOptions>>().Value;
        var defaultPolicy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);

        // Assert
        defaultPolicy.Should().NotBeNull("a default CORS policy should be configured");
        defaultPolicy!.AllowAnyOrigin.Should().BeTrue("the default policy should allow any origin");
    }

    [Fact]
    public void AddSorchaCors_DefaultPolicy_AllowsAnyMethod()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.AddSorchaCors();
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Act
        var corsOptions = serviceProvider.GetRequiredService<IOptions<CorsOptions>>().Value;
        var defaultPolicy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);

        // Assert
        defaultPolicy.Should().NotBeNull("a default CORS policy should be configured");
        defaultPolicy!.AllowAnyMethod.Should().BeTrue("the default policy should allow any HTTP method");
    }

    [Fact]
    public void AddSorchaCors_DefaultPolicy_AllowsAnyHeader()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.AddSorchaCors();
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Act
        var corsOptions = serviceProvider.GetRequiredService<IOptions<CorsOptions>>().Value;
        var defaultPolicy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);

        // Assert
        defaultPolicy.Should().NotBeNull("a default CORS policy should be configured");
        defaultPolicy!.AllowAnyHeader.Should().BeTrue("the default policy should allow any header");
    }

    [Fact]
    public void AddSorchaCors_DefaultPolicy_DoesNotSupportCredentials()
    {
        // Arrange — AllowAnyOrigin and SupportsCredentials are mutually exclusive in CORS spec
        var builder = WebApplication.CreateBuilder();
        builder.AddSorchaCors();
        var serviceProvider = builder.Services.BuildServiceProvider();

        // Act
        var corsOptions = serviceProvider.GetRequiredService<IOptions<CorsOptions>>().Value;
        var defaultPolicy = corsOptions.GetPolicy(corsOptions.DefaultPolicyName);

        // Assert
        defaultPolicy.Should().NotBeNull();
        defaultPolicy!.SupportsCredentials.Should().BeFalse(
            "AllowAnyOrigin and SupportsCredentials are mutually exclusive per the CORS specification");
    }
}
