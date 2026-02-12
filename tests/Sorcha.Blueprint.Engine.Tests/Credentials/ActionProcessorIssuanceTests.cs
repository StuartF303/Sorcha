// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Moq;
using Sorcha.Blueprint.Engine.Credentials;
using Sorcha.Blueprint.Engine.Implementation;
using Sorcha.Blueprint.Engine.Interfaces;
using Sorcha.Blueprint.Engine.Models;
using Sorcha.Blueprint.Models.Credentials;
using ActionModel = Sorcha.Blueprint.Models.Action;
using BlueprintModel = Sorcha.Blueprint.Models.Blueprint;

namespace Sorcha.Blueprint.Engine.Tests.Credentials;

/// <summary>
/// Unit tests for ActionProcessor Step 5 — credential issuance on credentialed actions.
/// </summary>
public class ActionProcessorIssuanceTests
{
    private readonly Mock<ISchemaValidator> _validatorMock;
    private readonly Mock<IJsonLogicEvaluator> _jsonLogicMock;
    private readonly Mock<IDisclosureProcessor> _disclosureMock;
    private readonly Mock<IRoutingEngine> _routingMock;
    private readonly Mock<ICredentialIssuer> _issuerMock;

    public ActionProcessorIssuanceTests()
    {
        _validatorMock = new Mock<ISchemaValidator>();
        _jsonLogicMock = new Mock<IJsonLogicEvaluator>();
        _disclosureMock = new Mock<IDisclosureProcessor>();
        _routingMock = new Mock<IRoutingEngine>();
        _issuerMock = new Mock<ICredentialIssuer>();

        SetupDefaults();
    }

