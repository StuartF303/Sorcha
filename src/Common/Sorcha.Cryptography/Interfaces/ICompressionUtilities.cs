using Sorcha.Cryptography.Enums;

namespace Sorcha.Cryptography.Interfaces;

/// <summary>
/// Provides data compression and decompression utilities.
/// </summary>
public interface ICompressionUtilities
{
    /// <summary>
    /// Compresses data if beneficial.
    /// </summary>
    /// <param name="data">The data to compress.</param>
    /// <param name="type">The compression type/level.</param>
    /// <returns>A tuple containing the (possibly compressed) data and whether it was compressed.</returns>
    (byte[]? Data, bool WasCompressed) Compress(
        byte[] data,
        CompressionType type = CompressionType.Balanced);

    /// <summary>
    /// Decompresses data if it was compressed.
    /// </summary>
    /// <param name="data">The data to decompress.</param>
    /// <returns>The decompressed data, or the original data if it wasn't compressed.</returns>
    byte[]? Decompress(byte[] data);

    /// <summary>
    /// Checks if data appears to already be compressed.
    /// </summary>
    /// <param name="data">The data to check.</param>
    /// <returns>True if the data appears to be compressed.</returns>
    bool IsCompressed(byte[] data);

    /// <summary>
    /// Detects the file type from the data signature.
    /// </summary>
    /// <param name="data">The data to analyze.</param>
    /// <returns>The detected file type, or null if unknown.</returns>
    string? DetectFileType(byte[] data);
}
