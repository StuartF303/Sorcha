// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors
using Sorcha.TransactionHandler.Enums;

namespace Sorcha.TransactionHandler.Interfaces;

/// <summary>
/// Factory interface for creating transactions with version support.
/// </summary>
public interface ITransactionFactory
{
    /// <summary>
    /// Creates a transaction of the specified version.
    /// </summary>
    /// <param name="version">The transaction version to create</param>
    /// <returns>A new transaction instance</returns>
    ITransaction Create(TransactionVersion version);

    /// <summary>
    /// Deserializes transaction from binary data, auto-detecting version.
    /// </summary>
    /// <param name="data">The binary transaction data</param>
    /// <returns>The deserialized transaction</returns>
    ITransaction Deserialize(byte[] data);

    /// <summary>
    /// Deserializes transaction from JSON, auto-detecting version.
    /// </summary>
    /// <param name="json">The JSON transaction data</param>
    /// <returns>The deserialized transaction</returns>
    ITransaction Deserialize(string json);
}
