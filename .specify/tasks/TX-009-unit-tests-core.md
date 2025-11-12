# Task: Unit Tests - Transaction Core

**ID:** TX-009
**Status:** Not Started
**Priority:** Critical
**Estimate:** 10 hours
**Created:** 2025-11-12

## Test Coverage

### TransactionTests.cs
- [ ] Create transaction with metadata
- [ ] Add recipients (valid wallets)
- [ ] Invalid recipient handling
- [ ] Sign transaction (double SHA-256)
- [ ] Verify signature
- [ ] Verify prevents modification after signing
- [ ] Transaction hash calculation
- [ ] Timestamp generation

### TransactionBuilderTests.cs
- [ ] Fluent API method chaining
- [ ] Create with different versions
- [ ] WithRecipients validation
- [ ] WithMetadata JSON validation
- [ ] AddPayload integration
- [ ] SignAsync working
- [ ] Build returns signed transaction
- [ ] Prevents modification after signing

### TransactionVerifierTests.cs
- [ ] Verify valid transaction
- [ ] Reject invalid signature
- [ ] Reject tampered metadata
- [ ] Reject tampered payload
- [ ] Verify double SHA-256 hashing

## Acceptance Criteria

- [ ] All core tests implemented
- [ ] Code coverage >90%
- [ ] All tests passing

---

**Dependencies:** TX-003, TX-004, TX-008
