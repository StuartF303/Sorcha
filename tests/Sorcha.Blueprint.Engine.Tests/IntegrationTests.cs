// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Blueprint.Engine.Implementation;
using Sorcha.Blueprint.Engine.Interfaces;
using Sorcha.Blueprint.Engine.Models;
using System.Text.Json.Nodes;
using Xunit;
using BpModels = Sorcha.Blueprint.Models;

namespace Sorcha.Blueprint.Engine.Tests;

/// <summary>
/// Integration tests that verify complete end-to-end workflows
/// using realistic blueprint scenarios.
/// </summary>
public class IntegrationTests
{
    private readonly IExecutionEngine _engine;

    public IntegrationTests()
    {
        // Create full dependency graph with real implementations
        var schemaValidator = new SchemaValidator();
        var jsonLogicEvaluator = new JsonLogicEvaluator();
        var disclosureProcessor = new DisclosureProcessor();
        var routingEngine = new RoutingEngine(jsonLogicEvaluator);
        var actionProcessor = new ActionProcessor(
            schemaValidator,
            jsonLogicEvaluator,
            disclosureProcessor,
            routingEngine);

        _engine = new ExecutionEngine(
            actionProcessor,
            schemaValidator,
            jsonLogicEvaluator,
            disclosureProcessor,
            routingEngine);
    }

    #region Loan Application Workflow

    [Fact]
    public async Task LoanApplicationWorkflow_CompletesSuccessfully()
    {
        // Arrange: Create a realistic loan application blueprint
        var blueprint = CreateLoanApplicationBlueprint();
        var applicationAction = blueprint.Actions.First(a => a.Id == "submit-application");

        var applicationData = new Dictionary<string, object>
        {
            ["applicantName"] = "Jane Smith",
            ["loanAmount"] = 50000,
            ["annualIncome"] = 75000,
            ["creditScore"] = 720,
            ["employmentYears"] = 5
        };

        var context = new ExecutionContext
        {
            Blueprint = blueprint,
            Action = applicationAction,
            ActionData = applicationData,
            ParticipantId = "applicant",
            WalletAddress = "0x1234567890abcdef"
        };

        // Act: Execute the loan application action
        var result = await _engine.ExecuteActionAsync(context);

        // Assert: Verify complete workflow execution
        result.Should().NotBeNull();
        result.Success.Should().BeTrue("the workflow should complete successfully");

        // Validation passed
        result.Validation.IsValid.Should().BeTrue();
        result.Validation.Errors.Should().BeEmpty();

        // Calculations applied (debt-to-income ratio)
        result.ProcessedData.Should().ContainKey("debtToIncomeRatio");
        var dtiRatio = Convert.ToDouble(result.ProcessedData["debtToIncomeRatio"]);
        dtiRatio.Should().BeApproximately(0.67, 0.01, "DTI = loanAmount / annualIncome");
        result.CalculatedValues.Should().ContainKey("debtToIncomeRatio");

        // Routing determined (should route to underwriter because credit score >= 700)
        result.Routing.Should().NotBeNull();
        result.Routing.NextParticipantId.Should().Be("underwriter");
        result.Routing.NextActionId.Should().Be("review-application");
        result.Routing.IsWorkflowComplete.Should().BeFalse();

        // Disclosures created
        result.Disclosures.Should().NotBeEmpty();
        result.Disclosures.Should().HaveCount(2, "one for applicant, one for underwriter");

        // Verify applicant sees all their data
        var applicantDisclosure = result.Disclosures.First(d => d.ParticipantId == "applicant");
        applicantDisclosure.DisclosedData.Should().ContainKey("applicantName");
        applicantDisclosure.DisclosedData.Should().ContainKey("loanAmount");
        applicantDisclosure.DisclosedData.Should().ContainKey("creditScore");

        // Verify underwriter sees necessary data but not sensitive fields
        var underwriterDisclosure = result.Disclosures.First(d => d.ParticipantId == "underwriter");
        underwriterDisclosure.DisclosedData.Should().ContainKey("applicantName");
        underwriterDisclosure.DisclosedData.Should().ContainKey("loanAmount");
        underwriterDisclosure.DisclosedData.Should().ContainKey("debtToIncomeRatio");
        underwriterDisclosure.DisclosedData.Should().NotContainKey("creditScore", "sensitive data");
    }

