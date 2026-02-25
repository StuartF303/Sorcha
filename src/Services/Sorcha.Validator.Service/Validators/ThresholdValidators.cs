// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentValidation;
using Sorcha.Validator.Service.Models;

namespace Sorcha.Validator.Service.Validators;

/// <summary>
/// Validates BLS threshold setup requests per Constitution Principle II.
/// </summary>
public class ThresholdSetupRequestValidator : AbstractValidator<ThresholdSetupRequest>
{
    public ThresholdSetupRequestValidator()
    {
        RuleFor(x => x.RegisterId)
            .NotEmpty().WithMessage("RegisterId is required");

        RuleFor(x => x.Threshold)
            .GreaterThan(0u).WithMessage("Threshold must be greater than 0");

        RuleFor(x => x.TotalValidators)
            .GreaterThan(0u).WithMessage("TotalValidators must be greater than 0");

        RuleFor(x => x.Threshold)
            .LessThanOrEqualTo(x => x.TotalValidators)
            .WithMessage("Threshold must be <= TotalValidators");

        RuleFor(x => x.ValidatorIds)
            .NotEmpty().WithMessage("ValidatorIds is required")
            .Must((request, ids) => ids.Length == (int)request.TotalValidators)
            .WithMessage("ValidatorIds count must match TotalValidators");

        RuleForEach(x => x.ValidatorIds)
            .NotEmpty().WithMessage("Each ValidatorId must be non-empty");
    }
}

/// <summary>
/// Validates BLS threshold signing requests per Constitution Principle II.
/// </summary>
public class ThresholdSignRequestValidator : AbstractValidator<ThresholdSignRequest>
{
    public ThresholdSignRequestValidator()
    {
        RuleFor(x => x.RegisterId)
            .NotEmpty().WithMessage("RegisterId is required");

        RuleFor(x => x.DocketHash)
            .NotEmpty().WithMessage("DocketHash is required");

        RuleFor(x => x.ValidatorId)
            .NotEmpty().WithMessage("ValidatorId is required");

        RuleFor(x => x.PartialSignature)
            .NotEmpty().WithMessage("PartialSignature is required")
            .Must(BeValidBase64).WithMessage("PartialSignature must be valid base64");
    }

    private static bool BeValidBase64(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        try
        {
            Convert.FromBase64String(value);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
