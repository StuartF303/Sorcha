# Clear Browser Cache & Service Worker

The Service Worker is caching old Blazor files and causing load errors. Follow these steps:

## Option 1: Clear Everything (Recommended)

1. **Open the app** in your browser: `http://localhost:9091`
2. **Open DevTools**: Press `F12`
3. **Go to Application tab**
4. **Service Workers section** (left sidebar):
   - Click "Unregister" next to the service worker
5. **Storage section** (left sidebar):
   - Click "Clear site data"
   - Check all boxes
   - Click "Clear site data"
6. **Hard reload**:
   - Windows/Linux: `Ctrl + Shift + R`
   - Mac: `Cmd + Shift + R`

## Option 2: Quick Fix

1. Open DevTools (`F12`)
2. **Network tab**
3. Check "Disable cache" checkbox
4. Keep DevTools open while browsing
5. Refresh the page (`F5`)

## Option 3: Incognito/Private Window

1. Close all browser windows
2. Open **Incognito/Private** window:
   - Chrome: `Ctrl + Shift + N`
   - Firefox: `Ctrl + Shift + P`
   - Edge: `Ctrl + Shift + N`
3. Navigate to `http://localhost:9091`

## Verify It's Fixed

After clearing cache, you should see:
- ✅ No "Failed to load module script" errors
- ✅ `[SorchaEncryption] Module loaded` message
- ✅ Blazor app loads successfully
- ✅ Login page appears

## If Still Not Working

The service worker might be stuck. Try:

```javascript
// In browser console (F12 → Console tab), run:
navigator.serviceWorker.getRegistrations().then(function(registrations) {
    for(let registration of registrations) {
        registration.unregister();
    }
    console.log('All service workers unregistered');
    location.reload();
});
```

## Re-enable Service Worker (Later)

Once everything works, you can re-enable it by uncommenting the service worker code in `index.html`.

The service worker is useful for:
- Offline support
- Faster load times
- Progressive Web App (PWA) features

But during development, it can cause caching issues.
