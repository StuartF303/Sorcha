# Critical Issues & Next Actions

**Last Updated:** 2025-12-14

---

## Resolved Issues

### ✅ Issue #1: Register Service API Disconnection (P0)

**Problem:** Register Service API stub existed but didn't use the Phase 1-2 core implementation

**Resolution (2025-11-16):**
1. ✅ Refactored Sorcha.Register.Service/Program.cs to use core managers
2. ✅ Replaced `TransactionStore` with `IRegisterRepository`
3. ✅ Integrated RegisterManager, TransactionManager, QueryManager
4. ✅ Added .NET Aspire integration
5. ✅ Implemented 20 REST endpoints + OData + SignalR
6. ✅ Complete OpenAPI documentation

**Commit:** `f9cdc86`

---

### ✅ Issue #2: DocketManager/ChainValidator Duplication (P1)

**Problem:** DocketManager and ChainValidator existed in both Register.Core and Validator.Service

**Resolution (2025-12-09):**
1. ✅ Confirmed implementations correctly moved to Validator.Service
2. ✅ Deleted orphaned test files from Register.Core.Tests
3. ✅ Implementations now only in: src/Services/Sorcha.Validator.Service/

---

### ✅ Issue #3: Missing SignalR Integration Tests (P1)

**Problem:** SignalR hub was implemented but had no integration tests

**Resolution (2025-11-16):**
1. ✅ Created SignalRIntegrationTests.cs (520+ lines, 14 tests)
2. ✅ Hub connection/disconnection lifecycle tests
3. ✅ Wallet subscription/unsubscription tests
4. ✅ All notification types tested
5. ✅ Multi-client broadcast scenarios
6. ✅ Wallet-specific notification isolation

---

### ✅ Issue #4: Register Service Missing Automated Tests (P1)

**Problem:** ~4,150 LOC of core implementation had no unit or integration tests

**Resolution (2025-11-16):**
1. ✅ Unit tests for all core managers
2. ✅ API integration tests with in-memory repository
3. ✅ SignalR hub integration tests
4. ✅ Query API integration tests
5. ✅ 112 comprehensive test methods
6. ✅ ~2,459 lines of test code

---

## Next Recommended Actions

### Completed Actions ✅

**Fix Register Service API Integration (P0)**
- ✅ Refactored to use Phase 1-2 core
- ✅ Added SignalR and OData support

**Add Blueprint Service SignalR Integration Tests (P1)**
- ✅ 14 tests covering all hub functionality

**Add Register Service Automated Tests (P1)**
- ✅ 112 test methods, ~2,459 lines of test code

---

### Immediate Priority (Week 1-2)

**1. Resolve Register Service Code Duplication (P1, 4-6h)**
- Decide on DocketManager/ChainValidator ownership
- Remove duplicate code
- Update references
- Document decision

**2. End-to-End Integration (P0, 24-32h)**
- Implement Wallet Service client in Blueprint Service
- Implement Register Service client in Blueprint Service
- Replace stub encryption/decryption with real Wallet Service calls
- Integration tests for Blueprint ↔ Wallet ↔ Register flow

**Total Effort:** ~30 hours

---

### Short-term Priority (Week 3-4)

**3. Wallet Service Production Readiness (P2, 16-20h)**
- ✅ EF Core repository implementation (DONE 2025-12-13)
- Azure Key Vault encryption provider
- Production authentication
- Address generation design decision

**Total Effort:** ~20 hours remaining

---

### Medium-term Priority (Week 5-8)

**4. End-to-End Integration (P0, 24-32h)**
- Blueprint → Action → Sign → Register flow
- File attachment end-to-end
- Multi-participant workflows
- Performance testing

**5. MongoDB Repository (P1, 12-16h)**
- Implement IRegisterRepository for MongoDB
- Add connection pooling and indexes
- Migration from in-memory

**6. Documentation Updates (P2, 16-20h)**
- API integration guides
- Deployment documentation
- Troubleshooting guides
- Code examples

**Total Effort:** ~60 hours

---

## Remaining Platform Gaps

1. **Persistent Storage** - MongoDB for Register, full production PostgreSQL
2. **API Gateway JWT validation** - Not yet implemented
3. **Peer Service tests** - 30% remaining (tests and polish)
4. **Validator Service** - 5% remaining (enclave support, persistence)
5. **Tenant Service** - 15% remaining (6 failing tests, Azure AD B2C)

---

## Recommendation

Focus on persistent storage implementation next. All three main services (Blueprint, Wallet, Register) now have JWT authentication integrated. The platform is production-ready and requires database implementation for production deployment.

---

**Back to:** [Development Status](../development-status.md)
