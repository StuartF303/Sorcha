// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json.Nodes;
using Sorcha.Blueprint.Engine.Implementation;
using Sorcha.Blueprint.Engine.Interfaces;
using Sorcha.Blueprint.Engine.Models;
using BpModels = Sorcha.Blueprint.Models;

namespace Sorcha.Blueprint.Engine.Tests;

/// <summary>
/// Unit tests for ActionProcessor.
/// </summary>
public class ActionProcessorTests
{
    private readonly IActionProcessor _processor;

    public ActionProcessorTests()
    {
        var schemaValidator = new SchemaValidator();
        var jsonLogicEvaluator = new JsonLogicEvaluator();
        var disclosureProcessor = new DisclosureProcessor();
        var routingEngine = new RoutingEngine(jsonLogicEvaluator);

        _processor = new ActionProcessor(
            schemaValidator,
            jsonLogicEvaluator,
            disclosureProcessor,
            routingEngine);
    }

    #region Valid Data Tests

    [Fact]
    public async Task ProcessAsync_ValidData_ReturnsSuccess()
    {
        // Arrange
        var blueprint = CreateSimpleBlueprint();
        var action = blueprint.Actions[0];

        var context = new ExecutionContext
        {
            Blueprint = blueprint,
            Action = action,
            ActionData = new Dictionary<string, object>
            {
                ["name"] = "Alice",
                ["age"] = 30
            },
            ParticipantId = "user1",
            WalletAddress = "0x123"
        };

        // Act
        var result = await _processor.ProcessAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Validation.IsValid.Should().BeTrue();
        result.ProcessedData.Should().ContainKey("name");
        result.ProcessedData.Should().ContainKey("age");
    }

    [Fact]
    public async Task ProcessAsync_NoSchema_SkipsValidation()
    {
        // Arrange
        var blueprint = new Blueprint
        {
            Id = "BP-001",
            Title = "Test",
            Description = "Test",
            Participants = new List<Participant>
            {
                new() { Id = "user1", Name = "User 1" }
            },
            Actions = new List<Models.Action>
            {
                new()
                {
                    Id = 1,
                    Title = "Test Action",
                    Sender = "user1",
                    Form = null // No schema
                }
            }
        };

        var context = new ExecutionContext
        {
            Blueprint = blueprint,
            Action = blueprint.Actions[0],
            ActionData = new Dictionary<string, object> { ["anything"] = "goes" },
            ParticipantId = "user1",
            WalletAddress = "0x123"
        };

        // Act
        var result = await _processor.ProcessAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Validation.IsValid.Should().BeTrue();
    }

    #endregion

    #region Invalid Data Tests

