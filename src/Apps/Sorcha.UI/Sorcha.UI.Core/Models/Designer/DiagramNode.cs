// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Blazor.Diagrams.Core.Geometry;

namespace Sorcha.UI.Core.Models.Designer;

/// <summary>
/// A positioned action node in the auto-layout diagram.
/// </summary>
public record DiagramNode(
    int ActionId,
    string Title,
    string SenderParticipantId,
    int Layer,
    Point Position,
    bool IsStarting,
    bool IsTerminal,
    bool IsCycleTarget,
    string DetailSummary);
