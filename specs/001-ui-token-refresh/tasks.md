# Tasks: Sorcha.UI Authentication Token Management and Login UX

**Input**: Design documents from `/specs/001-ui-token-refresh/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Unit tests included for security-critical components (UrlValidator, NavigationService). E2E tests included for user acceptance validation.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths included in descriptions

---

## Phase 1: Setup

**Purpose**: Create new files and directory structure for navigation service and utilities

- [x] T001 Create Navigation directory at `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Navigation/`
- [x] T002 Create Utilities directory at `src/Apps/Sorcha.UI/Sorcha.UI.Core/Utilities/`
- [x] T003 [P] Create Navigation test directory at `tests/Sorcha.UI.Core.Tests/Services/Navigation/`
- [x] T004 [P] Create Utilities test directory at `tests/Sorcha.UI.Core.Tests/Utilities/`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core security utility that ALL user stories depend on

**⚠️ CRITICAL**: UrlValidator MUST be complete before NavigationService or Login.razor changes

### Security Utility

- [x] T005 Implement UrlValidator static class in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Utilities/UrlValidator.cs`
  - IsValidReturnUrl(string? url, Uri baseUri) method
  - IsValidReturnUrl(string? url, string baseUri) overload
  - Reject null/empty/whitespace URLs
  - Accept relative paths starting with `/` (not `//`)
  - Reject javascript: and data: schemes
  - Accept same-origin absolute URLs only
  - Include XML documentation

