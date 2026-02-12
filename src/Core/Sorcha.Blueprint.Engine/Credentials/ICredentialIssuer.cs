// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Blueprint.Engine.Models;
using Sorcha.Blueprint.Models.Credentials;

namespace Sorcha.Blueprint.Engine.Credentials;

/// <summary>
/// Issues verifiable credentials from blueprint action execution.
/// </summary>
public interface ICredentialIssuer
{
    /// <summary>
    /// Issues a new SD-JWT VC credential based on the action's issuance configuration.
    /// </summary>
    /// <param name="config">Credential issuance configuration from the action definition.</param>
    /// <param name="processedData">Processed action data (original + calculated fields).</param>
    /// <param name="issuerDid">DID URI or wallet address of the issuing authority.</param>
    /// <param name="recipientDid">DID URI or wallet address of the credential recipient.</param>
    /// <param name="signingKey">Issuer's private key bytes for signing.</param>
    /// <param name="algorithm">Signing algorithm (e.g., "ES256", "EdDSA").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Issued credential info including the signed SD-JWT VC token.</returns>
    Task<IssuedCredentialInfo> IssueAsync(
        CredentialIssuanceConfig config,
        Dictionary<string, object> processedData,
        string issuerDid,
        string recipientDid,
        byte[] signingKey,
        string algorithm,
        CancellationToken cancellationToken = default);
}
