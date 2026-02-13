// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.UI.Core.Models.Blueprints;
using Xunit;

namespace Sorcha.UI.Core.Tests.Components.Blueprints;

/// <summary>
/// Tests for PublishWizardState immutable state record.
/// </summary>
public class PublishWizardStateTests
{
    #region Default Values

    [Fact]
    public void DefaultState_HasCorrectDefaults()
    {
        var state = new PublishWizardState();

        state.IsValidating.Should().BeFalse();
        state.IsValid.Should().BeFalse();
        state.ValidationResults.Should().BeEmpty();
        state.Warnings.Should().BeEmpty();
        state.SelectedRegisterId.Should().BeNull();
        state.SelectedRegisterName.Should().BeNull();
        state.IsCheckingRights.Should().BeFalse();
        state.HasPublishRights.Should().BeFalse();
        state.UserRole.Should().BeNull();
        state.Roster.Should().BeNull();
        state.RightsError.Should().BeNull();
        state.IsPublishing.Should().BeFalse();
        state.IsPublished.Should().BeFalse();
        state.ErrorMessage.Should().BeNull();
    }

    #endregion

    #region CanProceed — Step 0 (Validation)

    [Fact]
    public void CanProceed_Step0_ReturnsFalse_WhenNotValid()
    {
        var state = new PublishWizardState { IsValid = false, IsValidating = false };
        state.CanProceed(0).Should().BeFalse();
    }

    [Fact]
    public void CanProceed_Step0_ReturnsFalse_WhenStillValidating()
    {
        var state = new PublishWizardState { IsValid = true, IsValidating = true };
        state.CanProceed(0).Should().BeFalse();
    }

    [Fact]
    public void CanProceed_Step0_ReturnsTrue_WhenValidAndNotValidating()
    {
        var state = new PublishWizardState { IsValid = true, IsValidating = false };
        state.CanProceed(0).Should().BeTrue();
    }

    #endregion

    #region CanProceed — Step 1 (Register Selection)

    [Fact]
    public void CanProceed_Step1_ReturnsFalse_WhenNoRegisterSelected()
    {
        var state = new PublishWizardState { SelectedRegisterId = null };
        state.CanProceed(1).Should().BeFalse();
    }

    [Fact]
    public void CanProceed_Step1_ReturnsFalse_WhenEmptyRegisterId()
    {
        var state = new PublishWizardState { SelectedRegisterId = "" };
        state.CanProceed(1).Should().BeFalse();
    }

    [Fact]
    public void CanProceed_Step1_ReturnsTrue_WhenRegisterSelected()
    {
        var state = new PublishWizardState { SelectedRegisterId = "reg-123" };
        state.CanProceed(1).Should().BeTrue();
    }

    #endregion

    #region CanProceed — Step 2 (Rights Check)

    [Fact]
    public void CanProceed_Step2_ReturnsFalse_WhenNoPublishRights()
    {
        var state = new PublishWizardState { HasPublishRights = false, IsCheckingRights = false };
        state.CanProceed(2).Should().BeFalse();
    }

    [Fact]
    public void CanProceed_Step2_ReturnsFalse_WhenStillCheckingRights()
    {
        var state = new PublishWizardState { HasPublishRights = true, IsCheckingRights = true };
        state.CanProceed(2).Should().BeFalse();
    }

    [Fact]
    public void CanProceed_Step2_ReturnsTrue_WhenHasRightsAndNotChecking()
    {
        var state = new PublishWizardState { HasPublishRights = true, IsCheckingRights = false };
        state.CanProceed(2).Should().BeTrue();
    }

    #endregion

    #region CanProceed — Step 3 (Publish)

    [Fact]
    public void CanProceed_Step3_ReturnsTrue_Always()
    {
        var state = new PublishWizardState();
        state.CanProceed(3).Should().BeTrue();
    }

    #endregion

    #region CanProceed — Invalid Step

    [Fact]
    public void CanProceed_InvalidStep_ReturnsFalse()
    {
        var state = new PublishWizardState();
        state.CanProceed(4).Should().BeFalse();
        state.CanProceed(-1).Should().BeFalse();
    }

    #endregion

    #region State Transitions (with syntax)

    [Fact]
    public void WithSyntax_ValidationCompleted_UpdatesState()
    {
        var initial = new PublishWizardState { IsValidating = true };

        var updated = initial with
        {
            IsValidating = false,
            IsValid = true,
            Warnings = ["Cyclic route detected"]
        };

        updated.IsValidating.Should().BeFalse();
        updated.IsValid.Should().BeTrue();
        updated.Warnings.Should().ContainSingle();
    }

    [Fact]
    public void WithSyntax_RegisterSelected_UpdatesState()
    {
        var state = new PublishWizardState();

        var updated = state with
        {
            SelectedRegisterId = "reg-456",
            SelectedRegisterName = "My Register"
        };

        updated.SelectedRegisterId.Should().Be("reg-456");
        updated.SelectedRegisterName.Should().Be("My Register");
    }

    [Fact]
    public void WithSyntax_RightsGranted_UpdatesState()
    {
        var state = new PublishWizardState { IsCheckingRights = true };

        var updated = state with
        {
            IsCheckingRights = false,
            HasPublishRights = true,
            UserRole = "Owner"
        };

        updated.HasPublishRights.Should().BeTrue();
        updated.UserRole.Should().Be("Owner");
    }

    [Fact]
    public void WithSyntax_PublishingSuccess_UpdatesState()
    {
        var state = new PublishWizardState { IsPublishing = true };

        var updated = state with
        {
            IsPublishing = false,
            IsPublished = true
        };

        updated.IsPublishing.Should().BeFalse();
        updated.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void WithSyntax_PublishingError_UpdatesState()
    {
        var state = new PublishWizardState { IsPublishing = true };

        var updated = state with
        {
            IsPublishing = false,
            ErrorMessage = "Service unavailable"
        };

        updated.IsPublishing.Should().BeFalse();
        updated.ErrorMessage.Should().Be("Service unavailable");
    }

    #endregion

    #region GovernanceRosterViewModel

    [Fact]
    public void GovernanceRosterViewModel_DefaultValues_AreCorrect()
    {
        var roster = new GovernanceRosterViewModel();

        roster.RegisterId.Should().BeEmpty();
        roster.Members.Should().BeEmpty();
        roster.MemberCount.Should().Be(0);
        roster.ControlTransactionCount.Should().Be(0);
        roster.LastControlTxId.Should().BeNull();
    }

    [Fact]
    public void RosterMemberViewModel_DefaultValues_AreCorrect()
    {
        var member = new RosterMemberViewModel();

        member.Subject.Should().BeEmpty();
        member.Role.Should().BeEmpty();
        member.Algorithm.Should().BeEmpty();
        member.GrantedAt.Should().Be(default);
    }

    #endregion

    #region BlueprintValidationResponse

    [Fact]
    public void BlueprintValidationResponse_DefaultValues_AreCorrect()
    {
        var response = new BlueprintValidationResponse();

        response.BlueprintId.Should().BeEmpty();
        response.Title.Should().BeEmpty();
        response.IsValid.Should().BeFalse();
        response.ValidationResults.Should().BeEmpty();
        response.Warnings.Should().BeEmpty();
    }

    #endregion
}
