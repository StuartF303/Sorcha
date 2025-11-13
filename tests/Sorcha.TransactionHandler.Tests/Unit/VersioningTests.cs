using System;
using System.Text.Json;
using Xunit;
using Sorcha.TransactionHandler.Enums;
using Sorcha.TransactionHandler.Versioning;

namespace Sorcha.TransactionHandler.Tests.Unit;

/// <summary>
/// Unit tests for version detection and transaction factory.
/// </summary>
public class VersioningTests
{
    [Fact]
    public void DetectVersion_FromBinary_ShouldDetectV1()
    {
        // Arrange
        var detector = new VersionDetector();
        var data = BitConverter.GetBytes((uint)1);

        // Act
        var version = detector.DetectVersion(data);

        // Assert
        Assert.Equal(TransactionVersion.V1, version);
    }

    [Fact]
    public void DetectVersion_FromBinary_ShouldDetectV2()
    {
        // Arrange
        var detector = new VersionDetector();
        var data = BitConverter.GetBytes((uint)2);

        // Act
        var version = detector.DetectVersion(data);

        // Assert
        Assert.Equal(TransactionVersion.V2, version);
    }

    [Fact]
    public void DetectVersion_FromBinary_ShouldDetectV3()
    {
        // Arrange
        var detector = new VersionDetector();
        var data = BitConverter.GetBytes((uint)3);

        // Act
        var version = detector.DetectVersion(data);

        // Assert
        Assert.Equal(TransactionVersion.V3, version);
    }

    [Fact]
    public void DetectVersion_FromBinary_ShouldDetectV4()
    {
        // Arrange
        var detector = new VersionDetector();
        var data = BitConverter.GetBytes((uint)4);

        // Act
        var version = detector.DetectVersion(data);

        // Assert
        Assert.Equal(TransactionVersion.V4, version);
    }

    [Fact]
    public void DetectVersion_FromBinary_ShouldThrowOnInsufficientData()
    {
        // Arrange
        var detector = new VersionDetector();
        var data = new byte[] { 1, 2 }; // Only 2 bytes

        // Act & Assert
        Assert.Throws<ArgumentException>(() => detector.DetectVersion(data));
    }

    [Fact]
    public void DetectVersion_FromBinary_ShouldThrowOnNullData()
    {
        // Arrange
        var detector = new VersionDetector();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => detector.DetectVersion((byte[])null!));
    }

    [Fact]
    public void DetectVersion_FromBinary_ShouldThrowOnUnsupportedVersion()
    {
        // Arrange
        var detector = new VersionDetector();
        var data = BitConverter.GetBytes((uint)99);

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => detector.DetectVersion(data));
    }

    [Theory]
    [InlineData("{\"version\": 1}", TransactionVersion.V1)]
    [InlineData("{\"version\": 2}", TransactionVersion.V2)]
    [InlineData("{\"version\": 3}", TransactionVersion.V3)]
    [InlineData("{\"version\": 4}", TransactionVersion.V4)]
    public void DetectVersion_FromJson_ShouldDetectVersion(string json, TransactionVersion expected)
    {
        // Arrange
        var detector = new VersionDetector();

        // Act
        var version = detector.DetectVersion(json);

        // Assert
        Assert.Equal(expected, version);
    }

    [Fact]
    public void DetectVersion_FromJson_ShouldThrowOnNullJson()
    {
        // Arrange
        var detector = new VersionDetector();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => detector.DetectVersion((string)null!));
    }

    [Fact]
    public void DetectVersion_FromJson_ShouldThrowOnInvalidJson()
    {
        // Arrange
        var detector = new VersionDetector();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => detector.DetectVersion("{invalid}"));
    }

    [Fact]
    public void DetectVersion_FromJson_ShouldThrowOnMissingVersionProperty()
    {
        // Arrange
        var detector = new VersionDetector();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => detector.DetectVersion("{\"txId\": \"123\"}"));
    }

    [Fact]
    public void DetectVersion_FromJson_ShouldThrowOnUnsupportedVersion()
    {
        // Arrange
        var detector = new VersionDetector();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => detector.DetectVersion("{\"version\": 99}"));
    }
}
