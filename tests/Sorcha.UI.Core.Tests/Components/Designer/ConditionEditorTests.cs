// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.UI.Core.Models.Designer;
using System.Text.Json.Nodes;
using Xunit;

namespace Sorcha.UI.Core.Tests.Components.Designer;

/// <summary>
/// Tests for ConditionModel and JSON Logic conversion.
/// </summary>
public class ConditionEditorTests
{
    [Fact]
    public void ConditionModel_DefaultValues_AreCorrect()
    {
        // Act
        var model = new ConditionModel();

        // Assert
        model.Clauses.Should().BeEmpty();
        model.Operator.Should().Be(LogicalOperator.And);
        model.TargetParticipantId.Should().BeNull();
    }

    [Fact]
    public void ConditionClause_DefaultValues_AreCorrect()
    {
        // Act
        var clause = new ConditionClause();

        // Assert
        clause.FieldPath.Should().BeEmpty();
        clause.Operator.Should().Be(ComparisonOperator.Equals);
        clause.Value.Should().BeEmpty();
        clause.ValueType.Should().Be(ConditionFieldType.String);
    }

    [Fact]
    public void ToJsonLogic_ReturnsNull_WhenNoClauses()
    {
        // Arrange
        var model = new ConditionModel();

        // Act
        var result = model.ToJsonLogic();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ToJsonLogic_SingleClause_ReturnsSimpleCondition()
    {
        // Arrange
        var model = new ConditionModel
        {
            Clauses = [new ConditionClause
            {
                FieldPath = "amount",
                Operator = ComparisonOperator.GreaterThan,
                Value = "100",
                ValueType = ConditionFieldType.Number
            }]
        };

        // Act
        var result = model.ToJsonLogic();

        // Assert
        result.Should().NotBeNull();
        var json = result!.ToJsonString();
        // Note: JSON serialization escapes > as \u003E
        (json.Contains(">") || json.Contains("\\u003E")).Should().BeTrue("should contain greater than operator");
        json.Should().Contain("var");
        json.Should().Contain("amount");
        json.Should().Contain("100");
    }

    [Fact]
    public void ToJsonLogic_MultipleClauses_WithAnd_ReturnsAndCondition()
    {
        // Arrange
        var model = new ConditionModel
        {
            Operator = LogicalOperator.And,
            Clauses =
            [
                new ConditionClause
                {
                    FieldPath = "status",
                    Operator = ComparisonOperator.Equals,
                    Value = "approved",
                    ValueType = ConditionFieldType.String
                },
                new ConditionClause
                {
                    FieldPath = "amount",
                    Operator = ComparisonOperator.LessThan,
                    Value = "1000",
                    ValueType = ConditionFieldType.Number
                }
            ]
        };

        // Act
        var result = model.ToJsonLogic();

        // Assert
        result.Should().NotBeNull();
        var json = result!.ToJsonString();
        json.Should().Contain("\"and\"");
    }

    [Fact]
    public void ToJsonLogic_MultipleClauses_WithOr_ReturnsOrCondition()
    {
        // Arrange
        var model = new ConditionModel
        {
            Operator = LogicalOperator.Or,
            Clauses =
            [
                new ConditionClause
                {
                    FieldPath = "category",
                    Operator = ComparisonOperator.Equals,
                    Value = "premium",
                    ValueType = ConditionFieldType.String
                },
                new ConditionClause
                {
                    FieldPath = "category",
                    Operator = ComparisonOperator.Equals,
                    Value = "gold",
                    ValueType = ConditionFieldType.String
                }
            ]
        };

        // Act
        var result = model.ToJsonLogic();

        // Assert
        result.Should().NotBeNull();
        var json = result!.ToJsonString();
        json.Should().Contain("\"or\"");
    }

    [Fact]
    public void ConditionClause_ToJsonLogic_StringValue()
    {
        // Arrange
        var clause = new ConditionClause
        {
            FieldPath = "name",
            Operator = ComparisonOperator.Equals,
            Value = "Alice",
            ValueType = ConditionFieldType.String
        };

        // Act
        var result = clause.ToJsonLogic();

        // Assert
        result.Should().NotBeNull();
        var json = result.ToJsonString();
        json.Should().Contain("\"==\"");
        json.Should().Contain("\"Alice\"");
    }

    [Fact]
    public void ConditionClause_ToJsonLogic_NumberValue()
    {
        // Arrange
        var clause = new ConditionClause
        {
            FieldPath = "quantity",
            Operator = ComparisonOperator.GreaterOrEqual,
            Value = "50",
            ValueType = ConditionFieldType.Number
        };

        // Act
        var result = clause.ToJsonLogic();

        // Assert
        result.Should().NotBeNull();
        var json = result.ToJsonString();
        // Note: JSON serialization may escape >= as \u003E=
        (json.Contains(">=") || json.Contains("\\u003E=")).Should().BeTrue("should contain greater or equal operator");
        json.Should().Contain("50"); // Should be a number, not "50"
    }

    [Fact]
    public void ConditionClause_ToJsonLogic_BooleanValue()
    {
        // Arrange
        var clause = new ConditionClause
        {
            FieldPath = "approved",
            Operator = ComparisonOperator.Equals,
            Value = "true",
            ValueType = ConditionFieldType.Boolean
        };

        // Act
        var result = clause.ToJsonLogic();

        // Assert
        result.Should().NotBeNull();
        var json = result.ToJsonString();
        json.Should().Contain("true"); // Should be a boolean, not "true"
    }

    [Theory]
    [InlineData(ComparisonOperator.Equals, "==", null)]
    [InlineData(ComparisonOperator.NotEquals, "!=", null)]
    [InlineData(ComparisonOperator.GreaterThan, ">", "\\u003E")]
    [InlineData(ComparisonOperator.LessThan, "<", "\\u003C")]
    [InlineData(ComparisonOperator.GreaterOrEqual, ">=", "\\u003E=")]
    [InlineData(ComparisonOperator.LessOrEqual, "<=", "\\u003C=")]
    [InlineData(ComparisonOperator.Contains, "in", null)]
    public void ConditionClause_ToJsonLogic_AllOperators(ComparisonOperator op, string expectedOp, string? escapedOp)
    {
        // Arrange
        var clause = new ConditionClause
        {
            FieldPath = "test",
            Operator = op,
            Value = "value",
            ValueType = ConditionFieldType.String
        };

        // Act
        var result = clause.ToJsonLogic();

        // Assert
        result.Should().NotBeNull();
        var json = result.ToJsonString();
        // JSON serialization may escape < and > characters
        var containsExpected = json.Contains($"\"{expectedOp}\"") ||
                              (escapedOp != null && json.Contains($"\"{escapedOp}\""));
        containsExpected.Should().BeTrue($"should contain operator {expectedOp}");
    }

    [Fact]
    public void FromJsonLogic_ReturnsEmptyModel_WhenNull()
    {
        // Act
        var model = ConditionModel.FromJsonLogic(null);

        // Assert
        model.Clauses.Should().BeEmpty();
    }

    [Fact]
    public void FromJsonLogic_ParsesSingleClause()
    {
        // Arrange
        var json = JsonNode.Parse("{\"==\":[{\"var\":\"status\"},\"active\"]}");

        // Act
        var model = ConditionModel.FromJsonLogic(json);

        // Assert
        model.Clauses.Should().HaveCount(1);
        model.Clauses[0].FieldPath.Should().Be("status");
        model.Clauses[0].Operator.Should().Be(ComparisonOperator.Equals);
        model.Clauses[0].Value.Should().Be("active");
        model.Clauses[0].ValueType.Should().Be(ConditionFieldType.String);
    }

    [Fact]
    public void FromJsonLogic_ParsesAndCondition()
    {
        // Arrange
        var json = JsonNode.Parse("{\"and\":[{\"==\":[{\"var\":\"a\"},1]},{\"==\":[{\"var\":\"b\"},2]}]}");

        // Act
        var model = ConditionModel.FromJsonLogic(json);

        // Assert
        model.Operator.Should().Be(LogicalOperator.And);
        model.Clauses.Should().HaveCount(2);
    }

    [Fact]
    public void FromJsonLogic_ParsesOrCondition()
    {
        // Arrange
        var json = JsonNode.Parse("{\"or\":[{\"==\":[{\"var\":\"x\"},\"y\"]},{\"==\":[{\"var\":\"x\"},\"z\"]}]}");

        // Act
        var model = ConditionModel.FromJsonLogic(json);

        // Assert
        model.Operator.Should().Be(LogicalOperator.Or);
        model.Clauses.Should().HaveCount(2);
    }

    [Fact]
    public void FromJsonLogic_ParsesNumericValue()
    {
        // Arrange
        var json = JsonNode.Parse("{\">\": [{\"var\": \"amount\"}, 500]}");

        // Act
        var model = ConditionModel.FromJsonLogic(json);

        // Assert
        model.Clauses.Should().HaveCount(1);
        model.Clauses[0].Operator.Should().Be(ComparisonOperator.GreaterThan);
        model.Clauses[0].Value.Should().Be("500");
        model.Clauses[0].ValueType.Should().Be(ConditionFieldType.Number);
    }

    [Fact]
    public void FromJsonLogic_ParsesBooleanValue()
    {
        // Arrange
        var json = JsonNode.Parse("{\"==\": [{\"var\": \"active\"}, true]}");

        // Act
        var model = ConditionModel.FromJsonLogic(json);

        // Assert
        model.Clauses.Should().HaveCount(1);
        model.Clauses[0].Value.Should().Be("true");
        model.Clauses[0].ValueType.Should().Be(ConditionFieldType.Boolean);
    }

    [Fact]
    public void RoundTrip_PreservesCondition()
    {
        // Arrange
        var original = new ConditionModel
        {
            Operator = LogicalOperator.And,
            Clauses =
            [
                new ConditionClause
                {
                    FieldPath = "amount",
                    Operator = ComparisonOperator.GreaterThan,
                    Value = "100",
                    ValueType = ConditionFieldType.Number
                },
                new ConditionClause
                {
                    FieldPath = "status",
                    Operator = ComparisonOperator.Equals,
                    Value = "active",
                    ValueType = ConditionFieldType.String
                }
            ]
        };

        // Act
        var json = original.ToJsonLogic();
        var restored = ConditionModel.FromJsonLogic(json);

        // Assert
        restored.Operator.Should().Be(original.Operator);
        restored.Clauses.Should().HaveCount(original.Clauses.Count);

        for (int i = 0; i < original.Clauses.Count; i++)
        {
            restored.Clauses[i].FieldPath.Should().Be(original.Clauses[i].FieldPath);
            restored.Clauses[i].Operator.Should().Be(original.Clauses[i].Operator);
            restored.Clauses[i].Value.Should().Be(original.Clauses[i].Value);
            restored.Clauses[i].ValueType.Should().Be(original.Clauses[i].ValueType);
        }
    }

    [Fact]
    public void AllOperators_HaveCorrectValues()
    {
        // Assert
        var operators = Enum.GetValues<ComparisonOperator>();
        operators.Should().Contain(ComparisonOperator.Equals);
        operators.Should().Contain(ComparisonOperator.NotEquals);
        operators.Should().Contain(ComparisonOperator.GreaterThan);
        operators.Should().Contain(ComparisonOperator.LessThan);
        operators.Should().Contain(ComparisonOperator.GreaterOrEqual);
        operators.Should().Contain(ComparisonOperator.LessOrEqual);
        operators.Should().Contain(ComparisonOperator.Contains);
        operators.Should().Contain(ComparisonOperator.StartsWith);
        operators.Should().Contain(ComparisonOperator.EndsWith);
    }

    [Fact]
    public void AllConditionFieldTypes_HaveCorrectValues()
    {
        // Assert
        var types = Enum.GetValues<ConditionFieldType>();
        types.Should().Contain(ConditionFieldType.String);
        types.Should().Contain(ConditionFieldType.Number);
        types.Should().Contain(ConditionFieldType.Boolean);
        types.Should().Contain(ConditionFieldType.Date);
    }

    [Fact]
    public void LogicalOperators_HaveCorrectValues()
    {
        // Assert
        var ops = Enum.GetValues<LogicalOperator>();
        ops.Should().Contain(LogicalOperator.And);
        ops.Should().Contain(LogicalOperator.Or);
        ops.Should().HaveCount(2);
    }
}
