# Test Failure Quick Wins

**Created:** 2026-02-06
**Baseline:** 2,581 passing / 361 failing across 24 test projects
**14 projects fully green (1,863 tests)**

---

## Tier 1: Easy Fixes (~65 tests)

### 1. Peer.Service.Tests — Extract Interfaces (29 tests)

**Root cause:** `new Mock<ConcreteClass>()` fails because `StunClient`, `PeerListManager`, `NetworkAddressService`, `PeerDiscoveryService`, `HealthMonitorService`, `HubNodeDiscoveryService`, `HubNodeConnectionManager` all have constructor dependencies and no parameterless constructor.

**Fix:** Extract interfaces (`IStunClient`, `IPeerListManager`, etc.) and mock those instead. Alternatively, pass constructor args to `new Mock<T>(arg1, arg2)` and mark methods `virtual`.

**Files:**
- `tests/Sorcha.Peer.Service.Tests/PeerServiceTests.cs` (line 32)
- `tests/Sorcha.Peer.Service.Tests/Monitoring/HealthMonitorServiceTests.cs` (line 44)
- `tests/Sorcha.Peer.Service.Tests/Network/NetworkAddressServiceTests.cs` (line 26)

**Bonus:** 1 additional failure in `PeerListManagerTests.UpdateLocalPeerStatus_FailoverScenario_TracksCorrectly` — `GetLocalPeerStatus()` returns a mutable reference instead of a snapshot, so assertions see mutated state.

---

### 2. Cli.Tests — Add Missing `GetProfileAsync` Mock (22 tests)

**Root cause:** All command tests mock `GetActiveProfileAsync()` and `GetAccessTokenAsync()` but NOT `GetProfileAsync()`. The `HttpClientFactory.CreateWalletServiceClientAsync("test")` calls `_configService.GetProfileAsync("test")` internally, which returns `null` → throws `InvalidOperationException("Profile 'test' does not exist.")`.

**Fix:** Add one mock setup to each test class constructor:
```csharp
_mockConfigService.Setup(x => x.GetProfileAsync(It.IsAny<string>()))
    .ReturnsAsync(new Profile { Name = "test", ServiceUrl = "http://localhost" });
```

**Files:**
- `tests/Sorcha.Cli.Tests/Commands/WalletCommandsTests.cs`
- `tests/Sorcha.Cli.Tests/Commands/OrganizationCommandsTests.cs`
- `tests/Sorcha.Cli.Tests/Commands/PeerCommandsTests.cs`
- `tests/Sorcha.Cli.Tests/Commands/ServicePrincipalCommandsTests.cs`
- `tests/Sorcha.Cli.Tests/Commands/UserCommandsTests.cs`

---

### 3. Tenant.Service.Tests — BCrypt vs SHA256 Seeder Mismatch (6 tests)

**Root cause:** `TestDataSeeder` hashes client secret with BCrypt:
```csharp
ClientSecretEncrypted = Encoding.UTF8.GetBytes(BCrypt.Net.BCrypt.HashPassword("test-client-secret"))
```
But `ServiceAuthService.VerifyClientSecret()` verifies with SHA256. They never match.

**Fix:** Change seeder to use SHA256:
```csharp
using var sha256 = SHA256.Create();
ClientSecretEncrypted = sha256.ComputeHash(Encoding.UTF8.GetBytes("test-client-secret"));
```

**Files:**
- `tests/Sorcha.Tenant.Service.Tests/TestDataSeeder.cs` (line ~95)

**Affected tests:** `Login_WithValidCredentials`, `Bootstrap_CreatedAdminCanLoginWithPassword`, `RefreshToken_WithValidToken`, `GetOAuth2Token_WithClientCredentials`, `GetOAuth2Token_WithPasswordGrant`, `GetDelegatedToken_WithValidCredentials`

---

### 4. UI.Core.Tests — CreateRegisterWizard Step Numbers (3 tests)

**Root cause:** A wallet selection step was inserted at step 2, shifting options → step 3, review → step 4. Tests still use old 3-step numbering.

| Step | Old (tests expect) | New (code has) |
|------|-------------------|----------------|
| 1 | Name entry | Name entry |
| 2 | Options (always true) | Wallet selection (`HasValidWallet`) |
| 3 | Review (needs SignedControlRecord) | Options (always true) |
| 4 | N/A | Review (needs SignedControlRecord) |

**Fix:** Update test assertions to match new 4-step flow and set `SelectedWalletAddress` for step 2.

**Files:**
- `tests/Sorcha.UI.Core.Tests/Components/Registers/CreateRegisterWizardTests.cs`
- Source: `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Registers/RegisterCreationState.cs`

---

