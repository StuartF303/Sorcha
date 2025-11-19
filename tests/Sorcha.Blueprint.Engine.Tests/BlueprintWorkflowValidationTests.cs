// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json.Nodes;
using FluentAssertions;
using Sorcha.Blueprint.Engine.Implementation;
using BpModels = Sorcha.Blueprint.Models;

namespace Sorcha.Blueprint.Engine.Tests;

/// <summary>
/// Tests for Blueprint workflow validation including:
/// - Action routing validation (JSON Logic conditions)
/// - Action sequence validation (IDs, ordering)
/// - Graph cycle detection (prevent infinite loops)
/// </summary>
public class BlueprintWorkflowValidationTests
{
    private readonly JsonLogicEvaluator _evaluator;

    public BlueprintWorkflowValidationTests()
    {
        _evaluator = new JsonLogicEvaluator();
    }

    #region 3.1 Action Routing Validation

    [Fact]
    public void ValidateActionConditions_WithValidJsonLogic_ShouldPass()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Title = "Valid Routing",
            Description = "Blueprint with valid JSON Logic conditions",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new() { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 0,
                    Title = "Start",
                    Condition = JsonNode.Parse("{\"if\": [{\">\": [{\"var\": \"amount\"}, 100]}, 1, 2]}")
                },
                new() { Id = 1, Title = "High Amount" },
                new() { Id = 2, Title = "Low Amount" }
            }
        };

        // Act
        var result = ValidateActionConditions(blueprint);

        // Assert
        result.IsValid.Should().BeTrue("all action conditions are valid JSON Logic");
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateActionConditions_WithInvalidJsonLogic_ShouldFail()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Title = "Invalid Routing",
            Description = "Blueprint with invalid JSON Logic",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new() { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 0,
                    Title = "Start",
                    Condition = JsonNode.Parse("{\"invalid_operator\": [1, 2]}") // Invalid operator
                },
                new() { Id = 1, Title = "Next" }
            }
        };

        // Act
        var result = ValidateActionConditions(blueprint);

        // Assert
        result.IsValid.Should().BeFalse("action 0 has invalid JSON Logic operator");
        result.Errors.Should().Contain(e => e.Contains("Action 0") && e.Contains("condition"));
    }

    [Fact]
    public void ValidateActionRouting_ConditionEvaluatesToNonExistentAction_ShouldFail()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Title = "Invalid Route Target",
            Description = "Condition routes to non-existent action",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new() { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 0,
                    Title = "Start",
                    Condition = JsonNode.Parse("999") // Routes to non-existent action 999
                },
                new() { Id = 1, Title = "Valid Action" }
            }
        };

        // Act
        var result = ValidateRoutingTargets(blueprint);

        // Assert
        result.IsValid.Should().BeFalse("action 0 routes to non-existent action 999");
        result.Errors.Should().Contain(e => e.Contains("Action 0") && e.Contains("999"));
    }

    [Fact]
    public void ValidateWorkflowGraph_WithUnreachableActions_ShouldFail()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Title = "Unreachable Actions",
            Description = "Blueprint with orphaned actions",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new() { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 0,
                    Title = "Start",
                    Condition = JsonNode.Parse("1") // Always go to action 1
                },
                new()
                {
                    Id = 1,
                    Title = "Reachable",
                    Condition = JsonNode.Parse("{\"==\":[0,0]}") // Terminal (always false means end)
                },
                new()
                {
                    Id = 2,
                    Title = "Unreachable", // Never referenced
                    Condition = JsonNode.Parse("{\"==\":[0,0]}")
                }
            }
        };

        // Act
        var result = ValidateWorkflowReachability(blueprint);

        // Assert
        result.IsValid.Should().BeFalse("action 2 is unreachable");
        result.Errors.Should().Contain(e => e.Contains("Action 2") && e.Contains("unreachable"));
    }

    [Fact]
    public void ValidateWorkflowGraph_WithTerminalAction_ShouldPass()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Title = "Terminal Workflow",
            Description = "Blueprint with proper terminal action",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new() { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 0,
                    Title = "Start",
                    Condition = JsonNode.Parse("1")
                },
                new()
                {
                    Id = 1,
                    Title = "Terminal Action",
                    Condition = JsonNode.Parse("{\"==\":[0,0]}") // Default terminal condition
                }
            }
        };

        // Act
        var result = ValidateWorkflowReachability(blueprint);

        // Assert
        result.IsValid.Should().BeTrue("all actions are reachable and terminal action is valid");
        result.Errors.Should().BeEmpty();
    }

    #endregion

    #region 3.2 Action Sequence Validation

    [Fact]
    public void ValidateActionSequence_ActionZeroExists_ShouldPass()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Title = "Valid Start",
            Description = "Blueprint with action 0",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new() { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<BpModels.Action>
            {
                new() { Id = 0, Title = "Start" },
                new() { Id = 1, Title = "Next" }
            }
        };

        // Act
        var result = ValidateActionSequence(blueprint);

        // Assert
        result.IsValid.Should().BeTrue("action 0 exists");
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateActionSequence_MissingActionZero_ShouldFail()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Title = "Missing Start",
            Description = "Blueprint without action 0",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new() { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<BpModels.Action>
            {
                new() { Id = 1, Title = "Not Start" },
                new() { Id = 2, Title = "Next" }
            }
        };

        // Act
        var result = ValidateActionSequence(blueprint);

        // Assert
        result.IsValid.Should().BeFalse("action 0 must exist as starting action");
        result.Errors.Should().Contain(e => e.Contains("Action 0") && e.Contains("must exist"));
    }

    [Fact]
    public void ValidateActionSequence_SequentialIds_ShouldPass()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Title = "Sequential IDs",
            Description = "Blueprint with sequential action IDs",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new() { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<BpModels.Action>
            {
                new() { Id = 0, Title = "Start" },
                new() { Id = 1, Title = "Middle" },
                new() { Id = 2, Title = "End" }
            }
        };

        // Act
        var result = ValidateActionSequence(blueprint);

        // Assert
        result.IsValid.Should().BeTrue("action IDs are sequential starting from 0");
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateActionSequence_DuplicateIds_ShouldFail()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Title = "Duplicate IDs",
            Description = "Blueprint with duplicate action IDs",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new() { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<BpModels.Action>
            {
                new() { Id = 0, Title = "Start" },
                new() { Id = 1, Title = "First Duplicate" },
                new() { Id = 1, Title = "Second Duplicate" }
            }
        };

        // Act
        var result = ValidateActionSequence(blueprint);

        // Assert
        result.IsValid.Should().BeFalse("duplicate action ID 1 exists");
        result.Errors.Should().Contain(e => e.Contains("Duplicate") && e.Contains("1"));
    }

    [Fact]
    public void ValidateActionSequence_NegativeIds_ShouldFail()
    {
        // Arrange
        var blueprint = new BpModels.Blueprint
        {
            Title = "Negative IDs",
            Description = "Blueprint with negative action IDs",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new() { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<BpModels.Action>
            {
                new() { Id = 0, Title = "Start" },
                new() { Id = -1, Title = "Invalid" }
            }
        };

        // Act
        var result = ValidateActionSequence(blueprint);

        // Assert
        result.IsValid.Should().BeFalse("negative action IDs are not allowed");
        result.Errors.Should().Contain(e => e.Contains("negative") || e.Contains("-1"));
    }

    #endregion

    #region 3.3 Graph Cycle Detection

    [Fact]
    public void DetectCycles_SimpleCycle_ShouldFail()
    {
        // Arrange: A → B → A (simple cycle)
        var blueprint = new BpModels.Blueprint
        {
            Title = "Simple Cycle",
            Description = "A → B → A",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new() { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 0,
                    Title = "Action A",
                    Condition = JsonNode.Parse("1") // Goes to action 1
                },
                new()
                {
                    Id = 1,
                    Title = "Action B",
                    Condition = JsonNode.Parse("0") // Goes back to action 0 - CYCLE!
                }
            }
        };

        // Act
        var result = DetectCycles(blueprint);

        // Assert
        result.HasCycle.Should().BeTrue("blueprint contains simple cycle A → B → A");
        result.CyclePath.Should().Contain("0");
        result.CyclePath.Should().Contain("1");
    }

    [Fact]
    public void DetectCycles_ComplexCycle_ShouldFail()
    {
        // Arrange: A → B → C → A (complex cycle)
        var blueprint = new BpModels.Blueprint
        {
            Title = "Complex Cycle",
            Description = "A → B → C → A",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new() { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 0,
                    Title = "Action A",
                    Condition = JsonNode.Parse("1")
                },
                new()
                {
                    Id = 1,
                    Title = "Action B",
                    Condition = JsonNode.Parse("2")
                },
                new()
                {
                    Id = 2,
                    Title = "Action C",
                    Condition = JsonNode.Parse("0") // Back to start - CYCLE!
                }
            }
        };

        // Act
        var result = DetectCycles(blueprint);

        // Assert
        result.HasCycle.Should().BeTrue("blueprint contains complex cycle A → B → C → A");
        result.CyclePath.Should().Contain("0");
        result.CyclePath.Should().Contain("1");
        result.CyclePath.Should().Contain("2");
    }

    [Fact]
    public void DetectCycles_SelfReferencing_ShouldFail()
    {
        // Arrange: A → A (self-referencing)
        var blueprint = new BpModels.Blueprint
        {
            Title = "Self Reference",
            Description = "A → A (infinite loop)",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new() { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 0,
                    Title = "Self Loop",
                    Condition = JsonNode.Parse("0") // Points to itself!
                }
            }
        };

        // Act
        var result = DetectCycles(blueprint);

        // Assert
        result.HasCycle.Should().BeTrue("action 0 references itself");
        result.CyclePath.Should().Contain("0");
    }

    [Fact]
    public void DetectCycles_LinearWorkflow_ShouldPass()
    {
        // Arrange: A → B → C (linear, no cycles)
        var blueprint = new BpModels.Blueprint
        {
            Title = "Linear Workflow",
            Description = "A → B → C (no cycles)",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new() { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 0,
                    Title = "Start",
                    Condition = JsonNode.Parse("1")
                },
                new()
                {
                    Id = 1,
                    Title = "Middle",
                    Condition = JsonNode.Parse("2")
                },
                new()
                {
                    Id = 2,
                    Title = "End",
                    Condition = JsonNode.Parse("{\"==\":[0,0]}") // Terminal
                }
            }
        };

        // Act
        var result = DetectCycles(blueprint);

        // Assert
        result.HasCycle.Should().BeFalse("linear workflow has no cycles");
        result.CyclePath.Should().BeEmpty();
    }

    [Fact]
    public void DetectCycles_BranchingWorkflow_ShouldPass()
    {
        // Arrange: A → (B or C) → D (branching, no cycles)
        var blueprint = new BpModels.Blueprint
        {
            Title = "Branching Workflow",
            Description = "A → (B or C) → D",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new() { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 0,
                    Title = "Start",
                    Condition = JsonNode.Parse("{\"if\": [{\">\": [{\"var\": \"amount\"}, 100]}, 1, 2]}")
                },
                new()
                {
                    Id = 1,
                    Title = "High Amount Path",
                    Condition = JsonNode.Parse("3")
                },
                new()
                {
                    Id = 2,
                    Title = "Low Amount Path",
                    Condition = JsonNode.Parse("3")
                },
                new()
                {
                    Id = 3,
                    Title = "Merge Point",
                    Condition = JsonNode.Parse("{\"==\":[0,0]}") // Terminal
                }
            }
        };

        // Act
        var result = DetectCycles(blueprint);

        // Assert
        result.HasCycle.Should().BeFalse("branching workflow has no cycles");
        result.CyclePath.Should().BeEmpty();
    }

    [Fact]
    public void DetectCycles_TerminalAction_ShouldPass()
    {
        // Arrange: Terminal action (no next action)
        var blueprint = new BpModels.Blueprint
        {
            Title = "Terminal Workflow",
            Description = "Workflow ends properly",
            Participants = new List<BpModels.Participant>
            {
                new() { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new() { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<BpModels.Action>
            {
                new()
                {
                    Id = 0,
                    Title = "Only Action",
                    Condition = JsonNode.Parse("{\"==\":[0,0]}") // Default terminal condition
                }
            }
        };

        // Act
        var result = DetectCycles(blueprint);

        // Assert
        result.HasCycle.Should().BeFalse("terminal action does not create a cycle");
        result.CyclePath.Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Validates that all action conditions are valid JSON Logic.
    /// </summary>
    private ValidationResult ValidateActionConditions(BpModels.Blueprint blueprint)
    {
        var result = new ValidationResult { IsValid = true };

        foreach (var action in blueprint.Actions)
        {
            if (action.Condition == null)
                continue;

            try
            {
                // Try to evaluate the condition with empty data
                var testData = new Dictionary<string, object>();
                _evaluator.Evaluate(action.Condition, testData);
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Action {action.Id} has invalid condition: {ex.Message}");
            }
        }

        return result;
    }

    /// <summary>
    /// Validates that action routing targets reference existing actions.
    /// </summary>
    private static ValidationResult ValidateRoutingTargets(BpModels.Blueprint blueprint)
    {
        var result = new ValidationResult { IsValid = true };
        var validActionIds = blueprint.Actions.Select(a => a.Id).ToHashSet();

        foreach (var action in blueprint.Actions)
        {
            if (action.Condition == null)
                continue;

            // Extract potential action IDs from condition
            var targets = ExtractConditionTargets(action.Condition);

            foreach (var target in targets.Where(t => t >= 0))
            {
                if (!validActionIds.Contains(target))
                {
                    result.IsValid = false;
                    result.Errors.Add($"Action {action.Id} routes to non-existent action {target}");
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Validates that all actions (except terminal) are reachable from action 0.
    /// </summary>
    private static ValidationResult ValidateWorkflowReachability(BpModels.Blueprint blueprint)
    {
        var result = new ValidationResult { IsValid = true };

        // Build reachability graph
        var reachable = new HashSet<int>();
        var toVisit = new Queue<int>();

        // Start from action 0
        if (blueprint.Actions.Any(a => a.Id == 0))
        {
            toVisit.Enqueue(0);
            reachable.Add(0);
        }

        while (toVisit.Count > 0)
        {
            var currentId = toVisit.Dequeue();
            var action = blueprint.Actions.FirstOrDefault(a => a.Id == currentId);

            if (action?.Condition == null)
                continue;

            var targets = ExtractConditionTargets(action.Condition);

            foreach (var target in targets.Where(t => t >= 0 && !reachable.Contains(t)))
            {
                reachable.Add(target);
                toVisit.Enqueue(target);
            }
        }

        // Check for unreachable actions
        foreach (var action in blueprint.Actions)
        {
            if (!reachable.Contains(action.Id))
            {
                result.IsValid = false;
                result.Errors.Add($"Action {action.Id} is unreachable from the workflow start");
            }
        }

        return result;
    }

    /// <summary>
    /// Validates action sequence (IDs must be sequential, starting from 0, no duplicates).
    /// </summary>
    private static ValidationResult ValidateActionSequence(BpModels.Blueprint blueprint)
    {
        var result = new ValidationResult { IsValid = true };

        // Check for action 0
        if (!blueprint.Actions.Any(a => a.Id == 0))
        {
            result.IsValid = false;
            result.Errors.Add("Action 0 must exist as the starting action");
        }

        // Check for duplicates
        var duplicates = blueprint.Actions
            .GroupBy(a => a.Id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var duplicate in duplicates)
        {
            result.IsValid = false;
            result.Errors.Add($"Duplicate action ID: {duplicate}");
        }

        // Check for negative IDs
        var negativeIds = blueprint.Actions.Where(a => a.Id < 0).Select(a => a.Id);

        foreach (var negativeId in negativeIds)
        {
            result.IsValid = false;
            result.Errors.Add($"Action ID cannot be negative: {negativeId}");
        }

        return result;
    }

    /// <summary>
    /// Detects cycles in the workflow graph using DFS.
    /// </summary>
    private static CycleDetectionResult DetectCycles(BpModels.Blueprint blueprint)
    {
        var result = new CycleDetectionResult();
        var visited = new HashSet<int>();
        var recursionStack = new HashSet<int>();
        var path = new List<int>();

        foreach (var action in blueprint.Actions)
        {
            if (!visited.Contains(action.Id))
            {
                if (DfsDetectCycle(action.Id, blueprint, visited, recursionStack, path, result))
                {
                    result.HasCycle = true;
                    result.CyclePath = path.Select(id => id.ToString()).ToList();
                    return result;
                }
            }
        }

        return result;
    }

    /// <summary>
    /// DFS helper for cycle detection.
    /// </summary>
    private static bool DfsDetectCycle(
        int actionId,
        BpModels.Blueprint blueprint,
        HashSet<int> visited,
        HashSet<int> recursionStack,
        List<int> path,
        CycleDetectionResult result)
    {
        visited.Add(actionId);
        recursionStack.Add(actionId);
        path.Add(actionId);

        var action = blueprint.Actions.FirstOrDefault(a => a.Id == actionId);

        if (action?.Condition != null)
        {
            var targets = ExtractConditionTargets(action.Condition);

            foreach (var target in targets.Where(t => t >= 0))
            {
                if (!visited.Contains(target))
                {
                    if (DfsDetectCycle(target, blueprint, visited, recursionStack, path, result))
                        return true;
                }
                else if (recursionStack.Contains(target))
                {
                    // Cycle detected!
                    path.Add(target);
                    return true;
                }
            }
        }

        path.Remove(actionId);
        recursionStack.Remove(actionId);
        return false;
    }

    /// <summary>
    /// Extracts potential action IDs from a JSON Logic condition.
    /// Handles top-level integers and "if" statement results, ignoring comparison operators.
    /// Terminal conditions like {"==": [0,0]} return empty list.
    /// </summary>
    private static List<int> ExtractConditionTargets(JsonNode condition)
    {
        var targets = new List<int>();

        if (condition is JsonValue value)
        {
            // Top-level integer is a direct action reference
            if (value.TryGetValue(out int intValue) && intValue >= 0)
            {
                targets.Add(intValue);
            }
        }
        else if (condition is JsonObject obj)
        {
            // Handle "if" statements: {"if": [condition, thenAction, elseAction]}
            if (obj.TryGetPropertyValue("if", out var ifNode) && ifNode is JsonArray ifArray)
            {
                // Extract action IDs from then/else branches (indices 1 and 2)
                if (ifArray.Count > 1 && ifArray[1] is JsonValue thenValue)
                {
                    if (thenValue.TryGetValue(out int thenInt) && thenInt >= 0)
                        targets.Add(thenInt);
                }
                if (ifArray.Count > 2 && ifArray[2] is JsonValue elseValue)
                {
                    if (elseValue.TryGetValue(out int elseInt) && elseInt >= 0)
                        targets.Add(elseInt);
                }
            }
            // Ignore comparison operators like "==", ">", "<", etc.
            // These are used in terminal conditions and don't represent routing
        }

        return targets.Distinct().ToList();
    }

    private class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    private class CycleDetectionResult
    {
        public bool HasCycle { get; set; }
        public List<string> CyclePath { get; set; } = new();
    }

    #endregion
}
