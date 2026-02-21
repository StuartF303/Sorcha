// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System;
using System.Buffers.Text;
using System.Text;
using FluentAssertions;
using Sorcha.TransactionHandler.Services;
using Sorcha.TransactionHandler.Tests.TestData;
using Xunit;

namespace Sorcha.TransactionHandler.Tests.Services;

public class PayloadEncodingServiceTests
{
    private readonly PayloadEncodingService _service = new();

    // --- Base64url encode/decode round-trip ---

    [Fact]
    public void EncodeToString_Base64url_ProducesValidOutput()
    {
        var result = _service.EncodeToString(EncodingTestVectors.SimpleText, ContentEncodings.Base64Url);

        result.Should().Be(EncodingTestVectors.SimpleTextBase64Url);
    }

    [Fact]
    public void EncodeToString_Base64url_NeverContainsLegacyChars()
    {
        var result = _service.EncodeToString(EncodingTestVectors.BinaryWithSpecialChars, ContentEncodings.Base64Url);

        result.Should().NotContainAny("+", "/", "=");
        result.Should().Be(EncodingTestVectors.BinarySpecialBase64Url);
    }

    [Fact]
    public void DecodeToBytes_Base64url_RoundTrips()
    {
        var encoded = _service.EncodeToString(EncodingTestVectors.SimpleText, ContentEncodings.Base64Url);
        var decoded = _service.DecodeToBytes(encoded, ContentEncodings.Base64Url);

        decoded.Should().Equal(EncodingTestVectors.SimpleText);
    }

    [Fact]
    public void DecodeToBytes_Base64url_BinaryRoundTrips()
    {
        var encoded = _service.EncodeToString(EncodingTestVectors.BinaryWithSpecialChars, ContentEncodings.Base64Url);
        var decoded = _service.DecodeToBytes(encoded, ContentEncodings.Base64Url);

        decoded.Should().Equal(EncodingTestVectors.BinaryWithSpecialChars);
    }

    // --- Legacy Base64 decode ---

    [Fact]
    public void DecodeToBytes_LegacyBase64_DecodesCorrectly()
    {
        var decoded = _service.DecodeToBytes(EncodingTestVectors.SimpleTextBase64, ContentEncodings.Base64);

        decoded.Should().Equal(EncodingTestVectors.SimpleText);
    }

    [Fact]
    public void DecodeToBytes_NullEncoding_FallsBackToBase64()
    {
        var decoded = _service.DecodeToBytes(EncodingTestVectors.SimpleTextBase64, null);

        decoded.Should().Equal(EncodingTestVectors.SimpleText);
    }

    [Fact]
    public void EncodeToString_Base64_ThrowsOnWrite()
    {
        var act = () => _service.EncodeToString(EncodingTestVectors.SimpleText, ContentEncodings.Base64);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*legacy*");
    }

    // --- Identity encoding ---

    [Fact]
    public void EncodeToString_Identity_ReturnsUtf8String()
    {
        var jsonBytes = Encoding.UTF8.GetBytes("{\"key\":\"value\"}");
        var result = _service.EncodeToString(jsonBytes, ContentEncodings.Identity);

        result.Should().Be("{\"key\":\"value\"}");
    }

    [Fact]
    public void DecodeToBytes_Identity_ReturnsUtf8Bytes()
    {
        var result = _service.DecodeToBytes("{\"key\":\"value\"}", ContentEncodings.Identity);

        result.Should().Equal(Encoding.UTF8.GetBytes("{\"key\":\"value\"}"));
    }

    [Fact]
    public void EncodeToString_Identity_NonJsonData_ProducesUnparseableOutput()
    {
        // Identity encoding converts raw bytes to UTF-8 string regardless of content.
        // Invalid JSON is accepted by the encoding layer but will fail when consumers
        // attempt to parse it as JSON (e.g., JsonTransactionSerializer).
        var nonJsonBytes = Encoding.UTF8.GetBytes("not valid json {{{");
        var result = _service.EncodeToString(nonJsonBytes, ContentEncodings.Identity);

        result.Should().Be("not valid json {{{");

        // Attempting to parse as JSON should fail
        var act = () => System.Text.Json.JsonDocument.Parse(result);
        act.Should().Throw<System.Text.Json.JsonException>();
    }

    // --- Brotli compression ---

    [Fact]
    public void EncodeToString_Brotli_RoundTrips()
    {
        var encoded = _service.EncodeToString(EncodingTestVectors.LargeJsonPayload, ContentEncodings.BrotliBase64Url);
        var decoded = _service.DecodeToBytes(encoded, ContentEncodings.BrotliBase64Url);

        decoded.Should().Equal(EncodingTestVectors.LargeJsonPayload);
    }

    [Fact]
    public void EncodeToString_Brotli_CompressesData()
    {
        var encoded = _service.EncodeToString(EncodingTestVectors.LargeJsonPayload, ContentEncodings.BrotliBase64Url);

        // Brotli should significantly compress JSON data
        var compressedSize = Base64Url.DecodeFromChars(encoded).Length;
        compressedSize.Should().BeLessThan(EncodingTestVectors.LargeJsonPayload.Length);
    }

