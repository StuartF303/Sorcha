// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System;
using System.Buffers.Text;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Sorcha.Cryptography.Interfaces;
using Sorcha.TransactionHandler.Core;
using Sorcha.TransactionHandler.Enums;
using Sorcha.TransactionHandler.Interfaces;
using Sorcha.TransactionHandler.Models;
using Sorcha.TransactionHandler.Payload;
using Sorcha.TransactionHandler.Serialization;
using Sorcha.TransactionHandler.Services;
using Xunit;

namespace Sorcha.TransactionHandler.Tests.Serialization;

/// <summary>
/// Tests for JsonTransactionSerializer ContentType and ContentEncoding serialization.
/// </summary>
public class JsonTransactionSerializerTests
{
    private readonly Mock<ICryptoModule> _cryptoModule = new();
    private readonly Mock<IHashProvider> _hashProvider = new();
    private readonly Mock<ISymmetricCrypto> _symmetricCrypto = new();
    private readonly JsonTransactionSerializer _serializer;

    public JsonTransactionSerializerTests()
    {
        _serializer = new JsonTransactionSerializer(
            _cryptoModule.Object,
            _hashProvider.Object,
            _symmetricCrypto.Object);
    }

    [Fact]
    public async Task SerializeToJson_PayloadWithContentType_IncludesContentTypeInOutput()
    {
        // Arrange
        var payloadManager = new PayloadManager(
            _symmetricCrypto.Object, _cryptoModule.Object, _hashProvider.Object);

        await payloadManager.AddPayloadAsync(
            new byte[] { 1, 2, 3, 4 },
            new[] { "wallet1" },
            new PayloadOptions { ContentType = "application/json" });

        var transaction = new Transaction(
            _cryptoModule.Object, _hashProvider.Object, payloadManager, TransactionVersion.V1)
        {
            Recipients = new[] { "wallet1" },
            Timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var json = _serializer.SerializeToJson(transaction);
        using var doc = JsonDocument.Parse(json);
        var payloads = doc.RootElement.GetProperty("payloads");

        // Assert
        payloads.GetArrayLength().Should().Be(1);
        var payload = payloads[0];
        payload.GetProperty("contentType").GetString().Should().Be("application/json");
    }

    [Fact]
    public async Task SerializeToJson_PayloadWithoutContentType_SerializesContentTypeAsNull()
    {
        // Arrange
        var payloadManager = new PayloadManager(
            _symmetricCrypto.Object, _cryptoModule.Object, _hashProvider.Object);

        await payloadManager.AddPayloadAsync(
            new byte[] { 1, 2, 3, 4 },
            new[] { "wallet1" });

        var transaction = new Transaction(
            _cryptoModule.Object, _hashProvider.Object, payloadManager, TransactionVersion.V1)
        {
            Recipients = new[] { "wallet1" },
            Timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var json = _serializer.SerializeToJson(transaction);
        using var doc = JsonDocument.Parse(json);
        var payload = doc.RootElement.GetProperty("payloads")[0];

        // Assert — null contentType for legacy payloads
        payload.GetProperty("contentType").ValueKind.Should().Be(JsonValueKind.Null);
        payload.GetProperty("contentEncoding").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task SerializeToJson_PayloadContentType_RoundTripsViaJson()
    {
        // Arrange
        var payloadManager = new PayloadManager(
            _symmetricCrypto.Object, _cryptoModule.Object, _hashProvider.Object);

        await payloadManager.AddPayloadAsync(
            new byte[] { 10, 20, 30 },
            new[] { "wallet1" },
            new PayloadOptions { ContentType = "application/pdf" });

        var transaction = new Transaction(
            _cryptoModule.Object, _hashProvider.Object, payloadManager, TransactionVersion.V1)
        {
            Recipients = new[] { "wallet1" },
            Timestamp = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var json = _serializer.SerializeToJson(transaction);
        using var doc = JsonDocument.Parse(json);
        var payload = doc.RootElement.GetProperty("payloads")[0];

        // Assert
        payload.GetProperty("contentType").GetString().Should().Be("application/pdf");
    }

    // --- T016: Base64url serialization tests ---

    private const string Base64UrlAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

    private static void AssertBase64UrlOnly(string value, string fieldName)
    {
        foreach (var c in value)
        {
            Base64UrlAlphabet.Should().Contain(c.ToString(),
                $"field '{fieldName}' contains non-Base64url char '{c}'");
        }
        value.Should().NotContain("+", $"field '{fieldName}' should not contain '+'");
        value.Should().NotContain("/", $"field '{fieldName}' should not contain '/'");
        value.Should().NotContain("=", $"field '{fieldName}' should not contain '='");
    }

    [Fact]
    public async Task SerializeToJson_AllBinaryFields_UseBase64urlEncoding()
    {
        // Arrange — use bytes that produce +/=/characters in standard Base64
        var payloadManager = new PayloadManager(
            _symmetricCrypto.Object, _cryptoModule.Object, _hashProvider.Object);

        await payloadManager.AddPayloadAsync(
            new byte[] { 0x3B, 0x7F, 0xBE, 0x3F, 0xEF, 0xBB, 0xBF, 0xFF },
            new[] { "wallet1" });

        var transaction = new Transaction(
            _cryptoModule.Object, _hashProvider.Object, payloadManager, TransactionVersion.V1)
        {
            Recipients = new[] { "wallet1" },
            Timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        SetSignature(transaction, new byte[] { 0x3B, 0x7F, 0xBE, 0x3F, 0xEF, 0xBB, 0xBF, 0xFF });

        // Act
        var json = _serializer.SerializeToJson(transaction);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert — signature uses Base64url
        var sig = root.GetProperty("signature").GetString()!;
        AssertBase64UrlOnly(sig, "signature");

        // Assert — payload binary fields use Base64url
        var payload = root.GetProperty("payloads")[0];
        AssertBase64UrlOnly(payload.GetProperty("data").GetString()!, "data");
        AssertBase64UrlOnly(payload.GetProperty("iv").GetString()!, "iv");
        AssertBase64UrlOnly(payload.GetProperty("hash").GetString()!, "hash");
    }

    [Fact]
    public async Task SerializeToJson_BinaryFields_DecodeToCorrectBytes()
    {
        // Arrange
        var originalBytes = new byte[] { 0x3B, 0x7F, 0xBE, 0x3F, 0xEF, 0xBB, 0xBF, 0xFF };
        var payloadManager = new PayloadManager(
            _symmetricCrypto.Object, _cryptoModule.Object, _hashProvider.Object);

        await payloadManager.AddPayloadAsync(originalBytes, new[] { "wallet1" });

        var transaction = new Transaction(
            _cryptoModule.Object, _hashProvider.Object, payloadManager, TransactionVersion.V1)
        {
            Recipients = new[] { "wallet1" },
            Timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        SetSignature(transaction, originalBytes);

        // Act
        var json = _serializer.SerializeToJson(transaction);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Assert — decode Base64url back to original bytes
        var sigStr = root.GetProperty("signature").GetString()!;
        Base64Url.DecodeFromChars(sigStr).Should().Equal(originalBytes);

        var dataStr = root.GetProperty("payloads")[0].GetProperty("data").GetString()!;
        Base64Url.DecodeFromChars(dataStr).Should().Equal(originalBytes);
    }

    // --- T017: Legacy Base64 deserialization tests ---

    [Fact]
    public void DeserializeFromJson_DoesNotReconstructPayloads()
    {
        // Note: JsonTransactionSerializer.DeserializeFromJson() does not reconstruct
        // payloads (requires private key for encrypted data). This test verifies that
        // deserialization of basic properties works and payload count is zero.
        var json = """
        {
            "txId": "abc123",
            "version": 1,
            "timestamp": "2026-01-01T00:00:00Z",
            "recipients": ["wallet1"],
            "payloads": [{
                "id": 0,
                "data": "AQID",
                "iv": "AAAA",
                "hash": "AAAA",
                "contentType": "application/json",
                "contentEncoding": "base64"
            }]
        }
        """;

        // Act
        var transaction = _serializer.DeserializeFromJson(json);

        // Assert — payloads are not reconstructed (empty PayloadManager)
        transaction.PayloadManager.Count.Should().Be(0);
        transaction.Recipients.Should().Contain("wallet1");
    }

    // --- T048: Identity encoding tests ---

    private ITransaction CreateTransactionWithMockedPayload(
        byte[] data, string? contentType, string? contentEncoding)
    {
        var mockPayload = new Mock<IPayload>();
        mockPayload.Setup(p => p.Id).Returns(0);
        mockPayload.Setup(p => p.Type).Returns(PayloadType.Data);
        mockPayload.Setup(p => p.Data).Returns(data);
        mockPayload.Setup(p => p.IV).Returns(new byte[16]);
        mockPayload.Setup(p => p.Hash).Returns(new byte[32]);
        mockPayload.Setup(p => p.IsCompressed).Returns(false);
        mockPayload.Setup(p => p.OriginalSize).Returns(data.Length);
        mockPayload.Setup(p => p.GetInfo()).Returns(new PayloadInfo
        {
            Id = 0,
            Type = PayloadType.Data,
            OriginalSize = data.Length,
            CompressedSize = data.Length,
            IsCompressed = false,
            IsEncrypted = false,
            ContentType = contentType,
            ContentEncoding = contentEncoding,
            AccessibleBy = ["wallet1"]
        });

        var mockPayloadManager = new Mock<IPayloadManager>();
        mockPayloadManager.Setup(m => m.GetAllAsync())
            .ReturnsAsync(new[] { mockPayload.Object });
        mockPayloadManager.Setup(m => m.Count).Returns(1);

        var transaction = new Transaction(
            _cryptoModule.Object, _hashProvider.Object,
            mockPayloadManager.Object, TransactionVersion.V1)
        {
            Recipients = ["wallet1"],
            Timestamp = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        return transaction;
    }

    [Fact]
    public void SerializeToJson_IdentityEncoding_EmitsNativeJsonObject()
    {
        // Arrange — UTF-8 JSON bytes with identity encoding
        var jsonData = """{"name":"Alice","age":30}""";
        var dataBytes = Encoding.UTF8.GetBytes(jsonData);
        var transaction = CreateTransactionWithMockedPayload(
            dataBytes, "application/json", ContentEncodings.Identity);

        // Act
        var json = _serializer.SerializeToJson(transaction);
        using var doc = JsonDocument.Parse(json);
        var payload = doc.RootElement.GetProperty("payloads")[0];

        // Assert — data is a native JSON object, not a Base64url string
        payload.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Object);
        payload.GetProperty("data").GetProperty("name").GetString().Should().Be("Alice");
        payload.GetProperty("data").GetProperty("age").GetInt32().Should().Be(30);
        payload.GetProperty("contentEncoding").GetString().Should().Be("identity");
    }

    [Fact]
    public void SerializeToJson_IdentityEncoding_JsonArray_EmitsNativeArray()
    {
        // Arrange — JSON array with identity encoding
        var jsonData = """[1,2,3]""";
        var dataBytes = Encoding.UTF8.GetBytes(jsonData);
        var transaction = CreateTransactionWithMockedPayload(
            dataBytes, "application/json", ContentEncodings.Identity);

        // Act
        var json = _serializer.SerializeToJson(transaction);
        using var doc = JsonDocument.Parse(json);
        var payload = doc.RootElement.GetProperty("payloads")[0];

        // Assert — data is a native JSON array
        payload.GetProperty("data").ValueKind.Should().Be(JsonValueKind.Array);
        payload.GetProperty("data").GetArrayLength().Should().Be(3);
    }

    [Fact]
    public void SerializeToJson_Base64urlEncoding_EmitsString()
    {
        // Arrange — binary data with base64url encoding (default path)
        var dataBytes = new byte[] { 0x01, 0x02, 0x03, 0xFF };
        var transaction = CreateTransactionWithMockedPayload(
            dataBytes, "application/octet-stream", ContentEncodings.Base64Url);

        // Act
        var json = _serializer.SerializeToJson(transaction);
        using var doc = JsonDocument.Parse(json);
        var payload = doc.RootElement.GetProperty("payloads")[0];

        // Assert — data is a Base64url string
        payload.GetProperty("data").ValueKind.Should().Be(JsonValueKind.String);
        Base64Url.DecodeFromChars(payload.GetProperty("data").GetString()!)
            .Should().Equal(dataBytes);
    }

    [Fact]
    public void SerializeToJson_NullContentEncoding_EmitsBase64urlString()
    {
        // Arrange — legacy payload with no content encoding
        var dataBytes = new byte[] { 0x01, 0x02, 0x03 };
        var transaction = CreateTransactionWithMockedPayload(
            dataBytes, null, null);

        // Act
        var json = _serializer.SerializeToJson(transaction);
        using var doc = JsonDocument.Parse(json);
        var payload = doc.RootElement.GetProperty("payloads")[0];

        // Assert — falls through to Base64url (default)
        payload.GetProperty("data").ValueKind.Should().Be(JsonValueKind.String);
    }

    [Fact]
    public void SerializeToJson_IdentityEncoding_InvalidJson_ThrowsJsonException()
    {
        // Arrange — non-JSON bytes with identity encoding should fail
        var dataBytes = Encoding.UTF8.GetBytes("not valid json {{{");
        var transaction = CreateTransactionWithMockedPayload(
            dataBytes, "application/json", ContentEncodings.Identity);

        // Act & Assert — JsonSerializer.Deserialize<JsonElement> throws
        var act = () => _serializer.SerializeToJson(transaction);
        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void SerializeToJson_IdentityEncoding_RoundTripsData()
    {
        // Arrange — complex JSON structure
        var jsonData = """{"items":[{"id":1,"value":"test"},{"id":2,"value":"data"}],"total":2}""";
        var dataBytes = Encoding.UTF8.GetBytes(jsonData);
        var transaction = CreateTransactionWithMockedPayload(
            dataBytes, "application/json", ContentEncodings.Identity);

        // Act
        var json = _serializer.SerializeToJson(transaction);
        using var doc = JsonDocument.Parse(json);
        var dataElement = doc.RootElement.GetProperty("payloads")[0].GetProperty("data");

        // Assert — structure preserved
        dataElement.GetProperty("total").GetInt32().Should().Be(2);
        dataElement.GetProperty("items").GetArrayLength().Should().Be(2);
        dataElement.GetProperty("items")[0].GetProperty("value").GetString().Should().Be("test");
    }

    private static void SetSignature(Transaction transaction, byte[] signature)
    {
        typeof(Transaction)
            .GetProperty(nameof(Transaction.Signature))!
            .SetValue(transaction, signature);
    }
}
