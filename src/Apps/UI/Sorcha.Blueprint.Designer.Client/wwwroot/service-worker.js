// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

// Development service worker - minimal functionality
// This is used during development and provides basic offline support

self.addEventListener('install', event => {
    console.log('Service worker installing...');
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    console.log('Service worker activating...');
});

self.addEventListener('fetch', event => {
    // In development, we don't cache anything
    // Just pass through all requests
});
