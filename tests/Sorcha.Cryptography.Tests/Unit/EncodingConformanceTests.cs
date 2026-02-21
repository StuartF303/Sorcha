// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Buffers.Text;
using System.Text;
using FluentAssertions;
using Xunit;

namespace Sorcha.Cryptography.Tests.Unit;

/// <summary>
/// Conformance tests for Base64url encoding to ensure cryptographic operations
/// are not affected by the encoding migration from Base64 to Base64url.
/// </summary>
public class EncodingConformanceTests
{
    /// <summary>
    /// Valid Base64url alphabet: A-Z, a-z, 0-9, -, _
    /// </summary>
    private const string Base64UrlAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

    [Fact]
    public void Base64Url_EncodeKnownVector_ProducesExpectedOutput()
    {
        var input = Encoding.UTF8.GetBytes("Hello, Sorcha!");
        var result = Base64Url.EncodeToString(input);

        result.Should().Be("SGVsbG8sIFNvcmNoYSE");
    }

    [Fact]
    public void Base64Url_DecodeKnownVector_ProducesExpectedBytes()
    {
        var result = Base64Url.DecodeFromChars("SGVsbG8sIFNvcmNoYSE");

        Encoding.UTF8.GetString(result).Should().Be("Hello, Sorcha!");
    }

    [Fact]
    public void Base64Url_NeverContainsPlusSlashEquals()
    {
        // Test with bytes that produce +, /, = in standard Base64
        var problematicBytes = new byte[] { 0x3B, 0x7F, 0xBE, 0x3F, 0xEF, 0xBB, 0xBF, 0xFF };
        var standardBase64 = Convert.ToBase64String(problematicBytes);
        var base64url = Base64Url.EncodeToString(problematicBytes);

        // Standard Base64 SHOULD contain special chars
        standardBase64.Should().ContainAny("+", "/", "=");

        // Base64url MUST NOT contain them
        base64url.Should().NotContainAny("+", "/", "=");

        // All chars must be in Base64url alphabet
        foreach (var c in base64url)
        {
            Base64UrlAlphabet.Should().Contain(c.ToString(),
                $"character '{c}' is not in Base64url alphabet");
        }
    }

    [Fact]
    public void Base64Url_RoundTrip_IsLossless()
    {
        // Test with various byte patterns
        var testCases = new[]
        {
            Array.Empty<byte>(),
            new byte[] { 0 },
            new byte[] { 0xFF },
            new byte[] { 0x00, 0xFF, 0x00, 0xFF },
            Encoding.UTF8.GetBytes("test data"),
            GenerateRandomBytes(256),
            GenerateRandomBytes(1024),
            GenerateAllByteValues()
        };

        foreach (var input in testCases)
        {
            var encoded = Base64Url.EncodeToString(input);
            var decoded = Base64Url.DecodeFromChars(encoded);

            decoded.Should().Equal(input, $"round-trip failed for input of length {input.Length}");
        }
    }

    [Fact]
    public void Base64Url_SameBytesAsBase64_JustDifferentAlphabet()
    {
        var input = new byte[] { 0x3B, 0x7F, 0xBE, 0x3F, 0xEF, 0xBB, 0xBF, 0xFF };

        var base64 = Convert.FromBase64String(Convert.ToBase64String(input));
        var base64url = Base64Url.DecodeFromChars(Base64Url.EncodeToString(input));

        // Both decode to identical bytes
        base64url.Should().Equal(base64);
        base64url.Should().Equal(input);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(100)]
    [InlineData(255)]
    [InlineData(256)]
    public void Base64Url_VariousLengths_AllRoundTrip(int length)
    {
        var input = GenerateRandomBytes(length);
        var encoded = Base64Url.EncodeToString(input);
        var decoded = Base64Url.DecodeFromChars(encoded);

        decoded.Should().Equal(input);
    }

    [Fact]
    public void Base64Url_OnlyContainsAlphabetChars()
    {
        // Generate many test cases to ensure alphabet compliance
        for (int i = 0; i < 100; i++)
        {
            var input = GenerateRandomBytes(i + 1);
            var encoded = Base64Url.EncodeToString(input);

            foreach (var c in encoded)
            {
                Base64UrlAlphabet.Should().Contain(c.ToString(),
                    $"character '{c}' (0x{(int)c:X2}) at position in encoding of {i + 1} bytes is not in Base64url alphabet");
            }
        }
    }

    private static byte[] GenerateRandomBytes(int length)
    {
        var bytes = new byte[length];
        // Use deterministic seed for reproducible tests
        var rng = new Random(42 + length);
        rng.NextBytes(bytes);
        return bytes;
    }

    private static byte[] GenerateAllByteValues()
    {
        var bytes = new byte[256];
        for (int i = 0; i < 256; i++)
            bytes[i] = (byte)i;
        return bytes;
    }
}
