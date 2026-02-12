// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Moq;
using Sorcha.Blueprint.Engine.Credentials;
using Sorcha.Blueprint.Models.Credentials;
using Sorcha.Cryptography.SdJwt;

namespace Sorcha.Blueprint.Engine.Tests.Credentials;

public class CredentialVerifierRevocationTests
{
    private readonly Mock<ISdJwtService> _sdJwtServiceMock = new();
    private readonly Mock<IRevocationChecker> _revocationCheckerMock = new();

    private CredentialPresentation CreatePresentation(
        string credentialId, string type, string issuer, Dictionary<string, object>? extraClaims = null)
    {
        var claims = new Dictionary<string, object>
        {
            ["type"] = type,
            ["iss"] = issuer
        };

        if (extraClaims != null)
        {
            foreach (var kvp in extraClaims)
                claims[kvp.Key] = kvp.Value;
        }

        return new CredentialPresentation
        {
            CredentialId = credentialId,
            DisclosedClaims = claims
        };
    }

    [Fact]
    public async Task VerifyAsync_ActiveCredential_Accepted()
    {
        // Arrange
        _revocationCheckerMock
            .Setup(r => r.CheckRevocationStatusAsync("cred-1", "issuer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Active");

        var verifier = new CredentialVerifier(_sdJwtServiceMock.Object, _revocationCheckerMock.Object);
        var requirements = new[] { new CredentialRequirement { Type = "LicenseCredential" } };
        var presentations = new[] { CreatePresentation("cred-1", "LicenseCredential", "issuer-1") };

        // Act
        var result = await verifier.VerifyAsync(requirements, presentations);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.VerifiedCredentials.Should().HaveCount(1);
        result.VerifiedCredentials[0].RevocationStatus.Should().Be("Active");
    }

    [Fact]
    public async Task VerifyAsync_RevokedCredential_Rejected()
    {
        // Arrange
        _revocationCheckerMock
            .Setup(r => r.CheckRevocationStatusAsync("cred-revoked", "issuer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync("Revoked");

        var verifier = new CredentialVerifier(_sdJwtServiceMock.Object, _revocationCheckerMock.Object);
        var requirements = new[] { new CredentialRequirement { Type = "LicenseCredential" } };
        var presentations = new[] { CreatePresentation("cred-revoked", "LicenseCredential", "issuer-1") };

        // Act
        var result = await verifier.VerifyAsync(requirements, presentations);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.FailureReason == CredentialFailureReason.Revoked);
    }

    [Fact]
    public async Task VerifyAsync_UnavailableStatus_FailClosed_Blocked()
    {
        // Arrange
        _revocationCheckerMock
            .Setup(r => r.CheckRevocationStatusAsync("cred-unknown", "issuer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var verifier = new CredentialVerifier(_sdJwtServiceMock.Object, _revocationCheckerMock.Object);
        var requirements = new[]
        {
            new CredentialRequirement
            {
                Type = "LicenseCredential",
                RevocationCheckPolicy = RevocationCheckPolicy.FailClosed
            }
        };
        var presentations = new[] { CreatePresentation("cred-unknown", "LicenseCredential", "issuer-1") };

        // Act
        var result = await verifier.VerifyAsync(requirements, presentations);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.FailureReason == CredentialFailureReason.RevocationCheckUnavailable);
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task VerifyAsync_UnavailableStatus_FailOpen_AcceptedWithWarning()
    {
        // Arrange
        _revocationCheckerMock
            .Setup(r => r.CheckRevocationStatusAsync("cred-unknown", "issuer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var verifier = new CredentialVerifier(_sdJwtServiceMock.Object, _revocationCheckerMock.Object);
        var requirements = new[]
        {
            new CredentialRequirement
            {
                Type = "LicenseCredential",
                RevocationCheckPolicy = RevocationCheckPolicy.FailOpen
            }
        };
        var presentations = new[] { CreatePresentation("cred-unknown", "LicenseCredential", "issuer-1") };

        // Act
        var result = await verifier.VerifyAsync(requirements, presentations);

        // Assert
        result.IsValid.Should().BeTrue();
        result.VerifiedCredentials.Should().HaveCount(1);
        result.VerifiedCredentials[0].RevocationStatus.Should().Be("Unknown");
        result.Warnings.Should().ContainSingle(w => w.Contains("fail-open policy"));
    }

    [Fact]
    public async Task VerifyAsync_CheckerThrows_FailClosed_Blocked()
    {
        // Arrange
        _revocationCheckerMock
            .Setup(r => r.CheckRevocationStatusAsync("cred-error", "issuer-1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var verifier = new CredentialVerifier(_sdJwtServiceMock.Object, _revocationCheckerMock.Object);
        var requirements = new[]
        {
            new CredentialRequirement
            {
                Type = "LicenseCredential",
                RevocationCheckPolicy = RevocationCheckPolicy.FailClosed
            }
        };
        var presentations = new[] { CreatePresentation("cred-error", "LicenseCredential", "issuer-1") };

        // Act
        var result = await verifier.VerifyAsync(requirements, presentations);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e =>
            e.FailureReason == CredentialFailureReason.RevocationCheckUnavailable);
    }

    [Fact]
    public async Task VerifyAsync_CheckerThrows_FailOpen_AcceptedWithWarning()
    {
        // Arrange
        _revocationCheckerMock
            .Setup(r => r.CheckRevocationStatusAsync("cred-error", "issuer-1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var verifier = new CredentialVerifier(_sdJwtServiceMock.Object, _revocationCheckerMock.Object);
        var requirements = new[]
        {
            new CredentialRequirement
            {
                Type = "LicenseCredential",
                RevocationCheckPolicy = RevocationCheckPolicy.FailOpen
            }
        };
        var presentations = new[] { CreatePresentation("cred-error", "LicenseCredential", "issuer-1") };

        // Act
        var result = await verifier.VerifyAsync(requirements, presentations);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().ContainSingle(w => w.Contains("fail-open policy"));
        result.VerifiedCredentials[0].RevocationStatus.Should().Be("Unknown");
    }

    [Fact]
    public async Task VerifyAsync_NoRevocationChecker_SkipsCheckAndAccepts()
    {
        // Arrange — no revocation checker injected
        var verifier = new CredentialVerifier(_sdJwtServiceMock.Object);
        var requirements = new[] { new CredentialRequirement { Type = "LicenseCredential" } };
        var presentations = new[] { CreatePresentation("cred-1", "LicenseCredential", "issuer-1") };

        // Act
        var result = await verifier.VerifyAsync(requirements, presentations);

        // Assert
        result.IsValid.Should().BeTrue();
        result.VerifiedCredentials[0].RevocationStatus.Should().Be("Active");
        result.Warnings.Should().BeEmpty();
    }
}
