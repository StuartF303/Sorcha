// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.UI.Core.Services;

/// <summary>
/// Configuration options for the Chat Hub connection.
/// </summary>
public class ChatHubOptions
{
    /// <summary>
    /// Base URL for the Blueprint service where the Chat Hub is hosted.
    /// </summary>
    public required string BlueprintServiceUrl { get; init; }

    /// <summary>
    /// Profile name for authentication token retrieval.
    /// </summary>
    public required string ProfileName { get; init; }
}
