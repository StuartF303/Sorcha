// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Text.Json;

namespace Sorcha.Blueprint.Service.Models.Chat;

/// <summary>
/// Base class for AI streaming events.
/// </summary>
public abstract record AIStreamEvent;

/// <summary>
/// A chunk of text from the AI response.
/// </summary>
public record TextChunk(string Text) : AIStreamEvent;

/// <summary>
/// The AI wants to use a tool.
/// </summary>
public record ToolUse(string Id, string Name, JsonDocument Arguments) : AIStreamEvent;

/// <summary>
/// The AI has finished generating the response.
/// </summary>
public record StreamEnd(string? StopReason = null) : AIStreamEvent;

/// <summary>
/// An error occurred during streaming.
/// </summary>
public record StreamError(string Message, bool IsRetryable = false) : AIStreamEvent;
