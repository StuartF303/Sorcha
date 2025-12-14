// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

// Production service worker for Blazor WebAssembly with offline support
// Caution! Be sure you understand the caching implications of this code.
// The wrong caching strategy can prevent users from receiving updates.

const CACHE_NAME_PREFIX = 'sorcha-cache-';
const OFFLINE_URL = '/offline.html';

// Assets to cache on install
const PRECACHE_ASSETS = [
    '/',
    '/index.html',
    '/css/app.css',
    OFFLINE_URL
];

// Patterns for assets that should be cached
const CACHEABLE_PATTERNS = [
    /\.dll$/,
    /\.pdb$/,
    /\.wasm$/,
    /\.html$/,
    /\.js$/,
    /\.json$/,
    /\.css$/,
    /\.woff$/,
    /\.woff2$/,
    /\.ttf$/,
    /\.eot$/,
    /\.png$/,
    /\.jpe?g$/,
    /\.gif$/,
    /\.ico$/,
    /\.svg$/,
    /\.blat$/,
    /\.dat$/
];

// Patterns for assets that should NOT be cached
const EXCLUDE_PATTERNS = [
    /^service-worker\.js$/,
    /^service-worker\.published\.js$/,
    /\/api\//,  // Don't cache API calls
    /blazor\.boot\.json$/  // Always fetch fresh manifest
];

self.addEventListener('install', event => {
    console.log('Service worker installing...');

    event.waitUntil(
        (async () => {
            const cache = await caches.open(CACHE_NAME_PREFIX + 'v1');

            // Cache core assets
            try {
                await cache.addAll(PRECACHE_ASSETS.filter(url => url !== OFFLINE_URL));
            } catch (error) {
                console.error('Failed to cache core assets:', error);
            }

            // Create offline fallback page
            await cache.put(OFFLINE_URL, new Response(
                '<!DOCTYPE html><html><head><title>Offline</title><style>body{font-family:sans-serif;text-align:center;padding:50px;}h1{color:#594ae2;}</style></head><body><h1>Sorcha Blueprint Designer</h1><p>You are currently offline. Please check your internet connection.</p></body></html>',
                { headers: { 'Content-Type': 'text/html' } }
            ));

            await self.skipWaiting();
        })()
    );
});

self.addEventListener('activate', event => {
    console.log('Service worker activating...');

    event.waitUntil(
        (async () => {
            // Clean up old caches
            const cacheNames = await caches.keys();
            await Promise.all(
                cacheNames
                    .filter(name => name.startsWith(CACHE_NAME_PREFIX) && name !== CACHE_NAME_PREFIX + 'v1')
                    .map(name => caches.delete(name))
            );

            await self.clients.claim();
        })()
    );
});

self.addEventListener('fetch', event => {
    const url = new URL(event.request.url);

    // Skip non-GET requests
    if (event.request.method !== 'GET') {
        return;
    }

    // Skip excluded patterns
    if (EXCLUDE_PATTERNS.some(pattern => pattern.test(url.pathname))) {
        return;
    }

    // Check if URL should be cached
    const shouldCache = CACHEABLE_PATTERNS.some(pattern =>
        pattern.test(url.pathname) || pattern.test(url.href)
    );

    if (!shouldCache) {
        return;
    }

    event.respondWith(
        (async () => {
            try {
                // Try network first for HTML
                if (url.pathname.endsWith('.html') || url.pathname === '/') {
                    try {
                        const networkResponse = await fetch(event.request);
                        if (networkResponse.ok) {
                            const cache = await caches.open(CACHE_NAME_PREFIX + 'v1');
                            cache.put(event.request, networkResponse.clone());
                            return networkResponse;
                        }
                    } catch (error) {
                        // Network failed, try cache
                        const cachedResponse = await caches.match(event.request);
                        if (cachedResponse) {
                            return cachedResponse;
                        }
                        // Return offline page
                        return caches.match(OFFLINE_URL);
                    }
                }

                // For other assets, try cache first
                const cachedResponse = await caches.match(event.request);
                if (cachedResponse) {
                    // Return cached version and update in background
                    event.waitUntil(
                        (async () => {
                            try {
                                const networkResponse = await fetch(event.request);
                                if (networkResponse.ok) {
                                    const cache = await caches.open(CACHE_NAME_PREFIX + 'v1');
                                    cache.put(event.request, networkResponse);
                                }
                            } catch (error) {
                                // Ignore background update failures
                            }
                        })()
                    );
                    return cachedResponse;
                }

                // Not in cache, fetch from network
                const networkResponse = await fetch(event.request);

                // Cache successful responses
                if (networkResponse.ok) {
                    const cache = await caches.open(CACHE_NAME_PREFIX + 'v1');
                    cache.put(event.request, networkResponse.clone());
                }

                return networkResponse;

            } catch (error) {
                console.error('Fetch failed:', error);

                // Return offline page for navigation requests
                if (event.request.mode === 'navigate') {
                    const offlineResponse = await caches.match(OFFLINE_URL);
                    if (offlineResponse) {
                        return offlineResponse;
                    }
                }

                // For other requests, throw the error
                throw error;
            }
        })()
    );
});

// Handle background sync (future enhancement)
self.addEventListener('sync', event => {
    console.log('Background sync event:', event.tag);
    // TODO: Implement background sync for blueprint saves
});

// Handle push notifications (future enhancement)
self.addEventListener('push', event => {
    console.log('Push notification received:', event);
    // TODO: Implement push notifications for blueprint updates
});
