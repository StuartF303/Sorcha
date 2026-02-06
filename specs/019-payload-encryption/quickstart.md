# Quickstart: Payload Encryption Verification

**Feature**: 019-payload-encryption
**Date**: 2026-02-06

## Build & Test

```bash
# Build affected projects
dotnet build src/Common/Sorcha.TransactionHandler/Sorcha.TransactionHandler.csproj
dotnet build src/Common/Sorcha.Cryptography/Sorcha.Cryptography.csproj

# Run TransactionHandler tests (new encryption tests will be here)
dotnet test tests/Sorcha.TransactionHandler.Tests/

# Run all tests to verify no regressions
dotnet test
```

## Verify Encryption Works

After implementation, the following tests should pass:

### Core Encryption
- `AddPayloadAsync_WithRecipients_EncryptsData` — stored Data differs from input
- `AddPayloadAsync_WithRecipients_GeneratesRandomIV` — IV is non-zero, correct length
- `AddPayloadAsync_WithRecipients_ComputesSHA256Hash` — Hash is 32 bytes, matches plaintext digest
- `AddPayloadAsync_TwiceWithSameData_ProducesDifferentCiphertext` — non-deterministic encryption

### Decryption Round-Trip
- `GetPayloadDataAsync_AuthorizedRecipient_ReturnsOriginalPlaintext` — byte-for-byte match
- `GetPayloadDataAsync_UnauthorizedWallet_ReturnsAccessDenied` — no data leakage
- `GetPayloadDataAsync_WrongPrivateKey_ReturnsDecryptionFailed` — error, no partial data

### Integrity Verification
- `VerifyPayloadAsync_UntamperedPayload_ReturnsTrue`
- `VerifyPayloadAsync_TamperedData_ReturnsFalse`
- `VerifyAllAsync_MixedPayloads_ReturnsFalseOnAnyFailure`

### Access Control
- `GrantAccessAsync_NewRecipient_CanDecryptPayload`
- `GrantAccessAsync_ExistingRecipient_ReturnsSuccessNoChange`

### Backward Compatibility
- `GetPayloadDataAsync_LegacyZeroIV_ReturnsRawData`
- `VerifyPayloadAsync_LegacyZeroHash_ReturnsTrue`

## Key Files

| File | Purpose |
|------|---------|
| `src/Common/Sorcha.TransactionHandler/Payload/PayloadManager.cs` | Main implementation — all stubs replaced |
| `src/Common/Sorcha.TransactionHandler/Interfaces/IPayloadManager.cs` | Interface updates for crypto key params |
| `tests/Sorcha.TransactionHandler.Tests/Unit/PayloadManagerTests.cs` | New test file for encryption tests |
| `src/Common/Sorcha.TransactionHandler/Core/TransactionBuilder.cs` | Updated to pass crypto deps to PayloadManager |

## Smoke Test (Manual)

```csharp
// Create PayloadManager with real crypto
var crypto = new SymmetricCrypto();
var module = new CryptoModule();
var hash = new HashProvider();
var pm = new PayloadManager(crypto, module, hash);

// Add encrypted payload
var data = Encoding.UTF8.GetBytes("Hello, DAD Security!");
var result = await pm.AddPayloadAsync(data, recipients, options);

// Verify encryption
var payloads = await pm.GetAllAsync();
var payload = payloads.First();
payload.Data.Should().NotBeEquivalentTo(data); // Encrypted!
payload.IV.Should().NotBeEquivalentTo(new byte[24]); // Random IV!
```
