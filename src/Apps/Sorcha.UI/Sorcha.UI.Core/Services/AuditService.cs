// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace Sorcha.UI.Core.Services;

/// <summary>
/// Client-side audit service implementation.
/// Logs audit events to the backend Tenant Service.
/// </summary>
public class AuditService : IAuditService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuditService> _logger;
    private const string AuditEndpoint = "/api/audit";

    public AuditService(
        HttpClient httpClient,
        ILogger<AuditService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task LogAsync(
        AuditEventType eventType,
        Dictionary<string, object>? details = null,
        CancellationToken cancellationToken = default)
    {
        await SendAuditEventAsync(eventType, null, null, details, cancellationToken);
    }

    public async Task LogOrganizationEventAsync(
        AuditEventType eventType,
        Guid organizationId,
        Dictionary<string, object>? details = null,
        CancellationToken cancellationToken = default)
    {
        var enrichedDetails = details ?? new Dictionary<string, object>();
        enrichedDetails["organizationId"] = organizationId;

        await SendAuditEventAsync(eventType, organizationId, null, enrichedDetails, cancellationToken);
    }

    public async Task LogUserEventAsync(
        AuditEventType eventType,
        Guid organizationId,
        Guid userId,
        Dictionary<string, object>? details = null,
        CancellationToken cancellationToken = default)
    {
        var enrichedDetails = details ?? new Dictionary<string, object>();
        enrichedDetails["organizationId"] = organizationId;
        enrichedDetails["targetUserId"] = userId;

        await SendAuditEventAsync(eventType, organizationId, userId, enrichedDetails, cancellationToken);
    }

    private async Task SendAuditEventAsync(
        AuditEventType eventType,
        Guid? organizationId,
        Guid? userId,
        Dictionary<string, object>? details,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new AuditEventRequest
            {
                EventType = eventType.ToString(),
                OrganizationId = organizationId,
                TargetUserId = userId,
                Details = details,
                Timestamp = DateTimeOffset.UtcNow
            };

            // Fire and forget - don't block on audit logging
            var response = await _httpClient.PostAsJsonAsync(AuditEndpoint, request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to send audit event {EventType}: {StatusCode}",
                    eventType, response.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            // Log locally but don't throw - audit logging should not break functionality
            _logger.LogWarning(ex, "Failed to send audit event {EventType}", eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending audit event {EventType}", eventType);
        }
    }

    /// <summary>
    /// Request payload for audit events.
    /// </summary>
    private record AuditEventRequest
    {
        public required string EventType { get; init; }
        public Guid? OrganizationId { get; init; }
        public Guid? TargetUserId { get; init; }
        public Dictionary<string, object>? Details { get; init; }
        public DateTimeOffset Timestamp { get; init; }
    }
}
