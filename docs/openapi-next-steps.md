# OpenAPI Documentation - Next Steps

**Status:** Ready for Future Sprint
**Dependencies:** Phase 1 Complete ✅
**Priority:** P1 (Post-MVD)
**Estimated Effort:** 3-5 days

---

## Phase 2: Standards & Examples

### Task List

#### OA-2.1: Add OpenAPI Security Schemes
**Priority:** P0 (Blocks SDK generation)
**Effort:** 1 day

Add security scheme definitions to all services:

```csharp
options.AddDocumentTransformer((document, context, cancellationToken) =>
{
    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
    {
        ["BearerAuth"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "JWT Bearer token obtained from Tenant Service"
        }
    };

    // Apply to all operations
    document.Paths.Values.SelectMany(p => p.Operations)
        .Where(op => !op.Value.Tags.Contains("Public"))
        .ToList()
        .ForEach(op => op.Value.Security = new List<OpenApiSecurityRequirement>
        {
            new OpenApiSecurityRequirement
            {
                [document.Components.SecuritySchemes["BearerAuth"]] = new List<string>()
            }
        });

    return Task.CompletedTask;
});
```

**Files to modify:**
- `src/Services/Sorcha.Tenant.Service/Program.cs`
- `src/Services/Sorcha.Wallet.Service/Program.cs`
- `src/Services/Sorcha.Register.Service/Program.cs`
- `src/Services/Sorcha.Blueprint.Service/Program.cs`
- `src/Services/Sorcha.Peer.Service/Program.cs`

**Acceptance Criteria:**
- [ ] Security scheme defined in all services
- [ ] Protected endpoints require BearerAuth
- [ ] Public endpoints (auth token) excluded
- [ ] Scalar UI shows "Authorize" button

---

#### OA-2.2: Add Examples to Critical Endpoints
**Priority:** P1
**Effort:** 2 days

Add comprehensive examples using `.WithOpenApi()` enhancer:

**Critical Endpoints:**
1. `/api/service-auth/token` - Authentication
2. `/api/wallets` POST - Wallet creation
3. `/api/wallets/{id}/sign` POST - Transaction signing
4. `/api/registers/{id}/transactions` POST - Transaction submission
5. `/api/blueprints/{id}/execute` POST - Blueprint execution

**Example Implementation:**

```csharp
app.MapPost("/api/wallets", async (CreateWalletRequest request) =>
{
    // ... implementation
})
.WithOpenApi(operation =>
{
    operation.Summary = "Create a new HD wallet";
    operation.Description = "Creates a new hierarchical deterministic wallet...";

    // Add success example
    operation.Responses["201"].Content["application/json"].Example = new OpenApiString(@"{
      ""walletId"": ""wallet-abc123"",
      ""publicAddress"": ""0x1234567890abcdef..."",
      ""algorithm"": ""ED25519"",
      ""mnemonicWords"": [""word1"", ""word2"", ..., ""word12""],
      ""createdAt"": ""2025-12-11T10:30:00Z""
    }");

    // Add error examples
    operation.Responses["400"].Content["application/json"].Example = new OpenApiString(@"{
      ""type"": ""validation-error"",
      ""title"": ""Validation Error"",
      ""status"": 400,
      ""detail"": ""Invalid wallet algorithm"",
      ""errors"": {
        ""algorithm"": [""Must be ED25519, NISTP256, or RSA4096""]
      }
    }");

    return operation;
});
```

**Files to modify:**
- `src/Services/Sorcha.Tenant.Service/Endpoints/ServiceAuthEndpoints.cs`
- `src/Services/Sorcha.Wallet.Service/Endpoints/WalletEndpoints.cs`
- `src/Services/Sorcha.Register.Service/Program.cs` (transaction endpoints)
- `src/Services/Sorcha.Blueprint.Service/Endpoints/*.cs`

**Acceptance Criteria:**
- [ ] All 5 critical endpoints have success examples
- [ ] All 5 critical endpoints have error examples (400, 401, 404)
- [ ] Examples use realistic data from test dataset
- [ ] Examples visible in Scalar UI

---

#### OA-2.3: Implement RFC 7807 Error Responses
**Priority:** P1
**Effort:** 1 day

Update all endpoints to return `ProblemDetails`:

