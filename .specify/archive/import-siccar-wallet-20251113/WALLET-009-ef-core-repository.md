# WALLET-009: EF Core Repository Implementation

**Status:** Not Started
**Priority:** Critical
**Estimated Hours:** 20
**Dependencies:** WALLET-002, WALLET-008
**Related Spec:** [siccar-wallet-service.md](../specs/siccar-wallet-service.md#2-storage-implementations)

## Objective

Implement Entity Framework Core repository for wallet persistence supporting MySQL, PostgreSQL, and Azure Cosmos DB SQL API. Ensure compatibility with existing database schema for migration.

## Requirements

### DbContext Implementation

**File:** `Repositories/Implementation/WalletDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;

namespace Siccar.WalletService.Repositories.Implementation
{
    public class WalletDbContext : DbContext
    {
        public DbSet<Wallet> Wallets { get; set; } = null!;
        public DbSet<WalletAddress> Addresses { get; set; } = null!;
        public DbSet<WalletAccess> Delegates { get; set; } = null!;
        public DbSet<WalletTransaction> Transactions { get; set; } = null!;
        public DbSet<TransactionMetaData> TransactionMetaData { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        public WalletDbContext(DbContextOptions<WalletDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureWallet(modelBuilder);
            ConfigureWalletAddress(modelBuilder);
            ConfigureWalletAccess(modelBuilder);
            ConfigureWalletTransaction(modelBuilder);
            ConfigureAuditLog(modelBuilder);
        }

        private void ConfigureWallet(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Wallet>(entity =>
            {
                // Primary Key
                entity.HasKey(e => e.Address);
                entity.Property(e => e.Address).HasMaxLength(128).IsRequired();

                // Required Fields
                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Owner).HasMaxLength(256).IsRequired();
                entity.Property(e => e.Tenant).HasMaxLength(128).IsRequired();
                entity.Property(e => e.EncryptedPrivateKey).IsRequired();
                entity.Property(e => e.EncryptionKeyId).HasMaxLength(256).IsRequired();
                entity.Property(e => e.Algorithm).HasMaxLength(50).IsRequired();

                // Optional Fields
                entity.Property(e => e.Description).HasMaxLength(1000);

                // JSON Column for Tags (supported in MySQL 8, PostgreSQL, Cosmos)
                entity.Property(e => e.Tags)
                    .HasColumnType("json")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null));

                // Indexes
                entity.HasIndex(e => e.Owner).HasDatabaseName("IX_Wallet_Owner");
                entity.HasIndex(e => e.Tenant).HasDatabaseName("IX_Wallet_Tenant");
                entity.HasIndex(e => new { e.Tenant, e.Owner }).HasDatabaseName("IX_Wallet_Tenant_Owner");
                entity.HasIndex(e => e.Status).HasDatabaseName("IX_Wallet_Status");

                // Concurrency Token
                entity.Property(e => e.RowVersion).IsRowVersion();

                // Timestamps
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");

                // Relationships
                entity.HasMany(e => e.Addresses)
                    .WithOne(e => e.Wallet)
                    .HasForeignKey(e => e.WalletId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Delegates)
                    .WithOne(e => e.Wallet)
                    .HasForeignKey(e => e.WalletId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Transactions)
                    .WithOne(e => e.Wallet)
                    .HasForeignKey(e => e.WalletId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureWalletAddress(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WalletAddress>(entity =>
            {
                // Primary Key
                entity.HasKey(e => e.Address);
                entity.Property(e => e.Address).HasMaxLength(128).IsRequired();

                // Foreign Key
                entity.Property(e => e.WalletId).HasMaxLength(128).IsRequired();

                // Required Fields
                entity.Property(e => e.DerivationPath).HasMaxLength(100).IsRequired();

                // Optional Fields
                entity.Property(e => e.Label).HasMaxLength(200);

                // Indexes
                entity.HasIndex(e => e.WalletId).HasDatabaseName("IX_WalletAddress_WalletId");
                entity.HasIndex(e => new { e.WalletId, e.Index }).HasDatabaseName("IX_WalletAddress_WalletId_Index");
            });
        }

        private void ConfigureWalletAccess(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WalletAccess>(entity =>
            {
                // Primary Key
                entity.HasKey(e => e.Id);

                // Foreign Key
                entity.Property(e => e.WalletId).HasMaxLength(128).IsRequired();

                // Required Fields
                entity.Property(e => e.Subject).HasMaxLength(256).IsRequired();
                entity.Property(e => e.Tenant).HasMaxLength(128).IsRequired();
                entity.Property(e => e.Reason).HasMaxLength(500).IsRequired();
                entity.Property(e => e.AssignedBy).HasMaxLength(256).IsRequired();

                // Optional Fields
                entity.Property(e => e.RevokedBy).HasMaxLength(256);
                entity.Property(e => e.RevocationReason).HasMaxLength(500);

                // Indexes
                entity.HasIndex(e => e.Subject).HasDatabaseName("IX_WalletAccess_Subject");
                entity.HasIndex(e => new { e.WalletId, e.Subject }).HasDatabaseName("IX_WalletAccess_WalletId_Subject").IsUnique();
                entity.HasIndex(e => e.Tenant).HasDatabaseName("IX_WalletAccess_Tenant");

                // Concurrency Token for optimistic locking
                entity.Property(e => e.AssignedAt).IsETagConcurrency();
            });
        }

        private void ConfigureWalletTransaction(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<WalletTransaction>(entity =>
            {
                // Composite Primary Key
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(256).IsRequired();

                // Foreign Keys
                entity.Property(e => e.TransactionId).HasMaxLength(128).IsRequired();
                entity.Property(e => e.WalletId).HasMaxLength(128).IsRequired();

                // Fields
                entity.Property(e => e.Sender).HasMaxLength(128).IsRequired();
                entity.Property(e => e.ReceivedAddress).HasMaxLength(128).IsRequired();
                entity.Property(e => e.PreviousId).HasMaxLength(128);

                // Indexes
                entity.HasIndex(e => e.WalletId).HasDatabaseName("IX_WalletTransaction_WalletId");
                entity.HasIndex(e => e.TransactionId).HasDatabaseName("IX_WalletTransaction_TransactionId");
                entity.HasIndex(e => new { e.WalletId, e.IsConfirmed, e.IsSpent }).HasDatabaseName("IX_WalletTransaction_WalletId_Status");

                // Relationship with TransactionMetaData (owned entity)
                entity.HasOne(e => e.MetaData)
                    .WithOne()
                    .HasForeignKey<WalletTransaction>(e => e.Id);
            });
        }

        private void ConfigureAuditLog(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.WalletAddress).HasMaxLength(128).IsRequired();
                entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Subject).HasMaxLength(256).IsRequired();
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.Property(e => e.UserAgent).HasMaxLength(500);

                // JSON column for metadata
                entity.Property(e => e.Metadata)
                    .HasColumnType("json")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                        v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null));

                // Indexes
                entity.HasIndex(e => e.WalletAddress).HasDatabaseName("IX_AuditLog_WalletAddress");
                entity.HasIndex(e => e.Timestamp).HasDatabaseName("IX_AuditLog_Timestamp");
                entity.HasIndex(e => e.Subject).HasDatabaseName("IX_AuditLog_Subject");
                entity.HasIndex(e => e.Severity).HasDatabaseName("IX_AuditLog_Severity");
            });
        }
    }
}
```

### Repository Implementation

**File:** `Repositories/Implementation/EFCoreWalletRepository.cs`

```csharp
public class EFCoreWalletRepository : IWalletRepository
{
    private readonly WalletDbContext _context;
    private readonly ILogger<EFCoreWalletRepository> _logger;

    public EFCoreWalletRepository(WalletDbContext context, ILogger<EFCoreWalletRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Wallet?> GetByAddressAsync(string address, string? userSubject = null, CancellationToken ct = default)
    {
        var query = _context.Wallets
            .Include(w => w.Addresses)
            .Include(w => w.Delegates)
            .Include(w => w.Transactions)
                .ThenInclude(t => t.MetaData)
            .Where(w => w.Address == address && w.Status != WalletStatus.Deleted);

        // Apply user subject filter if provided (for multi-tenancy)
        if (!string.IsNullOrWhiteSpace(userSubject))
        {
            query = query.Where(w =>
                w.Owner == userSubject ||
                w.Delegates.Any(d => d.Subject == userSubject && !d.IsRevoked));
        }

        return await query.FirstOrDefaultAsync(ct);
    }

    public async Task<IEnumerable<Wallet>> GetByOwnerAsync(string ownerSubject, CancellationToken ct = default)
    {
        return await _context.Wallets
            .Include(w => w.Addresses)
            .Include(w => w.Delegates)
            .Where(w => w.Owner == ownerSubject && w.Status != WalletStatus.Deleted)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Wallet>> GetByTenantAsync(string tenantId, CancellationToken ct = default)
    {
        return await _context.Wallets
            .Include(w => w.Addresses)
            .Include(w => w.Delegates)
            .Where(w => w.Tenant == tenantId && w.Status != WalletStatus.Deleted)
            .ToListAsync(ct);
    }

    public async Task<Wallet> CreateAsync(Wallet wallet, CancellationToken ct = default)
    {
        wallet.CreatedAt = DateTime.UtcNow;
        wallet.UpdatedAt = DateTime.UtcNow;
        wallet.Version = 1;

        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Created wallet {Address} for owner {Owner} in tenant {Tenant}",
            wallet.Address, wallet.Owner, wallet.Tenant);

        return wallet;
    }

    public async Task<Wallet> UpdateAsync(Wallet wallet, CancellationToken ct = default)
    {
        wallet.UpdatedAt = DateTime.UtcNow;
        wallet.Version++;

        _context.Wallets.Update(wallet);

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating wallet {Address}", wallet.Address);
            throw new WalletException(
                WalletErrorCodes.ConcurrencyConflict,
                "Wallet was modified by another user. Please reload and try again.",
                HttpStatusCode.Conflict);
        }

        _logger.LogInformation("Updated wallet {Address}", wallet.Address);
        return wallet;
    }

    public async Task DeleteAsync(string address, CancellationToken ct = default)
    {
        var wallet = await GetByAddressAsync(address, null, ct);
        if (wallet == null)
        {
            throw new WalletException(
                WalletErrorCodes.WalletNotFound,
                $"Wallet {address} not found",
                HttpStatusCode.NotFound);
        }

        // Soft delete
        wallet.Status = WalletStatus.Deleted;
        wallet.DeletedAt = DateTime.UtcNow;
        wallet.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Soft deleted wallet {Address}", address);
    }

    public async Task<bool> ExistsAsync(string address, CancellationToken ct = default)
    {
        return await _context.Wallets
            .AnyAsync(w => w.Address == address && w.Status != WalletStatus.Deleted, ct);
    }
}
```

### Database Migrations

**Initial Migration:** Create using EF Core tools

```bash
dotnet ef migrations add InitialCreate \
    --project src/Libraries/Siccar.WalletService/Siccar.WalletService \
    --context WalletDbContext \
    --output-dir Repositories/Migrations
```

### Connection String Configuration

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "WalletDB": {
      "ConnectionString": "server=localhost;database=wallets;user=root;password=password",
      "ProviderType": "mysql"
    }
  }
}
```

**Dependency Injection Setup:**
```csharp
services.AddDbContext<WalletDbContext>((serviceProvider, options) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration["ConnectionStrings:WalletDB:ConnectionString"];
    var providerType = configuration["ConnectionStrings:WalletDB:ProviderType"]?.ToLower() ?? "mysql";

    switch (providerType)
    {
        case "mysql":
            var serverVersion = ServerVersion.AutoDetect(connectionString);
            options.UseMySql(connectionString, serverVersion);
            break;

        case "postgresql":
            options.UseNpgsql(connectionString);
            break;

        case "cosmossql":
            var parts = connectionString.Split(';')
                .Select(s => s.Split('=', 2))
                .ToDictionary(kv => kv[0], kv => kv[1]);
            options.UseCosmos(
                parts["AccountEndpoint"],
                parts["AccountKey"],
                "wallets");
            break;

        default:
            throw new InvalidOperationException($"Unsupported database provider: {providerType}");
    }
});

