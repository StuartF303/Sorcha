// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Blueprint.Models.JsonLd;
using Xunit;

namespace Sorcha.Blueprint.Models.Tests.JsonLd;

public class JsonLdTypeHelperTests
{
    [Theory]
    [InlineData("Self", "schema:Person")]
    [InlineData("Individual", "schema:Person")]
    [InlineData("", "schema:Person")]
    [InlineData(null, "schema:Person")]
    public void GetParticipantType_WithPersonIndicators_ShouldReturnPerson(string organisationName, string expectedType)
    {
        // Act
        var type = JsonLdTypeHelper.GetParticipantType(organisationName);

        // Assert
        Assert.Equal(expectedType, type);
    }

    [Theory]
    [InlineData("Acme Corp")]
    [InlineData("Global Bank Inc")]
    [InlineData("Some Organization")]
    public void GetParticipantType_WithOrganizationName_ShouldReturnOrganization(string organisationName)
    {
        // Act
        var type = JsonLdTypeHelper.GetParticipantType(organisationName);

        // Assert
        Assert.Equal(JsonLdTypes.Organization, type);
    }

    [Theory]
    [InlineData("Submit Application", "as:Create")]
    [InlineData("Create Order", "as:Create")]
    [InlineData("Apply for Loan", "as:Create")]
    public void GetActionType_WithCreateActions_ShouldReturnCreateAction(string title, string expectedType)
    {
        // Act
        var type = JsonLdTypeHelper.GetActionType(title);

        // Assert
        Assert.Equal(expectedType, type);
    }

    [Theory]
    [InlineData("Approve Request", "as:Accept")]
    [InlineData("Accept Order", "as:Accept")]
    [InlineData("Endorse Application", "as:Accept")]
    public void GetActionType_WithAcceptActions_ShouldReturnAcceptAction(string title, string expectedType)
    {
        // Act
        var type = JsonLdTypeHelper.GetActionType(title);

        // Assert
        Assert.Equal(expectedType, type);
    }

    [Theory]
    [InlineData("Reject Application", "as:Reject")]
    [InlineData("Deny Request", "as:Reject")]
    [InlineData("Decline Order", "as:Reject")]
    public void GetActionType_WithRejectActions_ShouldReturnRejectAction(string title, string expectedType)
    {
        // Act
        var type = JsonLdTypeHelper.GetActionType(title);

        // Assert
        Assert.Equal(expectedType, type);
    }

    [Theory]
    [InlineData("Update Profile", "as:Update")]
    [InlineData("Modify Settings", "as:Update")]
    [InlineData("Edit Document", "as:Update")]
    public void GetActionType_WithUpdateActions_ShouldReturnUpdateAction(string title, string expectedType)
    {
        // Act
        var type = JsonLdTypeHelper.GetActionType(title);

        // Assert
        Assert.Equal(expectedType, type);
    }

    [Theory]
    [InlineData("Review Document")]
    [InlineData("Process Payment")]
    [InlineData("Verify Identity")]
    public void GetActionType_WithGenericActions_ShouldReturnActivity(string title)
    {
        // Act
        var type = JsonLdTypeHelper.GetActionType(title);

        // Assert
        Assert.Equal(JsonLdTypes.Activity, type);
    }
}
