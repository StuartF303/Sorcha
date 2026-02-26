// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Blueprint.Service.Models;

namespace Sorcha.Blueprint.Service.Services.Interfaces;

/// <summary>
/// Service for managing activity events (activity log).
/// </summary>
public interface IEventService
{
    Task<(IReadOnlyList<ActivityEvent> Items, int TotalCount)> GetEventsAsync(
        Guid userId, int page, int pageSize, bool unreadOnly = false,
        EventSeverity? severity = null, DateTime? since = null,
        CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);

    Task<int> MarkReadAsync(Guid userId, Guid[]? eventIds = null, CancellationToken ct = default);

    Task<ActivityEvent> CreateEventAsync(ActivityEvent activityEvent, CancellationToken ct = default);

    Task<(IReadOnlyList<ActivityEvent> Items, int TotalCount)> GetAdminEventsAsync(
        Guid organizationId, int page, int pageSize, Guid? userId = null,
        EventSeverity? severity = null, DateTime? since = null,
        CancellationToken ct = default);

    Task<bool> DeleteEventAsync(Guid eventId, Guid userId, CancellationToken ct = default);
}
