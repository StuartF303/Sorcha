// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Sorcha.ServiceClients.Peer;
using Sorcha.ServiceClients.Register;
using Sorcha.Validator.Service.Configuration;
using Sorcha.Validator.Service.Models;
using Sorcha.Validator.Service.Services;

namespace Sorcha.Validator.Service.Tests.Services;

public class DocketDistributorTests
{
    private readonly Mock<IPeerServiceClient> _peerClientMock;
    private readonly Mock<IRegisterServiceClient> _registerClientMock;
    private readonly Mock<IOptions<DocketDistributorConfiguration>> _configMock;
    private readonly Mock<ILogger<DocketDistributor>> _loggerMock;
    private readonly DocketDistributorConfiguration _config;
    private readonly DocketDistributor _distributor;

    public DocketDistributorTests()
    {
        _config = new DocketDistributorConfiguration
        {
            BroadcastTimeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3,
            AutoSubmitToRegisterService = true
        };

        _peerClientMock = new Mock<IPeerServiceClient>();
        _registerClientMock = new Mock<IRegisterServiceClient>();
        _configMock = new Mock<IOptions<DocketDistributorConfiguration>>();
        _configMock.Setup(x => x.Value).Returns(_config);
        _loggerMock = new Mock<ILogger<DocketDistributor>>();

        _distributor = new DocketDistributor(
            _peerClientMock.Object,
            _registerClientMock.Object,
            _configMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullPeerClient_ThrowsArgumentNullException()
    {
        var act = () => new DocketDistributor(
            null!,
            _registerClientMock.Object,
            _configMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("peerClient");
    }

    [Fact]
    public void Constructor_NullRegisterClient_ThrowsArgumentNullException()
    {
        var act = () => new DocketDistributor(
            _peerClientMock.Object,
            null!,
            _configMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("registerClient");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DocketDistributor(
            _peerClientMock.Object,
            _registerClientMock.Object,
            _configMock.Object,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region BroadcastProposedDocketAsync Tests

    [Fact]
    public async Task BroadcastProposedDocketAsync_ValidDocket_BroadcastsToPeers()
    {
        // Arrange
        var docket = CreateValidDocket();
        var validators = new List<ValidatorInfo>
        {
            new() { ValidatorId = "val-1", GrpcEndpoint = "http://val1:5000" },
            new() { ValidatorId = "val-2", GrpcEndpoint = "http://val2:5000" }
        };

        _peerClientMock.Setup(x => x.QueryValidatorsAsync(docket.RegisterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validators);

        // Act
        var peerCount = await _distributor.BroadcastProposedDocketAsync(docket);

        // Assert
        peerCount.Should().Be(2);
        _peerClientMock.Verify(
            x => x.PublishProposedDocketAsync(
                docket.RegisterId,
                docket.DocketId,
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BroadcastProposedDocketAsync_NoPeers_ReturnsZero()
    {
        // Arrange
        var docket = CreateValidDocket();
        _peerClientMock.Setup(x => x.QueryValidatorsAsync(docket.RegisterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ValidatorInfo>());

        // Act
        var peerCount = await _distributor.BroadcastProposedDocketAsync(docket);

        // Assert
        peerCount.Should().Be(0);
    }

    [Fact]
    public async Task BroadcastProposedDocketAsync_NullDocket_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _distributor.BroadcastProposedDocketAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region BroadcastConfirmedDocketAsync Tests

    [Fact]
    public async Task BroadcastConfirmedDocketAsync_ValidDocket_BroadcastsToPeers()
    {
        // Arrange
        var docket = CreateConfirmedDocket();
        var validators = new List<ValidatorInfo>
        {
            new() { ValidatorId = "val-1", GrpcEndpoint = "http://val1:5000" },
            new() { ValidatorId = "val-2", GrpcEndpoint = "http://val2:5000" }
        };

        _peerClientMock.Setup(x => x.QueryValidatorsAsync(docket.RegisterId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validators);

        // Act
        var peerCount = await _distributor.BroadcastConfirmedDocketAsync(docket);

        // Assert
        peerCount.Should().Be(2);
        _peerClientMock.Verify(
            x => x.BroadcastConfirmedDocketAsync(
                docket.RegisterId,
                docket.DocketId,
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region SubmitToRegisterServiceAsync Tests

    [Fact]
    public async Task SubmitToRegisterServiceAsync_ValidDocket_ReturnsTrue()
    {
        // Arrange
        var docket = CreateConfirmedDocket();
        _registerClientMock.Setup(x => x.WriteDocketAsync(It.IsAny<DocketModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _distributor.SubmitToRegisterServiceAsync(docket);

        // Assert
        result.Should().BeTrue();
        _registerClientMock.Verify(
            x => x.WriteDocketAsync(It.IsAny<DocketModel>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SubmitToRegisterServiceAsync_RegisterServiceRejects_ReturnsFalse()
    {
        // Arrange
        var docket = CreateConfirmedDocket();
        _registerClientMock.Setup(x => x.WriteDocketAsync(It.IsAny<DocketModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _distributor.SubmitToRegisterServiceAsync(docket);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitToRegisterServiceAsync_Exception_ReturnsFalse()
    {
        // Arrange
        var docket = CreateConfirmedDocket();
        _registerClientMock.Setup(x => x.WriteDocketAsync(It.IsAny<DocketModel>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection failed"));

        // Act
        var result = await _distributor.SubmitToRegisterServiceAsync(docket);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetStats Tests

    [Fact]
    public void GetStats_InitialState_ReturnsZeroCounts()
    {
        // Act
        var stats = _distributor.GetStats();

        // Assert
        stats.TotalProposedBroadcasts.Should().Be(0);
        stats.TotalConfirmedBroadcasts.Should().Be(0);
        stats.TotalRegisterSubmissions.Should().Be(0);
        stats.FailedRegisterSubmissions.Should().Be(0);
    }

    [Fact]
    public async Task GetStats_AfterBroadcasts_TracksCorrectly()
    {
        // Arrange
        var docket = CreateValidDocket();
        var validators = new List<ValidatorInfo>
        {
            new() { ValidatorId = "val-1", GrpcEndpoint = "http://val1:5000" }
        };

        _peerClientMock.Setup(x => x.QueryValidatorsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validators);

        _registerClientMock.Setup(x => x.WriteDocketAsync(It.IsAny<DocketModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _distributor.BroadcastProposedDocketAsync(docket);
        await _distributor.BroadcastConfirmedDocketAsync(CreateConfirmedDocket());
        await _distributor.SubmitToRegisterServiceAsync(CreateConfirmedDocket());

        var stats = _distributor.GetStats();

        // Assert
        stats.TotalProposedBroadcasts.Should().Be(1);
        stats.TotalConfirmedBroadcasts.Should().Be(1);
        stats.TotalRegisterSubmissions.Should().Be(1);
    }

    #endregion

    #region Helper Methods

    private static Docket CreateValidDocket(
        string? docketId = null,
        string? registerId = null,
        long docketNumber = 1)
    {
        return new Docket
        {
            DocketId = docketId ?? $"docket-{Guid.NewGuid()}",
            RegisterId = registerId ?? "register-1",
            DocketNumber = docketNumber,
            DocketHash = "hash-abc123",
            PreviousHash = docketNumber > 0 ? "prev-hash" : null,
            MerkleRoot = "merkle-root",
            CreatedAt = DateTimeOffset.UtcNow,
            ProposerValidatorId = "validator-1",
            Status = DocketStatus.Proposed,
            ProposerSignature = new Signature
            {
                PublicKey = new byte[] { 1, 2, 3 },
                SignatureValue = new byte[] { 4, 5, 6 },
                Algorithm = "ED25519",
                SignedAt = DateTimeOffset.UtcNow
            },
            Transactions = new List<Transaction>()
        };
    }

    private static Docket CreateConfirmedDocket()
    {
        var docket = CreateValidDocket();
        return new Docket
        {
            DocketId = docket.DocketId,
            RegisterId = docket.RegisterId,
            DocketNumber = docket.DocketNumber,
            DocketHash = docket.DocketHash,
            PreviousHash = docket.PreviousHash,
            MerkleRoot = docket.MerkleRoot,
            CreatedAt = docket.CreatedAt,
            ProposerValidatorId = docket.ProposerValidatorId,
            Status = DocketStatus.Confirmed,
            ProposerSignature = docket.ProposerSignature,
            Transactions = docket.Transactions,
            Votes = new List<ConsensusVote>
            {
                new()
                {
                    VoteId = "vote-1",
                    DocketId = docket.DocketId,
                    ValidatorId = "val-1",
                    Decision = VoteDecision.Approve,
                    VotedAt = DateTimeOffset.UtcNow,
                    DocketHash = docket.DocketHash,
                    ValidatorSignature = new Signature
                    {
                        PublicKey = new byte[] { 1, 2, 3 },
                        SignatureValue = new byte[] { 4, 5, 6 },
                        Algorithm = "ED25519",
                        SignedAt = DateTimeOffset.UtcNow
                    }
                }
            }
        };
    }

    #endregion
}
