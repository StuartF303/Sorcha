// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sorcha.Peer.Service.Data;

/// <summary>
/// Design-time factory for PeerDbContext (used by EF Core migrations tooling)
/// </summary>
public class PeerDbContextFactory : IDesignTimeDbContextFactory<PeerDbContext>
{
    public PeerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PeerDbContext>();

        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=sorcha_peer;Username=postgres;Password=postgres",
            npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "peer");
            });

        return new PeerDbContext(optionsBuilder.Options);
    }
}
