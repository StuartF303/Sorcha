// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Refit;
using Sorcha.Cli.Models;

namespace Sorcha.Cli.Services;

/// <summary>
/// Refit client interface for verifiable credential operations.
/// Routes through the API Gateway to the appropriate service.
/// </summary>
public interface ICredentialServiceClient
{
    /// <summary>
    /// Lists all credentials for the current user.
    /// </summary>
    [Get("/api/credentials")]
    Task<List<CredentialSummary>> ListCredentialsAsync([Header("Authorization")] string authorization);

    /// <summary>
    /// Gets a credential by ID.
    /// </summary>
    [Get("/api/credentials/{id}")]
    Task<CredentialDetail> GetCredentialAsync(string id, [Header("Authorization")] string authorization);

    /// <summary>
    /// Issues a new verifiable credential.
    /// </summary>
    [Post("/api/credentials")]
    Task<CredentialDetail> IssueCredentialAsync([Body] IssueCredentialRequest request, [Header("Authorization")] string authorization);

    /// <summary>
    /// Presents a credential to a verifier.
    /// </summary>
    [Post("/api/credentials/{id}/present")]
    Task<PresentCredentialResponse> PresentCredentialAsync(string id, [Body] PresentCredentialRequest request, [Header("Authorization")] string authorization);

    /// <summary>
    /// Verifies a credential.
    /// </summary>
    [Post("/api/credentials/{id}/verify")]
    Task<VerifyCredentialResponse> VerifyCredentialAsync(string id, [Header("Authorization")] string authorization);

    /// <summary>
    /// Revokes a credential.
    /// </summary>
    [Post("/api/credentials/{id}/revoke")]
    Task RevokeCredentialAsync(string id, [Header("Authorization")] string authorization);

    /// <summary>
    /// Gets the status of a credential.
    /// </summary>
    [Get("/api/credentials/{id}/status")]
    Task<CredentialStatusResponse> GetCredentialStatusAsync(string id, [Header("Authorization")] string authorization);
}
