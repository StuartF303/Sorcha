// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.ServiceClients.Wallet;
using Sorcha.Tenant.Models;
using Sorcha.Tenant.Service.Data;
using Sorcha.Tenant.Service.Data.Repositories;
using Sorcha.Tenant.Service.Models;
using Sorcha.Tenant.Service.Models.Dtos;
using Sorcha.Tenant.Service.Services;
using Xunit;

namespace Sorcha.Tenant.Service.Tests.Services;

public class WalletVerificationServiceTests
{
    private readonly Mock<IParticipantRepository> _repositoryMock;
    private readonly Mock<IWalletServiceClient> _walletClientMock;
    private readonly Mock<ILogger<WalletVerificationService>> _loggerMock;
    private readonly TenantDbContext _dbContext;
    private readonly IWalletVerificationService _service;

    private readonly Guid _testParticipantId = Guid.NewGuid();
    private readonly Guid _testOrgId = Guid.NewGuid();
    private const string TestWalletAddress = "sorcha1test123456789";
    private const string TestPublicKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
    private const string TestAlgorithm = "ED25519";

    public WalletVerificationServiceTests()
    {
        _repositoryMock = new Mock<IParticipantRepository>();
        _walletClientMock = new Mock<IWalletServiceClient>();
        _loggerMock = new Mock<ILogger<WalletVerificationService>>();

        // Create in-memory DbContext for testing
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseInMemoryDatabase($"WalletVerificationTests_{Guid.NewGuid()}")
            .Options;
        _dbContext = new TenantDbContext(options);

        _service = new WalletVerificationService(
            _repositoryMock.Object,
            _walletClientMock.Object,
            _dbContext,
            _loggerMock.Object);
    }

    #region InitiateLinkAsync Tests

