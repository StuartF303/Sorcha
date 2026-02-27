# Data Model: 044 Codebase Consolidation

**Date**: 2026-02-27
**Branch**: `044-codebase-consolidation`

> This feature is a refactoring/consolidation effort. No new database entities, tables, or storage schemas are introduced. The "entities" below represent shared code structures being created or relocated.

## New Shared Structures

### AuthorizationPolicyExtensions (ServiceDefaults)

**Location**: `src/Common/Sorcha.ServiceDefaults/AuthorizationPolicyExtensions.cs`
**Namespace**: `Microsoft.Extensions.Hosting`

Provides 6 shared authorization policies:

| Policy Name | Claim Logic | Used By |
|------------|-------------|---------|
| RequireAuthenticated | `RequireAuthenticatedUser()` | All 6 services |
| RequireService | `TokenType == TokenTypeService` | All 6 services |
| RequireOrganizationMember | `OrgId` claim present and non-empty | All 6 services |
| RequireDelegatedAuthority | `TokenType == Service` AND `DelegatedUserId` present | 5 services (not Peer) |
| RequireAdministrator | `RequireRole("Administrator")` | 3 services (Blueprint, Validator, Tenant) |
| CanWriteDockets | `TokenType == TokenTypeService` | 2 services (Register, Validator) |

**Extension Method**: `AddSorchaAuthorizationPolicies(this TBuilder builder)` where `TBuilder : IHostApplicationBuilder`

### OpenApiExtensions (ServiceDefaults)

**Location**: `src/Common/Sorcha.ServiceDefaults/OpenApiExtensions.cs`
**Namespace**: `Microsoft.Extensions.Hosting`

| Method | Parameters | Purpose |
|--------|-----------|---------|
| `AddSorchaOpenApi` | `string title, string description` | Registers OpenAPI with standard document transformer (contact, license, version) |
| `MapSorchaOpenApiUi` | `string title, ScalarTheme theme = Purple` | Maps OpenAPI + Scalar UI in development |

### CorsExtensions (ServiceDefaults)

**Location**: `src/Common/Sorcha.ServiceDefaults/CorsExtensions.cs`
**Namespace**: `Microsoft.Extensions.Hosting`

| Method | Purpose |
|--------|---------|
| `AddSorchaCors` | Registers AllowAnyOrigin/Method/Header CORS policy |

### ErrorResponse (MCP Server)

**Location**: `src/Apps/Sorcha.McpServer/Infrastructure/Models/ErrorResponse.cs`
**Namespace**: `Sorcha.McpServer.Infrastructure.Models`

| Field | Type | Purpose |
|-------|------|---------|
| Error | `string?` | Error message from backend service |

Replaces 18 identical `private sealed class ErrorResponse` definitions across MCP tools.

### CreateWalletRequest (ServiceClients)

**Location**: `src/Common/Sorcha.ServiceClients/Wallet/Models/CreateWalletRequest.cs`
**Namespace**: `Sorcha.ServiceClients.Wallet.Models`

| Field | Type | Validation | Notes |
|-------|------|-----------|-------|
| Name | `required string` | `[Required], [StringLength(100, MinimumLength = 1)]` | Wallet display name |
| Algorithm | `required string` | `[Required]` | Cryptographic algorithm |
| WordCount | `int` | `[Range(12, 24)]`, default 12 | Mnemonic word count |
| Passphrase | `string?` | None | Optional passphrase |
| PqcAlgorithm | `string?` | None | Post-quantum algorithm (optional) |
| EnableHybrid | `bool` | None, default false | Hybrid PQC mode (optional) |
| Tags | `Dictionary<string, string>?` | None | Metadata tags |

Consolidates definitions from Wallet Service and UI Core. PQC fields are optional — existing callers that omit them get default behavior.

## Relocated Structures

| Structure | From | To |
|-----------|------|-----|
| Common auth policies | 6 × `AuthenticationExtensions.cs` | 1 × `AuthorizationPolicyExtensions.cs` |
| OpenAPI transformer | 2 × `Program.cs` inline | 1 × `OpenApiExtensions.cs` |
| Scalar configuration | 5 × `Program.cs` inline | 1 × `OpenApiExtensions.cs` |
| CORS configuration | 4 × `Program.cs` inline | 1 × `CorsExtensions.cs` |
| ErrorResponse | 18 × private sealed class | 1 × shared public class |
| CreateWalletRequest | 2 × separate class files | 1 × shared class file |

## No Changes

- No database schema changes
- No new tables, collections, or cache keys
- No API contract changes (endpoints remain identical)
- No configuration schema changes
