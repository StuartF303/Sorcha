// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Moq;
using Sorcha.Blueprint.Engine.Credentials;
using Sorcha.Blueprint.Models.Credentials;
using Sorcha.Cryptography.SdJwt;

namespace Sorcha.Blueprint.Engine.Tests.Credentials;

/// <summary>
/// Tests for CredentialVerifier with partial (selective) disclosure.
/// Verifies that the verifier correctly handles presentations with
/// only a subset of claims disclosed.
/// </summary>
public class CredentialVerifierSelectiveDisclosureTests
{
    private readonly Mock<ISdJwtService> _sdJwtMock = new();
    private readonly CredentialVerifier _verifier;

    public CredentialVerifierSelectiveDisclosureTests()
    {
        _verifier = new CredentialVerifier(_sdJwtMock.Object);
    }

    [Fact]
    public async Task VerifyAsync_RequiredClaimsDisclosed_Accepted()
    {
        // Arrange — Requirement needs license_type; presentation discloses it
        var requirements = new[]
        {
            new CredentialRequirement
            {
                Type = "LicenseCredential",
                RequiredClaims = [new ClaimConstraint { ClaimName = "license_type" }]
            }
        };

        var presentations = new[]
        {
            new CredentialPresentation
            {
                CredentialId = "cred-1",
                DisclosedClaims = new Dictionary<string, object>
                {
                    ["type"] = "LicenseCredential",
                    ["license_type"] = "ClassA"
                    // Other claims (name, address, etc.) are NOT disclosed
                },
                RawPresentation = "jwt~disc1~"
            }
        };

        // Act
        var result = await _verifier.VerifyAsync(requirements, presentations);

        // Assert
        result.IsValid.Should().BeTrue();
        result.VerifiedCredentials.Should().HaveCount(1);
        result.VerifiedCredentials[0].VerifiedClaims.Should().ContainKey("license_type");
        result.VerifiedCredentials[0].VerifiedClaims.Should().HaveCount(2); // type + license_type
    }

    [Fact]
    public async Task VerifyAsync_RequiredClaimNotDisclosed_Rejected()
    {
        // Arrange — Requirement needs license_type, but it's not disclosed
        var requirements = new[]
        {
            new CredentialRequirement
            {
                Type = "LicenseCredential",
                RequiredClaims = [new ClaimConstraint { ClaimName = "license_type" }]
            }
        };

        var presentations = new[]
        {
            new CredentialPresentation
            {
                CredentialId = "cred-1",
                DisclosedClaims = new Dictionary<string, object>
                {
                    ["type"] = "LicenseCredential",
                    ["name"] = "Alice"
                    // license_type intentionally NOT disclosed
                },
                RawPresentation = "jwt~disc1~"
            }
        };

        // Act
        var result = await _verifier.VerifyAsync(requirements, presentations);

        // Assert — Rejected because required claim is not disclosed
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(1);
        result.Errors[0].FailureReason.Should().Be(CredentialFailureReason.ClaimMismatch);
        result.Errors[0].Message.Should().Contain("license_type");
        result.Errors[0].Message.Should().Contain("not disclosed");
    }

    [Fact]
    public async Task VerifyAsync_RequiredClaimValueMismatch_Rejected()
    {
        // Arrange — Requirement needs license_type = "ClassA", but disclosed as "ClassB"
        var requirements = new[]
        {
            new CredentialRequirement
            {
                Type = "LicenseCredential",
                RequiredClaims =
                [
                    new ClaimConstraint { ClaimName = "license_type", ExpectedValue = "ClassA" }
                ]
            }
        };

        var presentations = new[]
        {
            new CredentialPresentation
            {
                CredentialId = "cred-1",
                DisclosedClaims = new Dictionary<string, object>
                {
                    ["type"] = "LicenseCredential",
                    ["license_type"] = "ClassB" // Wrong value
                },
                RawPresentation = "jwt~disc1~"
            }
        };

        // Act
        var result = await _verifier.VerifyAsync(requirements, presentations);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors[0].FailureReason.Should().Be(CredentialFailureReason.ClaimMismatch);
    }

