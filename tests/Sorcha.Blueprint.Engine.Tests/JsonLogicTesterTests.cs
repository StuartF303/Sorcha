// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json.Nodes;
using FluentAssertions;
using Sorcha.Blueprint.Engine.Implementation;
using Sorcha.Blueprint.Engine.Testing;
using Xunit;

// Alias to avoid conflict with xUnit.v3's TestResult
using TestResult = Sorcha.Blueprint.Engine.Testing.TestResult;

namespace Sorcha.Blueprint.Engine.Tests;

public class JsonLogicTesterTests
{
    private readonly JsonLogicTester _tester;

    public JsonLogicTesterTests()
    {
        var evaluator = new JsonLogicEvaluator();
        _tester = new JsonLogicTester(evaluator);
    }

    [Fact]
    public async Task RunTestAsync_PassingTest_ReturnsPassedResult()
    {
        // Arrange
        var expression = JsonNode.Parse(@"{
            "">"": [{""var"": ""amount""}, 1000]
        }")!;

        var testCase = new TestCase
        {
            Name = "Amount greater than 1000",
            InputData = new Dictionary<string, object>
            {
                ["amount"] = 1500
            },
            ExpectedOutput = true
        };

        // Act
        var result = await _tester.RunTestAsync(expression, testCase);

        // Assert
        result.Passed.Should().BeTrue();
        result.Name.Should().Be("Amount greater than 1000");
        result.ActualOutput.Should().Be(true);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task RunTestAsync_FailingTest_ReturnsFailedResult()
    {
        // Arrange
        var expression = JsonNode.Parse(@"{
            "">"": [{""var"": ""amount""}, 1000]
        }")!;

        var testCase = new TestCase
        {
            Name = "Amount greater than 1000",
            InputData = new Dictionary<string, object>
            {
                ["amount"] = 500
            },
            ExpectedOutput = true // Expects true but will be false
        };

        // Act
        var result = await _tester.RunTestAsync(expression, testCase);

        // Assert
        result.Passed.Should().BeFalse();
        result.ActualOutput.Should().Be(false);
        result.ExpectedOutput.Should().Be(true);
    }

    [Fact]
    public async Task RunTestAsync_ErrorInExpression_ReturnsErrorResult()
    {
        // Arrange - invalid expression that will throw
        var expression = JsonNode.Parse(@"{
            ""invalidOp"": [{""var"": ""amount""}, 1000]
        }")!;

        var testCase = new TestCase
        {
            Name = "Invalid operator test",
            InputData = new Dictionary<string, object>
            {
                ["amount"] = 1500
            },
            ExpectedOutput = true
        };

        // Act
        var result = await _tester.RunTestAsync(expression, testCase);

        // Assert
        result.Passed.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RunTestsAsync_MultipleTests_ReturnsReport()
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

        var testCases = new[]
        {
            new TestCase
            {
                Name = "High amount routes to director",
                InputData = new Dictionary<string, object> { ["amount"] = 15000 },
                ExpectedOutput = "director"
            },
            new TestCase
            {
                Name = "Medium amount routes to manager",
                InputData = new Dictionary<string, object> { ["amount"] = 7500 },
                ExpectedOutput = "manager"
            },
            new TestCase
            {
                Name = "Low amount auto-approves",
                InputData = new Dictionary<string, object> { ["amount"] = 2000 },
                ExpectedOutput = "auto-approve"
            }
        };

        // Act
        var report = await _tester.RunTestsAsync(expression, testCases);

        // Assert
        report.TotalTests.Should().Be(3);
        report.PassedTests.Should().Be(3);
        report.FailedTests.Should().Be(0);
        report.PassRate.Should().Be(1.0);
        report.Results.Should().HaveCount(3);
        report.Expression.Should().NotBeNull();
    }

    [Fact]
    public async Task RunTestsAsync_SomeFailingTests_ReturnsPartialPassReport()
    {
        // Arrange
        var expression = JsonNode.Parse(@"{
            "">"": [{""var"": ""amount""}, 1000]
        }")!;

        var testCases = new[]
        {
            new TestCase
            {
                Name = "Pass test 1",
                InputData = new Dictionary<string, object> { ["amount"] = 1500 },
                ExpectedOutput = true
            },
            new TestCase
            {
                Name = "Fail test",
                InputData = new Dictionary<string, object> { ["amount"] = 500 },
                ExpectedOutput = true // Wrong expectation
            },
            new TestCase
            {
                Name = "Pass test 2",
                InputData = new Dictionary<string, object> { ["amount"] = 2000 },
                ExpectedOutput = true
            }
        };

        // Act
        var report = await _tester.RunTestsAsync(expression, testCases);

        // Assert
        report.TotalTests.Should().Be(3);
        report.PassedTests.Should().Be(2);
        report.FailedTests.Should().Be(1);
        report.PassRate.Should().BeApproximately(0.666, 0.01);
    }

    [Fact]
    public void CreateTest_FluentBuilder_BuildsCorrectTestCase()
    {
        // Act
        var testCase = JsonLogicTester.CreateTest("Test name")
            .WithDescription("Test description")
            .WithInput("amount", 1500)
            .WithInput("status", "active")
            .ExpectOutput(true)
            .Build();

        // Assert
        testCase.Name.Should().Be("Test name");
        testCase.Description.Should().Be("Test description");
        testCase.InputData.Should().ContainKey("amount");
        testCase.InputData.Should().ContainKey("status");
        testCase.InputData["amount"].Should().Be(1500);
        testCase.ExpectedOutput.Should().Be(true);
    }

    [Fact]
    public void CreateTest_WithInputData_SetsAllData()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["field1"] = "value1",
            ["field2"] = 123
        };