- [x] T006 Write UrlValidator unit tests in `tests/Sorcha.UI.Core.Tests/Utilities/UrlValidatorTests.cs`
  - Test valid relative paths (/dashboard, /app/registers/123)
  - Test invalid relative paths (dashboard, //evil.com)
  - Test valid same-origin absolute URLs
  - Test invalid external URLs
  - Test dangerous schemes (javascript:, data:)
  - Test null/empty/whitespace handling

**Checkpoint**: UrlValidator complete with passing tests - navigation services can now be built

---

## Phase 3: User Story 1 - Seamless Token Refresh (Priority: P1)

**Goal**: Proactive token refresh before expiration to prevent session interruption

**Independent Test**: Login, wait for token to approach 5-minute threshold, verify API requests continue without interruption

**Note**: Token refresh is ALREADY IMPLEMENTED in existing code. This story validates current behavior and adds no new implementation tasks.

### Verification for User Story 1

- [x] T007 [US1] Verify IsNearExpiration threshold in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Authentication/TokenCacheEntry.cs` is 5 minutes
- [x] T008 [US1] Verify GetAccessTokenAsync proactive refresh in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Authentication/AuthenticationService.cs`
- [x] T009 [US1] Verify semaphore serialization in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Http/AuthenticatedHttpMessageHandler.cs`

**Checkpoint**: Token refresh behavior verified - existing implementation satisfies US1 requirements

---

## Phase 4: User Story 2 - Redirect to Login with Return URL (Priority: P1)

**Goal**: Automatic redirect to login page with return URL when tokens cannot be refreshed

**Independent Test**: Invalidate token, attempt authenticated action, verify redirect includes returnUrl parameter, login, verify navigation to original page

### Navigation Service for User Story 2

- [x] T010 [P] [US2] Create INavigationService interface in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Navigation/INavigationService.cs`
  - CurrentUri property
  - RedirectToLoginAsync(string? returnUrl) method
  - NavigateToValidatedUrl(string? url, string defaultDestination) method

- [x] T011 [P] [US2] Implement NavigationService in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Navigation/NavigationService.cs`
  - Inject NavigationManager
  - RedirectToLoginAsync: check if already on login page (prevent loops), build URL with returnUrl parameter
  - NavigateToValidatedUrl: use UrlValidator, fallback to default

- [x] T012 [US2] Write NavigationService unit tests in `tests/Sorcha.UI.Core.Tests/Services/Navigation/NavigationServiceTests.cs`
  - Test redirect includes returnUrl parameter
  - Test redirect loop prevention (already on login page)
  - Test NavigateToValidatedUrl with valid URL
  - Test NavigateToValidatedUrl with invalid URL (uses default)

### Service Registration for User Story 2

- [x] T013 [US2] Register INavigationService in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Extensions/ServiceCollectionExtensions.cs`
  - Add services.AddScoped<INavigationService, NavigationService>() in AddCoreServices method

### HTTP Handler Enhancement for User Story 2

- [x] T014 [US2] Update AuthenticatedHttpMessageHandler in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/Http/AuthenticatedHttpMessageHandler.cs`
  - Add INavigationService dependency to constructor
  - After refresh failure: call RedirectToLoginAsync with current URI as returnUrl

### Login Page Return URL Handling for User Story 2

- [x] T015 [US2] Update Login.razor to read returnUrl parameter in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Login.razor`
  - Add [SupplyParameterFromQuery(Name = "returnUrl")] parameter
  - Inject INavigationService
  - After successful login: call NavigateToValidatedUrl(ReturnUrl, "dashboard")

**Checkpoint**: User Story 2 complete - redirect flow works end-to-end

---

## Phase 5: User Story 3 - Enter Key Submits Login Form (Priority: P2)

**Goal**: Press Enter on password field to submit login form without mouse click

**Independent Test**: Navigate to login, enter credentials, press Enter on password field, verify form submits

### Login Page Keyboard Enhancement for User Story 3

- [x] T016 [US3] Add keyboard handler to Login.razor in `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/Login.razor`
  - Add HandleKeyDown(KeyboardEventArgs e) method
  - Check for Enter key and !_isLoading
  - Call HandleLogin() if conditions met
  - Add @onkeydown="HandleKeyDown" to password input element

**Checkpoint**: User Story 3 complete - Enter key submits login form

---

## Phase 6: E2E Tests & Polish

**Purpose**: End-to-end validation and cross-cutting concerns

### E2E Tests

- [x] T017 [P] Add return URL and keyboard tests to `tests/Sorcha.UI.E2E.Tests/Docker/LoginTests.cs`
  - Test login with returnUrl redirects to correct page
  - Test login without returnUrl redirects to dashboard
  - Test invalid returnUrl (external) redirected to dashboard (security)
  - Test javascript: URL rejected and redirects to dashboard (security)
  - Test Enter key on password field submits login form
  - Test Enter key on username field submits login form

### Documentation

- [x] T018 [P] Update Sorcha.UI.Core README if it exists with new NavigationService documentation
  - Note: No README exists in Sorcha.UI.Core - task N/A
- [x] T019 Run quickstart.md validation checklist to verify all items pass

---

## Dependencies & Execution Order

### Phase Dependencies

```
Phase 1: Setup
    ↓
Phase 2: Foundational (UrlValidator)
    ↓
Phase 3: US1 (Verification only - no blockers)
Phase 4: US2 (NavigationService + Handler + Login)
Phase 5: US3 (Login keyboard)
    ↓
Phase 6: E2E Tests & Polish
```

### User Story Dependencies

| Story | Depends On | Can Parallelize With |
|-------|------------|---------------------|
| US1 (Token Refresh) | Foundational | US2, US3 |
| US2 (Return URL) | Foundational, UrlValidator | US1, US3 |
| US3 (Enter Key) | Foundational | US1, US2 |

### Within Phase 4 (User Story 2)

```
T010 (INavigationService) ─┬─→ T011 (NavigationService) → T012 (Tests)
                           │                                   ↓
                           └─→ T013 (Registration) ────────────┤
                                                                ↓
                           T014 (Handler) ──────────────────────┤
                                                                ↓
                           T015 (Login.razor) ──────────────────┘
```

### Parallel Opportunities

**Phase 1** (all parallel):
```
T001, T002, T003, T004 can all run in parallel
```

**Phase 4** (partial parallel):
```
T010 (interface) and T011 (implementation) can run in parallel
T012 (tests) requires T011 to complete
T013, T014, T015 depend on T010/T011 but can run in parallel with each other
```

**Phase 6** (all parallel):
```
T017, T018, T019 can all run in parallel
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2)

1. Complete Phase 1: Setup directories
2. Complete Phase 2: UrlValidator with tests
3. Complete Phase 3: Verify US1 (token refresh already works)
4. Complete Phase 4: US2 (redirect with return URL)
5. **STOP and VALIDATE**: Test redirect flow manually
6. Deploy if ready

### Full Feature

1. Complete MVP (above)
2. Complete Phase 5: US3 (Enter key login)
3. Complete Phase 6: E2E tests and polish

### Suggested Parallel Execution

```bash
# After Phase 2 complete, launch US2 and US3 in parallel:

# Developer A (or sequential):
Task: T010 (INavigationService interface)
Task: T011 (NavigationService implementation)
Task: T012 (NavigationService tests)

# Developer B (or sequential after T010):
Task: T013 (Service registration)
Task: T014 (Handler enhancement)
Task: T015 (Login.razor return URL)
Task: T016 (Login.razor Enter key)
```

---

## Summary

| Phase | Task Count | Purpose |
|-------|------------|---------|
| Phase 1: Setup | 4 | Directory structure |
| Phase 2: Foundational | 2 | UrlValidator (security) |
| Phase 3: US1 | 3 | Token refresh verification |
| Phase 4: US2 | 6 | Return URL redirect |
| Phase 5: US3 | 1 | Enter key submit |
| Phase 6: Polish | 3 | E2E tests, docs |
| **Total** | **19** | |

### Tasks per User Story

| User Story | Tasks | Type |
|------------|-------|------|
| US1 (Token Refresh) | 3 | Verification only |
| US2 (Return URL) | 6 | New implementation |
| US3 (Enter Key) | 1 | Enhancement |

### Independent Test Criteria

| User Story | How to Test Independently |
|------------|--------------------------|
| US1 | Login, wait for token near expiration, make API request, verify no interruption |
| US2 | Expire token, attempt action, verify redirect with returnUrl, login, verify navigation |
| US3 | Go to login, enter credentials, press Enter on password, verify form submits |

---

## Notes

- US1 requires no new code - existing implementation satisfies requirements
- US2 is the main implementation effort (NavigationService + integrations)
- US3 is a single-file change to Login.razor
- UrlValidator is foundational for security - must be complete first
- E2E tests validate the complete flow including Docker infrastructure
