// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Buffers.Text;
using Sorcha.Register.Models;
using Sorcha.Register.Storage.MongoDB.Models;

namespace Sorcha.Register.Storage.MongoDB.Mappers;

/// <summary>
/// Bidirectional mapper between <see cref="TransactionModel"/> (wire/API format)
/// and <see cref="MongoTransactionDocument"/> (BSON Binary storage format).
/// </summary>
/// <remarks>
/// Write path: Base64url strings → decoded byte[] → stored as BSON Binary (~33% savings).
/// Read path: BSON Binary byte[] → Base64url strings → TransactionModel for API consumers.
/// Legacy detection is handled by <see cref="Serialization.Base64UrlBinarySerializer"/> during BSON deserialization.
/// </remarks>
public static class MongoDocumentMapper
{
    /// <summary>
    /// Converts a <see cref="TransactionModel"/> to a <see cref="MongoTransactionDocument"/>
    /// for MongoDB storage. Binary fields are decoded from Base64url strings to byte[].
    /// </summary>
    public static MongoTransactionDocument ToMongoDocument(TransactionModel transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        return new MongoTransactionDocument
        {
            Context = transaction.Context,
            Type = transaction.Type,
            Id = transaction.Id,
            RegisterId = transaction.RegisterId,
            TxId = transaction.TxId,
            PrevTxId = transaction.PrevTxId,
            DocketNumber = transaction.DocketNumber,
            Version = transaction.Version,
            SenderWallet = transaction.SenderWallet,
            RecipientsWallets = transaction.RecipientsWallets,
            TimeStamp = transaction.TimeStamp,
            MetaData = transaction.MetaData,
            PayloadCount = transaction.PayloadCount,
            Payloads = transaction.Payloads?.Select(ToMongoPayload).ToArray() ?? [],
            Signature = DecodeBase64Auto(transaction.Signature)
        };
    }

    /// <summary>
    /// Converts a <see cref="MongoTransactionDocument"/> back to a <see cref="TransactionModel"/>
    /// for API consumers. Binary fields are encoded from byte[] to Base64url strings.
    /// </summary>
    public static TransactionModel ToTransactionModel(MongoTransactionDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new TransactionModel
        {
            Context = document.Context,
            Type = document.Type,
            Id = document.Id,
            RegisterId = document.RegisterId,
            TxId = document.TxId,
            PrevTxId = document.PrevTxId,
            DocketNumber = document.DocketNumber,
            Version = document.Version,
            SenderWallet = document.SenderWallet,
            RecipientsWallets = document.RecipientsWallets,
            TimeStamp = document.TimeStamp,
            MetaData = document.MetaData,
            PayloadCount = document.PayloadCount,
            Payloads = document.Payloads?.Select(ToPayloadModel).ToArray() ?? [],
            Signature = Base64Url.EncodeToString(document.Signature)
        };
    }

    private static MongoPayloadDocument ToMongoPayload(PayloadModel payload)
    {
        return new MongoPayloadDocument
        {
            WalletAccess = payload.WalletAccess,
            PayloadSize = payload.PayloadSize,
            Hash = DecodeBase64Auto(payload.Hash),
            Data = DecodeBase64Auto(payload.Data),
            PayloadFlags = payload.PayloadFlags,
            IV = payload.IV is not null ? ToMongoChallengeDocument(payload.IV) : null,
            Challenges = payload.Challenges?.Select(ToMongoChallengeDocument).ToArray(),
            ContentType = payload.ContentType,
            ContentEncoding = payload.ContentEncoding
        };
    }

    private static PayloadModel ToPayloadModel(MongoPayloadDocument document)
    {
        return new PayloadModel
        {
            WalletAccess = document.WalletAccess,
            PayloadSize = document.PayloadSize,
            Hash = Base64Url.EncodeToString(document.Hash),
            Data = Base64Url.EncodeToString(document.Data),
            PayloadFlags = document.PayloadFlags,
            IV = document.IV is not null ? ToChallenge(document.IV) : null,
            Challenges = document.Challenges?.Select(ToChallenge).ToArray(),
            ContentType = document.ContentType,
            ContentEncoding = document.ContentEncoding
        };
    }

    private static MongoChallengeDocument ToMongoChallengeDocument(Challenge challenge)
    {
        return new MongoChallengeDocument
        {
            Data = challenge.Data is not null ? DecodeBase64Auto(challenge.Data) : null,
            Address = challenge.Address
        };
    }

    private static Challenge ToChallenge(MongoChallengeDocument document)
    {
        return new Challenge
        {
            Data = document.Data is not null ? Base64Url.EncodeToString(document.Data) : null,
            Address = document.Address
        };
    }

    /// <summary>
    /// Smart decode: detects legacy Base64 (+, /, =) vs Base64url.
    /// Falls back to UTF-8 bytes for strings that aren't valid Base64/Base64url
    /// (e.g., plaintext signatures in test data, hex strings).
    /// </summary>
    private static byte[] DecodeBase64Auto(string? encoded)
    {
        if (string.IsNullOrEmpty(encoded))
            return [];

        try
        {
            if (encoded.Contains('+') || encoded.Contains('/') || encoded.Contains('='))
                return Convert.FromBase64String(encoded);

            return Base64Url.DecodeFromChars(encoded);
        }
        catch (FormatException)
        {
            // Not valid Base64 or Base64url — store raw UTF-8 bytes.
            // This handles legacy test data and non-encoded string values.
            return System.Text.Encoding.UTF8.GetBytes(encoded);
        }
    }
}
