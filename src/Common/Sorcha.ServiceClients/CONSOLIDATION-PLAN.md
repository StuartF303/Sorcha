# Service Client Consolidation Plan

**Created:** 2025-12-22
**Status:** In Progress
**Priority:** P1 (Architectural Improvement)

## Problem Statement

gRPC/HTTP service clients are duplicated across multiple projects:

| Client | Locations | Duplication Level |
|--------|-----------|-------------------|
| WalletServiceClient | Validator.Service, Blueprint.Service, CLI | HIGH |
| RegisterServiceClient | Validator.Service, Blueprint.Service, CLI | HIGH |
| PeerServiceClient | Validator.Service, CLI | MEDIUM |
| BlueprintServiceClient | Validator.Service | LOW |

**Impact:**
- Code duplication (DRY violation)
- Inconsistent error handling
- Difficult to maintain and update
- Different services have different method signatures for the same operations

## Solution Architecture

### Create `Sorcha.ServiceClients` Common Library

```
src/Common/Sorcha.ServiceClients/
├── Wallet/
│   ├── IWalletServiceClient.cs         # Comprehensive interface
│   ├── WalletServiceClient.cs          # Implementation
│   └── Models/
│       └── WalletInfo.cs               # Shared DTOs
├── Register/
│   ├── IRegisterServiceClient.cs
│   ├── RegisterServiceClient.cs
│   └── Models/
│       └── TransactionPage.cs
├── Blueprint/
│   ├── IBlueprintServiceClient.cs
│   └── BlueprintServiceClient.cs
├── Peer/
│   ├── IPeerServiceClient.cs
│   ├── PeerServiceClient.cs
│   └── Models/
│       └── ValidatorInfo.cs
├── Tenant/
│   ├── ITenantServiceClient.cs
│   └── TenantServiceClient.cs
├── Configuration/
│   └── ServiceClientsOptions.cs        # Configuration binding
└── Extensions/
    └── ServiceCollectionExtensions.cs  # DI registration
```

## Migration Strategy

### Phase 1: Create Consolidated Library ✅ IN PROGRESS

1. ✅ Create `Sorcha.ServiceClients` project
2. ✅ Add dependencies (Grpc.Net.Client, etc.)
3. ✅ Create README and plan documents
4. ⏳ Implement consolidated `IWalletServiceClient` with ALL methods
5. ⏳ Implement consolidated `IRegisterServiceClient` with ALL methods
6. ⏳ Implement consolidated `IBlueprintServiceClient`
7. ⏳ Implement consolidated `IPeerServiceClient`
8. ⏳ Create DI extension methods (`AddServiceClients()`)

### Phase 2: Update Validator Service (Current Project)

Files to update:
```
src/Services/Sorcha.Validator.Service/
├── Sorcha.Validator.Service.csproj
│   └── ADD: <ProjectReference Include="Sorcha.ServiceClients" />
│   └── REMOVE: Clients/ folder references
├── Program.cs
│   └── REPLACE: Individual client registrations
│   └── WITH: builder.Services.AddServiceClients(builder.Configuration)
├── Services/
│   ├── ConsensusEngine.cs
│   │   └── UPDATE: using Sorcha.ServiceClients.Wallet;
│   ├── DocketBuilder.cs
│   │   └── UPDATE: using Sorcha.ServiceClients.Register;
│   └── MemPoolManager.cs
│       └── UPDATE: using Sorcha.ServiceClients.Blueprint;
└── DELETE: Clients/ folder (8 files)
```

### Phase 3: Update Blueprint Service

Files to update:
```
src/Services/Sorcha.Blueprint.Service/
├── Sorcha.Blueprint.Service.csproj
├── Program.cs
└── DELETE: Clients/ folder (4 files)
```

### Phase 4: Update CLI Application

Files to update:
```
src/Apps/Sorcha.Cli/
├── Sorcha.Cli.csproj
├── Program.cs
└── DELETE: Services/*ServiceClient.cs (4 files)
```

### Phase 5: Cleanup and Documentation

1. Run full solution build
2. Run all integration tests
3. Update architecture documentation
4. Update CLAUDE.md with new pattern
5. Delete old client files from git history

## Comprehensive Interface Design

### `IWalletServiceClient` (Union of All Needs)

