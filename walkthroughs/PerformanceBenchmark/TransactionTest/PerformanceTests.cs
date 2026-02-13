// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

namespace TransactionTest;

/// <summary>
/// Performance testing suite for transaction submission
/// Measures throughput, latency, concurrency, and payload size impact
/// </summary>
public class PerformanceTests
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _token;
    private readonly string _registerId;
    private readonly string _walletAddress;

    public PerformanceTests(HttpClient httpClient, string baseUrl, string token, string registerId, string walletAddress)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl;
        _token = token;
        _registerId = registerId;
        _walletAddress = walletAddress;
    }

    /// <summary>
    /// Test throughput (transactions per second)
    /// </summary>
    public async Task<ThroughputResult> RunThroughputTestAsync(int durationSeconds = 30)
    {
        Console.WriteLine($"\n🔥 Throughput Test ({durationSeconds}s)");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        var results = new ConcurrentBag<(bool success, long elapsedMs)>();
        var stopwatch = Stopwatch.StartNew();
        var endTime = stopwatch.Elapsed.Add(TimeSpan.FromSeconds(durationSeconds));

        int successCount = 0;
        int failureCount = 0;
        var latencies = new ConcurrentBag<long>();

        // Run continuous submissions for duration
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++) // 10 concurrent workers
        {
            tasks.Add(Task.Run(async () =>
            {
                int sequence = 0;
                while (stopwatch.Elapsed < endTime)
                {
                    var taskStopwatch = Stopwatch.StartNew();
                    try
                    {
                        var success = await SubmitTestTransactionAsync($"throughput-test-{i}-{sequence++}");
                        taskStopwatch.Stop();

                        if (success)
                        {
                            Interlocked.Increment(ref successCount);
                            latencies.Add(taskStopwatch.ElapsedMilliseconds);
                        }
                        else
                        {
                            Interlocked.Increment(ref failureCount);
                        }
                    }
                    catch
                    {
                        Interlocked.Increment(ref failureCount);
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        var latencyArray = latencies.OrderBy(x => x).ToArray();

        return new ThroughputResult
        {
            DurationSeconds = stopwatch.Elapsed.TotalSeconds,
            TotalTransactions = successCount + failureCount,
            SuccessCount = successCount,
            FailureCount = failureCount,
            TransactionsPerSecond = successCount / stopwatch.Elapsed.TotalSeconds,
            LatencyP50 = GetPercentile(latencyArray, 50),
            LatencyP95 = GetPercentile(latencyArray, 95),
            LatencyP99 = GetPercentile(latencyArray, 99),
            LatencyMin = latencyArray.Length > 0 ? latencyArray[0] : 0,
            LatencyMax = latencyArray.Length > 0 ? latencyArray[^1] : 0,
            LatencyMean = latencyArray.Length > 0 ? latencyArray.Average() : 0
        };
    }

    /// <summary>
    /// Test latency distribution with varying load
    /// </summary>
    public async Task<LatencyResult> RunLatencyTestAsync(int transactionCount = 100)
    {
        Console.WriteLine($"\n📊 Latency Test ({transactionCount} transactions)");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        var latencies = new List<long>();
        int successCount = 0;

        for (int i = 0; i < transactionCount; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var success = await SubmitTestTransactionAsync($"latency-test-{i}");
                stopwatch.Stop();

                if (success)
                {
                    successCount++;
                    latencies.Add(stopwatch.ElapsedMilliseconds);
                }

                if ((i + 1) % 10 == 0)
                {
                    Console.Write($"\rProgress: {i + 1}/{transactionCount}");
                }
            }
            catch
            {
                // Continue on error
            }
        }

        Console.WriteLine();

        var latencyArray = latencies.OrderBy(x => x).ToArray();

        return new LatencyResult
        {
            TotalTransactions = transactionCount,
            SuccessCount = successCount,
            P50 = GetPercentile(latencyArray, 50),
            P75 = GetPercentile(latencyArray, 75),
            P90 = GetPercentile(latencyArray, 90),
            P95 = GetPercentile(latencyArray, 95),
            P99 = GetPercentile(latencyArray, 99),
            Min = latencyArray.Length > 0 ? latencyArray[0] : 0,
            Max = latencyArray.Length > 0 ? latencyArray[^1] : 0,
            Mean = latencyArray.Length > 0 ? latencyArray.Average() : 0,
            StdDev = latencyArray.Length > 0 ? CalculateStdDev(latencyArray) : 0
        };
    }

    /// <summary>
    /// Test concurrency scaling
    /// </summary>
    public async Task<ConcurrencyResult> RunConcurrencyTestAsync(int maxConcurrency = 50)
    {
        Console.WriteLine($"\n⚡ Concurrency Test (up to {maxConcurrency} concurrent)");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        var results = new List<(int concurrency, double tps, double latencyMs)>();

        for (int concurrency = 1; concurrency <= maxConcurrency; concurrency *= 2)
        {
            var latencies = new ConcurrentBag<long>();
            var stopwatch = Stopwatch.StartNew();
            int successCount = 0;

            var tasks = Enumerable.Range(0, concurrency).Select(async i =>
            {
                var taskStopwatch = Stopwatch.StartNew();
                var success = await SubmitTestTransactionAsync($"concurrency-{concurrency}-{i}");
                taskStopwatch.Stop();

                if (success)
                {
                    Interlocked.Increment(ref successCount);
                    latencies.Add(taskStopwatch.ElapsedMilliseconds);
                }
            });

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            var tps = successCount / stopwatch.Elapsed.TotalSeconds;
            var avgLatency = latencies.Any() ? latencies.Average() : 0;

            results.Add((concurrency, tps, avgLatency));

            Console.WriteLine($"  Concurrency {concurrency,3}: {tps,6:F1} TPS, {avgLatency,6:F1}ms avg latency");
        }

        return new ConcurrencyResult
        {
            Results = results
        };
    }

    /// <summary>
    /// Test payload size impact on performance
    /// </summary>
    public async Task<PayloadSizeResult> RunPayloadSizeTestAsync()
    {
        Console.WriteLine($"\n📦 Payload Size Test");
        Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        var payloadSizes = new[] { 100, 500, 1024, 5 * 1024, 10 * 1024, 50 * 1024, 100 * 1024 };
        var results = new List<(int size, double avgLatency, double throughput)>();

        foreach (var size in payloadSizes)
        {
            var latencies = new List<long>();
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < 10; i++)
            {
                var taskStopwatch = Stopwatch.StartNew();
                var success = await SubmitTestTransactionAsync($"size-test-{size}-{i}", size);
                taskStopwatch.Stop();

                if (success)
                {
                    latencies.Add(taskStopwatch.ElapsedMilliseconds);
                }
            }

            stopwatch.Stop();

            var avgLatency = latencies.Any() ? latencies.Average() : 0;
            var throughput = latencies.Count / stopwatch.Elapsed.TotalSeconds;

            results.Add((size, avgLatency, throughput));

            Console.WriteLine($"  {FormatBytes(size),8}: {avgLatency,6:F1}ms avg, {throughput,5:F1} TPS");
        }

        return new PayloadSizeResult
        {
            Results = results
        };
    }

    private async Task<bool> SubmitTestTransactionAsync(string testId, int payloadSizeBytes = 100)
    {
        // Create test payload with specified size
        var testData = new string('X', Math.Max(1, payloadSizeBytes - 50)); // Rough size target
        var payload = new Dictionary<string, object>
        {
            ["testData"] = testData,
            ["timestamp"] = DateTimeOffset.UtcNow.ToString("o"),
            ["sequence"] = 1,
            ["testId"] = testId
        };

        var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        var hashBytes = SHA256.HashData(payloadBytes);
        var payloadHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var txIdSource = $"{_registerId}-{DateTimeOffset.UtcNow:o}-{testId}";
        var txIdBytes = SHA256.HashData(Encoding.UTF8.GetBytes(txIdSource));
        var transactionId = Convert.ToHexString(txIdBytes).ToLowerInvariant();

        var signRequest = new
        {
            transactionData = Convert.ToBase64String(txIdBytes),
            isPreHashed = true
        };

        var json = JsonSerializer.Serialize(signRequest);
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/v1/wallets/{_walletAddress}/sign")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
            Headers = { { "Authorization", $"Bearer {_token}" } }
        };

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return false;

        var responseJson = await response.Content.ReadAsStringAsync();
        var signDoc = JsonDocument.Parse(responseJson);

        var payloadElement = JsonSerializer.Deserialize<JsonElement>(payloadJson);

        var validateRequest = new
        {
            transactionId = transactionId,
            registerId = _registerId,
            blueprintId = "performance-test-v1",
            actionId = "1",
            payload = payloadElement,
            payloadHash = payloadHash,
            signatures = new[]
            {
                new
                {
                    publicKey = signDoc.RootElement.GetProperty("publicKey").GetString(),
                    signatureValue = signDoc.RootElement.GetProperty("signature").GetString(),
                    algorithm = "ED25519"
                }
            },
            createdAt = DateTimeOffset.UtcNow,
            expiresAt = DateTimeOffset.UtcNow.AddMinutes(5),
            priority = 1,
            metadata = new Dictionary<string, string>
            {
                ["source"] = "performance-test",
                ["testId"] = testId
            }
        };

        json = JsonSerializer.Serialize(validateRequest, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/validator/transactions/validate")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
            Headers = { { "Authorization", $"Bearer {_token}" } }
        };

        response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    private static double GetPercentile(long[] sortedData, int percentile)
    {
        if (sortedData.Length == 0) return 0;
        var index = (int)Math.Ceiling(percentile / 100.0 * sortedData.Length) - 1;
        return sortedData[Math.Max(0, Math.Min(index, sortedData.Length - 1))];
    }

    private static double CalculateStdDev(long[] data)
    {
        var mean = data.Average();
        var sumOfSquares = data.Sum(x => Math.Pow(x - mean, 2));
        return Math.Sqrt(sumOfSquares / data.Length);
    }

    private static string FormatBytes(int bytes)
    {
        if (bytes < 1024) return $"{bytes}B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024}KB";
        return $"{bytes / (1024 * 1024)}MB";
    }
}

public record ThroughputResult
{
    public double DurationSeconds { get; init; }
    public int TotalTransactions { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public double TransactionsPerSecond { get; init; }
    public double LatencyP50 { get; init; }
    public double LatencyP95 { get; init; }
    public double LatencyP99 { get; init; }
    public double LatencyMin { get; init; }
    public double LatencyMax { get; init; }
    public double LatencyMean { get; init; }
}

public record LatencyResult
{
    public int TotalTransactions { get; init; }
    public int SuccessCount { get; init; }
    public double P50 { get; init; }
    public double P75 { get; init; }
    public double P90 { get; init; }
    public double P95 { get; init; }
    public double P99 { get; init; }
    public double Min { get; init; }
    public double Max { get; init; }
    public double Mean { get; init; }
    public double StdDev { get; init; }
}

public record ConcurrencyResult
{
    public List<(int concurrency, double tps, double latencyMs)> Results { get; init; } = new();
}

public record PayloadSizeResult
{
    public List<(int size, double avgLatency, double throughput)> Results { get; init; } = new();
}
