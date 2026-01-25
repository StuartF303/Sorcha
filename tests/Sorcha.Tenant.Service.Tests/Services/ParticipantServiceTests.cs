// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.Tenant.Models;
using Sorcha.Tenant.Service.Data.Repositories;
using Sorcha.Tenant.Service.Models;
using Sorcha.Tenant.Service.Models.Dtos;
using Sorcha.Tenant.Service.Services;
using Xunit;

namespace Sorcha.Tenant.Service.Tests.Services;

public class ParticipantServiceTests
{
    private readonly Mock<IParticipantRepository> _participantRepositoryMock;
    private readonly Mock<IIdentityRepository> _identityRepositoryMock;
    private readonly Mock<ILogger<ParticipantService>> _loggerMock;
    private readonly IParticipantService _service;

    private readonly Guid _testOrgId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();

    public ParticipantServiceTests()
    {
        _participantRepositoryMock = new Mock<IParticipantRepository>();
        _identityRepositoryMock = new Mock<IIdentityRepository>();
        _loggerMock = new Mock<ILogger<ParticipantService>>();

        _service = new ParticipantService(
            _participantRepositoryMock.Object,
            _identityRepositoryMock.Object,
            _loggerMock.Object);
    }

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_ValidRequest_CreatesParticipant()
    {
        // Arrange
        var user = CreateTestUser();
        var request = new CreateParticipantRequest { UserId = _testUserId };

        _identityRepositoryMock
            .Setup(x => x.GetUserByIdAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _participantRepositoryMock
            .Setup(x => x.ExistsAsync(_testUserId, _testOrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _participantRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<ParticipantIdentity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantIdentity p, CancellationToken _) => p);

        _participantRepositoryMock
            .Setup(x => x.CreateAuditEntryAsync(It.IsAny<ParticipantAuditEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantAuditEntry e, CancellationToken _) => e);

        _participantRepositoryMock
            .Setup(x => x.GetWalletLinksAsync(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LinkedWalletAddress>());

        // Act
        var result = await _service.RegisterAsync(_testOrgId, request, "admin@test.com");

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(_testUserId);
        result.OrganizationId.Should().Be(_testOrgId);
        result.DisplayName.Should().Be(user.DisplayName);
        result.Email.Should().Be(user.Email);
        result.Status.Should().Be(ParticipantIdentityStatus.Active);

        _participantRepositoryMock.Verify(
            x => x.CreateAsync(It.Is<ParticipantIdentity>(p =>
                p.UserId == _testUserId &&
                p.OrganizationId == _testOrgId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _participantRepositoryMock.Verify(
            x => x.CreateAuditEntryAsync(It.Is<ParticipantAuditEntry>(e =>
                e.Action == ParticipantAuditAction.Created),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithCustomDisplayName_UsesProvidedName()
    {
        // Arrange
        var user = CreateTestUser();
        var request = new CreateParticipantRequest
        {
            UserId = _testUserId,
            DisplayName = "Custom Display Name"
        };

        _identityRepositoryMock
            .Setup(x => x.GetUserByIdAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _participantRepositoryMock
            .Setup(x => x.ExistsAsync(_testUserId, _testOrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _participantRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<ParticipantIdentity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantIdentity p, CancellationToken _) => p);

        _participantRepositoryMock
            .Setup(x => x.CreateAuditEntryAsync(It.IsAny<ParticipantAuditEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantAuditEntry e, CancellationToken _) => e);

        _participantRepositoryMock
            .Setup(x => x.GetWalletLinksAsync(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LinkedWalletAddress>());

        // Act
        var result = await _service.RegisterAsync(_testOrgId, request, "admin@test.com");

        // Assert
        result.DisplayName.Should().Be("Custom Display Name");
    }

    [Fact]
    public async Task RegisterAsync_UserNotFound_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateParticipantRequest { UserId = _testUserId };

        _identityRepositoryMock
            .Setup(x => x.GetUserByIdAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentity?)null);

        // Act & Assert
        var act = () => _service.RegisterAsync(_testOrgId, request, "admin@test.com");
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"*{_testUserId}*not found*");
    }

    [Fact]
    public async Task RegisterAsync_UserNotInOrganization_ThrowsArgumentException()
    {
        // Arrange
        var user = CreateTestUser();
        user.OrganizationId = Guid.NewGuid(); // Different org
        var request = new CreateParticipantRequest { UserId = _testUserId };

        _identityRepositoryMock
            .Setup(x => x.GetUserByIdAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act & Assert
        var act = () => _service.RegisterAsync(_testOrgId, request, "admin@test.com");
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"*does not belong to organization*");
    }

    [Fact]
    public async Task RegisterAsync_AlreadyRegistered_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = CreateTestUser();
        var request = new CreateParticipantRequest { UserId = _testUserId };

        _identityRepositoryMock
            .Setup(x => x.GetUserByIdAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _participantRepositoryMock
            .Setup(x => x.ExistsAsync(_testUserId, _testOrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        var act = () => _service.RegisterAsync(_testOrgId, request, "admin@test.com");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*already registered*");
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingParticipant_ReturnsDetailResponse()
    {
        // Arrange
        var participantId = Guid.NewGuid();
        var participant = CreateTestParticipant(participantId);

        _participantRepositoryMock
            .Setup(x => x.GetByIdWithWalletsAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);

        _participantRepositoryMock
            .Setup(x => x.GetWalletLinksAsync(participantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LinkedWalletAddress>());

        // Act
        var result = await _service.GetByIdAsync(participantId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(participantId);
        result.DisplayName.Should().Be(participant.DisplayName);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentParticipant_ReturnsNull()
    {
        // Arrange
        var participantId = Guid.NewGuid();

        _participantRepositoryMock
            .Setup(x => x.GetByIdWithWalletsAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantIdentity?)null);

        // Act
        var result = await _service.GetByIdAsync(participantId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ListAsync Tests

    [Fact]
    public async Task ListAsync_ReturnsPagedResults()
    {
        // Arrange
        var participants = new List<ParticipantIdentity>
        {
            CreateTestParticipant(Guid.NewGuid()),
            CreateTestParticipant(Guid.NewGuid())
        };

        _participantRepositoryMock
            .Setup(x => x.GetByOrganizationAsync(_testOrgId, 1, 20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((participants, 2));

        _participantRepositoryMock
            .Setup(x => x.GetActiveWalletLinkCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _service.ListAsync(_testOrgId);

        // Assert
        result.Should().NotBeNull();
        result.Participants.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task ListAsync_WithStatusFilter_FiltersResults()
    {
        // Arrange
        var participants = new List<ParticipantIdentity>
        {
            CreateTestParticipant(Guid.NewGuid())
        };

        _participantRepositoryMock
            .Setup(x => x.GetByOrganizationAsync(_testOrgId, 1, 20, ParticipantIdentityStatus.Active, It.IsAny<CancellationToken>()))
            .ReturnsAsync((participants, 1));

        _participantRepositoryMock
            .Setup(x => x.GetActiveWalletLinkCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _service.ListAsync(_testOrgId, status: ParticipantIdentityStatus.Active);

        // Assert
        result.Participants.Should().HaveCount(1);
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithQuery_ReturnsMatchingResults()
    {
        // Arrange
        var request = new ParticipantSearchRequest
        {
            Query = "alice",
            Page = 1,
            PageSize = 20
        };

        var participants = new List<ParticipantIdentity>
        {
            CreateTestParticipant(Guid.NewGuid())
        };

        _participantRepositoryMock
            .Setup(x => x.SearchAsync(It.IsAny<ParticipantSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((participants, 1));

        _participantRepositoryMock
            .Setup(x => x.GetActiveWalletLinkCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _service.SearchAsync(request, null, true);

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().HaveCount(1);
        result.Query.Should().Be("alice");
    }

    [Fact]
    public async Task SearchAsync_WithOrgFilter_PassesCriteriaToRepository()
    {
        // Arrange
        var specificOrgId = Guid.NewGuid();
        var request = new ParticipantSearchRequest
        {
            Query = "test",
            OrganizationId = specificOrgId,
            Page = 1,
            PageSize = 20
        };

        _participantRepositoryMock
            .Setup(x => x.SearchAsync(It.IsAny<ParticipantSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ParticipantIdentity>(), 0));

        // Act
        await _service.SearchAsync(request, null, true);

        // Assert
        _participantRepositoryMock.Verify(
            x => x.SearchAsync(It.Is<ParticipantSearchCriteria>(c =>
                c.OrganizationId == specificOrgId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WithStatusFilter_PassesCriteriaToRepository()
    {
        // Arrange
        var request = new ParticipantSearchRequest
        {
            Status = ParticipantIdentityStatus.Active,
            Page = 1,
            PageSize = 20
        };

        _participantRepositoryMock
            .Setup(x => x.SearchAsync(It.IsAny<ParticipantSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ParticipantIdentity>(), 0));

        // Act
        await _service.SearchAsync(request, null, true);

        // Assert
        _participantRepositoryMock.Verify(
            x => x.SearchAsync(It.Is<ParticipantSearchCriteria>(c =>
                c.Status == ParticipantIdentityStatus.Active),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WithHasLinkedWalletFilter_PassesCriteriaToRepository()
    {
        // Arrange
        var request = new ParticipantSearchRequest
        {
            HasLinkedWallet = true,
            Page = 1,
            PageSize = 20
        };

        _participantRepositoryMock
            .Setup(x => x.SearchAsync(It.IsAny<ParticipantSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ParticipantIdentity>(), 0));

        // Act
        await _service.SearchAsync(request, null, true);

        // Assert
        _participantRepositoryMock.Verify(
            x => x.SearchAsync(It.Is<ParticipantSearchCriteria>(c =>
                c.HasLinkedWallet == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchAsync_WithAccessibleOrganizations_AppliesOrgScopedVisibility()
    {
        // Arrange
        var org1 = Guid.NewGuid();
        var org2 = Guid.NewGuid();
        var accessibleOrgs = new List<Guid> { org1, org2 };
        var request = new ParticipantSearchRequest
        {
            Query = "test",
            Page = 1,
            PageSize = 20
        };

        _participantRepositoryMock
            .Setup(x => x.SearchAsync(It.IsAny<ParticipantSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ParticipantIdentity>(), 0));

        // Act
        await _service.SearchAsync(request, accessibleOrgs, isSystemAdmin: false);

        // Assert
        _participantRepositoryMock.Verify(
            x => x.SearchAsync(It.Is<ParticipantSearchCriteria>(c =>
                c.AccessibleOrganizations != null &&
                c.AccessibleOrganizations.Count == 2 &&
                c.IsSystemAdmin == false),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchAsync_AsSystemAdmin_BypassesOrgVisibility()
    {
        // Arrange
        var request = new ParticipantSearchRequest
        {
            Query = "test",
            Page = 1,
            PageSize = 20
        };

        _participantRepositoryMock
            .Setup(x => x.SearchAsync(It.IsAny<ParticipantSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ParticipantIdentity>(), 0));

        // Act - System admin with no accessible orgs specified
        await _service.SearchAsync(request, null, isSystemAdmin: true);

        // Assert
        _participantRepositoryMock.Verify(
            x => x.SearchAsync(It.Is<ParticipantSearchCriteria>(c =>
                c.IsSystemAdmin == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchAsync_ReturnsCorrectPaginationInfo()
    {
        // Arrange
        var request = new ParticipantSearchRequest
        {
            Query = "test",
            Page = 2,
            PageSize = 10
        };

        var participants = new List<ParticipantIdentity>
        {
            CreateTestParticipant(Guid.NewGuid()),
            CreateTestParticipant(Guid.NewGuid())
        };

        _participantRepositoryMock
            .Setup(x => x.SearchAsync(It.IsAny<ParticipantSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((participants, 25)); // Total of 25 results

        _participantRepositoryMock
            .Setup(x => x.GetActiveWalletLinkCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _service.SearchAsync(request, null, true);

        // Assert
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(25);
        result.Results.Should().HaveCount(2);
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsAllWithinScope()
    {
        // Arrange
        var request = new ParticipantSearchRequest
        {
            Page = 1,
            PageSize = 20
        };

        var participants = new List<ParticipantIdentity>
        {
            CreateTestParticipant(Guid.NewGuid()),
            CreateTestParticipant(Guid.NewGuid()),
            CreateTestParticipant(Guid.NewGuid())
        };

        _participantRepositoryMock
            .Setup(x => x.SearchAsync(It.IsAny<ParticipantSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((participants, 3));

        _participantRepositoryMock
            .Setup(x => x.GetActiveWalletLinkCountAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _service.SearchAsync(request, null, true);

        // Assert
        result.Results.Should().HaveCount(3);
        result.Query.Should().BeNull();
    }

    [Fact]
    public async Task SearchAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act
        var act = async () => await _service.SearchAsync(null!, null, true);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SearchAsync_WithLinkedWallets_ReturnsHasLinkedWalletTrue()
    {
        // Arrange
        var participantId = Guid.NewGuid();
        var request = new ParticipantSearchRequest
        {
            Query = "alice",
            Page = 1,
            PageSize = 20
        };

        var participants = new List<ParticipantIdentity>
        {
            CreateTestParticipant(participantId)
        };

        _participantRepositoryMock
            .Setup(x => x.SearchAsync(It.IsAny<ParticipantSearchCriteria>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((participants, 1));

        _participantRepositoryMock
            .Setup(x => x.GetActiveWalletLinkCountAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2); // Has 2 linked wallets

        // Act
        var result = await _service.SearchAsync(request, null, true);

        // Assert
        result.Results.Should().HaveCount(1);
        result.Results[0].HasLinkedWallet.Should().BeTrue();
    }

    #endregion

    #region GetByWalletAddressAsync Tests

    [Fact]
    public async Task GetByWalletAddressAsync_ExistingAddress_ReturnsParticipant()
    {
        // Arrange
        var walletAddress = "sorcha1abc123";
        var participantId = Guid.NewGuid();
        var participant = CreateTestParticipant(participantId);

        _participantRepositoryMock
            .Setup(x => x.GetParticipantByWalletAddressAsync(walletAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);

        _participantRepositoryMock
            .Setup(x => x.GetActiveWalletLinkCountAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.GetByWalletAddressAsync(walletAddress);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(participantId);
        result.HasLinkedWallet.Should().BeTrue();
    }

    [Fact]
    public async Task GetByWalletAddressAsync_NonExistentAddress_ReturnsNull()
    {
        // Arrange
        var walletAddress = "sorcha1nonexistent";

        _participantRepositoryMock
            .Setup(x => x.GetParticipantByWalletAddressAsync(walletAddress, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantIdentity?)null);

        // Act
        var result = await _service.GetByWalletAddressAsync(walletAddress);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByWalletAddressAsync_EmptyAddress_ReturnsNull()
    {
        // Act
        var result = await _service.GetByWalletAddressAsync("");

        // Assert
        result.Should().BeNull();
        _participantRepositoryMock.Verify(
            x => x.GetParticipantByWalletAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByWalletAddressAsync_WhitespaceAddress_ReturnsNull()
    {
        // Act
        var result = await _service.GetByWalletAddressAsync("   ");

        // Assert
        result.Should().BeNull();
        _participantRepositoryMock.Verify(
            x => x.GetParticipantByWalletAddressAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByWalletAddressAsync_NullAddress_ReturnsNull()
    {
        // Act
        var result = await _service.GetByWalletAddressAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetMyProfilesAsync Tests

    [Fact]
    public async Task GetMyProfilesAsync_UserWithMultipleProfiles_ReturnsAllProfiles()
    {
        // Arrange
        var org1 = Guid.NewGuid();
        var org2 = Guid.NewGuid();
        var participants = new List<ParticipantIdentity>
        {
            CreateTestParticipantForOrg(Guid.NewGuid(), _testUserId, org1),
            CreateTestParticipantForOrg(Guid.NewGuid(), _testUserId, org2)
        };

        _participantRepositoryMock
            .Setup(x => x.GetByUserIdAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participants);

        _participantRepositoryMock
            .Setup(x => x.GetWalletLinksAsync(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LinkedWalletAddress>());

        // Act
        var result = await _service.GetMyProfilesAsync(_testUserId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(p => p.OrganizationId == org1);
        result.Should().Contain(p => p.OrganizationId == org2);
    }

    [Fact]
    public async Task GetMyProfilesAsync_UserWithNoProfiles_ReturnsEmptyList()
    {
        // Arrange
        _participantRepositoryMock
            .Setup(x => x.GetByUserIdAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ParticipantIdentity>());

        // Act
        var result = await _service.GetMyProfilesAsync(_testUserId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyProfilesAsync_IncludesLinkedWallets()
    {
        // Arrange
        var participantId = Guid.NewGuid();
        var participants = new List<ParticipantIdentity>
        {
            CreateTestParticipant(participantId)
        };

        var wallets = new List<LinkedWalletAddress>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ParticipantId = participantId,
                WalletAddress = "sorcha1wallet1",
                Algorithm = "ED25519",
                Status = WalletLinkStatus.Active
            },
            new()
            {
                Id = Guid.NewGuid(),
                ParticipantId = participantId,
                WalletAddress = "sorcha1wallet2",
                Algorithm = "ED25519",
                Status = WalletLinkStatus.Active
            }
        };

        _participantRepositoryMock
            .Setup(x => x.GetByUserIdAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participants);

        _participantRepositoryMock
            .Setup(x => x.GetWalletLinksAsync(participantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallets);

        // Act
        var result = await _service.GetMyProfilesAsync(_testUserId);

        // Assert
        result.Should().HaveCount(1);
        result[0].LinkedWallets.Should().HaveCount(2);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidRequest_UpdatesParticipant()
    {
        // Arrange
        var participantId = Guid.NewGuid();
        var participant = CreateTestParticipant(participantId);
        var request = new UpdateParticipantRequest { DisplayName = "New Name" };

        _participantRepositoryMock
            .Setup(x => x.GetByIdAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);

        _participantRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<ParticipantIdentity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantIdentity p, CancellationToken _) => p);

        _participantRepositoryMock
            .Setup(x => x.CreateAuditEntryAsync(It.IsAny<ParticipantAuditEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantAuditEntry e, CancellationToken _) => e);

        _participantRepositoryMock
            .Setup(x => x.GetWalletLinksAsync(participantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LinkedWalletAddress>());

        // Act
        var result = await _service.UpdateAsync(participantId, request, "admin@test.com");

        // Assert
        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("New Name");

        _participantRepositoryMock.Verify(
            x => x.CreateAuditEntryAsync(It.Is<ParticipantAuditEntry>(e =>
                e.Action == ParticipantAuditAction.Updated),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentParticipant_ReturnsNull()
    {
        // Arrange
        var participantId = Guid.NewGuid();
        var request = new UpdateParticipantRequest { DisplayName = "New Name" };

        _participantRepositoryMock
            .Setup(x => x.GetByIdAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantIdentity?)null);

        // Act
        var result = await _service.UpdateAsync(participantId, request, "admin@test.com");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region DeactivateAsync Tests

    [Fact]
    public async Task DeactivateAsync_ExistingParticipant_SetsInactiveStatus()
    {
        // Arrange
        var participantId = Guid.NewGuid();
        var participant = CreateTestParticipant(participantId);

        _participantRepositoryMock
            .Setup(x => x.GetByIdAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);

        _participantRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<ParticipantIdentity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantIdentity p, CancellationToken _) => p);

        _participantRepositoryMock
            .Setup(x => x.CreateAuditEntryAsync(It.IsAny<ParticipantAuditEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantAuditEntry e, CancellationToken _) => e);

        // Act
        var result = await _service.DeactivateAsync(participantId, "admin@test.com");

        // Assert
        result.Should().BeTrue();

        _participantRepositoryMock.Verify(
            x => x.UpdateAsync(It.Is<ParticipantIdentity>(p =>
                p.Status == ParticipantIdentityStatus.Inactive &&
                p.DeactivatedAt != null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeactivateAsync_NonExistentParticipant_ReturnsFalse()
    {
        // Arrange
        var participantId = Guid.NewGuid();

        _participantRepositoryMock
            .Setup(x => x.GetByIdAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantIdentity?)null);

        // Act
        var result = await _service.DeactivateAsync(participantId, "admin@test.com");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region SelfRegisterAsync Tests

    [Fact]
    public async Task SelfRegisterAsync_ValidUser_CreatesParticipant()
    {
        // Arrange
        var user = CreateTestUser();

        _identityRepositoryMock
            .Setup(x => x.GetUserByIdAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _participantRepositoryMock
            .Setup(x => x.ExistsAsync(_testUserId, _testOrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _participantRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<ParticipantIdentity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantIdentity p, CancellationToken _) => p);

        _participantRepositoryMock
            .Setup(x => x.CreateAuditEntryAsync(It.IsAny<ParticipantAuditEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantAuditEntry e, CancellationToken _) => e);

        _participantRepositoryMock
            .Setup(x => x.GetWalletLinksAsync(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LinkedWalletAddress>());

        // Act
        var result = await _service.SelfRegisterAsync(_testOrgId, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(_testUserId);

        _participantRepositoryMock.Verify(
            x => x.CreateAuditEntryAsync(It.Is<ParticipantAuditEntry>(e =>
                e.ActorType == "User"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SelfRegisterAsync_UserNotFound_ThrowsArgumentException()
    {
        // Arrange
        _identityRepositoryMock
            .Setup(x => x.GetUserByIdAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentity?)null);

        // Act
        var act = async () => await _service.SelfRegisterAsync(_testOrgId, _testUserId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"*{_testUserId}*not found*");
    }

    [Fact]
    public async Task SelfRegisterAsync_AlreadyRegistered_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = CreateTestUser();

        _identityRepositoryMock
            .Setup(x => x.GetUserByIdAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _participantRepositoryMock
            .Setup(x => x.ExistsAsync(_testUserId, _testOrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _service.SelfRegisterAsync(_testOrgId, _testUserId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public async Task SelfRegisterAsync_UserNotInOrganization_ThrowsArgumentException()
    {
        // Arrange
        var user = CreateTestUser();
        var differentOrgId = Guid.NewGuid();

        _identityRepositoryMock
            .Setup(x => x.GetUserByIdAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var act = async () => await _service.SelfRegisterAsync(differentOrgId, _testUserId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"*does not belong to organization*");
    }

    [Fact]
    public async Task SelfRegisterAsync_WithCustomDisplayName_UsesProvidedName()
    {
        // Arrange
        var user = CreateTestUser();
        var customDisplayName = "My Custom Name";

        _identityRepositoryMock
            .Setup(x => x.GetUserByIdAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _participantRepositoryMock
            .Setup(x => x.ExistsAsync(_testUserId, _testOrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _participantRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<ParticipantIdentity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantIdentity p, CancellationToken _) => p);

        _participantRepositoryMock
            .Setup(x => x.CreateAuditEntryAsync(It.IsAny<ParticipantAuditEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantAuditEntry e, CancellationToken _) => e);

        _participantRepositoryMock
            .Setup(x => x.GetWalletLinksAsync(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LinkedWalletAddress>());

        // Act
        var result = await _service.SelfRegisterAsync(_testOrgId, _testUserId, customDisplayName);

        // Assert
        result.Should().NotBeNull();
        result.DisplayName.Should().Be(customDisplayName);

        _participantRepositoryMock.Verify(
            x => x.CreateAsync(It.Is<ParticipantIdentity>(p =>
                p.DisplayName == customDisplayName),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SelfRegisterAsync_WithIpAddress_IncludesInAuditEntry()
    {
        // Arrange
        var user = CreateTestUser();
        var ipAddress = "192.168.1.100";

        _identityRepositoryMock
            .Setup(x => x.GetUserByIdAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _participantRepositoryMock
            .Setup(x => x.ExistsAsync(_testUserId, _testOrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _participantRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<ParticipantIdentity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantIdentity p, CancellationToken _) => p);

        _participantRepositoryMock
            .Setup(x => x.CreateAuditEntryAsync(It.IsAny<ParticipantAuditEntry>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParticipantAuditEntry e, CancellationToken _) => e);

        _participantRepositoryMock
            .Setup(x => x.GetWalletLinksAsync(It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LinkedWalletAddress>());

        // Act
        var result = await _service.SelfRegisterAsync(_testOrgId, _testUserId, ipAddress: ipAddress);

        // Assert
        result.Should().NotBeNull();

        _participantRepositoryMock.Verify(
            x => x.CreateAuditEntryAsync(It.Is<ParticipantAuditEntry>(e =>
                e.IpAddress == ipAddress),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region ValidateSigningCapabilityAsync Tests

    [Fact]
    public async Task ValidateSigningCapabilityAsync_WithLinkedWallet_ReturnsTrue()
    {
        // Arrange
        var participantId = Guid.NewGuid();

        _participantRepositoryMock
            .Setup(x => x.GetActiveWalletLinkCountAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _service.ValidateSigningCapabilityAsync(participantId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateSigningCapabilityAsync_WithoutLinkedWallet_ReturnsFalse()
    {
        // Arrange
        var participantId = Guid.NewGuid();

        _participantRepositoryMock
            .Setup(x => x.GetActiveWalletLinkCountAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await _service.ValidateSigningCapabilityAsync(participantId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateSigningCapabilityAsync_WithMultipleLinkedWallets_ReturnsTrue()
    {
        // Arrange
        var participantId = Guid.NewGuid();

        _participantRepositoryMock
            .Setup(x => x.GetActiveWalletLinkCountAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        var result = await _service.ValidateSigningCapabilityAsync(participantId);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Participant Assignment Validation Tests (US3)

    [Fact]
    public async Task GetByIdAsync_ActiveParticipantWithWallet_CanBeAssignedToBlueprint()
    {
        // Arrange - Simulate a participant that can be assigned to a blueprint role
        var participantId = Guid.NewGuid();
        var participant = CreateTestParticipant(participantId);
        participant.Status = ParticipantIdentityStatus.Active;

        var linkedWallets = new List<LinkedWalletAddress>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ParticipantId = participantId,
                WalletAddress = "sorcha1test123",
                Algorithm = "ED25519",
                Status = WalletLinkStatus.Active
            }
        };

        _participantRepositoryMock
            .Setup(x => x.GetByIdWithWalletsAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);

        _participantRepositoryMock
            .Setup(x => x.GetWalletLinksAsync(participantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(linkedWallets);

        // Act
        var result = await _service.GetByIdAsync(participantId);

        // Assert - Participant should be found with active status and linked wallet
        result.Should().NotBeNull();
        result!.Status.Should().Be(ParticipantIdentityStatus.Active);
        result.LinkedWallets.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByIdAsync_SuspendedParticipant_ShouldNotBeAssigned()
    {
        // Arrange - Suspended participants should not be assignable
        var participantId = Guid.NewGuid();
        var participant = CreateTestParticipant(participantId);
        participant.Status = ParticipantIdentityStatus.Suspended;

        _participantRepositoryMock
            .Setup(x => x.GetByIdWithWalletsAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);

        _participantRepositoryMock
            .Setup(x => x.GetWalletLinksAsync(participantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LinkedWalletAddress>());

        // Act
        var result = await _service.GetByIdAsync(participantId);

        // Assert - Participant found but suspended
        result.Should().NotBeNull();
        result!.Status.Should().Be(ParticipantIdentityStatus.Suspended);
    }

    [Fact]
    public async Task GetByIdAsync_InactiveParticipant_ShouldNotBeAssigned()
    {
        // Arrange - Inactive participants should not be assignable
        var participantId = Guid.NewGuid();
        var participant = CreateTestParticipant(participantId);
        participant.Status = ParticipantIdentityStatus.Inactive;
        participant.DeactivatedAt = DateTimeOffset.UtcNow.AddDays(-1);

        _participantRepositoryMock
            .Setup(x => x.GetByIdWithWalletsAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);

        _participantRepositoryMock
            .Setup(x => x.GetWalletLinksAsync(participantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LinkedWalletAddress>());

        // Act
        var result = await _service.GetByIdAsync(participantId);

        // Assert - Participant found but inactive
        result.Should().NotBeNull();
        result!.Status.Should().Be(ParticipantIdentityStatus.Inactive);
        result.DeactivatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ActiveParticipantWithoutWallet_ReturnsWarningSignal()
    {
        // Arrange - Active participant without wallet should show warning
        var participantId = Guid.NewGuid();
        var participant = CreateTestParticipant(participantId);
        participant.Status = ParticipantIdentityStatus.Active;

        _participantRepositoryMock
            .Setup(x => x.GetByIdWithWalletsAsync(participantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(participant);

        _participantRepositoryMock
            .Setup(x => x.GetWalletLinksAsync(participantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<LinkedWalletAddress>());

        // Act
        var result = await _service.GetByIdAsync(participantId);

        // Assert - Active but no linked wallets (would trigger warning in Blueprint Service)
        result.Should().NotBeNull();
        result!.Status.Should().Be(ParticipantIdentityStatus.Active);
        result.LinkedWallets.Should().BeEmpty();
    }

    [Fact]
    public async Task IsRegisteredAsync_ExistingParticipant_ReturnsTrue()
    {
        // Arrange
        _participantRepositoryMock
            .Setup(x => x.ExistsAsync(_testUserId, _testOrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.IsRegisteredAsync(_testUserId, _testOrgId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsRegisteredAsync_NonExistingParticipant_ReturnsFalse()
    {
        // Arrange
        _participantRepositoryMock
            .Setup(x => x.ExistsAsync(_testUserId, _testOrgId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.IsRegisteredAsync(_testUserId, _testOrgId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private UserIdentity CreateTestUser()
    {
        return new UserIdentity
        {
            Id = _testUserId,
            OrganizationId = _testOrgId,
            Email = "alice@example.com",
            DisplayName = "Alice Johnson",
            Status = IdentityStatus.Active
        };
    }

    private ParticipantIdentity CreateTestParticipant(Guid id)
    {
        return new ParticipantIdentity
        {
            Id = id,
            UserId = _testUserId,
            OrganizationId = _testOrgId,
            DisplayName = "Alice Johnson",
            Email = "alice@example.com",
            Status = ParticipantIdentityStatus.Active
        };
    }

    private ParticipantIdentity CreateTestParticipantForOrg(Guid id, Guid userId, Guid orgId)
    {
        return new ParticipantIdentity
        {
            Id = id,
            UserId = userId,
            OrganizationId = orgId,
            DisplayName = "Test User",
            Email = "test@example.com",
            Status = ParticipantIdentityStatus.Active
        };
    }

    #endregion
}
