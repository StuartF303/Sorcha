# Sorcha.UI.Web Playwright Test Results

**Date:** 2026-01-06
**Environment:** Docker (http://172.19.0.14:8080/ via API Gateway)
**Status:** ❌ Multiple CSS and Configuration Issues Found

## Test Summary

### Home Page (/)
- ✅ Page loads successfully (HTTP 200)
- ✅ Navigation bar renders
- ✅ Content displays with proper headings
- ❌ CSS not loading - MIME type errors
- ❌ CSP inline script violation

### Login Page (/login)
- ✅ Page navigates successfully
- ❌ CSS not loading - MIME type errors
- ❌ `appsettings.json` returns 404
- ❌ Blazor WASM fails to load configuration
- ❌ Login form not rendering (CSS/WASM errors)

## Issues Identified

### 1. CSS File Reference Error (Critical)
**File:** `src/Apps/Sorcha.UI/Sorcha.UI.Web/Components/App.razor:12`

**Problem:**
```html
<link rel="stylesheet" href="@Assets["Temp.styles.css"]" />
```

**Should be:**
```html
<link rel="stylesheet" href="@Assets["Sorcha.UI.Web.styles.css"]" />
```

**Impact:** Scoped component styles fail to load, causing styling issues.

---

### 2. Static Asset Fingerprinting Not Working (Critical)

**Problem:**
HTML references fingerprinted CSS files that don't exist in the container:
- Requested: `bootstrap.min.46ein0sx1k.css`
  Actual: `bootstrap.min.css`
- Requested: `app.khy4lop6wu.css`
  Actual: `app.css`
- Requested: `layout.bd24ygd9xr.css`
  Actual: `layout.css`

**Console Errors:**
```
Refused to apply style from 'http://172.19.0.14:8080/lib/bootstrap/dist/css/bootstrap.min.46ein0sx1k.css'
because its MIME type ('') is not a supported stylesheet MIME type
```

**Root Cause:**
`.MapStaticAssets()` in `Program.cs` generates fingerprinted URLs, but the actual files in the published output (`/app/wwwroot/`) are NOT fingerprinted.

**Verified Files in Container:**
```bash
$ docker exec sorcha-ui-web sh -c "ls -la /app/wwwroot/"
-rw-r--r-- 1 root root  5805 Jan  6 16:33 Sorcha.UI.Web.styles.css
-rwxr-xr-x 1 root root  2900 Jan  6 13:54 app.css
-rwxr-xr-x 1 root root  6976 Jan  6 16:06 layout.css
-rwxr-xr-x 1 root root   127 Jan  6 13:50 appsettings.json
```

Files exist WITHOUT fingerprints, but HTML requests WITH fingerprints.

**Solution Options:**
1. **Use `.UseStaticFiles()` instead of `.MapStaticAssets()`** (works in Docker)
2. Investigate why `MapStaticAssets()` fingerprinting isn't working in Release builds

---

### 3. appsettings.json Not Loading in Blazor WASM (Critical)

**Problem:**
Blazor WebAssembly client tries to load `/appsettings.json` but receives 404:

**Console Error:**
```
Error in mono_download_assets: Error: download 'http://172.19.0.14:8080/appsettings.json'
for ../appsettings.json failed 404 Not Found
```

**File Status:**
- File EXISTS: `/app/wwwroot/appsettings.json` (127 bytes)
- Returns: 404 Not Found when accessed via HTTP

**Root Cause:**
The file exists but isn't being served correctly. Possible causes:
1. Static file middleware configuration issue
2. Routing conflict with API endpoints
3. File not included in static assets map

**Solution:**
Ensure static file middleware serves `appsettings.json`:
```csharp
app.UseStaticFiles();  // Before MapStaticAssets
app.MapStaticAssets();
```

---

### 4. Content Security Policy (CSP) Violation (Medium)

**Error:**
```
Executing inline script violates the following Content Security Policy directive
'script-src 'self' 'unsafe-eval' 'wasm-unsafe-eval''.
Either the 'unsafe-inline' keyword, a hash ('sha256-...'), or a nonce ('nonce-...')
is required to enable inline execution.
```

**Location:** `Program.cs:36`

**Current CSP:**
```csharp
"script-src 'self' 'unsafe-eval' 'wasm-unsafe-eval'"
```

**Issue:**
Inline scripts in the HTML are blocked. The import map script is inline.

**Solution:**
Either:
1. Add specific script hash to CSP
2. Temporarily add `'unsafe-inline'` for development (not recommended for production)
3. Move inline scripts to external files

---

## Browser Console Errors (Full Log)

### Home Page
```
[ERROR] Executing inline script violates CSP directive 'script-src...'
[ERROR] Refused to apply style from '.../bootstrap.min.46ein0sx1k.css' (MIME type: '')
[ERROR] Refused to apply style from '.../layout.bd24ygd9xr.css' (MIME type: '')
[ERROR] Refused to apply style from '.../Temp.styles.css' (MIME type: '')
[ERROR] Refused to apply style from '.../app.khy4lop6wu.css' (MIME type: '')
```

### Login Page (Additional)
```
[ERROR] Failed to load resource: 404 (Not Found) @ .../appsettings.json
[ERROR] Error in mono_download_assets: download '.../appsettings.json' failed 404
[ERROR] MONO_WASM: download '.../appsettings.json' for ../appsettings.json failed 404
```

---

## Recommendations

### Immediate Fixes (P0)

1. **Fix Component Styles Reference**
   - File: `src/Apps/Sorcha.UI/Sorcha.UI.Web/Components/App.razor:12`
   - Change: `Temp.styles.css` → `Sorcha.UI.Web.styles.css`

2. **Replace MapStaticAssets with UseStaticFiles**
   - File: `src/Apps/Sorcha.UI/Sorcha.UI.Web/Program.cs:62`
   - Change: `app.MapStaticAssets()` → `app.UseStaticFiles()`
   - Reason: Fingerprinting not working in Docker Release builds

3. **Fix appsettings.json Loading**
   - Ensure `UseStaticFiles()` is called before routing
   - Verify `appsettings.json` is in Web.Client wwwroot and published correctly

### Medium Priority (P1)

4. **Update CSP Policy**
   - Add inline script support or use nonces
   - Test with proper hash values

### Testing Checklist

- [ ] CSS files load without MIME type errors
- [ ] Scoped styles (`Sorcha.UI.Web.styles.css`) applied correctly
- [ ] `appsettings.json` loads successfully (200 OK)
- [ ] Login page renders with proper styling
- [ ] No CSP violations in console
- [ ] Authentication flow works end-to-end

---

## Test Environment Details

**Container:** sorcha-ui-web
**Image:** sorcha/ui-web:latest
**Container IP:** 172.19.0.6
**API Gateway IP:** 172.19.0.14
**Exposed Ports:** 5173:8080 (direct), 80:8080 (via gateway)

**Test Credentials:**
- Username: `admin@sorcha.local`
- Password: `Dev_Pass_2025!`

---

## Screenshots

![Login Page Errors](login-page-errors.png)
*Login page showing blank content due to CSS and WASM loading failures*

---

## Next Steps

1. Apply fixes for P0 issues
2. Rebuild Docker image
3. Retest with Playwright
4. Verify full user journey: Anonymous → Login → Authenticated

**Generated by:** Playwright Browser Automation
**Test Run:** 2026-01-06T16:30:00Z
