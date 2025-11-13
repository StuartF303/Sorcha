# WALLET-028: Data Migration Scripts

**Status:** Not Started
**Priority:** Critical
**Estimated Hours:** 24
**Dependencies:** WALLET-009, WALLET-027
**Related Spec:** [siccar-wallet-service.md](../specs/siccar-wallet-service.md#migration-path-from-current-implementation)

## Objective

Create comprehensive data migration scripts to migrate wallets from the existing WalletService database schema to the new Siccar.WalletService schema. Ensure zero data loss, maintain backward compatibility, and provide rollback capability.

## Current Schema Analysis

### Existing Tables (from /src/Services/Wallet/WalletSQLRepository)

**Wallets Table:**
```sql
CREATE TABLE Wallets (
    Address VARCHAR(128) PRIMARY KEY,
    PrivateKey VARCHAR(1000) NOT NULL,  -- Encrypted with DPAPI
    Name VARCHAR(200) NOT NULL,
    Owner VARCHAR(256) NOT NULL,
    Tenant VARCHAR(128) NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE INDEX IX_Wallet_Owner ON Wallets(Owner);
```

**WalletAddress Table:**
```sql
CREATE TABLE Addresses (
    Address VARCHAR(128) PRIMARY KEY,
    WalletId VARCHAR(128) NOT NULL,
    DerivationPath VARCHAR(100) NOT NULL DEFAULT 'm/',
    FOREIGN KEY (WalletId) REFERENCES Wallets(Address) ON DELETE CASCADE
);
```

**WalletAccess Table:**
```sql
CREATE TABLE Delegates (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    WalletId VARCHAR(128) NOT NULL,
    Tenant VARCHAR(128) NOT NULL,
    Subject VARCHAR(256) NOT NULL,
    AccessType INT NOT NULL,  -- 0=none, 1=owner, 2=delegaterw, 3=delegatero
    Reason VARCHAR(500) NOT NULL,
    AssignedTime DATETIME NOT NULL,
    FOREIGN KEY (WalletId) REFERENCES Wallets(Address) ON DELETE CASCADE
);

CREATE INDEX IX_WalletAccess_Subject ON Delegates(Subject);
```

**WalletTransaction Table:**
```sql
CREATE TABLE Transactions (
    Id VARCHAR(256) PRIMARY KEY,  -- Format: {TransactionId}:{WalletId}
    TransactionId VARCHAR(128) NOT NULL,
    WalletId VARCHAR(128) NOT NULL,
    ReceivedAddress VARCHAR(128) NOT NULL,
    PreviousId VARCHAR(128),
    Sender VARCHAR(128) NOT NULL,
    isSendingWallet BOOLEAN NOT NULL,
    isConfirmed BOOLEAN NOT NULL,
    isSpent BOOLEAN NOT NULL,
    Timestamp DATETIME NOT NULL,
    FOREIGN KEY (WalletId) REFERENCES Wallets(Address) ON DELETE CASCADE
);

CREATE INDEX IX_WalletTransaction_WalletId ON Transactions(WalletId);
CREATE INDEX IX_WalletTransaction_TransactionId ON Transactions(TransactionId);
```

**TransactionMetaData Table:**
```sql
CREATE TABLE TransactionMetaData (
    Id VARCHAR(256) PRIMARY KEY,  -- Same as Transactions.Id
    TransactionType INT NOT NULL,
    RegisterId VARCHAR(128),
    BlueprintId VARCHAR(128),
    -- ... other metadata fields
    _trackingDataJson TEXT
);
```

### New Schema Enhancements

**Added to Wallets:**
- `EncryptionKeyId` VARCHAR(256) - Reference to encryption key in Key Vault
- `Algorithm` VARCHAR(50) - ED25519, SECP256K1, RSA
- `Description` VARCHAR(1000) - Optional description
- `Tags` JSON - Key-value metadata
- `Status` INT - 0=Active, 1=Archived, 2=Deleted, 3=Locked
- `DeletedAt` DATETIME - Soft delete timestamp
- `Version` INT - For optimistic concurrency
- `RowVersion` TIMESTAMP - Concurrency token

**Renamed Fields:**
- `PrivateKey` → `EncryptedPrivateKey` (clarify it's encrypted)

**Added to WalletAccess:**
- `AssignedBy` VARCHAR(256) - Who granted access
- `ExpiresAt` DATETIME - Optional expiration
- `IsRevoked` BOOLEAN - Revocation flag
- `RevokedAt` DATETIME - When revoked
- `RevokedBy` VARCHAR(256) - Who revoked
- `RevocationReason` VARCHAR(500) - Why revoked

**New Table: AuditLogs**
```sql
CREATE TABLE AuditLogs (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    WalletAddress VARCHAR(128) NOT NULL,
    Action VARCHAR(100) NOT NULL,
    Subject VARCHAR(256) NOT NULL,
    IpAddress VARCHAR(45),
    UserAgent VARCHAR(500),
    Timestamp DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Metadata JSON,
    Severity INT NOT NULL,
    INDEX IX_AuditLog_WalletAddress (WalletAddress),
    INDEX IX_AuditLog_Timestamp (Timestamp),
    INDEX IX_AuditLog_Subject (Subject),
    INDEX IX_AuditLog_Severity (Severity)
);
```

## Migration Strategy

### Phase 1: Schema Migration (Non-Breaking)

**Objective:** Add new columns to existing tables without breaking current service

**Script:** `001_add_new_columns.sql`

```sql
-- Add new columns to Wallets table with defaults
ALTER TABLE Wallets
    ADD COLUMN EncryptionKeyId VARCHAR(256) DEFAULT 'legacy-dpapi-key' NOT NULL,
    ADD COLUMN Algorithm VARCHAR(50) DEFAULT 'ED25519' NOT NULL,
    ADD COLUMN Description VARCHAR(1000) NULL,
    ADD COLUMN Tags JSON NULL,
    ADD COLUMN Status INT DEFAULT 0 NOT NULL,
    ADD COLUMN DeletedAt DATETIME NULL,
    ADD COLUMN Version INT DEFAULT 1 NOT NULL,
    ADD COLUMN RowVersion TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP;

-- Rename PrivateKey to EncryptedPrivateKey (MySQL doesn't support RENAME COLUMN pre-8.0)
ALTER TABLE Wallets CHANGE COLUMN PrivateKey EncryptedPrivateKey VARCHAR(1000) NOT NULL;

-- Add new columns to Delegates table
ALTER TABLE Delegates
    ADD COLUMN AssignedBy VARCHAR(256) DEFAULT 'system' NOT NULL,
    ADD COLUMN ExpiresAt DATETIME NULL,
    ADD COLUMN IsRevoked BOOLEAN DEFAULT FALSE NOT NULL,
    ADD COLUMN RevokedAt DATETIME NULL,
    ADD COLUMN RevokedBy VARCHAR(256) NULL,
    ADD COLUMN RevocationReason VARCHAR(500) NULL;

-- Rename Delegates to WalletAccess for clarity
RENAME TABLE Delegates TO WalletAccess;

-- Add new columns to Addresses table
ALTER TABLE Addresses
    ADD COLUMN `Index` INT DEFAULT 0 NOT NULL,
    ADD COLUMN Label VARCHAR(200) NULL,
    ADD COLUMN Type INT DEFAULT 0 NOT NULL,  -- 0=Receive
    ADD COLUMN IsUsed BOOLEAN DEFAULT FALSE NOT NULL,
    ADD COLUMN TransactionCount INT DEFAULT 0 NOT NULL,
    ADD COLUMN FirstUsedAt DATETIME NULL,
    ADD COLUMN LastUsedAt DATETIME NULL;

-- Rename Addresses to WalletAddress
RENAME TABLE Addresses TO WalletAddress;

-- Add new columns to Transactions table
ALTER TABLE Transactions
    ADD COLUMN ConfirmedAt DATETIME NULL,
    ADD COLUMN SpentAt DATETIME NULL;

-- Rename Transactions to WalletTransaction
RENAME TABLE Transactions TO WalletTransaction;

-- Create AuditLogs table
CREATE TABLE AuditLogs (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    WalletAddress VARCHAR(128) NOT NULL,
    Action VARCHAR(100) NOT NULL,
    Subject VARCHAR(256) NOT NULL,
    IpAddress VARCHAR(45),
    UserAgent VARCHAR(500),
    Timestamp DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Metadata JSON,
    Severity INT NOT NULL,
    INDEX IX_AuditLog_WalletAddress (WalletAddress),
    INDEX IX_AuditLog_Timestamp (Timestamp),
    INDEX IX_AuditLog_Subject (Subject),
    INDEX IX_AuditLog_Severity (Severity)
);

-- Create audit log entries for existing wallets
INSERT INTO AuditLogs (WalletAddress, Action, Subject, Severity, Metadata)
SELECT
    Address,
    'Migrated',
    Owner,
    0,  -- Info
    JSON_OBJECT('migration', 'phase1', 'timestamp', NOW())
FROM Wallets;
```

**Validation Script:** `001_validate_schema.sql`

```sql
-- Verify all wallets have new columns
SELECT COUNT(*) AS MissingEncryptionKeyId
FROM Wallets
WHERE EncryptionKeyId IS NULL OR EncryptionKeyId = '';

SELECT COUNT(*) AS MissingAlgorithm
FROM Wallets
WHERE Algorithm IS NULL OR Algorithm = '';

-- Verify table renames
SELECT COUNT(*) FROM WalletAccess;
SELECT COUNT(*) FROM WalletAddress;
SELECT COUNT(*) FROM WalletTransaction;

-- Verify AuditLogs created
SELECT COUNT(*) FROM AuditLogs WHERE Action = 'Migrated';
```

### Phase 2: Data Cleanup and Enrichment

**Script:** `002_enrich_data.sql`

```sql
-- Update AssignedBy for existing delegates (mark as legacy)
UPDATE WalletAccess
SET AssignedBy = Owner
FROM Wallets
WHERE WalletAccess.WalletId = Wallets.Address
  AND WalletAccess.AssignedBy = 'system';

-- Set ConfirmedAt for confirmed transactions
UPDATE WalletTransaction
SET ConfirmedAt = Timestamp
WHERE isConfirmed = TRUE AND ConfirmedAt IS NULL;

-- Set SpentAt for spent transactions
UPDATE WalletTransaction
SET SpentAt = Timestamp
WHERE isSpent = TRUE AND SpentAt IS NULL;

-- Update address usage tracking
UPDATE WalletAddress wa
SET
    IsUsed = EXISTS (
        SELECT 1 FROM WalletTransaction wt
        WHERE wt.ReceivedAddress = wa.Address
    ),
    TransactionCount = (
        SELECT COUNT(*) FROM WalletTransaction wt
        WHERE wt.ReceivedAddress = wa.Address
    ),
    FirstUsedAt = (
        SELECT MIN(Timestamp) FROM WalletTransaction wt
        WHERE wt.ReceivedAddress = wa.Address
    ),
    LastUsedAt = (
        SELECT MAX(Timestamp) FROM WalletTransaction wt
        WHERE wt.ReceivedAddress = wa.Address
    );
```

### Phase 3: Encryption Key Migration

**Objective:** Re-encrypt private keys with new encryption provider

**C# Migration Tool:**

```csharp
public class EncryptionMigrationTool
{
    private readonly WalletDbContext _context;
    private readonly IDataProtector _oldProtector;  // Legacy DPAPI
    private readonly IEncryptionProvider _newProvider;  // Azure KV or new provider
    private readonly ILogger _logger;

    public async Task MigrateEncryptionAsync(CancellationToken ct = default)
    {
        var wallets = await _context.Wallets.ToListAsync(ct);
        int migrated = 0;
        int failed = 0;

        foreach (var wallet in wallets)
        {
            try
            {
                // Decrypt with old DPAPI protector
                var privateKeyBytes = Convert.FromBase64String(wallet.EncryptedPrivateKey);
                var decryptedOld = _oldProtector.Unprotect(privateKeyBytes);

                // Encrypt with new provider
                var encryptedNew = await _newProvider.EncryptAsync(decryptedOld, "wallet-encryption-key");
                wallet.EncryptedPrivateKey = Convert.ToBase64String(encryptedNew);
                wallet.EncryptionKeyId = "wallet-encryption-key";
                wallet.UpdatedAt = DateTime.UtcNow;

                migrated++;

                if (migrated % 100 == 0)
                {
                    await _context.SaveChangesAsync(ct);
                    _logger.LogInformation("Migrated {Count} wallets", migrated);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate wallet {Address}", wallet.Address);
                failed++;
            }
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Migration complete: {Migrated} migrated, {Failed} failed", migrated, failed);
    }
}
```

### Phase 4: Validation and Verification

**Validation Tool:**

```csharp
public class MigrationValidator
{
    public async Task<ValidationReport> ValidateMigrationAsync()
    {
        var report = new ValidationReport();

        // Verify record counts match
        report.WalletCount = await ValidateWalletCount();
        report.AddressCount = await ValidateAddressCount();
        report.DelegateCount = await ValidateDelegateCount();
        report.TransactionCount = await ValidateTransactionCount();

        // Verify data integrity
        report.OrphanedRecords = await FindOrphanedRecords();
        report.MissingRequiredFields = await FindMissingRequiredFields();

        // Verify encryption
        report.EncryptionStatus = await ValidateEncryption();

        // Verify access control
        report.InvalidDelegations = await FindInvalidDelegations();

        return report;
    }

    private async Task<int> ValidateWalletCount()
    {
        var oldCount = await _oldContext.Wallets.CountAsync();
        var newCount = await _newContext.Wallets.CountAsync();

        if (oldCount != newCount)
        {
            _logger.LogWarning("Wallet count mismatch: old={Old}, new={New}", oldCount, newCount);
        }

        return newCount;
    }

    // Additional validation methods...
}
```

### Phase 5: Rollback Procedures

**Rollback Script:** `rollback_migration.sql`

```sql
-- Rollback Phase 3 (Encryption) - Requires backup restore

-- Rollback Phase 2 (Data Enrichment)
UPDATE WalletAccess SET AssignedBy = 'system' WHERE AssignedBy != 'system';
UPDATE WalletTransaction SET ConfirmedAt = NULL, SpentAt = NULL;
UPDATE WalletAddress SET IsUsed = FALSE, TransactionCount = 0, FirstUsedAt = NULL, LastUsedAt = NULL;

-- Rollback Phase 1 (Schema Changes)
DROP TABLE IF EXISTS AuditLogs;

ALTER TABLE WalletTransaction
    DROP COLUMN ConfirmedAt,
    DROP COLUMN SpentAt;

ALTER TABLE WalletAddress
    DROP COLUMN `Index`,
    DROP COLUMN Label,
    DROP COLUMN Type,
    DROP COLUMN IsUsed,
    DROP COLUMN TransactionCount,
    DROP COLUMN FirstUsedAt,
    DROP COLUMN LastUsedAt;

RENAME TABLE WalletAddress TO Addresses;

ALTER TABLE WalletAccess
    DROP COLUMN AssignedBy,
    DROP COLUMN ExpiresAt,
    DROP COLUMN IsRevoked,
    DROP COLUMN RevokedAt,
    DROP COLUMN RevokedBy,
    DROP COLUMN RevocationReason;

RENAME TABLE WalletAccess TO Delegates;

ALTER TABLE Wallets
    DROP COLUMN EncryptionKeyId,
    DROP COLUMN Algorithm,
    DROP COLUMN Description,
    DROP COLUMN Tags,
    DROP COLUMN Status,
    DROP COLUMN DeletedAt,
    DROP COLUMN Version,
    DROP COLUMN RowVersion;

ALTER TABLE Wallets CHANGE COLUMN EncryptedPrivateKey PrivateKey VARCHAR(1000) NOT NULL;
```

## Migration Execution Plan

### Pre-Migration Checklist

- [ ] Full database backup created
- [ ] Backup verified and tested
- [ ] Migration scripts reviewed and approved
- [ ] Validation scripts prepared
- [ ] Rollback plan tested in staging
- [ ] Maintenance window scheduled
- [ ] Stakeholders notified

### Migration Steps

1. **Take Full Backup** (30 minutes)
   ```bash
   mysqldump -u root -p wallets > wallets_backup_$(date +%Y%m%d_%H%M%S).sql
   ```

2. **Run Phase 1 Schema Migration** (10 minutes)
   ```bash
   mysql -u root -p wallets < 001_add_new_columns.sql
   ```

3. **Validate Schema** (5 minutes)
   ```bash
   mysql -u root -p wallets < 001_validate_schema.sql
   ```

4. **Run Phase 2 Data Enrichment** (15 minutes)
   ```bash
   mysql -u root -p wallets < 002_enrich_data.sql
   ```

5. **Run Phase 3 Encryption Migration** (60-120 minutes depending on wallet count)
   ```bash
   dotnet run --project MigrationTool -- migrate-encryption
   ```

6. **Run Validation** (30 minutes)
   ```bash
   dotnet run --project MigrationTool -- validate
   ```

7. **Deploy New Service** (15 minutes)
   - Deploy new Siccar.WalletService.Api
   - Run in shadow mode alongside old service
   - Monitor for errors

8. **Gradual Traffic Migration** (1-2 days)
   - Route 10% traffic to new service
   - Monitor metrics, errors, performance
   - Increase to 50%, then 100%

9. **Decommission Old Service** (1 hour)
   - Stop old WalletService
   - Archive old code
   - Update documentation

### Post-Migration Checklist

- [ ] All wallets accessible
- [ ] All transactions visible
- [ ] All delegates functioning
- [ ] Performance meets or exceeds baseline
- [ ] No error rate increase
- [ ] Audit logs capturing events

## Acceptance Criteria

- [ ] All migration scripts created and tested
- [ ] Validation scripts verify 100% data integrity
- [ ] Encryption migration tool complete and tested
- [ ] Rollback procedure tested in staging
- [ ] Zero data loss during migration
- [ ] All existing API calls work with new service
- [ ] Performance metrics match or exceed old service
- [ ] Comprehensive migration documentation
- [ ] Migration executed successfully in production

## Testing

### Staging Environment Test

1. Clone production database to staging
2. Run full migration
3. Validate all data migrated correctly
4. Test old API calls against new service
5. Test new API features
6. Measure performance
7. Execute rollback
8. Verify rollback successful

### Production Migration Rehearsal

1. Create production-size synthetic dataset
2. Run full migration on synthetic data
3. Measure timing for each phase
4. Test validation under production load
5. Document any issues

## Dependencies

- Database backup solution
- Entity Framework Core migrations
- Azure Key Vault or AWS KMS (for encryption migration)
- Monitoring and alerting (Application Insights, Prometheus)
- Feature flags (for gradual rollout)

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Data loss during migration | Full backup before migration, validation after each phase |
| Encryption key loss | Backup encryption keys, test decryption before completing migration |
| Downtime exceeds window | Rehearse in staging, optimize scripts, plan rollback |
| Performance degradation | Load test new service, optimize queries, add indexes |
| Rollback needed | Tested rollback procedure, keep old service running in shadow mode |

## Next Steps

1. Test migration on staging environment
2. Conduct migration rehearsal with production-size data
3. Schedule production migration window
4. Execute production migration
5. Monitor and validate
