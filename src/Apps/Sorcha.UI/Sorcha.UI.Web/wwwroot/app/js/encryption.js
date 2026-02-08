/**
 * Web Crypto API wrapper for AES-256-GCM encryption
 * Used by BrowserEncryptionProvider for LocalStorage token encryption
 */

const EncryptionHelper = {
    /**
     * Checks if Web Crypto API is available
     * @returns {boolean} True if Web Crypto API is supported
     */
    isAvailable: function() {
        return typeof crypto !== 'undefined' &&
               typeof crypto.subtle !== 'undefined';
    },

    /**
     * Generates a new AES-256-GCM encryption key
     * @returns {Promise<string>} Base64-encoded key
     */
    generateKey: async function() {
        if (!this.isAvailable()) {
            throw new Error("Web Crypto API not available. Use HTTPS or localhost.");
        }

        const key = await crypto.subtle.generateKey(
            {
                name: "AES-GCM",
                length: 256
            },
            true,
            ["encrypt", "decrypt"]
        );

        const exported = await crypto.subtle.exportKey("raw", key);
        return this._arrayBufferToBase64(exported);
    },

    /**
     * Encrypts plaintext using AES-256-GCM
     * @param {string} plaintext - Text to encrypt
     * @param {string} base64Key - Base64-encoded encryption key
     * @returns {Promise<string>} Encrypted data in format: {iv}:{ciphertext}
     */
    encrypt: async function(plaintext, base64Key) {
        if (!this.isAvailable()) {
            throw new Error("Web Crypto API not available. Use HTTPS or localhost.");
        }

        // Import the key
        const keyData = this._base64ToArrayBuffer(base64Key);
        const key = await crypto.subtle.importKey(
            "raw",
            keyData,
            { name: "AES-GCM" },
            false,
            ["encrypt"]
        );

        // Generate random IV (12 bytes for GCM)
        const iv = crypto.getRandomValues(new Uint8Array(12));

        // Encrypt the plaintext
        const encoder = new TextEncoder();
        const encodedText = encoder.encode(plaintext);

        const ciphertext = await crypto.subtle.encrypt(
            {
                name: "AES-GCM",
                iv: iv
            },
            key,
            encodedText
        );

        // Return format: {iv}:{ciphertext}
        const ivBase64 = this._arrayBufferToBase64(iv);
        const ciphertextBase64 = this._arrayBufferToBase64(ciphertext);

        return `${ivBase64}:${ciphertextBase64}`;
    },

    /**
     * Decrypts ciphertext using AES-256-GCM
     * @param {string} encryptedData - Encrypted data in format: {iv}:{ciphertext}
     * @param {string} base64Key - Base64-encoded encryption key
     * @returns {Promise<string>} Decrypted plaintext
     */
    decrypt: async function(encryptedData, base64Key) {
        if (!this.isAvailable()) {
            throw new Error("Web Crypto API not available. Use HTTPS or localhost.");
        }

        // Parse the encrypted data
        const parts = encryptedData.split(':');
        if (parts.length !== 2) {
            throw new Error("Invalid encrypted data format. Expected: {iv}:{ciphertext}");
        }

        const iv = this._base64ToArrayBuffer(parts[0]);
        const ciphertext = this._base64ToArrayBuffer(parts[1]);

        // Import the key
        const keyData = this._base64ToArrayBuffer(base64Key);
        const key = await crypto.subtle.importKey(
            "raw",
            keyData,
            { name: "AES-GCM" },
            false,
            ["decrypt"]
        );

        // Decrypt the ciphertext
        const decrypted = await crypto.subtle.decrypt(
            {
                name: "AES-GCM",
                iv: iv
            },
            key,
            ciphertext
        );

        // Convert decrypted ArrayBuffer to string
        const decoder = new TextDecoder();
        return decoder.decode(decrypted);
    },

    /**
     * Converts ArrayBuffer to Base64 string
     * @private
     */
    _arrayBufferToBase64: function(buffer) {
        const bytes = new Uint8Array(buffer);
        let binary = '';
        for (let i = 0; i < bytes.byteLength; i++) {
            binary += String.fromCharCode(bytes[i]);
        }
        return btoa(binary);
    },

    /**
     * Converts Base64 string to ArrayBuffer
     * @private
     */
    _base64ToArrayBuffer: function(base64) {
        const binary = atob(base64);
        const bytes = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) {
            bytes[i] = binary.charCodeAt(i);
        }
        return bytes.buffer;
    }
};

// Export for .NET JSInterop
window.EncryptionHelper = EncryptionHelper;
