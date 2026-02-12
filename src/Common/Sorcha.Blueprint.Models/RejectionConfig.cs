// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using System.Text.Json.Serialization;
using DataAnnotations = System.ComponentModel.DataAnnotations;

namespace Sorcha.Blueprint.Models;

/// <summary>
/// Configures rejection routing for an action.
/// Defines where the workflow routes when a participant rejects inbound data.
/// </summary>
public class RejectionConfig
{
    /// <summary>
    /// Target action ID for rejection routing.
    /// The workflow routes to this action when the current action is rejected.
    /// </summary>
    [DataAnnotations.Required]
    [JsonPropertyName("targetActionId")]
    public int TargetActionId { get; set; }

    /// <summary>
    /// Override participant ID for rejection target.
    /// If not specified, uses the target action's default sender.
    /// </summary>
    [JsonPropertyName("targetParticipantId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TargetParticipantId { get; set; }

    /// <summary>
    /// Whether a rejection reason is required.
    /// Default is true - rejections must include a reason.
    /// </summary>
    [JsonPropertyName("requireReason")]
    public bool RequireReason { get; set; } = true;

    /// <summary>
    /// Optional JSON Schema for rejection data.
    /// Allows structured rejection feedback beyond simple reason text.
    /// </summary>
    [JsonPropertyName("rejectionSchema")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonElement? RejectionSchema { get; set; }

    /// <summary>
    /// Whether rejection terminates the entire workflow.
    /// If true, the workflow moves to Rejected state instead of routing.
    /// Default is false - rejection routes to target action.
    /// </summary>
    [JsonPropertyName("isTerminal")]
    public bool IsTerminal { get; set; } = false;
}