services.AddScoped<IWalletRepository, EFCoreWalletRepository>();
```

## Acceptance Criteria

- [ ] WalletDbContext implemented with all entity configurations
- [ ] EFCoreWalletRepository implements IWalletRepository
- [ ] Support for MySQL, PostgreSQL, and Cosmos SQL
- [ ] Database migrations created and tested
- [ ] Indexes for performance (Owner, Tenant, Status)
- [ ] Optimistic concurrency control (RowVersion)
- [ ] Cascade deletes configured correctly
- [ ] Soft delete support (DeletedAt, Status)
- [ ] Proper logging for all operations
- [ ] Unit tests with InMemory database
- [ ] Integration tests with real MySQL (Testcontainers)
- [ ] Integration tests with real PostgreSQL (Testcontainers)

## Testing

### Unit Tests (InMemory Database)

```csharp
public class EFCoreWalletRepositoryTests : IDisposable
{
    private readonly WalletDbContext _context;
    private readonly EFCoreWalletRepository _repository;

    public EFCoreWalletRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<WalletDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new WalletDbContext(options);
        _repository = new EFCoreWalletRepository(_context, Mock.Of<ILogger<EFCoreWalletRepository>>());
    }

    [Fact]
    public async Task CreateAsync_WithValidWallet_SavesSuccessfully()
    {
        // Arrange
        var wallet = new Wallet
        {
            Address = "test-address",
            Name = "Test Wallet",
            Owner = "user-123",
            Tenant = "tenant-1",
            // ... other fields
        };

        // Act
        var result = await _repository.CreateAsync(wallet);

        // Assert
        result.Should().NotBeNull();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        var saved = await _context.Wallets.FindAsync(wallet.Address);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithConcurrentModification_ThrowsConcurrencyException()
    {
        // Arrange
        var wallet = await CreateTestWallet();

        // Simulate concurrent modification
        var wallet1 = await _repository.GetByAddressAsync(wallet.Address);
        var wallet2 = await _repository.GetByAddressAsync(wallet.Address);

        wallet1!.Name = "Updated Name 1";
        await _repository.UpdateAsync(wallet1);

        wallet2!.Name = "Updated Name 2";

        // Act & Assert
        await Assert.ThrowsAsync<WalletException>(() => _repository.UpdateAsync(wallet2));
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
```

### Integration Tests (Real Database)

```csharp
public class EFCoreWalletRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly MySqlContainer _mySqlContainer;
    private WalletDbContext _context = null!;
    private EFCoreWalletRepository _repository = null!;

    public EFCoreWalletRepositoryIntegrationTests()
    {
        _mySqlContainer = new MySqlBuilder()
            .WithImage("mysql:8.0")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _mySqlContainer.StartAsync();

        var options = new DbContextOptionsBuilder<WalletDbContext>()
            .UseMySql(_mySqlContainer.GetConnectionString(), ServerVersion.AutoDetect(_mySqlContainer.GetConnectionString()))
            .Options;

        _context = new WalletDbContext(options);
        await _context.Database.MigrateAsync();

        _repository = new EFCoreWalletRepository(_context, Mock.Of<ILogger<EFCoreWalletRepository>>());
    }

    [Fact]
    public async Task GetByTenantAsync_WithMultipleWallets_ReturnsOnlyTenantWallets()
    {
        // Arrange
        await CreateWallet("wallet-1", "tenant-1");
        await CreateWallet("wallet-2", "tenant-1");
        await CreateWallet("wallet-3", "tenant-2");

        // Act
        var result = await _repository.GetByTenantAsync("tenant-1");

        // Assert
        result.Should().HaveCount(2);
        result.All(w => w.Tenant == "tenant-1").Should().BeTrue();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _mySqlContainer.DisposeAsync();
    }
}
```

## Dependencies

- Entity Framework Core 8.0
- Pomelo.EntityFrameworkCore.MySql 8.0 (MySQL)
- Npgsql.EntityFrameworkCore.PostgreSQL 8.0 (PostgreSQL)
- Microsoft.EntityFrameworkCore.Cosmos 8.0 (Cosmos DB)
- Testcontainers 3.6.0 (for integration tests)

## Notes

- Use Testcontainers for integration tests with real databases
- JSON column support requires MySQL 8+, PostgreSQL 9.4+
- Cosmos DB has different indexing and query capabilities
- Ensure migrations are compatible across all providers
- Consider using separate migration paths for different providers if needed

## Next Steps

1. Create initial database migration
2. Test migration on MySQL, PostgreSQL
3. Implement audit logging interceptor
4. Add query performance monitoring