    [Fact]
    public async Task LoanApplicationWorkflow_WithLowCreditScore_RoutesToManualReview()
    {
        // Arrange: Applicant with low credit score
        var blueprint = CreateLoanApplicationBlueprint();
        var applicationAction = blueprint.Actions.First(a => a.Id == "submit-application");

        var applicationData = new Dictionary<string, object>
        {
            ["applicantName"] = "John Doe",
            ["loanAmount"] = 30000,
            ["annualIncome"] = 60000,
            ["creditScore"] = 650, // Below 700 threshold
            ["employmentYears"] = 2
        };

        var context = new ExecutionContext
        {
            Blueprint = blueprint,
            Action = applicationAction,
            ActionData = applicationData,
            ParticipantId = "applicant",
            WalletAddress = "0xabcdef1234567890"
        };

        // Act
        var result = await _engine.ExecuteActionAsync(context);

        // Assert: Should route to manual review, not underwriter
        result.Success.Should().BeTrue();
        result.Routing.NextParticipantId.Should().Be("manual-reviewer");
        result.Routing.NextActionId.Should().Be("manual-review");
    }

    [Fact]
    public async Task LoanApplicationWorkflow_WithInvalidData_FailsValidation()
    {
        // Arrange: Missing required fields
        var blueprint = CreateLoanApplicationBlueprint();
        var applicationAction = blueprint.Actions.First(a => a.Id == "submit-application");

        var invalidData = new Dictionary<string, object>
        {
            ["applicantName"] = "Jane Smith",
            ["loanAmount"] = 50000
            // Missing required fields: annualIncome, creditScore, employmentYears
        };

        var context = new ExecutionContext
        {
            Blueprint = blueprint,
            Action = applicationAction,
            ActionData = invalidData,
            ParticipantId = "applicant",
            WalletAddress = "0x1234567890abcdef"
        };

        // Act
        var result = await _engine.ExecuteActionAsync(context);

        // Assert: Should fail validation
        result.Success.Should().BeFalse();
        result.Validation.IsValid.Should().BeFalse();
        result.Validation.Errors.Should().NotBeEmpty();
        result.Errors.Should().Contain(e => e.Contains("validation"));
    }

    #endregion

    #region Purchase Order Workflow

    [Fact]
    public async Task PurchaseOrderWorkflow_CalculatesTotalsAndAppliesDiscount()
    {
        // Arrange: Purchase order with line items
        var blueprint = CreatePurchaseOrderBlueprint();
        var createOrderAction = blueprint.Actions.First(a => a.Id == "create-order");

        var orderData = new Dictionary<string, object>
        {
            ["orderNumber"] = "PO-2025-001",
            ["quantity"] = 100,
            ["unitPrice"] = 25.50,
            ["isPreferredCustomer"] = true
        };

        var context = new ExecutionContext
        {
            Blueprint = blueprint,
            Action = createOrderAction,
            ActionData = orderData,
            ParticipantId = "buyer",
            WalletAddress = "0xbuyer123"
        };

        // Act
        var result = await _engine.ExecuteActionAsync(context);

        // Assert: Verify calculations
        result.Success.Should().BeTrue();

        // Subtotal calculation
        result.ProcessedData.Should().ContainKey("subtotal");
        var subtotal = Convert.ToDouble(result.ProcessedData["subtotal"]);
        subtotal.Should().Be(2550.0, "100 * 25.50 = 2550");

        // Discount calculation (10% for preferred customers)
        result.ProcessedData.Should().ContainKey("discount");
        var discount = Convert.ToDouble(result.ProcessedData["discount"]);
        discount.Should().Be(255.0, "10% of 2550 = 255");

        // Total calculation
        result.ProcessedData.Should().ContainKey("total");
        var total = Convert.ToDouble(result.ProcessedData["total"]);
        total.Should().Be(2295.0, "2550 - 255 = 2295");

        // All calculated fields should be tracked
        result.CalculatedValues.Should().ContainKey("subtotal");
        result.CalculatedValues.Should().ContainKey("discount");
        result.CalculatedValues.Should().ContainKey("total");
    }

