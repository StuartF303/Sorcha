// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Blueprint.Engine.Implementation;
using Sorcha.Blueprint.Models;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Sorcha.Blueprint.Engine.Tests;

/// <summary>
/// Demonstration tests for an expense approval workflow showing:
/// - JSON Logic routing based on expense amount
/// - Participant role fulfillment
/// - Data flow through the workflow
/// - CLI output visualization
/// </summary>
public class ExpenseApprovalWorkflowDemoTests
{
    private readonly JsonLogicEvaluator _evaluator;

    public ExpenseApprovalWorkflowDemoTests()
    {
        _evaluator = new JsonLogicEvaluator();
    }

    [Fact]
    public void ExpenseWorkflow_InstantApproval_Under100Dollars()
    {
        // Arrange
        PrintHeader("Expense Approval - Instant Approval (<$100)");

        var blueprint = CreateExpenseBlueprint();

        var expenseData = new Dictionary<string, object>
        {
            ["amount"] = 75.50,
            ["description"] = "Team lunch",
            ["category"] = "Meals"
        };

        // Act
        PrintExpenseClaim(expenseData, "Alice Johnson", "employee");

        var action0 = blueprint.Actions.First(a => a.Id == 0);
        var result = _evaluator.Evaluate(action0.Condition!, expenseData);

        var nextActionId = Convert.ToInt32(result);
        var nextAction = blueprint.Actions.First(a => a.Id == nextActionId);

        // Assert
        nextActionId.Should().Be(1, "amounts under $100 should route to instant approval");

        PrintRoutingDecision(nextActionId, nextAction.Title, nextAction.Sender, blueprint);
        PrintApprovalResult("AUTOMATICALLY APPROVED", "Amount under $100 threshold");
        PrintFooter();
    }

    [Fact]
    public void ExpenseWorkflow_ManagerReview_100To1000Dollars()
    {
        // Arrange
        PrintHeader("Expense Approval - Manager Review ($100-$1000)");

        var blueprint = CreateExpenseBlueprint();

        var expenseData = new Dictionary<string, object>
        {
            ["amount"] = 450.00,
            ["description"] = "Client dinner and entertainment",
            ["category"] = "Entertainment"
        };

        // Act
        PrintExpenseClaim(expenseData, "Alice Johnson", "employee");

        var action0 = blueprint.Actions.First(a => a.Id == 0);
        var result = _evaluator.Evaluate(action0.Condition!, expenseData);

        var nextActionId = Convert.ToInt32(result);
        var nextAction = blueprint.Actions.First(a => a.Id == nextActionId);

        PrintRoutingDecision(nextActionId, nextAction.Title, nextAction.Sender, blueprint);

        // Simulate manager approval
        var managerDecision = new Dictionary<string, object>
        {
            ["approved"] = true,
            ["comments"] = "Approved - valid client entertainment expense"
        };

        PrintManagerDecision(managerDecision, "Bob Smith");

        // Assert
        nextActionId.Should().Be(2, "amounts $100-$1000 should route to manager");

        PrintApprovalResult("APPROVED BY MANAGER", $"Amount: ${expenseData["amount"]}");
        PrintFooter();
    }

    [Fact]
    public void ExpenseWorkflow_FinanceReview_Over1000Dollars()
    {
        // Arrange
        PrintHeader("Expense Approval - Finance Review (>=$1000)");

        var blueprint = CreateExpenseBlueprint();

        var expenseData = new Dictionary<string, object>
        {
            ["amount"] = 2500.00,
            ["description"] = "Conference registration and travel",
            ["category"] = "Professional Development"
        };

        // Act
        PrintExpenseClaim(expenseData, "Alice Johnson", "employee");

        var action0 = blueprint.Actions.First(a => a.Id == 0);
        var result = _evaluator.Evaluate(action0.Condition!, expenseData);

        var nextActionId = Convert.ToInt32(result);
        var nextAction = blueprint.Actions.First(a => a.Id == nextActionId);

        PrintRoutingDecision(nextActionId, nextAction.Title, nextAction.Sender, blueprint);

        // Simulate finance approval
        var financeDecision = new Dictionary<string, object>
        {
            ["approved"] = true,
            ["comments"] = "Approved - within professional development budget",
            ["budgetCode"] = "PROF-DEV-2025"
        };

        PrintFinanceDecision(financeDecision, "Carol White");

        // Assert
        nextActionId.Should().Be(3, "amounts >= $1000 should route to finance");

        PrintApprovalResult("APPROVED BY FINANCE", $"Amount: ${expenseData["amount"]}, Budget: {financeDecision["budgetCode"]}");
        PrintFooter();
    }

