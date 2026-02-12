// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json.Nodes;
using Sorcha.Blueprint.Engine.Implementation;
using Sorcha.Blueprint.Engine.Interfaces;
using Sorcha.Blueprint.Engine.Models;

namespace Sorcha.Blueprint.Engine.Tests;

/// <summary>
/// Unit tests for JsonLogicEvaluator.
/// </summary>
public class JsonLogicEvaluatorTests
{
    private readonly IJsonLogicEvaluator _evaluator;

    public JsonLogicEvaluatorTests()
    {
        _evaluator = new JsonLogicEvaluator();
    }

    #region Comparison Operators

    [Fact]
    public void Evaluate_Equality_ReturnsCorrect()
    {
        // Arrange
        var expression = JsonNode.Parse("""{"==": [1, 1]}""");
        var data = new Dictionary<string, object>();

        // Act
        var result = _evaluator.Evaluate(expression!, data);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void Evaluate_Inequality_ReturnsCorrect()
    {
        // Arrange
        var expression = JsonNode.Parse("""{"!=": [1, 2]}""");
        var data = new Dictionary<string, object>();

        // Act
        var result = _evaluator.Evaluate(expression!, data);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void Evaluate_LessThan_ReturnsCorrect()
    {
        // Arrange
        var expression = JsonNode.Parse("""{"<": [5, 10]}""");
        var data = new Dictionary<string, object>();

        // Act
        var result = _evaluator.Evaluate(expression!, data);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void Evaluate_GreaterThan_ReturnsCorrect()
    {
        // Arrange
        var expression = JsonNode.Parse("""{">": [10, 5]}""");
        var data = new Dictionary<string, object>();

        // Act
        var result = _evaluator.Evaluate(expression!, data);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void Evaluate_LessThanOrEqual_ReturnsCorrect()
    {
        // Arrange
        var expression = JsonNode.Parse("""{"<=": [5, 5]}""");
        var data = new Dictionary<string, object>();

        // Act
        var result = _evaluator.Evaluate(expression!, data);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void Evaluate_GreaterThanOrEqual_ReturnsCorrect()
    {
        // Arrange
        var expression = JsonNode.Parse("""{">=": [10, 5]}""");
        var data = new Dictionary<string, object>();

        // Act
        var result = _evaluator.Evaluate(expression!, data);

        // Assert
        result.Should().Be(true);
    }

    #endregion

    #region Logical Operators

    [Fact]
    public void Evaluate_And_BothTrue_ReturnsTrue()
    {
        // Arrange
        var expression = JsonNode.Parse("""{"and": [true, true]}""");
        var data = new Dictionary<string, object>();

        // Act
        var result = _evaluator.Evaluate(expression!, data);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void Evaluate_And_OneFalse_ReturnsFalse()
    {
        // Arrange
        var expression = JsonNode.Parse("""{"and": [true, false]}""");
        var data = new Dictionary<string, object>();

        // Act
        var result = _evaluator.Evaluate(expression!, data);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void Evaluate_Or_OneTrue_ReturnsTrue()
    {
        // Arrange
        var expression = JsonNode.Parse("""{"or": [false, true]}""");
        var data = new Dictionary<string, object>();

        // Act
        var result = _evaluator.Evaluate(expression!, data);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void Evaluate_Not_True_ReturnsFalse()
    {
        // Arrange
        var expression = JsonNode.Parse("""{"!": [true]}""");
        var data = new Dictionary<string, object>();

        // Act
        var result = _evaluator.Evaluate(expression!, data);

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void Evaluate_Not_False_ReturnsTrue()
    {
        // Arrange
        var expression = JsonNode.Parse("""{"!": [false]}""");
        var data = new Dictionary<string, object>();

        // Act
        var result = _evaluator.Evaluate(expression!, data);

        // Assert
        result.Should().Be(true);
    }

    #endregion

    #region Arithmetic Operators

    [Fact]
    public void Evaluate_Addition_ReturnsSum()
    {
        // Arrange
        var expression = JsonNode.Parse("""{"+": [5, 3, 2]}""");
        var data = new Dictionary<string, object>();

        // Act
        var result = _evaluator.Evaluate(expression!, data);

        // Assert
        Convert.ToDouble(result).Should().Be(10);
    }

    [Fact]
    public void Evaluate_Subtraction_ReturnsDifference()
    {
        // Arrange
        var expression = JsonNode.Parse("""{"-": [10, 3]}""");
        var data = new Dictionary<string, object>();

        // Act
        var result = _evaluator.Evaluate(expression!, data);

        // Assert
        Convert.ToDouble(result).Should().Be(7);
    }

    [Fact]
    public void Evaluate_Multiplication_ReturnsProduct()
    {
        // Arrange
        var expression = JsonNode.Parse("""{"*": [5, 3]}""");
        var data = new Dictionary<string, object>();

        // Act
        var result = _evaluator.Evaluate(expression!, data);

        // Assert
        Convert.ToDouble(result).Should().Be(15);
    }

    [Fact]
    public void Evaluate_Division_ReturnsQuotient()
    {
        // Arrange
        var expression = JsonNode.Parse("""{"/": [10, 2]}""");
        var data = new Dictionary<string, object>();

        // Act
        var result = _evaluator.Evaluate(expression!, data);

        // Assert
        Convert.ToDouble(result).Should().Be(5);
    }

    [Fact]
    public void Evaluate_Modulo_ReturnsRemainder()
    {
        // Arrange
        var expression = JsonNode.Parse("""{"%": [10, 3]}""");
        var data = new Dictionary<string, object>();

        // Act
        var result = _evaluator.Evaluate(expression!, data);

        // Assert
        Convert.ToDouble(result).Should().Be(1);
    }

    #endregion

    #region Variable References

    [Fact]
    public void Evaluate_VariableReference_ReturnsValue()
    {
        // Arrange
        var expression = JsonNode.Parse("""{"var": "name"}""");
        var data = new Dictionary<string, object>
        {
            ["name"] = "Alice"
        };

        // Act
        var result = _evaluator.Evaluate(expression!, data);

        // Assert
        result.Should().Be("Alice");
    }

    [Fact]
    public void Evaluate_NestedVariableReference_ReturnsValue()
    {
        // Arrange
        var expression = JsonNode.Parse("""{"var": "user.name"}""");
        var data = new Dictionary<string, object>
        {
            ["user"] = new Dictionary<string, object>
            {
                ["name"] = "Bob"
            }
        };

        // Act
        var result = _evaluator.Evaluate(expression!, data);

        // Assert
        result.Should().Be("Bob");
    }

    [Fact]
    public void Evaluate_VariableInExpression_Works()
    {
        // Arrange
        var expression = JsonNode.Parse("""{"*": [{"var": "quantity"}, {"var": "price"}]}""");
        var data = new Dictionary<string, object>
        {
            ["quantity"] = 5,
            ["price"] = 10.5
        };

        // Act
        var result = _evaluator.Evaluate(expression!, data);

        // Assert
        Convert.ToDouble(result).Should().Be(52.5);
    }

    #endregion

    #region Complex Expressions

    [Fact]
    public void Evaluate_NestedExpression_Works()
    {
        // Arrange: (quantity * price) > 100
        var expression = JsonNode.Parse("""
        {
            ">": [
                {"*": [{"var": "quantity"}, {"var": "price"}]},
                100
            ]
        }
        """);
        var data = new Dictionary<string, object>
        {
            ["quantity"] = 10,
            ["price"] = 15
        };

        // Act
        var result = _evaluator.Evaluate(expression!, data);

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void Evaluate_IfCondition_ReturnsCorrectBranch()
    {
        // Arrange: if age >= 18 then "adult" else "minor"
        var expression = JsonNode.Parse("""
        {
            "if": [
                {">=": [{"var": "age"}, 18]},
                "adult",
                "minor"
            ]
        }
        """);

        var adultData = new Dictionary<string, object> { ["age"] = 25 };
        var minorData = new Dictionary<string, object> { ["age"] = 15 };

        // Act
        var adultResult = _evaluator.Evaluate(expression!, adultData);
        var minorResult = _evaluator.Evaluate(expression!, minorData);

        // Assert
        adultResult.Should().Be("adult");
        minorResult.Should().Be("minor");
    }

    [Fact]
    public void Evaluate_ComplexCondition_Works()
    {
        // Arrange: (age >= 18 AND country == "US") OR isAdmin == true
        var expression = JsonNode.Parse("""
        {
            "or": [
                {
                    "and": [
                        {">=": [{"var": "age"}, 18]},
                        {"==": [{"var": "country"}, "US"]}
                    ]
                },
                {"==": [{"var": "isAdmin"}, true]}
            ]
        }
        """);

        var validUser = new Dictionary<string, object>
        {
            ["age"] = 25,
            ["country"] = "US",
            ["isAdmin"] = false
        };

        var adminUser = new Dictionary<string, object>
        {
            ["age"] = 15,
            ["country"] = "UK",
            ["isAdmin"] = true
        };

        // Act
        var validResult = _evaluator.Evaluate(expression!, validUser);
        var adminResult = _evaluator.Evaluate(expression!, adminUser);

        // Assert
        validResult.Should().Be(true);
        adminResult.Should().Be(true);
    }

    #endregion

    #region ApplyCalculationsAsync

    [Fact]
    public async Task ApplyCalculationsAsync_SingleCalculation_AddsField()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["quantity"] = 5,
            ["unitPrice"] = 10.0
        };

        var calculations = new[]
        {
            Calculation.Create(
                "totalPrice",
                JsonNode.Parse("""{"*": [{"var": "quantity"}, {"var": "unitPrice"}]}""")!
            )
        };

        // Act
        var result = await _evaluator.ApplyCalculationsAsync(data, calculations);

        // Assert
        result.Should().ContainKey("quantity");
        result.Should().ContainKey("unitPrice");
        result.Should().ContainKey("totalPrice");
        Convert.ToDouble(result["totalPrice"]).Should().Be(50.0);
    }

    [Fact]
    public async Task ApplyCalculationsAsync_MultipleCalculations_AppliesInOrder()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["basePrice"] = 100.0,
            ["taxRate"] = 0.1
        };

        var calculations = new[]
        {
            // First calculate tax amount
            Calculation.Create(
                "taxAmount",
                JsonNode.Parse("""{"*": [{"var": "basePrice"}, {"var": "taxRate"}]}""")!
            ),
            // Then calculate total using the tax amount
            Calculation.Create(
                "total",
                JsonNode.Parse("""{"+": [{"var": "basePrice"}, {"var": "taxAmount"}]}""")!
            )
        };

        // Act
        var result = await _evaluator.ApplyCalculationsAsync(data, calculations);

        // Assert
        result.Should().ContainKey("basePrice");
        result.Should().ContainKey("taxRate");
        result.Should().ContainKey("taxAmount");
        result.Should().ContainKey("total");
        Convert.ToDouble(result["taxAmount"]).Should().Be(10.0);
        Convert.ToDouble(result["total"]).Should().Be(110.0);
    }

    [Fact]
    public async Task ApplyCalculationsAsync_DependentCalculations_Works()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["price"] = 100.0,
            ["quantity"] = 5
        };

        var calculations = new[]
        {
            // subtotal = price * quantity
            Calculation.Create(
                "subtotal",
                JsonNode.Parse("""{"*": [{"var": "price"}, {"var": "quantity"}]}""")!
            ),
            // discount = subtotal * 0.1
            Calculation.Create(
                "discount",
                JsonNode.Parse("""{"*": [{"var": "subtotal"}, 0.1]}""")!
            ),
            // total = subtotal - discount
            Calculation.Create(
                "total",
                JsonNode.Parse("""{"-": [{"var": "subtotal"}, {"var": "discount"}]}""")!
            )
        };

        // Act
        var result = await _evaluator.ApplyCalculationsAsync(data, calculations);

        // Assert
        Convert.ToDouble(result["subtotal"]).Should().Be(500.0);
        Convert.ToDouble(result["discount"]).Should().Be(50.0);
        Convert.ToDouble(result["total"]).Should().Be(450.0);
    }

    [Fact]
    public async Task ApplyCalculationsAsync_EmptyCalculations_ReturnsOriginalData()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["value"] = 42
        };
        var calculations = Array.Empty<Calculation>();

        // Act
        var result = await _evaluator.ApplyCalculationsAsync(data, calculations);

        // Assert
        result.Should().ContainKey("value");
        result["value"].Should().Be(42);
    }

    #endregion

    #region EvaluateConditionsAsync

    [Fact]
    public async Task EvaluateConditionsAsync_FirstConditionMatches_ReturnsParticipant()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["amount"] = 15000
        };

        var conditions = new[]
        {
            new Condition("manager", new List<string> { """{">=": [{"var": "amount"}, 10000]}""" }),
            new Condition("clerk", new List<string> { """{"<": [{"var": "amount"}, 10000]}""" })
        };

        // Act
        var result = await _evaluator.EvaluateConditionsAsync(data, conditions);

        // Assert
        result.Should().Be("manager");
    }

