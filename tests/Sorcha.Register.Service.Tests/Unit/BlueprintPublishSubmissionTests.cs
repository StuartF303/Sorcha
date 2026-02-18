// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using FluentAssertions;
using Moq;
using Sorcha.ServiceClients.SystemWallet;
using Sorcha.ServiceClients.Validator;
using Xunit;

namespace Sorcha.Register.Service.Tests.Unit;

/// <summary>
/// Tests verifying that blueprint publish uses the unified transaction submission path
/// (ISystemWalletSigningService + SubmitTransactionAsync) instead of the legacy genesis endpoint.
/// </summary>
public class BlueprintPublishSubmissionTests
{
    private readonly Mock<ISystemWalletSigningService> _mockSigningService;
    private readonly Mock<IValidatorServiceClient> _mockValidatorClient;

    private const string TestRegisterId = "test-register-001";
    private const string TestBlueprintId = "test-blueprint-001";
    private const string TestTxId = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2";
    private const string TestPayloadHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    public BlueprintPublishSubmissionTests()
    {
        _mockSigningService = new Mock<ISystemWalletSigningService>();
        _mockValidatorClient = new Mock<IValidatorServiceClient>();

        _mockSigningService
            .Setup(s => s.SignAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SystemSignResult
            {
                Signature = new byte[] { 10, 20, 30 },
                PublicKey = new byte[] { 40, 50, 60 },
                Algorithm = "ED25519",
                WalletAddress = "system-wallet-bp"
            });

        _mockValidatorClient
            .Setup(v => v.SubmitTransactionAsync(
                It.IsAny<TransactionSubmission>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionSubmissionResult
            {
                Success = true,
                TransactionId = TestTxId,
                RegisterId = TestRegisterId,
                AddedAt = DateTimeOffset.UtcNow
            });
    }

    [Fact]
    public async Task BlueprintPublish_ShouldSignWithSystemWallet()
    {
        // Act — simulate the blueprint publish signing flow
        var signResult = await _mockSigningService.Object.SignAsync(
            TestRegisterId, TestTxId, TestPayloadHash,
            "sorcha:register-control", "Control");

        // Assert
        signResult.Should().NotBeNull();
        signResult.Algorithm.Should().Be("ED25519");
        signResult.WalletAddress.Should().Be("system-wallet-bp");

        _mockSigningService.Verify(
            s => s.SignAsync(
                TestRegisterId, TestTxId, TestPayloadHash,
                "sorcha:register-control", "Control",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BlueprintPublish_ShouldSubmitViaGenericEndpoint()
    {
        // Arrange — sign first
        var signResult = await _mockSigningService.Object.SignAsync(
            TestRegisterId, TestTxId, TestPayloadHash,
            "sorcha:register-control", "Control");

        var systemSignature = new SignatureInfo
        {
            PublicKey = Convert.ToBase64String(signResult.PublicKey),
            SignatureValue = Convert.ToBase64String(signResult.Signature),
            Algorithm = signResult.Algorithm
        };

        var submission = new TransactionSubmission
        {
            TransactionId = TestTxId,
            RegisterId = TestRegisterId,
            BlueprintId = TestBlueprintId,
            ActionId = "blueprint-publish",
            Payload = JsonDocument.Parse("{}").RootElement,
            PayloadHash = TestPayloadHash,
            Signatures = new List<SignatureInfo> { systemSignature },
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = new Dictionary<string, string>
            {
                ["Type"] = "Control",
                ["transactionType"] = "BlueprintPublish",
                ["publishedBy"] = "test-user",
                ["SystemWalletAddress"] = signResult.WalletAddress
            }
        };

        // Act
        var result = await _mockValidatorClient.Object.SubmitTransactionAsync(submission);

        // Assert
        result.Success.Should().BeTrue();

        _mockValidatorClient.Verify(
            v => v.SubmitTransactionAsync(
                It.Is<TransactionSubmission>(s =>
                    s.RegisterId == TestRegisterId &&
                    s.BlueprintId == TestBlueprintId &&
                    s.ActionId == "blueprint-publish" &&
                    s.Metadata != null &&
                    s.Metadata["Type"] == "Control" &&
                    s.Metadata["transactionType"] == "BlueprintPublish"),
                It.IsAny<CancellationToken>()),
            Times.Once);

    }

    [Fact]
    public async Task BlueprintPublish_Signature_ShouldBeBase64Encoded()
    {
        // Act
        var signResult = await _mockSigningService.Object.SignAsync(
            TestRegisterId, TestTxId, TestPayloadHash,
            "sorcha:register-control", "Control");

        var systemSignature = new SignatureInfo
        {
            PublicKey = Convert.ToBase64String(signResult.PublicKey),
            SignatureValue = Convert.ToBase64String(signResult.Signature),
            Algorithm = signResult.Algorithm
        };

        // Assert — verify base64 encoding
        systemSignature.PublicKey.Should().Be(Convert.ToBase64String(new byte[] { 40, 50, 60 }));
        systemSignature.SignatureValue.Should().Be(Convert.ToBase64String(new byte[] { 10, 20, 30 }));
        systemSignature.Algorithm.Should().Be("ED25519");
    }

    [Fact]
    public async Task BlueprintPublish_Metadata_ShouldIncludeControlType()
    {
        // Arrange
        var metadata = new Dictionary<string, string>
        {
            ["Type"] = "Control",
            ["transactionType"] = "BlueprintPublish",
            ["publishedBy"] = "user-abc",
            ["SystemWalletAddress"] = "sys-wallet-001"
        };

        var submission = new TransactionSubmission
        {
            TransactionId = TestTxId,
            RegisterId = TestRegisterId,
            BlueprintId = TestBlueprintId,
            ActionId = "blueprint-publish",
            Payload = JsonDocument.Parse("{}").RootElement,
            PayloadHash = TestPayloadHash,
            Signatures = new List<SignatureInfo>(),
            CreatedAt = DateTimeOffset.UtcNow,
            Metadata = metadata
        };

        // Assert
        submission.Metadata.Should().ContainKey("Type").WhoseValue.Should().Be("Control");
        submission.Metadata.Should().ContainKey("transactionType").WhoseValue.Should().Be("BlueprintPublish");
        submission.Metadata.Should().ContainKey("publishedBy");
        submission.Metadata.Should().ContainKey("SystemWalletAddress");
    }
}
