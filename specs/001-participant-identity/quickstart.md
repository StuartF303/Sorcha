# Quickstart: Participant Identity Registry

**Feature**: 001-participant-identity
**Date**: 2026-01-24

## Overview

This guide covers implementing the Participant Identity Registry feature, which bridges Tenant Service users with Blueprint workflow participants and their Wallet signing keys.

## Prerequisites

- .NET 10 SDK installed
- Docker Desktop running
- Sorcha development environment set up (`docker-compose up -d`)
- Familiarity with Tenant Service codebase

## Implementation Order

### Phase 1: Data Layer (Backend)

1. **Add Domain Entities** to Tenant Service
   ```
   src/Services/Sorcha.Tenant.Service/Models/
   ├── ParticipantIdentity.cs
   ├── LinkedWalletAddress.cs
   └── WalletLinkChallenge.cs
   ```

2. **Add Enums** to Common Models
   ```
   src/Common/Sorcha.Tenant.Models/
   ├── ParticipantIdentityStatus.cs
   ├── WalletLinkStatus.cs
   └── ChallengeStatus.cs
   ```

3. **Update TenantDbContext**
   - Add DbSet properties for new entities
   - Configure entity mappings in OnModelCreating
   - Add public schema configuration for LinkedWalletAddress

4. **Create Migration**
   ```bash
   cd src/Services/Sorcha.Tenant.Service
   dotnet ef migrations add AddParticipantIdentity
   ```

### Phase 2: Repository Layer

1. **Define Repository Interface**
   ```csharp
   // IParticipantRepository.cs
   public interface IParticipantRepository
   {
       Task<ParticipantIdentity?> GetByIdAsync(Guid id, CancellationToken ct);
       Task<ParticipantIdentity?> GetByUserAndOrgAsync(Guid userId, Guid orgId, CancellationToken ct);
       Task<PagedResult<ParticipantIdentity>> SearchAsync(ParticipantSearchCriteria criteria, CancellationToken ct);
       Task<ParticipantIdentity> CreateAsync(ParticipantIdentity participant, CancellationToken ct);
       Task<ParticipantIdentity> UpdateAsync(ParticipantIdentity participant, CancellationToken ct);
   }
   ```

2. **Implement Repository**
   - Follow existing `IdentityRepository` pattern
   - Include full-text search using PostgreSQL tsvector
   - Support multi-tenant schema isolation

### Phase 3: Service Layer

1. **Define Service Interface**
   ```csharp
   // IParticipantService.cs
   public interface IParticipantService
   {
       Task<ParticipantResponse> RegisterAsync(Guid orgId, CreateParticipantRequest request, CancellationToken ct);
       Task<ParticipantResponse> SelfRegisterAsync(Guid orgId, CancellationToken ct);
       Task<ParticipantDetailResponse> GetByIdAsync(Guid orgId, Guid participantId, CancellationToken ct);
       Task<PagedResult<ParticipantResponse>> ListAsync(Guid orgId, ParticipantListRequest request, CancellationToken ct);
       Task<ParticipantResponse> UpdateAsync(Guid orgId, Guid participantId, UpdateParticipantRequest request, CancellationToken ct);
       Task DeactivateAsync(Guid orgId, Guid participantId, CancellationToken ct);
   }
   ```

2. **Define Wallet Verification Service**
   ```csharp
   // IWalletVerificationService.cs
   public interface IWalletVerificationService
   {
       Task<WalletLinkChallengeResponse> InitiateLinkAsync(Guid participantId, string walletAddress, CancellationToken ct);
       Task<LinkedWalletAddressResponse> VerifyLinkAsync(Guid challengeId, string signature, string publicKey, CancellationToken ct);
       Task RevokeLinkAsync(Guid linkId, CancellationToken ct);
   }
   ```

3. **Implement Services**
   - Inject `IWalletServiceClient` for signature verification
   - Inject `ITenantProvider` for organization context
   - Add audit logging for all mutations

### Phase 4: API Endpoints

1. **Create ParticipantEndpoints.cs**
   ```csharp
   public static class ParticipantEndpoints
   {
       public static IEndpointRouteBuilder MapParticipantEndpoints(this IEndpointRouteBuilder app)
       {
           var group = app.MapGroup("/api/v1/organizations/{organizationId}/participants")
               .WithTags("Participants")
               .RequireAuthorization();

           group.MapPost("/", CreateParticipant)
               .WithName("CreateParticipant")
               .WithSummary("Register a user as a participant");

           // ... additional endpoints

           return app;
       }
   }
   ```

2. **Register Endpoints in Program.cs**
   ```csharp
   app.MapParticipantEndpoints();
   ```

### Phase 5: Service Client

1. **Add to Sorcha.ServiceClients**
   ```csharp
   // IParticipantServiceClient.cs
   public interface IParticipantServiceClient
   {
       Task<ParticipantResponse?> GetByIdAsync(Guid participantId, CancellationToken ct);
       Task<ParticipantResponse?> GetByWalletAddressAsync(string address, CancellationToken ct);
       Task<bool> ValidateSigningCapabilityAsync(Guid participantId, CancellationToken ct);
   }
   ```

