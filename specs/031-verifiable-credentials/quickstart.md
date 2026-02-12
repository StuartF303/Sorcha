# Quickstart: Verifiable Credentials

**Branch**: `031-verifiable-credentials` | **Date**: 2026-02-12

## What This Feature Adds

Sorcha blueprints can now:
1. **Require credentials** on actions — participants must present a verifiable credential to proceed
2. **Issue credentials** from actions — blueprint flows can mint new credentials on completion
3. **Compose credential flows** — credential from Flow A gates entry to Flow B

## End-to-End Example: License Approval → Work Order

### Step 1: Create a License Approval Blueprint

A licensing authority creates a blueprint that issues a license credential:

```json
{
  "title": "Electrical License Approval",
  "participants": [
    { "id": "applicant", "name": "License Applicant", "walletAddress": "" },
    { "id": "certifier", "name": "Skill Certifier", "walletAddress": "0xcertifier..." },
    { "id": "authority", "name": "Licensing Authority", "walletAddress": "0xauthority..." }
  ],
  "actions": [
    {
      "id": 0,
      "title": "Apply for License",
      "sender": "applicant",
      "isStartingAction": true,
      "form": { "schema": { "properties": { "name": { "type": "string" }, "licenseType": { "type": "string" } } } },
      "routes": [{ "id": "to-certify", "nextActionIds": [1] }]
    },
    {
      "id": 1,
      "title": "Certify Skills",
      "sender": "certifier",
      "form": { "schema": { "properties": { "skillLevel": { "type": "string" } } } },
      "routes": [{ "id": "to-approve", "nextActionIds": [2] }]
    },
    {
      "id": 2,
      "title": "Approve License",
      "sender": "authority",
      "credentialIssuanceConfig": {
        "credentialType": "LicenseCredential",
        "claimMappings": [
          { "claimName": "licenseType", "sourceField": "/licenseType" },
          { "claimName": "level", "sourceField": "/skillLevel" },
          { "claimName": "name", "sourceField": "/name" }
        ],
        "recipientParticipantId": "applicant",
        "expiryDuration": "P365D",
        "registerId": "reg-licenses",
        "disclosable": ["licenseType", "level", "name"]
      }
    }
  ]
}
```

When the authority approves, the applicant receives a `LicenseCredential` stored in their wallet.

### Step 2: Create a Work Order Blueprint That Requires the License

```json
{
  "title": "Electrical Work Order",
  "participants": [
    { "id": "contractor", "name": "Licensed Contractor", "walletAddress": "" },
    { "id": "client", "name": "Property Owner", "walletAddress": "" }
  ],
  "actions": [
    {
      "id": 0,
      "title": "Submit Work Order",
      "sender": "contractor",
      "isStartingAction": true,
      "credentialRequirements": [
        {
          "type": "LicenseCredential",
          "acceptedIssuers": ["0xauthority..."],
          "requiredClaims": [
            { "claimName": "licenseType", "expectedValue": "electrical" }
          ],
          "revocationCheckPolicy": "failClosed",
          "description": "Valid electrical license from approved authority"
        }
      ],
      "form": { "schema": { "properties": { "workDescription": { "type": "string" } } } },
      "routes": [{ "id": "to-approve", "nextActionIds": [1] }]
    },
    {
      "id": 1,
      "title": "Approve Work Order",
      "sender": "client"
    }
  ]
}
```

### Step 3: The Credential Flow in Action

1. Contractor opens the "Submit Work Order" action
2. System auto-checks contractor's wallet for matching credentials
3. System finds the `LicenseCredential` issued by the authority
4. Contractor confirms the credential selection (selective disclosure: only `licenseType` is revealed)
5. System verifies: signature valid, not expired, not revoked, claim matches
6. Action proceeds — work order submitted

### Fluent API Equivalent

```csharp
var blueprint = BlueprintBuilder.Create()
    .WithTitle("Electrical Work Order")
    .AddParticipant("contractor", p => p
        .Named("Licensed Contractor"))
    .AddParticipant("client", p => p
        .Named("Property Owner"))
    .AddAction(0, a => a
        .WithTitle("Submit Work Order")
        .SentBy("contractor")
        .AsStartingAction()
        .RequiresCredential(cr => cr
            .OfType("LicenseCredential")
            .FromIssuer("0xauthority...")
            .RequireClaim("licenseType", "electrical")
            .WithRevocationCheck(RevocationCheckPolicy.FailClosed)
            .WithDescription("Valid electrical license from approved authority"))
        .RequiresData(d => d
            .AddProperty("workDescription", SchemaValueType.String))
        .RouteToNext("client"))
    .AddAction(1, a => a
        .WithTitle("Approve Work Order")
        .SentBy("client"))
    .Build();
```

## Key Concepts

| Concept | Description |
|---------|-------------|
| **Credential Requirement** | A gate on a blueprint action — "present credential X to proceed" |
| **Credential Issuance** | A blueprint action that mints a new credential for a participant |
| **Selective Disclosure** | Present only the claims needed — hide the rest cryptographically |
| **Credential Register** | An issuer-maintained ledger of all credentials they've issued (queryable) |
| **Revocation** | Issuer records revocation on the ledger; subsequent verifications reject it |
| **Auto-Match** | System automatically finds matching credentials in the participant's wallet |

## New Projects / Files

| Location | Purpose |
|----------|---------|
| `src/Common/Sorcha.Blueprint.Models/Credentials/` | CredentialRequirement, CredentialIssuanceConfig, ClaimConstraint, ClaimMapping models |
| `src/Core/Sorcha.Blueprint.Engine/Credentials/` | ICredentialVerifier, ICredentialIssuer, CredentialValidationResult |
| `src/Core/Sorcha.Blueprint.Fluent/CredentialBuilder.cs` | Fluent API for credential requirements and issuance config |
| `src/Common/Sorcha.Cryptography/SdJwt/` | SD-JWT VC creation, signing, verification, selective disclosure |
| `src/Services/Sorcha.Wallet.Service/Credentials/` | Credential storage, matching, export endpoints |
| `tests/Sorcha.Blueprint.Engine.Tests/Credentials/` | Credential verification tests |
| `tests/Sorcha.Cryptography.Tests/SdJwt/` | SD-JWT format tests |
