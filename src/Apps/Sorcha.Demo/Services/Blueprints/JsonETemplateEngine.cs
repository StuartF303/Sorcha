// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.More;

namespace Sorcha.Demo.Services.Blueprints;

/// <summary>
/// Processes JSON-e templates with runtime context for dynamic value injection
/// </summary>
public class JsonETemplateEngine
{
    private readonly ILogger<JsonETemplateEngine> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public JsonETemplateEngine(ILogger<JsonETemplateEngine> logger)
    {
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Processes a JSON-e template with the provided context
    /// </summary>
    /// <param name="templateJson">JSON-e template string containing $eval expressions</param>
    /// <param name="context">Runtime context values (e.g., wallet addresses)</param>
    /// <returns>Processed JSON string with $eval expressions replaced by actual values</returns>
    public string ProcessTemplate(string templateJson, Dictionary<string, object> context)
    {
        try
        {
            _logger.LogDebug("Processing JSON-e template with {ContextKeyCount} context keys",
                context.Keys.Count);

            // Parse template JSON to JsonNode
            var templateNode = JsonNode.Parse(templateJson);

            if (templateNode == null)
            {
                throw new InvalidOperationException("Failed to parse template JSON");
            }

            // Convert context dictionary to JsonObject
            var contextObject = CreateContextObject(context);

            // Evaluate JSON-e template with context
            var resultNode = Json.JsonE.JsonE.Evaluate(templateNode, contextObject);

            if (resultNode == null)
            {
                throw new InvalidOperationException("JSON-e evaluation returned null");
            }

            // Serialize result back to JSON string
            var resultJson = resultNode.ToJsonString(_jsonOptions);

            _logger.LogInformation("Successfully processed JSON-e template, result size: {Size} bytes",
                resultJson.Length);

            return resultJson;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error while processing template");
            throw new InvalidOperationException("Failed to parse JSON-e template", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process JSON-e template");
            throw;
        }
    }

    /// <summary>
    /// Creates a wallet address context from participant wallets for JSON-e evaluation
    /// </summary>
    /// <param name="participantWallets">Dictionary mapping participant IDs to wallet addresses</param>
    /// <returns>Context dictionary ready for JSON-e processing</returns>
    public Dictionary<string, object> CreateWalletContext(Dictionary<string, string> participantWallets)
    {
        _logger.LogDebug("Creating wallet context for {ParticipantCount} participants",
            participantWallets.Count);

        return new Dictionary<string, object>
        {
            ["walletAddresses"] = participantWallets
        };
    }

    /// <summary>
    /// Converts a context dictionary to a JsonObject for JSON-e evaluation
    /// </summary>
    private JsonObject CreateContextObject(Dictionary<string, object> context)
    {
        // Serialize the dictionary to JSON and parse back to JsonObject
        var contextJson = JsonSerializer.Serialize(context, _jsonOptions);
        var contextNode = JsonNode.Parse(contextJson);

        if (contextNode is not JsonObject contextObject)
        {
            throw new InvalidOperationException("Context must be a JSON object");
        }

        return contextObject;
    }

    /// <summary>
    /// Validates that a template contains the expected JSON-e expressions
    /// </summary>
    /// <param name="templateJson">Template JSON to validate</param>
    /// <returns>True if template contains $eval expressions</returns>
    public bool ContainsJsonEExpressions(string templateJson)
    {
        return templateJson.Contains("\"$eval\"", StringComparison.Ordinal);
    }
}