    // --- Gzip compression ---

    [Fact]
    public void EncodeToString_Gzip_RoundTrips()
    {
        var encoded = _service.EncodeToString(EncodingTestVectors.LargeJsonPayload, ContentEncodings.GzipBase64Url);
        var decoded = _service.DecodeToBytes(encoded, ContentEncodings.GzipBase64Url);

        decoded.Should().Equal(EncodingTestVectors.LargeJsonPayload);
    }

    // --- Legacy detection ---

    [Fact]
    public void DetectLegacyEncoding_WithPlusChar_ReturnsTrue()
    {
        _service.DetectLegacyEncoding("abc+def==").Should().BeTrue();
    }

    [Fact]
    public void DetectLegacyEncoding_WithSlashChar_ReturnsTrue()
    {
        _service.DetectLegacyEncoding("abc/def").Should().BeTrue();
    }

    [Fact]
    public void DetectLegacyEncoding_Base64urlString_ReturnsFalse()
    {
        _service.DetectLegacyEncoding("abc-def_ghi").Should().BeFalse();
    }

    [Fact]
    public void DetectLegacyEncoding_EmptyString_ReturnsFalse()
    {
        _service.DetectLegacyEncoding("").Should().BeFalse();
    }

    // --- Content encoding resolution ---

    [Fact]
    public void ResolveContentEncoding_JsonSmallUnencrypted_ReturnsIdentity()
    {
        var result = _service.ResolveContentEncoding("application/json", 100, isEncrypted: false);

        result.Should().Be(ContentEncodings.Identity);
    }

    [Fact]
    public void ResolveContentEncoding_JsonLargeUnencrypted_ReturnsBrotli()
    {
        var result = _service.ResolveContentEncoding("application/json", 5000, isEncrypted: false);

        result.Should().Be(ContentEncodings.BrotliBase64Url);
    }

    [Fact]
    public void ResolveContentEncoding_BinarySmallUnencrypted_ReturnsBase64Url()
    {
        var result = _service.ResolveContentEncoding("application/pdf", 100, isEncrypted: false);

        result.Should().Be(ContentEncodings.Base64Url);
    }

    [Fact]
    public void ResolveContentEncoding_EncryptedSmall_ReturnsBase64Url()
    {
        var result = _service.ResolveContentEncoding("application/json", 100, isEncrypted: true);

        result.Should().Be(ContentEncodings.Base64Url);
    }

    [Fact]
    public void ResolveContentEncoding_EncryptedLarge_ReturnsBrotli()
    {
        var result = _service.ResolveContentEncoding("application/json", 5000, isEncrypted: true);

        result.Should().Be(ContentEncodings.BrotliBase64Url);
    }

    [Fact]
    public void ResolveContentEncoding_NullContentType_ReturnsBase64Url()
    {
        var result = _service.ResolveContentEncoding(null, 100, isEncrypted: false);

        result.Should().Be(ContentEncodings.Base64Url);
    }

    // --- Empty/null handling ---

    [Fact]
    public void DecodeToBytes_EmptyString_ReturnsEmptyArray()
    {
        _service.DecodeToBytes("", ContentEncodings.Base64Url).Should().BeEmpty();
    }

    [Fact]
    public void EncodeToString_EmptyArray_ReturnsEmptyString()
    {
        _service.EncodeToString(Array.Empty<byte>(), ContentEncodings.Base64Url).Should().BeEmpty();
    }

    // --- Custom threshold ---

    [Fact]
    public void Constructor_CustomThreshold_IsRespected()
    {
        var service = new PayloadEncodingService(1024);

        service.CompressionThresholdBytes.Should().Be(1024);
        service.ResolveContentEncoding("application/json", 2000, isEncrypted: false)
            .Should().Be(ContentEncodings.BrotliBase64Url);
    }

    // --- T055: Compression threshold boundary tests ---

    [Fact]
    public void ResolveContentEncoding_BelowThreshold_NoCompression()
    {
        var belowThreshold = PayloadEncodingService.DefaultCompressionThresholdBytes - 1;

        _service.ResolveContentEncoding("application/octet-stream", belowThreshold, isEncrypted: false)
            .Should().Be(ContentEncodings.Base64Url, "binary below threshold should not compress");
        _service.ResolveContentEncoding("application/json", belowThreshold, isEncrypted: false)
            .Should().Be(ContentEncodings.Identity, "JSON below threshold should use identity");
        _service.ResolveContentEncoding("application/json", belowThreshold, isEncrypted: true)
            .Should().Be(ContentEncodings.Base64Url, "encrypted below threshold should use base64url");
    }

