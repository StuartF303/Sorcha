using Microsoft.JSInterop;

namespace Sorcha.UI.Core.Services.Encryption;

/// <summary>
/// Browser-based encryption provider using Web Crypto API
/// </summary>
public class BrowserEncryptionProvider : IEncryptionProvider
{
    private readonly IJSRuntime _jsRuntime;
    private readonly Lazy<Task<string>> _encryptionKey;

    public BrowserEncryptionProvider(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _encryptionKey = new Lazy<Task<string>>(async () => await GenerateKeyAsync());
    }

    /// <inheritdoc />
    public async Task<string> EncryptAsync(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            throw new ArgumentException("Plaintext cannot be null or empty", nameof(plaintext));
        }

        var key = await _encryptionKey.Value;
        return await _jsRuntime.InvokeAsync<string>(
            "EncryptionHelper.encrypt",
            plaintext,
            key
        );
    }

    /// <inheritdoc />
    public async Task<string> DecryptAsync(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext))
        {
            throw new ArgumentException("Ciphertext cannot be null or empty", nameof(ciphertext));
        }

        if (!ciphertext.Contains(':'))
        {
            throw new ArgumentException("Invalid ciphertext format. Expected: {iv}:{ciphertext}", nameof(ciphertext));
        }

        var key = await _encryptionKey.Value;
        return await _jsRuntime.InvokeAsync<string>(
            "EncryptionHelper.decrypt",
            ciphertext,
            key
        );
    }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("EncryptionHelper.isAvailable");
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<string> GenerateKeyAsync()
    {
        return await _jsRuntime.InvokeAsync<string>("EncryptionHelper.generateKey");
    }
}
