// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net.Http.Json;
using System.Text.Json;
using Sorcha.Cli.Configuration;
using Sorcha.Cli.UI;

namespace Sorcha.Cli.Services;

/// <summary>
/// Base class for API clients with common functionality
/// </summary>
public abstract class ApiClientBase
{
    protected readonly HttpClient HttpClient;
    protected readonly ActivityLog ActivityLog;
    protected readonly JsonSerializerOptions JsonOptions;

    protected ApiClientBase(HttpClient httpClient, ActivityLog activityLog)
    {
        HttpClient = httpClient;
        ActivityLog = activityLog;
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }

    protected void ApplyAuthHeaders(string role = "Administrator")
    {
        var headers = TestCredentials.GetAuthHeaders(role);
        foreach (var (key, value) in headers)
        {
            if (HttpClient.DefaultRequestHeaders.Contains(key))
            {
                HttpClient.DefaultRequestHeaders.Remove(key);
            }
            HttpClient.DefaultRequestHeaders.Add(key, value);
        }
    }

    protected async Task<T?> GetAsync<T>(string url, CancellationToken ct = default)
    {
        var response = await HttpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
    }

    protected async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string url,
        TRequest request,
        CancellationToken ct = default)
    {
        var response = await HttpClient.PostAsJsonAsync(url, request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct);
    }

    protected async Task<HttpResponseMessage> PostAsync<TRequest>(
        string url,
        TRequest request,
        CancellationToken ct = default)
    {
        var response = await HttpClient.PostAsJsonAsync(url, request, JsonOptions, ct);
        return response;
    }

    protected async Task<TResponse?> PutAsync<TRequest, TResponse>(
        string url,
        TRequest request,
        CancellationToken ct = default)
    {
        var response = await HttpClient.PutAsJsonAsync(url, request, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct);
    }

    protected async Task<HttpResponseMessage> DeleteAsync(string url, CancellationToken ct = default)
    {
        return await HttpClient.DeleteAsync(url, ct);
    }

    protected async Task<bool> CheckHealthAsync(string url, CancellationToken ct = default)
    {
        try
        {
            var response = await HttpClient.GetAsync($"{url}/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
