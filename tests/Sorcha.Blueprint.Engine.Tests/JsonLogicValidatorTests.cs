// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json.Nodes;
using FluentAssertions;
using Json.Schema;
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
        var result = _validator.Validate(expression, dataSchema: null);

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

    #region 6.1 Action Condition Validation

    [Fact]
    public void Validate_DefaultActionCondition_ReturnsValid()
    {
        // Arrange - Default action condition {\"==\":[0,0]} always evaluates to true
        var defaultCondition = JsonNode.Parse(@"{""=="": [0, 0]}")!;

        // Act
        var result = _validator.Validate(defaultCondition);

        // Assert
        result.IsValid.Should().BeTrue("Default condition {\"==\":[0,0]} should be valid");
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ActionConditionAlwaysSameAction_ReturnsValid()
    {
        // Arrange - Condition that always returns the same action ID
        var alwaysAction1 = JsonNode.Parse(@"{""=="": [1, 1]}")!;

        // Act
        var result = _validator.Validate(alwaysAction1);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ActionConditionUsingDataFields_ReturnsValid()
    {
        // Arrange - Condition that routes based on data field
        var dataFieldCondition = JsonNode.Parse(@"{
            ""if"": [
                {""=="": [{""var"": ""approved""}, true]},
                1,
                {""=="": [{""var"": ""approved""}, false]},
                2,
                0
            ]
        }")!;

        var schema = JsonSchema.FromText(@"{
            ""type"": ""object"",
            ""properties"": {
                ""approved"": { ""type"": ""boolean"" }
            }
        }");

        // Act
        var result = _validator.Validate(dataFieldCondition, schema);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ActionConditionWithComplexLogic_ReturnsValid()
    {
        // Arrange - Multi-condition routing logic
        var complexCondition = JsonNode.Parse(@"{
            ""if"": [
                {""and"": [
                    {"">"": [{""var"": ""amount""}, 10000]},
                    {""=="": [{""var"": ""status""}, ""pending""]}
                ]},
                3,
                {"">"": [{""var"": ""amount""}, 5000]},
                2,
                1
            ]
        }")!;

        var schema = JsonSchema.FromText(@"{
            ""type"": ""object"",
            ""properties"": {
                ""amount"": { ""type"": ""number"" },
                ""status"": { ""type"": ""string"" }
            }
        }");

        // Act
        var result = _validator.Validate(complexCondition, schema);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region 6.2 Participant Routing Conditions

    [Fact]
    public void Validate_ParticipantRoutingCondition_ReturnsValid()
    {
        // Arrange - Routing condition that selects participant based on role
        var routingCondition = JsonNode.Parse(@"{
            ""if"": [
                {""=="": [{""var"": ""role""}, ""manager""]},
                ""participant-manager"",
                {""=="": [{""var"": ""role""}, ""director""]},
                ""participant-director"",
                ""participant-default""
            ]
        }")!;

        var schema = JsonSchema.FromText(@"{
            ""type"": ""object"",
            ""properties"": {
                ""role"": { ""type"": ""string"" }
            }
        }");

        // Act
        var result = _validator.Validate(routingCondition, schema);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ParticipantRoutingInvalidCondition_ReturnsError()
    {
        // Arrange - Invalid operator in participant routing
        var invalidRouting = JsonNode.Parse(@"{
            ""invalidRoutingOp"": [{""var"": ""role""}, ""manager""]
        }")!;

        // Act
        var result = _validator.Validate(invalidRouting);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Unknown operator"));
    }

    [Fact]
    public void Validate_ParticipantRoutingToValidParticipantId_ReturnsValid()
    {
        // Arrange - Simple routing that returns participant ID
        var participantIdRouting = JsonNode.Parse(@"{
            ""if"": [
                {"">"": [{""var"": ""amount""}, 1000]},
                ""approver-senior"",
                ""approver-junior""
            ]
        }")!;

        var schema = JsonSchema.FromText(@"{
            ""type"": ""object"",
            ""properties"": {
                ""amount"": { ""type"": ""number"" }
            }
        }");

        // Act
        var result = _validator.Validate(participantIdRouting, schema);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MultipleParticipantRoutingConditions_AllValid()
    {
        // Arrange - Multiple routing conditions in an action
        var condition1 = JsonNode.Parse(@"{""=="": [{""var"": ""type""}, ""A""]}")!;
        var condition2 = JsonNode.Parse(@"{""=="": [{""var"": ""type""}, ""B""]}")!;
        var condition3 = JsonNode.Parse(@"{""=="": [{""var"": ""type""}, ""C""]}")!;

        var schema = JsonSchema.FromText(@"{
            ""type"": ""object"",
            ""properties"": {
                ""type"": { ""type"": ""string"" }
            }
        }");

        // Act
        var result1 = _validator.Validate(condition1, schema);
        var result2 = _validator.Validate(condition2, schema);
        var result3 = _validator.Validate(condition3, schema);

        // Assert
        result1.IsValid.Should().BeTrue();
        result2.IsValid.Should().BeTrue();
        result3.IsValid.Should().BeTrue();
    }

    #endregion

    #region 6.3 Calculations Validation

    [Fact]
    public void Validate_ActionCalculations_ValidJsonLogic_ReturnsValid()
    {
        // Arrange - Calculation using JSON Logic
        var calculation = JsonNode.Parse(@"{
            ""+"": [{""var"": ""price""}, {""*"": [{""var"": ""quantity""}, {""var"": ""taxRate""}]}]
        }")!;

        var schema = JsonSchema.FromText(@"{
            ""type"": ""object"",
            ""properties"": {
                ""price"": { ""type"": ""number"" },
                ""quantity"": { ""type"": ""number"" },
                ""taxRate"": { ""type"": ""number"" }
            }
        }");

        // Act
        var result = _validator.Validate(calculation, schema);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_CalculationReferencingNonExistentField_ReturnsError()
    {
        // Arrange
        var calculation = JsonNode.Parse(@"{
            ""+"": [{""var"": ""existingField""}, {""var"": ""nonExistentField""}]
        }")!;

        var schema = JsonSchema.FromText(@"{
            ""type"": ""object"",
            ""properties"": {
                ""existingField"": { ""type"": ""number"" }
            }
        }");

        // Act
        var result = _validator.Validate(calculation, schema);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("nonExistentField"));
    }

    [Fact]
    public void Validate_MultipleCalculationsInAction_AllValidate()
    {
        // Arrange - Multiple calculations
        var totalCalc = JsonNode.Parse(@"{""*"": [{""var"": ""quantity""}, {""var"": ""price""}]}")!;
        var discountCalc = JsonNode.Parse(@"{""*"": [{""var"": ""total""}, {""var"": ""discountRate""}]}")!;
        var finalCalc = JsonNode.Parse(@"{""-"": [{""var"": ""total""}, {""var"": ""discount""}]}")!;

        var schema = JsonSchema.FromText(@"{
            ""type"": ""object"",
            ""properties"": {
                ""quantity"": { ""type"": ""number"" },
                ""price"": { ""type"": ""number"" },
                ""total"": { ""type"": ""number"" },
                ""discountRate"": { ""type"": ""number"" },
                ""discount"": { ""type"": ""number"" }
            }
        }");

        // Act
        var result1 = _validator.Validate(totalCalc, schema);
        var result2 = _validator.Validate(discountCalc, schema);
        var result3 = _validator.Validate(finalCalc, schema);

        // Assert
        result1.IsValid.Should().BeTrue();
        result2.IsValid.Should().BeTrue();
        result3.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_CalculationResultUsedInSubsequentActions_ReturnsValid()
    {
        // Arrange - Calculation in Action 1
        var action1Calculation = JsonNode.Parse(@"{
            ""+"": [{""var"": ""baseAmount""}, {""var"": ""additionalAmount""}]
        }")!;

        // Calculation in Action 2 uses result from Action 1
        var action2Calculation = JsonNode.Parse(@"{
            ""*"": [{""var"": ""totalAmount""}, {""var"": ""multiplier""}]
        }")!;

        var schema1 = JsonSchema.FromText(@"{
            ""type"": ""object"",
            ""properties"": {
                ""baseAmount"": { ""type"": ""number"" },
                ""additionalAmount"": { ""type"": ""number"" }
            }
        }");

        var schema2 = JsonSchema.FromText(@"{
            ""type"": ""object"",
            ""properties"": {
                ""totalAmount"": { ""type"": ""number"" },
                ""multiplier"": { ""type"": ""number"" }
            }
        }");

        // Act
        var result1 = _validator.Validate(action1Calculation, schema1);
        var result2 = _validator.Validate(action2Calculation, schema2);

        // Assert
        result1.IsValid.Should().BeTrue("Action 1 calculation should be valid");
        result2.IsValid.Should().BeTrue("Action 2 calculation using Action 1 result should be valid");
    }

    [Fact]
    public void Validate_ComplexNestedCalculation_ReturnsValid()
    {
        // Arrange - Complex nested calculation
        var complexCalc = JsonNode.Parse(@"{
            ""+"": [
                {""*"": [{""var"": ""price""}, {""var"": ""quantity""}]},
                {""-"": [
                    {""var"": ""shippingCost""},
                    {""*"": [{""var"": ""discount""}, 0.1]}
                ]}
            ]
        }")!;

        var schema = JsonSchema.FromText(@"{
            ""type"": ""object"",
            ""properties"": {
                ""price"": { ""type"": ""number"" },
                ""quantity"": { ""type"": ""number"" },
                ""shippingCost"": { ""type"": ""number"" },
                ""discount"": { ""type"": ""number"" }
            }
        }");

        // Act
        var result = _validator.Validate(complexCalc, schema);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_CalculationWithStringConcatenation_ReturnsValid()
    {
        // Arrange - String concatenation calculation
        var stringCalc = JsonNode.Parse(@"{
            ""cat"": [{""var"": ""firstName""}, "" "", {""var"": ""lastName""}]
        }")!;

        var schema = JsonSchema.FromText(@"{
            ""type"": ""object"",
            ""properties"": {
                ""firstName"": { ""type"": ""string"" },
                ""lastName"": { ""type"": ""string"" }
            }
        }");

        // Act
        var result = _validator.Validate(stringCalc, schema);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion
}
