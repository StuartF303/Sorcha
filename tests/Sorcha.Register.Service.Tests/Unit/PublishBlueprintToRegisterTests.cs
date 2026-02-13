// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.ServiceClients.Register;
using Xunit;

namespace Sorcha.Register.Service.Tests.Unit;

/// <summary>
/// Tests for the blueprint publish to register DTOs and governance roster models.
/// </summary>
public class PublishBlueprintToRegisterTests
{
    [Fact]
    public void GovernanceRosterResponse_DefaultValues_AreCorrect()
    {
        var roster = new GovernanceRosterResponse();

        roster.RegisterId.Should().BeEmpty();
        roster.Members.Should().BeEmpty();
        roster.MemberCount.Should().Be(0);
        roster.ControlTransactionCount.Should().Be(0);
        roster.LastControlTxId.Should().BeNull();
    }

    [Fact]
    public void GovernanceRosterResponse_CanBePopulated()
    {
        var roster = new GovernanceRosterResponse
        {
            RegisterId = "reg-001",
            Members =
            [
                new RosterMember
                {
                    Subject = "did:sorcha:user1",
                    Role = "Owner",
                    Algorithm = "ED25519",
                    GrantedAt = DateTimeOffset.UtcNow
                }
            ],
            MemberCount = 1,
            ControlTransactionCount = 3,
            LastControlTxId = "tx-abc"
        };

        roster.RegisterId.Should().Be("reg-001");
        roster.Members.Should().HaveCount(1);
        roster.Members[0].Subject.Should().Be("did:sorcha:user1");
        roster.Members[0].Role.Should().Be("Owner");
        roster.MemberCount.Should().Be(1);
        roster.ControlTransactionCount.Should().Be(3);
        roster.LastControlTxId.Should().Be("tx-abc");
    }

    [Fact]
    public void RosterMember_DefaultValues_AreCorrect()
    {
        var member = new RosterMember();

        member.Subject.Should().BeEmpty();
        member.Role.Should().BeEmpty();
        member.Algorithm.Should().BeEmpty();
        member.GrantedAt.Should().Be(default);
    }

    [Fact]
    public void RosterMember_PublishingRoles_CanBeChecked()
    {
        var publishingRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Owner", "Admin", "Designer" };

        var owner = new RosterMember { Role = "Owner" };
        var admin = new RosterMember { Role = "Admin" };
        var designer = new RosterMember { Role = "Designer" };
        var auditor = new RosterMember { Role = "Auditor" };

        publishingRoles.Contains(owner.Role).Should().BeTrue();
        publishingRoles.Contains(admin.Role).Should().BeTrue();
        publishingRoles.Contains(designer.Role).Should().BeTrue();
        publishingRoles.Contains(auditor.Role).Should().BeFalse();
    }
}
