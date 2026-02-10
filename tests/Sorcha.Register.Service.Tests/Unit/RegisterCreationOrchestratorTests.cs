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
using Microsoft.AspNetCore.SignalR;
using Sorcha.Register.Service.Hubs;
using Sorcha.ServiceClients.Peer;
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
    private readonly Mock<TransactionManager> _mockTransactionManager;
    private readonly Mock<IWalletServiceClient> _mockWalletClient;
    private readonly Mock<IHashProvider> _mockHashProvider;
    private readonly Mock<ICryptoModule> _mockCryptoModule;
    private readonly Mock<IValidatorServiceClient> _mockValidatorClient;
    private readonly Mock<IPendingRegistrationStore> _mockPendingStore;
    private readonly Mock<IPeerServiceClient> _mockPeerClient;
    private readonly Mock<IHubContext<RegisterHub, IRegisterHubClient>> _mockHubContext;
    private readonly RegisterCreationOrchestrator _orchestrator;

    public RegisterCreationOrchestratorTests()
    {
        _mockLogger = new Mock<ILogger<RegisterCreationOrchestrator>>();
        _mockRegisterManager = new Mock<RegisterManager>(
            Mock.Of<Sorcha.Register.Core.Storage.IRegisterRepository>(),
            Mock.Of<Sorcha.Register.Core.Events.IEventPublisher>());
        _mockTransactionManager = new Mock<TransactionManager>(
            Mock.Of<Sorcha.Register.Core.Storage.IRegisterRepository>(),
            Mock.Of<Sorcha.Register.Core.Events.IEventPublisher>());
        _mockWalletClient = new Mock<IWalletServiceClient>();
        _mockHashProvider = new Mock<IHashProvider>();
        _mockCryptoModule = new Mock<ICryptoModule>();
        _mockValidatorClient = new Mock<IValidatorServiceClient>();
        _mockPendingStore = new Mock<IPendingRegistrationStore>();
        _mockPeerClient = new Mock<IPeerServiceClient>();
        _mockHubContext = new Mock<IHubContext<RegisterHub, IRegisterHubClient>>();

        // Default hub context: return a mock client that accepts all calls
        var mockHubClients = new Mock<IHubClients<IRegisterHubClient>>();
        var mockHubClient = new Mock<IRegisterHubClient>();
        mockHubClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockHubClient.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(mockHubClients.Object);

        // Default: UpdateRegisterStatusAsync returns the register with new status
        _mockRegisterManager
            .Setup(m => m.UpdateRegisterStatusAsync(
                It.IsAny<string>(),
                It.IsAny<Sorcha.Register.Models.Enums.RegisterStatus>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, Sorcha.Register.Models.Enums.RegisterStatus status, CancellationToken _) =>
                new Sorcha.Register.Models.Register { Id = id, Status = status });

        // Default pending store behavior: store and retrieve
        var store = new Dictionary<string, PendingRegistration>();
        _mockPendingStore
            .Setup(s => s.Add(It.IsAny<string>(), It.IsAny<PendingRegistration>()))
            .Callback<string, PendingRegistration>((key, value) => store[key] = value);
        _mockPendingStore
            .Setup(s => s.TryRemove(It.IsAny<string>(), out It.Ref<PendingRegistration?>.IsAny))
            .Returns((string key, out PendingRegistration? value) =>
            {
                if (store.TryGetValue(key, out var v))
                {
                    value = v;
                    store.Remove(key);
                    return true;
                }
                value = null;
                return false;
            });

        _orchestrator = new RegisterCreationOrchestrator(
            _mockLogger.Object,
            _mockRegisterManager.Object,
            _mockTransactionManager.Object,
            _mockWalletClient.Object,
            _mockHashProvider.Object,
            _mockCryptoModule.Object,
            _mockValidatorClient.Object,
            _mockPendingStore.Object,
            _mockPeerClient.Object,
            _mockHubContext.Object);
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
            Owners = new List<OwnerInfo>
            {
                new() { UserId = "user-001", WalletId = "wallet-001" }
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
        response.AttestationsToSign.Should().HaveCount(1);
        response.AttestationsToSign[0].Role.Should().Be(RegisterRole.Owner);
        response.AttestationsToSign[0].AttestationData.Subject.Should().Be("did:sorcha:user-001");
        response.AttestationsToSign[0].DataToSign.Should().NotBeNullOrEmpty();
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
            Owners = new List<OwnerInfo>
            {
                new() { UserId = "user-001", WalletId = "wallet-001" }
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
        response.AttestationsToSign.Should().HaveCount(3);
        response.AttestationsToSign[0].Role.Should().Be(RegisterRole.Owner);
        response.AttestationsToSign[0].AttestationData.Subject.Should().Be("did:sorcha:user-001");
        response.AttestationsToSign[1].Role.Should().Be(RegisterRole.Admin);
        response.AttestationsToSign[1].AttestationData.Subject.Should().Be("did:sorcha:user-002");
        response.AttestationsToSign[2].Role.Should().Be(RegisterRole.Auditor);
        response.AttestationsToSign[2].AttestationData.Subject.Should().Be("did:sorcha:user-003");
    }

    [Fact]
    public async Task InitiateAsync_ShouldComputeCanonicalJsonHash()
    {
        // Arrange
        var request = new InitiateRegisterCreationRequest
        {
            Name = "Test Register",
            TenantId = "tenant-123",
            Owners = new List<OwnerInfo>
            {
                new() { UserId = "user-001", WalletId = "wallet-001" }
            }
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
        json.Should().Contain("\"registerName\"");
        json.Should().Contain("Test Register");
        _mockHashProvider.Verify(
            h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256),
            Times.Once);
    }

    [Fact]
    public async Task InitiateAsync_WithAdvertiseTrue_ShouldStoreInPending()
    {
        // Arrange
        var request = new InitiateRegisterCreationRequest
        {
            Name = "Public Register",
            TenantId = "tenant-123",
            Owners = new List<OwnerInfo>
            {
                new() { UserId = "user-001", WalletId = "wallet-001" }
            },
            Advertise = true
        };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(new byte[32]);

        // Act
        await _orchestrator.InitiateAsync(request);

        // Assert
        _mockPendingStore.Verify(
            s => s.Add(It.IsAny<string>(), It.Is<PendingRegistration>(p => p.Advertise)),
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
            Owners = new List<OwnerInfo>
            {
                new() { UserId = "user-001", WalletId = "wallet-001" }
            }
        };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(new byte[32]);

        var initiateResponse = await _orchestrator.InitiateAsync(initiateRequest);

        var signedAttestations = initiateResponse.AttestationsToSign.Select(a => new SignedAttestation
        {
            AttestationData = a.AttestationData,
            PublicKey = Convert.ToBase64String(new byte[32]),
            Signature = Convert.ToBase64String(new byte[64]),
            Algorithm = SignatureAlgorithm.ED25519
        }).ToList();

        var finalizeRequest = new FinalizeRegisterCreationRequest
        {
            RegisterId = initiateResponse.RegisterId,
            Nonce = initiateResponse.Nonce,
            SignedAttestations = signedAttestations
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
                It.IsAny<string>(),
                It.IsAny<string?>(),
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
        response.GenesisTransactionId.Should().NotBeNullOrEmpty();
        response.GenesisDocketId.Should().Be("0");

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
            Owners = new List<OwnerInfo>
            {
                new() { UserId = "user-001", WalletId = "wallet-001" }
            }
        };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(new byte[32]);

        var initiateResponse = await _orchestrator.InitiateAsync(initiateRequest);

        var finalizeRequest = new FinalizeRegisterCreationRequest
        {
            RegisterId = initiateResponse.RegisterId,
            Nonce = "invalid-nonce", // Wrong nonce
            SignedAttestations = new List<SignedAttestation>()
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
            SignedAttestations = new List<SignedAttestation>()
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
            Owners = new List<OwnerInfo>
            {
                new() { UserId = "user-001", WalletId = "wallet-001" }
            }
        };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(new byte[32]);

        var initiateResponse = await _orchestrator.InitiateAsync(initiateRequest);

        var signedAttestations = initiateResponse.AttestationsToSign.Select(a => new SignedAttestation
        {
            AttestationData = a.AttestationData,
            PublicKey = Convert.ToBase64String(new byte[32]),
            Signature = Convert.ToBase64String(new byte[64]),
            Algorithm = SignatureAlgorithm.ED25519
        }).ToList();

        var finalizeRequest = new FinalizeRegisterCreationRequest
        {
            RegisterId = initiateResponse.RegisterId,
            Nonce = initiateResponse.Nonce,
            SignedAttestations = signedAttestations
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
            Owners = new List<OwnerInfo>
            {
                new() { UserId = "user-001", WalletId = "wallet-001" }
            }
        };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
            .Returns(new byte[32]);

        var initiateResponse = await _orchestrator.InitiateAsync(initiateRequest);

        var signedAttestations = initiateResponse.AttestationsToSign.Select(a => new SignedAttestation
        {
            AttestationData = a.AttestationData,
            PublicKey = Convert.ToBase64String(new byte[32]),
            Signature = Convert.ToBase64String(new byte[64]),
            Algorithm = inputAlgorithm
        }).ToList();

        var finalizeRequest = new FinalizeRegisterCreationRequest
        {
            RegisterId = initiateResponse.RegisterId,
            Nonce = initiateResponse.Nonce,
            SignedAttestations = signedAttestations
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
                It.IsAny<string>(),
                It.IsAny<string?>(),
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
