// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.Cryptography.Enums;
using Sorcha.Cryptography.Interfaces;
using Sorcha.Register.Core.Managers;
using Sorcha.Register.Models;
using Sorcha.Register.Service.Services;
using Sorcha.ServiceClients.Validator;
using Sorcha.ServiceClients.Wallet;
using Xunit;

namespace Sorcha.Register.Service.Tests.Unit;

/// <summary>
/// Unit tests for RegisterCreationOrchestrator
/// </summary>
public class RegisterCreationOrchestratorTests
{
    private readonly Mock<ILogger<RegisterCreationOrchestrator>> _mockLogger;
    private readonly Mock<RegisterManager> _mockRegisterManager;
    private readonly Mock<IWalletServiceClient> _mockWalletClient;
    private readonly Mock<IHashProvider> _mockHashProvider;
    private readonly Mock<ICryptoModule> _mockCryptoModule;
    private readonly Mock<IValidatorServiceClient> _mockValidatorClient;
    private readonly RegisterCreationOrchestrator _orchestrator;

    public RegisterCreationOrchestratorTests()
    {
        _mockLogger = new Mock<ILogger<RegisterCreationOrchestrator>>();
        _mockRegisterManager = new Mock<RegisterManager>(
            Mock.Of<Sorcha.Register.Core.Storage.IRegisterRepository>(),
            Mock.Of<Sorcha.Register.Core.Events.IEventPublisher>(),
            Mock.Of<ILogger<RegisterManager>>());
        _mockWalletClient = new Mock<IWalletServiceClient>();
        _mockHashProvider = new Mock<IHashProvider>();
        _mockCryptoModule = new Mock<ICryptoModule>();
        _mockValidatorClient = new Mock<IValidatorServiceClient>();

        _orchestrator = new RegisterCreationOrchestrator(
            _mockLogger.Object,
            _mockRegisterManager.Object,
            _mockWalletClient.Object,
            _mockHashProvider.Object,
            _mockCryptoModule.Object,
            _mockValidatorClient.Object);
    }

    #region InitiateAsync Tests

    [Fact]
    public async Task InitiateAsync_WithValidRequest_ShouldReturnInitiateResponse()
    {
        // Arrange
        var request = new InitiateRegisterCreationRequest
        {
            Name = "Test Register",
            Description = "Test Description",
            TenantId = "tenant-123",
            Creator = new CreatorInfo
            {
                UserId = "user-001",
                WalletId = "wallet-001"
            }
        };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(new byte[32]); // SHA-256 hash

        // Act
        var response = await _orchestrator.InitiateAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.RegisterId.Should().NotBeNullOrEmpty();
        response.RegisterId.Should().HaveLength(32); // GUID without hyphens
        response.ControlRecord.Should().NotBeNull();
        response.ControlRecord.Name.Should().Be("Test Register");
        response.ControlRecord.Description.Should().Be("Test Description");
        response.ControlRecord.TenantId.Should().Be("tenant-123");
        response.ControlRecord.Attestations.Should().HaveCount(1);
        response.ControlRecord.Attestations[0].Role.Should().Be(RegisterRole.Owner);
        response.ControlRecord.Attestations[0].Subject.Should().Be("did:sorcha:user-001");
        response.DataToSign.Should().NotBeNullOrEmpty();
        response.Nonce.Should().NotBeNullOrEmpty();
        response.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task InitiateAsync_WithAdditionalAdmins_ShouldIncludeAllAttestations()
    {
        // Arrange
        var request = new InitiateRegisterCreationRequest
        {
            Name = "Multi-Admin Register",
            TenantId = "tenant-123",
            Creator = new CreatorInfo
            {
                UserId = "user-001",
                WalletId = "wallet-001"
            },
            AdditionalAdmins = new List<AdditionalAdminInfo>
            {
                new()
                {
                    UserId = "user-002",
                    WalletId = "wallet-002",
                    Role = RegisterRole.Admin
                },
                new()
                {
                    UserId = "user-003",
                    WalletId = "wallet-003",
                    Role = RegisterRole.Auditor
                }
            }
        };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(new byte[32]);

        // Act
        var response = await _orchestrator.InitiateAsync(request);

        // Assert
        response.ControlRecord.Attestations.Should().HaveCount(3);
        response.ControlRecord.Attestations[0].Role.Should().Be(RegisterRole.Owner);
        response.ControlRecord.Attestations[0].Subject.Should().Be("did:sorcha:user-001");
        response.ControlRecord.Attestations[1].Role.Should().Be(RegisterRole.Admin);
        response.ControlRecord.Attestations[1].Subject.Should().Be("did:sorcha:user-002");
        response.ControlRecord.Attestations[2].Role.Should().Be(RegisterRole.Auditor);
        response.ControlRecord.Attestations[2].Subject.Should().Be("did:sorcha:user-003");
    }

    [Fact]
    public async Task InitiateAsync_ShouldComputeCanonicalJsonHash()
    {
        // Arrange
        var request = new InitiateRegisterCreationRequest
        {
            Name = "Test Register",
            TenantId = "tenant-123",
            Creator = new CreatorInfo { UserId = "user-001", WalletId = "wallet-001" }
        };

        byte[]? capturedBytes = null;
        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Callback<byte[], HashType>((data, _) => capturedBytes = data)
            .Returns(new byte[32]);

        // Act
        await _orchestrator.InitiateAsync(request);

        // Assert
        capturedBytes.Should().NotBeNull();
        var json = Encoding.UTF8.GetString(capturedBytes!);
        json.Should().Contain("\"registerId\"");
        json.Should().Contain("\"name\"");
        json.Should().Contain("Test Register");
        _mockHashProvider.Verify(
            h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256),
            Times.Once);
    }

