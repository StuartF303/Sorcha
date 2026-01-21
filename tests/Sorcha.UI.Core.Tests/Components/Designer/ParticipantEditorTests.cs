// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.UI.Core.Models.Designer;
using Sorcha.Blueprint.Models;
using Xunit;

namespace Sorcha.UI.Core.Tests.Components.Designer;

/// <summary>
/// Tests for ParticipantModel and participant editing logic.
/// </summary>
public class ParticipantEditorTests
{
    [Fact]
    public void ParticipantModel_DefaultValues_AreCorrect()
    {
        // Act
        var model = new ParticipantModel();

        // Assert
        model.Id.Should().NotBeNullOrEmpty("should have a generated ID");
        model.DisplayName.Should().BeEmpty();
        model.WalletAddress.Should().BeNull();
        model.Organisation.Should().BeEmpty();
        model.Role.Should().Be(ParticipantRole.Member, "default role should be Member");
        model.Description.Should().BeNull();
        model.IsNew.Should().BeTrue("new models should be marked as new");
    }

    [Fact]
    public void ParticipantModel_IsValid_ReturnsFalse_WhenDisplayNameIsEmpty()
    {
        // Arrange
        var model = new ParticipantModel { DisplayName = "" };

        // Assert
        model.IsValid.Should().BeFalse("empty display name should be invalid");
    }

    [Fact]
    public void ParticipantModel_IsValid_ReturnsFalse_WhenDisplayNameIsWhitespace()
    {
        // Arrange
        var model = new ParticipantModel { DisplayName = "   " };

        // Assert
        model.IsValid.Should().BeFalse("whitespace-only display name should be invalid");
    }

    [Fact]
    public void ParticipantModel_IsValid_ReturnsTrue_WhenDisplayNameIsSet()
    {
        // Arrange
        var model = new ParticipantModel { DisplayName = "Alice" };

        // Assert
        model.IsValid.Should().BeTrue("valid display name should make model valid");
    }

    [Fact]
    public void ToParticipant_CreatesCorrectParticipant()
    {
        // Arrange
        var model = new ParticipantModel
        {
            Id = "test-id",
            DisplayName = "Alice",
            WalletAddress = "0x1234567890abcdef",
            Organisation = "Test Org",
            Role = ParticipantRole.Approver
        };

        // Act
        var participant = model.ToParticipant();

        // Assert
        participant.Id.Should().Be("test-id");
        participant.Name.Should().Be("Alice");
        participant.WalletAddress.Should().Be("0x1234567890abcdef");
        participant.Organisation.Should().Be("Test Org");
    }

    [Fact]
    public void ToParticipant_UsesRoleAsOrganisation_WhenOrganisationIsEmpty()
    {
        // Arrange
        var model = new ParticipantModel
        {
            Id = "test-id",
            DisplayName = "Bob",
            Organisation = "",
            Role = ParticipantRole.Initiator
        };

        // Act
        var participant = model.ToParticipant();

        // Assert
        participant.Organisation.Should().Be("Initiator", "empty organisation should use role name");
    }

    [Fact]
    public void ToParticipant_HandlesNullWalletAddress()
    {
        // Arrange
        var model = new ParticipantModel
        {
            DisplayName = "Charlie",
            WalletAddress = null
        };

        // Act
        var participant = model.ToParticipant();

        // Assert
        participant.WalletAddress.Should().BeEmpty("null wallet address should become empty string");
    }

    [Fact]
    public void FromParticipant_CreatesCorrectModel()
    {
        // Arrange
        var participant = new Participant
        {
            Id = "participant-id",
            Name = "Diana",
            WalletAddress = "0xabcdef1234567890",
            Organisation = "Approver"
        };

        // Act
        var model = ParticipantModel.FromParticipant(participant);

        // Assert
        model.Id.Should().Be("participant-id");
        model.DisplayName.Should().Be("Diana");
        model.WalletAddress.Should().Be("0xabcdef1234567890");
        model.Organisation.Should().Be("Approver");
        model.Role.Should().Be(ParticipantRole.Approver, "should parse role from organisation");
        model.IsNew.Should().BeFalse("loaded participants should not be marked as new");
    }

