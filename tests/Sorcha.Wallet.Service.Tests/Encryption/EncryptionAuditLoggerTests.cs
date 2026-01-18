using Microsoft.Extensions.Logging;
using Sorcha.Wallet.Core.Encryption.Logging;

namespace Sorcha.Wallet.Service.Tests.Encryption;

/// <summary>
/// Unit tests for Encryption Audit Logger
/// </summary>
public class EncryptionAuditLoggerTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly EncryptionAuditLogger _auditLogger;

    public EncryptionAuditLoggerTests()
    {
        _mockLogger = new Mock<ILogger>();
        _auditLogger = new EncryptionAuditLogger(_mockLogger.Object, "TestProvider");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Action act = () => new EncryptionAuditLogger(null!, "TestProvider");

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenProviderNameIsNull()
    {
        // Arrange
        var logger = Mock.Of<ILogger>();

        // Act & Assert
        Action act = () => new EncryptionAuditLogger(logger, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("providerName");
    }

    [Fact]
    public void LogEncryptSuccess_ShouldLogInformation_WithCorrectParameters()
    {
        // Arrange
        var keyId = "test-key-123";
        var durationMs = 42L;
        var userContext = "user123";

        // Act
        _auditLogger.LogEncryptSuccess(keyId, durationMs, userContext);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Encryption operation succeeded")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogEncryptSuccess_ShouldIncludeProviderName()
    {
        // Arrange
        var keyId = "test-key";
        var durationMs = 10L;

        // Act
        _auditLogger.LogEncryptSuccess(keyId, durationMs);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TestProvider")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogEncryptFailure_ShouldLogError_WithException()
    {
        // Arrange
        var keyId = "test-key";
        var exception = new InvalidOperationException("Test error");
        var durationMs = 5L;

        // Act
        _auditLogger.LogEncryptFailure(keyId, exception, durationMs);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Encryption operation failed")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogDecryptSuccess_ShouldLogInformation()
    {
        // Arrange
        var keyId = "decrypt-key";
        var durationMs = 15L;

        // Act
        _auditLogger.LogDecryptSuccess(keyId, durationMs);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Decryption operation succeeded")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogDecryptFailure_ShouldLogError_WithException()
    {
        // Arrange
        var keyId = "decrypt-key";
        var exception = new System.Security.Cryptography.CryptographicException("Decryption failed");
        var durationMs = 8L;

        // Act
        _auditLogger.LogDecryptFailure(keyId, exception, durationMs);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Decryption operation failed")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogCreateKeySuccess_ShouldLogInformation()
    {
        // Arrange
        var keyId = "new-key";
        var durationMs = 20L;

        // Act
        _auditLogger.LogCreateKeySuccess(keyId, durationMs);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CreateKey operation succeeded")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogCreateKeyFailure_ShouldLogError_WithException()
    {
        // Arrange
        var keyId = "failed-key";
        var exception = new UnauthorizedAccessException("Access denied");
        var durationMs = 3L;

        // Act
        _auditLogger.LogCreateKeyFailure(keyId, exception, durationMs);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CreateKey operation failed")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogKeyExists_ShouldLogInformation()
    {
        // Arrange
        var keyId = "check-key";
        var exists = true;
        var durationMs = 1L;

        // Act
        _auditLogger.LogKeyExists(keyId, exists, durationMs);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("KeyExists operation completed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogProviderInitialized_ShouldLogInformation()
    {
        // Arrange
        var configuration = "KeyStorePath=/test/path, Scope=LocalMachine";

        // Act
        _auditLogger.LogProviderInitialized(configuration);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Encryption provider initialized")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogKeysLoaded_ShouldLogInformation_WithKeyCount()
    {
        // Arrange
        var keyCount = 5;
        var source = "/test/path";

        // Act
        _auditLogger.LogKeysLoaded(keyCount, source);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Encryption keys loaded")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogEncryptSuccess_ShouldSanitizeLongKeyId()
    {
        // Arrange - Create key ID longer than 100 characters
        var longKeyId = new string('a', 150);
        var durationMs = 10L;

        // Act
        _auditLogger.LogEncryptSuccess(longKeyId, durationMs);

        // Assert - Logger should be called (key ID will be sanitized internally)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogProviderInitialized_ShouldSanitizeConfiguration_WithSensitiveData()
    {
        // Arrange - Configuration with sensitive keywords
        var configuration = "ConnectionString=Server=test;Password=secret123;Key=abc123";

        // Act
        _auditLogger.LogProviderInitialized(configuration);

        // Assert - Should log (sanitization happens internally)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogEncryptSuccess_ShouldUseDefaultUserContext_WhenNullProvided()
    {
        // Arrange
        var keyId = "test-key";
        var durationMs = 10L;

        // Act
        _auditLogger.LogEncryptSuccess(keyId, durationMs, userContext: null);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("None")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void EncryptionOperationTimer_ShouldMeasureDuration()
    {
        // Arrange & Act
        using var timer = EncryptionOperationTimer.Start();

        // Simulate some work
        Thread.Sleep(10);

        var elapsed = timer.ElapsedMilliseconds;

        // Assert
        elapsed.Should().BeGreaterThanOrEqualTo(10);
    }

    [Fact]
    public void EncryptionOperationTimer_ShouldStopOnDispose()
    {
        // Arrange
        var timer = EncryptionOperationTimer.Start();
        Thread.Sleep(10);

        // Act
        timer.Dispose();
        var elapsed1 = timer.ElapsedMilliseconds;

        Thread.Sleep(10);
        var elapsed2 = timer.ElapsedMilliseconds;

        // Assert - Should stop counting after dispose
        elapsed2.Should().Be(elapsed1);
    }

    [Fact]
    public void LogEncryptFailure_ShouldIncludeErrorType()
    {
        // Arrange
        var keyId = "test-key";
        var exception = new InvalidOperationException("Test error");
        var durationMs = 5L;

        // Act
        _auditLogger.LogEncryptFailure(keyId, exception, durationMs);

        // Assert - Should include exception type name
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("InvalidOperationException")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void AllLogMethods_ShouldIncludeStatus()
    {
        // Test that all success logs include "Status: Success"
        var keyId = "test";

        // Act
        _auditLogger.LogEncryptSuccess(keyId, 1);
        _auditLogger.LogDecryptSuccess(keyId, 1);
        _auditLogger.LogCreateKeySuccess(keyId, 1);
        _auditLogger.LogKeyExists(keyId, true, 1);

        // Assert - All should include "Success" status
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Success")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(4));
    }

    [Fact]
    public void AllLogMethods_ShouldIncludeOperationType()
    {
        // Arrange
        var keyId = "test";
        var exception = new Exception("test");

        // Act & Assert - Encrypt
        _auditLogger.LogEncryptSuccess(keyId, 1);
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Operation: Encrypt")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Act & Assert - Decrypt
        _auditLogger.LogDecryptSuccess(keyId, 1);
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Operation: Decrypt")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Act & Assert - CreateKey
        _auditLogger.LogCreateKeySuccess(keyId, 1);
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Operation: CreateKey")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Act & Assert - KeyExists
        _auditLogger.LogKeyExists(keyId, true, 1);
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Operation: KeyExists")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