    #endregion

    #region FinalizeAsync Tests

    [Fact]
    public async Task FinalizeAsync_WithValidSignatures_ShouldCreateRegister()
    {
        // Arrange
        var initiateRequest = new InitiateRegisterCreationRequest
        {
            Name = "Test Register",
            TenantId = "tenant-123",
            Creator = new CreatorInfo { UserId = "user-001", WalletId = "wallet-001" }
        };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(new byte[32]);

        var initiateResponse = await _orchestrator.InitiateAsync(initiateRequest);

        // Fill in signatures
        initiateResponse.ControlRecord.Attestations[0].PublicKey = Convert.ToBase64String(new byte[32]);
        initiateResponse.ControlRecord.Attestations[0].Signature = Convert.ToBase64String(new byte[64]);

        var finalizeRequest = new FinalizeRegisterCreationRequest
        {
            RegisterId = initiateResponse.RegisterId,
            Nonce = initiateResponse.Nonce,
            ControlRecord = initiateResponse.ControlRecord
        };

        var createdRegister = new Sorcha.Register.Models.Register
        {
            Id = initiateResponse.RegisterId,
            Name = "Test Register",
            TenantId = "tenant-123",
            CreatedAt = DateTime.UtcNow
        };

        _mockCryptoModule
            .Setup(c => c.VerifyAsync(
                It.IsAny<byte[]>(),
                It.IsAny<byte[]>(),
                It.IsAny<byte>(),
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CryptoStatus.Success);

        _mockRegisterManager
            .Setup(m => m.CreateRegisterAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdRegister);

        _mockValidatorClient
            .Setup(v => v.SubmitGenesisTransactionAsync(
                It.IsAny<GenesisTransactionSubmission>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var response = await _orchestrator.FinalizeAsync(finalizeRequest);

        // Assert
        response.Should().NotBeNull();
        response.RegisterId.Should().Be(initiateResponse.RegisterId);
        response.Status.Should().Be("created");
        response.GenesisTransactionId.Should().StartWith("genesis-");
        response.GenesisDocketId.Should().Be("0");

        _mockRegisterManager.Verify(
            m => m.CreateRegisterAsync(
                "Test Register",
                "tenant-123",
                false,
                true,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mockValidatorClient.Verify(
            v => v.SubmitGenesisTransactionAsync(
                It.Is<GenesisTransactionSubmission>(s =>
                    s.RegisterId == initiateResponse.RegisterId &&
                    s.RegisterName == "Test Register" &&
                    s.TenantId == "tenant-123"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task FinalizeAsync_WithInvalidNonce_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var initiateRequest = new InitiateRegisterCreationRequest
        {
            Name = "Test Register",
            TenantId = "tenant-123",
            Creator = new CreatorInfo { UserId = "user-001", WalletId = "wallet-001" }
        };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(new byte[32]);

        var initiateResponse = await _orchestrator.InitiateAsync(initiateRequest);

        var finalizeRequest = new FinalizeRegisterCreationRequest
        {
            RegisterId = initiateResponse.RegisterId,
            Nonce = "invalid-nonce", // Wrong nonce
            ControlRecord = initiateResponse.ControlRecord
        };

        // Act & Assert
        var act = async () => await _orchestrator.FinalizeAsync(finalizeRequest);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*nonce*");
    }

    [Fact]
    public async Task FinalizeAsync_WithExpiredPending_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var finalizeRequest = new FinalizeRegisterCreationRequest
        {
            RegisterId = "non-existent-register",
            Nonce = "some-nonce",
            ControlRecord = new RegisterControlRecord
            {
                RegisterId = "non-existent-register",
                Name = "Test",
                TenantId = "tenant-123",
                CreatedAt = DateTimeOffset.UtcNow,
                Attestations = new List<RegisterAttestation>()
            }
        };

        // Act & Assert
        var act = async () => await _orchestrator.FinalizeAsync(finalizeRequest);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Pending registration not found*");
    }

    [Fact]
    public async Task FinalizeAsync_WithInvalidSignature_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var initiateRequest = new InitiateRegisterCreationRequest
        {
            Name = "Test Register",
            TenantId = "tenant-123",
            Creator = new CreatorInfo { UserId = "user-001", WalletId = "wallet-001" }
        };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(new byte[32]);

        var initiateResponse = await _orchestrator.InitiateAsync(initiateRequest);

        initiateResponse.ControlRecord.Attestations[0].PublicKey = Convert.ToBase64String(new byte[32]);
        initiateResponse.ControlRecord.Attestations[0].Signature = Convert.ToBase64String(new byte[64]);

        var finalizeRequest = new FinalizeRegisterCreationRequest
        {
            RegisterId = initiateResponse.RegisterId,
            Nonce = initiateResponse.Nonce,
            ControlRecord = initiateResponse.ControlRecord
        };

        _mockCryptoModule
            .Setup(c => c.VerifyAsync(
                It.IsAny<byte[]>(),
                It.IsAny<byte[]>(),
                It.IsAny<byte>(),
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CryptoStatus.InvalidSignature); // Invalid signature

        // Act & Assert
        var act = async () => await _orchestrator.FinalizeAsync(finalizeRequest);
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid signature*");
    }

    [Fact]
    public async Task FinalizeAsync_WithMissingOwnerAttestation_ShouldThrowArgumentException()
    {
        // Arrange
        var initiateRequest = new InitiateRegisterCreationRequest
        {
            Name = "Test Register",
            TenantId = "tenant-123",
            Creator = new CreatorInfo { UserId = "user-001", WalletId = "wallet-001" }
        };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(new byte[32]);

        var initiateResponse = await _orchestrator.InitiateAsync(initiateRequest);

        // Change role from Owner to Admin (no owner!)
        initiateResponse.ControlRecord.Attestations[0].Role = RegisterRole.Admin;
        initiateResponse.ControlRecord.Attestations[0].PublicKey = Convert.ToBase64String(new byte[32]);
        initiateResponse.ControlRecord.Attestations[0].Signature = Convert.ToBase64String(new byte[64]);

        var finalizeRequest = new FinalizeRegisterCreationRequest
        {
            RegisterId = initiateResponse.RegisterId,
            Nonce = initiateResponse.Nonce,
            ControlRecord = initiateResponse.ControlRecord
        };

        // Act & Assert
        var act = async () => await _orchestrator.FinalizeAsync(finalizeRequest);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Owner attestation*");
    }

    [Fact]
    public async Task FinalizeAsync_WithTooManyAttestations_ShouldThrowArgumentException()
    {
        // Arrange
        var initiateRequest = new InitiateRegisterCreationRequest
        {
            Name = "Test Register",
            TenantId = "tenant-123",
            Creator = new CreatorInfo { UserId = "user-001", WalletId = "wallet-001" }
        };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(new byte[32]);

        var initiateResponse = await _orchestrator.InitiateAsync(initiateRequest);

        // Add 10 more attestations (total 11, max is 10)
        for (int i = 0; i < 10; i++)
        {
            initiateResponse.ControlRecord.Attestations.Add(new RegisterAttestation
            {
                Role = RegisterRole.Admin,
                Subject = $"did:sorcha:user-{i:000}",
                PublicKey = Convert.ToBase64String(new byte[32]),
                Signature = Convert.ToBase64String(new byte[64]),
                Algorithm = SignatureAlgorithm.ED25519,
                GrantedAt = DateTimeOffset.UtcNow
            });
        }

        initiateResponse.ControlRecord.Attestations[0].PublicKey = Convert.ToBase64String(new byte[32]);
        initiateResponse.ControlRecord.Attestations[0].Signature = Convert.ToBase64String(new byte[64]);

        var finalizeRequest = new FinalizeRegisterCreationRequest
        {
            RegisterId = initiateResponse.RegisterId,
            Nonce = initiateResponse.Nonce,
            ControlRecord = initiateResponse.ControlRecord
        };

        // Act & Assert
        var act = async () => await _orchestrator.FinalizeAsync(finalizeRequest);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Maximum 10 attestations*");
    }

    #endregion

    #region Signature Algorithm Mapping Tests

    [Theory]
    [InlineData(SignatureAlgorithm.ED25519, WalletNetworks.ED25519)]
    [InlineData(SignatureAlgorithm.NISTP256, WalletNetworks.NISTP256)]
    [InlineData(SignatureAlgorithm.RSA4096, WalletNetworks.RSA4096)]
    public async Task FinalizeAsync_ShouldMapSignatureAlgorithmsCorrectly(
        SignatureAlgorithm inputAlgorithm,
        WalletNetworks expectedWalletNetwork)
    {
        // Arrange
        var initiateRequest = new InitiateRegisterCreationRequest
        {
            Name = "Test Register",
            TenantId = "tenant-123",
            Creator = new CreatorInfo { UserId = "user-001", WalletId = "wallet-001" }
        };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(new byte[32]);

        var initiateResponse = await _orchestrator.InitiateAsync(initiateRequest);

        initiateResponse.ControlRecord.Attestations[0].Algorithm = inputAlgorithm;
        initiateResponse.ControlRecord.Attestations[0].PublicKey = Convert.ToBase64String(new byte[32]);
        initiateResponse.ControlRecord.Attestations[0].Signature = Convert.ToBase64String(new byte[64]);

        var finalizeRequest = new FinalizeRegisterCreationRequest
        {
            RegisterId = initiateResponse.RegisterId,
            Nonce = initiateResponse.Nonce,
            ControlRecord = initiateResponse.ControlRecord
        };

        byte? capturedNetwork = null;
        _mockCryptoModule
            .Setup(c => c.VerifyAsync(
                It.IsAny<byte[]>(),
                It.IsAny<byte[]>(),
                It.IsAny<byte>(),
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .Callback<byte[], byte[], byte, byte[], CancellationToken>((_, _, network, _, _) =>
                capturedNetwork = network)
            .ReturnsAsync(CryptoStatus.Success);

        _mockRegisterManager
            .Setup(m => m.CreateRegisterAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Sorcha.Register.Models.Register
            {
                Id = initiateResponse.RegisterId,
                Name = "Test Register",
                TenantId = "tenant-123",
                CreatedAt = DateTime.UtcNow
            });

        _mockValidatorClient
            .Setup(v => v.SubmitGenesisTransactionAsync(
                It.IsAny<GenesisTransactionSubmission>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _orchestrator.FinalizeAsync(finalizeRequest);

        // Assert
        capturedNetwork.Should().Be((byte)expectedWalletNetwork);
    }

    #endregion
}
