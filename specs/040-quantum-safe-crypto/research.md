# Research: Quantum-Safe Cryptography Upgrade

**Feature Branch**: `040-quantum-safe-crypto`
**Date**: 2026-02-25

## R1: BouncyCastle.NET PQC Integration

**Decision**: Add `BouncyCastle.Cryptography` (v2.6.2, already used in Tenant Service) to `Sorcha.Cryptography.csproj` for ML-DSA, ML-KEM, and SLH-DSA implementations.

**Rationale**: BouncyCastle 2.5.0+ includes NIST-standardized PQC algorithms (FIPS 203/204/205). v2.6.2 is already in the solution dependency graph via Tenant Service, so no new transitive dependency risk. Sodium.Core (libsodium) remains for ED25519 — BouncyCastle supplements, doesn't replace.

**Alternatives considered**:
- liboqs (Open Quantum Safe): C library with .NET bindings — adds native dependency complexity unsuitable for chiseled Docker images.
- .NET 10 native PQC: Not yet available. Microsoft has an open issue but no timeline.
- Custom implementation: Never implement your own cryptography.

## R2: CryptoModule Extension Pattern

**Decision**: Extend the existing `WalletNetworks` byte enum and switch-expression dispatch pattern in `CryptoModule.cs` (6 switch expressions across Sign/Verify/Encrypt/Decrypt/Generate/Recover methods).

**Rationale**: CryptoModule already dispatches on `WalletNetworks` byte. Adding `ML_DSA_65 = 0x10`, `SLH_DSA_128 = 0x11`, `ML_KEM_768 = 0x12` keeps the existing contract intact. All 6 switch expressions need new cases, but the pattern is proven and consistent.

**Alternatives considered**:
- Strategy pattern with DI-registered handlers: Over-engineering for 3 additional algorithms. The switch expression is clear and maintainable.
- Separate IPqcModule interface: Violates "single module" principle. Callers shouldn't need to know if an algorithm is classical or PQC.

## R3: Wallet Address Format for PQC Keys

**Decision**: Use hash-based addressing with a new HRP prefix `ws2` (Sorcha Wallet v2). Address = `ws2` + Bech32m(SHA-256(network_byte + public_key)[0..31]). Full public key stored as witness data in transactions.

**Rationale**:
- Current `ws1` Bech32 has 90-char limit (~52 bytes decoded). ML-DSA-65 public key is 1,952 bytes — doesn't fit.
- Bitcoin BIP-360 proposes P2QRH (hash-based) for the same reason. Industry consensus is hash-then-encode for PQC addresses.
- SHA-256 hash gives 32 bytes → fits within Bech32m encoding → ~62 character address.
- New `ws2` prefix clearly distinguishes PQC from classical without breaking existing `ws1` parsing.
- Bech32m (improved checksum) preferred over Bech32 for new address versions.

**Alternatives considered**:
- Extend Bech32 data limit: Would break the standard and all existing tooling.
- Use Base58 with larger capacity: Loses the error-detection benefits of Bech32.
- Same `ws1` prefix with version byte: Ambiguous; could confuse parsers expecting raw public key.

## R4: TransactionModel Signature Field

**Decision**: Evolve the `Signature` field from a simple `string` to a structured format supporting multiple signatures (hybrid mode). Use a JSON structure: `{"classical":"<base64>","pqc":"<base64>","witness":"<base64-public-key>"}`. Serialize as string for backward compatibility.

**Rationale**: The current `string Signature` field (TransactionModel.cs:93-97) holds a single base64 signature. For hybrid mode, both signatures must be stored. A JSON-encoded string maintains wire compatibility while adding structure. The `witness` field carries the full PQC public key (since the address only contains a hash).

**Alternatives considered**:
- Add separate `PqcSignature` property: Breaking change to TransactionModel, all serializers, all consumers.
- Use `string[]` Signatures array: Loses semantic meaning (which is classical, which is PQC?).
- New TransactionModel v2: Too disruptive; version field already exists for forward compatibility.

## R5: Register Crypto Policy Storage

**Decision**: Add a `CryptoPolicy` section to the `RegisterControlRecord` payload embedded in genesis control transactions. Policy updates use the existing governance control transaction mechanism with a new action type `control.crypto.update`.

