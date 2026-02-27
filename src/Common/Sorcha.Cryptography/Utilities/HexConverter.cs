// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors
using System;
using System.Linq;
using System.Text;

namespace Sorcha.Cryptography.Utilities;

/// <summary>
/// Provides hexadecimal encoding and decoding functionality.
/// </summary>
public static class HexConverter
{
    private const string HexCharsLower = "0123456789abcdef";
    private const string HexCharsUpper = "0123456789ABCDEF";

    /// <summary>
    /// Converts a byte array to a hexadecimal string.
    /// </summary>
    /// <param name="data">The data to convert.</param>
    /// <param name="uppercase">Whether to use uppercase letters.</param>
    /// <returns>The hexadecimal string.</returns>
    public static string ToHex(byte[] data, bool uppercase = false)
    {
        if (data == null || data.Length == 0)
            return string.Empty;

        return uppercase ? Convert.ToHexString(data) : Convert.ToHexString(data).ToLowerInvariant();
    }

    /// <summary>
    /// Converts a hexadecimal string to a byte array.
    /// </summary>
    /// <param name="hex">The hexadecimal string.</param>
    /// <returns>The byte array, or null if invalid.</returns>
    public static byte[]? FromHex(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            return Array.Empty<byte>();

        // Remove any whitespace or common prefixes
        hex = hex.Trim().Replace(" ", "").Replace("-", "");
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            hex = hex.Substring(2);

        // Check if valid length
        if (hex.Length % 2 != 0)
            return null;

        try
        {
            return Convert.FromHexString(hex);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Converts a byte array to a hexadecimal string with optional formatting.
    /// </summary>
    /// <param name="data">The data to convert.</param>
    /// <param name="separator">Optional separator between bytes (e.g., ":", "-", " ").</param>
    /// <param name="uppercase">Whether to use uppercase letters.</param>
    /// <returns>The formatted hexadecimal string.</returns>
    public static string ToFormattedHex(byte[] data, string separator = "", bool uppercase = false)
    {
        if (data == null || data.Length == 0)
            return string.Empty;

        string hexChars = uppercase ? HexCharsUpper : HexCharsLower;
        var result = new StringBuilder(data.Length * (2 + separator.Length));

        for (int i = 0; i < data.Length; i++)
        {
            if (i > 0 && !string.IsNullOrEmpty(separator))
                result.Append(separator);

            result.Append(hexChars[data[i] >> 4]);
            result.Append(hexChars[data[i] & 0xF]);
        }

        return result.ToString();
    }

    /// <summary>
    /// Validates whether a string is valid hexadecimal.
    /// </summary>
    /// <param name="hex">The string to validate.</param>
    /// <returns>True if the string is valid hexadecimal.</returns>
    public static bool IsValidHex(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            return false;

        hex = hex.Trim().Replace(" ", "").Replace("-", "");
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            hex = hex.Substring(2);

        if (hex.Length % 2 != 0)
            return false;

        return hex.All(c => Uri.IsHexDigit(c));
    }
}
