// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Sorcha.UI.Core.Models.Blueprints;
using Sorcha.UI.Core.Models.Templates;

namespace Sorcha.UI.Core.Services;

/// <summary>
/// Implementation of <see cref="ITemplateApiService"/> calling the Blueprint Service API.
/// </summary>
public class TemplateApiService : ITemplateApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TemplateApiService> _logger;

    public TemplateApiService(HttpClient httpClient, ILogger<TemplateApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<TemplateListItemViewModel>> GetTemplatesAsync(string? category = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = "/api/templates";
            if (!string.IsNullOrEmpty(category)) url += $"?category={Uri.EscapeDataString(category)}";

            var templates = await _httpClient.GetFromJsonAsync<List<TemplateListItemViewModel>>(url, cancellationToken);
            return templates ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching templates");
            return [];
        }
    }

    public async Task<TemplateListItemViewModel?> GetTemplateAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<TemplateListItemViewModel>($"/api/templates/{id}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching template {Id}", id);
            return null;
        }
    }

    public async Task<BlueprintListItemViewModel?> EvaluateTemplateAsync(string id, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/templates/evaluate",
                new { templateId = id, parameters }, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<BlueprintListItemViewModel>(cancellationToken: cancellationToken);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating template {Id}", id);
            return null;
        }
    }

    public async Task<bool> ValidateParametersAsync(string id, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/templates/{id}/validate",
                new { parameters }, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating template parameters {Id}", id);
            return false;
        }
    }
}