    #region Helper Methods

    private static Sorcha.Blueprint.Models.Blueprint CreateExpenseBlueprint()
    {
        var blueprint = new Sorcha.Blueprint.Models.Blueprint
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Expense Approval Workflow",
            Description = "Multi-level expense approval based on amount",
            Version = 1,
            Participants = new List<Participant>
            {
                new() { Id = "employee", Name = "Alice Johnson", Organisation = "Acme Corporation", WalletAddress = "employee-wallet-001" },
                new() { Id = "manager", Name = "Bob Smith", Organisation = "Acme Corporation", WalletAddress = "manager-wallet-001" },
                new() { Id = "finance", Name = "Carol White", Organisation = "Acme Corporation", WalletAddress = "finance-wallet-001" },
                new() { Id = "system", Name = "Expense System", Organisation = "Acme Corporation", WalletAddress = "system-wallet-001" }
            },
            Actions = new List<Sorcha.Blueprint.Models.Action>
            {
                new()
                {
                    Id = 0,
                    Title = "Submit Expense Claim",
                    Description = "Employee submits expense for reimbursement",
                    Sender = "employee",
                    Condition = JsonNode.Parse("""
                    {
                        "if": [
                            { "<": [{ "var": "amount" }, 100] },
                            1,
                            { "if": [
                                { "<": [{ "var": "amount" }, 1000] },
                                2,
                                3
                            ]}
                        ]
                    }
                    """)
                },
                new()
                {
                    Id = 1,
                    Title = "Instant Approval",
                    Description = "Auto-approved by system",
                    Sender = "system"
                },
                new()
                {
                    Id = 2,
                    Title = "Manager Review",
                    Description = "Requires manager approval",
                    Sender = "manager"
                },
                new()
                {
                    Id = 3,
                    Title = "Finance Review",
                    Description = "Requires finance director approval",
                    Sender = "finance"
                }
            }
        };

        return blueprint;
    }

    private static void PrintHeader(string title)
    {
        Console.WriteLine();
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║ {title.PadRight(57)} ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        Console.WriteLine();
    }

    private static void PrintExpenseClaim(Dictionary<string, object> data, string employeeName, string participantId)
    {
        Console.WriteLine($"📝 Expense Claim Submitted:");
        Console.WriteLine($"   Employee: {employeeName} ({participantId})");
        Console.WriteLine($"   Amount: ${data["amount"]}");
        Console.WriteLine($"   Description: {data["description"]}");
        Console.WriteLine($"   Category: {data["category"]}");
        Console.WriteLine();
    }

    private static void PrintRoutingDecision(int actionId, string actionTitle, string assignedTo, Sorcha.Blueprint.Models.Blueprint blueprint)
    {
        Console.WriteLine($"🔀 Routing Decision:");
        Console.WriteLine($"   Next Action: {actionId} - {actionTitle}");

        var participant = blueprint.Participants.FirstOrDefault(p => p.Id == assignedTo);
        if (participant != null)
        {
            Console.WriteLine($"   Assigned To: {participant.Name} ({participant.Id})");
        }
        Console.WriteLine();
    }

    private static void PrintManagerDecision(Dictionary<string, object> decision, string managerName)
    {
        Console.WriteLine($"👔 Manager: {managerName}");
        Console.WriteLine($"   Decision: {((bool)decision["approved"] ? "APPROVED ✅" : "REJECTED ❌")}");
        Console.WriteLine($"   Comments: {decision["comments"]}");
        Console.WriteLine();
    }

    private static void PrintFinanceDecision(Dictionary<string, object> decision, string financeName)
    {
        Console.WriteLine($"💰 Finance Director: {financeName}");
        Console.WriteLine($"   Decision: {((bool)decision["approved"] ? "APPROVED ✅" : "REJECTED ❌")}");
        Console.WriteLine($"   Comments: {decision["comments"]}");
        Console.WriteLine($"   Budget Code: {decision["budgetCode"]}");
        Console.WriteLine();
    }

    private static void PrintApprovalResult(string status, string details)
    {
        Console.WriteLine($"✅ Result: {status}");
        Console.WriteLine($"   {details}");
        Console.WriteLine();
    }

    private static void PrintFooter()
    {
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine();
    }

    #endregion
}
