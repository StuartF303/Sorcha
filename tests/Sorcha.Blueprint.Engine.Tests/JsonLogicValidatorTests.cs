// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json.Nodes;
using FluentAssertions;
using JsonSchema.Net;
using Sorcha.Blueprint.Engine.Validation;
using Xunit;

namespace Sorcha.Blueprint.Engine.Tests;

public class JsonLogicValidatorTests
{
    private readonly JsonLogicValidator _validator;

    public JsonLogicValidatorTests()
    {
        _validator = new JsonLogicValidator();
    }

    [Fact]
    public void Validate_SimpleExpression_ReturnsValid()
    {
        // Arrange
        var expression = JsonNode.Parse(@"{
            "">"": [{""var"": ""amount""}, 1000]
        }")!;

        // Act
        var result = _validator.Validate(expression);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithValidSchema_ValidatesVariableReferences()
    {
        // Arrange
        var expression = JsonNode.Parse(@"{
            "">"": [{""var"": ""amount""}, 1000]
        }")!;

        var schema = JsonSchema.FromText(@"{
            ""type"": ""object"",
            ""properties"": {
                ""amount"": { ""type"": ""number"" }
            }
        }");

        // Act
        var result = _validator.Validate(expression, schema);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_InvalidVariableReference_ReturnsError()
    {
        // Arrange
        var expression = JsonNode.Parse(@"{
            "">"": [{""var"": ""nonexistentField""}, 1000]
        }")!;

        var schema = JsonSchema.FromText(@"{
            ""type"": ""object"",
            ""properties"": {
                ""amount"": { ""type"": ""number"" }
            }
        }");

        // Act
        var result = _validator.Validate(expression, schema);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("nonexistentField"));
    }

    [Fact]
    public void Validate_ExceedsMaxDepth_ReturnsError()
    {
        // Arrange - create deeply nested expression
        var expression = JsonNode.Parse(@"{
            ""and"": [
                {""and"": [
                    {""and"": [
                        {""and"": [
                            {""and"": [
                                {""and"": [
                                    {""and"": [
                                        {""and"": [
                                            {""and"": [
                                                {""and"": [
                                                    {""and"": [{"">"": [{""var"": ""a""}, 1]}, true]},
                                                    true
                                                ]},
                                                true
                                            ]},
                                            true
                                        ]},
                                        true
                                    ]},
                                    true
                                ]},
                                true
                            ]},
                            true
                        ]},
                        true
                    ]},
                    true
                ]}
            ]}
        }")!;

        // Act
        var result = _validator.Validate(expression);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("depth"));
    }

    [Fact]
    public void Validate_InvalidOperator_ReturnsError()
    {
        // Arrange
        var expression = JsonNode.Parse(@"{
            ""invalidOp"": [{""var"": ""amount""}, 1000]
        }")!;

        // Act
        var result = _validator.Validate(expression);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Unknown operator"));
    }

    [Fact]
    public void Validate_NullExpression_ReturnsError()
    {
        // Arrange
        JsonNode expression = null!;

        // Act
        var result = _validator.Validate(expression);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("cannot be null"));
    }

    [Fact]
    public void Validate_WithoutSchema_ReturnsWarning()
    {
        // Arrange
        var expression = JsonNode.Parse(@"{
            "">"": [{""var"": ""amount""}, 1000]
        }")!;

        // Act
        var result = _validator.Validate(expression, schema: null);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("No schema provided"));
    }

    [Fact]
    public void CalculateDepth_SimpleExpression_ReturnsCorrectDepth()
    {
        // Arrange
        var expression = JsonNode.Parse(@"{
            "">"": [{""var"": ""amount""}, 1000]
        }")!;

        // Act
        var depth = _validator.CalculateDepth(expression);

        // Assert
        depth.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CountNodes_SimpleExpression_ReturnsCorrectCount()
    {
        // Arrange
        var expression = JsonNode.Parse(@"{
            "">"": [{""var"": ""amount""}, 1000]
        }")!;

        // Act
        var count = _validator.CountNodes(expression);

        // Assert
        count.Should().BeGreaterThan(3); // At least object, array, and values
    }

    [Fact]
    public void ExtractVariables_MultipleVariables_ReturnsAllVariables()
    {
        // Arrange
        var expression = JsonNode.Parse(@"{
            ""and"": [
                {"">"": [{""var"": ""amount""}, 1000]},
                {""=="": [{""var"": ""status""}, ""active""]}
            ]
        }")!;

        // Act
        var variables = _validator.ExtractVariables(expression);

        // Assert
        variables.Should().Contain("amount");
        variables.Should().Contain("status");
        variables.Should().HaveCount(2);
    }

    [Fact]
    public void ExtractVariables_NestedFieldAccess_ReturnsNestedPath()
    {
        // Arrange
        var expression = JsonNode.Parse(@"{
            "">"": [{""var"": ""address.zipCode""}, 10000]
        }")!;

        // Act
        var variables = _validator.ExtractVariables(expression);

        // Assert
        variables.Should().Contain("address.zipCode");
    }

    [Fact]
    public void Validate_ComplexValidExpression_ReturnsValid()
    {
        // Arrange
        var expression = JsonNode.Parse(@"{
            ""if"": [
                {"">"": [{""var"": ""amount""}, 10000]},
                ""director"",
                {"">"": [{""var"": ""amount""}, 5000]},
                ""manager"",
                ""auto-approve""
            ]
        }")!;

        var schema = JsonSchema.FromText(@"{
            ""type"": ""object"",
            ""properties"": {
                ""amount"": { ""type"": ""number"" }
            }
        }");

        // Act
        var result = _validator.Validate(expression, schema);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_AllCommonOperators_ReturnsValid()
    {
        // Arrange - test expression with many valid operators
        var expression = JsonNode.Parse(@"{
            ""and"": [
                {""=="": [{""var"": ""status""}, ""active""]},
                {"">"": [{""var"": ""amount""}, 0]},
                {""in"": [""value"", {""var"": ""list""}]}
            ]
        }")!;

        // Act
        var result = _validator.Validate(expression);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ExceedsNodeCount_ReturnsError()
    {
        // Arrange - create expression with many nodes
        var nodes = new List<object>();
        for (int i = 0; i < 120; i++)
        {
            nodes.Add(new Dictionary<string, object>
            {
                [">"] = new object[] { new Dictionary<string, object> { ["var"] = $"field{i}" }, i }
            });
        }

        var expression = JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(new
        {
            and = nodes
        }))!;

        // Act
        var result = _validator.Validate(expression);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("node count"));
    }
}
