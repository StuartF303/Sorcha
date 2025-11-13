namespace Sorcha.TransactionHandler.Models;

/// <summary>
/// Represents a transaction transport packet for network transmission.
/// </summary>
public class TransportPacket
{
    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    public string? TxId { get; set; }

    /// <summary>
    /// Gets or sets the register ID.
    /// </summary>
    public string? RegisterId { get; set; }

    /// <summary>
    /// Gets or sets the binary serialized transaction data.
    /// </summary>
    public byte[]? Data { get; set; }
}
