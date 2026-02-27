# Quickstart: 044 Codebase Consolidation

## Implementation Order

Work items are independent and can be parallelized. Recommended execution order for safety:

1. **License headers** (P3, zero risk) — automatable script, no code logic changes
2. **Remove ReadLine package** (P3, trivial) — one-line .csproj edit
3. **MCP ErrorResponse extraction** (P3, low risk) — isolated to McpServer project
4. **CLI endpoint fixes** (P2, low risk) — path corrections and dead code removal
5. **CORS consolidation** (P2, low risk) — extract to ServiceDefaults
6. **OpenAPI/Scalar consolidation** (P2, medium risk) — extract to ServiceDefaults, update 7 Program.cs
7. **Authorization policy consolidation** (P1, medium risk) — security-critical, test thoroughly
8. **Pipeline bug fixes** (P2, low risk) — Tenant double rate limiter, Wallet middleware ordering
9. **CreateWalletRequest DTO merge** (P3, medium risk) — cross-project reference changes
10. **SignalR naming exception** (P3, trivial) — document in CLAUDE.md

## Verification

After each work item: `dotnet build` + `dotnet test` for affected projects.
After all items: full `dotnet build && dotnet test` across entire solution.

## Key Files to Modify

### New Files
- `src/Common/Sorcha.ServiceDefaults/AuthorizationPolicyExtensions.cs`
- `src/Common/Sorcha.ServiceDefaults/OpenApiExtensions.cs`
- `src/Common/Sorcha.ServiceDefaults/CorsExtensions.cs`
- `src/Apps/Sorcha.McpServer/Infrastructure/Models/ErrorResponse.cs`
- `src/Common/Sorcha.ServiceClients/Wallet/Models/CreateWalletRequest.cs`

### Modified Files (per work item)
- 6 × `AuthenticationExtensions.cs` (remove shared policies, keep service-specific)
- 7 × `Program.cs` (replace inline OpenAPI/CORS/Scalar with shared calls)
- 18 × MCP tool files (replace private ErrorResponse with shared import)
- ~168 × `.cs` files (add license headers)
- `Sorcha.Cli.csproj` (remove ReadLine)
- `ICredentialServiceClient.cs` (fix paths)
- `IAdminServiceClient.cs` (fix alert path, remove schema endpoints)
- `AdminCommands.cs` (remove schema commands)
- `CLAUDE.md` (add SignalR naming exception note)
