// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System;

namespace Sorcha.TransactionHandler.Services;

/// <summary>
/// Centralized binary-to-text encoding/decoding with format detection and compression support.
/// </summary>
public interface IPayloadEncodingService
{
    /// <summary>
    /// Encodes binary data to a string representation based on the specified content encoding.
    /// </summary>
    /// <param name="bytes">The binary data to encode.</param>
    /// <param name="contentEncoding">The target encoding: "base64url", "identity", "br+base64url", "gzip+base64url".</param>
    /// <returns>The encoded string representation.</returns>
    /// <exception cref="ArgumentException">If contentEncoding is "base64" (legacy, write-prohibited).</exception>
    string EncodeToString(byte[] bytes, string contentEncoding);

    /// <summary>
    /// Decodes an encoded string back to binary data based on the specified content encoding.
    /// </summary>
    /// <param name="encoded">The encoded string.</param>
    /// <param name="contentEncoding">The encoding used: "base64url", "base64", "identity", "br+base64url", "gzip+base64url".
    /// Null is treated as "base64" (legacy fallback).</param>
    /// <returns>The decoded binary data.</returns>
    byte[] DecodeToBytes(string encoded, string? contentEncoding);

    /// <summary>
    /// Detects whether an encoded string uses legacy standard Base64 encoding
    /// by checking for '+' or '/' characters (not present in Base64url).
    /// </summary>
    /// <param name="encoded">The encoded string to inspect.</param>
    /// <returns>True if the string contains standard Base64 characters.</returns>
    bool DetectLegacyEncoding(string encoded);

    /// <summary>
    /// Resolves the appropriate content encoding for a payload based on its characteristics.
    /// </summary>
    /// <param name="contentType">The MIME type of the payload data.</param>
    /// <param name="dataSize">The size of the payload data in bytes.</param>
    /// <param name="isEncrypted">Whether the payload will be encrypted.</param>
    /// <returns>The resolved content encoding string.</returns>
    string ResolveContentEncoding(string? contentType, long dataSize, bool isEncrypted);

    /// <summary>
    /// Gets the configured compression threshold in bytes. Payloads below this size are not compressed.
    /// </summary>
    int CompressionThresholdBytes { get; }
}
