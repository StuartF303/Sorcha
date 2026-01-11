using System.Security.Cryptography;
using Sorcha.Wallet.Core.Encryption.Interfaces;
using Sorcha.Wallet.Core.Encryption.Providers;

namespace Sorcha.Wallet.Service.Tests.Encryption;

/// <summary>
/// Unit tests for Windows DPAPI encryption provider
/// </summary>
public class WindowsDpapiEncryptionProviderTests : IDisposable
{
    private readonly string _testKeyStorePath;
    private readonly ILogger<WindowsDpapiEncryptionProvider> _logger;

    public WindowsDpapiEncryptionProviderTests()
    {
        // Create temporary directory for test keys
        _testKeyStorePath = Path.Combine(Path.GetTempPath(), $"sorcha-test-keys-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testKeyStorePath);

        // Setup logger
        _logger = Mock.Of<ILogger<WindowsDpapiEncryptionProvider>>();
    }

    public void Dispose()
    {
        // Cleanup test directory
        if (Directory.Exists(_testKeyStorePath))
        {
            try
            {
                Directory.Delete(_testKeyStorePath, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    [Fact]
    public void Constructor_ShouldThrowPlatformNotSupportedException_OnNonWindowsPlatforms()
    {
        // Arrange & Act & Assert
        if (!OperatingSystem.IsWindows())
        {
            Action act = () => new WindowsDpapiEncryptionProvider(
                keyStorePath: _testKeyStorePath,
                defaultKeyId: "test-key",
                scope: DataProtectionScope.LocalMachine,
                logger: _logger);

            act.Should().Throw<PlatformNotSupportedException>()
                .WithMessage("*Windows DPAPI*only available on Windows*");
        }
    }

    [Fact]
    public void Constructor_ShouldCreateKeyStoreDirectory_IfNotExists()
    {
        // Skip if not on Windows
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange
        var nonExistentPath = Path.Combine(_testKeyStorePath, "subdir");

        // Act
        var provider = new WindowsDpapiEncryptionProvider(
            keyStorePath: nonExistentPath,
            defaultKeyId: "test-key",
            scope: DataProtectionScope.CurrentUser,
            logger: _logger);

        // Assert
        Directory.Exists(nonExistentPath).Should().BeTrue();
    }

    [Fact]
    public void GetDefaultKeyId_ShouldReturnConfiguredDefaultKeyId()
    {
        // Skip if not on Windows
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange
        var expectedKeyId = "test-key-2025";
        var provider = new WindowsDpapiEncryptionProvider(
            keyStorePath: _testKeyStorePath,
            defaultKeyId: expectedKeyId,
            scope: DataProtectionScope.CurrentUser,
            logger: _logger);

        // Act
        var actualKeyId = provider.GetDefaultKeyId();

        // Assert
        actualKeyId.Should().Be(expectedKeyId);
    }

    [Fact]
    public async Task EncryptAsync_ShouldEncryptData_Successfully()
    {
        // Skip if not on Windows
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange
        var provider = new WindowsDpapiEncryptionProvider(
            keyStorePath: _testKeyStorePath,
            defaultKeyId: "test-key",
            scope: DataProtectionScope.CurrentUser,
            logger: _logger);

        var plaintext = "Hello, World!"u8.ToArray();
        var keyId = "test-encryption-key";

        // Act
        var ciphertext = await provider.EncryptAsync(plaintext, keyId);

        // Assert
        ciphertext.Should().NotBeNullOrEmpty();
        ciphertext.Should().NotBe(Convert.ToBase64String(plaintext));
    }

    [Fact]
    public async Task EncryptAsync_ShouldThrowArgumentException_WhenPlaintextIsEmpty()
    {
        // Skip if not on Windows
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange
        var provider = new WindowsDpapiEncryptionProvider(
            keyStorePath: _testKeyStorePath,
            defaultKeyId: "test-key",
            scope: DataProtectionScope.CurrentUser,
            logger: _logger);

        var emptyPlaintext = Array.Empty<byte>();
        var keyId = "test-key";

        // Act
        Func<Task> act = async () => await provider.EncryptAsync(emptyPlaintext, keyId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Plaintext cannot be null or empty*");
    }

    [Fact]
    public async Task EncryptAsync_ShouldThrowArgumentException_WhenKeyIdIsEmpty()
    {
        // Skip if not on Windows
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange
        var provider = new WindowsDpapiEncryptionProvider(
            keyStorePath: _testKeyStorePath,
            defaultKeyId: "test-key",
            scope: DataProtectionScope.CurrentUser,
            logger: _logger);

        var plaintext = "test"u8.ToArray();

        // Act
        Func<Task> act = async () => await provider.EncryptAsync(plaintext, string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Key ID cannot be null or empty*");
    }

    [Fact]
    public async Task DecryptAsync_ShouldDecryptData_Successfully()
    {
        // Skip if not on Windows
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange
        var provider = new WindowsDpapiEncryptionProvider(
            keyStorePath: _testKeyStorePath,
            defaultKeyId: "test-key",
            scope: DataProtectionScope.CurrentUser,
            logger: _logger);

        var originalPlaintext = "Hello, World!"u8.ToArray();
        var keyId = "test-key";

        var ciphertext = await provider.EncryptAsync(originalPlaintext, keyId);

        // Act
        var decryptedPlaintext = await provider.DecryptAsync(ciphertext, keyId);

        // Assert
        decryptedPlaintext.Should().Equal(originalPlaintext);
    }

    [Fact]
    public async Task DecryptAsync_ShouldDecryptData_UsingCachedKey()
    {
        // Skip if not on Windows
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange
        var provider = new WindowsDpapiEncryptionProvider(
            keyStorePath: _testKeyStorePath,
            defaultKeyId: "test-key",
            scope: DataProtectionScope.CurrentUser,
            logger: _logger);

        var originalPlaintext = "Cached key test"u8.ToArray();
        var keyId = "cached-test-key";

        var ciphertext = await provider.EncryptAsync(originalPlaintext, keyId);

        // Act - Second encryption should use cached key
        var ciphertext2 = await provider.EncryptAsync("Another message"u8.ToArray(), keyId);
        var decryptedPlaintext = await provider.DecryptAsync(ciphertext, keyId);

        // Assert
        decryptedPlaintext.Should().Equal(originalPlaintext);
    }

    [Fact]
    public async Task DecryptAsync_ShouldThrowCryptographicException_ForInvalidCiphertext()
    {
        // Skip if not on Windows
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange
        var provider = new WindowsDpapiEncryptionProvider(
            keyStorePath: _testKeyStorePath,
            defaultKeyId: "test-key",
            scope: DataProtectionScope.CurrentUser,
            logger: _logger);

        var invalidCiphertext = Convert.ToBase64String("invalid"u8.ToArray());
        var keyId = "test-key";

        // Act
        Func<Task> act = async () => await provider.DecryptAsync(invalidCiphertext, keyId);

        // Assert
        await act.Should().ThrowAsync<CryptographicException>();
    }

    [Fact]
    public async Task DecryptAsync_ShouldThrowCryptographicException_ForTamperedCiphertext()
    {
        // Skip if not on Windows
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange
        var provider = new WindowsDpapiEncryptionProvider(
            keyStorePath: _testKeyStorePath,
            defaultKeyId: "test-key",
            scope: DataProtectionScope.CurrentUser,
            logger: _logger);

        var plaintext = "Hello, World!"u8.ToArray();
        var keyId = "test-key";

        var ciphertext = await provider.EncryptAsync(plaintext, keyId);

        // Tamper with ciphertext (flip a byte in the middle)
        var ciphertextBytes = Convert.FromBase64String(ciphertext);
        ciphertextBytes[ciphertextBytes.Length / 2] ^= 0xFF;
        var tamperedCiphertext = Convert.ToBase64String(ciphertextBytes);

        // Act
        Func<Task> act = async () => await provider.DecryptAsync(tamperedCiphertext, keyId);

        // Assert
        await act.Should().ThrowAsync<CryptographicException>();
    }

    [Fact]
    public async Task CreateKeyAsync_ShouldCreateKey_Successfully()
    {
        // Skip if not on Windows
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange
        var provider = new WindowsDpapiEncryptionProvider(
            keyStorePath: _testKeyStorePath,
            defaultKeyId: "test-key",
            scope: DataProtectionScope.CurrentUser,
            logger: _logger);

        var keyId = "new-test-key";

        // Act
        await provider.CreateKeyAsync(keyId);

        // Assert
        var keyExists = await provider.KeyExistsAsync(keyId);
        keyExists.Should().BeTrue();

        // Verify key file was created
        var expectedKeyFile = Path.Combine(_testKeyStorePath, $"{keyId}.key");
        File.Exists(expectedKeyFile).Should().BeTrue();
    }

    [Fact]
    public async Task CreateKeyAsync_ShouldThrowArgumentException_WhenKeyIdIsEmpty()
    {
        // Skip if not on Windows
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange
        var provider = new WindowsDpapiEncryptionProvider(
            keyStorePath: _testKeyStorePath,
            defaultKeyId: "test-key",
            scope: DataProtectionScope.CurrentUser,
            logger: _logger);

        // Act
        Func<Task> act = async () => await provider.CreateKeyAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Key ID cannot be null or empty*");
    }

    [Fact]
    public async Task KeyExistsAsync_ShouldReturnTrue_WhenKeyExists()
    {
        // Skip if not on Windows
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange
        var provider = new WindowsDpapiEncryptionProvider(
            keyStorePath: _testKeyStorePath,
            defaultKeyId: "test-key",
            scope: DataProtectionScope.CurrentUser,
            logger: _logger);

        var keyId = "existing-key";
        await provider.CreateKeyAsync(keyId);

        // Act
        var exists = await provider.KeyExistsAsync(keyId);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task KeyExistsAsync_ShouldReturnFalse_WhenKeyDoesNotExist()
    {
        // Skip if not on Windows
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange
        var provider = new WindowsDpapiEncryptionProvider(
            keyStorePath: _testKeyStorePath,
            defaultKeyId: "test-key",
            scope: DataProtectionScope.CurrentUser,
            logger: _logger);

        var keyId = "non-existent-key";

        // Act
        var exists = await provider.KeyExistsAsync(keyId);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task EncryptDecrypt_ShouldHandleMultipleKeys_Independently()
    {
        // Skip if not on Windows
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange
        var provider = new WindowsDpapiEncryptionProvider(
            keyStorePath: _testKeyStorePath,
            defaultKeyId: "test-key",
            scope: DataProtectionScope.CurrentUser,
            logger: _logger);

        var plaintext1 = "Message 1"u8.ToArray();
        var plaintext2 = "Message 2"u8.ToArray();
        var keyId1 = "key-1";
        var keyId2 = "key-2";

        // Act
        var ciphertext1 = await provider.EncryptAsync(plaintext1, keyId1);
        var ciphertext2 = await provider.EncryptAsync(plaintext2, keyId2);

        var decrypted1 = await provider.DecryptAsync(ciphertext1, keyId1);
        var decrypted2 = await provider.DecryptAsync(ciphertext2, keyId2);

        // Assert
        decrypted1.Should().Equal(plaintext1);
        decrypted2.Should().Equal(plaintext2);
        ciphertext1.Should().NotBe(ciphertext2);
    }

    [Fact]
    public async Task Provider_ShouldLoadExistingKeys_OnInitialization()
    {
        // Skip if not on Windows
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange - Create provider and key
        var keyId = "persistent-key";
        var plaintext = "Persistent data"u8.ToArray();

        var provider1 = new WindowsDpapiEncryptionProvider(
            keyStorePath: _testKeyStorePath,
            defaultKeyId: "test-key",
            scope: DataProtectionScope.CurrentUser,
            logger: _logger);

        var ciphertext = await provider1.EncryptAsync(plaintext, keyId);

        // Act - Create new provider instance (should load existing keys)
        var provider2 = new WindowsDpapiEncryptionProvider(
            keyStorePath: _testKeyStorePath,
            defaultKeyId: "test-key",
            scope: DataProtectionScope.CurrentUser,
            logger: Mock.Of<ILogger<WindowsDpapiEncryptionProvider>>());

        var decrypted = await provider2.DecryptAsync(ciphertext, keyId);

        // Assert
        decrypted.Should().Equal(plaintext);
    }

    [Fact]
    public async Task EncryptAsync_ShouldProduceDifferentCiphertexts_ForSameData()
    {
        // Skip if not on Windows
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange
        var provider = new WindowsDpapiEncryptionProvider(
            keyStorePath: _testKeyStorePath,
            defaultKeyId: "test-key",
            scope: DataProtectionScope.CurrentUser,
            logger: _logger);

        var plaintext = "Same message"u8.ToArray();
        var keyId = "test-key";

        // Act - Encrypt same data twice
        var ciphertext1 = await provider.EncryptAsync(plaintext, keyId);
        var ciphertext2 = await provider.EncryptAsync(plaintext, keyId);

        // Assert - Should be different due to random nonce
        ciphertext1.Should().NotBe(ciphertext2);

        // But both should decrypt to same plaintext
        var decrypted1 = await provider.DecryptAsync(ciphertext1, keyId);
        var decrypted2 = await provider.DecryptAsync(ciphertext2, keyId);

        decrypted1.Should().Equal(plaintext);
        decrypted2.Should().Equal(plaintext);
    }

    [Fact]
    public async Task Provider_ShouldHandleLargeData_Successfully()
    {
        // Skip if not on Windows
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange
        var provider = new WindowsDpapiEncryptionProvider(
            keyStorePath: _testKeyStorePath,
            defaultKeyId: "test-key",
            scope: DataProtectionScope.CurrentUser,
            logger: _logger);

        // Create large plaintext (1MB)
        var plaintext = new byte[1024 * 1024];
        Random.Shared.NextBytes(plaintext);
        var keyId = "large-data-key";

        // Act
        var ciphertext = await provider.EncryptAsync(plaintext, keyId);
        var decrypted = await provider.DecryptAsync(ciphertext, keyId);

        // Assert
        decrypted.Should().Equal(plaintext);
    }
}
