// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net.Http.Json;
using System.Text.Json;
using Sorcha.Cli.Configuration;
using Sorcha.Cli.UI;

namespace Sorcha.Cli.Services;

/// <summary>
/// Client for the Blueprint Service API
/// </summary>
public class BlueprintApiClient : ApiClientBase
{
    private readonly string _baseUrl;

    public BlueprintApiClient(HttpClient httpClient, ActivityLog activityLog)
        : base(httpClient, activityLog)
    {
        _baseUrl = TestCredentials.BlueprintServiceUrl;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        return await CheckHealthAsync(_baseUrl, ct);
    }

    public async Task<BlueprintDto?> CreateBlueprintAsync(CreateBlueprintRequest request, CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await PostAsync<CreateBlueprintRequest, BlueprintDto>(
            $"{_baseUrl}/api/blueprints", request, ct);
    }

    public async Task<PagedResult<BlueprintDto>?> ListBlueprintsAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await GetAsync<PagedResult<BlueprintDto>>(
            $"{_baseUrl}/api/blueprints?page={page}&pageSize={pageSize}", ct);
    }

    public async Task<BlueprintDto?> GetBlueprintAsync(string blueprintId, CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await GetAsync<BlueprintDto>($"{_baseUrl}/api/blueprints/{blueprintId}", ct);
    }

    public async Task<BlueprintDto?> PublishBlueprintAsync(string blueprintId, CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        var response = await PostAsync($"{_baseUrl}/api/blueprints/{blueprintId}/publish", new { }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BlueprintDto>(JsonOptions, ct);
    }

    public async Task<List<BlueprintVersionDto>?> GetBlueprintVersionsAsync(
        string blueprintId,
        CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await GetAsync<List<BlueprintVersionDto>>(
            $"{_baseUrl}/api/blueprints/{blueprintId}/versions", ct);
    }

    public async Task<List<TemplateDto>?> ListTemplatesAsync(CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await GetAsync<List<TemplateDto>>($"{_baseUrl}/api/templates", ct);
    }

    public async Task<ValidationResultDto?> ValidateActionDataAsync(
        ValidateActionRequest request,
        CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await PostAsync<ValidateActionRequest, ValidationResultDto>(
            $"{_baseUrl}/api/execution/validate", request, ct);
    }

    public async Task<CalculationResultDto?> CalculateAsync(
        CalculateRequest request,
        CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await PostAsync<CalculateRequest, CalculationResultDto>(
            $"{_baseUrl}/api/execution/calculate", request, ct);
    }

    public async Task<HttpResponseMessage> DeleteBlueprintAsync(string blueprintId, CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await DeleteAsync($"{_baseUrl}/api/blueprints/{blueprintId}", ct);
    }
}

// DTOs for Blueprint Service
public record BlueprintDto(
    string Id,
    string Title,
    string? Description,
    int Version,
    string Status,
    string Author,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    List<ActionDefinitionDto>? Actions,
    List<ParticipantDto>? Participants
);

public record ActionDefinitionDto(
    int Id,
    string Title,
    string? Description,
    int? PreviousTxId,
    List<string>? Signatories
);

public record ParticipantDto(
    string Id,
    string Name,
    string? Description,
    string? Organisation
);

public record CreateBlueprintRequest(
    string Id,
    string Title,
    string? Description,
    string Author,
    List<ActionDefinitionDto>? Actions = null,
    List<ParticipantDto>? Participants = null
);

public record BlueprintVersionDto(
    int VersionNumber,
    string BlueprintId,
    DateTime CreatedAt,
    string? ChangeLog
);

public record TemplateDto(
    string Id,
    string Name,
    string? Description,
    string? Category,
    bool IsPublished,
    JsonDocument? Parameters
);

public record ValidateActionRequest(
    string BlueprintId,
    int ActionId,
    JsonDocument Data
);

public record ValidationResultDto(
    bool IsValid,
    List<string>? Errors
);

public record CalculateRequest(
    string BlueprintId,
    int ActionId,
    JsonDocument Data
);

public record CalculationResultDto(
    JsonDocument Result,
    bool Success,
    string? Error
);

public record PagedResult<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);