### 5. Blueprint.Engine.Tests — JsonLogicCache (3 tests)

**Root cause:** Cache key doesn't incorporate generic type `T`, so `GetOrAdd<string>` and `GetOrAdd<int>` with the same expression collide → `InvalidCastException`. Also missing null guard on factory parameter (throws `NullReferenceException` instead of `ArgumentNullException`). Parallel access test shows no deduplication.

**Fix:** Include `typeof(T).FullName` in cache key. Add `ArgumentNullException.ThrowIfNull(factory)` guard. Use `Lazy<T>` or `SemaphoreSlim` for parallel deduplication.

**Files:**
- `src/Core/Sorcha.Blueprint.Engine/Caching/JsonLogicCache.cs` (line ~53)

---

### 6. Blueprint.Engine.Tests — JSON Pointer Unescaping (2 tests)

**Root cause:** `DisclosureProcessor` uses JSON Pointer escaped names (`~1`, `~0`) as output dictionary keys without unescaping per RFC 6901 (`~1` → `/`, `~0` → `~`).

**Fix:** Add unescaping: `fieldName.Replace("~1", "/").Replace("~0", "~")` (order matters — unescape `~1` first).

**Files:**
- `src/Core/Sorcha.Blueprint.Engine/Processors/DisclosureProcessor.cs`

---

## Tier 2: Medium Effort (~31 tests)

### 7. Cli.Tests — xUnit Collection Race Condition (9 tests)

Three test classes use different `[Collection]` names but all mutate the global `SORCHA_CONFIG_DIR` environment variable. xUnit runs different collections in parallel.

**Fix:** Use a single shared `[Collection("CliTests")]` across `ConfigurationServiceTests`, `HttpClientFactoryIntegrationTests`, and `AuthCommandsTests`.

---

### 8. Tenant.Service.Tests — Serilog Static Logger Freeze (7 tests)

Multiple `WebApplicationFactory<Program>` instances try to freeze the same static `Log.Logger` concurrently.

**Fix:** Either `[assembly: CollectionBehavior(DisableTestParallelization = true)]` or put all integration test classes in a single `[Collection("TenantService")]`.

---

### 9. Blueprint.Engine.Tests — Routing Returns Null (5 tests)

`ExecutionEngine` routing evaluation doesn't populate `result.Routing.NextParticipantId`. All integration tests checking conditional routing get `null`.

**Fix:** Debug the routing code path in `ExecutionEngine` — likely the routing rules/conditions are evaluated but the result isn't assigned.

---

### 10. UI.Core.Tests — YAML Round-Trip (3 tests)

YamlDotNet deserialization fails for `Blueprint` model properties with `CamelCaseNamingConvention`. JSON round-trip works fine.

**Fix:** Investigate YamlDotNet serializer settings or add `[YamlMember]` attributes.

---

### 11. Blueprint.Engine.Tests — Calculations as Strings (2 tests)

Calculation engine returns `"55.0"` (string) instead of `55.0` (double) in `ProcessedData`.

**Fix:** Preserve numeric types in JSON-e/JsonLogic evaluator output.

---

### 12. Miscellaneous (5 tests)

- `JsonLogicValidatorTests.Validate_ExceedsMaxDepth` — test has malformed JSON (extra `}`)
- `DisclosureValidationTests` (2) — `[MinLength(0)]` should be `[MinLength(1)]` on Disclosures collection
- `JsonEEvaluatorTests` (2) — JSON-e evaluator not merging nested context objects
- `ExportImportTests.MissingId` — `Blueprint.Id` default initializer (`Guid.NewGuid()`) means ID is never empty after deserialization

---

## Tier 3: Infrastructure-Dependent (not code fixes)

| Project | Failures | Requires |
|---------|----------|----------|
| UI.E2E.Tests | 213 | Docker + Playwright browser |
| Gateway.Integration.Tests | 20 | All services running |
| Register.Storage.MongoDB.Tests | 19 | MongoDB instance |
| Integration.Tests | 6 | Running services |
| Wallet.Service.Api.Tests | 2 | PostgreSQL instance |

**Total: 260 failures** — these are expected when infrastructure isn't running.

---

## Priority Order

1. **Peer interfaces** (29) + **CLI mock** (22) + **Tenant seeder** (6) = **57 tests** with minimal risk
2. **Wizard steps** (3) + **JsonLogicCache** (3) + **JSON Pointer** (2) = **8 tests**, small targeted fixes
3. **Collection attributes** for CLI (9) and Tenant (7) = **16 tests**, test infrastructure only
4. **Routing null** (5) + **YAML** (3) + **calc types** (2) + **misc** (5) = **15 tests**, deeper investigation needed

**Estimated total recoverable: ~96 tests (without infrastructure)**
