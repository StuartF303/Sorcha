// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Sorcha.Blueprint.Engine.Implementation;
using Sorcha.Blueprint.Models;
using Sorcha.Blueprint.Service.Templates;
using Xunit;

namespace Sorcha.Blueprint.Engine.Tests;

public class BlueprintTemplateServiceTests
{
    private readonly BlueprintTemplateService _service;

    public BlueprintTemplateServiceTests()
    {
        var evaluator = new JsonEEvaluator();
        var logger = NullLogger<BlueprintTemplateService>.Instance;
        _service = new BlueprintTemplateService(evaluator, logger);
    }

    [Fact]
    public async Task SaveTemplateAsync_NewTemplate_SavesSuccessfully()
    {
        // Arrange
        var template = CreateSimpleTemplate();

        // Act
        var saved = await _service.SaveTemplateAsync(template);

        // Assert
        saved.Should().NotBeNull();
        saved.Id.Should().Be(template.Id);
        saved.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetTemplateAsync_ExistingTemplate_ReturnsTemplate()
    {
        // Arrange
        var template = CreateSimpleTemplate();
        await _service.SaveTemplateAsync(template);

        // Act
        var retrieved = await _service.GetTemplateAsync(template.Id);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be(template.Id);
        retrieved.Title.Should().Be(template.Title);
    }

    [Fact]
    public async Task GetTemplateAsync_NonExistentTemplate_ReturnsNull()
    {
        // Act
        var retrieved = await _service.GetTemplateAsync("nonexistent-id");

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTemplateAsync_ExistingTemplate_ReturnsTrue()
    {
        // Arrange
        var template = CreateSimpleTemplate();
        await _service.SaveTemplateAsync(template);

        // Act
        var deleted = await _service.DeleteTemplateAsync(template.Id);

        // Assert
        deleted.Should().BeTrue();

        var retrieved = await _service.GetTemplateAsync(template.Id);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task DeleteTemplateAsync_NonExistentTemplate_ReturnsFalse()
    {
        // Act
        var deleted = await _service.DeleteTemplateAsync("nonexistent-id");

        // Assert
        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetPublishedTemplatesAsync_ReturnsOnlyPublished()
    {
        // Arrange
        var publishedTemplate = CreateSimpleTemplate();
        publishedTemplate.Id = "published-1";
        publishedTemplate.Published = true;

        var unpublishedTemplate = CreateSimpleTemplate();
        unpublishedTemplate.Id = "unpublished-1";
        unpublishedTemplate.Published = false;

        await _service.SaveTemplateAsync(publishedTemplate);
        await _service.SaveTemplateAsync(unpublishedTemplate);

        // Act
        var published = await _service.GetPublishedTemplatesAsync();

        // Assert
        var publishedList = published.ToList();
        publishedList.Should().HaveCount(1);
        publishedList.First().Id.Should().Be("published-1");
    }

    [Fact]
    public async Task GetTemplatesByCategoryAsync_ReturnsMatchingTemplates()
    {
        // Arrange
        var financeTemplate = CreateSimpleTemplate();
        financeTemplate.Id = "finance-1";
        financeTemplate.Category = "finance";
        financeTemplate.Published = true;

        var supplyChainTemplate = CreateSimpleTemplate();
        supplyChainTemplate.Id = "supply-chain-1";
        supplyChainTemplate.Category = "supply-chain";
        supplyChainTemplate.Published = true;

        await _service.SaveTemplateAsync(financeTemplate);
        await _service.SaveTemplateAsync(supplyChainTemplate);

        // Act
        var financeTemplates = await _service.GetTemplatesByCategoryAsync("finance");

        // Assert
        var financeList = financeTemplates.ToList();
        financeList.Should().HaveCount(1);
        financeList.First().Id.Should().Be("finance-1");
    }

    [Fact]
    public async Task EvaluateTemplateAsync_ValidTemplate_ReturnsBlueprint()
    {
        // Arrange
        var template = CreateSimpleTemplate();
        await _service.SaveTemplateAsync(template);

        var request = new TemplateEvaluationRequest
        {
            TemplateId = template.Id,
            Parameters = new Dictionary<string, object>
            {
                ["blueprintId"] = "test-blueprint",
                ["blueprintTitle"] = "Test Blueprint"
            },
            Validate = true
        };

        // Act
        var result = await _service.EvaluateTemplateAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Blueprint.Should().NotBeNull();
        result.Blueprint!.Id.Should().Be("test-blueprint");
        result.Blueprint.Title.Should().Be("Test Blueprint");
    }

    [Fact]
    public async Task EvaluateTemplateAsync_NonExistentTemplate_ReturnsError()
    {
        // Arrange
        var request = new TemplateEvaluationRequest
        {
            TemplateId = "nonexistent-template",
            Parameters = new Dictionary<string, object>()
        };

        // Act
        var result = await _service.EvaluateTemplateAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task EvaluateTemplateAsync_WithDefaultParameters_MergesCorrectly()
    {
        // Arrange
        var template = CreateSimpleTemplate();
        template.DefaultParameters = new Dictionary<string, object>
        {
            ["blueprintId"] = "default-id",
            ["blueprintTitle"] = "Default Title",
            ["description"] = "Default Description"
        };
        await _service.SaveTemplateAsync(template);

        var request = new TemplateEvaluationRequest
        {
            TemplateId = template.Id,
            Parameters = new Dictionary<string, object>
            {
                ["blueprintTitle"] = "Override Title" // Override default
            },
            Validate = false
        };

        // Act
        var result = await _service.EvaluateTemplateAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Blueprint!.Id.Should().Be("default-id"); // From defaults
        result.Blueprint.Title.Should().Be("Override Title"); // Overridden
    }

    [Fact]
    public async Task EvaluateTemplateAsync_WithTrace_ReturnsTraceInformation()
    {
        // Arrange
        var template = CreateSimpleTemplate();
        await _service.SaveTemplateAsync(template);

        var request = new TemplateEvaluationRequest
        {
            TemplateId = template.Id,
            Parameters = new Dictionary<string, object>
            {
                ["blueprintId"] = "test-id",
                ["blueprintTitle"] = "Test Title"
            },
            IncludeTrace = true,
            Validate = false
        };

        // Act
        var result = await _service.EvaluateTemplateAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.Trace.Should().NotBeNull();
        result.Trace.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task ValidateParametersAsync_ValidParameters_ReturnsSuccess()
    {
        // Arrange
        var template = CreateTemplateWithParameterSchema();
        await _service.SaveTemplateAsync(template);

        var parameters = new Dictionary<string, object>
        {
            ["blueprintId"] = "valid-id",
            ["blueprintTitle"] = "Valid Title"
        };

        // Act
        var result = await _service.ValidateParametersAsync(template.Id, parameters);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateParametersAsync_MissingRequiredParameter_ReturnsError()
    {
        // Arrange
        var template = CreateTemplateWithParameterSchema();
        await _service.SaveTemplateAsync(template);

        var parameters = new Dictionary<string, object>
        {
            // Missing blueprintId which is required
            ["blueprintTitle"] = "Valid Title"
        };

        // Act
        var result = await _service.ValidateParametersAsync(template.Id, parameters);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task EvaluateExampleAsync_ValidExample_ReturnsBlueprint()
    {
        // Arrange
        var template = CreateTemplateWithExamples();
        await _service.SaveTemplateAsync(template);

        // Act
        var result = await _service.EvaluateExampleAsync(template.Id, "standard");

        // Assert
        result.Success.Should().BeTrue();
        result.Blueprint.Should().NotBeNull();
    }

    [Fact]
    public async Task EvaluateExampleAsync_NonExistentExample_ReturnsError()
    {
        // Arrange
        var template = CreateTemplateWithExamples();
        await _service.SaveTemplateAsync(template);

        // Act
        var result = await _service.EvaluateExampleAsync(template.Id, "nonexistent");

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    private BlueprintTemplate CreateSimpleTemplate()
    {
        return new BlueprintTemplate
        {
            Id = "test-template-" + Guid.NewGuid().ToString("N"),
            Title = "Test Template",
            Description = "A simple test template",
            Version = 1,
            Template = JsonNode.Parse(@"{
                ""$eval"": ""blueprint"",
                ""context"": {
                    ""blueprint"": {
                        ""id"": { ""$eval"": ""blueprintId"" },
                        ""title"": { ""$eval"": ""blueprintTitle"" },
                        ""description"": ""Test blueprint"",
                        ""version"": 1,
                        ""participants"": [
                            { ""id"": ""participant1"", ""name"": ""Participant 1"" },
                            { ""id"": ""participant2"", ""name"": ""Participant 2"" }
                        ],
                        ""actions"": [
                            { ""id"": 0, ""title"": ""Action 1"", ""sender"": ""participant1"" }
                        ]
                    },
                    ""blueprintId"": { ""$eval"": ""blueprintId"" },
                    ""blueprintTitle"": { ""$eval"": ""blueprintTitle"" }
                }
            }")!,
            Published = false
        };
    }

    private BlueprintTemplate CreateTemplateWithParameterSchema()
    {
        var template = CreateSimpleTemplate();
        template.ParameterSchema = JsonDocument.Parse(@"{
            ""type"": ""object"",
            ""properties"": {
                ""blueprintId"": { ""type"": ""string"", ""minLength"": 3 },
                ""blueprintTitle"": { ""type"": ""string"", ""minLength"": 3 }
            },
            ""required"": [""blueprintId"", ""blueprintTitle""]
        }");
        return template;
    }

    private BlueprintTemplate CreateTemplateWithExamples()
    {
        var template = CreateSimpleTemplate();
        template.Examples = new List<TemplateExample>
        {
            new TemplateExample
            {
                Name = "standard",
                Description = "Standard example",
                Parameters = new Dictionary<string, object>
                {
                    ["blueprintId"] = "example-1",
                    ["blueprintTitle"] = "Example Blueprint"
                }
            }
        };
        return template;
    }
}
