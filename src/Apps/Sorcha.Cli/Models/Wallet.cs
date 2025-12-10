using System.Text.Json.Serialization;

namespace Sorcha.Cli.Models;

/// <summary>
/// Data transfer object for wallet information
/// </summary>
public class Wallet
{
    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("publicKey")]
    public string PublicKey { get; set; } = string.Empty;

    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("owner")]
    public string Owner { get; set; } = string.Empty;

    [JsonPropertyName("tenant")]
    public string Tenant { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Request model for creating a new wallet
/// </summary>
public class CreateWalletRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = string.Empty;

    [JsonPropertyName("wordCount")]
    public int WordCount { get; set; } = 12;

    [JsonPropertyName("passphrase")]
    public string? Passphrase { get; set; }

    [JsonPropertyName("tags")]
    public Dictionary<string, string>? Tags { get; set; }
}

/// <summary>
/// Response model for creating a new wallet
/// </summary>
public class CreateWalletResponse
{
    [JsonPropertyName("wallet")]
    public Wallet? Wallet { get; set; }

    [JsonPropertyName("mnemonicWords")]
    public string[] MnemonicWords { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Request model for recovering a wallet from mnemonic
/// </summary>
public class RecoverWalletRequest
{
    [JsonPropertyName("mnemonicWords")]
    public string[] MnemonicWords { get; set; } = Array.Empty<string>();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = string.Empty;

    [JsonPropertyName("passphrase")]
    public string? Passphrase { get; set; }
}

/// <summary>
/// Request model for signing a transaction
/// </summary>
public class SignTransactionRequest
{
    [JsonPropertyName("transactionData")]
    public string TransactionData { get; set; } = string.Empty;
}

/// <summary>
/// Response model for signing a transaction
/// </summary>
public class SignTransactionResponse
{
    [JsonPropertyName("signature")]
    public string Signature { get; set; } = string.Empty;

    [JsonPropertyName("signedBy")]
    public string SignedBy { get; set; } = string.Empty;

    [JsonPropertyName("signedAt")]
    public DateTimeOffset SignedAt { get; set; }
}

/// <summary>
/// Request model for updating wallet metadata
/// </summary>
public class UpdateWalletRequest
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("tags")]
    public Dictionary<string, string>? Tags { get; set; }
}
