# AI Learnings - Common Mistakes and Best Practices

**Version:** 1.0
**Last Updated:** 2026-01-02
**Purpose:** Document lessons learned during AI-assisted development to avoid repeating mistakes

---

## Docker and Container Development

### Learning 1: Always Rebuild Containers After Code Changes
**Date:** 2026-01-02
**Context:** Dashboard tenant count feature implementation

**Mistake:** After modifying service code, tested against local development server (port 5110) instead of Docker container, resulting in 404 errors for new endpoints.

**Root Cause:** Code changes are not automatically reflected in running Docker containers. The container image must be rebuilt to include the new code.

**Correct Process:**
```bash
# After making code changes to a service:
docker-compose build tenant-service api-gateway
docker-compose restart tenant-service api-gateway

# Or rebuild specific service:
docker-compose up -d --build tenant-service
```

**Key Insight:** Always test against the correct environment:
- **Local Development (port 5110):** Uses `dotnet run`, reflects code changes immediately
- **Docker Containers (port 80 via API Gateway):** Uses pre-built images, requires rebuild

**Related Files:**
- `docker-compose.yml`
- Service-specific `Dockerfile`

---

## .NET 10 Package References

### Learning 2: System.Text.Json is Built-In to .NET 10
**Date:** 2026-01-02
**Context:** Adding JSON parsing to Blazor WebAssembly Index.razor page

**Mistake:** Considered adding `System.Text.Json` as a NuGet package reference.

**Root Cause:** `System.Text.Json` is part of the .NET 10 runtime and doesn't need to be explicitly referenced.

**Correct Approach:**
```csharp
// Just add the using statement
@using System.Text.Json

// No package reference needed in .csproj
```

**Key Insight:** .NET 10 includes many commonly-used packages in the base class library (BCL):
- `System.Text.Json` - JSON serialization/deserialization
- `System.Net.Http` - HTTP client
- `System.Threading.Tasks` - Async/await support
- `System.Linq` - LINQ queries

**When to Add Package References:**
- Third-party libraries (MudBlazor, FluentAssertions, etc.)
- Microsoft extensions not in BCL (Microsoft.EntityFrameworkCore, etc.)
- .NET Aspire components

**Related Files:**
- `src/Apps/Sorcha.Admin/Sorcha.Admin.csproj`
- `src/Apps/Sorcha.Admin/Pages/Index.razor`

---

## API Design Patterns

### Learning 3: Separate Public Stats Endpoints from Authenticated List Endpoints
**Date:** 2026-01-02
**Context:** Dashboard statistics need tenant count without authentication

**Pattern:** Create dedicated stats/summary endpoints for dashboard widgets instead of reusing authenticated list endpoints.

**Example:**
```csharp
// ❌ Bad: Reusing authenticated endpoint requires token management
group.MapGet("/", ListOrganizations)
    .RequireAuthorization("admin");

// ✅ Good: Separate public stats endpoint for dashboard
group.MapGet("/stats", GetOrganizationStats)
    .AllowAnonymous()
    .Produces<OrganizationStatsResponse>();

public record OrganizationStatsResponse
{
    public int TotalOrganizations { get; init; }
    public int TotalUsers { get; init; }
}
```

**Benefits:**
- No authentication complexity for simple dashboard widgets
- Better performance (no need to fetch full entity lists)
- Clear separation of concerns
- Easier to cache aggregate statistics

**Related Files:**
- `src/Services/Sorcha.Tenant.Service/Endpoints/OrganizationEndpoints.cs`
- `src/Services/Sorcha.ApiGateway/Services/DashboardStatisticsService.cs`

---

## Frontend Data Fetching

### Learning 4: Blazor WASM Async Data Loading Pattern
**Date:** 2026-01-02
**Context:** Loading dashboard statistics in Index.razor

**Pattern:** Use `OnInitializedAsync` lifecycle method with proper error handling and loading state.

**Example:**
```csharp
@code {
    private int totalTenants = 0;
    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadDashboardStatisticsAsync();
    }

    private async Task LoadDashboardStatisticsAsync()
    {
        try
        {
            isLoading = true;
            var response = await Http.GetAsync("/api/dashboard");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);

                if (doc.RootElement.TryGetProperty("totalTenants", out var element))
                {
                    totalTenants = element.GetInt32();
                }
            }
        }
        catch (Exception ex)
        {
            EventLog.LogError("Error loading statistics", ex.Message);
        }
        finally
        {
            isLoading = false;
        }
    }
}
```

**Key Points:**
- Always set loading state to prevent race conditions
- Use try-finally to ensure loading state is cleared
- Use `TryGetProperty` for safe JSON parsing
- Log errors to help debugging

**Related Files:**
- `src/Apps/Sorcha.Admin/Pages/Index.razor`

---

## Testing and Verification

### Learning 5: Test the Full Request Path in Distributed Systems
**Date:** 2026-01-02
**Context:** Verifying dashboard statistics flow

**Verification Steps:**
1. **Database Level:** Verify data exists
   ```bash
   docker exec -it sorcha-postgres psql -U sorcha_user -d sorcha_tenant -c "SELECT COUNT(*) FROM organizations WHERE status = 1;"
   ```

2. **Service Endpoint:** Test direct service endpoint
   ```bash
   curl http://localhost/api/organizations/stats
   ```

3. **Aggregation Layer:** Test dashboard API
   ```bash
   curl http://localhost/api/dashboard
   ```

4. **Frontend:** Verify UI displays correct data

**Key Insight:** In microservices architecture, test each layer independently to isolate issues:
- Database → Service → API Gateway → Frontend
- Each layer can fail independently

**Common Issues:**
- ✅ Database has data, but service endpoint returns 0 → Repository query issue
- ✅ Service endpoint works, but dashboard API returns 0 → Aggregation logic issue
- ✅ Dashboard API works, but frontend shows wrong value → Frontend parsing issue
- ✅ Endpoint returns 404 → Service not deployed or routing issue

---

## Quick Reference Checklist

**After Making Code Changes to a Dockerized Service:**
- [ ] Rebuild the Docker image: `docker-compose build <service-name>`
- [ ] Restart the container: `docker-compose restart <service-name>`
- [ ] Test the endpoint through API Gateway (correct port)
- [ ] Verify data flows through all layers
- [ ] Check logs for errors: `docker logs <container-name>`

**When Adding JSON Parsing in .NET 10:**
- [ ] Use `@using System.Text.Json` (no package needed)
- [ ] Use `JsonDocument.Parse()` for read-only parsing
- [ ] Use `TryGetProperty()` for safe property access
- [ ] Dispose of `JsonDocument` with `using` statement

**When Creating Dashboard Statistics:**
- [ ] Create dedicated stats endpoint (don't reuse list endpoints)
- [ ] Mark stats endpoint as `AllowAnonymous()` if needed
- [ ] Return aggregate data only (counts, not full entities)
- [ ] Add proper error handling and logging
- [ ] Test full data flow: DB → Service → Gateway → Frontend

---

## Contributing to This Document

When you encounter a new learning or mistake:
1. Add a new learning section with date and context
2. Explain the mistake and root cause
3. Provide the correct approach with code examples
4. Add to quick reference checklist if applicable
5. Update version and last updated date

**Document Maintenance:**
- Review quarterly for outdated learnings
- Archive lessons that are no longer relevant
- Update examples to match current code patterns