    [Fact]
    public async Task EvaluateConditionsAsync_SecondConditionMatches_ReturnsParticipant()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["amount"] = 5000
        };

        var conditions = new[]
        {
            new Condition("manager", new List<string> { """{">=": [{"var": "amount"}, 10000]}""" }),
            new Condition("clerk", new List<string> { """{"<": [{"var": "amount"}, 10000]}""" })
        };

        // Act
        var result = await _evaluator.EvaluateConditionsAsync(data, conditions);

        // Assert
        result.Should().Be("clerk");
    }

    [Fact]
    public async Task EvaluateConditionsAsync_NoConditionsMatch_ReturnsNull()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["amount"] = 5000
        };

        var conditions = new[]
        {
            new Condition("vip", new List<string> { """{">=": [{"var": "amount"}, 100000]}""" })
        };

        // Act
        var result = await _evaluator.EvaluateConditionsAsync(data, conditions);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task EvaluateConditionsAsync_MultipleCriteriaAllMatch_ReturnsParticipant()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["age"] = 25,
            ["country"] = "US"
        };

        var conditions = new[]
        {
            new Condition("adult-us", new List<string>
            {
                """{">=": [{"var": "age"}, 18]}""",
                """{"==": [{"var": "country"}, "US"]}"""
            })
        };

        // Act
        var result = await _evaluator.EvaluateConditionsAsync(data, conditions);

        // Assert
        result.Should().Be("adult-us");
    }

    [Fact]
    public async Task EvaluateConditionsAsync_OneCriteriaFails_ContinuesToNextCondition()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["age"] = 25,
            ["country"] = "UK"
        };

        var conditions = new[]
        {
            new Condition("adult-us", new List<string>
            {
                """{">=": [{"var": "age"}, 18]}""",
                """{"==": [{"var": "country"}, "US"]}"""
            }),
            new Condition("adult-other", new List<string>
            {
                """{">=": [{"var": "age"}, 18]}"""
            })
        };

        // Act
        var result = await _evaluator.EvaluateConditionsAsync(data, conditions);

        // Assert
        result.Should().Be("adult-other");
    }

    [Fact]
    public async Task EvaluateConditionsAsync_EmptyConditions_ReturnsNull()
    {
        // Arrange
        var data = new Dictionary<string, object>();
        var conditions = Array.Empty<Condition>();

        // Act
        var result = await _evaluator.EvaluateConditionsAsync(data, conditions);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Error Handling

    [Fact]
    public void Evaluate_NullExpression_ThrowsArgumentNullException()
    {
        // Arrange
        var data = new Dictionary<string, object>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _evaluator.Evaluate(null!, data));
    }

    [Fact]
    public void Evaluate_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        var expression = JsonNode.Parse("""{"==": [1, 1]}""");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _evaluator.Evaluate(expression!, null!));
    }

    [Fact]
    public async Task ApplyCalculationsAsync_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        var calculations = Array.Empty<Calculation>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _evaluator.ApplyCalculationsAsync(null!, calculations)
        );
    }

    [Fact]
    public async Task ApplyCalculationsAsync_NullCalculations_ThrowsArgumentNullException()
    {
        // Arrange
        var data = new Dictionary<string, object>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _evaluator.ApplyCalculationsAsync(data, null!)
        );
    }

    [Fact]
    public async Task EvaluateConditionsAsync_NullData_ThrowsArgumentNullException()
    {
        // Arrange
        var conditions = Array.Empty<Condition>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _evaluator.EvaluateConditionsAsync(null!, conditions)
        );
    }

    [Fact]
    public async Task EvaluateConditionsAsync_NullConditions_ThrowsArgumentNullException()
    {
        // Arrange
        var data = new Dictionary<string, object>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _evaluator.EvaluateConditionsAsync(data, null!)
        );
    }

    #endregion
}
