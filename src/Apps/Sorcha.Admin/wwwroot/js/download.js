// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

/**
 * File download helper functions for Blazor
 */

/**
 * Downloads a file by creating a temporary anchor element and triggering a click.
 * @param {string} fileName - The name of the file to download
 * @param {string} base64Content - The file content as a base64-encoded string
 * @param {string} mimeType - The MIME type of the file
 */
window.downloadFile = function (fileName, base64Content, mimeType) {
    // Convert base64 to Blob
    const byteCharacters = atob(base64Content);
    const byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    const byteArray = new Uint8Array(byteNumbers);
    const blob = new Blob([byteArray], { type: mimeType });

    // Create a URL for the Blob
    const url = URL.createObjectURL(blob);

    // Create a temporary anchor element and trigger download
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();

    // Cleanup
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};
