// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.UI.Core.Models.Registers;

namespace Sorcha.UI.Core.Services;

/// <summary>
/// Service for interacting with the Register API.
/// </summary>
public interface IRegisterService
{
    /// <summary>
    /// Gets all registers, optionally filtered by tenant.
    /// </summary>
    /// <param name="tenantId">Optional tenant ID filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of registers.</returns>
    Task<IReadOnlyList<RegisterViewModel>> GetRegistersAsync(
        string? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single register by ID.
    /// </summary>
    /// <param name="registerId">Register identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Register details or null if not found.</returns>
    Task<RegisterViewModel?> GetRegisterAsync(
        string registerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates register creation (phase 1 of genesis).
    /// </summary>
    /// <param name="request">Register creation request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Initiate response with unsigned control record.</returns>
    Task<InitiateRegisterResponse?> InitiateRegisterAsync(
        CreateRegisterRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finalizes register creation (phase 2 of genesis).
    /// </summary>
    /// <param name="request">Finalize request with signed control record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created register or null on failure.</returns>
    Task<RegisterViewModel?> FinalizeRegisterAsync(
        FinalizeRegisterRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Request model for creating a new register.
/// </summary>
public record CreateRegisterRequest
{
    /// <summary>
    /// Register name (1-38 characters)
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Tenant identifier (organization ID)
    /// </summary>
    public required string TenantId { get; init; }

    /// <summary>
    /// Wallet address for signing the control record
    /// </summary>
    public string? WalletAddress { get; init; }

    /// <summary>
    /// Whether to advertise register to network peers
    /// </summary>
    public bool Advertise { get; init; }

    /// <summary>
    /// Whether to maintain full transaction history
    /// </summary>
    public bool IsFullReplica { get; init; } = true;
}

/// <summary>
/// Response from initiating register creation.
/// </summary>
public record InitiateRegisterResponse
{
    /// <summary>
    /// Generated register ID
    /// </summary>
    public required string RegisterId { get; init; }

    /// <summary>
    /// Unsigned control record to be signed by the user
    /// </summary>
    public required string UnsignedControlRecord { get; init; }

    /// <summary>
    /// When this initiation request expires
    /// </summary>
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Request to finalize register creation.
/// </summary>
public record FinalizeRegisterRequest
{
    /// <summary>
    /// Register ID from initiation response
    /// </summary>
    public required string RegisterId { get; init; }

    /// <summary>
    /// Signed control record
    /// </summary>
    public required string SignedControlRecord { get; init; }
}
