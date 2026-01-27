# Sorcha UI Workflows Reference

## Contents
- Page Development Workflow
- Adding a New Feature Area
- Running and Debugging Tests
- Rebuilding Docker After Changes
- Page Priority Order

---

## Page Development Workflow

### Complete Workflow for One Page

When told to work on a Sorcha.UI page, execute these steps:

#### 1. Read the Existing Page

```bash
# Find the current page implementation
cat src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/{PageName}.razor
```

Check if it's a template stub (hardcoded values, no API calls) or a real implementation.

#### 2. Identify the Backend API

Check what API endpoints the page should call:

```bash
# Find available endpoints in the relevant service
grep -r "MapGet\|MapPost\|MapPut\|MapDelete" src/Services/Sorcha.{Service}.Service/Endpoints/
```

Check the API Gateway routes:
```bash
cat src/Services/Sorcha.ApiGateway/appsettings.json
```

#### 3. Create the Page Object

Create `tests/Sorcha.UI.E2E.Tests/PageObjects/{PageName}Page.cs`:
- Define locators using `data-testid` selectors
- Add navigation method
- Add state query methods
- Add action methods

See `patterns.md` for the standard page object template.

#### 4. Write Failing Tests

Create `tests/Sorcha.UI.E2E.Tests/Docker/{Feature}Tests.cs`:
- Extend `AuthenticatedDockerTestBase` (or `DockerTestBase` for public pages)
- Add appropriate `[Category]` attributes
- Write smoke, structure, and behavior tests
- Tests should fail because the page is still a template

#### 5. Implement the Blazor Page

Edit `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/{Page}.razor`:
- Use MudBlazor components
- Add `data-testid` attributes to key elements
- Follow the Loading -> Data -> Empty State pattern
- Call backend APIs via HttpClient
- Handle errors gracefully

#### 6. Build and Test

```bash
# Build the test project
dotnet build tests/Sorcha.UI.E2E.Tests

# Run tests for your feature
dotnet test tests/Sorcha.UI.E2E.Tests --filter "Category=YourFeature"
```

#### 7. Fix Failures

If tests fail:
- Check screenshots in `bin/Debug/net10.0/screenshots/`
- Check console error output in test results
- Check network failure output in test results
- Fix and re-run

---

## Adding a New Feature Area

### Checklist for a New Feature

1. **Add route to TestConstants.cs**:
   ```csharp
   // In TestConstants.AuthenticatedRoutes
   public const string NewFeature = $"{AppBase}/new-feature";
   ```

2. **Create Page Object**: `PageObjects/NewFeaturePage.cs`

3. **Create Test Class**: `Docker/NewFeatureTests.cs`
   - Add `[Category("NewFeature")]`
   - Add smoke test for the route

4. **Add to NavigationTests smoke test** (if not already covered):
   The parameterized test `Page_LoadsWithoutCriticalErrors` in `NavigationTests.cs`
   covers all routes listed in `TestConstants.AuthenticatedRoutes`.

