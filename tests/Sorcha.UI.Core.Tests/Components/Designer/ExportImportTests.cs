// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.UI.Core.Models.Designer;
using Sorcha.UI.Core.Services;
using Sorcha.Blueprint.Models;
using Xunit;

namespace Sorcha.UI.Core.Tests.Components.Designer;

/// <summary>
/// Tests for BlueprintSerializationService and export/import functionality.
/// </summary>
public class ExportImportTests
{
    private readonly BlueprintSerializationService _service = new();

    [Fact]
    public void ToJson_SerializesBlueprint()
    {
        // Arrange
        var blueprint = CreateTestBlueprint();

        // Act
        var json = _service.ToJson(blueprint);

        // Assert
        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("\"title\":");
        json.Should().Contain("Test Blueprint");
        json.Should().Contain("\"formatVersion\":");
    }

    [Fact]
    public void ToYaml_SerializesBlueprint()
    {
        // Arrange
        var blueprint = CreateTestBlueprint();

        // Act
        var yaml = _service.ToYaml(blueprint);

        // Assert
        yaml.Should().NotBeNullOrWhiteSpace();
        yaml.Should().Contain("formatVersion:");
        yaml.Should().Contain("blueprint:");
    }

    [Fact]
    public void ValidateAndParse_Json_ValidBlueprint_ReturnsSuccess()
    {
        // Arrange
        var blueprint = CreateTestBlueprint();
        var json = _service.ToJson(blueprint);

        // Act
        var result = _service.ValidateAndParse(json, "test.json");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Blueprint.Should().NotBeNull();
        result.Blueprint!.Title.Should().Be("Test Blueprint");
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateAndParse_Yaml_ValidBlueprint_ReturnsSuccess()
    {
        // Arrange
        var blueprint = CreateTestBlueprint();
        var yaml = _service.ToYaml(blueprint);

        // Act
        var result = _service.ValidateAndParse(yaml, "test.yaml");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Blueprint.Should().NotBeNull();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateAndParse_Yml_ValidBlueprint_ReturnsSuccess()
    {
        // Arrange
        var blueprint = CreateTestBlueprint();
        var yaml = _service.ToYaml(blueprint);

        // Act
        var result = _service.ValidateAndParse(yaml, "test.yml");

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateAndParse_InvalidJson_ReturnsError()
    {
        // Arrange
        var invalidJson = "{ this is not valid json }";

        // Act
        var result = _service.ValidateAndParse(invalidJson, "test.json");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Type.Should().Be(ImportErrorType.InvalidFormat);
    }

    [Fact]
    public void ValidateAndParse_InvalidYaml_ReturnsError()
    {
        // Arrange
        var invalidYaml = "not: valid: yaml: syntax:";

        // Act
        var result = _service.ValidateAndParse(invalidYaml, "test.yaml");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Type.Should().Be(ImportErrorType.InvalidFormat);
    }

    [Fact]
    public void ValidateAndParse_MissingBlueprint_ReturnsError()
    {
        // Arrange
        var json = "{ \"formatVersion\": \"1.0\" }";

        // Act
        var result = _service.ValidateAndParse(json, "test.json");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Type.Should().Be(ImportErrorType.InvalidFormat);
    }

    [Fact]
    public void ValidateAndParse_MissingTitle_ReturnsError()
    {
        // Arrange
        var json = @"{
            ""formatVersion"": ""1.0"",
            ""blueprint"": {
                ""id"": ""test-id"",
                ""title"": """"
            }
        }";

        // Act
        var result = _service.ValidateAndParse(json, "test.json");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Type.Should().Be(ImportErrorType.MissingRequiredField);
    }

    [Fact]
    public void ValidateAndParse_MissingId_GeneratesId()
    {
        // Arrange
        var json = @"{
            ""formatVersion"": ""1.0"",
            ""blueprint"": {
                ""title"": ""No ID Blueprint""
            }
        }";

        // Act
        var result = _service.ValidateAndParse(json, "test.json");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Blueprint!.Id.Should().NotBeNullOrWhiteSpace();
        result.Warnings.Should().Contain(w => w.Path.Contains("id"));
    }

    [Fact]
    public void ValidateAndParse_InvalidParticipantReference_ReturnsWarning()
    {
        // Arrange
        var blueprint = CreateTestBlueprint();
        blueprint.Actions[0].Target = "non-existent-participant";
        var json = _service.ToJson(blueprint);

        // Act
        var result = _service.ValidateAndParse(json, "test.json");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Message.Contains("non-existent"));
    }

    [Fact]
    public void ValidateAndParse_DifferentFormatVersion_ReturnsWarning()
    {
        // Arrange
        var json = @"{
            ""formatVersion"": ""2.0"",
            ""blueprint"": {
                ""id"": ""test-id"",
                ""title"": ""Version 2 Blueprint""
            }
        }";

        // Act
        var result = _service.ValidateAndParse(json, "test.json");

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Message.Contains("format version"));
    }

    [Fact]
    public void GetFileExtension_ReturnsCorrectExtensions()
    {
        // Assert
        BlueprintSerializationService.GetFileExtension(ExportFormat.Json).Should().Be(".json");
        BlueprintSerializationService.GetFileExtension(ExportFormat.Yaml).Should().Be(".yaml");
    }

    [Fact]
    public void GetMimeType_ReturnsCorrectMimeTypes()
    {
        // Assert
        BlueprintSerializationService.GetMimeType(ExportFormat.Json).Should().Be("application/json");
        BlueprintSerializationService.GetMimeType(ExportFormat.Yaml).Should().Be("text/yaml");
    }

    [Fact]
    public void RoundTrip_Json_PreservesData()
    {
        // Arrange
        var original = CreateTestBlueprint();
        original.Participants.Add(new Participant { Id = "p1", Name = "Alice", WalletAddress = "0x123" });
        original.Actions.Add(new Sorcha.Blueprint.Models.Action { Id = 2, Title = "Action 2" });

        // Act
        var json = _service.ToJson(original);
        var result = _service.ValidateAndParse(json, "test.json");

        // Assert
        result.IsValid.Should().BeTrue();
        var restored = result.Blueprint!;
        restored.Title.Should().Be(original.Title);
        restored.Description.Should().Be(original.Description);
        restored.Actions.Should().HaveCount(original.Actions.Count);
        restored.Participants.Should().HaveCount(original.Participants.Count);
    }

    [Fact]
    public void RoundTrip_Yaml_PreservesData()
    {
        // Arrange
        var original = CreateTestBlueprint();
        original.Participants.Add(new Participant { Id = "p1", Name = "Bob", WalletAddress = "0x456" });

        // Act
        var yaml = _service.ToYaml(original);
        var result = _service.ValidateAndParse(yaml, "test.yaml");

        // Assert
        result.IsValid.Should().BeTrue();
        var restored = result.Blueprint!;
        restored.Title.Should().Be(original.Title);
        restored.Participants.Should().HaveCount(original.Participants.Count);
    }

    private static Sorcha.Blueprint.Models.Blueprint CreateTestBlueprint()
    {
        return new Sorcha.Blueprint.Models.Blueprint
        {
            Id = "test-blueprint-id",
            Title = "Test Blueprint",
            Description = "A test blueprint for unit tests",
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            Actions =
            [
                new Sorcha.Blueprint.Models.Action
                {
                    Id = 1,
                    Title = "Test Action",
                    Description = "A test action"
                }
            ],
            Participants = []
        };
    }
}
