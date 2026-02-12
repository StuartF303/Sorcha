// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.UI.Core.Models.Dashboard;

/// <summary>
/// View model for dashboard statistics cards.
/// </summary>
public record DashboardStatsViewModel
{
    public int ActiveBlueprints { get; init; }
    public int TotalWallets { get; init; }
    public int RecentTransactions { get; init; }
    public int ConnectedPeers { get; init; }
    public int ActiveRegisters { get; init; }
    public int TotalOrganizations { get; init; }
    public bool IsLoaded { get; init; }
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;
}
