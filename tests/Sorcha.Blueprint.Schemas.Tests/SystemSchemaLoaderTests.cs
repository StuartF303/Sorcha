// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.Blueprint.Schemas.Models;
using Sorcha.Blueprint.Schemas.Services;

namespace Sorcha.Blueprint.Schemas.Tests;

/// <summary>
/// Unit tests for SystemSchemaLoader.
/// </summary>
public class SystemSchemaLoaderTests
{
    private readonly SystemSchemaLoader _loader;

    public SystemSchemaLoaderTests()
    {
        var loggerMock = new Mock<ILogger<SystemSchemaLoader>>();
        _loader = new SystemSchemaLoader(loggerMock.Object);
    }

    [Fact]
    public void GetSystemSchemas_ReturnsAllFourSchemas()
    {
        // Act
        var schemas = _loader.GetSystemSchemas();

        // Assert
        schemas.Should().HaveCount(4);
        schemas.Select(s => s.Identifier).Should().Contain(new[]
        {
            "installation",
            "organisation",
            "participant",
            "register"
        });
    }

    [Fact]
    public void GetSystemSchemas_AllSchemasHaveSystemCategory()
    {
        // Act
        var schemas = _loader.GetSystemSchemas();

        // Assert
        schemas.Should().OnlyContain(s => s.Category == SchemaCategory.System);
    }

    [Fact]
    public void GetSystemSchemas_AllSchemasHaveActiveStatus()
    {
        // Act
        var schemas = _loader.GetSystemSchemas();

        // Assert
        schemas.Should().OnlyContain(s => s.Status == SchemaStatus.Active);
    }

    [Fact]
    public void GetSystemSchemas_AllSchemasAreGloballyPublished()
    {
        // Act
        var schemas = _loader.GetSystemSchemas();

        // Assert
        schemas.Should().OnlyContain(s => s.IsGloballyPublished);
    }

    [Fact]
    public void GetSystemSchemas_AllSchemasHaveNoOrganization()
    {
        // Act
        var schemas = _loader.GetSystemSchemas();

        // Assert
        schemas.Should().OnlyContain(s => s.OrganizationId == null);
    }

    [Fact]
    public void GetSystemSchemas_AllSchemasHaveValidContent()
    {
        // Act
        var schemas = _loader.GetSystemSchemas();

        // Assert
        foreach (var schema in schemas)
        {
            schema.Content.Should().NotBeNull();
            schema.Content.RootElement.TryGetProperty("$schema", out var schemaProp).Should().BeTrue();
            schemaProp.GetString().Should().Contain("json-schema.org");
        }
    }

    [Theory]
    [InlineData("installation")]
    [InlineData("organisation")]
    [InlineData("participant")]
    [InlineData("register")]
    public void GetSystemSchema_ReturnsCorrectSchema(string identifier)
    {
        // Act
        var schema = _loader.GetSystemSchema(identifier);

        // Assert
        schema.Should().NotBeNull();
        schema!.Identifier.Should().Be(identifier);
    }

    [Fact]
    public void GetSystemSchema_WithInvalidIdentifier_ReturnsNull()
    {
        // Act
        var schema = _loader.GetSystemSchema("non-existent");

        // Assert
        schema.Should().BeNull();
    }

    [Theory]
    [InlineData("installation", true)]
    [InlineData("organisation", true)]
    [InlineData("participant", true)]
    [InlineData("register", true)]
    [InlineData("INSTALLATION", true)] // Case insensitive
    [InlineData("custom-schema", false)]
    [InlineData("", false)]
    public void IsSystemSchema_ReturnsCorrectResult(string identifier, bool expected)
    {
        // Act
        var result = SystemSchemaLoader.IsSystemSchema(identifier);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetSystemSchemas_InstallationSchema_HasRequiredProperties()
    {
        // Act
        var schema = _loader.GetSystemSchema("installation");

        // Assert
        schema.Should().NotBeNull();
        schema!.Title.Should().Be("Installation");

        var root = schema.Content.RootElement;
        root.TryGetProperty("required", out var required).Should().BeTrue();
        var requiredFields = required.EnumerateArray().Select(x => x.GetString()).ToList();
        requiredFields.Should().Contain(new[] { "name", "publicKey" });
    }

    [Fact]
    public void GetSystemSchemas_OrganisationSchema_HasRequiredProperties()
    {
        // Act
        var schema = _loader.GetSystemSchema("organisation");

        // Assert
        schema.Should().NotBeNull();
        schema!.Title.Should().Be("Organisation");

        var root = schema.Content.RootElement;
        root.TryGetProperty("required", out var required).Should().BeTrue();
        var requiredFields = required.EnumerateArray().Select(x => x.GetString()).ToList();
        requiredFields.Should().Contain(new[] { "identifier", "name" });
    }

    [Fact]
    public void GetSystemSchemas_ParticipantSchema_HasRequiredProperties()
    {
        // Act
        var schema = _loader.GetSystemSchema("participant");

        // Assert
        schema.Should().NotBeNull();
        schema!.Title.Should().Be("Participant");

        var root = schema.Content.RootElement;
        root.TryGetProperty("required", out var required).Should().BeTrue();
        var requiredFields = required.EnumerateArray().Select(x => x.GetString()).ToList();
        requiredFields.Should().Contain(new[] { "identifier", "displayName", "role" });
    }

    [Fact]
    public void GetSystemSchemas_RegisterSchema_HasRequiredProperties()
    {
        // Act
        var schema = _loader.GetSystemSchema("register");

        // Assert
        schema.Should().NotBeNull();
        schema!.Title.Should().Be("Register");

        var root = schema.Content.RootElement;
        root.TryGetProperty("required", out var required).Should().BeTrue();
        var requiredFields = required.EnumerateArray().Select(x => x.GetString()).ToList();
        requiredFields.Should().Contain(new[] { "identifier", "title" });
    }

    [Fact]
    public void GetSystemSchemas_ReturnsSameInstanceOnMultipleCalls()
    {
        // Act
        var schemas1 = _loader.GetSystemSchemas();
        var schemas2 = _loader.GetSystemSchemas();

        // Assert (lazy loading should return same instance)
        schemas1.Should().BeSameAs(schemas2);
    }

    [Fact]
    public void GetSystemSchemas_AllSchemasHaveInternalSource()
    {
        // Act
        var schemas = _loader.GetSystemSchemas();

        // Assert
        foreach (var schema in schemas)
        {
            schema.Source.Type.Should().Be(SourceType.Internal);
            schema.Source.Uri.Should().BeNull();
            schema.Source.Provider.Should().BeNull();
        }
    }
}
