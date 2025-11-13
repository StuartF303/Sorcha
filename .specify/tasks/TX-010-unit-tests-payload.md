# Task: Unit Tests - Payload Management

**ID:** TX-010
**Status:** Not Started
**Priority:** Critical
**Estimate:** 10 hours
**Created:** 2025-11-12

## Test Coverage

### PayloadManagerTests.cs
- [ ] Add payload with single recipient
- [ ] Add payload with multiple recipients
- [ ] Decrypt payload with correct key
- [ ] Decrypt fails with wrong key
- [ ] Decrypt fails for non-recipient
- [ ] Grant access to new recipient
- [ ] Revoke access from recipient
- [ ] Verify payload hash
- [ ] Compress large payloads
- [ ] Skip compression for small payloads
- [ ] Skip compression for already compressed data

### PayloadEncryptionTests.cs
- [ ] Encrypt with ChaCha20-Poly1305
- [ ] Encrypt with AES-GCM
- [ ] Per-recipient key encryption (ED25519)
- [ ] Per-recipient key encryption (NISTP256)
- [ ] Per-recipient key encryption (RSA4096)
- [ ] Multiple payloads in transaction

### PayloadAccessControlTests.cs
- [ ] GetAccessiblePayloads returns correct payloads
- [ ] GetAccessiblePayloads excludes inaccessible
- [ ] GrantAccess adds recipient
- [ ] RevokeAccess removes recipient

## Acceptance Criteria

- [ ] All payload tests implemented
- [ ] Code coverage >90%
- [ ] All tests passing

---

**Dependencies:** TX-005, TX-008
