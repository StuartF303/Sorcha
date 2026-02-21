// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using Sorcha.Wallet.Service.Credentials;
using Sorcha.Wallet.Service.Models;
using Sorcha.Wallet.Service.Services;

namespace Sorcha.Wallet.Service.Tests.Presentations;

public class PresentationRequestServiceTests
{
    private readonly Mock<ICredentialStore> _storeMock = new();
    private readonly Mock<ILogger<PresentationRequestService>> _loggerMock = new();
    private readonly PresentationRequestService _service;

    public PresentationRequestServiceTests()
    {
        _service = new PresentationRequestService(_storeMock.Object, _loggerMock.Object);
    }

    // --- CreateRequestAsync ---

    [Fact]
    public async Task CreateRequestAsync_ValidDto_ReturnsRequestWithNonce()
    {
        var dto = CreateTestDto();
        var request = await _service.CreateRequestAsync(dto);

        request.Should().NotBeNull();
        request.Id.Should().NotBeNullOrEmpty();
        request.Nonce.Should().HaveLength(32);
        request.CredentialType.Should().Be("ChemicalHandlingLicense");
        request.Status.Should().Be(PresentationStatus.Pending);
        request.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task CreateRequestAsync_CustomTtl_SetsExpiryCorrectly()
    {
        var dto = CreateTestDto();
        dto = new CreatePresentationRequestDto
        {
            CredentialType = dto.CredentialType,
            CallbackUrl = dto.CallbackUrl,
            TtlSeconds = 60
        };

        var request = await _service.CreateRequestAsync(dto);

        request.ExpiresAt.Should().BeCloseTo(
            DateTimeOffset.UtcNow.AddSeconds(60), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateRequestAsync_EachRequestHasUniqueNonce()
    {
        var dto = CreateTestDto();
        var r1 = await _service.CreateRequestAsync(dto);
        var r2 = await _service.CreateRequestAsync(dto);

        r1.Nonce.Should().NotBe(r2.Nonce);
        r1.Id.Should().NotBe(r2.Id);
    }

    // --- GetRequestAsync ---

    [Fact]
    public async Task GetRequestAsync_ExistingRequest_ReturnsRequest()
    {
        var request = await _service.CreateRequestAsync(CreateTestDto());
        var fetched = await _service.GetRequestAsync(request.Id);

        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(request.Id);
    }

    [Fact]
    public async Task GetRequestAsync_NonExistent_ReturnsNull()
    {
        var result = await _service.GetRequestAsync("nonexistent");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRequestAsync_ExpiredRequest_TransitionsToExpired()
    {
        var dto = new CreatePresentationRequestDto
        {
            CredentialType = "Test",
            CallbackUrl = "https://example.com/callback",
            TtlSeconds = -1 // Already expired
        };

        var request = await _service.CreateRequestAsync(dto);
        var fetched = await _service.GetRequestAsync(request.Id);

        fetched.Should().NotBeNull();
        fetched!.Status.Should().Be(PresentationStatus.Expired);
    }

    // --- FindMatchingCredentialsAsync ---

    [Fact]
    public async Task FindMatchingCredentialsAsync_MatchingCredentials_ReturnsInfo()
    {
        var request = await _service.CreateRequestAsync(CreateTestDto());

        _storeMock
            .Setup(s => s.MatchAsync("wallet-1", "ChemicalHandlingLicense", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateTestCredential("cred-1", "ChemicalHandlingLicense", "did:sorcha:w:hse")
            ]);

        var matches = await _service.FindMatchingCredentialsAsync(request, "wallet-1");

        matches.Should().HaveCount(1);
        matches[0].CredentialId.Should().Be("cred-1");
        matches[0].Type.Should().Be("ChemicalHandlingLicense");
    }

    [Fact]
    public async Task FindMatchingCredentialsAsync_WithRequiredClaims_IncludesRequestedClaims()
    {
        var dto = new CreatePresentationRequestDto
        {
            CredentialType = "ChemicalHandlingLicense",
            CallbackUrl = "https://example.com/callback",
            RequiredClaims = [new ClaimConstraint { ClaimName = "class" }]
        };
        var request = await _service.CreateRequestAsync(dto);

        _storeMock
            .Setup(s => s.MatchAsync("wallet-1", "ChemicalHandlingLicense", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateTestCredential("cred-1", "ChemicalHandlingLicense", "did:sorcha:w:hse",
                    """{"class":"CategoryB","permitNumber":"HSE-001"}""")
            ]);

        var matches = await _service.FindMatchingCredentialsAsync(request, "wallet-1");

        matches.Should().HaveCount(1);
        matches[0].RequestedClaims.Should().Contain("class");
        matches[0].DisclosableClaims.Should().Contain("class");
        matches[0].DisclosableClaims.Should().Contain("permitNumber");
    }

    // --- SubmitPresentationAsync ---

    [Fact]
    public async Task SubmitPresentationAsync_ValidPresentation_VerifiedStatus()
    {
        var request = await _service.CreateRequestAsync(CreateTestDto());

        _storeMock
            .Setup(s => s.GetByIdAsync("cred-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestCredential("cred-1", "ChemicalHandlingLicense", "did:sorcha:w:hse"));

        _storeMock
            .Setup(s => s.RecordPresentationAsync("cred-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.SubmitPresentationAsync(
            request.Id, "cred-1", ["class"], "vp-token");

        result.Status.Should().Be(PresentationStatus.Verified);
        result.VpToken.Should().Be("vp-token");
        result.VerificationResult.Should().NotBeNullOrEmpty();

        var verification = JsonSerializer.Deserialize<VerificationResult>(result.VerificationResult!);
        verification!.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitPresentationAsync_TypeMismatch_DeniedStatus()
    {
        var request = await _service.CreateRequestAsync(CreateTestDto());

        _storeMock
            .Setup(s => s.GetByIdAsync("cred-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestCredential("cred-1", "DifferentType", "did:sorcha:w:hse"));

        var result = await _service.SubmitPresentationAsync(
            request.Id, "cred-1", [], "vp-token");

        result.Status.Should().Be(PresentationStatus.Denied);

        var verification = JsonSerializer.Deserialize<VerificationResult>(result.VerificationResult!);
        verification!.IsValid.Should().BeFalse();
        verification.Errors.Should().Contain(e => e.FailureReason == "TypeMismatch");
    }

    [Fact]
    public async Task SubmitPresentationAsync_RevokedCredential_DeniedStatus()
    {
        var request = await _service.CreateRequestAsync(CreateTestDto());
        var cred = CreateTestCredential("cred-1", "ChemicalHandlingLicense", "did:sorcha:w:hse");
        cred.Status = "Revoked";

        _storeMock
            .Setup(s => s.GetByIdAsync("cred-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cred);

        var result = await _service.SubmitPresentationAsync(
            request.Id, "cred-1", [], "vp-token");

        result.Status.Should().Be(PresentationStatus.Denied);

        var verification = JsonSerializer.Deserialize<VerificationResult>(result.VerificationResult!);
        verification!.Errors.Should().Contain(e => e.FailureReason == "Revoked");
    }

    [Fact]
    public async Task SubmitPresentationAsync_ExpiredCredential_DeniedStatus()
    {
        var request = await _service.CreateRequestAsync(CreateTestDto());
        var cred = CreateTestCredential("cred-1", "ChemicalHandlingLicense", "did:sorcha:w:hse");
        cred.ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1);

        _storeMock
            .Setup(s => s.GetByIdAsync("cred-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cred);

        var result = await _service.SubmitPresentationAsync(
            request.Id, "cred-1", [], "vp-token");

        result.Status.Should().Be(PresentationStatus.Denied);

        var verification = JsonSerializer.Deserialize<VerificationResult>(result.VerificationResult!);
        verification!.Errors.Should().Contain(e => e.FailureReason == "Expired");
    }

    [Fact]
    public async Task SubmitPresentationAsync_UntrustedIssuer_DeniedStatus()
    {
        var dto = new CreatePresentationRequestDto
        {
            CredentialType = "ChemicalHandlingLicense",
            CallbackUrl = "https://example.com/callback",
            AcceptedIssuers = ["did:sorcha:w:trusted-issuer"]
        };
        var request = await _service.CreateRequestAsync(dto);

        _storeMock
            .Setup(s => s.GetByIdAsync("cred-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestCredential("cred-1", "ChemicalHandlingLicense", "did:sorcha:w:untrusted"));

        var result = await _service.SubmitPresentationAsync(
            request.Id, "cred-1", [], "vp-token");

        result.Status.Should().Be(PresentationStatus.Denied);

        var verification = JsonSerializer.Deserialize<VerificationResult>(result.VerificationResult!);
        verification!.Errors.Should().Contain(e => e.FailureReason == "UntrustedIssuer");
    }

    [Fact]
    public async Task SubmitPresentationAsync_CredentialNotFound_DeniedStatus()
    {
        var request = await _service.CreateRequestAsync(CreateTestDto());

        _storeMock
            .Setup(s => s.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CredentialEntity?)null);

        var result = await _service.SubmitPresentationAsync(
            request.Id, "missing", [], "vp-token");

        result.Status.Should().Be(PresentationStatus.Denied);
    }

    [Fact]
    public async Task SubmitPresentationAsync_NonexistentRequest_ThrowsKeyNotFoundException()
    {
        var act = () => _service.SubmitPresentationAsync(
            "missing", "cred-1", [], "vp-token");

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task SubmitPresentationAsync_ExpiredRequest_ThrowsInvalidOperation()
    {
        var dto = new CreatePresentationRequestDto
        {
            CredentialType = "Test",
            CallbackUrl = "https://example.com/callback",
            TtlSeconds = -1
        };
        var request = await _service.CreateRequestAsync(dto);

        var act = () => _service.SubmitPresentationAsync(
            request.Id, "cred-1", [], "vp-token");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*expired*");
    }

    [Fact]
    public async Task SubmitPresentationAsync_RecordsPresentationUsage()
    {
        var request = await _service.CreateRequestAsync(CreateTestDto());

        _storeMock
            .Setup(s => s.GetByIdAsync("cred-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestCredential("cred-1", "ChemicalHandlingLicense", "did:sorcha:w:hse"));

        _storeMock
            .Setup(s => s.RecordPresentationAsync("cred-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _service.SubmitPresentationAsync(request.Id, "cred-1", [], "vp-token");

        _storeMock.Verify(
            s => s.RecordPresentationAsync("cred-1", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SubmitPresentationAsync_RequiredClaimMissing_DeniedWithMissingClaimError()
    {
        var dto = new CreatePresentationRequestDto
        {
            CredentialType = "ChemicalHandlingLicense",
            CallbackUrl = "https://example.com/callback",
            RequiredClaims = [new ClaimConstraint { ClaimName = "nonExistentClaim" }]
        };
        var request = await _service.CreateRequestAsync(dto);

        _storeMock
            .Setup(s => s.GetByIdAsync("cred-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestCredential("cred-1", "ChemicalHandlingLicense", "did:sorcha:w:hse",
                """{"class":"CategoryB"}"""));

        var result = await _service.SubmitPresentationAsync(
            request.Id, "cred-1", [], "vp-token");

        result.Status.Should().Be(PresentationStatus.Denied);

        var verification = JsonSerializer.Deserialize<VerificationResult>(result.VerificationResult!);
        verification!.Errors.Should().Contain(e => e.FailureReason == "MissingClaim");
    }

    [Fact]
    public async Task SubmitPresentationAsync_ClaimValueMismatch_DeniedWithMismatchError()
    {
        var dto = new CreatePresentationRequestDto
        {
            CredentialType = "ChemicalHandlingLicense",
            CallbackUrl = "https://example.com/callback",
            RequiredClaims = [new ClaimConstraint { ClaimName = "class", ExpectedValue = "CategoryA" }]
        };
        var request = await _service.CreateRequestAsync(dto);

        _storeMock
            .Setup(s => s.GetByIdAsync("cred-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestCredential("cred-1", "ChemicalHandlingLicense", "did:sorcha:w:hse",
                """{"class":"CategoryB"}"""));

        var result = await _service.SubmitPresentationAsync(
            request.Id, "cred-1", [], "vp-token");

        result.Status.Should().Be(PresentationStatus.Denied);

        var verification = JsonSerializer.Deserialize<VerificationResult>(result.VerificationResult!);
        verification!.Errors.Should().Contain(e => e.FailureReason == "ClaimValueMismatch");
    }

    // --- DenyRequestAsync ---

    [Fact]
    public async Task DenyRequestAsync_PendingRequest_SetsToDenied()
    {
        var request = await _service.CreateRequestAsync(CreateTestDto());
        var result = await _service.DenyRequestAsync(request.Id);

        result.Should().NotBeNull();
        result!.Status.Should().Be(PresentationStatus.Denied);
    }

    [Fact]
    public async Task DenyRequestAsync_NonExistent_ReturnsNull()
    {
        var result = await _service.DenyRequestAsync("nonexistent");
        result.Should().BeNull();
    }

    // --- Replay prevention ---

    [Fact]
    public async Task SubmitPresentationAsync_DoubleSubmit_ThrowsInvalidOperation()
    {
        var request = await _service.CreateRequestAsync(CreateTestDto());

        _storeMock
            .Setup(s => s.GetByIdAsync("cred-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestCredential("cred-1", "ChemicalHandlingLicense", "did:sorcha:w:hse"));

        _storeMock
            .Setup(s => s.RecordPresentationAsync("cred-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // First submit succeeds
        await _service.SubmitPresentationAsync(request.Id, "cred-1", [], "vp-token");

        // Second submit should fail (request is no longer Pending)
        var act = () => _service.SubmitPresentationAsync(
            request.Id, "cred-1", [], "vp-token-2");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Verified*expected Pending*");
    }

    // --- Helpers ---

    private static CreatePresentationRequestDto CreateTestDto() => new()
    {
        CredentialType = "ChemicalHandlingLicense",
        CallbackUrl = "https://verifier.example/callback",
        VerifierIdentity = "Test Verifier"
    };

    private static CredentialEntity CreateTestCredential(
        string id, string type, string issuerDid, string? claimsJson = null) => new()
    {
        Id = id,
        Type = type,
        IssuerDid = issuerDid,
        SubjectDid = "did:sorcha:w:holder-1",
        WalletAddress = "wallet-1",
        ClaimsJson = claimsJson ?? """{"class":"CategoryB","permitNumber":"HSE-2026-001"}""",
        RawToken = "eyJhbGciOiJFZERTQSJ9.test",
        Status = "Active",
        IssuedAt = DateTimeOffset.UtcNow.AddDays(-30),
        CreatedAt = DateTimeOffset.UtcNow.AddDays(-30)
    };
}
