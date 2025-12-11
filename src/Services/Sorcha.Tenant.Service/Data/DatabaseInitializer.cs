// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.EntityFrameworkCore;
using Sorcha.Tenant.Service.Models;

namespace Sorcha.Tenant.Service.Data;

/// <summary>
/// Handles database initialization including migrations and seed data.
/// Runs automatically on application startup.
/// </summary>
public class DatabaseInitializer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly IConfiguration _configuration;

    // Well-known IDs for default organization and admin
    public static readonly Guid DefaultOrganizationId = new("00000000-0000-0000-0000-000000000001");
    public static readonly Guid DefaultAdminUserId = new("00000000-0000-0000-0001-000000000001");

    // Default credentials (can be overridden via configuration)
    public const string DefaultAdminEmail = "admin@sorcha.local";
    public const string DefaultAdminPassword = "Dev_Pass_2025!";
    public const string DefaultOrganizationName = "Sorcha Local";
    public const string DefaultOrganizationSubdomain = "sorcha-local";

    public DatabaseInitializer(
        IServiceProvider serviceProvider,
        ILogger<DatabaseInitializer> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Initializes the database by applying migrations and seeding default data.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting database initialization...");

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();

            // Check if we're using a real database (not in-memory)
            var isInMemory = dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";

            if (!isInMemory)
            {
                // Apply pending migrations
                await ApplyMigrationsAsync(dbContext, cancellationToken);
            }
            else
            {
                // Ensure in-memory database is created
                await dbContext.Database.EnsureCreatedAsync(cancellationToken);
                _logger.LogInformation("In-memory database created");
            }

            // Seed default data
            await SeedDefaultDataAsync(dbContext, cancellationToken);

            _logger.LogInformation("Database initialization completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database initialization failed");
            throw;
        }
    }

    private async Task ApplyMigrationsAsync(TenantDbContext dbContext, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking for pending database migrations...");

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        var pendingList = pendingMigrations.ToList();

        if (pendingList.Count > 0)
        {
            _logger.LogInformation("Applying {Count} pending migration(s): {Migrations}",
                pendingList.Count, string.Join(", ", pendingList));

            await dbContext.Database.MigrateAsync(cancellationToken);

            _logger.LogInformation("Database migrations applied successfully");
        }
        else
        {
            _logger.LogInformation("Database is up to date, no migrations to apply");
        }
    }

    private async Task SeedDefaultDataAsync(TenantDbContext dbContext, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Checking for seed data...");

        // Check if default organization exists
        var existingOrg = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == DefaultOrganizationId, cancellationToken);

        if (existingOrg == null)
        {
            _logger.LogInformation("Creating default organization: {Name}", DefaultOrganizationName);

            var organization = new Organization
            {
                Id = DefaultOrganizationId,
                Name = _configuration["Seed:OrganizationName"] ?? DefaultOrganizationName,
                Subdomain = _configuration["Seed:OrganizationSubdomain"] ?? DefaultOrganizationSubdomain,
                Status = OrganizationStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow,
                Branding = new BrandingConfiguration
                {
                    PrimaryColor = "#6366f1",
                    SecondaryColor = "#8b5cf6",
                    CompanyTagline = "Distributed Ledger Platform"
                }
            };

            dbContext.Organizations.Add(organization);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Default organization created with ID: {Id}", organization.Id);
        }
        else
        {
            _logger.LogInformation("Default organization already exists: {Name}", existingOrg.Name);
        }

        // Check if default admin user exists
        var existingAdmin = await dbContext.UserIdentities
            .FirstOrDefaultAsync(u => u.Id == DefaultAdminUserId, cancellationToken);

        if (existingAdmin == null)
        {
            var adminEmail = _configuration["Seed:AdminEmail"] ?? DefaultAdminEmail;
            var adminPassword = _configuration["Seed:AdminPassword"] ?? DefaultAdminPassword;

            _logger.LogInformation("Creating default administrator: {Email}", adminEmail);

            // Hash the password using BCrypt
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);

            var adminUser = new UserIdentity
            {
                Id = DefaultAdminUserId,
                OrganizationId = DefaultOrganizationId,
                Email = adminEmail,
                DisplayName = "System Administrator",
                PasswordHash = passwordHash,
                ExternalIdpUserId = null, // Local authentication
                Roles = new[] { UserRole.Administrator },
                Status = IdentityStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow
            };

            dbContext.UserIdentities.Add(adminUser);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Default administrator created with ID: {Id}", adminUser.Id);
            _logger.LogWarning("Default admin credentials - Email: {Email}, Password: {Password} - CHANGE IN PRODUCTION!",
                adminEmail, adminPassword);
        }
        else
        {
            _logger.LogInformation("Default administrator already exists: {Email}", existingAdmin.Email);
        }
    }
}

/// <summary>
/// Hosted service that runs database initialization on application startup.
/// </summary>
public class DatabaseInitializerHostedService : IHostedService
{
    private readonly DatabaseInitializer _initializer;
    private readonly ILogger<DatabaseInitializerHostedService> _logger;

    public DatabaseInitializerHostedService(
        DatabaseInitializer initializer,
        ILogger<DatabaseInitializerHostedService> logger)
    {
        _initializer = initializer;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Database initializer hosted service starting...");

        try
        {
            await _initializer.InitializeAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database. Service may not function correctly.");
            // Don't rethrow - allow service to start even if DB init fails
            // Health checks will report unhealthy status
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
