// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Blueprint.Service.Models.Responses;

/// <summary>
/// Response containing available blueprints for a wallet/register
/// </summary>
public record AvailableBlueprintsResponse
{
    /// <summary>
    /// The wallet address
    /// </summary>
    public required string WalletAddress { get; init; }

    /// <summary>
    /// The register address
    /// </summary>
    public required string RegisterAddress { get; init; }

    /// <summary>
    /// List of blueprints available to this wallet
    /// </summary>
    public required List<BlueprintInfo> Blueprints { get; init; }
}

/// <summary>
/// Information about a blueprint and its available actions
/// </summary>
public record BlueprintInfo
{
    /// <summary>
    /// The blueprint ID
    /// </summary>
    public required string BlueprintId { get; init; }

    /// <summary>
    /// The blueprint title
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// The blueprint description
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The published version
    /// </summary>
    public int Version { get; init; }

    /// <summary>
    /// Available actions for the wallet
    /// </summary>
    public required List<ActionInfo> AvailableActions { get; init; }
}

/// <summary>
/// Information about an available action
/// </summary>
public record ActionInfo
{
    /// <summary>
    /// The action ID
    /// </summary>
    public required string ActionId { get; init; }

    /// <summary>
    /// The action title
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// The action description
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Whether the action is currently available based on routing rules
    /// </summary>
    public bool IsAvailable { get; init; }

    /// <summary>
    /// Required schema for the action payload
    /// </summary>
    public string? DataSchema { get; init; }
}
