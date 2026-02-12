// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentValidation;

namespace Sorcha.Blueprint.Models.Credentials;

/// <summary>
/// FluentValidation validator for <see cref="CredentialIssuanceConfig"/>.
/// </summary>
public class CredentialIssuanceConfigValidator : AbstractValidator<CredentialIssuanceConfig>
{
    /// <summary>
    /// Initializes validation rules for credential issuance configuration.
    /// </summary>
    public CredentialIssuanceConfigValidator()
    {
        RuleFor(x => x.CredentialType)
            .NotEmpty().WithMessage("Credential type is required")
            .MaximumLength(200).WithMessage("Credential type must not exceed 200 characters");

        RuleFor(x => x.ClaimMappings)
            .NotEmpty().WithMessage("At least one claim mapping is required");

        RuleForEach(x => x.ClaimMappings)
            .SetValidator(new ClaimMappingValidator());

        RuleFor(x => x.RecipientParticipantId)
            .NotEmpty().WithMessage("Recipient participant ID is required")
            .MaximumLength(100).WithMessage("Recipient participant ID must not exceed 100 characters");

        RuleFor(x => x.ExpiryDuration)
            .Must(BeValidIsoDuration).WithMessage("Expiry duration must be a valid ISO 8601 duration (e.g., 'P365D')")
            .When(x => x.ExpiryDuration != null);

        RuleFor(x => x.RegisterId)
            .MaximumLength(100).WithMessage("Register ID must not exceed 100 characters")
            .When(x => x.RegisterId != null);
    }

    private static bool BeValidIsoDuration(string? duration)
    {
        if (string.IsNullOrWhiteSpace(duration))
            return false;

        // Basic ISO 8601 duration validation: must start with P
        return duration.StartsWith('P') && duration.Length > 1;
    }
}

/// <summary>
/// FluentValidation validator for <see cref="ClaimMapping"/>.
/// </summary>
public class ClaimMappingValidator : AbstractValidator<ClaimMapping>
{
    /// <summary>
    /// Initializes validation rules for claim mappings.
    /// </summary>
    public ClaimMappingValidator()
    {
        RuleFor(x => x.ClaimName)
            .NotEmpty().WithMessage("Claim name is required")
            .MaximumLength(200).WithMessage("Claim name must not exceed 200 characters");

        RuleFor(x => x.SourceField)
            .NotEmpty().WithMessage("Source field is required")
            .Must(BeValidJsonPointer).WithMessage("Source field must be a valid JSON Pointer (e.g., '/fieldName')")
            .MaximumLength(500).WithMessage("Source field must not exceed 500 characters");
    }

    private static bool BeValidJsonPointer(string sourceField)
    {
        return sourceField.StartsWith('/');
    }
}
