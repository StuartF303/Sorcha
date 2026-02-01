// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Blueprint.Service.Models.Chat;

namespace Sorcha.Blueprint.Service.Services.Interfaces;

/// <summary>
/// Abstraction for AI provider integration (Anthropic Claude, etc.).
/// </summary>
public interface IAIProviderService
{
    /// <summary>
    /// Streams a completion from the AI provider.
    /// </summary>
    /// <param name="messages">Conversation history.</param>
    /// <param name="tools">Available tools for the AI to use.</param>
    /// <param name="systemPrompt">Optional system prompt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of stream events.</returns>
    IAsyncEnumerable<AIStreamEvent> StreamCompletionAsync(
        IReadOnlyList<ChatMessage> messages,
        IReadOnlyList<ToolDefinition> tools,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default);
}
