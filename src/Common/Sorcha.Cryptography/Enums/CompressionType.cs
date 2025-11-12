namespace Sorcha.Cryptography.Enums;

/// <summary>
/// Compression level types for data compression.
/// </summary>
public enum CompressionType
{
    /// <summary>
    /// No compression.
    /// </summary>
    None,

    /// <summary>
    /// Fast compression with lower ratio.
    /// </summary>
    Fast,

    /// <summary>
    /// Balanced compression (good speed and ratio).
    /// </summary>
    Balanced,

    /// <summary>
    /// Maximum compression (slower but best ratio).
    /// </summary>
    Maximum
}
