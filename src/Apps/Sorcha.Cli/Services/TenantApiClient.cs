// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net.Http.Json;
using Sorcha.Cli.Configuration;
using Sorcha.Cli.UI;

namespace Sorcha.Cli.Services;

/// <summary>
/// Client for the Tenant Service API
/// </summary>
public class TenantApiClient : ApiClientBase
{
    private readonly string _baseUrl;

    public TenantApiClient(HttpClient httpClient, ActivityLog activityLog)
        : base(httpClient, activityLog)
    {
        _baseUrl = TestCredentials.TenantServiceUrl;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        return await CheckHealthAsync(_baseUrl, ct);
    }

    public async Task<OrganizationDto?> GetOrganizationAsync(Guid orgId, CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await GetAsync<OrganizationDto>($"{_baseUrl}/api/organizations/{orgId}", ct);
    }

    public async Task<OrganizationDto?> GetOrganizationBySubdomainAsync(string subdomain, CancellationToken ct = default)
    {
        return await GetAsync<OrganizationDto>($"{_baseUrl}/api/organizations/by-subdomain/{subdomain}", ct);
    }

    public async Task<OrganizationDto?> CreateOrganizationAsync(CreateOrganizationRequest request, CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await PostAsync<CreateOrganizationRequest, OrganizationDto>(
            $"{_baseUrl}/api/organizations", request, ct);
    }

    public async Task<List<UserDto>?> GetOrganizationUsersAsync(Guid orgId, CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await GetAsync<List<UserDto>>($"{_baseUrl}/api/organizations/{orgId}/users", ct);
    }

    public async Task<UserDto?> GetUserAsync(Guid orgId, Guid userId, CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await GetAsync<UserDto>($"{_baseUrl}/api/organizations/{orgId}/users/{userId}", ct);
    }

    public async Task<UserDto?> AddUserAsync(Guid orgId, AddUserRequest request, CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await PostAsync<AddUserRequest, UserDto>(
            $"{_baseUrl}/api/organizations/{orgId}/users", request, ct);
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(CancellationToken ct = default)
    {
        ApplyAuthHeaders();
        return await GetAsync<CurrentUserDto>($"{_baseUrl}/api/auth/me", ct);
    }

    public async Task<bool> ValidateSubdomainAsync(string subdomain, CancellationToken ct = default)
    {
        var response = await HttpClient.GetAsync(
            $"{_baseUrl}/api/organizations/validate-subdomain/{subdomain}", ct);
        return response.IsSuccessStatusCode;
    }
}

// DTOs for Tenant Service
public record OrganizationDto(
    Guid Id,
    string Name,
    string Subdomain,
    string? Description,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateOrganizationRequest(
    string Name,
    string Subdomain,
    string? Description = null
);

public record UserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    string Status,
    string? ExternalId,
    DateTime CreatedAt
);

public record AddUserRequest(
    string Email,
    string DisplayName,
    string Role,
    string? ExternalId = null
);

public record CurrentUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string Role,
    Guid OrganizationId,
    string OrganizationName
);
