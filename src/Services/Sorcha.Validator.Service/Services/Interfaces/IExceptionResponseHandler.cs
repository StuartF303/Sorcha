// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Validator.Service.Services.Interfaces;

/// <summary>
/// Handles failed validations by creating exception responses that can be
/// sent back to the transaction submitter via various channels.
/// </summary>
public interface IExceptionResponseHandler
{
    /// <summary>
    /// Creates an exception response from a failed validation result
    /// </summary>
    /// <param name="validationResult">The failed validation result</param>
    /// <param name="originalTransaction">The original transaction that failed</param>
    /// <returns>An exception response ready to be delivered</returns>
    ExceptionResponse CreateResponse(
        ValidationEngineResult validationResult,
        Models.Transaction originalTransaction);

    /// <summary>
    /// Creates an exception response from a batch of failed validation results
    /// </summary>
    /// <param name="validationResults">The failed validation results</param>
    /// <param name="originalTransactions">The original transactions that failed</param>
    /// <returns>Exception responses for each failed transaction</returns>
    IReadOnlyList<ExceptionResponse> CreateResponses(
        IReadOnlyList<ValidationEngineResult> validationResults,
        IReadOnlyList<Models.Transaction> originalTransactions);

    /// <summary>
    /// Delivers an exception response to the transaction submitter via SignalR
    /// </summary>
    /// <param name="response">The exception response to deliver</param>
    /// <param name="connectionId">Optional SignalR connection ID (if known)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if delivery was successful</returns>
    Task<bool> DeliverViaSignalRAsync(
        ExceptionResponse response,
        string? connectionId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Records an exception response to the audit log
    /// </summary>
    /// <param name="response">The exception response to record</param>
    /// <param name="ct">Cancellation token</param>
    Task RecordToAuditLogAsync(
        ExceptionResponse response,
        CancellationToken ct = default);

    /// <summary>
    /// Gets statistics about exception responses
    /// </summary>
    ExceptionResponseStats GetStats();
}

/// <summary>
/// An exception response for a failed transaction validation
/// </summary>
public record ExceptionResponse
{
    /// <summary>Unique ID for this exception response</summary>
    public required string ExceptionId { get; init; }

    /// <summary>Original transaction ID that failed</summary>
    public required string TransactionId { get; init; }

    /// <summary>Register ID the transaction was targeting</summary>
    public required string RegisterId { get; init; }

    /// <summary>Blueprint ID the transaction was targeting</summary>
    public string? BlueprintId { get; init; }

    /// <summary>When the exception was created</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Overall exception code</summary>
    public required ExceptionCode Code { get; init; }

    /// <summary>Human-readable summary of the failure</summary>
    public required string Summary { get; init; }

    /// <summary>Detailed error information</summary>
    public IReadOnlyList<ExceptionDetail> Details { get; init; } = [];

    /// <summary>Time taken for the validation that produced this exception</summary>
    public TimeSpan ValidationDuration { get; init; }

    /// <summary>Validator node that produced this exception</summary>
    public string? ValidatorId { get; init; }

    /// <summary>Whether the exception has been delivered to the submitter</summary>
    public bool IsDelivered { get; init; }

    /// <summary>When the exception was delivered (if delivered)</summary>
    public DateTimeOffset? DeliveredAt { get; init; }

    /// <summary>Delivery method used (if delivered)</summary>
    public ExceptionDeliveryMethod? DeliveryMethod { get; init; }
}

/// <summary>
/// Detailed information about a specific validation error
/// </summary>
public record ExceptionDetail
{
    /// <summary>Error code</summary>
    public required string Code { get; init; }

    /// <summary>Error message</summary>
    public required string Message { get; init; }

    /// <summary>Error category</summary>
    public required ValidationErrorCategory Category { get; init; }

    /// <summary>Field where the error occurred (if applicable)</summary>
    public string? Field { get; init; }

    /// <summary>Whether this error was fatal</summary>
    public bool IsFatal { get; init; }

    /// <summary>Suggested remediation action</summary>
    public string? Remediation { get; init; }
}

/// <summary>
/// High-level exception codes for categorizing failures
/// </summary>
public enum ExceptionCode
{
    /// <summary>Transaction structure is invalid</summary>
    InvalidStructure = 1,

    /// <summary>Payload doesn't match schema</summary>
    SchemaViolation = 2,

    /// <summary>Cryptographic verification failed</summary>
    CryptographicFailure = 3,

    /// <summary>Chain integrity violation</summary>
    ChainViolation = 4,

    /// <summary>Blueprint/action not found or invalid</summary>
    BlueprintError = 5,

    /// <summary>Permission/authorization failure</summary>
    PermissionDenied = 6,

    /// <summary>Timing constraint violation</summary>
    TimingViolation = 7,

    /// <summary>Internal validator error</summary>
    InternalError = 8,

    /// <summary>Multiple validation failures</summary>
    MultipleFailures = 9
}

/// <summary>
/// Methods for delivering exception responses
/// </summary>
public enum ExceptionDeliveryMethod
{
    /// <summary>Delivered via SignalR WebSocket</summary>
    SignalR,

    /// <summary>Delivered via API response</summary>
    ApiResponse,

    /// <summary>Delivered via gRPC</summary>
    Grpc,

    /// <summary>Recorded to audit log only</summary>
    AuditLogOnly
}

/// <summary>
/// Statistics about exception response handling
/// </summary>
public record ExceptionResponseStats
{
    /// <summary>Total exceptions created</summary>
    public long TotalCreated { get; init; }

    /// <summary>Total exceptions delivered</summary>
    public long TotalDelivered { get; init; }

    /// <summary>Exceptions by code</summary>
    public IReadOnlyDictionary<ExceptionCode, long> ByCode { get; init; }
        = new Dictionary<ExceptionCode, long>();

    /// <summary>Exceptions by delivery method</summary>
    public IReadOnlyDictionary<ExceptionDeliveryMethod, long> ByDeliveryMethod { get; init; }
        = new Dictionary<ExceptionDeliveryMethod, long>();

    /// <summary>Average time from validation to delivery</summary>
    public TimeSpan AverageDeliveryLatency { get; init; }
}