    [Fact]
    public async Task InitiateLinkAsync_ValidRequest_CreatesChallengeWithCorrectData()
    {
        // Arrange
        var participant = CreateTestParticipant();
        var request = new InitiateWalletLinkRequest
        {
            WalletAddress = TestWalletAddress,
            Algorithm = TestAlgorithm
        };

        SetupParticipantExists(participant);
        SetupNoActiveLinks();
        SetupAddressNotLinked();
        SetupNoPendingChallenge();
        SetupChallengeCreation();

        // Act
        var result = await _service.InitiateLinkAsync(_testParticipantId, request, "user@test.com");

        // Assert
        result.Should().NotBeNull();
        result.WalletAddress.Should().Be(TestWalletAddress);
        result.Algorithm.Should().Be(TestAlgorithm);
        result.Status.Should().Be(ChallengeStatus.Pending);
        result.Challenge.Should().Contain(_testParticipantId.ToString());
        result.Challenge.Should().Contain(TestWalletAddress);
        result.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
        result.ExpiresAt.Should().BeBefore(DateTimeOffset.UtcNow.AddMinutes(6));

        _repositoryMock.Verify(
            x => x.CreateChallengeAsync(It.Is<WalletLinkChallenge>(c =>
                c.ParticipantId == _testParticipantId &&
                c.WalletAddress == TestWalletAddress &&
                c.Status == ChallengeStatus.Pending),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task InitiateLinkAsync_ParticipantNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var request = new InitiateWalletLinkRequest
        {
            WalletAddress = TestWalletAddress,
            Algorithm = TestAlgorithm
        };

        _repositoryMock
            .Setup(x => x.GetByIdAsync(_testParticipantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantIdentity?)null);

        // Act
        var act = async () => await _service.InitiateLinkAsync(_testParticipantId, request, "user@test.com");

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{_testParticipantId}*");
    }

    [Fact]
    public async Task InitiateLinkAsync_MaxLinksReached_ThrowsInvalidOperationException()
    {
        // Arrange
        var participant = CreateTestParticipant();
        var request = new InitiateWalletLinkRequest
        {
            WalletAddress = TestWalletAddress,
            Algorithm = TestAlgorithm
        };

        SetupParticipantExists(participant);

        _repositoryMock
            .Setup(x => x.GetActiveWalletLinkCountAsync(_testParticipantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10); // Max limit

        // Act
        var act = async () => await _service.InitiateLinkAsync(_testParticipantId, request, "user@test.com");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*maximum*10*");
    }

    [Fact]
    public async Task InitiateLinkAsync_AddressAlreadyLinked_ThrowsInvalidOperationException()
    {
        // Arrange
        var participant = CreateTestParticipant();
        var request = new InitiateWalletLinkRequest
        {
            WalletAddress = TestWalletAddress,
            Algorithm = TestAlgorithm
        };

        SetupParticipantExists(participant);
        SetupNoActiveLinks();

        _repositoryMock
            .Setup(x => x.GetActiveWalletLinkByAddressAsync(TestWalletAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LinkedWalletAddress { WalletAddress = TestWalletAddress });

        // Act
        var act = async () => await _service.InitiateLinkAsync(_testParticipantId, request, "user@test.com");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{TestWalletAddress}*already linked*");
    }

    [Fact]
    public async Task InitiateLinkAsync_ExistingPendingChallenge_ReturnsExistingChallenge()
    {
        // Arrange
        var participant = CreateTestParticipant();
        var existingChallenge = new WalletLinkChallenge
        {
            Id = Guid.NewGuid(),
            ParticipantId = _testParticipantId,
            WalletAddress = TestWalletAddress,
            Challenge = "existing challenge",
            Status = ChallengeStatus.Pending,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(3)
        };

        var request = new InitiateWalletLinkRequest
        {
            WalletAddress = TestWalletAddress,
            Algorithm = TestAlgorithm
        };

        SetupParticipantExists(participant);
        SetupNoActiveLinks();
        SetupAddressNotLinked();

        _repositoryMock
            .Setup(x => x.GetPendingChallengeAsync(_testParticipantId, TestWalletAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingChallenge);

        // Act
        var result = await _service.InitiateLinkAsync(_testParticipantId, request, "user@test.com");

        // Assert
        result.Should().NotBeNull();
        result.ChallengeId.Should().Be(existingChallenge.Id);
        result.Challenge.Should().Be("existing challenge");

        _repositoryMock.Verify(
            x => x.CreateChallengeAsync(It.IsAny<WalletLinkChallenge>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region VerifyLinkAsync Tests

    [Fact]
    public async Task VerifyLinkAsync_ValidSignature_CreatesWalletLink()
    {
        // Arrange
        var challengeId = Guid.NewGuid();
        var participant = CreateTestParticipant();
        var challenge = new WalletLinkChallenge
        {
            Id = challengeId,
            ParticipantId = _testParticipantId,
            WalletAddress = TestWalletAddress,
            Challenge = "sign this challenge",
            Status = ChallengeStatus.Pending,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(3)
        };

        var request = new VerifyWalletLinkRequest
        {
            Signature = "valid_signature_base64"
        };

        _repositoryMock
            .Setup(x => x.GetChallengeByIdAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        SetupAddressNotLinked();
        SetupParticipantExists(participant);

        _walletClientMock
            .Setup(x => x.GetWalletAsync(TestWalletAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestWalletInfo());

        _walletClientMock
            .Setup(x => x.VerifySignatureAsync(
                TestPublicKey,
                challenge.Challenge,
                request.Signature,
                TestAlgorithm,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(x => x.UpdateChallengeAsync(It.IsAny<WalletLinkChallenge>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WalletLinkChallenge c, CancellationToken _) => c);

        _repositoryMock
            .Setup(x => x.CreateWalletLinkAsync(It.IsAny<LinkedWalletAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LinkedWalletAddress l, CancellationToken _) => l);

        _repositoryMock
            .Setup(x => x.CreateAuditEntryAsync(It.IsAny<ParticipantAuditEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantAuditEntry e, CancellationToken _) => e);

        // Act
        var result = await _service.VerifyLinkAsync(_testParticipantId, challengeId, request, "user@test.com", "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.WalletAddress.Should().Be(TestWalletAddress);
        result.Algorithm.Should().Be(TestAlgorithm);
        result.Status.Should().Be(WalletLinkStatus.Active);

        _repositoryMock.Verify(
            x => x.CreateWalletLinkAsync(It.Is<LinkedWalletAddress>(l =>
                l.ParticipantId == _testParticipantId &&
                l.WalletAddress == TestWalletAddress &&
                l.Status == WalletLinkStatus.Active),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _repositoryMock.Verify(
            x => x.CreateAuditEntryAsync(It.Is<ParticipantAuditEntry>(e =>
                e.Action == ParticipantAuditAction.WalletLinked),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task VerifyLinkAsync_ChallengeNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var challengeId = Guid.NewGuid();
        var request = new VerifyWalletLinkRequest { Signature = "sig" };

        _repositoryMock
            .Setup(x => x.GetChallengeByIdAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WalletLinkChallenge?)null);

        // Act
        var act = async () => await _service.VerifyLinkAsync(_testParticipantId, challengeId, request, "user@test.com");

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{challengeId}*");
    }

    [Fact]
    public async Task VerifyLinkAsync_ChallengeBelongsToDifferentParticipant_ThrowsInvalidOperationException()
    {
        // Arrange
        var challengeId = Guid.NewGuid();
        var differentParticipantId = Guid.NewGuid();
        var challenge = new WalletLinkChallenge
        {
            Id = challengeId,
            ParticipantId = differentParticipantId,
            WalletAddress = TestWalletAddress,
            Challenge = "challenge",
            Status = ChallengeStatus.Pending,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(3)
        };

        var request = new VerifyWalletLinkRequest { Signature = "sig" };

        _repositoryMock
            .Setup(x => x.GetChallengeByIdAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        // Act
        var act = async () => await _service.VerifyLinkAsync(_testParticipantId, challengeId, request, "user@test.com");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*does not belong*{_testParticipantId}*");
    }

    [Fact]
    public async Task VerifyLinkAsync_ChallengeAlreadyCompleted_ThrowsInvalidOperationException()
    {
        // Arrange
        var challengeId = Guid.NewGuid();
        var challenge = new WalletLinkChallenge
        {
            Id = challengeId,
            ParticipantId = _testParticipantId,
            WalletAddress = TestWalletAddress,
            Challenge = "challenge",
            Status = ChallengeStatus.Completed,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(3)
        };

        var request = new VerifyWalletLinkRequest { Signature = "sig" };

        _repositoryMock
            .Setup(x => x.GetChallengeByIdAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        // Act
        var act = async () => await _service.VerifyLinkAsync(_testParticipantId, challengeId, request, "user@test.com");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*not pending*{ChallengeStatus.Completed}*");
    }

    [Fact]
    public async Task VerifyLinkAsync_ChallengeExpired_UpdatesStatusAndThrows()
    {
        // Arrange
        var challengeId = Guid.NewGuid();
        var challenge = new WalletLinkChallenge
        {
            Id = challengeId,
            ParticipantId = _testParticipantId,
            WalletAddress = TestWalletAddress,
            Challenge = "challenge",
            Status = ChallengeStatus.Pending,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1) // Expired
        };

        var request = new VerifyWalletLinkRequest { Signature = "sig" };

        _repositoryMock
            .Setup(x => x.GetChallengeByIdAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        _repositoryMock
            .Setup(x => x.UpdateChallengeAsync(It.IsAny<WalletLinkChallenge>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WalletLinkChallenge c, CancellationToken _) => c);

        // Act
        var act = async () => await _service.VerifyLinkAsync(_testParticipantId, challengeId, request, "user@test.com");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{challengeId}*expired*");

        _repositoryMock.Verify(
            x => x.UpdateChallengeAsync(It.Is<WalletLinkChallenge>(c =>
                c.Status == ChallengeStatus.Expired),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task VerifyLinkAsync_InvalidSignature_UpdatesStatusAndThrows()
    {
        // Arrange
        var challengeId = Guid.NewGuid();
        var challenge = new WalletLinkChallenge
        {
            Id = challengeId,
            ParticipantId = _testParticipantId,
            WalletAddress = TestWalletAddress,
            Challenge = "sign this challenge",
            Status = ChallengeStatus.Pending,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(3)
        };

        var request = new VerifyWalletLinkRequest { Signature = "invalid_signature" };

        _repositoryMock
            .Setup(x => x.GetChallengeByIdAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        SetupAddressNotLinked();

        _walletClientMock
            .Setup(x => x.GetWalletAsync(TestWalletAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestWalletInfo());

        _walletClientMock
            .Setup(x => x.VerifySignatureAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _repositoryMock
            .Setup(x => x.UpdateChallengeAsync(It.IsAny<WalletLinkChallenge>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WalletLinkChallenge c, CancellationToken _) => c);

        // Act
        var act = async () => await _service.VerifyLinkAsync(_testParticipantId, challengeId, request, "user@test.com");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Signature verification failed*");

        _repositoryMock.Verify(
            x => x.UpdateChallengeAsync(It.Is<WalletLinkChallenge>(c =>
                c.Status == ChallengeStatus.Failed),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task VerifyLinkAsync_WalletNotFoundInService_UpdatesStatusAndThrows()
    {
        // Arrange
        var challengeId = Guid.NewGuid();
        var challenge = new WalletLinkChallenge
        {
            Id = challengeId,
            ParticipantId = _testParticipantId,
            WalletAddress = TestWalletAddress,
            Challenge = "sign this challenge",
            Status = ChallengeStatus.Pending,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(3)
        };

        var request = new VerifyWalletLinkRequest { Signature = "sig" };

        _repositoryMock
            .Setup(x => x.GetChallengeByIdAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        SetupAddressNotLinked();

        _walletClientMock
            .Setup(x => x.GetWalletAsync(TestWalletAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WalletInfo?)null);

        _repositoryMock
            .Setup(x => x.UpdateChallengeAsync(It.IsAny<WalletLinkChallenge>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WalletLinkChallenge c, CancellationToken _) => c);

        // Act
        var act = async () => await _service.VerifyLinkAsync(_testParticipantId, challengeId, request, "user@test.com");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{TestWalletAddress}*not found*");

        _repositoryMock.Verify(
            x => x.UpdateChallengeAsync(It.Is<WalletLinkChallenge>(c =>
                c.Status == ChallengeStatus.Failed),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region ListLinksAsync Tests

    [Fact]
    public async Task ListLinksAsync_ReturnsActiveLinks()
    {
        // Arrange
        var links = new List<LinkedWalletAddress>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ParticipantId = _testParticipantId,
                WalletAddress = "sorcha1wallet1",
                Algorithm = "ED25519",
                Status = WalletLinkStatus.Active,
                LinkedAt = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                ParticipantId = _testParticipantId,
                WalletAddress = "sorcha1wallet2",
                Algorithm = "NIST-P256",
                Status = WalletLinkStatus.Active,
                LinkedAt = DateTimeOffset.UtcNow.AddDays(-2)
            }
        };

        _repositoryMock
            .Setup(x => x.GetWalletLinksAsync(_testParticipantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(links);

        // Act
        var result = await _service.ListLinksAsync(_testParticipantId, includeRevoked: false);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(r => r.Status.Should().Be(WalletLinkStatus.Active));
    }

    [Fact]
    public async Task ListLinksAsync_WithIncludeRevoked_ReturnsAllLinks()
    {
        // Arrange
        var links = new List<LinkedWalletAddress>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ParticipantId = _testParticipantId,
                WalletAddress = "sorcha1wallet1",
                Algorithm = "ED25519",
                Status = WalletLinkStatus.Active,
                LinkedAt = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                ParticipantId = _testParticipantId,
                WalletAddress = "sorcha1wallet2",
                Algorithm = "ED25519",
                Status = WalletLinkStatus.Revoked,
                LinkedAt = DateTimeOffset.UtcNow.AddDays(-5),
                RevokedAt = DateTimeOffset.UtcNow.AddDays(-3)
            }
        };

        _repositoryMock
            .Setup(x => x.GetWalletLinksAsync(_testParticipantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(links);

        // Act
        var result = await _service.ListLinksAsync(_testParticipantId, includeRevoked: true);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.Status == WalletLinkStatus.Active);
        result.Should().Contain(r => r.Status == WalletLinkStatus.Revoked);
    }

    #endregion

    #region RevokeLinkAsync Tests

    [Fact]
    public async Task RevokeLinkAsync_ValidLink_RevokesAndCreatesAudit()
    {
        // Arrange
        var linkId = Guid.NewGuid();
        var link = new LinkedWalletAddress
        {
            Id = linkId,
            ParticipantId = _testParticipantId,
            WalletAddress = TestWalletAddress,
            Algorithm = TestAlgorithm,
            Status = WalletLinkStatus.Active,
            LinkedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        _repositoryMock
            .Setup(x => x.GetWalletLinkByIdAsync(linkId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(link);

        _repositoryMock
            .Setup(x => x.UpdateWalletLinkAsync(It.IsAny<LinkedWalletAddress>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LinkedWalletAddress l, CancellationToken _) => l);

        _repositoryMock
            .Setup(x => x.CreateAuditEntryAsync(It.IsAny<ParticipantAuditEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantAuditEntry e, CancellationToken _) => e);

        // Act
        var result = await _service.RevokeLinkAsync(_testParticipantId, linkId, "admin@test.com", "127.0.0.1");

        // Assert
        result.Should().BeTrue();

        _repositoryMock.Verify(
            x => x.UpdateWalletLinkAsync(It.Is<LinkedWalletAddress>(l =>
                l.Status == WalletLinkStatus.Revoked &&
                l.RevokedAt != null),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _repositoryMock.Verify(
            x => x.CreateAuditEntryAsync(It.Is<ParticipantAuditEntry>(e =>
                e.Action == ParticipantAuditAction.WalletRevoked &&
                e.ActorId == "admin@test.com" &&
                e.IpAddress == "127.0.0.1"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RevokeLinkAsync_LinkNotFound_ReturnsFalse()
    {
        // Arrange
        var linkId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetWalletLinkByIdAsync(linkId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LinkedWalletAddress?)null);

        // Act
        var result = await _service.RevokeLinkAsync(_testParticipantId, linkId, "admin@test.com");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeLinkAsync_LinkBelongsToDifferentParticipant_ReturnsFalse()
    {
        // Arrange
        var linkId = Guid.NewGuid();
        var differentParticipantId = Guid.NewGuid();
        var link = new LinkedWalletAddress
        {
            Id = linkId,
            ParticipantId = differentParticipantId,
            WalletAddress = TestWalletAddress,
            Status = WalletLinkStatus.Active
        };

        _repositoryMock
            .Setup(x => x.GetWalletLinkByIdAsync(linkId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(link);

        // Act
        var result = await _service.RevokeLinkAsync(_testParticipantId, linkId, "admin@test.com");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeLinkAsync_AlreadyRevoked_ReturnsFalse()
    {
        // Arrange
        var linkId = Guid.NewGuid();
        var link = new LinkedWalletAddress
        {
            Id = linkId,
            ParticipantId = _testParticipantId,
            WalletAddress = TestWalletAddress,
            Status = WalletLinkStatus.Revoked,
            RevokedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        _repositoryMock
            .Setup(x => x.GetWalletLinkByIdAsync(linkId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(link);

        // Act
        var result = await _service.RevokeLinkAsync(_testParticipantId, linkId, "admin@test.com");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetChallengeAsync Tests

    [Fact]
    public async Task GetChallengeAsync_ValidChallenge_ReturnsChallenge()
    {
        // Arrange
        var challengeId = Guid.NewGuid();
        var challenge = new WalletLinkChallenge
        {
            Id = challengeId,
            ParticipantId = _testParticipantId,
            WalletAddress = TestWalletAddress,
            Challenge = "challenge message",
            Status = ChallengeStatus.Pending,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(3)
        };

        _repositoryMock
            .Setup(x => x.GetChallengeByIdAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        // Act
        var result = await _service.GetChallengeAsync(challengeId);

        // Assert
        result.Should().NotBeNull();
        result!.ChallengeId.Should().Be(challengeId);
        result.WalletAddress.Should().Be(TestWalletAddress);
    }

    [Fact]
    public async Task GetChallengeAsync_ChallengeNotFound_ReturnsNull()
    {
        // Arrange
        var challengeId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.GetChallengeByIdAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WalletLinkChallenge?)null);

        // Act
        var result = await _service.GetChallengeAsync(challengeId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetChallengeAsync_ExpiredChallenge_ReturnsNull()
    {
        // Arrange
        var challengeId = Guid.NewGuid();
        var challenge = new WalletLinkChallenge
        {
            Id = challengeId,
            ParticipantId = _testParticipantId,
            WalletAddress = TestWalletAddress,
            Challenge = "challenge message",
            Status = ChallengeStatus.Pending,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1) // Expired
        };

        _repositoryMock
            .Setup(x => x.GetChallengeByIdAsync(challengeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(challenge);

        // Act
        var result = await _service.GetChallengeAsync(challengeId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region IsAddressLinkedAsync Tests

    [Fact]
    public async Task IsAddressLinkedAsync_AddressLinked_ReturnsTrue()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetActiveWalletLinkByAddressAsync(TestWalletAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LinkedWalletAddress { WalletAddress = TestWalletAddress });

        // Act
        var result = await _service.IsAddressLinkedAsync(TestWalletAddress);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAddressLinkedAsync_AddressNotLinked_ReturnsFalse()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetActiveWalletLinkByAddressAsync(TestWalletAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LinkedWalletAddress?)null);

        // Act
        var result = await _service.IsAddressLinkedAsync(TestWalletAddress);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetActiveLinksCountAsync Tests

    [Fact]
    public async Task GetActiveLinksCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.GetActiveWalletLinkCountAsync(_testParticipantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var result = await _service.GetActiveLinksCountAsync(_testParticipantId);

        // Assert
        result.Should().Be(5);
    }

    #endregion

    #region ExpirePendingChallengesAsync Tests

    [Fact]
    public async Task ExpirePendingChallengesAsync_CallsRepositoryAndReturnsCount()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.ExpirePendingChallengesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        // Act
        var result = await _service.ExpirePendingChallengesAsync();

        // Assert
        result.Should().Be(10);
    }

    #endregion

    #region Helper Methods

    private ParticipantIdentity CreateTestParticipant()
    {
        return new ParticipantIdentity
        {
            Id = _testParticipantId,
            UserId = Guid.NewGuid(),
            OrganizationId = _testOrgId,
            DisplayName = "Test User",
            Email = "test@test.com",
            Status = ParticipantIdentityStatus.Active
        };
    }

    private WalletInfo CreateTestWalletInfo()
    {
        return new WalletInfo
        {
            Address = TestWalletAddress,
            Name = "Test Wallet",
            PublicKey = TestPublicKey,
            Algorithm = TestAlgorithm,
            Status = "Active",
            Owner = "test@test.com",
            Tenant = "default"
        };
    }

    private void SetupParticipantExists(ParticipantIdentity participant)
    {
        _repositoryMock
            .Setup(x => x.GetByIdAsync(_testParticipantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);
    }

    private void SetupNoActiveLinks()
    {
        _repositoryMock
            .Setup(x => x.GetActiveWalletLinkCountAsync(_testParticipantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
    }

    private void SetupAddressNotLinked()
    {
        _repositoryMock
            .Setup(x => x.GetActiveWalletLinkByAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LinkedWalletAddress?)null);
    }

    private void SetupNoPendingChallenge()
    {
        _repositoryMock
            .Setup(x => x.GetPendingChallengeAsync(_testParticipantId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WalletLinkChallenge?)null);
    }

    private void SetupChallengeCreation()
    {
        _repositoryMock
            .Setup(x => x.CreateChallengeAsync(It.IsAny<WalletLinkChallenge>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((WalletLinkChallenge c, CancellationToken _) => c);
    }

    #endregion
}
