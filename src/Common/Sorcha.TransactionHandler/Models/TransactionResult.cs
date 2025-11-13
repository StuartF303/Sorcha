using Sorcha.TransactionHandler.Enums;

namespace Sorcha.TransactionHandler.Models;

/// <summary>
/// Represents the result of a transaction operation.
/// </summary>
/// <typeparam name="T">The type of the result value</typeparam>
public class TransactionResult<T>
{
    /// <summary>
    /// Gets the status of the operation.
    /// </summary>
    public TransactionStatus Status { get; init; }

    /// <summary>
    /// Gets the result value if successful.
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess => Status == TransactionStatus.Success;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="value">The result value</param>
    /// <returns>A successful TransactionResult</returns>
    public static TransactionResult<T> Success(T value) => new()
    {
        Status = TransactionStatus.Success,
        Value = value
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="status">The error status</param>
    /// <param name="error">Optional error message</param>
    /// <returns>A failed TransactionResult</returns>
    public static TransactionResult<T> Failure(
        TransactionStatus status,
        string? error = null) => new()
    {
        Status = status,
        ErrorMessage = error
    };
}

/// <summary>
/// Represents the result of a payload operation.
/// </summary>
/// <typeparam name="T">The type of the result value</typeparam>
public class PayloadResult<T>
{
    /// <summary>
    /// Gets the status of the operation.
    /// </summary>
    public TransactionStatus Status { get; init; }

    /// <summary>
    /// Gets the result value if successful.
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess => Status == TransactionStatus.Success;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="value">The result value</param>
    /// <returns>A successful PayloadResult</returns>
    public static PayloadResult<T> Success(T value) => new()
    {
        Status = TransactionStatus.Success,
        Value = value
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="status">The error status</param>
    /// <param name="error">Optional error message</param>
    /// <returns>A failed PayloadResult</returns>
    public static PayloadResult<T> Failure(
        TransactionStatus status,
        string? error = null) => new()
    {
        Status = status,
        ErrorMessage = error
    };
}

/// <summary>
/// Represents the result of a payload operation (non-generic).
/// </summary>
public class PayloadResult
{
    /// <summary>
    /// Gets the status of the operation.
    /// </summary>
    public TransactionStatus Status { get; init; }

    /// <summary>
    /// Gets the payload ID if successful.
    /// </summary>
    public uint PayloadId { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess => Status == TransactionStatus.Success;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="payloadId">The payload ID</param>
    /// <returns>A successful PayloadResult</returns>
    public static PayloadResult Success(uint payloadId) => new()
    {
        Status = TransactionStatus.Success,
        PayloadId = payloadId
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="status">The error status</param>
    /// <param name="error">Optional error message</param>
    /// <returns>A failed PayloadResult</returns>
    public static PayloadResult Failure(
        TransactionStatus status,
        string? error = null) => new()
    {
        Status = status,
        ErrorMessage = error
    };
}
