// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentValidation;

namespace Sorcha.Blueprint.Models.Credentials;

/// <summary>
/// FluentValidation validator for <see cref="CredentialPresentation"/>.
/// </summary>
public class CredentialPresentationValidator : AbstractValidator<CredentialPresentation>
{
    /// <summary>
    /// Initializes validation rules for credential presentations.
    /// </summary>
    public CredentialPresentationValidator()
    {
        RuleFor(x => x.CredentialId)
            .NotEmpty().WithMessage("Credential ID is required")
            .MaximumLength(500).WithMessage("Credential ID must not exceed 500 characters");

        RuleFor(x => x.DisclosedClaims)
            .NotNull().WithMessage("Disclosed claims must not be null");

        RuleFor(x => x.RawPresentation)
            .NotEmpty().WithMessage("Raw presentation token is required");
    }
}
