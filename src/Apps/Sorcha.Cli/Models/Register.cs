using System.Text.Json.Serialization;

namespace Sorcha.Cli.Models;

/// <summary>
/// Represents a register in the Sorcha platform.
/// </summary>
public class Register
{
    /// <summary>
    /// Gets or sets the register ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the register name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the register description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the organization ID that owns this register.
    /// </summary>
    [JsonPropertyName("organizationId")]
    public string OrganizationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the register is active.
    /// </summary>
    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets when the register was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the total number of transactions in this register.
    /// </summary>
    [JsonPropertyName("transactionCount")]
    public long TransactionCount { get; set; }
}

/// <summary>
/// Request to create a new register.
/// </summary>
public class CreateRegisterRequest
{
    /// <summary>
    /// Gets or sets the register name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the register description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the organization ID.
    /// </summary>
    [JsonPropertyName("organizationId")]
    public string OrganizationId { get; set; } = string.Empty;
}

/// <summary>
/// Represents a transaction in a register.
/// </summary>
public class Transaction
{
    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the register ID.
    /// </summary>
    [JsonPropertyName("registerId")]
    public string RegisterId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction type.
    /// </summary>
    [JsonPropertyName("txType")]
    public string TxType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender wallet address.
    /// </summary>
    [JsonPropertyName("senderWallet")]
    public string SenderWallet { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction payload (JSON).
    /// </summary>
    [JsonPropertyName("payload")]
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction signature.
    /// </summary>
    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the previous transaction ID in the chain.
    /// </summary>
    [JsonPropertyName("previousTxId")]
    public string? PreviousTxId { get; set; }

    /// <summary>
    /// Gets or sets the transaction timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the transaction status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Request to submit a new transaction.
/// </summary>
public class SubmitTransactionRequest
{
    /// <summary>
    /// Gets or sets the register ID.
    /// </summary>
    [JsonPropertyName("registerId")]
    public string RegisterId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction type.
    /// </summary>
    [JsonPropertyName("txType")]
    public string TxType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sender wallet address.
    /// </summary>
    [JsonPropertyName("senderWallet")]
    public string SenderWallet { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction payload (JSON).
    /// </summary>
    [JsonPropertyName("payload")]
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction signature.
    /// </summary>
    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the previous transaction ID in the chain.
    /// </summary>
    [JsonPropertyName("previousTxId")]
    public string? PreviousTxId { get; set; }
}

/// <summary>
/// Response after submitting a transaction.
/// </summary>
public class SubmitTransactionResponse
{
    /// <summary>
    /// Gets or sets the transaction ID.
    /// </summary>
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets any error message.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
