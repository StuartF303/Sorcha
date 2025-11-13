// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using FluentAssertions;
using Sorcha.Peer.Service.Integration.Tests.Infrastructure;
using System.Diagnostics;

namespace Sorcha.Peer.Service.Integration.Tests;

/// <summary>
/// Integration tests for peer-to-peer streaming throughput
/// Tests high-volume transaction processing and performance metrics
/// </summary>
[Collection("PeerIntegration")]
public class PeerThroughputTests : IClassFixture<PeerTestFixture>
{
    private readonly PeerTestFixture _fixture;

    public PeerThroughputTests(PeerTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task High_Volume_Transaction_Stream_Should_Maintain_Performance()
    {
        // Arrange
        var peer = _fixture.Peers[0];
        var transactionCount = 1000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        using var call = peer.GrpcClient.StreamTransactions();

        var sendTask = Task.Run(async () =>
        {
            for (int i = 0; i < transactionCount; i++)
            {
                await call.RequestStream.WriteAsync(new TransactionMessage
                {
                    TransactionId = $"perf-{i}",
                    FromPeer = peer.PeerId,
                    ToPeer = "destination",
                    Payload = Google.Protobuf.ByteString.CopyFrom(TestHelpers.CreateRandomPayload(512)),
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                });
            }
            await call.RequestStream.CompleteAsync();
        });

        var responses = new List<TransactionResponse>();
        await foreach (var response in call.ResponseStream.ReadAllAsync())
        {
            responses.Add(response);
        }

        await sendTask;
        stopwatch.Stop();

        // Assert
        responses.Should().HaveCount(transactionCount);
        responses.Should().OnlyContain(r => r.Success);

        var throughput = transactionCount / stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Throughput: {throughput:F2} transactions/second");
        Console.WriteLine($"Total time: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        Console.WriteLine($"Average latency: {stopwatch.Elapsed.TotalMilliseconds / transactionCount:F2} ms");

        // Performance assertion - should handle at least 100 tx/sec
        throughput.Should().BeGreaterThan(100, "System should handle at least 100 transactions per second");
    }

    [Fact]
    public async Task Sustained_Load_Should_Not_Degrade_Performance()
    {
        // Arrange
        var peer = _fixture.Peers[0];
        var batchSize = 100;
        var batchCount = 5;
        var batchThroughputs = new List<double>();

        // Act - Run multiple batches and measure throughput
        for (int batch = 0; batch < batchCount; batch++)
        {
            var stopwatch = Stopwatch.StartNew();

            using var call = peer.GrpcClient.StreamTransactions();

            var sendTask = Task.Run(async () =>
            {
                for (int i = 0; i < batchSize; i++)
                {
                    await call.RequestStream.WriteAsync(new TransactionMessage
                    {
                        TransactionId = $"sustained-batch{batch}-{i}",
                        FromPeer = peer.PeerId,
                        ToPeer = "destination",
                        Payload = Google.Protobuf.ByteString.CopyFrom(TestHelpers.CreateRandomPayload(512)),
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    });
                }
                await call.RequestStream.CompleteAsync();
            });

            var responseCount = 0;
            await foreach (var response in call.ResponseStream.ReadAllAsync())
            {
                responseCount++;
            }

            await sendTask;
            stopwatch.Stop();

            var throughput = batchSize / stopwatch.Elapsed.TotalSeconds;
            batchThroughputs.Add(throughput);

            Console.WriteLine($"Batch {batch + 1}: {throughput:F2} tx/sec");

            // Small delay between batches
            await Task.Delay(100);
        }

        // Assert - Performance should not degrade significantly
        var firstBatchThroughput = batchThroughputs.First();
        var lastBatchThroughput = batchThroughputs.Last();

        Console.WriteLine($"First batch: {firstBatchThroughput:F2} tx/sec");
        Console.WriteLine($"Last batch: {lastBatchThroughput:F2} tx/sec");
        Console.WriteLine($"Performance delta: {((lastBatchThroughput / firstBatchThroughput) * 100):F2}%");

        // Last batch should be at least 70% as fast as first batch
        lastBatchThroughput.Should().BeGreaterThan(firstBatchThroughput * 0.7,
            "Performance should not degrade more than 30% under sustained load");
    }

    [Fact]
    public async Task Large_Payload_Throughput_Test()
    {
        // Arrange
        var peer = _fixture.Peers[0];
        var transactionCount = 100;
        var payloadSize = 10 * 1024; // 10 KB
        var stopwatch = Stopwatch.StartNew();

        // Act
        using var call = peer.GrpcClient.StreamTransactions();

        var sendTask = Task.Run(async () =>
        {
            for (int i = 0; i < transactionCount; i++)
            {
                await call.RequestStream.WriteAsync(new TransactionMessage
                {
                    TransactionId = $"large-{i}",
                    FromPeer = peer.PeerId,
                    ToPeer = "destination",
                    Payload = Google.Protobuf.ByteString.CopyFrom(TestHelpers.CreateRandomPayload(payloadSize)),
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                });
            }
            await call.RequestStream.CompleteAsync();
        });

        var responseCount = 0;
        await foreach (var response in call.ResponseStream.ReadAllAsync())
        {
            responseCount++;
        }

        await sendTask;
        stopwatch.Stop();

        // Assert
        responseCount.Should().Be(transactionCount);

        var totalDataMB = (transactionCount * payloadSize) / (1024.0 * 1024.0);
        var throughputMBps = totalDataMB / stopwatch.Elapsed.TotalSeconds;

        Console.WriteLine($"Total data transferred: {totalDataMB:F2} MB");
        Console.WriteLine($"Time taken: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        Console.WriteLine($"Throughput: {throughputMBps:F2} MB/s");
        Console.WriteLine($"Transaction rate: {transactionCount / stopwatch.Elapsed.TotalSeconds:F2} tx/sec");

        // Should handle at least 1 MB/s
        throughputMBps.Should().BeGreaterThan(1, "System should handle at least 1 MB/s throughput");
    }

    [Fact]
    public async Task Parallel_Peer_Throughput_Test()
    {
        // Arrange
        var peers = _fixture.Peers.Take(3).ToList();
        var transactionsPerPeer = 200;
        var stopwatch = Stopwatch.StartNew();

        // Act - All peers send transactions concurrently
        var tasks = peers.Select(async peer =>
        {
            using var call = peer.GrpcClient.StreamTransactions();
            var responses = new List<TransactionResponse>();

            var sendTask = Task.Run(async () =>
            {
                for (int i = 0; i < transactionsPerPeer; i++)
                {
                    await call.RequestStream.WriteAsync(new TransactionMessage
                    {
                        TransactionId = $"{peer.PeerId}-{i}",
                        FromPeer = peer.PeerId,
                        ToPeer = "destination",
                        Payload = Google.Protobuf.ByteString.CopyFrom(TestHelpers.CreateRandomPayload(512)),
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    });
                }
                await call.RequestStream.CompleteAsync();
            });

            await foreach (var response in call.ResponseStream.ReadAllAsync())
            {
                responses.Add(response);
            }

            await sendTask;
            return responses.Count;
        });

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var totalTransactions = results.Sum();
        var throughput = totalTransactions / stopwatch.Elapsed.TotalSeconds;

        Console.WriteLine($"Total transactions: {totalTransactions}");
        Console.WriteLine($"Time taken: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        Console.WriteLine($"Combined throughput: {throughput:F2} tx/sec");
        Console.WriteLine($"Per-peer throughput: {throughput / peers.Count:F2} tx/sec");

        results.Should().OnlyContain(count => count == transactionsPerPeer);
        throughput.Should().BeGreaterThan(200, "Combined throughput should be at least 200 tx/sec");
    }

    [Fact]
    public async Task Metrics_Should_Track_Throughput_Accurately()
    {
        // Arrange
        var peer = _fixture.Peers[0];
        var transactionCount = 500;

        // Get baseline metrics
        var baselineMetrics = await peer.GrpcClient.GetMetricsAsync(new MetricsRequest());
        var baselineCount = baselineMetrics.TotalTransactions;

        // Act - Send transactions at controlled rate
        var stopwatch = Stopwatch.StartNew();
        using var call = peer.GrpcClient.StreamTransactions();

        for (int i = 0; i < transactionCount; i++)
        {
            await call.RequestStream.WriteAsync(new TransactionMessage
            {
                TransactionId = $"metrics-{i}",
                FromPeer = peer.PeerId,
                ToPeer = "destination",
                Payload = Google.Protobuf.ByteString.CopyFrom(TestHelpers.CreateRandomPayload(256)),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });

            // Read response
            await call.ResponseStream.MoveNext();

            // Small delay to simulate realistic timing
            if (i % 10 == 0)
            {
                await Task.Delay(1);
            }
        }

        await call.RequestStream.CompleteAsync();
        stopwatch.Stop();

        // Wait a moment for metrics to update
        await Task.Delay(100);

        // Get updated metrics
        var updatedMetrics = await peer.GrpcClient.GetMetricsAsync(new MetricsRequest());

        // Assert
        var actualCount = updatedMetrics.TotalTransactions - baselineCount;
        actualCount.Should().Be(transactionCount, "Metrics should accurately track transaction count");

        Console.WriteLine($"Total transactions: {updatedMetrics.TotalTransactions}");
        Console.WriteLine($"Throughput per second: {updatedMetrics.ThroughputPerSecond:F2}");
        Console.WriteLine($"CPU usage: {updatedMetrics.CpuUsagePercent:F2}%");
        Console.WriteLine($"Memory usage: {updatedMetrics.MemoryUsageBytes / (1024.0 * 1024.0):F2} MB");
        Console.WriteLine($"Uptime: {updatedMetrics.UptimeSeconds} seconds");
    }

    [Fact]
    public async Task Burst_Traffic_Should_Be_Handled_Gracefully()
    {
        // Arrange
        var peer = _fixture.Peers[0];
        var burstSize = 500;
        var burstCount = 3;
        var delayBetweenBursts = TimeSpan.FromMilliseconds(500);

        // Act
        var allResponses = new List<TransactionResponse>();
        var stopwatch = Stopwatch.StartNew();

        for (int burst = 0; burst < burstCount; burst++)
        {
            using var call = peer.GrpcClient.StreamTransactions();

            // Send burst
            var sendTask = Task.Run(async () =>
            {
                for (int i = 0; i < burstSize; i++)
                {
                    await call.RequestStream.WriteAsync(new TransactionMessage
                    {
                        TransactionId = $"burst-{burst}-{i}",
                        FromPeer = peer.PeerId,
                        ToPeer = "destination",
                        Payload = Google.Protobuf.ByteString.CopyFrom(TestHelpers.CreateRandomPayload(256)),
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    });
                }
                await call.RequestStream.CompleteAsync();
            });

            await foreach (var response in call.ResponseStream.ReadAllAsync())
            {
                allResponses.Add(response);
            }

            await sendTask;

            Console.WriteLine($"Burst {burst + 1} completed: {burstSize} transactions");

            if (burst < burstCount - 1)
            {
                await Task.Delay(delayBetweenBursts);
            }
        }

        stopwatch.Stop();

        // Assert
        var expectedTotal = burstSize * burstCount;
        allResponses.Should().HaveCount(expectedTotal);
        allResponses.Should().OnlyContain(r => r.Success);

        var overallThroughput = expectedTotal / stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Total transactions: {expectedTotal}");
        Console.WriteLine($"Total time: {stopwatch.Elapsed.TotalSeconds:F2} seconds");
        Console.WriteLine($"Overall throughput: {overallThroughput:F2} tx/sec");
    }

    [Fact]
    public async Task Memory_Usage_Should_Remain_Stable_Under_Load()
    {
        // Arrange
        var peer = _fixture.Peers[0];
        var iterations = 5;
        var transactionsPerIteration = 200;
        var memoryReadings = new List<long>();

        // Act
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            using var call = peer.GrpcClient.StreamTransactions();

            // Send transactions
            for (int i = 0; i < transactionsPerIteration; i++)
            {
                await call.RequestStream.WriteAsync(new TransactionMessage
                {
                    TransactionId = $"mem-{iteration}-{i}",
                    FromPeer = peer.PeerId,
                    ToPeer = "destination",
                    Payload = Google.Protobuf.ByteString.CopyFrom(TestHelpers.CreateRandomPayload(1024)),
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                });

                await call.ResponseStream.MoveNext();
            }

            await call.RequestStream.CompleteAsync();

            // Get memory reading
            var metrics = await peer.GrpcClient.GetMetricsAsync(new MetricsRequest());
            memoryReadings.Add(metrics.MemoryUsageBytes);

            Console.WriteLine($"Iteration {iteration + 1} - Memory: {metrics.MemoryUsageBytes / (1024.0 * 1024.0):F2} MB");

            // Force garbage collection between iterations
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await Task.Delay(100);
        }

        // Assert - Memory should not grow unbounded
        var firstReading = memoryReadings.First();
        var lastReading = memoryReadings.Last();
        var memoryGrowthPercent = ((double)(lastReading - firstReading) / firstReading) * 100;

        Console.WriteLine($"First reading: {firstReading / (1024.0 * 1024.0):F2} MB");
        Console.WriteLine($"Last reading: {lastReading / (1024.0 * 1024.0):F2} MB");
        Console.WriteLine($"Memory growth: {memoryGrowthPercent:F2}%");

        // Memory growth should be reasonable (less than 50% growth)
        memoryGrowthPercent.Should().BeLessThan(50, "Memory usage should remain relatively stable");
    }

    [Fact]
    public async Task Mixed_Payload_Sizes_Should_Be_Handled_Efficiently()
    {
        // Arrange
        var peer = _fixture.Peers[0];
        var transactionCount = 300;
        var payloadSizes = new[] { 128, 512, 1024, 4096, 8192 }; // Bytes
        var stopwatch = Stopwatch.StartNew();

        // Act
        using var call = peer.GrpcClient.StreamTransactions();

        var sendTask = Task.Run(async () =>
        {
            for (int i = 0; i < transactionCount; i++)
            {
                var payloadSize = payloadSizes[i % payloadSizes.Length];
                await call.RequestStream.WriteAsync(new TransactionMessage
                {
                    TransactionId = $"mixed-{i}",
                    FromPeer = peer.PeerId,
                    ToPeer = "destination",
                    Payload = Google.Protobuf.ByteString.CopyFrom(TestHelpers.CreateRandomPayload(payloadSize)),
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                });
            }
            await call.RequestStream.CompleteAsync();
        });

        var responseCount = 0;
        await foreach (var response in call.ResponseStream.ReadAllAsync())
        {
            responseCount++;
        }

        await sendTask;
        stopwatch.Stop();

        // Assert
        responseCount.Should().Be(transactionCount);

        var throughput = transactionCount / stopwatch.Elapsed.TotalSeconds;
        Console.WriteLine($"Mixed payload throughput: {throughput:F2} tx/sec");
        Console.WriteLine($"Total time: {stopwatch.Elapsed.TotalSeconds:F2} seconds");

        throughput.Should().BeGreaterThan(50, "System should handle mixed payloads efficiently");
    }
}