    [Fact]
    public void FromParticipant_HandlesNullName()
    {
        // Arrange
        var participant = new Participant { Id = "id", Name = null! };

        // Act
        var model = ParticipantModel.FromParticipant(participant);

        // Assert
        model.DisplayName.Should().BeEmpty("null name should become empty string");
    }

    [Fact]
    public void FromParticipant_DefaultsToMember_WhenOrganisationIsNotRole()
    {
        // Arrange
        var participant = new Participant
        {
            Id = "id",
            Name = "Eve",
            Organisation = "ACME Corp"
        };

        // Act
        var model = ParticipantModel.FromParticipant(participant);

        // Assert
        model.Role.Should().Be(ParticipantRole.Member, "non-role organisation should default to Member");
        model.Organisation.Should().Be("ACME Corp");
    }

    [Fact]
    public void FromParticipant_ParsesRoleCaseInsensitive()
    {
        // Arrange
        var participant = new Participant
        {
            Id = "id",
            Name = "Frank",
            Organisation = "ADMINISTRATOR"
        };

        // Act
        var model = ParticipantModel.FromParticipant(participant);

        // Assert
        model.Role.Should().Be(ParticipantRole.Administrator, "should parse role case-insensitively");
    }

    [Theory]
    [InlineData("Initiator", ParticipantRole.Initiator)]
    [InlineData("Approver", ParticipantRole.Approver)]
    [InlineData("Observer", ParticipantRole.Observer)]
    [InlineData("Member", ParticipantRole.Member)]
    [InlineData("Administrator", ParticipantRole.Administrator)]
    public void FromParticipant_ParsesAllRoles(string organisation, ParticipantRole expectedRole)
    {
        // Arrange
        var participant = new Participant
        {
            Id = "id",
            Name = "Test",
            Organisation = organisation
        };

        // Act
        var model = ParticipantModel.FromParticipant(participant);

        // Assert
        model.Role.Should().Be(expectedRole);
    }

    [Fact]
    public void RoundTrip_PreservesData()
    {
        // Arrange
        var original = new ParticipantModel
        {
            Id = "round-trip-id",
            DisplayName = "Grace",
            WalletAddress = "0x9876543210fedcba",
            Organisation = "Test Company",
            Role = ParticipantRole.Observer,
            Description = "Test description"
        };

        // Act
        var participant = original.ToParticipant();
        var restored = ParticipantModel.FromParticipant(participant);

        // Assert
        restored.Id.Should().Be(original.Id);
        restored.DisplayName.Should().Be(original.DisplayName);
        restored.WalletAddress.Should().Be(original.WalletAddress);
        restored.Organisation.Should().Be(original.Organisation);
        // Note: Role will be Member since Organisation is not a role name
        restored.IsNew.Should().BeFalse();
    }

    [Fact]
    public void RoundTrip_PreservesRole_WhenOrganisationMatchesRole()
    {
        // Arrange
        var original = new ParticipantModel
        {
            Id = "role-id",
            DisplayName = "Henry",
            WalletAddress = "0x1111111111111111",
            Organisation = "", // Empty org means role will be used
            Role = ParticipantRole.Initiator
        };

        // Act
        var participant = original.ToParticipant();
        var restored = ParticipantModel.FromParticipant(participant);

        // Assert
        restored.Role.Should().Be(ParticipantRole.Initiator, "role should be preserved through round-trip");
    }

    [Fact]
    public void ParticipantRole_HasAllExpectedValues()
    {
        // Assert
        var roles = Enum.GetValues<ParticipantRole>();
        roles.Should().Contain(ParticipantRole.Initiator);
        roles.Should().Contain(ParticipantRole.Approver);
        roles.Should().Contain(ParticipantRole.Observer);
        roles.Should().Contain(ParticipantRole.Member);
        roles.Should().Contain(ParticipantRole.Administrator);
        roles.Should().HaveCount(5);
    }
}
