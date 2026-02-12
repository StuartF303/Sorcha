// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using BlueprintModel = Sorcha.Blueprint.Models.Blueprint;

namespace Sorcha.Blueprint.Service.Models.Chat;

/// <summary>
/// Represents an active conversation for designing a blueprint.
/// </summary>
public record ChatSession
{
    /// <summary>
    /// Unique session identifier.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Authenticated user ID.
    /// </summary>
    public required string UserId { get; init; }

    /// <summary>
    /// User's organization ID.
    /// </summary>
    public required string OrganizationId { get; init; }

    /// <summary>
    /// Current in-progress blueprint draft.
    /// </summary>
    public BlueprintModel? BlueprintDraft { get; set; }

    /// <summary>
    /// If editing an existing blueprint, its ID.
    /// </summary>
    public string? ExistingBlueprintId { get; init; }

    /// <summary>
    /// Conversation history (max 100 items per FR-019).
    /// </summary>
    public List<ChatMessage> Messages { get; init; } = [];

    /// <summary>
    /// Current session status.
    /// </summary>
    public SessionStatus Status { get; set; } = SessionStatus.Active;

    /// <summary>
    /// When the session was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Last message timestamp (for expiration tracking).
    /// </summary>
    public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Maximum messages per session (FR-019).
    /// </summary>
    public const int MaxMessages = 100;

    /// <summary>
    /// Warning threshold for message limit.
    /// </summary>
    public const int MessageWarningThreshold = 10;

    /// <summary>
    /// Session expiration time in hours (SC-008).
    /// </summary>
    public const int ExpirationHours = 24;

    /// <summary>
    /// Gets the number of remaining messages before limit.
    /// </summary>
    public int RemainingMessages => MaxMessages - Messages.Count;

    /// <summary>
    /// Whether the session is approaching the message limit.
    /// </summary>
    public bool IsApproachingMessageLimit => RemainingMessages <= MessageWarningThreshold;

    /// <summary>
    /// Whether the session has expired.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow - LastActivityAt > TimeSpan.FromHours(ExpirationHours);

    /// <summary>
    /// Whether the message limit has been reached.
    /// </summary>
    public bool IsMessageLimitReached => Messages.Count >= MaxMessages;
}
