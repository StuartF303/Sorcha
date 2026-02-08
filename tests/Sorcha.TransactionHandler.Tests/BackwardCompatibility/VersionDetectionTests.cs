using System;
using System.Text.Json;
using Xunit;
using Sorcha.TransactionHandler.Enums;
using Sorcha.TransactionHandler.Versioning;

namespace Sorcha.TransactionHandler.Tests.BackwardCompatibility;

/// <summary>
/// Tests for detecting transaction versions from various formats.
/// </summary>
public class VersionDetectionTests
{
    private readonly VersionDetector _versionDetector;

    public VersionDetectionTests()
    {
        _versionDetector = new VersionDetector();
    }

    [Fact]
    public void DetectVersion_FromBinary_V1_ShouldDetectCorrectly()
    {
        // Arrange
        var data = BitConverter.GetBytes((uint)1);

        // Act
        var detectedVersion = _versionDetector.DetectVersion(data);

        // Assert
        Assert.Equal(TransactionVersion.V1, detectedVersion);
    }

    [Fact]
    public void DetectVersion_FromJson_V1_ShouldDetectCorrectly()
    {
        // Arrange
        var json = "{\"version\": 1, \"txId\": \"test123\"}";

        // Act
        var detectedVersion = _versionDetector.DetectVersion(json);

        // Assert
        Assert.Equal(TransactionVersion.V1, detectedVersion);
    }

    [Fact]
    public void DetectVersion_FromBinary_V1Format_ShouldDetect()
    {
        // Arrange - Simulate V1 transaction format with additional data
        var v1Data = new byte[100];
        BitConverter.GetBytes((uint)1).CopyTo(v1Data, 0);

        // Act
        var version = _versionDetector.DetectVersion(v1Data);

        // Assert
        Assert.Equal(TransactionVersion.V1, version);
    }

    [Fact]
    public void DetectVersion_FromJson_V1Format_ShouldDetect()
    {
        // Arrange - V1 JSON format with full field set
        var v1Json = @"{
            ""version"": 1,
            ""txId"": ""v1_transaction_id"",
            ""timestamp"": ""2023-01-01T00:00:00Z"",
            ""senderWallet"": ""ws1sender"",
            ""recipients"": [""ws1recipient1""],
            ""metadata"": {""type"": ""transfer""},
            ""payloads"": []
        }";

        // Act
        var version = _versionDetector.DetectVersion(v1Json);

        // Assert
        Assert.Equal(TransactionVersion.V1, version);
    }

    [Theory]
    [InlineData((uint)2)]
    [InlineData((uint)3)]
    [InlineData((uint)4)]
    [InlineData((uint)99)]
    public void DetectVersion_FromBinary_UnsupportedVersion_ShouldThrow(uint versionNumber)
    {
        // Arrange
        var unsupportedData = BitConverter.GetBytes(versionNumber);

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => _versionDetector.DetectVersion(unsupportedData));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(99)]
    public void DetectVersion_FromJson_UnsupportedVersion_ShouldThrow(uint versionNumber)
    {
        // Arrange
        var json = $"{{\"version\": {versionNumber}, \"txId\": \"test123\"}}";

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => _versionDetector.DetectVersion(json));
    }

    [Fact]
    public void DetectVersion_InsufficientData_ShouldThrow()
    {
        // Arrange - Only 2 bytes
        var insufficientData = new byte[] { 1, 0 };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _versionDetector.DetectVersion(insufficientData));
    }

    [Fact]
    public void DetectVersion_NullBinaryData_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _versionDetector.DetectVersion((byte[])null!));
    }

    [Fact]
    public void DetectVersion_NullJsonData_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _versionDetector.DetectVersion((string)null!));
    }

    [Fact]
    public void DetectVersion_InvalidJson_ShouldThrow()
    {
        // Arrange
        var invalidJson = "{invalid json format}";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _versionDetector.DetectVersion(invalidJson));
    }

    [Fact]
    public void DetectVersion_JsonWithoutVersion_ShouldThrow()
    {
        // Arrange
        var jsonWithoutVersion = @"{""txId"": ""test"", ""data"": ""test""}";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _versionDetector.DetectVersion(jsonWithoutVersion));
    }
}
