// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.UI.Core.Models.Registers;

/// <summary>
/// View model for displaying transaction information in the UI.
/// Wraps the TransactionModel with UI-specific formatting.
/// </summary>
public record TransactionViewModel
{
    /// <summary>
    /// Transaction identifier (64-char hex hash)
    /// </summary>
    public required string TxId { get; init; }

    /// <summary>
    /// Register identifier this transaction belongs to
    /// </summary>
    public required string RegisterId { get; init; }

    /// <summary>
    /// Sender wallet address (Base58 encoded)
    /// </summary>
    public required string SenderWallet { get; init; }

    /// <summary>
    /// Recipient wallet addresses
    /// </summary>
    public IReadOnlyList<string> RecipientsWallets { get; init; } = [];

    /// <summary>
    /// Transaction timestamp (UTC)
    /// </summary>
    public DateTime TimeStamp { get; init; }

    /// <summary>
    /// Block number (docket ID) this transaction is sealed in
    /// </summary>
    public ulong? BlockNumber { get; init; }

    /// <summary>
    /// Number of payloads in transaction
    /// </summary>
    public ulong PayloadCount { get; init; }

    /// <summary>
    /// Cryptographic signature of transaction
    /// </summary>
    public required string Signature { get; init; }

    /// <summary>
    /// Previous transaction ID for blockchain chain
    /// </summary>
    public string? PrevTxId { get; init; }

    /// <summary>
    /// Transaction format version
    /// </summary>
    public uint Version { get; init; } = 1;

    /// <summary>
    /// Blueprint ID from metadata (if present)
    /// </summary>
    public string? BlueprintId { get; init; }

    /// <summary>
    /// Instance ID from metadata (if present)
    /// </summary>
    public string? InstanceId { get; init; }

    /// <summary>
    /// Action ID from metadata (if present)
    /// </summary>
    public uint? ActionId { get; init; }

    /// <summary>
    /// Computed: Formatted timestamp (relative or absolute)
    /// </summary>
    public string TimeStampFormatted => GetFormattedTime(TimeStamp);

    /// <summary>
    /// Computed: Whether this transaction is recent (within last 5 seconds)
    /// </summary>
    public bool IsRecent => (DateTime.UtcNow - TimeStamp).TotalSeconds < 5;

    /// <summary>
    /// Computed: Transaction type derived from metadata or default
    /// </summary>
    public string TransactionType => ActionId.HasValue
        ? "Action"
        : !string.IsNullOrEmpty(BlueprintId)
            ? "Blueprint"
            : "Transfer";

    /// <summary>
    /// Computed: Full DID URI for this transaction
    /// </summary>
    public string DidUri => $"did:sorcha:register:{RegisterId}/tx/{TxId}";

    private static string GetFormattedTime(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;

        // For recent transactions, show relative time
        if (timeSpan.TotalMinutes < 60)
        {
            return timeSpan.TotalSeconds < 60
                ? "just now"
                : $"{(int)timeSpan.TotalMinutes}m ago";
        }

        // For today's transactions, show time only
        if (dateTime.Date == DateTime.UtcNow.Date)
        {
            return dateTime.ToString("HH:mm:ss");
        }

        // For older transactions, show full date and time
        return dateTime.ToString("MMM dd, HH:mm");
    }
}
