# Task: Update Register Service to Use New Library

**ID:** TASK-023
**Status:** Not Started
**Priority:** Medium
**Estimate:** 6 hours
**Assignee:** Unassigned
**Created:** 2025-11-12

## Objective

Update Register Service cryptographic operations to use Siccar.Cryptography v2.0 for transaction validation and processing.

## Migration Steps

### 1. Update Dependencies
- Remove SiccarPlatformCryptography reference
- Add Siccar.Cryptography v2.0 package

### 2. Update Transaction Verification
- Update signature verification to use new CryptoModule
- Handle async verification
- Maintain support for old transaction formats

### 3. Update Block Hashing
- Use new HashProvider for block hash computation
- Verify double SHA-256 compatibility
- Update Merkle tree computation if needed

### 4. Transaction Formatting
- Create custom transaction serialization (not in v2.0)
- Use new crypto for signing/verification only
- Maintain backward compatibility with existing transactions

## Testing Requirements

- [ ] Transaction validation working
- [ ] Block hashing working
- [ ] Old transaction format compatibility
- [ ] Performance comparable
- [ ] All integration tests passing

## Acceptance Criteria

- [ ] Register Service compiles with new library
- [ ] Transaction validation functional
- [ ] Backward compatibility maintained
- [ ] All tests passing

---

**Task Control**
- **Created By:** Claude Code
- **Dependencies:** TASK-001 through TASK-020, TASK-021