    [Fact]
    public async Task PurchaseOrderWorkflow_WithHighValue_RoutesToApproval()
    {
        // Arrange: High-value order requiring approval
        var blueprint = CreatePurchaseOrderBlueprint();
        var createOrderAction = blueprint.Actions.First(a => a.Id == "create-order");

        var orderData = new Dictionary<string, object>
        {
            ["orderNumber"] = "PO-2025-002",
            ["quantity"] = 200,
            ["unitPrice"] = 100.00,
            ["isPreferredCustomer"] = false
        };

        var context = new ExecutionContext
        {
            Blueprint = blueprint,
            Action = createOrderAction,
            ActionData = orderData,
            ParticipantId = "buyer",
            WalletAddress = "0xbuyer123"
        };

        // Act
        var result = await _engine.ExecuteActionAsync(context);

        // Assert: Should route to approver for orders > $10,000
        result.Success.Should().BeTrue();

        var total = Convert.ToDouble(result.ProcessedData["total"]);
        total.Should().Be(20000.0, "200 * 100 = 20000");

        result.Routing.NextParticipantId.Should().Be("approver");
        result.Routing.NextActionId.Should().Be("approve-order");
    }

    [Fact]
    public async Task PurchaseOrderWorkflow_WithLowValue_RoutesDirectlyToVendor()
    {
        // Arrange: Low-value order that doesn't need approval
        var blueprint = CreatePurchaseOrderBlueprint();
        var createOrderAction = blueprint.Actions.First(a => a.Id == "create-order");

        var orderData = new Dictionary<string, object>
        {
            ["orderNumber"] = "PO-2025-003",
            ["quantity"] = 10,
            ["unitPrice"] = 50.00,
            ["isPreferredCustomer"] = false
        };

        var context = new ExecutionContext
        {
            Blueprint = blueprint,
            Action = createOrderAction,
            ActionData = orderData,
            ParticipantId = "buyer",
            WalletAddress = "0xbuyer123"
        };

        // Act
        var result = await _engine.ExecuteActionAsync(context);

        // Assert: Should route directly to vendor
        result.Success.Should().BeTrue();

        var total = Convert.ToDouble(result.ProcessedData["total"]);
        total.Should().Be(500.0, "10 * 50 = 500");

        result.Routing.NextParticipantId.Should().Be("vendor");
        result.Routing.NextActionId.Should().Be("fulfill-order");
    }

    #endregion

    #region Multi-Step Survey Workflow

    [Fact]
    public async Task SurveyWorkflow_ProgressesThroughMultipleSteps()
    {
        // Arrange: Multi-step survey with conditional paths
        var blueprint = CreateSurveyBlueprint();

        // Step 1: Demographics
        var step1Action = blueprint.Actions.First(a => a.Id == "demographics");
        var step1Data = new Dictionary<string, object>
        {
            ["age"] = 35,
            ["country"] = "USA",
            ["occupation"] = "Engineer"
        };

        var step1Context = new ExecutionContext
        {
            Blueprint = blueprint,
            Action = step1Action,
            ActionData = step1Data,
            ParticipantId = "respondent",
            WalletAddress = "0xsurvey123"
        };

        // Act: Execute step 1
        var step1Result = await _engine.ExecuteActionAsync(step1Context);

        // Assert: Step 1 completed successfully
        step1Result.Success.Should().BeTrue();
        step1Result.Routing.NextParticipantId.Should().Be("respondent");
        step1Result.Routing.NextActionId.Should().Be("preferences");

        // Step 2: Use data from step 1 and add new data
        var step2Action = blueprint.Actions.First(a => a.Id == "preferences");
        var step2Data = new Dictionary<string, object>
        {
            ["favoriteColor"] = "Blue",
            ["rating"] = 8
        };

        var step2Context = new ExecutionContext
        {
            Blueprint = blueprint,
            Action = step2Action,
            ActionData = step2Data,
            PreviousData = step1Data, // Carry forward previous data
            ParticipantId = "respondent",
            WalletAddress = "0xsurvey123"
        };

        // Act: Execute step 2
        var step2Result = await _engine.ExecuteActionAsync(step2Context);

        // Assert: Step 2 completed, workflow complete
        step2Result.Success.Should().BeTrue();
        step2Result.Routing.IsWorkflowComplete.Should().BeTrue();
    }

