using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sorcha.Cryptography.Utilities;

/// <summary>
/// Provides Bech32 encoding and decoding functionality.
/// </summary>
public static class Bech32
{
    private const string Charset = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
    private static readonly int[] Generator = { 0x3b6a57b2, 0x26508e6d, 0x1ea119fa, 0x3d4233dd, 0x2a1462b3 };

    /// <summary>
    /// Encodes data to Bech32 format.
    /// </summary>
    /// <param name="hrp">Human-readable part (e.g., "ws1").</param>
    /// <param name="data">The data to encode (5-bit values).</param>
    /// <returns>The Bech32 encoded string.</returns>
    public static string Encode(string hrp, byte[] data)
    {
        if (string.IsNullOrEmpty(hrp))
            throw new ArgumentException("HRP cannot be null or empty", nameof(hrp));

        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        // Convert 8-bit data to 5-bit data
        byte[] data5bit = ConvertBits(data, 8, 5, true);

        // Create checksum
        byte[] checksum = CreateChecksum(hrp, data5bit);

        // Combine data and checksum
        byte[] combined = data5bit.Concat(checksum).ToArray();

        // Build result
        var result = new StringBuilder();
        result.Append(hrp.ToLowerInvariant());
        result.Append('1');

        foreach (byte b in combined)
        {
            result.Append(Charset[b]);
        }

        return result.ToString();
    }

    /// <summary>
    /// Decodes a Bech32 encoded string.
    /// </summary>
    /// <param name="encoded">The Bech32 encoded string.</param>
    /// <returns>A tuple containing the HRP and decoded data, or null if invalid.</returns>
    public static (string Hrp, byte[] Data)? Decode(string encoded)
    {
        if (string.IsNullOrEmpty(encoded))
            return null;

        encoded = encoded.ToLowerInvariant();

        // Find the separator
        int separatorPos = encoded.LastIndexOf('1');
        if (separatorPos < 1 || separatorPos + 7 > encoded.Length || encoded.Length > 90)
            return null;

        // Split HRP and data
        string hrp = encoded.Substring(0, separatorPos);
        string dataStr = encoded.Substring(separatorPos + 1);

        // Convert characters to 5-bit values
        var data5bit = new byte[dataStr.Length];
        for (int i = 0; i < dataStr.Length; i++)
        {
            int value = Charset.IndexOf(dataStr[i]);
            if (value < 0)
                return null;
            data5bit[i] = (byte)value;
        }

        // Verify checksum
        if (!VerifyChecksum(hrp, data5bit))
            return null;

        // Remove checksum (last 6 characters)
        byte[] dataWithoutChecksum = data5bit.Take(data5bit.Length - 6).ToArray();

        // Convert 5-bit data back to 8-bit
        byte[] data8bit = ConvertBits(dataWithoutChecksum, 5, 8, false);

        return (hrp, data8bit);
    }

    private static byte[] CreateChecksum(string hrp, byte[] data)
    {
        byte[] values = ExpandHrp(hrp).Concat(data).Concat(new byte[6]).ToArray();
        uint polymod = Polymod(values) ^ 1;

        byte[] checksum = new byte[6];
        for (int i = 0; i < 6; i++)
        {
            checksum[i] = (byte)((polymod >> (5 * (5 - i))) & 31);
        }

        return checksum;
    }

    private static bool VerifyChecksum(string hrp, byte[] data)
    {
        byte[] values = ExpandHrp(hrp).Concat(data).ToArray();
        return Polymod(values) == 1;
    }

    private static byte[] ExpandHrp(string hrp)
    {
        var result = new List<byte>();

        foreach (char c in hrp)
        {
            result.Add((byte)(c >> 5));
        }

        result.Add(0);

        foreach (char c in hrp)
        {
            result.Add((byte)(c & 31));
        }

        return result.ToArray();
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
                {
                    chk ^= (uint)Generator[i];
                }
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
            {
                result.Add((byte)((acc << (toBits - bits)) & maxv));
            }
        }
        else if (bits >= fromBits || ((acc << (toBits - bits)) & maxv) != 0)
        {
            throw new ArgumentException("Invalid padding in data");
        }

        return result.ToArray();
    }
}