    [Fact]
    public async Task VerifyAsync_PartialDisclosure_ExtraClaimsIgnored()
    {
        // Arrange — Requirement only needs license_type.
        // Credential has 5 claims but holder discloses 2 (type + license_type).
        // Extra undisclosed claims should not cause rejection.
        var requirements = new[]
        {
            new CredentialRequirement
            {
                Type = "LicenseCredential",
                RequiredClaims = [new ClaimConstraint { ClaimName = "license_type" }]
            }
        };

        var presentations = new[]
        {
            new CredentialPresentation
            {
                CredentialId = "cred-1",
                DisclosedClaims = new Dictionary<string, object>
                {
                    ["type"] = "LicenseCredential",
                    ["license_type"] = "ClassA"
                    // name, address, qualification_date are not disclosed — that's fine
                },
                RawPresentation = "jwt~disc1~"
            }
        };

        // Act
        var result = await _verifier.VerifyAsync(requirements, presentations);

        // Assert — Accepted with only the disclosed claims verified
        result.IsValid.Should().BeTrue();
        result.VerifiedCredentials.Should().HaveCount(1);
        result.VerifiedCredentials[0].VerifiedClaims.Should().HaveCount(2); // Only type + license_type
        result.VerifiedCredentials[0].VerifiedClaims.Should().NotContainKey("name");
        result.VerifiedCredentials[0].VerifiedClaims.Should().NotContainKey("address");
    }

    [Fact]
    public async Task VerifyAsync_NoRequiredClaims_TypeMatchSufficient()
    {
        // Arrange — Requirement has no claim constraints, just type match.
        // Even with minimal disclosure, it should match.
        var requirements = new[]
        {
            new CredentialRequirement
            {
                Type = "LicenseCredential"
                // No RequiredClaims
            }
        };

        var presentations = new[]
        {
            new CredentialPresentation
            {
                CredentialId = "cred-1",
                DisclosedClaims = new Dictionary<string, object>
                {
                    ["type"] = "LicenseCredential"
                    // Holder chose to disclose nothing else
                },
                RawPresentation = "jwt~"
            }
        };

        // Act
        var result = await _verifier.VerifyAsync(requirements, presentations);

        // Assert — Accepted based on type alone
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyAsync_MultipleRequirementsWithPartialDisclosure_AllMustSatisfy()
    {
        // Arrange — Two requirements, each needing different claims from different credentials
        var requirements = new[]
        {
            new CredentialRequirement
            {
                Type = "IdentityAttestation",
                RequiredClaims = [new ClaimConstraint { ClaimName = "name" }]
            },
            new CredentialRequirement
            {
                Type = "LicenseCredential",
                RequiredClaims = [new ClaimConstraint { ClaimName = "license_type" }]
            }
        };

        // Both credentials are partially disclosed
        var presentations = new[]
        {
            new CredentialPresentation
            {
                CredentialId = "identity-cred",
                DisclosedClaims = new Dictionary<string, object>
                {
                    ["type"] = "IdentityAttestation",
                    ["name"] = "Alice"
                    // birthdate, address not disclosed
                },
                RawPresentation = "jwt~disc1~"
            },
            new CredentialPresentation
            {
                CredentialId = "license-cred",
                DisclosedClaims = new Dictionary<string, object>
                {
                    ["type"] = "LicenseCredential",
                    ["license_type"] = "ClassA"
                    // holder_name, expiry not disclosed
                },
                RawPresentation = "jwt~disc2~"
            }
        };

        // Act
        var result = await _verifier.VerifyAsync(requirements, presentations);

        // Assert — Both accepted with partial disclosure
        result.IsValid.Should().BeTrue();
        result.VerifiedCredentials.Should().HaveCount(2);
    }

    [Fact]
    public async Task VerifyAsync_VctClaimUsedForType_AcceptedForPartialDisclosure()
    {
        // Arrange — SD-JWT VC uses "vct" instead of "type" for credential type
        var requirements = new[]
        {
            new CredentialRequirement
            {
                Type = "LicenseCredential",
                RequiredClaims = [new ClaimConstraint { ClaimName = "license_type" }]
            }
        };

        var presentations = new[]
        {
            new CredentialPresentation
            {
                CredentialId = "cred-1",
                DisclosedClaims = new Dictionary<string, object>
                {
                    ["vct"] = "LicenseCredential", // Using vct instead of type
                    ["license_type"] = "ClassA"
                },
                RawPresentation = "jwt~disc1~"
            }
        };

        // Act
        var result = await _verifier.VerifyAsync(requirements, presentations);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
