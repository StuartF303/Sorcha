// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Blueprint.Models;
using Action = Sorcha.Blueprint.Models.Action;

namespace Sorcha.Blueprint.Engine;

/// <summary>
/// Extension methods for Blueprint to support O(1) action lookups.
/// </summary>
public static class BlueprintExtensions
{
    /// <summary>
    /// Build a dictionary index of actions by their ID for O(1) lookup.
    /// Call once per blueprint per operation to avoid repeated O(n) scans.
    /// </summary>
    public static Dictionary<int, Action> BuildActionIndex(this Sorcha.Blueprint.Models.Blueprint blueprint)
    {
        if (blueprint.Actions == null || blueprint.Actions.Count == 0)
            return new Dictionary<int, Action>();

        return blueprint.Actions.ToDictionary(a => a.Id);
    }
}
