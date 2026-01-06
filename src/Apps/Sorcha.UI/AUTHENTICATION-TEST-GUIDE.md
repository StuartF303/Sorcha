# Authentication Testing Guide - Sorcha.UI

**Created:** 2026-01-06
**Status:** Ready for Testing

## Overview

This guide describes how to test the newly implemented authentication system in Sorcha.UI.

## Architecture Summary

### Components Implemented

1. **Core Services (Sorcha.UI.Core)**:
   - `IAuthenticationService` + `AuthenticationService` - OAuth2 Password Grant
   - `ITokenCache` + `BrowserTokenCache` - Encrypted LocalStorage token caching
   - `IEncryptionProvider` + `BrowserEncryptionProvider` - AES-256-GCM encryption
   - `IConfigurationService` + `ConfigurationService` - Profile management
   - `CustomAuthenticationStateProvider` - Blazor authentication state
   - `AuthenticatedHttpMessageHandler` - JWT Bearer token injection

2. **JavaScript Interop**:
   - `encryption.js` - Web Crypto API wrapper (AES-256-GCM)

3. **UI Components**:
   - `/login` - Login page with profile selection
   - `/auth-test` - Protected test page showing authentication state

4. **Domain Models**:
   - `LoginRequest`, `TokenResponse`, `TokenCacheEntry`
   - `AuthenticationStateInfo`, `Profile`, `UiConfiguration`
   - `ApiResponse<T>`, `PaginatedList<T>`

## Test Scenarios

### Scenario 1: Basic Login Flow

**Prerequisites:**
- Backend services running (API Gateway, Tenant Service)
- Default profiles initialized (Development, Docker)

**Steps:**
1. Start application: `dotnet run --project Sorcha.UI.Web`
2. Navigate to: https://localhost:7083
3. You should be automatically redirected to `/login`
4. Select profile: "Docker" or "Development"
5. Enter credentials:
   - Username: `admin@sorcha.local`
   - Password: `Admin123!`
6. Click "Sign In"

**Expected Result:**
- ✅ Login succeeds
- ✅ Redirected to home page (/)
- ✅ JWT token cached in encrypted LocalStorage
- ✅ User authenticated state persists

**Verification:**
- Open browser DevTools → Application → Local Storage
- Check for key: `sorcha:tokens:{profileName}`
- Value should be encrypted (format: `{iv}:{ciphertext}`)

---

### Scenario 2: Protected Page Access

**Steps:**
1. After logging in (Scenario 1), navigate to: `/auth-test`
2. Observe authentication information displayed

**Expected Result:**
- ✅ Page displays user information:
  - Username: `admin@sorcha.local`
  - Profile: Docker (or Development)
  - Roles: Administrator (or role from JWT)
  - Expires At: (timestamp)
  - Authenticated: True

---

### Scenario 3: Unauthorized Access

**Steps:**
1. Open browser in Incognito/Private mode
2. Navigate directly to: https://localhost:7083/auth-test

**Expected Result:**
- ✅ Automatically redirected to `/login`
- ✅ No access to protected page without authentication

---

### Scenario 4: Logout Flow

**Steps:**
1. After logging in, navigate to `/auth-test`
2. Click "Logout" button

**Expected Result:**
- ✅ Token removed from LocalStorage
- ✅ Redirected to `/login`
- ✅ Subsequent navigation to `/auth-test` redirects to login

---

### Scenario 5: Token Persistence (Page Refresh)

**Steps:**
1. Log in successfully
2. Navigate to `/auth-test`
3. Refresh the browser (F5)

**Expected Result:**
- ✅ User remains authenticated
- ✅ Token loaded from LocalStorage
- ✅ Authentication state restored
- ✅ No redirect to login page

---

### Scenario 6: Encryption Verification

**Steps:**
1. Log in successfully
2. Open DevTools → Console
3. Run: `EncryptionHelper.isAvailable()`
4. Check LocalStorage token value

**Expected Result:**
- ✅ `isAvailable()` returns `true`
- ✅ Token is encrypted (not plain JWT)
- ✅ Format matches: `{base64_iv}:{base64_ciphertext}`

---

### Scenario 7: Profile Switching

**Steps:**
1. Log in with "Development" profile
2. Log out
3. Log in with "Docker" profile

**Expected Result:**
- ✅ Different tokens cached per profile
- ✅ Active profile updated in configuration
- ✅ Subsequent API calls use correct profile URL

---

## Manual Testing Checklist

- [ ] Scenario 1: Basic Login Flow
- [ ] Scenario 2: Protected Page Access
- [ ] Scenario 3: Unauthorized Access
- [ ] Scenario 4: Logout Flow
- [ ] Scenario 5: Token Persistence
- [ ] Scenario 6: Encryption Verification
- [ ] Scenario 7: Profile Switching

## Known Limitations

1. **Backend Required**: Tests require running backend services (API Gateway + Tenant Service)
2. **HTTPS Required**: Web Crypto API requires HTTPS or localhost
3. **Token Refresh**: Automatic refresh on 401 not yet fully tested
4. **Profile Management UI**: No UI for creating/editing profiles yet

## Debugging Tips

### Check Encryption is Working
```javascript
// Browser console
EncryptionHelper.isAvailable()  // Should return true
EncryptionHelper.generateKey()  // Generates new AES-256 key
```

### Check Token Cache
```javascript
// Browser console
localStorage.getItem('sorcha:tokens:Development')  // Encrypted token
localStorage.getItem('sorcha:profiles')  // Profile list (plaintext JSON)
localStorage.getItem('sorcha:ui-config')  // UI config (plaintext JSON)
```

### Check Authentication State
```javascript
// Add to Login.razor or AuthTest.razor @code block
protected override async Task OnInitializedAsync()
{
    var authInfo = await AuthService.GetAuthenticationInfoAsync();
    Console.WriteLine($"Authenticated: {authInfo.IsAuthenticated}");
    Console.WriteLine($"Username: {authInfo.Username}");
    Console.WriteLine($"Roles: {string.Join(", ", authInfo.Roles)}");
}
```

## Next Steps

After successful authentication testing:
1. Implement MudBlazor UI components
2. Create layout with user menu
3. Add profile management UI
4. Implement module lazy loading (Admin, Designer, Explorer)
5. Add token refresh UI feedback
6. Implement logout confirmation dialog

---

**Build Status:** ✅ All projects building successfully
**Test Status:** ⏳ Ready for manual testing
**Last Updated:** 2026-01-06
