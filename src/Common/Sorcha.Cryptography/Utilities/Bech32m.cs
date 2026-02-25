// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System;
using System.Collections.Generic;
using System.Text;

namespace Sorcha.Cryptography.Utilities;

/// <summary>
/// Provides Bech32m encoding and decoding (BIP-350) for ws2 PQC wallet addresses.
/// Bech32m uses a different checksum constant (0x2bc830a3) than Bech32 (0x01),
/// providing improved error detection for segwit v1+ addresses.
/// </summary>
public static class Bech32m
{
    private const string Charset = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
    private const uint Bech32mConst = 0x2bc830a3;
    private static readonly int[] Generator = [0x3b6a57b2, 0x26508e6d, 0x1ea119fa, 0x3d4233dd, 0x2a1462b3];

    /// <summary>
    /// Encodes data to Bech32m format.
    /// </summary>
    /// <param name="hrp">Human-readable part (e.g., "ws2").</param>
    /// <param name="data">The 8-bit data to encode.</param>
    /// <returns>The Bech32m encoded string.</returns>
    public static string Encode(string hrp, byte[] data)
    {
        if (string.IsNullOrEmpty(hrp))
            throw new ArgumentException("HRP cannot be null or empty", nameof(hrp));
        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        byte[] data5bit = ConvertBits(data, 8, 5, true);
        byte[] checksum = CreateChecksum(hrp, data5bit);
        byte[] combined = [.. data5bit, .. checksum];

        var result = new StringBuilder(hrp.Length + 1 + combined.Length);
        result.Append(hrp.ToLowerInvariant());
        result.Append('1');

        foreach (byte b in combined)
            result.Append(Charset[b]);

        return result.ToString();
    }

    /// <summary>
    /// Decodes a Bech32m encoded string.
    /// </summary>
    /// <param name="encoded">The Bech32m encoded string.</param>
    /// <returns>A tuple of (HRP, 8-bit data), or null if invalid.</returns>
    public static (string Hrp, byte[] Data)? Decode(string encoded)
    {
        if (string.IsNullOrEmpty(encoded))
            return null;

        encoded = encoded.ToLowerInvariant();

        int separatorPos = encoded.LastIndexOf('1');
        if (separatorPos < 1 || separatorPos + 7 > encoded.Length || encoded.Length > 90)
            return null;

        string hrp = encoded[..separatorPos];
        string dataStr = encoded[(separatorPos + 1)..];

        var data5bit = new byte[dataStr.Length];
        for (int i = 0; i < dataStr.Length; i++)
        {
            int value = Charset.IndexOf(dataStr[i]);
            if (value < 0)
                return null;
            data5bit[i] = (byte)value;
        }

        if (!VerifyChecksum(hrp, data5bit))
            return null;

        byte[] dataWithoutChecksum = data5bit[..^6];
        byte[] data8bit = ConvertBits(dataWithoutChecksum, 5, 8, false);

        return (hrp, data8bit);
    }

    private static byte[] CreateChecksum(string hrp, byte[] data)
    {
        byte[] values = [.. ExpandHrp(hrp), .. data, 0, 0, 0, 0, 0, 0];
        uint polymod = Polymod(values) ^ Bech32mConst;

        byte[] checksum = new byte[6];
        for (int i = 0; i < 6; i++)
            checksum[i] = (byte)((polymod >> (5 * (5 - i))) & 31);

        return checksum;
    }

    private static bool VerifyChecksum(string hrp, byte[] data)
    {
        byte[] values = [.. ExpandHrp(hrp), .. data];
        return Polymod(values) == Bech32mConst;
    }

    private static byte[] ExpandHrp(string hrp)
    {
        var result = new byte[hrp.Length * 2 + 1];
        for (int i = 0; i < hrp.Length; i++)
            result[i] = (byte)(hrp[i] >> 5);
        result[hrp.Length] = 0;
        for (int i = 0; i < hrp.Length; i++)
            result[hrp.Length + 1 + i] = (byte)(hrp[i] & 31);
        return result;
    }

    private static uint Polymod(byte[] values)
    {
        uint chk = 1;
        foreach (byte value in values)
        {
            byte top = (byte)(chk >> 25);
            chk = ((chk & 0x1ffffff) << 5) ^ value;
            for (int i = 0; i < 5; i++)
            {
                if (((top >> i) & 1) != 0)
                    chk ^= (uint)Generator[i];
            }
        }
        return chk;
    }

    private static byte[] ConvertBits(byte[] data, int fromBits, int toBits, bool pad)
    {
        int acc = 0;
        int bits = 0;
        var result = new List<byte>();
        int maxv = (1 << toBits) - 1;

        foreach (byte value in data)
        {
            if ((value >> fromBits) != 0)
                throw new ArgumentException("Invalid data for conversion");

            acc = (acc << fromBits) | value;
            bits += fromBits;

            while (bits >= toBits)
            {
                bits -= toBits;
                result.Add((byte)((acc >> bits) & maxv));
            }
        }

        if (pad)
        {
            if (bits > 0)
                result.Add((byte)((acc << (toBits - bits)) & maxv));
        }
        else if (bits >= fromBits || ((acc << (toBits - bits)) & maxv) != 0)
        {
            throw new ArgumentException("Invalid padding in data");
        }

        return result.ToArray();
    }
}
