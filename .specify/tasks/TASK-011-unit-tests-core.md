# Task: Implement Unit Tests - Core Cryptography

**ID:** TASK-011
**Status:** Not Started
**Priority:** Critical
**Estimate:** 12 hours
**Assignee:** Unassigned
**Created:** 2025-11-12

## Objective

Implement comprehensive unit tests for core cryptography components (CryptoModule, KeyManager, SymmetricCrypto, HashProvider) achieving >90% code coverage.

## Test Coverage Requirements

### CryptoModuleTests.cs
- Key generation (ED25519, NISTP256, RSA4096)
- Signing/verification (all algorithms)
- Encryption/decryption (all algorithms)
- Public key calculation
- Error handling (invalid keys, null parameters)
- Async cancellation

### KeyManagerTests.cs
- Mnemonic generation (12 words, checksum)
- Mnemonic recovery
- KeyRing creation/management
- KeyChain export/import with encryption
- Password protection
- Key zeroization
- Argon2id seed derivation

### SymmetricCryptoTests.cs
- Encryption/decryption (AES-128/256, AES-GCM, ChaCha20, XChaCha20)
- Authentication tag validation (AEAD modes)
- Key/IV generation
- Round-trip tests
- Tamper detection

### HashProviderTests.cs
- Hash computation (SHA-256/384/512, Blake2b)
- Streaming hash computation
- HMAC computation
- Double hash
- Determinism tests

## Acceptance Criteria

- [ ] All core component tests implemented
- [ ] Code coverage >90%
- [ ] All tests passing
- [ ] Edge cases covered
- [ ] Performance within targets
- [ ] Proper async/await testing

---

**Task Control**
- **Created By:** Claude Code
- **Dependencies:** TASK-003, TASK-004, TASK-005, TASK-006, TASK-010
