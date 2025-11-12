using System.Numerics;
using System.Text;

namespace Sorcha.Cryptography.Utilities;

/// <summary>
/// Provides Base58 encoding and decoding functionality.
/// </summary>
public static class Base58
{
    private const string Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
    private static readonly char[] AlphabetArray = Alphabet.ToCharArray();
    private static readonly Dictionary<char, int> AlphabetMap = CreateAlphabetMap();

    private static Dictionary<char, int> CreateAlphabetMap()
    {
        var map = new Dictionary<char, int>();
        for (int i = 0; i < Alphabet.Length; i++)
        {
            map[Alphabet[i]] = i;
        }
        return map;
    }

    /// <summary>
    /// Encodes a byte array to Base58 string.
    /// </summary>
    /// <param name="data">The data to encode.</param>
    /// <returns>The Base58 encoded string.</returns>
    public static string Encode(byte[] data)
    {
        if (data == null || data.Length == 0)
            return string.Empty;

        // Count leading zeros
        int leadingZeros = 0;
        for (int i = 0; i < data.Length && data[i] == 0; i++)
        {
            leadingZeros++;
        }

        // Convert byte array to BigInteger
        var value = new BigInteger(data.Reverse().Concat(new byte[] { 0 }).ToArray());

        // Convert to Base58
        var result = new StringBuilder();
        while (value > 0)
        {
            value = BigInteger.DivRem(value, 58, out BigInteger remainder);
            result.Insert(0, AlphabetArray[(int)remainder]);
        }

        // Add leading '1' for each leading zero byte
        for (int i = 0; i < leadingZeros; i++)
        {
            result.Insert(0, '1');
        }

        return result.ToString();
    }

    /// <summary>
    /// Decodes a Base58 string to byte array.
    /// </summary>
    /// <param name="encoded">The Base58 encoded string.</param>
    /// <returns>The decoded byte array.</returns>
    public static byte[]? Decode(string encoded)
    {
        if (string.IsNullOrEmpty(encoded))
            return Array.Empty<byte>();

        try
        {
            // Count leading '1' characters
            int leadingOnes = 0;
            for (int i = 0; i < encoded.Length && encoded[i] == '1'; i++)
            {
                leadingOnes++;
            }

            // Convert Base58 to BigInteger
            var value = BigInteger.Zero;
            for (int i = 0; i < encoded.Length; i++)
            {
                if (!AlphabetMap.TryGetValue(encoded[i], out int digit))
                    return null; // Invalid character

                value = value * 58 + digit;
            }

            // Convert BigInteger to byte array
            byte[] bytes = value.ToByteArray();

            // Remove extra zero byte added by BigInteger (if present)
            if (bytes.Length > 1 && bytes[bytes.Length - 1] == 0)
            {
                Array.Resize(ref bytes, bytes.Length - 1);
            }

            // Reverse and add leading zeros
            Array.Reverse(bytes);
            byte[] result = new byte[leadingOnes + bytes.Length];
            Array.Copy(bytes, 0, result, leadingOnes, bytes.Length);

            return result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Encodes data with a checksum (Base58Check).
    /// </summary>
    /// <param name="data">The data to encode.</param>
    /// <returns>The Base58Check encoded string.</returns>
    public static string EncodeCheck(byte[] data)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Data cannot be null or empty", nameof(data));

        // Calculate double SHA-256 checksum
        byte[] hash = System.Security.Cryptography.SHA256.HashData(data);
        hash = System.Security.Cryptography.SHA256.HashData(hash);

        // Take first 4 bytes of hash as checksum
        byte[] checksum = hash.Take(4).ToArray();

        // Concatenate data and checksum
        byte[] dataWithChecksum = data.Concat(checksum).ToArray();

        return Encode(dataWithChecksum);
    }

    /// <summary>
    /// Decodes Base58Check data and verifies checksum.
    /// </summary>
    /// <param name="encoded">The Base58Check encoded string.</param>
    /// <returns>The decoded data without checksum, or null if invalid.</returns>
    public static byte[]? DecodeCheck(string encoded)
    {
        if (string.IsNullOrEmpty(encoded))
            return null;

        byte[]? decoded = Decode(encoded);
        if (decoded == null || decoded.Length < 4)
            return null;

        // Split data and checksum
        byte[] data = decoded.Take(decoded.Length - 4).ToArray();
        byte[] checksum = decoded.Skip(decoded.Length - 4).ToArray();

        // Calculate expected checksum
        byte[] hash = System.Security.Cryptography.SHA256.HashData(data);
        hash = System.Security.Cryptography.SHA256.HashData(hash);
        byte[] expectedChecksum = hash.Take(4).ToArray();

        // Verify checksum
        if (!checksum.SequenceEqual(expectedChecksum))
            return null;

        return data;
    }
}
