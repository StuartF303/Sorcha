// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json.Nodes;
using System.Text.Json;

namespace Sorcha.Blueprint.Models.JsonLd;

/// <summary>
/// Provides JSON-LD context definitions for Sorcha blueprints
/// </summary>
public static class JsonLdContext
{
    /// <summary>
    /// Default JSON-LD context for Sorcha blueprints
    /// Maps blueprint fields to standard vocabularies (schema.org, W3C DID, etc.)
    /// </summary>
    public static readonly JsonNode DefaultContext = JsonNode.Parse(@"
    {
      ""@vocab"": ""https://sorcha.io/blueprint/v1#"",
      ""schema"": ""https://schema.org/"",
      ""gs1"": ""https://gs1.org/voc/"",
      ""did"": ""https://www.w3.org/ns/did#"",
      ""xsd"": ""http://www.w3.org/2001/XMLSchema#"",
      ""as"": ""https://www.w3.org/ns/activitystreams#"",

      ""id"": ""@id"",
      ""type"": ""@type"",

      ""Blueprint"": ""schema:WebApplication"",
      ""Participant"": ""schema:Person"",
      ""Action"": ""as:Activity"",

      ""title"": ""schema:name"",
      ""description"": ""schema:description"",
      ""name"": ""schema:name"",
      ""organisation"": ""schema:affiliation"",

      ""createdAt"": {
        ""@id"": ""schema:dateCreated"",
        ""@type"": ""xsd:dateTime""
      },
      ""updatedAt"": {
        ""@id"": ""schema:dateModified"",
        ""@type"": ""xsd:dateTime""
      },

      ""didUri"": {
        ""@id"": ""did:Document"",
        ""@type"": ""@id""
      },
      ""walletAddress"": ""did:walletAddress"",

      ""participants"": ""schema:participant"",
      ""actions"": ""schema:potentialAction"",

      ""sender"": {
        ""@id"": ""as:actor"",
        ""@type"": ""@id""
      },
      ""target"": {
        ""@id"": ""as:target"",
        ""@type"": ""@id""
      }
    }")!;

    /// <summary>
    /// Extended context for supply chain and GS1 integration
    /// </summary>
    public static readonly JsonNode SupplyChainContext = JsonNode.Parse(@"
    {
      ""@vocab"": ""https://sorcha.io/blueprint/v1#"",
      ""schema"": ""https://schema.org/"",
      ""gs1"": ""https://gs1.org/voc/"",
      ""did"": ""https://www.w3.org/ns/did#"",
      ""xsd"": ""http://www.w3.org/2001/XMLSchema#"",
      ""as"": ""https://www.w3.org/ns/activitystreams#"",

      ""id"": ""@id"",
      ""type"": ""@type"",

      ""Order"": ""schema:Order"",
      ""Product"": ""schema:Product"",
      ""Organization"": ""schema:Organization"",

      ""orderNumber"": ""schema:orderNumber"",
      ""productId"": ""gs1:gtin"",
      ""productName"": ""schema:name"",
      ""quantity"": ""schema:orderQuantity"",
      ""unitPrice"": ""schema:price"",
      ""trackingNumber"": ""gs1:trackingNumber"",
      ""deliveryAddress"": ""schema:deliveryAddress"",
      ""expectedDeliveryDate"": ""schema:expectedDeliveryDate"",
      ""paymentTerms"": ""schema:paymentDue"",

      ""amount"": {
        ""@id"": ""schema:amount"",
        ""@type"": ""schema:MonetaryAmount""
      }
    }")!;

    /// <summary>
    /// Context for financial/loan applications
    /// </summary>
    public static readonly JsonNode FinanceContext = JsonNode.Parse(@"
    {
      ""@vocab"": ""https://sorcha.io/blueprint/v1#"",
      ""schema"": ""https://schema.org/"",
      ""did"": ""https://www.w3.org/ns/did#"",
      ""xsd"": ""http://www.w3.org/2001/XMLSchema#"",

      ""id"": ""@id"",
      ""type"": ""@type"",

      ""LoanApplication"": ""schema:LoanOrCredit"",
      ""applicant"": {
        ""@id"": ""schema:customer"",
        ""@type"": ""schema:Person""
      },
      ""loanAmount"": {
        ""@id"": ""schema:amount"",
        ""@type"": ""schema:MonetaryAmount""
      },
      ""interestRate"": ""schema:annualPercentageRate"",
      ""loanTerm"": ""schema:loanTerm"",
      ""creditScore"": ""schema:creditRating"",

      ""givenName"": ""schema:givenName"",
      ""familyName"": ""schema:familyName"",
      ""email"": ""schema:email""
    }")!;

    /// <summary>
    /// Merges a custom context with the default context
    /// </summary>
    /// <param name="customContext">Custom context to merge</param>
    /// <returns>Merged JSON-LD context</returns>
    public static JsonNode MergeContexts(JsonNode customContext)
    {
        if (customContext is JsonArray contextArray)
        {
            // If already an array, prepend default context
            var merged = new JsonArray { DefaultContext.DeepClone() };
            foreach (var item in contextArray)
            {
                if (item != null)
                    merged.Add(item.DeepClone());
            }
            return merged;
        }
        else if (customContext is JsonObject customObject)
        {
            // Merge objects
            var defaultObject = DefaultContext.DeepClone() as JsonObject;
            foreach (var kvp in customObject)
            {
                defaultObject![kvp.Key] = kvp.Value?.DeepClone();
            }
            return defaultObject!;
        }

        return DefaultContext.DeepClone();
    }

    /// <summary>
    /// Gets context by category
    /// </summary>
    /// <param name="category">Category name (e.g., "supply-chain", "finance")</param>
    /// <returns>Appropriate JSON-LD context</returns>
    public static JsonNode GetContextByCategory(string? category)
    {
        return category?.ToLowerInvariant() switch
        {
            "supply-chain" => SupplyChainContext.DeepClone(),
            "finance" => FinanceContext.DeepClone(),
            "loan" => FinanceContext.DeepClone(),
            _ => DefaultContext.DeepClone()
        };
    }

    /// <summary>
    /// Creates a compact context reference URL
    /// </summary>
    /// <param name="category">Blueprint category</param>
    /// <returns>Context URL or embedded context</returns>
    public static object GetContextReference(string? category = null)
    {
        // In production, these would be hosted at actual URLs
        // For now, return embedded contexts
        return GetContextByCategory(category);
    }

    /// <summary>
    /// Validates if a JSON node contains JSON-LD context
    /// </summary>
    public static bool HasJsonLdContext(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            return obj.ContainsKey("@context");
        }
        return false;
    }

    /// <summary>
    /// Extracts JSON-LD context from a JSON node
    /// </summary>
    public static JsonNode? ExtractContext(JsonNode? node)
    {
        if (node is JsonObject obj && obj.TryGetPropertyValue("@context", out var context))
        {
            return context;
        }
        return null;
    }
}