2. **Register in ServiceCollectionExtensions**
   ```csharp
   services.AddHttpClient<IParticipantServiceClient, ParticipantServiceClient>(client =>
   {
       client.BaseAddress = new Uri(configuration["Services:Tenant"] ?? "http://tenant-service");
   });
   ```

### Phase 6: UI Components

1. **Create Blazor Components**
   ```
   src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Participants/
   ├── ParticipantList.razor
   ├── ParticipantForm.razor
   ├── WalletLinkDialog.razor
   └── ParticipantSearch.razor
   ```

2. **Create UI Service Layer**
   ```csharp
   // ParticipantApiService.cs
   public class ParticipantApiService
   {
       private readonly HttpClient _http;

       public async Task<ParticipantListResponse> ListAsync(Guid orgId, int page = 1);
       public async Task<ParticipantResponse> CreateAsync(Guid orgId, CreateParticipantRequest request);
       // ... additional methods
   }
   ```

3. **Create Pages**
   ```
   src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Participants/
   ├── Index.razor         # /participants
   ├── Create.razor        # /participants/create
   ├── Details.razor       # /participants/{id}
   └── MyProfile.razor     # /my-participant-profile
   ```

### Phase 7: Testing

1. **Unit Tests**
   ```
   tests/Sorcha.Tenant.Service.Tests/
   ├── Services/ParticipantServiceTests.cs
   └── Repositories/ParticipantRepositoryTests.cs
   ```

2. **Integration Tests**
   ```
   tests/Sorcha.Tenant.Service.IntegrationTests/
   └── ParticipantEndpointsTests.cs
   ```

3. **Run Tests**
   ```bash
   dotnet test --filter "FullyQualifiedName~Participant"
   ```

## Key Patterns to Follow

### Endpoint Pattern (from WalletEndpoints)
```csharp
private static async Task<Results<Ok<ParticipantResponse>, ValidationProblem, NotFound>> GetParticipant(
    [FromRoute] Guid organizationId,
    [FromRoute] Guid participantId,
    IParticipantService service,
    ILogger<ParticipantEndpoints> logger,
    CancellationToken cancellationToken)
{
    var result = await service.GetByIdAsync(organizationId, participantId, cancellationToken);
    return result is null
        ? TypedResults.NotFound()
        : TypedResults.Ok(result);
}
```

### Repository Pattern (from IdentityRepository)
```csharp
public async Task<ParticipantIdentity?> GetByIdAsync(Guid id, CancellationToken ct)
{
    return await _context.ParticipantIdentities
        .AsNoTracking()
        .FirstOrDefaultAsync(p => p.Id == id, ct);
}
```

### Audit Logging Pattern
```csharp
private async Task LogAuditAsync(
    Guid participantId,
    string action,
    object? oldValues,
    object? newValues,
    CancellationToken ct)
{
    var entry = new ParticipantAuditEntry
    {
        Id = Guid.NewGuid(),
        ParticipantId = participantId,
        Action = action,
        ActorId = _tenantProvider.GetCurrentUserId(),
        ActorType = "User",
        Timestamp = DateTimeOffset.UtcNow,
        OldValues = oldValues != null ? JsonSerializer.SerializeToDocument(oldValues) : null,
        NewValues = newValues != null ? JsonSerializer.SerializeToDocument(newValues) : null,
        IpAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
    };

    _context.ParticipantAuditEntries.Add(entry);
    await _context.SaveChangesAsync(ct);
}
```

## Verification Steps

After implementation, verify:

1. **API Health**
   ```bash
   curl http://localhost:5110/health
   ```

2. **Create Participant**
   ```bash
   curl -X POST http://localhost:5110/api/v1/organizations/{orgId}/participants \
     -H "Authorization: Bearer {token}" \
     -H "Content-Type: application/json" \
     -d '{"userId": "...", "displayName": "Test Participant"}'
   ```

3. **Initiate Wallet Link**
   ```bash
   curl -X POST http://localhost:5110/api/v1/organizations/{orgId}/participants/{id}/wallet-links \
     -H "Authorization: Bearer {token}" \
     -H "Content-Type: application/json" \
     -d '{"walletAddress": "..."}'
   ```

4. **Search Participants**
   ```bash
   curl -X POST http://localhost:5110/api/v1/participants/search \
     -H "Authorization: Bearer {token}" \
     -H "Content-Type: application/json" \
     -d '{"query": "test"}'
   ```

## Common Issues

### Issue: Schema not created for new org
**Solution**: Ensure migration runs for new org schemas via TenantDbContext dynamic schema support.

### Issue: Wallet address uniqueness conflict
**Solution**: Check public.linked_wallet_addresses for existing active link. Address must be revoked before re-linking.

### Issue: Challenge expired
**Solution**: Challenges expire after 5 minutes. Initiate a new challenge.

## Next Steps

After implementation:
1. Run full test suite: `dotnet test`
2. Update API documentation in Scalar
3. Add navigation links in UI
4. Update CLAUDE.md with new endpoints
