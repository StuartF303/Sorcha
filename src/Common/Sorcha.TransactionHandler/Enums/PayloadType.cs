namespace Sorcha.TransactionHandler.Enums;

/// <summary>
/// Specifies the type of payload data.
/// </summary>
public enum PayloadType : ushort
{
    /// <summary>
    /// Generic data payload
    /// </summary>
    Data = 0,

    /// <summary>
    /// Document or file payload
    /// </summary>
    Document = 1,

    /// <summary>
    /// Text message payload
    /// </summary>
    Message = 2,

    /// <summary>
    /// JSON metadata payload
    /// </summary>
    Metadata = 3,

    /// <summary>
    /// User-defined custom payload
    /// </summary>
    Custom = 999
}
