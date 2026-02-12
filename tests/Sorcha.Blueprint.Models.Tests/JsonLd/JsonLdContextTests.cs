// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Blueprint.Models.JsonLd;
using System.Text.Json.Nodes;
using Xunit;

namespace Sorcha.Blueprint.Models.Tests.JsonLd;

public class JsonLdContextTests
{
    [Fact]
    public void DefaultContext_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var context = JsonLdContext.DefaultContext;

        // Assert
        Assert.NotNull(context);
        var contextObj = context as JsonObject;
        Assert.NotNull(contextObj);
        Assert.Contains("@vocab", contextObj);
        Assert.Contains("schema", contextObj);
        Assert.Contains("did", contextObj);
        Assert.Contains("xsd", contextObj);
    }

    [Fact]
    public void SupplyChainContext_ShouldIncludeGS1Vocabulary()
    {
        // Arrange & Act
        var context = JsonLdContext.SupplyChainContext;

        // Assert
        var contextObj = context as JsonObject;
        Assert.NotNull(contextObj);
        Assert.Contains("gs1", contextObj);
        Assert.Contains("Order", contextObj);
        Assert.Contains("Product", contextObj);
    }

    [Fact]
    public void FinanceContext_ShouldIncludeLoanTerms()
    {
        // Arrange & Act
        var context = JsonLdContext.FinanceContext;

        // Assert
        var contextObj = context as JsonObject;
        Assert.NotNull(contextObj);
        Assert.Contains("LoanApplication", contextObj);
        Assert.Contains("loanAmount", contextObj);
        Assert.Contains("creditScore", contextObj);
    }

    [Theory]
    [InlineData("supply-chain")]
    [InlineData("finance")]
    [InlineData("loan")]
    [InlineData(null)]
    [InlineData("unknown")]
    public void GetContextByCategory_ShouldReturnValidContext(string? category)
    {
        // Arrange & Act
        var context = JsonLdContext.GetContextByCategory(category);

        // Assert
        Assert.NotNull(context);
        var contextObj = context as JsonObject;
        Assert.NotNull(contextObj);
    }

    [Fact]
    public void GetContextByCategory_SupplyChain_ShouldReturnSupplyChainContext()
    {
        // Arrange & Act
        var context = JsonLdContext.GetContextByCategory("supply-chain");

        // Assert
        var contextObj = context as JsonObject;
        Assert.NotNull(contextObj);
        Assert.Contains("gs1", contextObj);
    }

    [Fact]
    public void GetContextByCategory_Finance_ShouldReturnFinanceContext()
    {
        // Arrange & Act
        var context = JsonLdContext.GetContextByCategory("finance");

        // Assert
        var contextObj = context as JsonObject;
        Assert.NotNull(contextObj);
        Assert.Contains("LoanApplication", contextObj);
    }

    [Fact]
    public void MergeContexts_WithObject_ShouldMergeProperties()
    {
        // Arrange
        var customContext = JsonNode.Parse(@"{""custom"": ""value"", ""another"": ""test""}")!;

        // Act
        var merged = JsonLdContext.MergeContexts(customContext);

        // Assert
        var mergedObj = merged as JsonObject;
        Assert.NotNull(mergedObj);
        Assert.Contains("@vocab", mergedObj); // From default
        Assert.Contains("custom", mergedObj); // From custom
        Assert.Contains("another", mergedObj); // From custom
    }

    [Fact]
    public void MergeContexts_WithArray_ShouldPrependDefaultContext()
    {
        // Arrange
        var customContext = new JsonArray
        {
            JsonNode.Parse(@"{""custom"": ""value""}")!
        };

        // Act
        var merged = JsonLdContext.MergeContexts(customContext);

        // Assert
        var mergedArray = merged as JsonArray;
        Assert.NotNull(mergedArray);
        Assert.Equal(2, mergedArray.Count);
    }

    [Fact]
    public void HasJsonLdContext_WithContext_ShouldReturnTrue()
    {
        // Arrange
        var node = JsonNode.Parse(@"{""@context"": {""test"": ""value""}, ""id"": ""123""}")!;

        // Act
        var hasContext = JsonLdContext.HasJsonLdContext(node);

        // Assert
        Assert.True(hasContext);
    }

    [Fact]
    public void HasJsonLdContext_WithoutContext_ShouldReturnFalse()
    {
        // Arrange
        var node = JsonNode.Parse(@"{""id"": ""123"", ""title"": ""Test""}")!;

        // Act
        var hasContext = JsonLdContext.HasJsonLdContext(node);

        // Assert
        Assert.False(hasContext);
    }

    [Fact]
    public void ExtractContext_WithContext_ShouldReturnContext()
    {
        // Arrange
        var node = JsonNode.Parse(@"{""@context"": {""test"": ""value""}, ""id"": ""123""}")!;

        // Act
        var context = JsonLdContext.ExtractContext(node);

        // Assert
        Assert.NotNull(context);
        var contextObj = context as JsonObject;
        Assert.NotNull(contextObj);
        Assert.Contains("test", contextObj);
    }

    [Fact]
    public void ExtractContext_WithoutContext_ShouldReturnNull()
    {
        // Arrange
        var node = JsonNode.Parse(@"{""id"": ""123"", ""title"": ""Test""}")!;

        // Act
        var context = JsonLdContext.ExtractContext(node);

        // Assert
        Assert.Null(context);
    }
}
