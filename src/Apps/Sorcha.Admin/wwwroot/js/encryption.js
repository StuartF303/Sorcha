/**
 * Sorcha Admin - Browser Encryption Module
 *
 * Provides AES-256-GCM encryption using the Web Crypto API (SubtleCrypto).
 * Used for encrypting JWT tokens stored in browser LocalStorage.
 *
 * SECURITY NOTE: This provides encryption at rest, but the key is derivable
 * from JavaScript in the same origin. Protects against casual inspection and
 * accidental exposure, but NOT against XSS attacks or determined attackers.
 *
 * Mitigation: Use short token lifetimes, aggressive refresh, and CSP headers.
 */

window.SorchaEncryption = {
    cryptoKey: null,
    isInitialized: false,

    /**
     * Initializes the encryption system by deriving an AES-256 key from browser fingerprint.
     * This key persists for the browser session and is used for all encrypt/decrypt operations.
     */
    async initialize() {
        if (this.isInitialized) {
            return;
        }

        try {
            // Generate a browser fingerprint (simple version - could use FingerprintJS in production)
            const fingerprint = await this.getBrowserFingerprint();

            // Import the fingerprint as key material
            const keyMaterial = await crypto.subtle.importKey(
                "raw",
                new TextEncoder().encode(fingerprint),
                "PBKDF2",
                false,
                ["deriveBits", "deriveKey"]
            );

            // Derive an AES-256-GCM key using PBKDF2 (100k iterations)
            this.cryptoKey = await crypto.subtle.deriveKey(
                {
                    name: "PBKDF2",
                    salt: new TextEncoder().encode("sorcha-admin-v1"), // Fixed salt
                    iterations: 100000,
                    hash: "SHA-256"
                },
                keyMaterial,
                { name: "AES-GCM", length: 256 },
                false,
                ["encrypt", "decrypt"]
            );

            this.isInitialized = true;
            console.debug("[SorchaEncryption] Initialized successfully");
        } catch (error) {
            console.error("[SorchaEncryption] Initialization failed:", error);
            throw new Error("Failed to initialize encryption system");
        }
    },

    /**
     * Encrypts plaintext using AES-256-GCM.
     * @param {string} plaintext - The plaintext string to encrypt
     * @returns {Uint8Array} Encrypted data (IV + ciphertext combined)
     */
    async encrypt(plaintext) {
        if (!this.isInitialized) {
            await this.initialize();
        }

        try {
            // Generate a random 12-byte IV (initialization vector) for GCM
            const iv = crypto.getRandomValues(new Uint8Array(12));

            // Encode plaintext to bytes
            const encoded = new TextEncoder().encode(plaintext);

            // Encrypt using AES-GCM
            const ciphertext = await crypto.subtle.encrypt(
                { name: "AES-GCM", iv: iv },
                this.cryptoKey,
                encoded
            );

            // Combine IV + ciphertext (IV is needed for decryption)
            const result = new Uint8Array(iv.length + ciphertext.byteLength);
            result.set(iv, 0);
            result.set(new Uint8Array(ciphertext), iv.length);

            // Return as array (Blazor will receive as byte[])
            return Array.from(result);
        } catch (error) {
            console.error("[SorchaEncryption] Encryption failed:", error);
            throw new Error("Encryption failed");
        }
    },

    /**
     * Decrypts ciphertext using AES-256-GCM.
     * @param {Array<number>|Uint8Array} encryptedData - The encrypted data (IV + ciphertext)
     * @returns {string} Decrypted plaintext string
     */
    async decrypt(encryptedData) {
        if (!this.isInitialized) {
            await this.initialize();
        }

        try {
            // Convert to Uint8Array if needed
            const data = new Uint8Array(encryptedData);

            // Extract IV (first 12 bytes) and ciphertext (remaining bytes)
            const iv = data.slice(0, 12);
            const ciphertext = data.slice(12);

            // Decrypt using AES-GCM
            const decrypted = await crypto.subtle.decrypt(
                { name: "AES-GCM", iv: iv },
                this.cryptoKey,
                ciphertext
            );

            // Decode bytes to string
            return new TextDecoder().decode(decrypted);
        } catch (error) {
            console.error("[SorchaEncryption] Decryption failed:", error);
            throw new Error("Decryption failed - data may be corrupted or tampered with");
        }
    },

    /**
     * Generates a browser fingerprint for key derivation.
     * This is a simple implementation - production systems should use a library like FingerprintJS.
     *
     * @returns {string} Browser fingerprint string
     */
    async getBrowserFingerprint() {
        // Collect browser characteristics
        const components = [
            navigator.userAgent,
            navigator.language,
            screen.colorDepth.toString(),
            screen.width.toString(),
            screen.height.toString(),
            new Date().getTimezoneOffset().toString(),
            navigator.hardwareConcurrency?.toString() || "0",
            navigator.deviceMemory?.toString() || "0"
        ];

        // Canvas fingerprinting (simple version)
        try {
            const canvas = document.createElement('canvas');
            const ctx = canvas.getContext('2d');
            ctx.textBaseline = 'top';
            ctx.font = '14px Arial';
            ctx.textBaseline = 'alphabetic';
            ctx.fillStyle = '#f60';
            ctx.fillRect(125, 1, 62, 20);
            ctx.fillStyle = '#069';
            ctx.fillText('Sorcha Admin 🔐', 2, 15);
            ctx.fillStyle = 'rgba(102, 204, 0, 0.7)';
            ctx.fillText('Browser Fingerprint', 4, 17);

            components.push(canvas.toDataURL());
        } catch (e) {
            console.warn("[SorchaEncryption] Canvas fingerprinting failed:", e);
        }

        // Combine all components and hash them
        const fingerprintString = components.join('|');

        // Simple hash (in production, use a proper hash function)
        let hash = 0;
        for (let i = 0; i < fingerprintString.length; i++) {
            const char = fingerprintString.charCodeAt(i);
            hash = ((hash << 5) - hash) + char;
            hash = hash & hash; // Convert to 32-bit integer
        }

        return fingerprintString + hash.toString(36);
    }
};

// Automatically initialize on script load
console.log("[SorchaEncryption] Module loaded");
