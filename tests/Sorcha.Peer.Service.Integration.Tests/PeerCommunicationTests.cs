// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Grpc.Core;
using Sorcha.Peer.Service.Integration.Tests.Infrastructure;
using System.Diagnostics;

namespace Sorcha.Peer.Service.Integration.Tests;

/// <summary>
/// Integration tests for simple peer-to-peer communication
/// Tests basic message exchange between peers
/// </summary>
[Collection("PeerIntegration")]
public class PeerCommunicationTests : IClassFixture<PeerTestFixture>
{
    private readonly PeerTestFixture _fixture;

    public PeerCommunicationTests(PeerTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Single_Transaction_Should_Be_Processed_Successfully()
    {
        // Arrange
        var peer1 = _fixture.Peers[0];
        var peer2 = _fixture.Peers[1];

        var transactionId = TestHelpers.GenerateTransactionId();
        var payload = TestHelpers.CreateRandomPayload(512);

        // Act
        using var call = peer1.GrpcClient.StreamTransactions();

        var transaction = new TransactionMessage
        {
            TransactionId = transactionId,
            FromPeer = peer1.PeerId,
            ToPeer = peer2.PeerId,
            Payload = Google.Protobuf.ByteString.CopyFrom(payload),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        await call.RequestStream.WriteAsync(transaction);
        await call.RequestStream.CompleteAsync();

        var response = await call.ResponseStream.ReadAllAsync().FirstAsync();

        // Assert
        response.Should().NotBeNull();
        response.TransactionId.Should().Be(transactionId);
        response.Success.Should().BeTrue();
        response.Message.Should().Be("Transaction processed successfully");
        response.ProcessedAt.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Multiple_Sequential_Transactions_Should_Be_Processed()
    {
        // Arrange
        var peer = _fixture.Peers[0];
        var transactionCount = 10;
        var responses = new List<TransactionResponse>();

        // Act
        using var call = peer.GrpcClient.StreamTransactions();

        // Send multiple transactions
        for (int i = 0; i < transactionCount; i++)
        {
            var transaction = new TransactionMessage
            {
                TransactionId = TestHelpers.GenerateTransactionId(),
                FromPeer = peer.PeerId,
                ToPeer = "destination-peer",
                Payload = Google.Protobuf.ByteString.CopyFrom(TestHelpers.CreateRandomPayload(256)),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            await call.RequestStream.WriteAsync(transaction);

            // Read response
            if (await call.ResponseStream.MoveNext())
            {
                responses.Add(call.ResponseStream.Current);
            }
        }

        await call.RequestStream.CompleteAsync();

        // Read any remaining responses
        await foreach (var response in call.ResponseStream.ReadAllAsync())
        {
            responses.Add(response);
        }

        // Assert
        responses.Should().HaveCount(transactionCount);
        responses.Should().OnlyContain(r => r.Success);
        responses.Select(r => r.TransactionId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Transactions_Between_Different_Peers_Should_Work()
    {
        // Arrange
        var peer1 = _fixture.Peers[0];
        var peer2 = _fixture.Peers[1];
        var peer3 = _fixture.Peers[2];

        var transactionId1 = TestHelpers.GenerateTransactionId();
        var transactionId2 = TestHelpers.GenerateTransactionId();

        // Act - Peer 1 sends to Peer 2
        using var call1 = peer1.GrpcClient.StreamTransactions();
        await call1.RequestStream.WriteAsync(new TransactionMessage
        {
            TransactionId = transactionId1,
            FromPeer = peer1.PeerId,
            ToPeer = peer2.PeerId,
            Payload = Google.Protobuf.ByteString.CopyFrom(TestHelpers.CreateRandomPayload(128)),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });
        await call1.RequestStream.CompleteAsync();
        var response1 = await call1.ResponseStream.ReadAllAsync().FirstAsync();

        // Act - Peer 2 sends to Peer 3
        using var call2 = peer2.GrpcClient.StreamTransactions();
        await call2.RequestStream.WriteAsync(new TransactionMessage
        {
            TransactionId = transactionId2,
            FromPeer = peer2.PeerId,
            ToPeer = peer3.PeerId,
            Payload = Google.Protobuf.ByteString.CopyFrom(TestHelpers.CreateRandomPayload(128)),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        });
        await call2.RequestStream.CompleteAsync();
        var response2 = await call2.ResponseStream.ReadAllAsync().FirstAsync();

        // Assert
        response1.Should().NotBeNull();
        response1.Success.Should().BeTrue();
        response1.TransactionId.Should().Be(transactionId1);

        response2.Should().NotBeNull();
        response2.Success.Should().BeTrue();
        response2.TransactionId.Should().Be(transactionId2);
    }

    [Fact]
    public async Task Large_Payload_Transaction_Should_Be_Handled()
    {
        // Arrange
        var peer = _fixture.Peers[0];
        var transactionId = TestHelpers.GenerateTransactionId();
        var largePayload = TestHelpers.CreateRandomPayload(1024 * 1024); // 1 MB

        // Act
        using var call = peer.GrpcClient.StreamTransactions();

        var transaction = new TransactionMessage
        {
            TransactionId = transactionId,
            FromPeer = peer.PeerId,
            ToPeer = "destination-peer",
            Payload = Google.Protobuf.ByteString.CopyFrom(largePayload),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        await call.RequestStream.WriteAsync(transaction);
        await call.RequestStream.CompleteAsync();

        var response = await call.ResponseStream.ReadAllAsync().FirstAsync();

        // Assert
        response.Should().NotBeNull();
        response.Success.Should().BeTrue();
        response.TransactionId.Should().Be(transactionId);
    }

    [Fact]
    public async Task Transaction_Processing_Should_Update_Metrics()
    {
        // Arrange
        var peer = _fixture.Peers[0];

        // Get initial metrics
        var initialMetrics = await peer.GrpcClient.GetMetricsAsync(new MetricsRequest());
        var initialCount = initialMetrics.TotalTransactions;

        // Act - Send 5 transactions
        using var call = peer.GrpcClient.StreamTransactions();
        for (int i = 0; i < 5; i++)
        {
            await call.RequestStream.WriteAsync(new TransactionMessage
            {
                TransactionId = TestHelpers.GenerateTransactionId(),
                FromPeer = peer.PeerId,
                ToPeer = "test-peer",
                Payload = Google.Protobuf.ByteString.CopyFrom(TestHelpers.CreateRandomPayload(128)),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });

            // Read response
            await call.ResponseStream.MoveNext();
        }
        await call.RequestStream.CompleteAsync();

        // Get updated metrics
        var updatedMetrics = await peer.GrpcClient.GetMetricsAsync(new MetricsRequest());

        // Assert
        updatedMetrics.TotalTransactions.Should().Be(initialCount + 5);
    }

    [Fact]
    public async Task Bidirectional_Stream_Should_Work_Correctly()
    {
        // Arrange
        var peer = _fixture.Peers[0];
        var transactionCount = 20;
        var responses = new List<TransactionResponse>();
        var responseTask = Task.Run(async () =>
        {
            using var call = peer.GrpcClient.StreamTransactions();

            // Start reading responses in background
            var readTask = Task.Run(async () =>
            {
                await foreach (var response in call.ResponseStream.ReadAllAsync())
                {
                    responses.Add(response);
                }
            });

            // Send transactions
            for (int i = 0; i < transactionCount; i++)
            {
                await call.RequestStream.WriteAsync(new TransactionMessage
                {
                    TransactionId = $"bidir-{i}",
                    FromPeer = peer.PeerId,
                    ToPeer = "destination",
                    Payload = Google.Protobuf.ByteString.CopyFrom(TestHelpers.CreateRandomPayload(128)),
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                });

                await Task.Delay(10); // Small delay between sends
            }

            await call.RequestStream.CompleteAsync();
            await readTask;
        });

        // Act
        await responseTask.WaitAsync(TimeSpan.FromSeconds(10));

        // Assert
        responses.Should().HaveCount(transactionCount);
        responses.Should().OnlyContain(r => r.Success);
    }

    [Fact]
    public async Task Concurrent_Streams_From_Same_Peer_Should_Work()
    {
        // Arrange
        var peer = _fixture.Peers[0];
        var streamsCount = 5;
        var transactionsPerStream = 10;

        // Act
        var tasks = Enumerable.Range(0, streamsCount).Select(async streamId =>
        {
            using var call = peer.GrpcClient.StreamTransactions();
            var responses = new List<TransactionResponse>();

            for (int i = 0; i < transactionsPerStream; i++)
            {
                await call.RequestStream.WriteAsync(new TransactionMessage
                {
                    TransactionId = $"stream-{streamId}-txn-{i}",
                    FromPeer = peer.PeerId,
                    ToPeer = "destination",
                    Payload = Google.Protobuf.ByteString.CopyFrom(TestHelpers.CreateRandomPayload(128)),
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                });

                if (await call.ResponseStream.MoveNext())
                {
                    responses.Add(call.ResponseStream.Current);
                }
            }

            await call.RequestStream.CompleteAsync();

            // Read remaining responses
            await foreach (var response in call.ResponseStream.ReadAllAsync())
            {
                responses.Add(response);
            }

            return responses;
        });

        var allResponses = await Task.WhenAll(tasks);

        // Assert
        allResponses.Should().HaveCount(streamsCount);
        allResponses.Should().OnlyContain(responses => responses.Count == transactionsPerStream);

        var totalResponses = allResponses.SelectMany(r => r).ToList();
        totalResponses.Should().HaveCount(streamsCount * transactionsPerStream);
        totalResponses.Should().OnlyContain(r => r.Success);
    }

    [Fact]
    public async Task Empty_Stream_Should_Complete_Successfully()
    {
        // Arrange
        var peer = _fixture.Peers[0];

        // Act
        using var call = peer.GrpcClient.StreamTransactions();
        await call.RequestStream.CompleteAsync();

        var responses = await call.ResponseStream.ReadAllAsync().ToListAsync();

        // Assert
        responses.Should().BeEmpty();
    }

    [Fact]
    public async Task Transaction_Timestamp_Should_Be_Preserved()
    {
        // Arrange
        var peer = _fixture.Peers[0];
        var sentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var transactionId = TestHelpers.GenerateTransactionId();

        // Act
        using var call = peer.GrpcClient.StreamTransactions();
        await call.RequestStream.WriteAsync(new TransactionMessage
        {
            TransactionId = transactionId,
            FromPeer = peer.PeerId,
            ToPeer = "destination",
            Payload = Google.Protobuf.ByteString.CopyFrom(TestHelpers.CreateRandomPayload(128)),
            Timestamp = sentTimestamp
        });
        await call.RequestStream.CompleteAsync();

        var response = await call.ResponseStream.ReadAllAsync().FirstAsync();

        // Assert
        response.ProcessedAt.Should().BeGreaterOrEqualTo(sentTimestamp);
        response.ProcessedAt.Should().BeLessThan(sentTimestamp + 10); // Within 10 seconds
    }
}
