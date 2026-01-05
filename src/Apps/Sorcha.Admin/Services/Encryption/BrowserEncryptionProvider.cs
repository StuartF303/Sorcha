using Microsoft.JSInterop;

namespace Sorcha.Admin.Services.Encryption;

/// <summary>
/// Browser-compatible encryption provider using Web Crypto API (SubtleCrypto).
/// Uses AES-256-GCM encryption with a key derived from browser fingerprint.
///
/// SECURITY NOTE: This provides encryption at rest in LocalStorage, but the encryption
/// key is derivable from JavaScript running in the same origin. This protects against
/// casual inspection and accidental exposure, but NOT against XSS attacks or determined
/// attackers with browser access. Use short token lifetimes and aggressive refresh as
/// additional mitigation.
/// </summary>
public class BrowserEncryptionProvider : IEncryptionProvider
{
    private readonly IJSRuntime _jsRuntime;
    private bool _isInitialized = false;

    public BrowserEncryptionProvider(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    }

    /// <summary>
    /// SubtleCrypto is available in all modern browsers.
    /// </summary>
    public bool IsAvailable => true;

    /// <summary>
    /// Initializes the encryption system by generating/loading the encryption key.
    /// This is called automatically on first use.
    /// </summary>
    private async Task EnsureInitializedAsync()
    {
        if (!_isInitialized)
        {
            // Retry logic to handle race conditions during initial load
            const int maxRetries = 3;
            const int delayMs = 100;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // Check if the JavaScript module exists
                    var moduleExists = await _jsRuntime.InvokeAsync<bool>(
                        "eval", "typeof window.SorchaEncryption !== 'undefined'");

                    if (!moduleExists)
                    {
                        if (attempt < maxRetries)
                        {
                            // Wait a bit for the script to load
                            await Task.Delay(delayMs * attempt);
                            continue;
                        }

                        throw new InvalidOperationException(
                            "SorchaEncryption JavaScript module is not loaded. Ensure encryption.js is included in index.html.");
                    }

                    // Initialize the encryption module in JavaScript
                    await _jsRuntime.InvokeVoidAsync("SorchaEncryption.initialize");
                    _isInitialized = true;
                    return;
                }
                catch (JSException) when (attempt < maxRetries)
                {
                    // Retry on JS exceptions (module might not be ready yet)
                    await Task.Delay(delayMs * attempt);
                }
                catch (JSException ex) when (attempt == maxRetries)
                {
                    throw new InvalidOperationException(
                        "Failed to initialize browser encryption after multiple attempts. Ensure encryption.js is loaded and Web Crypto API is available.", ex);
                }
            }
        }
    }

    /// <summary>
    /// Encrypts plaintext using AES-256-GCM via Web Crypto API.
    /// </summary>
    /// <param name="plaintext">The plaintext string to encrypt.</param>
    /// <returns>Encrypted data as a byte array (IV + ciphertext).</returns>
    public async Task<byte[]> EncryptAsync(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
            throw new ArgumentException("Plaintext cannot be null or empty.", nameof(plaintext));

        await LogAsync("debug", $"EncryptAsync called with plaintext length: {plaintext.Length}");

        await EnsureInitializedAsync();

        try
        {
            await LogAsync("debug", "Calling SorchaEncryption.encrypt via JSInterop...");
            // JavaScript returns either Base64 string or plaintext with marker
            var result = await _jsRuntime.InvokeAsync<string>(
                "SorchaEncryption.encrypt", plaintext);

            await LogAsync("debug", $"Encryption result: {result.Length} characters returned");

            // Check if this is fallback mode (plaintext with marker)
            if (result.StartsWith("PLAINTEXT:"))
            {
                await LogAsync("warn", "Using fallback mode - data is NOT encrypted");
                // Remove marker and convert plaintext to UTF-8 bytes
                var plaintextData = result.Substring("PLAINTEXT:".Length);
                var bytes = System.Text.Encoding.UTF8.GetBytes(plaintextData);
                await LogAsync("debug", $"Converted plaintext to {bytes.Length} UTF-8 bytes");
                return bytes;
            }

            // Normal encrypted mode - convert Base64 to bytes
            var encrypted = Convert.FromBase64String(result);
            await LogAsync("debug", $"Converted encrypted Base64 to {encrypted.Length} bytes");

            return encrypted;
        }
        catch (JSException ex)
        {
            await LogAsync("error", $"Encryption failed: {ex.Message}", new {
                ExceptionType = ex.GetType().Name,
                Message = ex.Message,
                StackTrace = ex.StackTrace
            });
            throw new InvalidOperationException($"Encryption failed in browser: {ex.Message}", ex);
        }
    }

    private async Task LogAsync(string level, string message, object? data = null)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync($"console.{level}", $"[BrowserEncryptionProvider] {message}", data ?? "");
        }
        catch
        {
            // Ignore logging errors
        }
    }

    /// <summary>
    /// Decrypts ciphertext using AES-256-GCM via Web Crypto API.
    /// </summary>
    /// <param name="ciphertext">The encrypted data to decrypt (IV + ciphertext).</param>
    /// <returns>Decrypted plaintext string.</returns>
    public async Task<string> DecryptAsync(byte[] ciphertext)
    {
        if (ciphertext == null || ciphertext.Length == 0)
            throw new ArgumentException("Ciphertext cannot be null or empty.", nameof(ciphertext));

        await EnsureInitializedAsync();

        try
        {
            // Check if this is plaintext (UTF-8 bytes, not Base64-encoded encrypted data)
            var text = System.Text.Encoding.UTF8.GetString(ciphertext);
            if (text.StartsWith("PLAINTEXT:"))
            {
                await LogAsync("warn", "Decrypting fallback mode data (NOT encrypted)");
                return text.Substring("PLAINTEXT:".Length);
            }

            // Normal encrypted mode - convert to Base64 for JavaScript
            var base64Ciphertext = Convert.ToBase64String(ciphertext);

            var decrypted = await _jsRuntime.InvokeAsync<string>(
                "SorchaEncryption.decrypt", base64Ciphertext);

            return decrypted;
        }
        catch (JSException ex)
        {
            throw new System.Security.Cryptography.CryptographicException(
                "Decryption failed. The data may be corrupted or tampered with.", ex);
        }
    }
}
