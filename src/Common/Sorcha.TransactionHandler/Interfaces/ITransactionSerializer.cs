using Sorcha.TransactionHandler.Models;

namespace Sorcha.TransactionHandler.Interfaces;

/// <summary>
/// Interface for transaction serialization.
/// </summary>
public interface ITransactionSerializer
{
    /// <summary>
    /// Serializes a transaction to binary format.
    /// </summary>
    /// <param name="transaction">The transaction to serialize</param>
    /// <returns>The binary representation</returns>
    byte[] SerializeToBinary(ITransaction transaction);

    /// <summary>
    /// Deserializes a transaction from binary format.
    /// </summary>
    /// <param name="data">The binary data</param>
    /// <returns>The deserialized transaction</returns>
    ITransaction DeserializeFromBinary(byte[] data);

    /// <summary>
    /// Serializes a transaction to JSON format.
    /// </summary>
    /// <param name="transaction">The transaction to serialize</param>
    /// <returns>The JSON representation</returns>
    string SerializeToJson(ITransaction transaction);

    /// <summary>
    /// Deserializes a transaction from JSON format.
    /// </summary>
    /// <param name="json">The JSON string</param>
    /// <returns>The deserialized transaction</returns>
    ITransaction DeserializeFromJson(string json);

    /// <summary>
    /// Creates a transport packet for network transmission.
    /// </summary>
    /// <param name="transaction">The transaction</param>
    /// <returns>The transport packet</returns>
    TransportPacket CreateTransportPacket(ITransaction transaction);
}