```csharp
app.MapPost("/api/wallets", async (CreateWalletRequest request) =>
{
    if (!IsValid(request))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["name"] = ["The name field is required"],
            ["algorithm"] = ["Must be ED25519, NISTP256, or RSA4096"]
        });
    }

    try
    {
        var wallet = await walletService.CreateWalletAsync(request);
        return Results.Created($"/api/wallets/{wallet.WalletId}", wallet);
    }
    catch (DuplicateWalletException ex)
    {
        return Results.Problem(
            title: "Duplicate Wallet",
            detail: ex.Message,
            statusCode: 409
        );
    }
})
.ProducesValidationProblem()
.ProducesProblem(401)
.ProducesProblem(404)
.ProducesProblem(409)
.ProducesProblem(500);
```

**Files to modify:**
- All endpoint files across all services

**Acceptance Criteria:**
- [ ] All endpoints return `ProblemDetails` for errors
- [ ] Validation errors use `Results.ValidationProblem()`
- [ ] Business errors use `Results.Problem()` with appropriate status
- [ ] All responses documented with `.ProducesProblem()`

---

#### OA-2.4: Add cURL Examples to Scalar UI
**Priority:** P2
**Effort:** 0.5 day

Update Scalar configuration to show cURL examples:

```csharp
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("Service Name")
        .WithTheme(ScalarTheme.Purple)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .WithHttpClient(ScalarTarget.Shell, ScalarClient.Curl);
});
```

**Files to modify:**
- `src/Services/Sorcha.Tenant.Service/Program.cs`
- `src/Services/Sorcha.Wallet.Service/Program.cs`
- `src/Services/Sorcha.Register.Service/Program.cs`
- `src/Services/Sorcha.Blueprint.Service/Program.cs`
- `src/Services/Sorcha.Peer.Service/Program.cs`
- `src/Services/Sorcha.ApiGateway/Program.cs`

**Acceptance Criteria:**
- [ ] Scalar UI shows both C# and cURL examples
- [ ] cURL examples include authentication header
- [ ] Examples are copy-paste ready

---

#### OA-2.5: Create Error Documentation
**Priority:** P2
**Effort:** 1 day

Create comprehensive error documentation:

**File:** `docs/api-error-reference.md`

**Contents:**
- Error response format (RFC 7807)
- Common error types and status codes
- Error handling best practices
- Example error responses for each service
- Troubleshooting guide

**Acceptance Criteria:**
- [ ] Error reference document created
- [ ] Linked from aggregated OpenAPI description
- [ ] Examples for each error type
- [ ] Troubleshooting section with common issues

---

## Phase 3: Polish & Validation

### Task List

#### OA-3.1: Validate OpenAPI Specs
**Priority:** P1
**Effort:** 0.5 day

Add OpenAPI validation to CI/CD:

```yaml
# .github/workflows/openapi-validation.yml
name: OpenAPI Validation
on: [pull_request]
jobs:
  validate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Validate OpenAPI Specs
        uses: openapi-generators/openapi-validator-action@v1
        with:
          specs: |
            src/Services/*/openapi.json
```

**Tools:**
- Spectral (OpenAPI linter)
- openapi-validator
- Redocly CLI

**Acceptance Criteria:**
- [ ] CI/CD validates all OpenAPI specs
- [ ] No validation errors
- [ ] No breaking changes detected

---

#### OA-3.2: Add Comprehensive Endpoint Examples
**Priority:** P2
**Effort:** 2 days

Extend examples to all endpoints (not just critical):

**Coverage Target:**
- Critical endpoints: Success + 4 error examples
- Standard CRUD: Success example
- Query endpoints: Success with pagination/filtering

**Acceptance Criteria:**
- [ ] 80%+ endpoints have examples
- [ ] All examples validated against schema
- [ ] Examples use consistent test dataset

---

#### OA-3.3: Create Migration Guides
**Priority:** P2 (needed when deprecation occurs)
**Effort:** 1 day

Create template for migration guides:

**File:** `docs/migration-guide-template.md`

**Contents:**
- Deprecation announcement template
- Breaking changes checklist
- Code migration examples
- Timeline template
- Communication plan

**Acceptance Criteria:**
- [ ] Template created
- [ ] Example migration guide for sample deprecation
- [ ] Communication plan defined

---

#### OA-3.4: Generate SDK Samples
**Priority:** P2
**Effort:** 1 day

Create SDK generation scripts and samples:

**Languages:**
- C# (NSwag)
- TypeScript (OpenAPI Generator)
- Python (OpenAPI Generator)

