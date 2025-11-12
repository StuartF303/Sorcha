using System;

namespace Sorcha.Cryptography.Utilities;

/// <summary>
/// Provides variable-length integer encoding and decoding (Bitcoin-style VarInt).
/// </summary>
public static class VariableLengthInteger
{
    /// <summary>
    /// Encodes a 64-bit integer as a variable-length integer.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded bytes.</returns>
    public static byte[] Encode(ulong value)
    {
        if (value < 0xFD)
        {
            return new byte[] { (byte)value };
        }
        else if (value <= 0xFFFF)
        {
            return new byte[]
            {
                0xFD,
                (byte)(value & 0xFF),
                (byte)((value >> 8) & 0xFF)
            };
        }
        else if (value <= 0xFFFFFFFF)
        {
            return new byte[]
            {
                0xFE,
                (byte)(value & 0xFF),
                (byte)((value >> 8) & 0xFF),
                (byte)((value >> 16) & 0xFF),
                (byte)((value >> 24) & 0xFF)
            };
        }
        else
        {
            return new byte[]
            {
                0xFF,
                (byte)(value & 0xFF),
                (byte)((value >> 8) & 0xFF),
                (byte)((value >> 16) & 0xFF),
                (byte)((value >> 24) & 0xFF),
                (byte)((value >> 32) & 0xFF),
                (byte)((value >> 40) & 0xFF),
                (byte)((value >> 48) & 0xFF),
                (byte)((value >> 56) & 0xFF)
            };
        }
    }

    /// <summary>
    /// Decodes a variable-length integer from a byte array.
    /// </summary>
    /// <param name="data">The data containing the encoded integer.</param>
    /// <param name="offset">The offset to start reading from.</param>
    /// <param name="bytesRead">Outputs the number of bytes read.</param>
    /// <returns>The decoded value.</returns>
    public static ulong Decode(byte[] data, int offset, out int bytesRead)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        if (offset < 0 || offset >= data.Length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        byte firstByte = data[offset];

        if (firstByte < 0xFD)
        {
            bytesRead = 1;
            return firstByte;
        }
        else if (firstByte == 0xFD)
        {
            if (offset + 2 >= data.Length)
                throw new ArgumentException("Not enough data for 2-byte VarInt");

            bytesRead = 3;
            return (ulong)data[offset + 1] |
                   ((ulong)data[offset + 2] << 8);
        }
        else if (firstByte == 0xFE)
        {
            if (offset + 4 >= data.Length)
                throw new ArgumentException("Not enough data for 4-byte VarInt");

            bytesRead = 5;
            return (ulong)data[offset + 1] |
                   ((ulong)data[offset + 2] << 8) |
                   ((ulong)data[offset + 3] << 16) |
                   ((ulong)data[offset + 4] << 24);
        }
        else // 0xFF
        {
            if (offset + 8 >= data.Length)
                throw new ArgumentException("Not enough data for 8-byte VarInt");

            bytesRead = 9;
            return (ulong)data[offset + 1] |
                   ((ulong)data[offset + 2] << 8) |
                   ((ulong)data[offset + 3] << 16) |
                   ((ulong)data[offset + 4] << 24) |
                   ((ulong)data[offset + 5] << 32) |
                   ((ulong)data[offset + 6] << 40) |
                   ((ulong)data[offset + 7] << 48) |
                   ((ulong)data[offset + 8] << 56);
        }
    }

    /// <summary>
    /// Gets the encoded length of a value without actually encoding it.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>The number of bytes required to encode the value.</returns>
    public static int GetEncodedLength(ulong value)
    {
        if (value < 0xFD) return 1;
        if (value <= 0xFFFF) return 3;
        if (value <= 0xFFFFFFFF) return 5;
        return 9;
    }
}
