// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sorcha.Wallet.Core.Data;

/// <summary>
/// Design-time factory for WalletDbContext.
/// Used by EF Core tools for migrations.
/// </summary>
public class WalletDbContextFactory : IDesignTimeDbContextFactory<WalletDbContext>
{
    public WalletDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WalletDbContext>();

        // Use a placeholder connection string for design-time operations
        // The actual connection string is provided at runtime via configuration
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=sorcha_wallet;Username=postgres;Password=postgres",
            npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "wallet");
            });

        return new WalletDbContext(optionsBuilder.Options);
    }
}
