// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text;
using System.Text.Json;

namespace Sorcha.Peer.Service.Integration.Tests.Infrastructure;

/// <summary>
/// Helper utilities for integration tests
/// </summary>
public static class TestHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Serializes an object to JSON StringContent
    /// </summary>
    public static StringContent ToJsonContent<T>(this T obj)
    {
        var json = JsonSerializer.Serialize(obj);
        return new StringContent(json, Encoding.UTF8, "application/json");
    }

    /// <summary>
    /// Deserializes JSON response to object
    /// </summary>
    public static async Task<T?> DeserializeAsync<T>(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    /// <summary>
    /// Creates a random transaction payload
    /// </summary>
    public static byte[] CreateRandomPayload(int sizeInBytes = 1024)
    {
        var payload = new byte[sizeInBytes];
        Random.Shared.NextBytes(payload);
        return payload;
    }

    /// <summary>
    /// Generates a unique transaction ID
    /// </summary>
    public static string GenerateTransactionId()
    {
        return $"txn-{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Waits for a condition to be true with timeout
    /// </summary>
    public static async Task<bool> WaitForConditionAsync(
        Func<bool> condition,
        TimeSpan timeout,
        TimeSpan? pollingInterval = null)
    {
        pollingInterval ??= TimeSpan.FromMilliseconds(100);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            if (condition())
            {
                return true;
            }

            await Task.Delay(pollingInterval.Value);
        }

        return false;
    }

    /// <summary>
    /// Waits for an async condition to be true with timeout
    /// </summary>
    public static async Task<bool> WaitForConditionAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout,
        TimeSpan? pollingInterval = null)
    {
        pollingInterval ??= TimeSpan.FromMilliseconds(100);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            if (await condition())
            {
                return true;
            }

            await Task.Delay(pollingInterval.Value);
        }

        return false;
    }
}