    [Fact]
    public async Task ProcessAsync_ActionWithIssuanceConfig_IssuesCredential()
    {
        // Arrange
        var processor = CreateProcessor(credentialIssuer: _issuerMock.Object);
        var issuanceConfig = new CredentialIssuanceConfig
        {
            CredentialType = "LicenseCredential",
            ClaimMappings = [new ClaimMapping { ClaimName = "type", SourceField = "/licenseType" }],
            RecipientParticipantId = "applicant"
        };

        var context = CreateContext(issuanceConfig: issuanceConfig);

        var expectedCredential = new IssuedCredentialInfo
        {
            CredentialId = "urn:uuid:test-123",
            Type = "LicenseCredential",
            IssuerDid = "did:issuer:1",
            SubjectDid = "did:recipient:1",
            IssuedAt = DateTimeOffset.UtcNow,
            RawToken = "eyJ.eyJ.sig~"
        };

        _issuerMock.Setup(i => i.IssueAsync(
                It.IsAny<CredentialIssuanceConfig>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<byte[]>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCredential);

        // Act
        var result = await processor.ProcessAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.IssuedCredential.Should().NotBeNull();
        result.IssuedCredential!.CredentialId.Should().Be("urn:uuid:test-123");
        result.IssuedCredential.Type.Should().Be("LicenseCredential");
    }

    [Fact]
    public async Task ProcessAsync_ActionWithoutIssuanceConfig_NoCredentialIssued()
    {
        // Arrange
        var processor = CreateProcessor(credentialIssuer: _issuerMock.Object);
        var context = CreateContext(issuanceConfig: null);

        // Act
        var result = await processor.ProcessAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.IssuedCredential.Should().BeNull();
        _issuerMock.Verify(i => i.IssueAsync(
            It.IsAny<CredentialIssuanceConfig>(),
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<byte[]>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_NoIssuerInjected_SkipsIssuance()
    {
        // Arrange — create processor without credential issuer
        var processor = CreateProcessor(credentialIssuer: null);
        var issuanceConfig = new CredentialIssuanceConfig
        {
            CredentialType = "LicenseCredential",
            ClaimMappings = [],
            RecipientParticipantId = "applicant"
        };

        var context = CreateContext(issuanceConfig: issuanceConfig);

        // Act
        var result = await processor.ProcessAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.IssuedCredential.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAsync_NoIssuanceContext_SkipsIssuance()
    {
        // Arrange — action has config but no signing context
        var processor = CreateProcessor(credentialIssuer: _issuerMock.Object);
        var issuanceConfig = new CredentialIssuanceConfig
        {
            CredentialType = "LicenseCredential",
            ClaimMappings = [],
            RecipientParticipantId = "applicant"
        };

        var context = CreateContext(issuanceConfig: issuanceConfig, includeIssuanceContext: false);

        // Act
        var result = await processor.ProcessAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.IssuedCredential.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAsync_IssuanceUsesProcessedData_IncludingCalculations()
    {
        // Arrange
        var processor = CreateProcessor(credentialIssuer: _issuerMock.Object);
        var issuanceConfig = new CredentialIssuanceConfig
        {
            CredentialType = "TestCred",
            ClaimMappings = [],
            RecipientParticipantId = "recipient"
        };

        var context = CreateContext(issuanceConfig: issuanceConfig);
        Dictionary<string, object>? capturedData = null;

        _issuerMock.Setup(i => i.IssueAsync(
                It.IsAny<CredentialIssuanceConfig>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<byte[]>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<CredentialIssuanceConfig, Dictionary<string, object>,
                string, string, byte[], string, CancellationToken>(
                (_, data, _, _, _, _, _) => capturedData = data)
            .ReturnsAsync(new IssuedCredentialInfo
            {
                CredentialId = "urn:uuid:test", Type = "TestCred",
                IssuerDid = "i", SubjectDid = "s",
                IssuedAt = DateTimeOffset.UtcNow, RawToken = "t"
            });

        // Act
        await processor.ProcessAsync(context);

        // Assert — issuer receives the processed (merged) data
        capturedData.Should().NotBeNull();
        capturedData.Should().ContainKey("field1");
    }

    [Fact]
    public async Task ProcessAsync_IssuancePassesCorrectSigningContext()
    {
        // Arrange
        var processor = CreateProcessor(credentialIssuer: _issuerMock.Object);
        var issuanceConfig = new CredentialIssuanceConfig
        {
            CredentialType = "TestCred",
            ClaimMappings = [],
            RecipientParticipantId = "recipient"
        };

        var signingKey = new byte[] { 10, 20, 30 };
        var context = CreateContext(issuanceConfig: issuanceConfig, signingKey: signingKey);

        string? capturedIssuer = null;
        string? capturedRecipient = null;
        byte[]? capturedKey = null;
        string? capturedAlgorithm = null;

        _issuerMock.Setup(i => i.IssueAsync(
                It.IsAny<CredentialIssuanceConfig>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<byte[]>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<CredentialIssuanceConfig, Dictionary<string, object>,
                string, string, byte[], string, CancellationToken>(
                (_, _, issuer, recipient, key, algorithm, _) =>
                {
                    capturedIssuer = issuer;
                    capturedRecipient = recipient;
                    capturedKey = key;
                    capturedAlgorithm = algorithm;
                })
            .ReturnsAsync(new IssuedCredentialInfo
            {
                CredentialId = "urn:uuid:test", Type = "TestCred",
                IssuerDid = "i", SubjectDid = "s",
                IssuedAt = DateTimeOffset.UtcNow, RawToken = "t"
            });

        // Act
        await processor.ProcessAsync(context);

        // Assert
        capturedIssuer.Should().Be("did:issuer:authority");
        capturedRecipient.Should().Be("did:recipient:holder");
        capturedKey.Should().BeEquivalentTo(signingKey);
        capturedAlgorithm.Should().Be("EdDSA");
    }

    private ActionProcessor CreateProcessor(ICredentialIssuer? credentialIssuer)
    {
        return new ActionProcessor(
            _validatorMock.Object,
            _jsonLogicMock.Object,
            _disclosureMock.Object,
            _routingMock.Object,
            credentialVerifier: null,
            credentialIssuer: credentialIssuer);
    }

    private static Engine.Models.ExecutionContext CreateContext(
        CredentialIssuanceConfig? issuanceConfig = null,
        bool includeIssuanceContext = true,
        byte[]? signingKey = null)
    {
        var action = new ActionModel
        {
            Id = 1,
            Title = "Test Action",
            CredentialIssuanceConfig = issuanceConfig
        };

        var blueprint = new BlueprintModel
        {
            Title = "Test Blueprint"
        };

        CredentialIssuanceContext? issuanceCtx = null;
        if (includeIssuanceContext && issuanceConfig != null)
        {
            issuanceCtx = new CredentialIssuanceContext
            {
                IssuerDid = "did:issuer:authority",
                RecipientDid = "did:recipient:holder",
                SigningKey = signingKey ?? [1, 2, 3],
                Algorithm = "EdDSA"
            };
        }

        return new Engine.Models.ExecutionContext
        {
            Blueprint = blueprint,
            Action = action,
            ActionData = new Dictionary<string, object> { ["field1"] = "value1" },
            ParticipantId = "participant-1",
            WalletAddress = "ws1test",
            IssuanceContext = issuanceCtx
        };
    }

    private void SetupDefaults()
    {
        _validatorMock.Setup(v => v.ValidateAsync(
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<System.Text.Json.Nodes.JsonNode>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Engine.Models.ValidationResult.Valid());

        _routingMock.Setup(r => r.DetermineNextAsync(
                It.IsAny<BlueprintModel>(),
                It.IsAny<ActionModel>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoutingResult { IsWorkflowComplete = true });

        _disclosureMock.Setup(d => d.CreateDisclosures(
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<List<Sorcha.Blueprint.Models.Disclosure>>()))
            .Returns(new List<DisclosureResult>());
    }
}