**Scripts:**
```bash
# scripts/generate-sdk.sh
nswag openapi2csclient /input:openapi.json /output:SorchaClient.cs
openapi-generator-cli generate -i openapi.json -g typescript-axios -o ./sdk/typescript
openapi-generator-cli generate -i openapi.json -g python -o ./sdk/python
```

**Acceptance Criteria:**
- [ ] SDK generation scripts created
- [ ] Sample SDKs generated and tested
- [ ] SDK usage examples documented
- [ ] CI/CD validates SDK generation

---

#### OA-3.5: Create Postman Collection
**Priority:** P2
**Effort:** 0.5 day

Export OpenAPI specs to Postman format:

**Tools:**
- openapi-to-postman converter
- Manual environment setup

**Collection Structure:**
- Folder per service
- Pre-request scripts for auth
- Environment variables
- Example requests

**Acceptance Criteria:**
- [ ] Postman collection exported
- [ ] Environment variables configured
- [ ] Authentication flow automated
- [ ] Collection published to docs

---

## Phase 4: Ongoing Improvements

### Task List

#### OA-4.1: Developer Feedback Integration
**Priority:** P3
**Effort:** Ongoing

**Process:**
- Collect feedback via GitHub issues
- Survey developers quarterly
- Monitor documentation usage metrics
- Iterate based on common questions

**Acceptance Criteria:**
- [ ] Feedback mechanism established
- [ ] Quarterly review process
- [ ] Metrics dashboard created

---

#### OA-4.2: Tutorial Creation
**Priority:** P3
**Effort:** 1 day per tutorial

**Tutorials to Create:**
1. Getting Started with Sorcha (30 min)
2. Building a Document Timestamping App (1 hour)
3. Multi-Party Workflow Implementation (2 hours)
4. Advanced Querying with OData (1 hour)
5. Security Best Practices (1 hour)

**Format:**
- Step-by-step markdown
- Code samples
- Screenshots
- Video walkthrough

**Acceptance Criteria:**
- [ ] 5 tutorials created
- [ ] All tutorials tested by external developer
- [ ] Video walkthroughs recorded

---

#### OA-4.3: SDK Maintenance
**Priority:** P3
**Effort:** Ongoing

**Process:**
- Regenerate SDKs on OpenAPI changes
- Version SDKs with platform versions
- Publish to package registries (NuGet, npm, PyPI)
- Maintain SDK documentation

**Acceptance Criteria:**
- [ ] SDK release process documented
- [ ] CI/CD publishes SDKs automatically
- [ ] SDK versioning strategy defined

---

## Quick Reference

### Completed (Phase 1)
- ✅ Scalar UI on all services
- ✅ API Gateway aggregation (5 services)
- ✅ Service introductions
- ✅ Platform overview
- ✅ Workflow documentation
- ✅ Decision documentation

### Next Sprint (Phase 2)
- ⏳ OpenAPI security schemes
- ⏳ Critical endpoint examples
- ⏳ RFC 7807 error responses
- ⏳ cURL examples
- ⏳ Error documentation

### Future (Phase 3)
- ⏳ OpenAPI validation
- ⏳ Comprehensive examples
- ⏳ Migration guides
- ⏳ SDK samples
- ⏳ Postman collection

### Ongoing (Phase 4)
- ⏳ Developer feedback
- ⏳ Tutorial creation
- ⏳ SDK maintenance

---

## Dependencies

**Before Starting Phase 2:**
- [ ] Phase 1 complete ✅
- [ ] Decision document reviewed
- [ ] Test dataset created (optional)

**Before Starting Phase 3:**
- [ ] Phase 2 complete
- [ ] Error responses standardized
- [ ] Examples validated

**Before Starting Phase 4:**
- [ ] Phase 3 complete
- [ ] SDK generation tested
- [ ] Documentation published

---

## Effort Summary

| Phase | Tasks | Estimated Effort | Priority |
|-------|-------|-----------------|----------|
| Phase 1 | 6 | 5 days | P0 | ✅ COMPLETE |
| Phase 2 | 5 | 5 days | P1 | Next Sprint |
| Phase 3 | 5 | 5 days | P2 | Sprint +2 |
| Phase 4 | 3 | Ongoing | P3 | Continuous |

**Total Estimated Effort:** 15 days + ongoing maintenance

---

**Last Updated:** 2025-12-11
**Next Review:** After MVD release