```csharp
public interface IWalletServiceClient
{
    // Validator Service needs
    Task<string> CreateOrRetrieveSystemWalletAsync(string validatorId, CancellationToken cancellationToken = default);
    Task<string> SignDataAsync(string walletId, string dataToSign, CancellationToken cancellationToken = default);
    Task<bool> VerifySignatureAsync(string publicKey, string data, string signature, string algorithm, CancellationToken cancellationToken = default);

    // Blueprint Service needs
    Task<byte[]> EncryptPayloadAsync(string recipientWalletAddress, byte[] payload, CancellationToken cancellationToken = default);
    Task<byte[]> DecryptPayloadAsync(string walletAddress, byte[] encryptedPayload, CancellationToken cancellationToken = default);
    Task<byte[]> DecryptWithDelegationAsync(string walletAddress, byte[] encryptedPayload, string delegationToken, CancellationToken cancellationToken = default);
    Task<byte[]> SignTransactionAsync(string walletAddress, byte[] transactionData, CancellationToken cancellationToken = default);
    Task<WalletInfo?> GetWalletAsync(string walletAddress, CancellationToken cancellationToken = default);

    // CLI needs (add as discovered)
    // ...
}
```

### `IRegisterServiceClient` (Union of All Needs)

```csharp
public interface IRegisterServiceClient
{
    // Validator Service needs
    Task<bool> WriteDocketAsync(Docket docket, CancellationToken cancellationToken = default);
    Task<Docket?> ReadDocketAsync(string registerId, long docketNumber, CancellationToken cancellationToken = default);
    Task<Docket?> ReadLatestDocketAsync(string registerId, CancellationToken cancellationToken = default);
    Task<long> GetRegisterHeightAsync(string registerId, CancellationToken cancellationToken = default);

    // Blueprint Service needs
    Task<TransactionModel> SubmitTransactionAsync(string registerId, TransactionModel transaction, CancellationToken cancellationToken = default);
    Task<TransactionModel?> GetTransactionAsync(string registerId, string transactionId, CancellationToken cancellationToken = default);
    Task<TransactionPage> GetTransactionsAsync(string registerId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<TransactionPage> GetTransactionsByWalletAsync(string registerId, string walletAddress, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<Register?> GetRegisterAsync(string registerId, CancellationToken cancellationToken = default);
    Task<List<TransactionModel>> GetTransactionsByInstanceIdAsync(string registerId, string instanceId, CancellationToken cancellationToken = default);
}
```

## Benefits

### Before (Duplicated)
- **Total Files:** 16 client files across 3 projects
- **Maintenance:** Update in 3 places for bug fixes
- **Testing:** Test same logic 3 times
- **Inconsistency:** Different retry policies, error handling

### After (Consolidated)
- **Total Files:** 8 client files in 1 library
- **Maintenance:** Update once, all services benefit
- **Testing:** Test once comprehensively
- **Consistency:** Unified error handling, retry policies

## Risks and Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| Breaking changes during migration | Service downtime | Migrate one service at a time with rollback plan |
| Missing methods in consolidated interface | Compilation errors | Comprehensive audit before migration |
| Configuration differences | Runtime errors | Standardize configuration format across services |
| gRPC proto changes | Incompatibility | Version proto contracts, use backward compatibility |

## Testing Strategy

1. **Unit Tests:** Mock service clients, test business logic
2. **Integration Tests:** Test actual gRPC/HTTP calls with Testcontainers
3. **Contract Tests:** Verify proto compatibility
4. **Smoke Tests:** End-to-end validation after migration

## Timeline

- **Phase 1:** 1-2 days (Create library, implement clients)
- **Phase 2:** 1 day (Update Validator Service)
- **Phase 3:** 1 day (Update Blueprint Service)
- **Phase 4:** 0.5 days (Update CLI)
- **Phase 5:** 0.5 days (Cleanup, documentation)

**Total Estimated Effort:** 4-5 days

## Next Steps

1. ✅ Create `Sorcha.ServiceClients` project
2. ⏳ Implement `IWalletServiceClient` with all methods
3. ⏳ Implement `IRegisterServiceClient` with all methods
4. ⏳ Implement `IBlueprintServiceClient`
5. ⏳ Implement `IPeerServiceClient`
6. ⏳ Create DI extension methods
7. ⏳ Update Validator Service to use consolidated clients
8. ⏳ Update Blueprint Service
9. ⏳ Update CLI
10. ⏳ Delete old client files
11. ⏳ Update documentation

---

**Status:** Library created, interfaces designed, implementation in progress
**Next Action:** Implement consolidated WalletServiceClient as reference implementation