    [Fact]
    public void ResolveContentEncoding_AtThreshold_Compresses()
    {
        var atThreshold = PayloadEncodingService.DefaultCompressionThresholdBytes;

        _service.ResolveContentEncoding("application/octet-stream", atThreshold, isEncrypted: false)
            .Should().Be(ContentEncodings.BrotliBase64Url, "binary at threshold should compress");
        _service.ResolveContentEncoding("application/json", atThreshold, isEncrypted: false)
            .Should().Be(ContentEncodings.BrotliBase64Url, "JSON at threshold should compress");
        _service.ResolveContentEncoding("application/json", atThreshold, isEncrypted: true)
            .Should().Be(ContentEncodings.BrotliBase64Url, "encrypted at threshold should compress");
    }

    [Fact]
    public void ResolveContentEncoding_AboveThreshold_Compresses()
    {
        var aboveThreshold = PayloadEncodingService.DefaultCompressionThresholdBytes + 1;

        _service.ResolveContentEncoding("application/octet-stream", aboveThreshold, isEncrypted: false)
            .Should().Be(ContentEncodings.BrotliBase64Url);
        _service.ResolveContentEncoding("application/json", aboveThreshold, isEncrypted: false)
            .Should().Be(ContentEncodings.BrotliBase64Url);
    }

    // --- T058: Hash on compressed bytes ---

    [Fact]
    public void HashOnCompressedBytes_VerifiesWithoutDecompression()
    {
        // Simulate the compression pipeline: compress, encode, hash compressed bytes
        var original = Encoding.UTF8.GetBytes(new string('A', 5000)); // > 4KB
        var encoded = _service.EncodeToString(original, ContentEncodings.BrotliBase64Url);
        var compressedBytes = Base64Url.DecodeFromChars(encoded);

        // Hash the compressed representation
        var hash = System.Security.Cryptography.SHA256.HashData(compressedBytes);

        // After retrieval: decode (without decompression) and verify hash
        var retrievedCompressed = Base64Url.DecodeFromChars(encoded);
        var verifyHash = System.Security.Cryptography.SHA256.HashData(retrievedCompressed);

        verifyHash.Should().Equal(hash, "hash on compressed bytes should verify without decompression");
        compressedBytes.Length.Should().BeLessThan(original.Length, "compressed should be smaller");
    }

    [Fact]
    public void HashOnCompressedBytes_DiffersFromPlaintextHash()
    {
        var original = Encoding.UTF8.GetBytes(new string('B', 5000));
        var encoded = _service.EncodeToString(original, ContentEncodings.BrotliBase64Url);
        var compressedBytes = Base64Url.DecodeFromChars(encoded);

        var compressedHash = System.Security.Cryptography.SHA256.HashData(compressedBytes);
        var plaintextHash = System.Security.Cryptography.SHA256.HashData(original);

        compressedHash.Should().NotEqual(plaintextHash,
            "hash of compressed bytes differs from hash of plaintext");
    }

    // --- T036: Encoding round-trip stability for all ContentEncoding values ---

    [Theory]
    [InlineData(ContentEncodings.Base64Url)]
    [InlineData(ContentEncodings.Identity)]
    [InlineData(ContentEncodings.BrotliBase64Url)]
    [InlineData(ContentEncodings.GzipBase64Url)]
    public void EncodingRoundTrip_AllContentEncodings_ByteIdentical(string encoding)
    {
        var original = encoding == ContentEncodings.Identity
            ? Encoding.UTF8.GetBytes("{\"test\":\"data\"}")
            : new byte[] { 0x00, 0xFF, 0x3B, 0x7F, 0xBE, 0x3F, 0xEF, 0xBB, 0xBF, 0xFF };

        var encoded = _service.EncodeToString(original, encoding);
        var decoded = _service.DecodeToBytes(encoded, encoding);

        decoded.Should().Equal(original, $"round-trip for {encoding} should be byte-identical");
    }

    [Fact]
    public void DecodeBase64Auto_LegacyBase64_DecodesCorrectly()
    {
        var bytes = new byte[] { 0x3B, 0x7F, 0xBE, 0x3F };
        var legacy = Convert.ToBase64String(bytes);

        ContentEncodings.DecodeBase64Auto(legacy).Should().Equal(bytes);
    }

    [Fact]
    public void DecodeBase64Auto_Base64Url_DecodesCorrectly()
    {
        var bytes = new byte[] { 0x3B, 0x7F, 0xBE, 0x3F };
        var base64Url = Base64Url.EncodeToString(bytes);

        ContentEncodings.DecodeBase64Auto(base64Url).Should().Equal(bytes);
    }

    [Fact]
    public void DecodeBase64Auto_Empty_ReturnsEmpty()
    {
        ContentEncodings.DecodeBase64Auto("").Should().BeEmpty();
    }

    // --- Unsupported encoding ---

    [Fact]
    public void EncodeToString_UnsupportedEncoding_Throws()
    {
        var act = () => _service.EncodeToString(new byte[] { 1 }, "unknown");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void DecodeToBytes_UnsupportedEncoding_Throws()
    {
        var act = () => _service.DecodeToBytes("data", "unknown");

        act.Should().Throw<ArgumentException>();
    }
}
