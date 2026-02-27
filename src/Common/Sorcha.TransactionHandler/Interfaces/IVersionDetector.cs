// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors
using Sorcha.TransactionHandler.Enums;

namespace Sorcha.TransactionHandler.Interfaces;

/// <summary>
/// Interface for detecting transaction versions from binary or JSON data.
/// </summary>
public interface IVersionDetector
{
    /// <summary>
    /// Detects transaction version from binary data.
    /// </summary>
    /// <param name="data">The binary transaction data</param>
    /// <returns>The detected transaction version</returns>
    TransactionVersion DetectVersion(byte[] data);

    /// <summary>
    /// Detects transaction version from JSON.
    /// </summary>
    /// <param name="json">The JSON transaction data</param>
    /// <returns>The detected transaction version</returns>
    TransactionVersion DetectVersion(string json);
}
