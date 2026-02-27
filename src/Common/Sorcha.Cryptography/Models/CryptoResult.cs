// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors
using System;
using Sorcha.Cryptography.Enums;

namespace Sorcha.Cryptography.Models;

/// <summary>
/// Represents the result of a cryptographic operation.
/// </summary>
/// <typeparam name="T">The type of value returned on success.</typeparam>
public class CryptoResult<T>
{
    /// <summary>
    /// Gets the status of the operation.
    /// </summary>
    public CryptoStatus Status { get; init; }

    /// <summary>
    /// Gets the result value if the operation succeeded.
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool IsSuccess => Status == CryptoStatus.Success;

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <param name="value">The result value.</param>
    /// <returns>A successful CryptoResult.</returns>
    public static CryptoResult<T> Success(T value) => new()
    {
        Status = CryptoStatus.Success,
        Value = value
    };

    /// <summary>
    /// Creates a failed result with status and optional error message.
    /// </summary>
    /// <param name="status">The error status code.</param>
    /// <param name="errorMessage">Optional error message.</param>
    /// <returns>A failed CryptoResult.</returns>
    public static CryptoResult<T> Failure(CryptoStatus status, string? errorMessage = null) => new()
    {
        Status = status,
        ErrorMessage = errorMessage
    };
}