        // Act
        var testCase = JsonLogicTester.CreateTest("Test")
            .WithInputData(data)
            .ExpectOutput("result")
            .Build();

        // Assert
        testCase.InputData.Should().BeEquivalentTo(data);
    }

    [Fact]
    public void TestResult_ToString_FormatsCorrectly()
    {
        // Arrange
        var result = new TestResult
        {
            Name = "Test name",
            Passed = true,
            Duration = TimeSpan.FromMilliseconds(12.34)
        };

        // Act
        var str = result.ToString();

        // Assert
        str.Should().Contain("PASS");
        str.Should().Contain("Test name");
        str.Should().Contain("12.34ms");
    }

    [Fact]
    public void TestReport_ToString_FormatsCorrectly()
    {
        // Arrange
        var report = new TestReport
        {
            TotalTests = 10,
            PassedTests = 8,
            FailedTests = 2,
            TotalDuration = TimeSpan.FromMilliseconds(100)
        };

        // Act
        var str = report.ToString();

        // Assert
        str.Should().Contain("8/10");
        str.Should().Contain("80%");
        str.Should().Contain("100");
    }

    [Fact]
    public async Task RunTestAsync_MeasuresDuration()
    {
        // Arrange
        var expression = JsonNode.Parse(@"{
            "">"": [{""var"": ""amount""}, 1000]
        }")!;

        var testCase = new TestCase
        {
            Name = "Duration test",
            InputData = new Dictionary<string, object> { ["amount"] = 1500 },
            ExpectedOutput = true
        };

        // Act
        var result = await _tester.RunTestAsync(expression, testCase);

        // Assert
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task RunTestsAsync_MeasuresTotalDuration()
    {
        // Arrange
        var expression = JsonNode.Parse(@"{
            "">"": [{""var"": ""amount""}, 1000]
        }")!;

        var testCases = new[]
        {
            new TestCase
            {
                Name = "Test 1",
                InputData = new Dictionary<string, object> { ["amount"] = 1500 },
                ExpectedOutput = true
            },
            new TestCase
            {
                Name = "Test 2",
                InputData = new Dictionary<string, object> { ["amount"] = 2500 },
                ExpectedOutput = true
            }
        };

        // Act
        var report = await _tester.RunTestsAsync(expression, testCases);

        // Assert
        report.TotalDuration.Should().BeGreaterThan(TimeSpan.Zero);
        report.TotalDuration.Should().BeGreaterThanOrEqualTo(
            TimeSpan.FromTicks(report.Results.Sum(r => r.Duration.Ticks))
        );
    }

    [Fact]
    public async Task RunTestAsync_ComplexExpression_ComparesCorrectly()
    {
        // Arrange
        var expression = JsonNode.Parse(@"{
            ""and"": [
                {"">"": [{""var"": ""amount""}, 1000]},
                {""=="": [{""var"": ""status""}, ""active""]}
            ]
        }")!;

        var testCase = new TestCase
        {
            Name = "Complex condition test",
            InputData = new Dictionary<string, object>
            {
                ["amount"] = 1500,
                ["status"] = "active"
            },
            ExpectedOutput = true
        };

        // Act
        var result = await _tester.RunTestAsync(expression, testCase);

        // Assert
        result.Passed.Should().BeTrue();
    }
}