5. **Add category to SKILL.md** category table (this file's parent).

6. **Build and verify**: `dotnet build tests/Sorcha.UI.E2E.Tests`

---

## Running and Debugging Tests

### Run Commands

```bash
# All Docker tests
dotnet test tests/Sorcha.UI.E2E.Tests --filter "Category=Docker"

# Smoke tests only (fast)
dotnet test tests/Sorcha.UI.E2E.Tests --filter "Category=Smoke"

# Authenticated tests (requires Docker running)
dotnet test tests/Sorcha.UI.E2E.Tests --filter "Category=Authenticated"

# Single feature
dotnet test tests/Sorcha.UI.E2E.Tests --filter "Category=Wallets"

# Single test by name
dotnet test tests/Sorcha.UI.E2E.Tests --filter "FullyQualifiedName~WalletList_LoadsWithoutErrors"

# With detailed output
dotnet test tests/Sorcha.UI.E2E.Tests --filter "Category=Docker" --logger "console;verbosity=detailed"
```

### Headed Mode (See the Browser)

```powershell
# PowerShell
$env:HEADED="1"
dotnet test tests/Sorcha.UI.E2E.Tests --filter "Category=Dashboard"
```

### Playwright Inspector (Step Through)

```powershell
$env:PWDEBUG="1"
dotnet test tests/Sorcha.UI.E2E.Tests --filter "FullyQualifiedName~Dashboard_LoadsSuccessfully"
```

### Debugging Auth Issues

If authenticated tests fail with "redirected to login":

1. Check Docker is running: `docker-compose ps`
2. Check Tenant Service is healthy: `curl http://localhost:80/api/health`
3. Check login works manually: open `http://localhost:5400/app/auth/login`
4. Auth state file is at `%TEMP%/sorcha-e2e-auth-state-*.json`
5. Delete temp auth files and re-run to force fresh login

---

## Rebuilding Docker After UI Changes

When you modify a Blazor page, the Docker container needs rebuilding:

```bash
# Rebuild only the UI service
docker-compose build sorcha-ui-web && docker-compose up -d --force-recreate sorcha-ui-web

# Then run tests
dotnet test tests/Sorcha.UI.E2E.Tests --filter "Category=Docker"
```

For rapid iteration without Docker rebuild, use Aspire mode:
```bash
dotnet run --project src/Apps/Sorcha.AppHost
# Then run tests against Aspire URLs (different ports)
```

---

## Page Priority Order

When implementing pages, follow this dependency order:

| Priority | Page | Route | Depends On | Service |
|----------|------|-------|------------|---------|
| 1 | Dashboard | `/dashboard` | Auth only | Blueprint, Wallet, Register |
| 2 | Wallet List | `/wallets` | Auth | Wallet Service |
| 3 | Create Wallet | `/wallets/create` | Wallet List | Wallet Service |
| 4 | My Wallet | `/my-wallet` | Wallet List | Wallet Service |
| 5 | Schema Library | `/schemas` | Auth | Blueprint Service |
| 6 | Blueprint List | `/blueprints` | Auth | Blueprint Service |
| 7 | Register List | `/registers` | Auth | Register Service |
| 8 | Register Detail | `/registers/{id}` | Register List | Register Service |
| 9 | Pending Actions | `/my-actions` | Blueprint List | Blueprint Service |
| 10 | My Workflows | `/my-workflows` | Blueprint List | Blueprint Service |
| 11 | My Transactions | `/my-transactions` | Register List | Register Service |
| 12 | Templates | `/templates` | Blueprint List | Blueprint Service |
| 13 | Designer | `/designer` | Schema Library | Blueprint Service |
| 14 | Participants | `/participants` | Auth | Tenant Service |
| 15 | Administration | `/admin` | Auth | All Services |
| 16 | Settings | `/settings` | Auth | Tenant Service |
| 17 | Help | `/help` | None | Static |

### Current Page Status

| Page | State | data-testid | Page Object | Tests |
|------|-------|-------------|-------------|-------|
| Login | Real implementation | Partial | LoginPage.cs | LoginTests.cs |
| Dashboard/Home | Template (hardcoded 0s) | None | DashboardPage.cs | DashboardTests.cs |
| Navigation/Layout | Real implementation | None | NavigationComponent.cs | NavigationTests.cs |
| All other pages | Template stubs | None | Not yet | Not yet |

When you work on a page, update this table in the workflow reference.

---

## File Creation Checklist

For every page you implement, create or update these files:

- [ ] `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/{Page}.razor` - The Blazor page
- [ ] `tests/Sorcha.UI.E2E.Tests/PageObjects/{Page}Page.cs` - Page object model
- [ ] `tests/Sorcha.UI.E2E.Tests/Docker/{Feature}Tests.cs` - Playwright tests
- [ ] `tests/Sorcha.UI.E2E.Tests/Infrastructure/TestConstants.cs` - Add route if new
- [ ] Verify build: `dotnet build tests/Sorcha.UI.E2E.Tests`
- [ ] Run tests: `dotnet test tests/Sorcha.UI.E2E.Tests --filter "Category={Feature}"`