    #endregion

    #region Helper Methods - Loan Application Blueprint

    private BpModels.Blueprint CreateLoanApplicationBlueprint()
    {
        return new BpModels.Blueprint
        {
            Id = "loan-application-v1",
            Title = "Loan Application Workflow",
            Description = "Complete loan application and approval workflow",
            Version = 1,
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = "submit-application",
                    Title = "Submit Loan Application",
                    Form = new BpModels.Control
                    {
                        Schema = JsonNode.Parse("""
                        {
                            "type": "object",
                            "properties": {
                                "applicantName": { "type": "string", "minLength": 1 },
                                "loanAmount": { "type": "number", "minimum": 1000 },
                                "annualIncome": { "type": "number", "minimum": 0 },
                                "creditScore": { "type": "integer", "minimum": 300, "maximum": 850 },
                                "employmentYears": { "type": "integer", "minimum": 0 }
                            },
                            "required": ["applicantName", "loanAmount", "annualIncome", "creditScore", "employmentYears"]
                        }
                        """)
                    },
                    Calculations = new Dictionary<string, JsonNode>
                    {
                        ["debtToIncomeRatio"] = JsonNode.Parse("""
                        {
                            "/": [
                                { "var": "loanAmount" },
                                { "var": "annualIncome" }
                            ]
                        }
                        """)!
                    },
                    Disclosures = new List<BpModels.Disclosure>
                    {
                        new()
                        {
                            Id = "applicant-disclosure",
                            ParticipantId = "applicant",
                            DataPointers = new List<string> { "/*" } // All data
                        },
                        new()
                        {
                            Id = "underwriter-disclosure",
                            ParticipantId = "underwriter",
                            DataPointers = new List<string>
                            {
                                "/applicantName",
                                "/loanAmount",
                                "/annualIncome",
                                "/debtToIncomeRatio",
                                "/employmentYears"
                                // Note: creditScore is excluded (sensitive)
                            }
                        }
                    }
                },
                new()
                {
                    Id = "review-application",
                    Title = "Review Application"
                },
                new()
                {
                    Id = "manual-review",
                    Title = "Manual Credit Review"
                }
            },
            Participants = new List<BpModels.Participant>
            {
                new()
                {
                    Id = "applicant",
                    WalletAddress = "applicant-wallet",
                    Actions = new List<string> { "submit-application" }
                },
                new()
                {
                    Id = "underwriter",
                    WalletAddress = "underwriter-wallet",
                    Actions = new List<string> { "review-application" },
                    Conditions = new List<BpModels.Condition>
                    {
                        new()
                        {
                            Principal = "underwriter",
                            Criteria = new List<string>
                            {
                                """{">=": [{"var": "creditScore"}, 700]}"""
                            }
                        }
                    }
                },
                new()
                {
                    Id = "manual-reviewer",
                    WalletAddress = "reviewer-wallet",
                    Actions = new List<string> { "manual-review" },
                    Conditions = new List<BpModels.Condition>
                    {
                        new()
                        {
                            Principal = "manual-reviewer",
                            Criteria = new List<string>
                            {
                                """{"<": [{"var": "creditScore"}, 700]}"""
                            }
                        }
                    }
                }
            }
        };
    }

    #endregion

    #region Helper Methods - Purchase Order Blueprint

    private BpModels.Blueprint CreatePurchaseOrderBlueprint()
    {
        return new BpModels.Blueprint
        {
            Id = "purchase-order-v1",
            Title = "Purchase Order Workflow",
            Description = "Purchase order creation and approval",
            Version = 1,
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = "create-order",
                    Title = "Create Purchase Order",
                    Form = new BpModels.Control
                    {
                        Schema = JsonNode.Parse("""
                        {
                            "type": "object",
                            "properties": {
                                "orderNumber": { "type": "string" },
                                "quantity": { "type": "integer", "minimum": 1 },
                                "unitPrice": { "type": "number", "minimum": 0 },
                                "isPreferredCustomer": { "type": "boolean" }
                            },
                            "required": ["orderNumber", "quantity", "unitPrice", "isPreferredCustomer"]
                        }
                        """)
                    },
                    Calculations = new Dictionary<string, JsonNode>
                    {
                        ["subtotal"] = JsonNode.Parse("""
                        {
                            "*": [
                                { "var": "quantity" },
                                { "var": "unitPrice" }
                            ]
                        }
                        """)!,
                        ["discount"] = JsonNode.Parse("""
                        {
                            "if": [
                                { "var": "isPreferredCustomer" },
                                { "*": [{ "var": "subtotal" }, 0.10] },
                                0
                            ]
                        }
                        """)!,
                        ["total"] = JsonNode.Parse("""
                        {
                            "-": [
                                { "var": "subtotal" },
                                { "var": "discount" }
                            ]
                        }
                        """)!
                    }
                },
                new()
                {
                    Id = "approve-order",
                    Title = "Approve Order"
                },
                new()
                {
                    Id = "fulfill-order",
                    Title = "Fulfill Order"
                }
            },
            Participants = new List<BpModels.Participant>
            {
                new()
                {
                    Id = "buyer",
                    WalletAddress = "buyer-wallet",
                    Actions = new List<string> { "create-order" }
                },
                new()
                {
                    Id = "approver",
                    WalletAddress = "approver-wallet",
                    Actions = new List<string> { "approve-order" },
                    Conditions = new List<BpModels.Condition>
                    {
                        new()
                        {
                            Principal = "approver",
                            Criteria = new List<string>
                            {
                                """{">": [{"var": "total"}, 10000]}"""
                            }
                        }
                    }
                },
                new()
                {
                    Id = "vendor",
                    WalletAddress = "vendor-wallet",
                    Actions = new List<string> { "fulfill-order" },
                    Conditions = new List<BpModels.Condition>
                    {
                        new()
                        {
                            Principal = "vendor",
                            Criteria = new List<string>
                            {
                                """{"<=": [{"var": "total"}, 10000]}"""
                            }
                        }
                    }
                }
            }
        };
    }

    #endregion

    #region Helper Methods - Survey Blueprint

    private BpModels.Blueprint CreateSurveyBlueprint()
    {
        return new BpModels.Blueprint
        {
            Id = "survey-v1",
            Title = "Multi-Step Survey",
            Description = "Multi-step survey with demographic and preference questions",
            Version = 1,
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = "demographics",
                    Title = "Demographics Survey",
                    Form = new BpModels.Control
                    {
                        Schema = JsonNode.Parse("""
                        {
                            "type": "object",
                            "properties": {
                                "age": { "type": "integer", "minimum": 18 },
                                "country": { "type": "string" },
                                "occupation": { "type": "string" }
                            },
                            "required": ["age", "country", "occupation"]
                        }
                        """)
                    }
                },
                new()
                {
                    Id = "preferences",
                    Title = "Preferences Survey",
                    Form = new BpModels.Control
                    {
                        Schema = JsonNode.Parse("""
                        {
                            "type": "object",
                            "properties": {
                                "favoriteColor": { "type": "string" },
                                "rating": { "type": "integer", "minimum": 1, "maximum": 10 }
                            },
                            "required": ["favoriteColor", "rating"]
                        }
                        """)
                    }
                }
            },
            Participants = new List<BpModels.Participant>
            {
                new()
                {
                    Id = "respondent",
                    WalletAddress = "respondent-wallet",
                    Actions = new List<string> { "demographics", "preferences" }
                }
            }
        };
    }

    #endregion
}
