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

    [Theory]
    [InlineData(1, TransactionVersion.V1)]
    [InlineData(2, TransactionVersion.V2)]
    [InlineData(3, TransactionVersion.V3)]
    [InlineData(4, TransactionVersion.V4)]
    public void DetectVersion_FromBinary_AllVersions_ShouldDetectCorrectly(uint versionNumber, TransactionVersion expectedVersion)
    {
        // Arrange - Create binary data with version number
        var data = BitConverter.GetBytes(versionNumber);

        // Act
        var detectedVersion = _versionDetector.DetectVersion(data);

        // Assert
        Assert.Equal(expectedVersion, detectedVersion);
    }

    [Theory]
    [InlineData(1, TransactionVersion.V1)]
    [InlineData(2, TransactionVersion.V2)]
    [InlineData(3, TransactionVersion.V3)]
    [InlineData(4, TransactionVersion.V4)]
    public void DetectVersion_FromJson_AllVersions_ShouldDetectCorrectly(uint versionNumber, TransactionVersion expectedVersion)
    {
        // Arrange
        var json = $"{{\"version\": {versionNumber}, \"txId\": \"test123\"}}";

        // Act
        var detectedVersion = _versionDetector.DetectVersion(json);

        // Assert
        Assert.Equal(expectedVersion, detectedVersion);
    }

    [Fact]
    public void DetectVersion_FromBinary_V1Format_ShouldDetect()
    {
        // Arrange - Simulate V1 transaction format
        var v1Data = new byte[100];
        BitConverter.GetBytes((uint)1).CopyTo(v1Data, 0);

        // Act
        var version = _versionDetector.DetectVersion(v1Data);

        // Assert
        Assert.Equal(TransactionVersion.V1, version);
    }

    [Fact]
    public void DetectVersion_FromBinary_V2Format_ShouldDetect()
    {
        // Arrange - Simulate V2 transaction format
        var v2Data = new byte[100];
        BitConverter.GetBytes((uint)2).CopyTo(v2Data, 0);

        // Act
        var version = _versionDetector.DetectVersion(v2Data);

        // Assert
        Assert.Equal(TransactionVersion.V2, version);
    }

    [Fact]
    public void DetectVersion_FromBinary_V3Format_ShouldDetect()
    {
        // Arrange - Simulate V3 transaction format
        var v3Data = new byte[100];
        BitConverter.GetBytes((uint)3).CopyTo(v3Data, 0);

        // Act
        var version = _versionDetector.DetectVersion(v3Data);

        // Assert
        Assert.Equal(TransactionVersion.V3, version);
    }

    [Fact]
    public void DetectVersion_FromJson_V1Format_ShouldDetect()
    {
        // Arrange - V1 JSON format
        var v1Json = @"{
            ""version"": 1,
            ""txId"": ""v1_transaction_id"",
            ""timestamp"": ""2023-01-01T00:00:00Z"",
            ""payload"": ""base64encodeddata""
        }";

        // Act
        var version = _versionDetector.DetectVersion(v1Json);

        // Assert
        Assert.Equal(TransactionVersion.V1, version);
    }

    [Fact]
    public void DetectVersion_FromJson_V2Format_ShouldDetect()
    {
        // Arrange - V2 JSON format with additional fields
        var v2Json = @"{
            ""version"": 2,
            ""txId"": ""v2_transaction_id"",
            ""timestamp"": ""2023-06-01T00:00:00Z"",
            ""senderWallet"": ""ws1sender"",
            ""recipients"": [""ws1recipient1""],
            ""payload"": ""base64encodeddata""
        }";

        // Act
        var version = _versionDetector.DetectVersion(v2Json);

        // Assert
        Assert.Equal(TransactionVersion.V2, version);
    }

    [Fact]
    public void DetectVersion_FromJson_V3Format_ShouldDetect()
    {
        // Arrange - V3 JSON format with metadata
        var v3Json = @"{
            ""version"": 3,
            ""txId"": ""v3_transaction_id"",
            ""timestamp"": ""2024-01-01T00:00:00Z"",
            ""metadata"": {""type"": ""transfer""},
            ""payloads"": []
        }";

        // Act
        var version = _versionDetector.DetectVersion(v3Json);

        // Assert
        Assert.Equal(TransactionVersion.V3, version);
    }

    [Fact]
    public void DetectVersion_UnsupportedVersion_ShouldThrow()
    {
        // Arrange
        var unsupportedData = BitConverter.GetBytes((uint)99);

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => _versionDetector.DetectVersion(unsupportedData));
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
