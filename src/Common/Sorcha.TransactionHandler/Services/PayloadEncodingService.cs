// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System;
using System.Buffers.Text;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Sorcha.TransactionHandler.Services;

/// <summary>
/// Centralized binary-to-text encoding/decoding with Base64url, legacy Base64, and compression support.
/// </summary>
public class PayloadEncodingService : IPayloadEncodingService
{
    /// <summary>
    /// Default compression threshold: 4096 bytes (4KB).
    /// </summary>
    public const int DefaultCompressionThresholdBytes = 4096;

    /// <inheritdoc/>
    public int CompressionThresholdBytes { get; }

    /// <summary>
    /// Initializes a new instance of PayloadEncodingService with default settings.
    /// </summary>
    public PayloadEncodingService()
        : this(DefaultCompressionThresholdBytes)
    {
    }

    /// <summary>
    /// Initializes a new instance of PayloadEncodingService with a custom compression threshold.
    /// </summary>
    /// <param name="compressionThresholdBytes">Minimum payload size in bytes before compression is applied.</param>
    public PayloadEncodingService(int compressionThresholdBytes)
    {
        CompressionThresholdBytes = compressionThresholdBytes > 0
            ? compressionThresholdBytes
            : DefaultCompressionThresholdBytes;
    }

    /// <inheritdoc/>
    public string EncodeToString(byte[] bytes, string contentEncoding)
    {
        ArgumentNullException.ThrowIfNull(contentEncoding);

        return contentEncoding switch
        {
            ContentEncodings.Base64Url => Base64Url.EncodeToString(bytes),
            ContentEncodings.Identity => Encoding.UTF8.GetString(bytes),
            ContentEncodings.BrotliBase64Url => Base64Url.EncodeToString(CompressBrotli(bytes)),
            ContentEncodings.GzipBase64Url => Base64Url.EncodeToString(CompressGzip(bytes)),
            ContentEncodings.Base64 => throw new ArgumentException(
                "Legacy 'base64' encoding must not be produced by new write operations. Use 'base64url' instead.",
                nameof(contentEncoding)),
            _ => throw new ArgumentException($"Unsupported content encoding: {contentEncoding}", nameof(contentEncoding))
        };
    }

    /// <inheritdoc/>
    public byte[] DecodeToBytes(string encoded, string? contentEncoding)
    {
        if (string.IsNullOrEmpty(encoded))
            return Array.Empty<byte>();

        // Null ContentEncoding = legacy fallback to standard Base64
        var encoding = contentEncoding ?? ContentEncodings.Base64;

        return encoding switch
        {
            ContentEncodings.Base64Url => Base64Url.DecodeFromChars(encoded),
            ContentEncodings.Base64 => Convert.FromBase64String(encoded),
            ContentEncodings.Identity => Encoding.UTF8.GetBytes(encoded),
            ContentEncodings.BrotliBase64Url => DecompressBrotli(Base64Url.DecodeFromChars(encoded)),
            ContentEncodings.GzipBase64Url => DecompressGzip(Base64Url.DecodeFromChars(encoded)),
            _ => throw new ArgumentException($"Unsupported content encoding: {encoding}", nameof(contentEncoding))
        };
    }

    /// <inheritdoc/>
    public bool DetectLegacyEncoding(string encoded)
    {
        if (string.IsNullOrEmpty(encoded))
            return false;

        // Standard Base64 uses '+' and '/' which are not in Base64url alphabet
        return encoded.Contains('+') || encoded.Contains('/');
    }

    /// <inheritdoc/>
    public string ResolveContentEncoding(string? contentType, long dataSize, bool isEncrypted)
    {
        // Encrypted payloads are always binary ciphertext
        if (isEncrypted)
        {
            return dataSize >= CompressionThresholdBytes
                ? ContentEncodings.BrotliBase64Url
                : ContentEncodings.Base64Url;
        }

        // Unencrypted JSON can be embedded natively if small enough
        if (IsJsonContentType(contentType))
        {
            return dataSize >= CompressionThresholdBytes
                ? ContentEncodings.BrotliBase64Url
                : ContentEncodings.Identity;
        }

        // Binary content
        return dataSize >= CompressionThresholdBytes
            ? ContentEncodings.BrotliBase64Url
            : ContentEncodings.Base64Url;
    }

    private static bool IsJsonContentType(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;

        return contentType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
            || contentType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] CompressBrotli(byte[] data)
    {
        using var output = new MemoryStream();
        using (var brotli = new BrotliStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            brotli.Write(data);
        }
        return output.ToArray();
    }

    private static byte[] DecompressBrotli(byte[] compressedData)
    {
        using var input = new MemoryStream(compressedData);
        using var brotli = new BrotliStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        brotli.CopyTo(output);
        return output.ToArray();
    }

    private static byte[] CompressGzip(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            gzip.Write(data);
        }
        return output.ToArray();
    }

    private static byte[] DecompressGzip(byte[] compressedData)
    {
        using var input = new MemoryStream(compressedData);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }
}

/// <summary>
/// Well-known content encoding constants.
/// </summary>
public static class ContentEncodings
{
    /// <summary>Base64url encoding (RFC 4648 §5, no padding).</summary>
    public const string Base64Url = "base64url";

    /// <summary>Standard Base64 encoding (legacy, read-only).</summary>
    public const string Base64 = "base64";

    /// <summary>Identity encoding — data is represented natively (e.g., JSON object).</summary>
    public const string Identity = "identity";

    /// <summary>Brotli-compressed then Base64url-encoded.</summary>
    public const string BrotliBase64Url = "br+base64url";

    /// <summary>Gzip-compressed then Base64url-encoded.</summary>
    public const string GzipBase64Url = "gzip+base64url";

    /// <summary>
    /// Decodes a Base64 or Base64url encoded string to bytes.
    /// Auto-detects the format by checking for standard Base64 characters (+, /, =).
    /// </summary>
    public static byte[] DecodeBase64Auto(string encoded)
    {
        if (string.IsNullOrEmpty(encoded))
            return Array.Empty<byte>();

        // Standard Base64 uses +, /, and = padding — if any are present, it's legacy
        if (encoded.Contains('+') || encoded.Contains('/') || encoded.Contains('='))
            return Convert.FromBase64String(encoded);

        return System.Buffers.Text.Base64Url.DecodeFromChars(encoded);
    }
}
