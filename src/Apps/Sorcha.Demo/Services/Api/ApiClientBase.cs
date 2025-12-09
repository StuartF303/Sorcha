// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace Sorcha.Demo.Services.Api;

/// <summary>
/// Base class for API clients with common HTTP functionality and logging
/// </summary>
public abstract class ApiClientBase
{
    protected readonly HttpClient HttpClient;
    protected readonly ILogger Logger;
    protected readonly JsonSerializerOptions JsonOptions;

    protected ApiClientBase(HttpClient httpClient, ILogger logger)
    {
        HttpClient = httpClient;
        Logger = logger;
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }

    protected async Task<T?> GetAsync<T>(string url, CancellationToken ct = default)
    {
        try
        {
            Logger.LogInformation("GET {Url}", url);
            var response = await HttpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
            Logger.LogDebug("GET {Url} -> Success", url);
            return result;
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "GET {Url} failed", url);
            throw;
        }
    }

    protected async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string url,
        TRequest request,
        CancellationToken ct = default)
    {
        try
        {
            Logger.LogInformation("POST {Url}", url);
            var response = await HttpClient.PostAsJsonAsync(url, request, JsonOptions, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct);
            Logger.LogDebug("POST {Url} -> Success", url);
            return result;
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "POST {Url} failed", url);
            throw;
        }
    }

    protected async Task<HttpResponseMessage> PostAsync<TRequest>(
        string url,
        TRequest request,
        CancellationToken ct = default)
    {
        Logger.LogInformation("POST {Url}", url);
        var response = await HttpClient.PostAsJsonAsync(url, request, JsonOptions, ct);
        return response;
    }

    protected async Task<TResponse?> PutAsync<TRequest, TResponse>(
        string url,
        TRequest request,
        CancellationToken ct = default)
    {
        try
        {
            Logger.LogInformation("PUT {Url}", url);
            var response = await HttpClient.PutAsJsonAsync(url, request, JsonOptions, ct);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<TResponse>(JsonOptions, ct);
            Logger.LogDebug("PUT {Url} -> Success", url);
            return result;
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "PUT {Url} failed", url);
            throw;
        }
    }

    protected async Task<HttpResponseMessage> DeleteAsync(string url, CancellationToken ct = default)
    {
        Logger.LogInformation("DELETE {Url}", url);
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
