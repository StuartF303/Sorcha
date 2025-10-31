// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DataAnnotations = System.ComponentModel.DataAnnotations;

namespace Sorcha.Blueprint.Models;

/// <summary>
/// Defines an action within a Blueprint workflow
/// Specifies what data is received and returned for the next participant
/// </summary>
public class Action
{
    /// <summary>
    /// The Action ID (typically matches the transaction ID that contains it)
    /// </summary>
    [DataAnnotations.Required]
    [DataAnnotations.MaxLength(64)]
    [JsonPropertyName("id")]
    public int Id { get; set; } = 0;

    /// <summary>
    /// Reference to the previous transaction ID
    /// </summary>
    [DataAnnotations.MaxLength(64)]
    [JsonPropertyName("previousTxId")]
    public string PreviousTxId { get; set; } = string.Empty;

    /// <summary>
    /// Blueprint ID this action belongs to
    /// </summary>
    [DataAnnotations.MaxLength(64)]
    [JsonPropertyName("blueprint")]
    public string BlueprintId { get; set; } = string.Empty;

    /// <summary>
    /// Title for this action (e.g., "Apply", "Endorse", "Approve")
    /// </summary>
    [DataAnnotations.Required]
    [DataAnnotations.MinLength(1)]
    [DataAnnotations.MaxLength(100)]
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this action does
    /// </summary>
    [DataAnnotations.MaxLength(2048)]
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Address of the sender (may be a stealth/derived address)
    /// </summary>
    [DataAnnotations.MaxLength(100)]
    [JsonPropertyName("sender")]
    public string Sender { get; set; } = string.Empty;

    /// <summary>
    /// Potential participants to enact this step (supports branching/routing)
    /// </summary>
    [JsonPropertyName("participants")]
    public IEnumerable<Condition>? Participants { get; set; } = [];

    /// <summary>
    /// List of required action data IDs
    /// </summary>
    [JsonPropertyName("requiredActionData")]
    public IEnumerable<string> RequiredActionData { get; set; } = [];

    /// <summary>
    /// Additional recipients (excludes the sender)
    /// </summary>
    [DataAnnotations.MinLength(0)]
    [JsonPropertyName("additionalRecipients")]
    public IEnumerable<string> AdditionalRecipients { get; set; } = [];

    /// <summary>
    /// List of disclosures defining data visibility rules
    /// </summary>
    [DataAnnotations.MinLength(1)]
    [JsonPropertyName("disclosures")]
    public IEnumerable<Disclosure> Disclosures { get; set; } = [];

    /// <summary>
    /// JSON of data elements received from previous action
    /// </summary>
    [JsonPropertyName("previousData")]
    public JsonDocument? PreviousData { get; set; }

    /// <summary>
    /// JSON schemas for data elements to pass to next participant
    /// </summary>
    [DataAnnotations.MinLength(0)]
    [JsonPropertyName("dataSchemas")]
    public IEnumerable<JsonDocument>? DataSchemas { get; set; }

    /// <summary>
    /// Routing condition (JSON Logic) that resolves to the next action number
    /// </summary>
    [JsonPropertyName("condition")]
    public JsonNode? Condition { get; set; } = JsonNode.Parse("{\"==\":[0,0]}");

    /// <summary>
    /// User-defined calculations performed on submitted data (JSON Logic)
    /// </summary>
    [JsonPropertyName("calculations")]
    public Dictionary<string, JsonNode>? Calculations { get; set; } = [];

    /// <summary>
    /// Specifies the format of data presentation (UI form)
    /// </summary>
    [JsonPropertyName("form")]
    public Control? Form { get; set; } = new()
    {
        ControlType = ControlTypes.Layout,
        Layout = LayoutTypes.VerticalLayout
    };
}
