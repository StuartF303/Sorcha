// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Blueprint.Models.JsonLd;

/// <summary>
/// JSON-LD types for Sorcha blueprint entities
/// </summary>
public static class JsonLdTypes
{
    // Blueprint types
    public const string Blueprint = "Blueprint";
    public const string Action = "Action";
    public const string Participant = "Participant";

    // Schema.org types
    public const string Person = "schema:Person";
    public const string Organization = "schema:Organization";
    public const string WebApplication = "schema:WebApplication";
    public const string Order = "schema:Order";
    public const string Product = "schema:Product";
    public const string LoanApplication = "schema:LoanOrCredit";

    // ActivityStreams types
    public const string Activity = "as:Activity";
    public const string CreateAction = "as:Create";
    public const string UpdateAction = "as:Update";
    public const string AcceptAction = "as:Accept";
    public const string RejectAction = "as:Reject";
}

/// <summary>
/// Helper methods for JSON-LD type handling
/// </summary>
public static class JsonLdTypeHelper
{
    /// <summary>
    /// Gets the appropriate JSON-LD type for a participant based on organization name
    /// </summary>
    public static string GetParticipantType(string organisationName)
    {
        // If organisation name suggests it's a person (e.g., "Self"), use Person
        if (string.IsNullOrWhiteSpace(organisationName) ||
            organisationName.Equals("Self", StringComparison.OrdinalIgnoreCase) ||
            organisationName.Equals("Individual", StringComparison.OrdinalIgnoreCase))
        {
            return JsonLdTypes.Person;
        }

        return JsonLdTypes.Organization;
    }

    /// <summary>
    /// Maps action title to ActivityStreams type
    /// </summary>
    public static string GetActionType(string actionTitle)
    {
        var title = actionTitle.ToLowerInvariant();

        if (title.Contains("create") || title.Contains("submit") || title.Contains("apply"))
            return JsonLdTypes.CreateAction;

        if (title.Contains("update") || title.Contains("modify") || title.Contains("edit"))
            return JsonLdTypes.UpdateAction;

        if (title.Contains("accept") || title.Contains("approve") || title.Contains("endorse"))
            return JsonLdTypes.AcceptAction;

        if (title.Contains("reject") || title.Contains("deny") || title.Contains("decline"))
            return JsonLdTypes.RejectAction;

        return JsonLdTypes.Activity;
    }
}
