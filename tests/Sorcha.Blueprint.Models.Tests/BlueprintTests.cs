// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Blueprint.Models;
using System.ComponentModel.DataAnnotations;

namespace Sorcha.Blueprint.Models.Tests;

public class BlueprintTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        // Act
        var blueprint = new Blueprint();

        // Assert
        blueprint.Id.Should().NotBeNullOrEmpty();
        blueprint.Title.Should().BeEmpty();
        blueprint.Description.Should().BeEmpty();
        blueprint.Version.Should().Be(1);
        blueprint.Participants.Should().NotBeNull().And.BeEmpty();
        blueprint.Actions.Should().NotBeNull().And.BeEmpty();
        blueprint.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        blueprint.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Id_ShouldBeUnique()
    {
        // Act
        var blueprint1 = new Blueprint();
        var blueprint2 = new Blueprint();

        // Assert
        blueprint1.Id.Should().NotBe(blueprint2.Id);
    }

    [Fact]
    public void Title_ShouldAcceptValidValue()
    {
        // Arrange
        var blueprint = new Blueprint();
        var title = "Valid Blueprint Title";

        // Act
        blueprint.Title = title;

        // Assert
        blueprint.Title.Should().Be(title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Title_WithEmptyValue_ShouldFailValidation(string? title)
    {
        // Arrange
        var blueprint = new Blueprint { Title = title!, Description = "Valid description" };
        var context = new ValidationContext(blueprint);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(blueprint, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("Title"));
    }

    [Fact]
    public void Title_TooShort_ShouldFailValidation()
    {
        // Arrange
        var blueprint = new Blueprint { Title = "ab", Description = "Valid description" };
        var context = new ValidationContext(blueprint);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(blueprint, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("Title"));
    }

    [Fact]
    public void Title_TooLong_ShouldFailValidation()
    {
        // Arrange
        var blueprint = new Blueprint
        {
            Title = new string('a', 201),
            Description = "Valid description"
        };
        var context = new ValidationContext(blueprint);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(blueprint, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("Title"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Description_WithEmptyValue_ShouldFailValidation(string? description)
    {
        // Arrange
        var blueprint = new Blueprint { Title = "Valid Title", Description = description! };
        var context = new ValidationContext(blueprint);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(blueprint, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("Description"));
    }

    [Fact]
    public void Description_TooShort_ShouldFailValidation()
    {
        // Arrange
        var blueprint = new Blueprint { Title = "Valid Title", Description = "abcd" };
        var context = new ValidationContext(blueprint);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(blueprint, context, results, validateAllProperties: true);

        // Assert
        isValid.Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("Description"));
    }

    [Fact]
    public void Version_ShouldDefaultToOne()
    {
        // Act
        var blueprint = new Blueprint();

        // Assert
        blueprint.Version.Should().Be(1);
    }

    [Fact]
    public void Version_ShouldAcceptCustomValue()
    {
        // Arrange
        var blueprint = new Blueprint();

        // Act
        blueprint.Version = 5;

        // Assert
        blueprint.Version.Should().Be(5);
    }

    [Fact]
    public void Participants_ShouldBeEmptyByDefault()
    {
        // Act
        var blueprint = new Blueprint();

        // Assert
        blueprint.Participants.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Actions_ShouldBeEmptyByDefault()
    {
        // Act
        var blueprint = new Blueprint();

        // Assert
        blueprint.Actions.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Metadata_ShouldBeNullByDefault()
    {
        // Act
        var blueprint = new Blueprint();

        // Assert
        blueprint.Metadata.Should().BeNull();
    }

    [Fact]
    public void Metadata_ShouldAcceptKeyValuePairs()
    {
        // Arrange
        var blueprint = new Blueprint();
        var metadata = new Dictionary<string, string>
        {
            { "author", "John Doe" },
            { "category", "Finance" }
        };

        // Act
        blueprint.Metadata = metadata;

        // Assert
        blueprint.Metadata.Should().NotBeNull();
        blueprint.Metadata.Should().ContainKey("author").WhoseValue.Should().Be("John Doe");
        blueprint.Metadata.Should().ContainKey("category").WhoseValue.Should().Be("Finance");
    }

    [Fact]
    public void Equals_WithSameId_ShouldReturnTrue()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var blueprint1 = new Blueprint
        {
            Id = id,
            Title = "Test Blueprint",
            Description = "Test Description",
            Version = 1
        };
        var blueprint2 = new Blueprint
        {
            Id = id,
            Title = "Test Blueprint",
            Description = "Test Description",
            Version = 1
        };

        // Act & Assert
        blueprint1.Equals(blueprint2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentId_ShouldReturnFalse()
    {
        // Arrange
        var blueprint1 = new Blueprint
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Test Blueprint",
            Description = "Test Description"
        };
        var blueprint2 = new Blueprint
        {
            Id = Guid.NewGuid().ToString(),
            Title = "Test Blueprint",
            Description = "Test Description"
        };

        // Act & Assert
        blueprint1.Equals(blueprint2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var blueprint = new Blueprint();

        // Act & Assert
        blueprint.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_WithSameData_ShouldBeEqual()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var blueprint1 = new Blueprint
        {
            Id = id,
            Title = "Test",
            Description = "Description",
            Version = 1
        };
        var blueprint2 = new Blueprint
        {
            Id = id,
            Title = "Test",
            Description = "Description",
            Version = 1
        };

        // Act & Assert
        blueprint1.GetHashCode().Should().Be(blueprint2.GetHashCode());
    }

    [Fact]
    public void Timestamps_ShouldBeSetOnCreation()
    {
        // Arrange
        var beforeCreation = DateTimeOffset.UtcNow;

        // Act
        var blueprint = new Blueprint();
        var afterCreation = DateTimeOffset.UtcNow;

        // Assert
        blueprint.CreatedAt.Should().BeOnOrAfter(beforeCreation).And.BeOnOrBefore(afterCreation);
        blueprint.UpdatedAt.Should().BeOnOrAfter(beforeCreation).And.BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void WithCompleteData_ShouldPassValidation()
    {
        // Arrange
        var blueprint = new Blueprint
        {
            Title = "Complete Blueprint",
            Description = "A complete blueprint with all required fields",
            Participants = new List<Participant>
            {
                new Participant { Id = "p1", Name = "Participant 1" },
                new Participant { Id = "p2", Name = "Participant 2" }
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
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }
}
