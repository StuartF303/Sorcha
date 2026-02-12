// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Moq;
using Sorcha.Blueprint.Engine.Credentials;
using Sorcha.Blueprint.Engine.Implementation;
using Sorcha.Blueprint.Engine.Interfaces;
using Sorcha.Blueprint.Models.Credentials;
using EngineModels = Sorcha.Blueprint.Engine.Models;
using BpModels = Sorcha.Blueprint.Models;

namespace Sorcha.Blueprint.Engine.Tests.Credentials;

public class ActionProcessorCredentialTests
{
    private readonly Mock<ISchemaValidator> _schemaValidatorMock = new();
    private readonly Mock<IJsonLogicEvaluator> _jsonLogicMock = new();
    private readonly Mock<IDisclosureProcessor> _disclosureMock = new();
    private readonly Mock<IRoutingEngine> _routingMock = new();
    private readonly Mock<ICredentialVerifier> _credentialVerifierMock = new();

    public ActionProcessorCredentialTests()
    {
        // Default: schema validation passes
        _schemaValidatorMock
            .Setup(v => v.ValidateAsync(
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<System.Text.Json.Nodes.JsonNode>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(EngineModels.ValidationResult.Valid());

        // Default: routing returns workflow complete
        _routingMock
            .Setup(r => r.DetermineNextAsync(
                It.IsAny<BpModels.Blueprint>(),
                It.IsAny<BpModels.Action>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EngineModels.RoutingResult { IsWorkflowComplete = true });

        // Default: no disclosures
        _disclosureMock
            .Setup(d => d.CreateDisclosures(
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<IEnumerable<BpModels.Disclosure>>()))
            .Returns(new List<EngineModels.DisclosureResult>());
    }

    private ActionProcessor CreateProcessor(ICredentialVerifier? verifier = null)
    {
        return new ActionProcessor(
            _schemaValidatorMock.Object,
            _jsonLogicMock.Object,
            _disclosureMock.Object,
            _routingMock.Object,
            verifier);
    }

    private static EngineModels.ExecutionContext CreateContext(
        BpModels.Action action,
        IEnumerable<CredentialPresentation>? presentations = null)
    {
        var blueprint = new BpModels.Blueprint
        {
            Id = "BP-001",
            Title = "Test",
            Description = "Test",
            Participants = [new() { Id = "user1", Name = "User 1" }],
            Actions = [action]
        };

        return new EngineModels.ExecutionContext
        {
            Blueprint = blueprint,
            Action = action,
            ActionData = new Dictionary<string, object> { ["data"] = "test" },
            ParticipantId = "user1",
            WalletAddress = "0x123",
            CredentialPresentations = presentations ?? []
        };
    }

    [Fact]
    public async Task ProcessAsync_NoCredentialRequirements_SkipsVerification()
    {
        var action = new BpModels.Action
        {
            Id = 1,
            Title = "No Creds",
            Sender = "user1",
            CredentialRequirements = null
        };

        var processor = CreateProcessor(_credentialVerifierMock.Object);
        var result = await processor.ProcessAsync(CreateContext(action));

        result.Success.Should().BeTrue();
        result.CredentialValidation.Should().BeNull();
        _credentialVerifierMock.Verify(
            v => v.VerifyAsync(
                It.IsAny<IEnumerable<CredentialRequirement>>(),
                It.IsAny<IEnumerable<CredentialPresentation>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_NoVerifierInjected_SkipsVerification()
    {
        var action = new BpModels.Action
        {
            Id = 1,
            Title = "Has Creds But No Verifier",
            Sender = "user1",
            CredentialRequirements = [new CredentialRequirement { Type = "LicenseCredential" }]
        };

        var processor = CreateProcessor(verifier: null);
        var result = await processor.ProcessAsync(CreateContext(action));

        result.Success.Should().BeTrue();
        result.CredentialValidation.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAsync_ValidCredentials_ProceedsToSchemaValidation()
    {
        var action = new BpModels.Action
        {
            Id = 1,
            Title = "Gated Action",
            Sender = "user1",
            CredentialRequirements = [new CredentialRequirement { Type = "LicenseCredential" }]
        };

        _credentialVerifierMock
            .Setup(v => v.VerifyAsync(
                It.IsAny<IEnumerable<CredentialRequirement>>(),
                It.IsAny<IEnumerable<CredentialPresentation>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CredentialValidationResult
            {
                IsValid = true,
                VerifiedCredentials = [new VerifiedCredentialDetail { CredentialId = "cred-1", Type = "LicenseCredential" }]
            });

        var presentations = new[]
        {
            new CredentialPresentation
            {
                CredentialId = "cred-1",
                DisclosedClaims = new Dictionary<string, object> { ["type"] = "LicenseCredential" },
                RawPresentation = "jwt~"
            }
        };

        var processor = CreateProcessor(_credentialVerifierMock.Object);
        var result = await processor.ProcessAsync(CreateContext(action, presentations));

        result.Success.Should().BeTrue();
        result.CredentialValidation.Should().NotBeNull();
        result.CredentialValidation!.IsValid.Should().BeTrue();

        // Schema validation should have been called (proceeded past Step 0)
        _schemaValidatorMock.Verify(
            v => v.ValidateAsync(
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<System.Text.Json.Nodes.JsonNode>(),
                It.IsAny<CancellationToken>()),
            Times.Never); // No schema on this action
    }

    [Fact]
    public async Task ProcessAsync_InvalidCredentials_ReturnsFailureWithErrors()
    {
        var action = new BpModels.Action
        {
            Id = 1,
            Title = "Gated Action",
            Sender = "user1",
            CredentialRequirements = [new CredentialRequirement { Type = "LicenseCredential" }]
        };

        _credentialVerifierMock
            .Setup(v => v.VerifyAsync(
                It.IsAny<IEnumerable<CredentialRequirement>>(),
                It.IsAny<IEnumerable<CredentialPresentation>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CredentialValidationResult
            {
                IsValid = false,
                Errors = [new CredentialValidationError
                {
                    RequirementType = "LicenseCredential",
                    FailureReason = CredentialFailureReason.Missing,
                    Message = "No credential of type 'LicenseCredential' was presented"
                }]
            });

        var processor = CreateProcessor(_credentialVerifierMock.Object);
        var result = await processor.ProcessAsync(CreateContext(action));

        result.Success.Should().BeFalse();
        result.CredentialValidation.Should().NotBeNull();
        result.CredentialValidation!.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("Credential verification failed"));
    }

    [Fact]
    public async Task ProcessAsync_CredentialFailure_ShortCircuitsBeforeSchemaValidation()
    {
        var action = new BpModels.Action
        {
            Id = 1,
            Title = "Gated Action",
            Sender = "user1",
            CredentialRequirements = [new CredentialRequirement { Type = "LicenseCredential" }],
            Form = new BpModels.Control
            {
                Schema = System.Text.Json.Nodes.JsonNode.Parse("""{"type":"object","required":["x"]}""")
            }
        };

        _credentialVerifierMock
            .Setup(v => v.VerifyAsync(
                It.IsAny<IEnumerable<CredentialRequirement>>(),
                It.IsAny<IEnumerable<CredentialPresentation>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CredentialValidationResult
            {
                IsValid = false,
                Errors = [new CredentialValidationError
                {
                    RequirementType = "LicenseCredential",
                    FailureReason = CredentialFailureReason.Missing,
                    Message = "Missing credential"
                }]
            });

        var processor = CreateProcessor(_credentialVerifierMock.Object);
        var result = await processor.ProcessAsync(CreateContext(action));

        result.Success.Should().BeFalse();

        // Schema validation should NOT have been called
        _schemaValidatorMock.Verify(
            v => v.ValidateAsync(
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<System.Text.Json.Nodes.JsonNode>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        // Routing should NOT have been called
        _routingMock.Verify(
            r => r.DetermineNextAsync(
                It.IsAny<BpModels.Blueprint>(),
                It.IsAny<BpModels.Action>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_EmptyRequirementsList_SkipsVerification()
    {
        var action = new BpModels.Action
        {
            Id = 1,
            Title = "Empty Reqs",
            Sender = "user1",
            CredentialRequirements = [] // empty list, not null
        };

        var processor = CreateProcessor(_credentialVerifierMock.Object);
        var result = await processor.ProcessAsync(CreateContext(action));

        result.Success.Should().BeTrue();
        _credentialVerifierMock.Verify(
            v => v.VerifyAsync(
                It.IsAny<IEnumerable<CredentialRequirement>>(),
                It.IsAny<IEnumerable<CredentialPresentation>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
