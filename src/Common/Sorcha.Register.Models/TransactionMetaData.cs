// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Register.Models.Enums;

namespace Sorcha.Register.Models;

/// <summary>
/// Metadata for blueprint workflow tracking
/// </summary>
public class TransactionMetaData
{
    /// <summary>
    /// Register this transaction belongs to
    /// </summary>
    public string RegisterId { get; set; } = string.Empty;

    /// <summary>
    /// Type of transaction
    /// </summary>
    public TransactionType TransactionType { get; set; }

    /// <summary>
    /// Blueprint definition identifier
    /// </summary>
    public string? BlueprintId { get; set; }

    /// <summary>
    /// Blueprint instance identifier
    /// </summary>
    public string? InstanceId { get; set; }

    /// <summary>
    /// Current action/step in blueprint
    /// </summary>
    public uint? ActionId { get; set; }

    /// <summary>
    /// Next action/step in blueprint
    /// </summary>
    public uint? NextActionId { get; set; }

    /// <summary>
    /// Custom tracking data (JSON serialized)
    /// </summary>
    public SortedList<string, string>? TrackingData { get; set; }
}
