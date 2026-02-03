# Feature Specification: Sorcha.UI Authentication Token Management and Login UX

**Feature Branch**: `001-ui-token-refresh`
**Created**: 2026-02-03
**Status**: Draft
**Input**: User description: "Sorcha.UI Authentication Token Management and Login UX - automatic token refresh, redirect to login with return URL on expiration, Enter key submits login form"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Seamless Token Refresh During Active Session (Priority: P1)

As an authenticated user actively working in Sorcha.UI, I want my session to automatically refresh before my token expires so that I can continue working without interruption or being unexpectedly logged out.

**Why this priority**: This is the core feature that enables uninterrupted user workflow. Without proactive token refresh, users would experience session timeouts during active work, leading to data loss and frustration.

**Independent Test**: Can be fully tested by logging in, waiting for the token to approach expiration, and verifying that API requests continue to succeed without user intervention. Delivers continuous session experience.

**Acceptance Scenarios**:

1. **Given** a user is authenticated with a token expiring in 5 minutes, **When** the user makes an API request, **Then** the system proactively refreshes the token before making the request and the user experiences no interruption.

2. **Given** a user is authenticated with a token expiring in 10 minutes, **When** the user makes an API request, **Then** the system uses the existing token without refreshing (refresh threshold not yet reached).

3. **Given** a token refresh is already in progress, **When** another API request is made simultaneously, **Then** the second request waits for the first refresh to complete and uses the new token (no duplicate refresh calls).

4. **Given** a user's token is refreshed successfully, **When** the user continues working, **Then** all subsequent requests use the new token with the updated expiration time.

---

### User Story 2 - Redirect to Login with Return URL (Priority: P1)

As a user whose session has expired beyond recovery, I want to be redirected to the login page with a return URL so that after logging in again, I am taken back to where I was working.

**Why this priority**: This is equally critical as P1 because it handles the failure path gracefully. When tokens cannot be refreshed, users must be guided back to authentication without losing their navigation context.

**Independent Test**: Can be tested by invalidating a token (or letting it expire beyond refresh capability) and verifying the redirect includes the return URL parameter and post-login navigation works correctly.

**Acceptance Scenarios**:

1. **Given** a user's token has expired and refresh fails, **When** the user attempts any authenticated action, **Then** the user is redirected to the login page with a `returnUrl` parameter containing the current page path.

2. **Given** a user's token is invalid (e.g., tampered or revoked), **When** the user attempts any authenticated action, **Then** the user is redirected to the login page with a `returnUrl` parameter.

3. **Given** a user is on the login page with a `returnUrl` parameter, **When** the user successfully logs in, **Then** the user is redirected to the URL specified in `returnUrl`.

4. **Given** a user is on the login page with no `returnUrl` parameter, **When** the user successfully logs in, **Then** the user is redirected to the default landing page (dashboard/home).

5. **Given** a user is redirected to login with a `returnUrl`, **When** the user manually navigates to a different page after login, **Then** the system respects the user's manual navigation (no forced redirect).

---

### User Story 3 - Enter Key Submits Login Form (Priority: P2)

As a user on the login page, I want to press Enter after typing my password to submit the login form so that I can log in quickly without additional mouse clicks or tab navigation.

**Why this priority**: This is a UX improvement that reduces friction during login. While not blocking functionality, it significantly improves the login experience and matches user expectations from standard web forms.

**Independent Test**: Can be tested by navigating to the login page, entering credentials, and pressing Enter while focused on the password field. Delivers faster, more intuitive login experience.

**Acceptance Scenarios**:

1. **Given** a user is on the login page with valid username and password entered, **When** the user presses Enter while focused on the password field, **Then** the login form is submitted.

2. **Given** a user is on the login page with the username field focused, **When** the user presses Enter, **Then** focus moves to the password field (or form submits if password is already filled).

3. **Given** a user is on the login page during an ongoing login request, **When** the user presses Enter again, **Then** no duplicate submission occurs (submit is debounced/disabled during loading).

4. **Given** a user presses Enter with empty or invalid credentials, **When** the form validation fails, **Then** appropriate validation messages are displayed (same as clicking submit).

---

### Edge Cases

- What happens when the refresh token itself has expired? The system should redirect to login with return URL.
- What happens if the user has multiple browser tabs open? Token refresh should be coordinated to prevent race conditions (existing semaphore mechanism).
- What happens if the network is temporarily unavailable during refresh? The system should retry the original request if connection is restored, or redirect to login if refresh ultimately fails.
- What happens if the returnUrl contains an external/malicious URL? The system should validate that returnUrl is a relative path or same-origin URL only.
- What happens if the user is already on the login page when token expires? No redirect loop should occur.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST monitor token expiration time and initiate refresh when the token is within 5 minutes of expiring.
- **FR-002**: System MUST use the refresh token to obtain a new access token without requiring user credentials.
- **FR-003**: System MUST serialize concurrent refresh attempts to prevent duplicate refresh requests (maintain existing semaphore behavior).
- **FR-004**: System MUST redirect to the login page when token refresh fails due to expired or invalid refresh token.
- **FR-005**: System MUST include a `returnUrl` query parameter when redirecting to the login page, containing the user's current navigation path.
- **FR-006**: System MUST redirect users to the `returnUrl` after successful login if the parameter is present.
- **FR-007**: System MUST validate that `returnUrl` is a relative path or same-origin URL to prevent open redirect vulnerabilities.
- **FR-008**: System MUST submit the login form when the user presses Enter while focused on the password input field.
- **FR-009**: System MUST prevent duplicate form submissions while a login request is in progress.
- **FR-010**: System MUST clear all cached tokens and authentication state before redirecting to the login page on session expiration.

### Key Entities

- **TokenCacheEntry**: Represents a cached authentication token with AccessToken, RefreshToken, ExpiresAt, ProfileName, IssuedAt, and computed properties (IsExpired, IsNearExpiration).
- **ReturnUrl**: A URL-encoded path representing where the user should be redirected after successful authentication. Must be validated for security.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users experience zero unexpected session terminations during active work when their token is refreshable (measured by absence of mid-workflow login redirects).
- **SC-002**: 100% of login redirects due to token expiration include the correct return URL parameter.
- **SC-003**: 100% of successful logins with a valid return URL redirect users to their previous location.
- **SC-004**: Users can complete the login flow using only the keyboard (no mouse required for credential entry and submission).
- **SC-005**: No security vulnerabilities introduced by return URL handling (all return URLs validated against open redirect attacks).
- **SC-006**: Token refresh completes transparently with no user-visible delay or interruption during normal operation.

## Assumptions

- The existing `AuthenticatedHttpMessageHandler` semaphore mechanism is sufficient for preventing concurrent refresh race conditions.
- The 5-minute refresh threshold (existing `IsNearExpiration` check) is appropriate for the token lifetime configured on the server.
- The OAuth2 refresh token grant type is supported by the authentication endpoint (`/api/service-auth/token`).
- Browser localStorage is available and functioning for token storage (existing requirement).
- The login page currently uses a standard HTML form or MudBlazor form component that can be enhanced for keyboard submission.
