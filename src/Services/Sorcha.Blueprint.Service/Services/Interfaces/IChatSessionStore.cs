// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Blueprint.Service.Models.Chat;

namespace Sorcha.Blueprint.Service.Services.Interfaces;

/// <summary>
/// Interface for chat session storage operations (Redis-backed).
/// </summary>
public interface IChatSessionStore
{
    /// <summary>
    /// Creates a new chat session.
    /// </summary>
    Task<ChatSession> CreateSessionAsync(string userId, string organizationId, string? existingBlueprintId = null);

    /// <summary>
    /// Gets a session by ID.
    /// </summary>
    Task<ChatSession?> GetSessionAsync(string sessionId);

    /// <summary>
    /// Gets the active session for a user (if any).
    /// </summary>
    Task<ChatSession?> GetActiveSessionForUserAsync(string userId);

    /// <summary>
    /// Updates a session (refreshes TTL).
    /// </summary>
    Task UpdateSessionAsync(ChatSession session);

    /// <summary>
    /// Adds a message to a session.
    /// </summary>
    Task AddMessageAsync(string sessionId, ChatMessage message);

    /// <summary>
    /// Gets messages for a session.
    /// </summary>
    Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string sessionId);

    /// <summary>
    /// Deletes a session (on completion or explicit end).
    /// </summary>
    Task DeleteSessionAsync(string sessionId);

    /// <summary>
    /// Sets the active session for a user.
    /// </summary>
    Task SetActiveSessionForUserAsync(string userId, string sessionId);

    /// <summary>
    /// Clears the active session for a user.
    /// </summary>
    Task ClearActiveSessionForUserAsync(string userId);
}
