# Research: CLI Register Commands Update

**Branch**: `016-cli-register-update` | **Date**: 2026-01-28

## R1: Shared Model Compatibility

**Decision**: Reference `Sorcha.Register.Models` directly from the CLI project.

**Rationale**: The Register.Models project has zero external dependencies (only System/Microsoft assemblies), so adding it as a project reference introduces no version conflicts. The UI project (`Sorcha.UI.Core`) already does this successfully.

**Alternatives considered**:
- Copy models into CLI (current approach) - rejected: already out of sync, maintenance burden
- Create a shared DTO project - rejected: unnecessary indirection, models already exist

**Key differences to handle**:
- Backend `Register` model uses `TenantId`; CLI model uses `OrganizationId` - resolve by using shared model's `TenantId`
- Backend `TransactionModel` has JSON-LD properties (`@context`, `@type`, `@id`) - these serialize correctly via `JsonPropertyName` attributes already present
- Backend model field `Height` is `uint`; CLI model has `TransactionCount` as `long` - these are different fields; `TransactionCount` doesn't exist in shared model (derived from queries)
- CLI `Transaction` model uses flat `Payload` string; backend `TransactionModel` uses `PayloadModel[] Payloads` array - must update display logic

## R2: Two-Phase Register Creation Signing Flow

**Decision**: Use the wallet service's `/api/v1/wallets/{address}/sign` endpoint with `IsPreHashed=true` to sign attestation data.

**Rationale**: The attestation flow requires signing a SHA-256 hash of canonical JSON. The wallet sign endpoint accepts pre-hashed data via `IsPreHashed=true` and returns base64 signature + public key. The CLI's existing `IWalletServiceClient` already has the `SignTransactionAsync` method, but the CLI's `SignTransactionRequest` model is missing `IsPreHashed` and `DerivationPath` fields.

**Flow**:
1. CLI calls `POST /api/registers/initiate` with name, tenantId, single owner (userId + walletId)
2. Backend returns `InitiateRegisterCreationResponse` with `attestationsToSign` array containing hex-encoded `dataToSign` hash per attestation
3. CLI converts each `dataToSign` hex string to bytes, base64-encodes it, sends to wallet sign endpoint with `IsPreHashed=true`
4. Wallet returns base64 signature + public key
5. CLI constructs `FinalizeRegisterCreationRequest` with signed attestations
6. CLI calls `POST /api/registers/finalize`
7. Backend returns register ID, genesis transaction ID, genesis docket ID

**Alternatives considered**:
- External signing (user signs manually) - rejected: poor UX for CLI tool
- Direct crypto in CLI (embed signing library) - rejected: wallet service exists for this purpose

## R3: API Gateway Routes for Query and OData Endpoints

**Decision**: CLI connects directly to the Register Service for query/OData endpoints (bypassing API Gateway).

**Rationale**: The API Gateway has no routes for `/api/query/*` or `/odata/*`. Adding gateway routes is a separate infrastructure concern. The CLI already has `CreateRegisterServiceClientAsync` which points to the register service URL from the profile. All query and OData endpoints are on the same register service, so the existing client base URL works.

**Alternatives considered**:
- Add YARP routes to gateway first - rejected: out of scope; would require gateway redeployment
- Create separate query service client - rejected: same host, unnecessary duplication

**Note**: The register service client already points to the register service directly (not through the gateway). The existing `IRegisterServiceClient` Refit interface will be extended with the new endpoints.

## R4: CLI Sign Request Model Gap

**Decision**: Update the CLI's `SignTransactionRequest` to include `IsPreHashed` and `DerivationPath` fields.

**Rationale**: The backend `SignTransactionRequest` (in `Sorcha.Wallet.Service.Models`) has three fields: `TransactionData`, `DerivationPath`, and `IsPreHashed`. The CLI's copy only has `TransactionData`. For attestation signing, we need `IsPreHashed=true` to pass the SHA-256 hash directly.

**Note**: Wallet models remain as CLI-local DTOs since there is no shared `Sorcha.Wallet.Models` library. Only register/blueprint models move to shared references.

## R5: Output Formatting Infrastructure

**Decision**: Use existing `IOutputFormatter` infrastructure with `--output` global flag.

**Rationale**: The CLI already has `JsonOutputFormatter`, `CsvOutputFormatter`, and `TableOutputFormatter` implementations. New commands should use `WriteOutput<T>` and `WriteCollection<T>` from the base command pattern to support all output formats automatically.

**No additional work needed** beyond using the existing formatters in new commands.

## R6: Pagination Alignment

**Decision**: New commands use `--page` and `--page-size` matching the backend. Existing `tx list` command's `--skip`/`--take` will be migrated to `--page`/`--page-size`.

**Rationale**: The backend query API uses `page`/`pageSize` parameters. Aligning the CLI with the backend convention reduces confusion.

**Migration path**: Replace `--skip`/`--take` options in `TxListCommand` with `--page`/`--page-size`. Convert page/pageSize to skip/take internally if the underlying API still uses offset-based pagination.
