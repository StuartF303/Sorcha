# Code Review Findings - 2025-11-18

## New Tasks to Add to MASTER-TASKS.md

### Phase 0: Critical Production Blockers (NEW)

| ID | Task | Priority | Effort | Status | File | Line |
|----|------|----------|--------|--------|------|------|
| **TX-001** | **Implement PayloadManager encryption** | **P0** | 16h | 📋 Not Started | PayloadManager.cs | 58 |
| **TX-002** | **Implement PayloadManager decryption** | **P0** | 12h | 📋 Not Started | PayloadManager.cs | 89 |
| **TX-003** | **Implement PayloadManager key encryption** | **P0** | 8h | 📋 Not Started | PayloadManager.cs | 118 |
| **TX-004** | **Implement PayloadManager verification** | **P0** | 6h | 📋 Not Started | PayloadManager.cs | 141, 152 |
| **WS-041** | **Implement Wallet Service authentication middleware** | **P0** | 16h | 📋 Not Started | Program.cs | 49 |
| **WS-042** | **Implement Wallet Service authorization checks** | **P0** | 12h | 📋 Not Started | WalletEndpoints.cs | 219 |
| **WS-043** | **Replace InMemoryWalletRepository with EF Core** | **P0** | 24h | 📋 Not Started | WalletServiceExtensions.cs | 33 |
| **WS-044** | **Implement JWT claims extraction (user/tenant)** | **P0** | 8h | 📋 Not Started | WalletEndpoints.cs | 504, 510 |
| **BS-041** | **Replace participant wallet resolution placeholder** | **P0** | 12h | 📋 Not Started | ActionResolverService.cs | 151 |
| **TX-005** | **Fix Transaction deserialization to support signed transactions** | **P0** | 16h | 📋 Not Started | BinaryTransactionSerializer.cs | 122-209 |

### Phase 1: High-Priority Enhancements

| ID | Task | Priority | Effort | Status | File | Line |
|----|------|----------|--------|--------|------|------|
| **CRYPT-020** | **Implement BIP32 hierarchical key derivation** | **P1** | 40h | 📋 Not Started | N/A (new files needed) | - |
| **CRYPT-021** | **Implement BIP44 multi-account hierarchy** | **P1** | 24h | 📋 Not Started | N/A (new files needed) | - |
| **CRYPT-022** | **Implement NIST P-256 ECIES encryption** | **P1** | 16h | 📋 Not Started | CryptoModule.cs | 447-461 |
| **CRYPT-023** | **Implement KeyChain export with encryption** | **P1** | 12h | 📋 Not Started | KeyChain.cs | 112 |
| **CRYPT-024** | **Implement KeyChain import with decryption** | **P1** | 12h | 📋 Not Started | KeyChain.cs | 130 |
| **CRYPT-025** | **Implement RecoverKeySetAsync** | **P1** | 12h | 📋 Not Started | CryptoModule.cs | 56 |
| **CRYPT-026** | **Increase test coverage to 90%+ with test vectors** | **P1** | 32h | 📋 Not Started | Multiple test files | - |
| **TX-006** | **Implement Transaction V3 adapter** | **P1** | 16h | 📋 Not Started | TransactionFactory.cs | 88 |
| **TX-007** | **Implement Transaction V2 adapter** | **P1** | 16h | 📋 Not Started | TransactionFactory.cs | 96 |
| **TX-008** | **Implement Transaction V1 adapter** | **P1** | 16h | 📋 Not Started | TransactionFactory.cs | 104 |
| **TX-009** | **Implement version-specific serializers** | **P1** | 20h | 📋 Not Started | TransactionFactory.cs | 113 |
| **TX-010** | **Extract network and key from WIF** | **P1** | 8h | 📋 Not Started | Transaction.cs | 96 |
| **TX-011** | **Calculate sender wallet from private key** | **P1** | 8h | 📋 Not Started | Transaction.cs | 112 |
| **TX-012** | **Extract public key from wallet address** | **P1** | 8h | 📋 Not Started | Transaction.cs | 143 |
| **TX-013** | **Implement Transaction binary serialization** | **P1** | 16h | 📋 Not Started | Transaction.cs | 162 |
| **TX-014** | **Implement JsonSerializer binary serialization** | **P1** | 12h | 📋 Not Started | JsonTransactionSerializer.cs | 107 |
| **TX-015** | **Implement JsonSerializer binary deserialization** | **P1** | 12h | 📋 Not Started | JsonTransactionSerializer.cs | 114 |
| **BS-042** | **Implement schema repository integration** | **P1** | 16h | 📋 Not Started | Program.cs (Blueprint) | 272 |
| **BS-043** | **Add Redis SignalR backplane** | **P1** | 8h | 📋 Not Started | Program.cs (Blueprint) | 68 |
| **BS-044** | **Implement action routing rules evaluation** | **P1** | 12h | 📋 Not Started | Program.cs (Blueprint) | 450 |
| **BS-045** | **Implement blueprint validation (participant references)** | **P1** | 8h | 📋 Not Started | Program.cs (Blueprint) | 1367 |
| **BS-046** | **Implement graph cycle detection for blueprints** | **P1** | 12h | 📋 Not Started | Program.cs (Blueprint) | 1374 |

