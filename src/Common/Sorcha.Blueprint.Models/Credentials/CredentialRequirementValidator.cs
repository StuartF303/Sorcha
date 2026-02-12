// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentValidation;

namespace Sorcha.Blueprint.Models.Credentials;

/// <summary>
/// FluentValidation validator for <see cref="CredentialRequirement"/>.
/// </summary>
public class CredentialRequirementValidator : AbstractValidator<CredentialRequirement>
{
    /// <summary>
    /// Initializes validation rules for credential requirements.
    /// </summary>
    public CredentialRequirementValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Credential type is required")
            .MaximumLength(200).WithMessage("Credential type must not exceed 200 characters");

        RuleForEach(x => x.AcceptedIssuers)
            .NotEmpty().WithMessage("Accepted issuer must not be empty")
            .MaximumLength(500).WithMessage("Issuer identifier must not exceed 500 characters")
            .When(x => x.AcceptedIssuers != null);

        RuleForEach(x => x.RequiredClaims)
            .SetValidator(new ClaimConstraintValidator())
            .When(x => x.RequiredClaims != null);

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters")
            .When(x => x.Description != null);
    }
}

/// <summary>
/// FluentValidation validator for <see cref="ClaimConstraint"/>.
/// </summary>
public class ClaimConstraintValidator : AbstractValidator<ClaimConstraint>
{
    /// <summary>
    /// Initializes validation rules for claim constraints.
    /// </summary>
    public ClaimConstraintValidator()
    {
        RuleFor(x => x.ClaimName)
            .NotEmpty().WithMessage("Claim name is required")
            .MaximumLength(200).WithMessage("Claim name must not exceed 200 characters");
    }
}
