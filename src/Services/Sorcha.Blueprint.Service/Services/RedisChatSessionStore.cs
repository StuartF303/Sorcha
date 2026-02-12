// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Sorcha.Blueprint.Service.Models.Chat;
using Sorcha.Blueprint.Service.Services.Interfaces;

namespace Sorcha.Blueprint.Service.Services;

/// <summary>
/// Redis-backed implementation of chat session storage.
/// </summary>
/// <remarks>
/// Key Patterns:
/// - chat:session:{sessionId} - Hash containing session data
/// - chat:messages:{sessionId} - List of messages
/// - chat:user:{userId}:active - String containing active session ID
/// TTL: 86400 seconds (24 hours) per SC-008
/// </remarks>
public class RedisChatSessionStore : IChatSessionStore
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisChatSessionStore> _logger;

    private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(24);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisChatSessionStore(
        IDistributedCache cache,
        ILogger<RedisChatSessionStore> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ChatSession> CreateSessionAsync(string userId, string organizationId, string? existingBlueprintId = null)
    {
        var session = new ChatSession
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            OrganizationId = organizationId,
            ExistingBlueprintId = existingBlueprintId,
            Status = SessionStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            LastActivityAt = DateTimeOffset.UtcNow
        };

        await UpdateSessionAsync(session);
        await SetActiveSessionForUserAsync(userId, session.Id);

        _logger.LogInformation("Created chat session {SessionId} for user {UserId}", session.Id, userId);

        return session;
    }

    /// <inheritdoc />
    public async Task<ChatSession?> GetSessionAsync(string sessionId)
    {
        var key = GetSessionKey(sessionId);
        var json = await _cache.GetStringAsync(key);

        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            var session = JsonSerializer.Deserialize<ChatSession>(json, JsonOptions);

            if (session != null)
            {
                // Load messages separately
                var messages = await GetMessagesAsync(sessionId);
                session = session with { Messages = messages.ToList() };

                // Check if expired
                if (session.IsExpired)
                {
                    session = session with { Status = SessionStatus.Expired };
                }
            }

            return session;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize session {SessionId}", sessionId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<ChatSession?> GetActiveSessionForUserAsync(string userId)
    {
        var key = GetUserActiveSessionKey(userId);
        var sessionId = await _cache.GetStringAsync(key);

        if (string.IsNullOrEmpty(sessionId))
        {
            return null;
        }

        return await GetSessionAsync(sessionId);
    }

    /// <inheritdoc />
    public async Task UpdateSessionAsync(ChatSession session)
    {
        var key = GetSessionKey(session.Id);
        var updatedSession = session with { LastActivityAt = DateTimeOffset.UtcNow };

        // Serialize without messages (stored separately)
        var sessionForStorage = updatedSession with { Messages = [] };
        var json = JsonSerializer.Serialize(sessionForStorage, JsonOptions);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = SessionTtl
        };

        await _cache.SetStringAsync(key, json, options);

        _logger.LogDebug("Updated session {SessionId}, TTL refreshed", session.Id);
    }

    /// <inheritdoc />
    public async Task AddMessageAsync(string sessionId, ChatMessage message)
    {
        var key = GetMessagesKey(sessionId);
        var json = JsonSerializer.Serialize(message, JsonOptions);

        // Get existing messages
        var existingJson = await _cache.GetStringAsync(key);
        var messages = string.IsNullOrEmpty(existingJson)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(existingJson, JsonOptions) ?? new List<string>();

        messages.Add(json);

        // Enforce 100 message limit (FR-019)
        if (messages.Count > ChatSession.MaxMessages)
        {
            messages = messages.TakeLast(ChatSession.MaxMessages).ToList();
        }

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = SessionTtl
        };

        await _cache.SetStringAsync(key, JsonSerializer.Serialize(messages, JsonOptions), options);

        _logger.LogDebug("Added message to session {SessionId}, total messages: {Count}", sessionId, messages.Count);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string sessionId)
    {
        var key = GetMessagesKey(sessionId);
        var json = await _cache.GetStringAsync(key);

        if (string.IsNullOrEmpty(json))
        {
            return Array.Empty<ChatMessage>();
        }

        try
        {
            var messageJsonList = JsonSerializer.Deserialize<List<string>>(json, JsonOptions);
            if (messageJsonList == null)
            {
                return Array.Empty<ChatMessage>();
            }

            return messageJsonList
                .Select(m => JsonSerializer.Deserialize<ChatMessage>(m, JsonOptions))
                .Where(m => m != null)
                .Cast<ChatMessage>()
                .ToList();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize messages for session {SessionId}", sessionId);
            return Array.Empty<ChatMessage>();
        }
    }

    /// <inheritdoc />
    public async Task DeleteSessionAsync(string sessionId)
    {
        var session = await GetSessionAsync(sessionId);
        if (session != null)
        {
            await ClearActiveSessionForUserAsync(session.UserId);
        }

        await _cache.RemoveAsync(GetSessionKey(sessionId));
        await _cache.RemoveAsync(GetMessagesKey(sessionId));

        _logger.LogInformation("Deleted session {SessionId}", sessionId);
    }

    /// <inheritdoc />
    public async Task SetActiveSessionForUserAsync(string userId, string sessionId)
    {
        var key = GetUserActiveSessionKey(userId);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = SessionTtl
        };

        await _cache.SetStringAsync(key, sessionId, options);
    }

    /// <inheritdoc />
    public async Task ClearActiveSessionForUserAsync(string userId)
    {
        var key = GetUserActiveSessionKey(userId);
        await _cache.RemoveAsync(key);
    }

    private static string GetSessionKey(string sessionId) => $"chat:session:{sessionId}";
    private static string GetMessagesKey(string sessionId) => $"chat:messages:{sessionId}";
    private static string GetUserActiveSessionKey(string userId) => $"chat:user:{userId}:active";
}
