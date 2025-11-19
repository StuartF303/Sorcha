// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Blueprint.Models;
using System.ComponentModel.DataAnnotations;

namespace Sorcha.Blueprint.Models.Tests;

/// <summary>
/// Tests for Blueprint structural validation including:
/// - Participant count validation
/// - Action count validation
/// - Participant reference integrity
/// - Wallet address validation
/// </summary>
public class BlueprintStructuralValidationTests
{
    #region 2.1 Participant Count Validation

    [Fact]
    public void Blueprint_WithZeroParticipants_ShouldFailValidation()
    {
        // Arrange
        var blueprint = new Blueprint
        {
            Title = "Valid Title",
            Description = "Valid description with sufficient length",
            Participants = new List<Participant>(), // Empty list
            Actions = new List<Models.Action>
            {
                new Models.Action { Id = 0, Title = "Action 1" }
            }
        };
        var context = new ValidationContext(blueprint);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(blueprint, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse("blueprint requires minimum 2 participants");
        results.Should().Contain(r => r.MemberNames.Contains("Participants"),
            "validation should indicate Participants collection is invalid");
    }

    [Fact]
    public void Blueprint_WithOneParticipant_ShouldFailValidation()
    {
        // Arrange
        var blueprint = new Blueprint
        {
            Title = "Valid Title",
            Description = "Valid description with sufficient length",
            Participants = new List<Participant>
            {
                new Participant { Id = "p1", Name = "Participant 1", Organisation = "Org 1" }
            },
            Actions = new List<Models.Action>
            {
                new Models.Action { Id = 0, Title = "Action 1" }
            }
        };
        var context = new ValidationContext(blueprint);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(blueprint, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse("blueprint requires minimum 2 participants");
        results.Should().Contain(r => r.MemberNames.Contains("Participants"),
            "validation should indicate Participants collection needs at least 2 items");
    }

    [Fact]
    public void Blueprint_WithTwoParticipants_ShouldPassValidation()
    {
        // Arrange
        var blueprint = new Blueprint
        {
            Title = "Valid Title",
            Description = "Valid description with sufficient length",
            Participants = new List<Participant>
            {
                new Participant { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new Participant { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<Models.Action>
            {
                new Models.Action { Id = 0, Title = "Action 1" }
            }
        };
        var context = new ValidationContext(blueprint);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(blueprint, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeTrue("blueprint with 2 participants should pass validation");
        results.Should().BeEmpty();
    }

    [Fact]
    public void Blueprint_WithTenParticipants_ShouldPassValidation()
    {
        // Arrange
        var participants = Enumerable.Range(1, 10)
            .Select(i => new Participant
            {
                Id = $"p{i}",
                Name = $"Participant {i}",
                Organisation = "Test Organization"
            })
            .ToList();

        var blueprint = new Blueprint
        {
            Title = "Valid Title",
            Description = "Valid description with sufficient length",
            Participants = participants,
            Actions = new List<Models.Action>
            {
                new Models.Action { Id = 0, Title = "Action 1" }
            }
        };
        var context = new ValidationContext(blueprint);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(blueprint, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeTrue("blueprint with 10 participants should pass validation");
        results.Should().BeEmpty();
    }

    #endregion

    #region 2.2 Action Count Validation

    [Fact]
    public void Blueprint_WithZeroActions_ShouldFailValidation()
    {
        // Arrange
        var blueprint = new Blueprint
        {
            Title = "Valid Title",
            Description = "Valid description with sufficient length",
            Participants = new List<Participant>
            {
                new Participant { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new Participant { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<Models.Action>() // Empty list
        };
        var context = new ValidationContext(blueprint);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(blueprint, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse("blueprint requires minimum 1 action");
        results.Should().Contain(r => r.MemberNames.Contains("Actions"),
            "validation should indicate Actions collection is invalid");
    }

    [Fact]
    public void Blueprint_WithOneAction_ShouldPassValidation()
    {
        // Arrange
        var blueprint = new Blueprint
        {
            Title = "Valid Title",
            Description = "Valid description with sufficient length",
            Participants = new List<Participant>
            {
                new Participant { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new Participant { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<Models.Action>
            {
                new Models.Action { Id = 0, Title = "Action 1" }
            }
        };
        var context = new ValidationContext(blueprint);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(blueprint, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeTrue("blueprint with 1 action should pass validation");
        results.Should().BeEmpty();
    }

    [Fact]
    public void Blueprint_WithMultipleActions_ShouldPassValidation()
    {
        // Arrange
        var blueprint = new Blueprint
        {
            Title = "Valid Title",
            Description = "Valid description with sufficient length",
            Participants = new List<Participant>
            {
                new Participant { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new Participant { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<Models.Action>
            {
                new Models.Action { Id = 0, Title = "Action 1" },
                new Models.Action { Id = 1, Title = "Action 2" },
                new Models.Action { Id = 2, Title = "Action 3" }
            }
        };
        var context = new ValidationContext(blueprint);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(blueprint, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeTrue("blueprint with multiple actions should pass validation");
        results.Should().BeEmpty();
    }

    #endregion

    #region 2.3 Participant Reference Integrity

    [Fact]
    public void ValidateParticipantReferences_ActionSenderReferencesExistingParticipant_ShouldPass()
    {
        // Arrange
        var blueprint = new Blueprint
        {
            Title = "Valid Title",
            Description = "Valid description with sufficient length",
            Participants = new List<Participant>
            {
                new Participant { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new Participant { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<Models.Action>
            {
                new Models.Action { Id = 0, Title = "Action 1", Sender = "p1" }
            }
        };

        // Act
        var result = ValidateParticipantReferences(blueprint);

        // Assert
        result.IsValid.Should().BeTrue("action sender references existing participant");
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateParticipantReferences_ActionSenderReferencesNonExistentParticipant_ShouldFail()
    {
        // Arrange
        var blueprint = new Blueprint
        {
            Title = "Valid Title",
            Description = "Valid description with sufficient length",
            Participants = new List<Participant>
            {
                new Participant { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new Participant { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<Models.Action>
            {
                new Models.Action { Id = 0, Title = "Action 1", Sender = "p999" } // Non-existent
            }
        };

        // Act
        var result = ValidateParticipantReferences(blueprint);

        // Assert
        result.IsValid.Should().BeFalse("action sender references non-existent participant");
        result.Errors.Should().Contain(e => e.Contains("Action 0") && e.Contains("sender 'p999'"));
    }

    [Fact]
    public void ValidateParticipantReferences_ActionTargetReferencesExistingParticipant_ShouldPass()
    {
        // Arrange
        var blueprint = new Blueprint
        {
            Title = "Valid Title",
            Description = "Valid description with sufficient length",
            Participants = new List<Participant>
            {
                new Participant { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new Participant { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<Models.Action>
            {
                new Models.Action { Id = 0, Title = "Action 1", Sender = "p1", Target = "p2" }
            }
        };

        // Act
        var result = ValidateParticipantReferences(blueprint);

        // Assert
        result.IsValid.Should().BeTrue("action target references existing participant");
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateParticipantReferences_ActionTargetReferencesNonExistentParticipant_ShouldFail()
    {
        // Arrange
        var blueprint = new Blueprint
        {
            Title = "Valid Title",
            Description = "Valid description with sufficient length",
            Participants = new List<Participant>
            {
                new Participant { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new Participant { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<Models.Action>
            {
                new Models.Action { Id = 0, Title = "Action 1", Sender = "p1", Target = "p999" }
            }
        };

        // Act
        var result = ValidateParticipantReferences(blueprint);

        // Assert
        result.IsValid.Should().BeFalse("action target references non-existent participant");
        result.Errors.Should().Contain(e => e.Contains("Action 0") && e.Contains("target 'p999'"));
    }

    [Fact]
    public void ValidateParticipantReferences_AdditionalRecipientsReferenceExistingParticipants_ShouldPass()
    {
        // Arrange
        var blueprint = new Blueprint
        {
            Title = "Valid Title",
            Description = "Valid description with sufficient length",
            Participants = new List<Participant>
            {
                new Participant { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new Participant { Id = "p2", Name = "Participant 2", Organisation = "Org 1" },
                new Participant { Id = "p3", Name = "Participant 3", Organisation = "Org 1" }
            },
            Actions = new List<Models.Action>
            {
                new Models.Action
                {
                    Id = 0,
                    Title = "Action 1",
                    Sender = "p1",
                    AdditionalRecipients = new List<string> { "p2", "p3" }
                }
            }
        };

        // Act
        var result = ValidateParticipantReferences(blueprint);

        // Assert
        result.IsValid.Should().BeTrue("additional recipients reference existing participants");
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateParticipantReferences_AdditionalRecipientsReferenceNonExistentParticipant_ShouldFail()
    {
        // Arrange
        var blueprint = new Blueprint
        {
            Title = "Valid Title",
            Description = "Valid description with sufficient length",
            Participants = new List<Participant>
            {
                new Participant { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new Participant { Id = "p2", Name = "Participant 2", Organisation = "Org 1" }
            },
            Actions = new List<Models.Action>
            {
                new Models.Action
                {
                    Id = 0,
                    Title = "Action 1",
                    Sender = "p1",
                    AdditionalRecipients = new List<string> { "p2", "p999" }
                }
            }
        };

        // Act
        var result = ValidateParticipantReferences(blueprint);

        // Assert
        result.IsValid.Should().BeFalse("additional recipient references non-existent participant");
        result.Errors.Should().Contain(e => e.Contains("Action 0") && e.Contains("additionalRecipient 'p999'"));
    }

    [Fact]
    public void ValidateParticipantReferences_AllReferencesValidAcrossMultipleActions_ShouldPass()
    {
        // Arrange
        var blueprint = new Blueprint
        {
            Title = "Valid Title",
            Description = "Valid description with sufficient length",
            Participants = new List<Participant>
            {
                new Participant { Id = "p1", Name = "Participant 1", Organisation = "Org 1" },
                new Participant { Id = "p2", Name = "Participant 2", Organisation = "Org 1" },
                new Participant { Id = "p3", Name = "Participant 3", Organisation = "Org 1" }
            },
            Actions = new List<Models.Action>
            {
                new Models.Action { Id = 0, Title = "Action 1", Sender = "p1", Target = "p2" },
                new Models.Action { Id = 1, Title = "Action 2", Sender = "p2", Target = "p3" },
                new Models.Action { Id = 2, Title = "Action 3", Sender = "p3", AdditionalRecipients = new List<string> { "p1" } }
            }
        };

        // Act
        var result = ValidateParticipantReferences(blueprint);

        // Assert
        result.IsValid.Should().BeTrue("all participant references are valid across all actions");
        result.Errors.Should().BeEmpty();
    }

    #endregion

    #region 2.4 Wallet Address Validation

    [Fact]
    public void Participant_WithEmptyWalletAddress_ShouldPassValidation()
    {
        // Arrange
        var participant = new Participant
        {
            Id = "p1",
            Name = "Participant 1",
            Organisation = "Test Organization",
            WalletAddress = string.Empty // Assigned later
        };
        var context = new ValidationContext(participant);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(participant, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeTrue("empty wallet address should be allowed (assigned later)");
        results.Should().BeEmpty();
    }

    [Fact]
    public void Participant_WithWalletAddressMaxLength_ShouldPassValidation()
    {
        // Arrange
        var maxLengthAddress = new string('a', 100); // Max length is 100
        var participant = new Participant
        {
            Id = "p1",
            Name = "Participant 1",
            Organisation = "Test Organization",
            WalletAddress = maxLengthAddress
        };
        var context = new ValidationContext(participant);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(participant, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeTrue("wallet address at max length (100 chars) should pass validation");
        results.Should().BeEmpty();
    }

    [Fact]
    public void Participant_WithWalletAddressExceedingMaxLength_ShouldFailValidation()
    {
        // Arrange
        var tooLongAddress = new string('a', 101); // Exceeds max length of 100
        var participant = new Participant
        {
            Id = "p1",
            Name = "Participant 1",
            Organisation = "Test Organization",
            WalletAddress = tooLongAddress
        };
        var context = new ValidationContext(participant);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(participant, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse("wallet address exceeding 100 chars should fail validation");
        results.Should().Contain(r => r.MemberNames.Contains("WalletAddress"));
    }

    [Fact]
    public void Participants_WithSameOrganizationButUniqueIds_ShouldBeValid()
    {
        // Arrange
        var blueprint = new Blueprint
        {
            Title = "Valid Title",
            Description = "Valid description with sufficient length",
            Participants = new List<Participant>
            {
                new Participant { Id = "p1", Name = "Participant 1", Organisation = "Acme Corp" },
                new Participant { Id = "p2", Name = "Participant 2", Organisation = "Acme Corp" },
                new Participant { Id = "p3", Name = "Participant 3", Organisation = "Acme Corp" }
            },
            Actions = new List<Models.Action>
            {
                new Models.Action { Id = 0, Title = "Action 1" }
            }
        };
        var context = new ValidationContext(blueprint);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(blueprint, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeTrue("participants can share organization if IDs are unique");
        results.Should().BeEmpty();

        // Verify IDs are unique
        var uniqueIds = blueprint.Participants.Select(p => p.Id).Distinct().Count();
        uniqueIds.Should().Be(3, "all participant IDs should be unique");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Validates that all participant references in actions point to existing participants.
    /// This is a custom validation that goes beyond data annotations.
    /// </summary>
    private static ParticipantReferenceValidationResult ValidateParticipantReferences(Blueprint blueprint)
    {
        var result = new ParticipantReferenceValidationResult { IsValid = true };
        var participantIds = blueprint.Participants.Select(p => p.Id).ToHashSet();

        foreach (var action in blueprint.Actions)
        {
            // Validate Sender
            if (!string.IsNullOrEmpty(action.Sender) && !participantIds.Contains(action.Sender))
            {
                result.IsValid = false;
                result.Errors.Add($"Action {action.Id} references non-existent sender '{action.Sender}'");
            }

            // Validate Target
            if (!string.IsNullOrEmpty(action.Target) && !participantIds.Contains(action.Target))
            {
                result.IsValid = false;
                result.Errors.Add($"Action {action.Id} references non-existent target '{action.Target}'");
            }

            // Validate AdditionalRecipients
            if (action.AdditionalRecipients != null)
            {
                foreach (var recipient in action.AdditionalRecipients)
                {
                    if (!string.IsNullOrEmpty(recipient) && !participantIds.Contains(recipient))
                    {
                        result.IsValid = false;
                        result.Errors.Add($"Action {action.Id} references non-existent additionalRecipient '{recipient}'");
                    }
                }
            }
        }

        return result;
    }

    private class ParticipantReferenceValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    #endregion
}
