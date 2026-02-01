// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Blueprint.Service.Models.Chat;

/// <summary>
/// Status of a chat session.
/// </summary>
public enum SessionStatus
{
    /// <summary>Session is actively in use.</summary>
    Active,

    /// <summary>Blueprint was saved successfully and session completed.</summary>
    Completed,

    /// <summary>Session expired after 24 hours of inactivity.</summary>
    Expired
}

/// <summary>
/// Role of a message sender.
/// </summary>
public enum MessageRole
{
    /// <summary>Message from the human user.</summary>
    User,

    /// <summary>Message from the AI assistant.</summary>
    Assistant
}
