// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.EntityFrameworkCore;
using Sorcha.Blueprint.Service.Models;

namespace Sorcha.Blueprint.Service.Data;

/// <summary>
/// PostgreSQL context for activity events storage.
/// Separate from the Blueprint Service's existing MongoDB collections.
/// </summary>
public class BlueprintEventsDbContext : DbContext
{
    public BlueprintEventsDbContext(DbContextOptions<BlueprintEventsDbContext> options)
        : base(options)
    {
    }

    public DbSet<ActivityEvent> ActivityEvents => Set<ActivityEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityEvent>(entity =>
        {
            entity.ToTable("ActivityEvents");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Severity).IsRequired()
                .HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.SourceService).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EntityId).HasMaxLength(200);
            entity.Property(e => e.EntityType).HasMaxLength(50);

            // Indexes — only for relational providers
            var isInMemory = Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
            if (!isInMemory)
            {
                entity.HasIndex(e => new { e.UserId, e.CreatedAt })
                    .HasDatabaseName("IX_ActivityEvent_UserId_CreatedAt")
                    .IsDescending(false, true);
                entity.HasIndex(e => new { e.OrganizationId, e.CreatedAt })
                    .HasDatabaseName("IX_ActivityEvent_OrgId_CreatedAt")
                    .IsDescending(false, true);
                entity.HasIndex(e => e.ExpiresAt)
                    .HasDatabaseName("IX_ActivityEvent_ExpiresAt");
                entity.HasIndex(e => new { e.UserId, e.IsRead })
                    .HasDatabaseName("IX_ActivityEvent_UserId_IsRead")
                    .HasFilter("\"IsRead\" = false");
            }
        });
    }
}