**Rationale**: Control transactions already carry the full register configuration (roster, attestations). Adding CryptoPolicy to this structure follows the established pattern. The governance roster reconstruction service already reads the latest control TX payload — it naturally extends to include crypto policy. The 7 existing control action types (validator register/approve/suspend/remove, config update, blueprint publish, metadata update) have a clear extension point for `control.crypto.update`.

**Alternatives considered**:
- Separate register metadata endpoint: Doesn't provide immutable audit trail or versioning.
- Store in MongoDB register collection: Loses the on-chain governance guarantees.
- Per-transaction algorithm negotiation: Too much overhead; register-level policy is the right granularity.

## R6: HD Wallet Derivation for PQC Keys

**Decision**: Use separate derivation paths with a new coinType for PQC keys. Classical: `m/44'/0'/account'/change/index`. PQC: `m/44'/1'/account'/change/index`. The BIP39 seed generates a master entropy that feeds both classical and PQC key generation independently.

**Rationale**: ML-DSA and SLH-DSA do not use elliptic curve operations, so BIP32 hierarchical derivation (which depends on secp256k1 math) cannot directly derive PQC keys. Instead, derive 32 bytes of keying material from the HD path using HMAC-SHA512, then use those bytes as seed entropy for the PQC key generator.

**Alternatives considered**:
- Completely separate mnemonic for PQC: User burden of managing two recovery phrases.
- NIST SP 800-108 KDF: More complex, no ecosystem tooling. HMAC-SHA512 from BIP32 is sufficient entropy.
- No HD derivation for PQC (random keys only): Loses the deterministic recovery benefit.

## R7: BLS Threshold Library

**Decision**: Use Herumi BLS library (C# bindings) for BLS12-381 threshold signatures. This is the most widely deployed BLS implementation (used by Ethereum 2.0).

**Rationale**: BLS12-381 is the industry standard curve for threshold signatures. Herumi's implementation is battle-tested in Ethereum's beacon chain with billions of dollars at stake. C# bindings exist via NuGet.

**Alternatives considered**:
- BouncyCastle BLS: Less mature than Herumi for threshold operations.
- CIRCL (Cloudflare): Go library, no native C# support.
- Noble-BLS (JavaScript): Wrong platform.

## R8: Zero-Knowledge Proof Approach

**Decision**: Use Bulletproofs for range proofs (no trusted setup required) and Merkle inclusion proofs with Poseidon hash for ZK-SNARK-friendly verification. Defer full ZK-SNARK integration (Groth16/PLONK) to a future phase.

**Rationale**: Bulletproofs are well-understood, require no trusted setup, and directly address the range proof use case in blueprint validation. Merkle inclusion proofs are already partially implemented via the existing MerkleTree utility. Full ZK-SNARK systems add significant complexity (circuit compilation, proving key generation) that is better addressed in a dedicated follow-up feature.

**Alternatives considered**:
- Full Groth16 ZK-SNARKs from day one: Too complex for initial release; trusted setup requirement.
- ZK-STARKs: Larger proof sizes, better suited for computational integrity than simple inclusion/range proofs.
- No ZKP (just Merkle proofs): Misses the privacy-preserving verification requirement.

## R9: Infrastructure Size Impact

**Decision**: PQC signatures (~3.3KB for ML-DSA-65) are well within MongoDB's 16MB document limit and gRPC's 4MB default. No infrastructure changes needed for Phase 1. Monitor and adjust for BLS aggregate operations in Phase 2.

**Rationale**: A hybrid-signed transaction adds ~3.4KB (3.3KB PQC sig + ~100 bytes overhead). Current transactions are typically <10KB. Even with 100 transactions per docket, total docket size is ~1.3MB — well within limits. Redis key size limit is 512MB. API Gateway YARP proxying has no payload size limit by default.

**Impact Summary**:
| Resource | Current | With PQC | Margin |
|----------|---------|----------|--------|
| MongoDB doc | ~10KB avg | ~13.4KB avg | 16MB limit → 99.9% headroom |
| gRPC message | ~10KB | ~13.4KB | 4MB limit → 99.7% headroom |
| Redis cache entry | ~1KB | ~4.4KB | 512MB limit → negligible |
| Merkle tree hash input | 64 bytes (TxId) | 64 bytes (TxId unchanged) | No impact |
| Docket Merkle root | 32 bytes | 32 bytes | No impact |