    [Fact]
    public async Task ProcessAsync_InvalidData_ReturnsErrors()
    {
        // Arrange
        var blueprint = CreateSimpleBlueprint();
        var action = blueprint.Actions[0];

        var context = new ExecutionContext
        {
            Blueprint = blueprint,
            Action = action,
            ActionData = new Dictionary<string, object>
            {
                ["name"] = "Alice"
                // Missing required field 'age'
            },
            ParticipantId = "user1",
            WalletAddress = "0x123"
        };

        // Act
        var result = await _processor.ProcessAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.Validation.IsValid.Should().BeFalse();
        result.Validation.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Contains("validation"));
    }

    [Fact]
    public async Task ProcessAsync_InvalidData_DoesNotProcessFurther()
    {
        // Arrange
        var blueprint = CreateBlueprintWithCalculations();
        var action = blueprint.Actions[0];

        var context = new ExecutionContext
        {
            Blueprint = blueprint,
            Action = action,
            ActionData = new Dictionary<string, object>
            {
                ["quantity"] = "invalid" // Should be number
            },
            ParticipantId = "user1",
            WalletAddress = "0x123"
        };

        // Act
        var result = await _processor.ProcessAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.CalculatedValues.Should().BeEmpty(); // Calculations not applied
        result.Disclosures.Should().BeEmpty(); // Disclosures not created
    }

    #endregion

    #region Calculations Tests

    [Fact]
    public async Task ProcessAsync_WithCalculations_AppliesCorrectly()
    {
        // Arrange
        var blueprint = CreateBlueprintWithCalculations();
        var action = blueprint.Actions[0];

        var context = new ExecutionContext
        {
            Blueprint = blueprint,
            Action = action,
            ActionData = new Dictionary<string, object>
            {
                ["quantity"] = 5,
                ["unitPrice"] = 10.0
            },
            ParticipantId = "user1",
            WalletAddress = "0x123"
        };

        // Act
        var result = await _processor.ProcessAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.ProcessedData.Should().ContainKey("totalPrice");
        result.CalculatedValues.Should().ContainKey("totalPrice");
        Convert.ToDouble(result.ProcessedData["totalPrice"]).Should().Be(50.0);
    }

    [Fact]
    public async Task ProcessAsync_WithMultipleCalculations_AppliesInOrder()
    {
        // Arrange
        var blueprint = new Blueprint
        {
            Id = "BP-CALC",
            Title = "Calculation Test",
            Description = "Test",
            Participants = new List<Participant>
            {
                new() { Id = "user1", Name = "User 1" }
            },
            Actions = new List<Models.Action>
            {
                new()
                {
                    Id = 1,
                    Title = "Calculate",
                    Sender = "user1",
                    Form = new BpModels.Control
                    {
                        Schema = JsonNode.Parse("""
                        {
                            "type": "object",
                            "properties": {
                                "basePrice": { "type": "number" },
                                "taxRate": { "type": "number" }
                            },
                            "required": ["basePrice", "taxRate"]
                        }
                        """)
                    },
                    Calculations = new Dictionary<string, JsonNode>
                    {
                        ["taxAmount"] = JsonNode.Parse("""{"*": [{"var": "basePrice"}, {"var": "taxRate"}]}""")!,
                        ["total"] = JsonNode.Parse("""{"+": [{"var": "basePrice"}, {"var": "taxAmount"}]}""")!
                    }
                }
            }
        };

        var context = new ExecutionContext
        {
            Blueprint = blueprint,
            Action = blueprint.Actions[0],
            ActionData = new Dictionary<string, object>
            {
                ["basePrice"] = 100.0,
                ["taxRate"] = 0.1
            },
            ParticipantId = "user1",
            WalletAddress = "0x123"
        };

        // Act
        var result = await _processor.ProcessAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.CalculatedValues.Should().ContainKey("taxAmount");
        result.CalculatedValues.Should().ContainKey("total");
        Convert.ToDouble(result.ProcessedData["taxAmount"]).Should().Be(10.0);
        Convert.ToDouble(result.ProcessedData["total"]).Should().Be(110.0);
    }

    #endregion

    #region Routing Tests

    [Fact]
    public async Task ProcessAsync_WithConditionalRouting_Works()
    {
        // Arrange
        var blueprint = CreateBlueprintWithRouting();
        var action = blueprint.Actions[0];

        // Test high amount - should route to manager
        var highAmountContext = new ExecutionContext
        {
            Blueprint = blueprint,
            Action = action,
            ActionData = new Dictionary<string, object>
            {
                ["amount"] = 15000
            },
            ParticipantId = "user",
            WalletAddress = "0x123"
        };

        // Test low amount - should route to clerk
        var lowAmountContext = new ExecutionContext
        {
            Blueprint = blueprint,
            Action = action,
            ActionData = new Dictionary<string, object>
            {
                ["amount"] = 5000
            },
            ParticipantId = "user",
            WalletAddress = "0x456"
        };

        // Act
        var highResult = await _processor.ProcessAsync(highAmountContext);
        var lowResult = await _processor.ProcessAsync(lowAmountContext);

        // Assert
        highResult.Success.Should().BeTrue();
        highResult.Routing.NextParticipantId.Should().Be("manager");

        lowResult.Success.Should().BeTrue();
        lowResult.Routing.NextParticipantId.Should().Be("clerk");
    }

    [Fact]
    public async Task ProcessAsync_WorkflowComplete_SetsFlag()
    {
        // Arrange
        var blueprint = new Blueprint
        {
            Id = "BP-END",
            Title = "End Test",
            Description = "Test",
            Participants = new List<Participant>
            {
                new() { Id = "user1", Name = "User 1" }
            },
            Actions = new List<Models.Action>
            {
                new()
                {
                    Id = 1,
                    Title = "Final Action",
                    Sender = "user1",
                    Participants = new List<Condition>() // No next
                }
            }
        };

        var context = new ExecutionContext
        {
            Blueprint = blueprint,
            Action = blueprint.Actions[0],
            ActionData = new Dictionary<string, object>(),
            ParticipantId = "user1",
            WalletAddress = "0x123"
        };

        // Act
        var result = await _processor.ProcessAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Routing.IsWorkflowComplete.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("Workflow complete"));
    }

    #endregion

    #region Disclosure Tests

    [Fact]
    public async Task ProcessAsync_WithDisclosures_CreatesCorrectly()
    {
        // Arrange
        var blueprint = CreateBlueprintWithDisclosures();
        var action = blueprint.Actions[0];

        var context = new ExecutionContext
        {
            Blueprint = blueprint,
            Action = action,
            ActionData = new Dictionary<string, object>
            {
                ["orderId"] = "ORD-123",
                ["productId"] = "PROD-001",
                ["buyerAddress"] = "123 Main St",
                ["sellerAddress"] = "456 Oak Ave"
            },
            ParticipantId = "buyer",
            WalletAddress = "0x123"
        };

        // Act
        var result = await _processor.ProcessAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Disclosures.Should().HaveCount(2);

        var buyerDisclosure = result.Disclosures.First(d => d.ParticipantId == "buyer");
        buyerDisclosure.DisclosedData.Should().ContainKey("orderId");
        buyerDisclosure.DisclosedData.Should().ContainKey("buyerAddress");
        buyerDisclosure.DisclosedData.Should().NotContainKey("sellerAddress");

        var sellerDisclosure = result.Disclosures.First(d => d.ParticipantId == "seller");
        sellerDisclosure.DisclosedData.Should().ContainKey("orderId");
        sellerDisclosure.DisclosedData.Should().ContainKey("sellerAddress");
        sellerDisclosure.DisclosedData.Should().NotContainKey("buyerAddress");
    }

    [Fact]
    public async Task ProcessAsync_NoDisclosures_AddsWarning()
    {
        // Arrange
        var blueprint = new Blueprint
        {
            Id = "BP-NO-DISC",
            Title = "No Disclosures",
            Description = "Test",
            Participants = new List<Participant>
            {
                new() { Id = "user1", Name = "User 1" }
            },
            Actions = new List<Models.Action>
            {
                new()
                {
                    Id = 1,
                    Title = "Test",
                    Sender = "user1",
                    Disclosures = new List<Disclosure>() // Empty
                }
            }
        };

        var context = new ExecutionContext
        {
            Blueprint = blueprint,
            Action = blueprint.Actions[0],
            ActionData = new Dictionary<string, object>(),
            ParticipantId = "user1",
            WalletAddress = "0x123"
        };

        // Act
        var result = await _processor.ProcessAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Disclosures.Should().BeEmpty();
        result.Warnings.Should().Contain(w => w.Contains("No disclosures"));
    }

    #endregion

    #region Complete Workflow Tests

    [Fact]
    public async Task ProcessAsync_CompleteWorkflow_Works()
    {
        // Arrange - A complete workflow with validation, calculations, routing, and disclosures
        var blueprint = CreateCompleteBlueprint();
        var action = blueprint.Actions[0];

        var context = new ExecutionContext
        {
            Blueprint = blueprint,
            Action = action,
            ActionData = new Dictionary<string, object>
            {
                ["productId"] = "PROD-001",
                ["quantity"] = 5,
                ["unitPrice"] = 100.0
            },
            ParticipantId = "buyer",
            WalletAddress = "0x123"
        };

        // Act
        var result = await _processor.ProcessAsync(context);

        // Assert
        result.Success.Should().BeTrue();

        // Validation passed
        result.Validation.IsValid.Should().BeTrue();

        // Calculations applied
        result.CalculatedValues.Should().ContainKey("totalPrice");
        Convert.ToDouble(result.ProcessedData["totalPrice"]).Should().Be(500.0);

        // Routing determined
        result.Routing.NextParticipantId.Should().Be("seller");

        // Disclosures created
        result.Disclosures.Should().NotBeEmpty();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ProcessAsync_NullContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _processor.ProcessAsync(null!)
        );
    }

    [Fact]
    public void Constructor_NullSchemaValidator_ThrowsArgumentNullException()
    {
        // Arrange
        var evaluator = new JsonLogicEvaluator();
        var disclosure = new DisclosureProcessor();
        var routing = new RoutingEngine(evaluator);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new ActionProcessor(null!, evaluator, disclosure, routing)
        );
    }

    [Fact]
    public void Constructor_NullEvaluator_ThrowsArgumentNullException()
    {
        // Arrange
        var validator = new SchemaValidator();
        var disclosure = new DisclosureProcessor();
        var evaluator = new JsonLogicEvaluator();
        var routing = new RoutingEngine(evaluator);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => new ActionProcessor(validator, null!, disclosure, routing)
        );
    }

    #endregion

    #region Helper Methods

    private static BpModels.Blueprint CreateSimpleBlueprint()
    {
        return new BpModels.Blueprint
        {
            Id = "BP-SIMPLE",
            Title = "Simple Test",
            Description = "Test",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "user1", Name = "User 1" },
                new() { Id = "user2", Name = "User 2" }
            },
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 1,
                    Title = "Submit",
                    Sender = "user1",
                    Form = new BpModels.Control
                    {
                        Schema = JsonNode.Parse("""
                        {
                            "type": "object",
                            "properties": {
                                "name": { "type": "string" },
                                "age": { "type": "number" }
                            },
                            "required": ["name", "age"]
                        }
                        """)
                    },
                    Participants = new List<BpModels.Condition>
                    {
                        new("user2", true)
                    }
                },
                new()
                {
                    Id = 2,
                    Title = "Review",
                    Sender = "user2"
                }
            }
        };
    }

    private static BpModels.Blueprint CreateBlueprintWithCalculations()
    {
        return new BpModels.Blueprint
        {
            Id = "BP-CALC",
            Title = "Calculation Test",
            Description = "Test",
            Participants = new List<Participant>
            {
                new() { Id = "user1", Name = "User 1" }
            },
            Actions = new List<Models.Action>
            {
                new()
                {
                    Id = 1,
                    Title = "Calculate Total",
                    Sender = "user1",
                    Form = new BpModels.Control
                    {
                        Schema = JsonNode.Parse("""
                        {
                            "type": "object",
                            "properties": {
                                "quantity": { "type": "number" },
                                "unitPrice": { "type": "number" }
                            },
                            "required": ["quantity", "unitPrice"]
                        }
                        """)
                    },
                    Calculations = new Dictionary<string, JsonNode>
                    {
                        ["totalPrice"] = JsonNode.Parse("""{"*": [{"var": "quantity"}, {"var": "unitPrice"}]}""")!
                    }
                }
            }
        };
    }

    private static BpModels.Blueprint CreateBlueprintWithRouting()
    {
        return new BpModels.Blueprint
        {
            Id = "BP-ROUTE",
            Title = "Routing Test",
            Description = "Test",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "user", Name = "User" },
                new() { Id = "manager", Name = "Manager" },
                new() { Id = "clerk", Name = "Clerk" }
            },
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 1,
                    Title = "Submit Request",
                    Sender = "user",
                    Form = new BpModels.Control
                    {
                        Schema = JsonNode.Parse("""
                        {
                            "type": "object",
                            "properties": {
                                "amount": { "type": "number" }
                            },
                            "required": ["amount"]
                        }
                        """)
                    },
                    Participants = new List<BpModels.Condition>
                    {
                        new("manager", new List<string> { """{">=": [{"var": "amount"}, 10000]}""" }),
                        new("clerk", true)
                    }
                },
                new()
                {
                    Id = 2,
                    Title = "Manager Approval",
                    Sender = "manager"
                },
                new()
                {
                    Id = 3,
                    Title = "Clerk Approval",
                    Sender = "clerk"
                }
            }
        };
    }

    private static BpModels.Blueprint CreateBlueprintWithDisclosures()
    {
        return new BpModels.Blueprint
        {
            Id = "BP-DISC",
            Title = "Disclosure Test",
            Description = "Test",
            Participants = new List<Participant>
            {
                new() { Id = "buyer", Name = "Buyer" },
                new() { Id = "seller", Name = "Seller" }
            },
            Actions = new List<Models.Action>
            {
                new()
                {
                    Id = 1,
                    Title = "Order",
                    Sender = "buyer",
                    Disclosures = new List<BpModels.Disclosure>
                    {
                        new("buyer", new List<string> { "/orderId", "/productId", "/buyerAddress" }),
                        new("seller", new List<string> { "/orderId", "/productId", "/sellerAddress" })
                    }
                }
            }
        };
    }

    private static BpModels.Blueprint CreateCompleteBlueprint()
    {
        return new BpModels.Blueprint
        {
            Id = "BP-COMPLETE",
            Title = "Complete Workflow",
            Description = "Test all features",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "buyer", Name = "Buyer" },
                new() { Id = "seller", Name = "Seller" }
            },
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 1,
                    Title = "Create Order",
                    Sender = "buyer",
                    Form = new BpModels.Control
                    {
                        Schema = JsonNode.Parse("""
                        {
                            "type": "object",
                            "properties": {
                                "productId": { "type": "string" },
                                "quantity": { "type": "number" },
                                "unitPrice": { "type": "number" }
                            },
                            "required": ["productId", "quantity", "unitPrice"]
                        }
                        """)
                    },
                    Calculations = new Dictionary<string, JsonNode>
                    {
                        ["totalPrice"] = JsonNode.Parse("""{"*": [{"var": "quantity"}, {"var": "unitPrice"}]}""")!
                    },
                    Participants = new List<BpModels.Condition>
                    {
                        new("seller", true)
                    },
                    Disclosures = new List<BpModels.Disclosure>
                    {
                        new("buyer", new List<string> { "/productId", "/quantity", "/totalPrice" }),
                        new("seller", new List<string> { "/productId", "/quantity", "/totalPrice" })
                    }
                },
                new()
                {
                    Id = 2,
                    Title = "Fulfill Order",
                    Sender = "seller"
                }
            }
        };
    }

    #endregion
}
