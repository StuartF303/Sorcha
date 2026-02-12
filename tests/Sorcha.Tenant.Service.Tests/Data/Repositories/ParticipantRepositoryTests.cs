// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sorcha.Tenant.Models;
using Sorcha.Tenant.Service.Data;
using Sorcha.Tenant.Service.Data.Repositories;
using Sorcha.Tenant.Service.Models;
using Sorcha.Tenant.Service.Tests.Helpers;
using Xunit;

namespace Sorcha.Tenant.Service.Tests.Data.Repositories;

public class ParticipantRepositoryTests : IDisposable
{
    private readonly TenantDbContext _context;
    private readonly IParticipantRepository _repository;
    private readonly Guid _testOrgId = Guid.NewGuid();
    private readonly Guid _testUserId = Guid.NewGuid();

    public ParticipantRepositoryTests()
    {
        _context = InMemoryDbContextFactory.Create();
        _repository = new ParticipantRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region ParticipantIdentity Tests

    [Fact]
    public async Task CreateAsync_ShouldAddParticipantToDatabase()
    {
        // Arrange
        var participant = CreateTestParticipant();

        // Act
        var result = await _repository.CreateAsync(participant);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.DisplayName.Should().Be("Alice Johnson");
        result.Email.Should().Be("alice@example.com");

        // Verify in database
        var saved = await _repository.GetByIdAsync(result.Id);
        saved.Should().NotBeNull();
        saved!.DisplayName.Should().Be("Alice Johnson");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnParticipant_WhenExists()
    {
        // Arrange
        var participant = CreateTestParticipant();
        var created = await _repository.CreateAsync(participant);

        // Act
        var result = await _repository.GetByIdAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(created.Id);
        result.DisplayName.Should().Be("Alice Johnson");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUserAndOrgAsync_ShouldReturnParticipant_WhenExists()
    {
        // Arrange
        var participant = CreateTestParticipant();
        await _repository.CreateAsync(participant);

        // Act
        var result = await _repository.GetByUserAndOrgAsync(_testUserId, _testOrgId);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(_testUserId);
        result.OrganizationId.Should().Be(_testOrgId);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnAllParticipants_ForUser()
    {
        // Arrange
        var org1 = Guid.NewGuid();
        var org2 = Guid.NewGuid();

        await _repository.CreateAsync(new ParticipantIdentity
        {
            UserId = _testUserId,
            OrganizationId = org1,
            DisplayName = "User in Org 1",
            Email = "user@org1.com",
            Status = ParticipantIdentityStatus.Active
        });

        await _repository.CreateAsync(new ParticipantIdentity
        {
            UserId = _testUserId,
            OrganizationId = org2,
            DisplayName = "User in Org 2",
            Email = "user@org2.com",
            Status = ParticipantIdentityStatus.Active
        });

        // Act
        var result = await _repository.GetByUserIdAsync(_testUserId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByOrganizationAsync_ShouldReturnPaginatedResults()
    {
        // Arrange
        for (var i = 0; i < 25; i++)
        {
            await _repository.CreateAsync(new ParticipantIdentity
            {
                UserId = Guid.NewGuid(),
                OrganizationId = _testOrgId,
                DisplayName = $"Participant {i:D2}",
                Email = $"participant{i}@example.com",
                Status = ParticipantIdentityStatus.Active
            });
        }

        // Act
        var (participants, totalCount) = await _repository.GetByOrganizationAsync(_testOrgId, page: 2, pageSize: 10);

        // Assert
        totalCount.Should().Be(25);
        participants.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetByOrganizationAsync_ShouldFilterByStatus()
    {
        // Arrange
        await _repository.CreateAsync(new ParticipantIdentity
        {
            UserId = Guid.NewGuid(),
            OrganizationId = _testOrgId,
            DisplayName = "Active Participant",
            Email = "active@example.com",
            Status = ParticipantIdentityStatus.Active
        });

        await _repository.CreateAsync(new ParticipantIdentity
        {
            UserId = Guid.NewGuid(),
            OrganizationId = _testOrgId,
            DisplayName = "Suspended Participant",
            Email = "suspended@example.com",
            Status = ParticipantIdentityStatus.Suspended
        });

        // Act
        var (active, activeCount) = await _repository.GetByOrganizationAsync(
            _testOrgId, status: ParticipantIdentityStatus.Active);
        var (suspended, suspendedCount) = await _repository.GetByOrganizationAsync(
            _testOrgId, status: ParticipantIdentityStatus.Suspended);

        // Assert
        activeCount.Should().Be(1);
        suspendedCount.Should().Be(1);
        active.First().DisplayName.Should().Be("Active Participant");
        suspended.First().DisplayName.Should().Be("Suspended Participant");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateParticipantProperties()
    {
        // Arrange
        var participant = CreateTestParticipant();
        var created = await _repository.CreateAsync(participant);

        // Act
        created.DisplayName = "Updated Name";
        created.Status = ParticipantIdentityStatus.Suspended;
        var updated = await _repository.UpdateAsync(created);

        // Assert
        updated.DisplayName.Should().Be("Updated Name");
        updated.Status.Should().Be(ParticipantIdentityStatus.Suspended);
        updated.UpdatedAt.Should().BeAfter(created.CreatedAt);

        // Verify in database
        var fetched = await _repository.GetByIdAsync(created.Id);
        fetched!.DisplayName.Should().Be("Updated Name");
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenParticipantExists()
    {
        // Arrange
        var participant = CreateTestParticipant();
        await _repository.CreateAsync(participant);

        // Act
        var exists = await _repository.ExistsAsync(_testUserId, _testOrgId);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenParticipantNotExists()
    {
        // Act
        var exists = await _repository.ExistsAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task SearchAsync_ShouldFilterByQuery()
    {
        // Arrange
        await _repository.CreateAsync(new ParticipantIdentity
        {
            UserId = Guid.NewGuid(),
            OrganizationId = _testOrgId,
            DisplayName = "Alice Johnson",
            Email = "alice@example.com",
            Status = ParticipantIdentityStatus.Active
        });

        await _repository.CreateAsync(new ParticipantIdentity
        {
            UserId = Guid.NewGuid(),
            OrganizationId = _testOrgId,
            DisplayName = "Bob Smith",
            Email = "bob@example.com",
            Status = ParticipantIdentityStatus.Active
        });

        var criteria = new ParticipantSearchCriteria
        {
            Query = "alice",
            IsSystemAdmin = true // Bypass org filtering
        };

        // Act
        var (results, count) = await _repository.SearchAsync(criteria);

        // Assert
        count.Should().Be(1);
        results.First().DisplayName.Should().Be("Alice Johnson");
    }

    [Fact]
    public async Task SearchAsync_ShouldRespectOrgVisibility()
    {
        // Arrange
        var accessibleOrg = Guid.NewGuid();
        var inaccessibleOrg = Guid.NewGuid();

        await _repository.CreateAsync(new ParticipantIdentity
        {
            UserId = Guid.NewGuid(),
            OrganizationId = accessibleOrg,
            DisplayName = "Accessible Participant",
            Email = "accessible@example.com",
            Status = ParticipantIdentityStatus.Active
        });

        await _repository.CreateAsync(new ParticipantIdentity
        {
            UserId = Guid.NewGuid(),
            OrganizationId = inaccessibleOrg,
            DisplayName = "Inaccessible Participant",
            Email = "inaccessible@example.com",
            Status = ParticipantIdentityStatus.Active
        });

        var criteria = new ParticipantSearchCriteria
        {
            AccessibleOrganizations = new[] { accessibleOrg },
            IsSystemAdmin = false
        };

        // Act
        var (results, count) = await _repository.SearchAsync(criteria);

        // Assert
        count.Should().Be(1);
        results.First().DisplayName.Should().Be("Accessible Participant");
    }

    #endregion

    #region LinkedWalletAddress Tests

    [Fact]
    public async Task CreateWalletLinkAsync_ShouldAddWalletLinkToDatabase()
    {
        // Arrange
        var participant = await _repository.CreateAsync(CreateTestParticipant());
        var walletLink = CreateTestWalletLink(participant.Id);

        // Act
        var result = await _repository.CreateWalletLinkAsync(walletLink);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.WalletAddress.Should().Be("sorcha1abc123xyz");

        // Verify in database
        var saved = await _repository.GetWalletLinkByIdAsync(result.Id);
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task GetActiveWalletLinkByAddressAsync_ShouldReturnLink_WhenActive()
    {
        // Arrange
        var participant = await _repository.CreateAsync(CreateTestParticipant());
        var walletLink = CreateTestWalletLink(participant.Id);
        await _repository.CreateWalletLinkAsync(walletLink);

        // Act
        var result = await _repository.GetActiveWalletLinkByAddressAsync("sorcha1abc123xyz");

        // Assert
        result.Should().NotBeNull();
        result!.WalletAddress.Should().Be("sorcha1abc123xyz");
    }

    [Fact]
    public async Task GetActiveWalletLinkByAddressAsync_ShouldReturnNull_WhenRevoked()
    {
        // Arrange
        var participant = await _repository.CreateAsync(CreateTestParticipant());
        var walletLink = CreateTestWalletLink(participant.Id);
        walletLink.Status = WalletLinkStatus.Revoked;
        walletLink.RevokedAt = DateTimeOffset.UtcNow;
        await _repository.CreateWalletLinkAsync(walletLink);

        // Act
        var result = await _repository.GetActiveWalletLinkByAddressAsync("sorcha1abc123xyz");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetWalletLinksAsync_ShouldExcludeRevokedByDefault()
    {
        // Arrange
        var participant = await _repository.CreateAsync(CreateTestParticipant());

        await _repository.CreateWalletLinkAsync(new LinkedWalletAddress
        {
            ParticipantId = participant.Id,
            OrganizationId = _testOrgId,
            WalletAddress = "active_wallet",
            PublicKey = new byte[] { 1, 2, 3 },
            Algorithm = "ED25519",
            Status = WalletLinkStatus.Active
        });

        await _repository.CreateWalletLinkAsync(new LinkedWalletAddress
        {
            ParticipantId = participant.Id,
            OrganizationId = _testOrgId,
            WalletAddress = "revoked_wallet",
            PublicKey = new byte[] { 4, 5, 6 },
            Algorithm = "ED25519",
            Status = WalletLinkStatus.Revoked,
            RevokedAt = DateTimeOffset.UtcNow
        });

        // Act
        var result = await _repository.GetWalletLinksAsync(participant.Id);

        // Assert
        result.Should().HaveCount(1);
        result.First().WalletAddress.Should().Be("active_wallet");
    }

    [Fact]
    public async Task GetWalletLinksAsync_ShouldIncludeRevoked_WhenRequested()
    {
        // Arrange
        var participant = await _repository.CreateAsync(CreateTestParticipant());

        await _repository.CreateWalletLinkAsync(new LinkedWalletAddress
        {
            ParticipantId = participant.Id,
            OrganizationId = _testOrgId,
            WalletAddress = "active_wallet",
            PublicKey = new byte[] { 1, 2, 3 },
            Algorithm = "ED25519",
            Status = WalletLinkStatus.Active
        });

        await _repository.CreateWalletLinkAsync(new LinkedWalletAddress
        {
            ParticipantId = participant.Id,
            OrganizationId = _testOrgId,
            WalletAddress = "revoked_wallet",
            PublicKey = new byte[] { 4, 5, 6 },
            Algorithm = "ED25519",
            Status = WalletLinkStatus.Revoked,
            RevokedAt = DateTimeOffset.UtcNow
        });

        // Act
        var result = await _repository.GetWalletLinksAsync(participant.Id, includeRevoked: true);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetActiveWalletLinkCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var participant = await _repository.CreateAsync(CreateTestParticipant());

        for (var i = 0; i < 5; i++)
        {
            await _repository.CreateWalletLinkAsync(new LinkedWalletAddress
            {
                ParticipantId = participant.Id,
                OrganizationId = _testOrgId,
                WalletAddress = $"wallet_{i}",
                PublicKey = new byte[] { (byte)i },
                Algorithm = "ED25519",
                Status = WalletLinkStatus.Active
            });
        }

        // Act
        var count = await _repository.GetActiveWalletLinkCountAsync(participant.Id);

        // Assert
        count.Should().Be(5);
    }

    [Fact]
    public async Task GetParticipantByWalletAddressAsync_ShouldReturnParticipant()
    {
        // Arrange
        var participant = await _repository.CreateAsync(CreateTestParticipant());
        await _repository.CreateWalletLinkAsync(CreateTestWalletLink(participant.Id));

        // Act
        var result = await _repository.GetParticipantByWalletAddressAsync("sorcha1abc123xyz");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(participant.Id);
    }

    #endregion

    #region WalletLinkChallenge Tests

    [Fact]
    public async Task CreateChallengeAsync_ShouldAddChallengeToDatabase()
    {
        // Arrange
        var participant = await _repository.CreateAsync(CreateTestParticipant());
        var challenge = CreateTestChallenge(participant.Id);

        // Act
        var result = await _repository.CreateChallengeAsync(challenge);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.Challenge.Should().Contain("sorcha1abc123xyz");
    }

    [Fact]
    public async Task GetPendingChallengeAsync_ShouldReturnChallenge_WhenPendingAndNotExpired()
    {
        // Arrange
        var participant = await _repository.CreateAsync(CreateTestParticipant());
        var challenge = CreateTestChallenge(participant.Id);
        await _repository.CreateChallengeAsync(challenge);

        // Act
        var result = await _repository.GetPendingChallengeAsync(participant.Id, "sorcha1abc123xyz");

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(ChallengeStatus.Pending);
    }

    [Fact]
    public async Task GetPendingChallengeAsync_ShouldReturnNull_WhenExpired()
    {
        // Arrange
        var participant = await _repository.CreateAsync(CreateTestParticipant());
        var challenge = new WalletLinkChallenge
        {
            ParticipantId = participant.Id,
            WalletAddress = "sorcha1abc123xyz",
            Challenge = "Sign this: expired challenge",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1), // Already expired
            Status = ChallengeStatus.Pending
        };
        await _repository.CreateChallengeAsync(challenge);

        // Act
        var result = await _repository.GetPendingChallengeAsync(participant.Id, "sorcha1abc123xyz");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExpirePendingChallengesAsync_ShouldExpireOldChallenges()
    {
        // Arrange
        var participant = await _repository.CreateAsync(CreateTestParticipant());

        await _repository.CreateChallengeAsync(new WalletLinkChallenge
        {
            ParticipantId = participant.Id,
            WalletAddress = "wallet1",
            Challenge = "expired challenge",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            Status = ChallengeStatus.Pending
        });

        await _repository.CreateChallengeAsync(new WalletLinkChallenge
        {
            ParticipantId = participant.Id,
            WalletAddress = "wallet2",
            Challenge = "valid challenge",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            Status = ChallengeStatus.Pending
        });

        // Act
        var expiredCount = await _repository.ExpirePendingChallengesAsync();

        // Assert
        expiredCount.Should().Be(1);

        var expired = await _repository.GetChallengeByIdAsync(
            (await _context.WalletLinkChallenges.FirstAsync(c => c.WalletAddress == "wallet1")).Id);
        expired!.Status.Should().Be(ChallengeStatus.Expired);
    }

    #endregion

    #region ParticipantAuditEntry Tests

    [Fact]
    public async Task CreateAuditEntryAsync_ShouldAddEntryToDatabase()
    {
        // Arrange
        var participant = await _repository.CreateAsync(CreateTestParticipant());
        var entry = new ParticipantAuditEntry
        {
            ParticipantId = participant.Id,
            Action = ParticipantAuditAction.Created,
            ActorId = "admin@example.com",
            ActorType = "Admin",
            IpAddress = "192.168.1.1"
        };

        // Act
        var result = await _repository.CreateAuditEntryAsync(entry);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.Action.Should().Be(ParticipantAuditAction.Created);
    }

    [Fact]
    public async Task GetAuditEntriesAsync_ShouldReturnPaginatedEntries()
    {
        // Arrange
        var participant = await _repository.CreateAsync(CreateTestParticipant());

        for (var i = 0; i < 15; i++)
        {
            await _repository.CreateAuditEntryAsync(new ParticipantAuditEntry
            {
                ParticipantId = participant.Id,
                Action = "Action" + i,
                ActorId = "actor",
                ActorType = "User"
            });
        }

        // Act
        var (entries, totalCount) = await _repository.GetAuditEntriesAsync(
            participant.Id, page: 1, pageSize: 10);

        // Assert
        totalCount.Should().Be(15);
        entries.Should().HaveCount(10);
    }

    #endregion

    #region Helper Methods

    private ParticipantIdentity CreateTestParticipant()
    {
        return new ParticipantIdentity
        {
            UserId = _testUserId,
            OrganizationId = _testOrgId,
            DisplayName = "Alice Johnson",
            Email = "alice@example.com",
            Status = ParticipantIdentityStatus.Active
        };
    }

    private LinkedWalletAddress CreateTestWalletLink(Guid participantId)
    {
        return new LinkedWalletAddress
        {
            ParticipantId = participantId,
            OrganizationId = _testOrgId,
            WalletAddress = "sorcha1abc123xyz",
            PublicKey = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
            Algorithm = "ED25519",
            Status = WalletLinkStatus.Active
        };
    }

    private WalletLinkChallenge CreateTestChallenge(Guid participantId)
    {
        return new WalletLinkChallenge
        {
            ParticipantId = participantId,
            WalletAddress = "sorcha1abc123xyz",
            Challenge = $"Sign this message to link wallet sorcha1abc123xyz to participant {participantId}. Nonce: {Guid.NewGuid()}",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            Status = ChallengeStatus.Pending
        };
    }

    #endregion
}
