# Quickstart: Verifiable Credential Lifecycle & Presentations

**Feature**: 039-verifiable-presentations
**Branch**: `039-verifiable-presentations`

## Prerequisites

- .NET 10 SDK
- Docker Desktop (for integration tests)
- Existing Sorcha development environment

## Build & Test

```bash
# Build the solution
dotnet build

# Run all tests
dotnet test

# Run specific test projects relevant to this feature
dotnet test tests/Sorcha.ServiceClients.Tests
dotnet test tests/Sorcha.Wallet.Service.Tests
dotnet test tests/Sorcha.Blueprint.Service.Tests
dotnet test tests/Sorcha.Blueprint.Engine.Tests
dotnet test tests/Sorcha.Blueprint.Models.Tests
dotnet test tests/Sorcha.UI.Core.Tests
```

## Key Files to Understand First

1. **Existing credential models**: `src/Common/Sorcha.Blueprint.Models/Credentials/` — all 7 model files
2. **Existing SD-JWT crypto**: `src/Common/Sorcha.Cryptography/SdJwt/SdJwtService.cs` — the signing engine
3. **Existing wallet endpoints**: `src/Services/Sorcha.Wallet.Service/Endpoints/CredentialEndpoints.cs` — 9 REST endpoints
4. **Existing engine integration**: `src/Core/Sorcha.Blueprint.Engine/Credentials/` — CredentialVerifier + CredentialIssuer
5. **Existing DID model**: `src/Common/Sorcha.Register.Models/SorchaDidIdentifier.cs` — the `did:sorcha` value object
6. **Construction Permit example**: `walkthroughs/ConstructionPermit/construction-permit-template.json` — credential issuance in practice

## Implementation Order

The feature is organized into 6 implementation phases:

1. **DID Resolution Layer** — IDidResolver interface + 3 implementations
2. **Credential Lifecycle** — Extended states, UsagePolicy, CredentialDisplayConfig
3. **Bitstring Status List** — Status list model, manager, register storage, cached endpoint
4. **OID4VP Presentations** — Presentation request/submit/verify endpoints
5. **Wallet Card UI** — Credential cards, detail view, presentation inbox
6. **Cross-Blueprint Integration** — Engine upgrades for status list checks + usage policy

Each phase is independently testable and deployable.

## Verification

After implementation, verify the feature works end-to-end:

1. Issue a credential via a blueprint action
2. View it in the wallet card UI
3. Create a presentation request (verifier side)
4. Present the credential with selective disclosure (holder side)
5. Verify the presentation succeeds
6. Revoke the credential (issuer side)
7. Verify subsequent presentations fail
8. Test the QR code flow with a second browser tab as the terminal
