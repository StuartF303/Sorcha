# PostgreSQL Patterns Reference

## Contents
- DbContext Configuration
- Column Type Mappings
- Indexing Strategies
- Soft Delete Pattern
- Concurrency Control
- Anti-Patterns

---

## DbContext Configuration

### Schema Isolation

Sorcha uses dedicated schemas to isolate domain data:

```csharp
// WalletDbContext.cs:54
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.HasDefaultSchema("wallet");
    
    // Tables: wallets, wallet_addresses, wallet_access, wallet_transactions
}
```

### JSONB for Dictionary Metadata

**Required:** Enable dynamic JSON in Npgsql 8.0+:

```csharp
// WalletServiceExtensions.cs:84-88
services.AddNpgsqlDataSource(connectionString, dataSourceBuilder =>
{
    dataSourceBuilder.EnableDynamicJson();
});
```

Then configure the column:

```csharp
// WalletDbContext.cs:95
entity.Property(e => e.Metadata)
    .HasColumnType("jsonb");
```

### Owned JSON Types (TenantDbContext Pattern)

```csharp
// TenantDbContext.cs - Branding as owned JSON
entity.OwnsOne(e => e.Branding, branding =>
{
    branding.ToJson();  // Serializes entire object as JSON column
});
```

---

## Column Type Mappings

| C# Type | PostgreSQL Type | Use Case |
|---------|-----------------|----------|
| `decimal` | `numeric(28,18)` | Cryptocurrency amounts |
| `Guid` | `uuid` | Primary keys |
| `DateTime` | `timestamp with time zone` | Audit timestamps |
| `byte[]` | `bytea` | Row version, binary data |
| `Dictionary<string, string>` | `jsonb` | Flexible metadata |
| `string` (enum) | `text` | Enum storage |

### High-Precision Decimals

```csharp
// WalletDbContext.cs - For crypto amounts
entity.Property(e => e.Balance)
    .HasColumnType("numeric(28,18)")
    .HasPrecision(28, 18);
```

### UUID with Auto-Generation

```csharp
entity.Property(e => e.Id)
    .HasColumnType("uuid")
    .HasDefaultValueSql("gen_random_uuid()");
```

---

## Indexing Strategies

### Composite Index for Multi-Tenant Queries

```csharp
// WalletDbContext.cs:145-149
entity.HasIndex(e => new { e.Owner, e.Tenant })
    .HasDatabaseName("IX_wallets_owner_tenant");
```

### Descending Index for Time-Series

```csharp
// For "most recent first" queries
entity.HasIndex(e => new { e.ParentWalletAddress, e.CreatedAt })
    .IsDescending(false, true);  // Address ASC, CreatedAt DESC
```

### Unique Index with Filter

```csharp
// TenantDbContext.cs - Unique only for non-null values
entity.HasIndex(e => e.ExternalIdpUserId)
    .IsUnique()
    .HasFilter("external_idp_user_id IS NOT NULL");
```

---

## Soft Delete Pattern

### Global Query Filter

```csharp
// WalletDbContext.cs:152
entity.HasQueryFilter(e => e.DeletedAt == null);
```

### Bypassing Filter for Admin Queries

```csharp
// EfCoreWalletRepository.cs - When you need deleted records
var allRecords = await _context.Wallets
    .IgnoreQueryFilters()
    .ToListAsync();
```

### Idempotent Delete

```csharp
// EfCoreWalletRepository.cs:280-295
public async Task<bool> DeleteAsync(string address)
{
    var wallet = await _context.Wallets
        .IgnoreQueryFilters()  // Check if already deleted
        .FirstOrDefaultAsync(w => w.Address == address);
    
    if (wallet == null) return false;
    if (wallet.DeletedAt != null) return true;  // Already deleted
    
    wallet.DeletedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
    return true;
}
```

---

## Concurrency Control

### Row Version (Optimistic Locking)

```csharp
// WalletDbContext.cs:133
entity.Property(e => e.RowVersion)
    .HasColumnType("bytea")
    .IsRowVersion();
```

### Handling Concurrency Exceptions

```csharp
try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException ex)
{
    // Reload and retry, or notify user
    await ex.Entries.Single().ReloadAsync();
}
```

---

## Anti-Patterns

### WARNING: Missing EnableDynamicJson

**The Problem:**

```csharp
// BAD - Dictionary serialization fails silently in Npgsql 8.0+
services.AddDbContext<MyContext>(options =>
    options.UseNpgsql(connectionString));
```

**Why This Breaks:**
1. `Dictionary<string, string>` columns serialize as empty `{}`
2. No error thrown - data silently lost
3. Only discovered when reading back empty metadata

**The Fix:**

```csharp
// GOOD - Enable dynamic JSON before adding DbContext
services.AddNpgsqlDataSource(connectionString, dataSourceBuilder =>
{
    dataSourceBuilder.EnableDynamicJson();
});

services.AddDbContext<MyContext>(options =>
    options.UseNpgsql(connectionString));
```

### WARNING: Using timestamp without time zone

**The Problem:**

```csharp
// BAD - Timezone ambiguity
entity.Property(e => e.CreatedAt)
    .HasColumnType("timestamp");
```

**Why This Breaks:**
1. Different servers interpret timestamps differently
2. Daylight saving time causes off-by-one-hour bugs
3. Distributed systems show inconsistent times

**The Fix:**

```csharp
// GOOD - Always use timezone-aware timestamps
entity.Property(e => e.CreatedAt)
    .HasColumnType("timestamp with time zone");
```

### WARNING: N+1 Queries in Loops

**The Problem:**

```csharp
// BAD - One query per wallet
foreach (var wallet in wallets)
{
    wallet.Addresses = await _context.Addresses
        .Where(a => a.WalletId == wallet.Id)
        .ToListAsync();
}
```

**Why This Breaks:**
1. 100 wallets = 101 database roundtrips
2. Network latency dominates performance
3. Connection pool exhaustion under load

**The Fix:**

```csharp
// GOOD - Eager loading
var wallets = await _context.Wallets
    .Include(w => w.Addresses)
    .ToListAsync();
```

See the **entity-framework** skill for more query optimization patterns.

---

## Foreign Key Configuration

### Cascade Delete

```csharp
// WalletDbContext.cs
entity.HasOne(e => e.Wallet)
    .WithMany(w => w.Addresses)
    .HasForeignKey(e => e.ParentWalletAddress)
    .OnDelete(DeleteBehavior.Cascade);
```

### Restrict Delete (Prevent Orphans)

```csharp
entity.HasOne(e => e.Organization)
    .WithMany(o => o.Users)
    .OnDelete(DeleteBehavior.Restrict);
```