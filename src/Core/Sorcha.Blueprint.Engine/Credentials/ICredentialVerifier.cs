// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Blueprint.Models.Credentials;

namespace Sorcha.Blueprint.Engine.Credentials;

/// <summary>
/// Verifies credential presentations against action credential requirements.
/// </summary>
public interface ICredentialVerifier
{
    /// <summary>
    /// Verifies that the provided credential presentations satisfy all action requirements.
    /// </summary>
    /// <param name="requirements">The credential requirements defined on the action.</param>
    /// <param name="presentations">The credential presentations submitted by the participant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result indicating success or specific failures per requirement.</returns>
    Task<CredentialValidationResult> VerifyAsync(
        IEnumerable<CredentialRequirement> requirements,
        IEnumerable<CredentialPresentation> presentations,
        CancellationToken cancellationToken = default);
}
