// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System;
using System.Text;

namespace Sorcha.TransactionHandler.Tests.TestData;

/// <summary>
/// Known test vectors for encoding conformance testing.
/// All values are pre-computed and deterministic.
/// </summary>
public static class EncodingTestVectors
{
    // --- Fixed input bytes ---

    /// <summary>Simple ASCII text: "Hello, Sorcha!"</summary>
    public static readonly byte[] SimpleText = Encoding.UTF8.GetBytes("Hello, Sorcha!");

    /// <summary>Binary data with all byte values that differ between Base64 and Base64url.</summary>
    public static readonly byte[] BinaryWithSpecialChars = new byte[]
    {
        0x3B, 0x7F, 0xBE, 0x3F, 0xEF, 0xBB, 0xBF, 0xFF
    };

    /// <summary>Empty byte array.</summary>
    public static readonly byte[] EmptyBytes = Array.Empty<byte>();

    /// <summary>Single zero byte.</summary>
    public static readonly byte[] SingleZeroByte = new byte[] { 0x00 };

    /// <summary>JSON payload for compression testing (must be > 4KB when repeated).</summary>
    public static readonly byte[] LargeJsonPayload = Encoding.UTF8.GetBytes(
        GenerateLargeJson());

    /// <summary>Small JSON payload (under 4KB threshold).</summary>
    public static readonly byte[] SmallJsonPayload = Encoding.UTF8.GetBytes(
        """{"name":"test","value":42,"active":true}""");

    // --- Expected Base64 outputs ---

    /// <summary>Standard Base64 encoding of SimpleText.</summary>
    public const string SimpleTextBase64 = "SGVsbG8sIFNvcmNoYSE=";

    /// <summary>Standard Base64 encoding of BinaryWithSpecialChars. Contains '+' and '/'.</summary>
    public const string BinarySpecialBase64 = "O3++P++7v/8=";

    // --- Expected Base64url outputs ---

    /// <summary>Base64url encoding of SimpleText (no padding).</summary>
    public const string SimpleTextBase64Url = "SGVsbG8sIFNvcmNoYSE";

    /// <summary>Base64url encoding of BinaryWithSpecialChars (- and _ instead of + and /).</summary>
    public const string BinarySpecialBase64Url = "O3--P--7v_8";

    // --- Expected SHA-256 hashes ---

    /// <summary>SHA-256 hash of SimpleText as hex string (for verification).</summary>
    public const string SimpleTextSha256Hex =
        "c6b9cb4c82b32c69e8e2e7f82ada2bf30c5d5a2cc69d0e7e9b7f9cc5f1c0e6a8";

    // --- Helpers ---

    /// <summary>
    /// Characters that appear in standard Base64 but NOT in Base64url.
    /// </summary>
    public static readonly char[] LegacyBase64OnlyChars = new[] { '+', '/', '=' };

    /// <summary>
    /// Valid Base64url alphabet characters.
    /// </summary>
    public const string Base64UrlAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

    private static string GenerateLargeJson()
    {
        var sb = new StringBuilder();
        sb.Append("{\"items\":[");
        for (int i = 0; i < 200; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append($"{{\"id\":{i},\"name\":\"item-{i}\",\"description\":\"This is a test item number {i} with some additional text to increase size\"}}");
        }
        sb.Append("]}");
        return sb.ToString();
    }
}
