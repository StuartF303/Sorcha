// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json.Nodes;
using FluentAssertions;
using Sorcha.Blueprint.Engine.Implementation;
using Xunit;

namespace Sorcha.Blueprint.Engine.Tests;

public class JsonEEvaluatorTests
{
    private readonly JsonEEvaluator _evaluator;

    public JsonEEvaluatorTests()
    {
        _evaluator = new JsonEEvaluator();
    }

    [Fact]
    public async Task EvaluateAsync_SimpleVariableSubstitution_ReturnsExpectedValue()
    {
        // Arrange
        var template = JsonNode.Parse(@"{
            ""name"": { ""$eval"": ""userName"" }
        }")!;

        var context = new Dictionary<string, object>
        {
            ["userName"] = "John Doe"
        };

        // Act
        var result = await _evaluator.EvaluateAsync(template, context);

        // Assert
        result.Should().NotBeNull();
        result["name"]?.GetValue<string>().Should().Be("John Doe");
    }

    [Fact]
    public async Task EvaluateAsync_ConditionalRendering_IncludesFieldWhenTrue()
    {
        // Arrange
        var template = JsonNode.Parse(@"{
            ""field"": {
                ""$if"": ""includeField"",
                ""then"": ""included"",
                ""else"": ""excluded""
            }
        }")!;

        var context = new Dictionary<string, object>
        {
            ["includeField"] = true
        };

        // Act
        var result = await _evaluator.EvaluateAsync(template, context);

        // Assert
        result["field"]?.GetValue<string>().Should().Be("included");
    }

    [Fact]
    public async Task EvaluateAsync_ConditionalRendering_ExcludesFieldWhenFalse()
    {
        // Arrange
        var template = JsonNode.Parse(@"{
            ""field"": {
                ""$if"": ""includeField"",
                ""then"": ""included"",
                ""else"": ""excluded""
            }
        }")!;

        var context = new Dictionary<string, object>
        {
            ["includeField"] = false
        };

        // Act
        var result = await _evaluator.EvaluateAsync(template, context);

        // Assert
        result["field"]?.GetValue<string>().Should().Be("excluded");
    }

    [Fact]
    public async Task EvaluateAsync_ArrayMapping_TransformsAllItems()
    {
        // Arrange
        var template = JsonNode.Parse(@"{
            ""items"": {
                ""$map"": { ""$eval"": ""products"" },
                ""each(product)"": {
                    ""name"": { ""$eval"": ""product.name"" },
                    ""price"": { ""$eval"": ""product.price"" }
                }
            }
        }")!;

        var context = new Dictionary<string, object>
        {
            ["products"] = new[]
            {
                new { name = "Widget", price = 9.99 },
                new { name = "Gadget", price = 19.99 }
            }
        };

        // Act
        var result = await _evaluator.EvaluateAsync(template, context);

        // Assert
        var items = result["items"] as JsonArray;
        items.Should().NotBeNull();
        items!.Count.Should().Be(2);
        items[0]!["name"]?.GetValue<string>().Should().Be("Widget");
        items[1]!["name"]?.GetValue<string>().Should().Be("Gadget");
    }

    [Fact]
    public async Task EvaluateAsync_NestedContext_EvaluatesCorrectly()
    {
        // Arrange — use $let to define nested variables, then reference them with $eval
        var template = JsonNode.Parse(@"{
            ""$let"": {
                ""blueprintId"": ""test-001"",
                ""blueprintTitle"": ""Test Blueprint""
            },
            ""in"": {
                ""id"": { ""$eval"": ""blueprintId"" },
                ""title"": { ""$eval"": ""blueprintTitle"" }
            }
        }")!;

        var context = new Dictionary<string, object>();

        // Act
        var result = await _evaluator.EvaluateAsync(template, context);

        // Assert
        result["id"]?.GetValue<string>().Should().Be("test-001");
        result["title"]?.GetValue<string>().Should().Be("Test Blueprint");
    }

    [Fact]
    public async Task EvaluateAsync_NullTemplate_ThrowsArgumentNullException()
    {
        // Arrange
        JsonNode template = null!;
        var context = new Dictionary<string, object>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _evaluator.EvaluateAsync(template, context));
    }

    [Fact]
    public async Task EvaluateAsync_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var template = JsonNode.Parse("{}")!;
        Dictionary<string, object> context = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _evaluator.EvaluateAsync(template, context));
    }

    [Fact]
    public async Task EvaluateAsync_WithGenericType_DeserializesCorrectly()
    {
        // Arrange
        var template = JsonNode.Parse(@"{
            ""id"": { ""$eval"": ""blueprintId"" },
            ""title"": { ""$eval"": ""title"" }
        }")!;

        var context = new Dictionary<string, object>
        {
            ["blueprintId"] = "test-001",
            ["title"] = "Test Blueprint"
        };

        // Act
        var result = await _evaluator.EvaluateAsync<TestBlueprint>(template, context);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("test-001");
        result.Title.Should().Be("Test Blueprint");
    }

    [Fact]
    public async Task ValidateTemplateAsync_ValidTemplate_ReturnsSuccess()
    {
        // Arrange
        var template = JsonNode.Parse(@"{
            ""field"": { ""$eval"": ""value"" }
        }")!;

        // Act
        var result = await _evaluator.ValidateTemplateAsync(template);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateWithTraceAsync_ReturnsTraceInformation()
    {
        // Arrange
        var template = JsonNode.Parse(@"{
            ""name"": { ""$eval"": ""userName"" }
        }")!;

        var context = new Dictionary<string, object>
        {
            ["userName"] = "John Doe"
        };

        // Act
        var trace = await _evaluator.EvaluateWithTraceAsync(template, context);

        // Assert
        trace.Success.Should().BeTrue();
        trace.Result.Should().NotBeNull();
        trace.Steps.Should().HaveCountGreaterThan(0);
        trace.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task EvaluateWithTraceAsync_InvalidTemplate_ReturnsErrorTrace()
    {
        // Arrange - template that will cause evaluation error
        var template = JsonNode.Parse(@"{
            ""value"": { ""$eval"": ""nonexistentVariable"" }
        }")!;

        var context = new Dictionary<string, object>();

        // Act
        var trace = await _evaluator.EvaluateWithTraceAsync(template, context);

        // Assert
        trace.Success.Should().BeFalse();
        trace.Error.Should().NotBeNullOrEmpty();
    }

    private class TestBlueprint
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }
}
