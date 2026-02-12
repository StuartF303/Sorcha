// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.Options;
using Sorcha.Cryptography.Interfaces;
using Sorcha.Cryptography.Enums;
using Sorcha.Cryptography.Utilities;
using Sorcha.ServiceClients.Register;
using Sorcha.ServiceClients.Wallet;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Models;
using Sorcha.Validator.Service.Services;

namespace Sorcha.Validator.Service.Tests.Services;

/// <summary>
/// Unit tests for GenesisManager — genesis docket creation and needs-genesis detection
/// </summary>
public class GenesisManagerTests
{
    private readonly Mock<IRegisterServiceClient> _mockRegisterClient;
    private readonly Mock<IWalletServiceClient> _mockWalletClient;
    private readonly Mock<ISystemWalletProvider> _mockSystemWalletProvider;
    private readonly Mock<IHashProvider> _mockHashProvider;
    private readonly MerkleTree _merkleTree;
    private readonly DocketHasher _docketHasher;
    private readonly ValidatorConfiguration _validatorConfig;
    private readonly Mock<ILogger<GenesisManager>> _mockLogger;
    private readonly GenesisManager _genesisManager;

    public GenesisManagerTests()
    {
        _mockRegisterClient = new Mock<IRegisterServiceClient>();
        _mockWalletClient = new Mock<IWalletServiceClient>();
        _mockSystemWalletProvider = new Mock<ISystemWalletProvider>();
        _mockHashProvider = new Mock<IHashProvider>();
        _mockLogger = new Mock<ILogger<GenesisManager>>();

        _merkleTree = new MerkleTree(_mockHashProvider.Object);
        _docketHasher = new DocketHasher(_mockHashProvider.Object);

        _validatorConfig = new ValidatorConfiguration
        {
            ValidatorId = "validator-1",
            SystemWalletAddress = "system-wallet-1"
        };

        _mockHashProvider
            .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), It.IsAny<HashType>()))
            .Returns((byte[] data, HashType _) =>
                System.Text.Encoding.UTF8.GetBytes($"hash-{data.Length}"));

        _mockSystemWalletProvider
            .Setup(p => p.GetSystemWalletId())
            .Returns("system-wallet-1");

        _mockWalletClient
            .Setup(w => w.SignDataAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WalletSignResult
            {
                Signature = System.Text.Encoding.UTF8.GetBytes("sig"),
                PublicKey = System.Text.Encoding.UTF8.GetBytes("pub"),
                SignedBy = "system-wallet-1",
                Algorithm = "ED25519"
            });

        _genesisManager = new GenesisManager(
            _mockRegisterClient.Object,
            _mockWalletClient.Object,
            _mockSystemWalletProvider.Object,
            _merkleTree,
            _docketHasher,
            Options.Create(_validatorConfig),
            _mockLogger.Object);
    }

    #region NeedsGenesisDocketAsync Tests

    [Fact]
    public async Task NeedsGenesisDocketAsync_WhenHeightIsZero_ReturnsTrue()
    {
        // Arrange
        _mockRegisterClient
            .Setup(r => r.GetRegisterHeightAsync("register-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);

        // Act
        var result = await _genesisManager.NeedsGenesisDocketAsync("register-1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task NeedsGenesisDocketAsync_WhenHeightIsPositive_ReturnsFalse()
    {
        // Arrange
        _mockRegisterClient
            .Setup(r => r.GetRegisterHeightAsync("register-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        // Act
        var result = await _genesisManager.NeedsGenesisDocketAsync("register-1");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task NeedsGenesisDocketAsync_WhenHeightIsNegative_ReturnsTrue()
    {
        // Arrange — height < 0 means register not found or error
        _mockRegisterClient
            .Setup(r => r.GetRegisterHeightAsync("register-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(-1L);

        // Act
        var result = await _genesisManager.NeedsGenesisDocketAsync("register-1");

        // Assert — treat as needs genesis rather than building a non-genesis Docket 0
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(0L, true)]
    [InlineData(1L, false)]
    [InlineData(5L, false)]
    [InlineData(-1L, true)]
    [InlineData(-100L, true)]
    public async Task NeedsGenesisDocketAsync_VariousHeights_ReturnsExpected(long height, bool expected)
    {
        // Arrange
        _mockRegisterClient
            .Setup(r => r.GetRegisterHeightAsync("register-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(height);

        // Act
        var result = await _genesisManager.NeedsGenesisDocketAsync("register-1");

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region CreateGenesisDocketAsync Tests

    [Fact]
    public async Task CreateGenesisDocketAsync_WithTransactions_CreatesDocketNumberZero()
    {
        // Arrange
        var transactions = new List<Transaction>
        {
            new Transaction
            {
                TransactionId = "tx-1",
                RegisterId = "register-1",
                BlueprintId = "blueprint-1",
                ActionId = "action-1",
                PayloadHash = "payload-hash",
                CreatedAt = DateTimeOffset.UtcNow,
                Payload = System.Text.Json.JsonDocument.Parse("{}").RootElement,
                Signatures = []
            }
        };

        // Act
        var docket = await _genesisManager.CreateGenesisDocketAsync("register-1", transactions);

        // Assert
        docket.DocketNumber.Should().Be(0);
        docket.PreviousHash.Should().BeNull();
        docket.RegisterId.Should().Be("register-1");
        docket.Transactions.Should().HaveCount(1);
        docket.Status.Should().Be(DocketStatus.Proposed);
    }

    [Fact]
    public async Task CreateGenesisDocketAsync_WithNoTransactions_CreatesDocketNumberZero()
    {
        // Act
        var docket = await _genesisManager.CreateGenesisDocketAsync("register-1", []);

        // Assert
        docket.DocketNumber.Should().Be(0);
        docket.PreviousHash.Should().BeNull();
        docket.Transactions.Should().BeEmpty();
    }

    #endregion
}