### Phase 2: Code Quality & Documentation

| ID | Task | Priority | Effort | Status | Notes |
|----|------|----------|--------|--------|-------|
| **BP-M-001** | Add XML documentation for 43 missing items | P2 | 4h | 📋 Not Started | Blueprint.Models |
| **BP-M-002** | Add @context property to Action model | P2 | 2h | 📋 Not Started | Action.cs |
| **BP-M-003** | Add @context property to Participant model | P2 | 2h | 📋 Not Started | Participant.cs |
| **BP-M-004** | Remove FluentValidation dependency | P2 | 1h | 📋 Not Started | Blueprint.Models.csproj |
| **BP-M-005** | Create Blueprint.Models README.md | P2 | 2h | 📋 Not Started | Blueprint.Models/ |
| **BP-M-006** | Consider Action.Id type change (int → string) | P2 | 4h | 📋 Not Started | Requires analysis |
| **CRYPT-027** | Fix or remove RIPEMD-160 placeholder | P2 | 8h | 📋 Not Started | HashProvider.cs:172-175 |
| **CLEAN-001** | Delete Sorcha.Validator.Core (empty project) | P2 | 1h | 📋 Not Started | src/Common/ |
| **CLEAN-002** | Delete Sorcha.Validator.Core.Tests (empty project) | P2 | 1h | 📋 Not Started | tests/ |
| **CLEAN-003** | Delete Sorcha.WalletService build artifacts directory | P2 | 1h | 📋 Not Started | src/Common/ |
| **CLEAN-004** | Delete UnitTest1.cs stub file | P2 | 1h | 📋 Not Started | tests/Wallet.Service.Tests/ |
| **CLEAN-005** | Delete obsolete Controller tests directory | P2 | 1h | 📋 Not Started | tests/Wallet.Service.Api.Tests/Controllers/ |
| **CLEAN-006** | Delete CryptoPayloadRequest.cs (deprecated) | P2 | 1h | 📋 Not Started | Wallet.Service/Models/ |
| **CLEAN-007** | Fix AppHost.cs project reference | P2 | 1h | 📋 Not Started | AppHost.cs:15 |
| **CLEAN-008** | Run git clean -fdX to remove build artifacts | P2 | 1h | 📋 Not Started | Solution-wide |
| **DOC-001** | Update MASTER-PLAN.md completion percentages | P2 | 2h | 📋 Not Started | Based on review |
| **DOC-002** | Update development-status.md metrics | P2 | 2h | 📋 Not Started | LOC, test counts |
| **DOC-003** | Fix path references (Windows format) | P2 | 1h | 📋 Not Started | development-status.md |
| **DOC-004** | Add README.md files to archive directories | P2 | 4h | 📋 Not Started | .specify/archive/ |
| **DOC-005** | Update Validator Service documentation | P2 | 2h | 📋 Not Started | Reflect current architecture |
| **DOC-006** | Move Action Service spec to archive | P2 | 1h | 📋 Not Started | .specify/specs/ |

### Phase 3: Future Enhancements (P3)

| ID | Task | Priority | Effort | Status | Notes |
|----|------|----------|--------|--------|-------|
| **UI-001** | Implement service worker background sync | P3 | 8h | 📋 Not Started | service-worker.published.js:189 |
| **UI-002** | Implement service worker push notifications | P3 | 8h | 📋 Not Started | service-worker.published.js:195 |
| **WS-045** | Design address generation security model | P3 | 16h | 📋 Not Started | Requires architecture decision |

## Summary Statistics

**New P0 Tasks:** 10 (Critical MVD blockers)
**New P1 Tasks:** 22 (High-priority enhancements)
**New P2 Tasks:** 23 (Code quality & documentation)
**New P3 Tasks:** 3 (Future enhancements)
**Total New Tasks:** 58

**Estimated Total Effort:**
- P0: 146 hours (~3.7 weeks for 1 developer)
- P1: 404 hours (~10 weeks for 1 developer)
- P2: 43 hours (~1 week for 1 developer)
- P3: 32 hours (~0.8 weeks for 1 developer)

## Immediate Actions Required

1. ✅ Create this findings document
2. ⏭️ Update MASTER-TASKS.md with new tasks
3. ⏭️ Fix AppHost.cs project reference (CLEAN-007)
4. ⏭️ Delete empty/obsolete projects (CLEAN-001 to CLEAN-006)
5. ⏭️ Clean build artifacts (CLEAN-008)
