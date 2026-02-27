// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors
namespace Sorcha.Cli.Commands;

/// <summary>
/// Interface for formatting command output in different formats (table, json, csv).
/// </summary>
public interface IOutputFormatter
{
    /// <summary>
    /// Formats a single object for output.
    /// </summary>
    /// <typeparam name="T">Type of object to format</typeparam>
    /// <param name="data">Object to format</param>
    /// <returns>Formatted string</returns>
    string FormatSingle<T>(T data) where T : class;

    /// <summary>
    /// Formats a collection of objects for output.
    /// </summary>
    /// <typeparam name="T">Type of objects to format</typeparam>
    /// <param name="data">Collection to format</param>
    /// <returns>Formatted string</returns>
    string FormatCollection<T>(IEnumerable<T> data) where T : class;

    /// <summary>
    /// Formats a simple message.
    /// </summary>
    /// <param name="message">Message to format</param>
    /// <returns>Formatted message</returns>
    string FormatMessage(string message);
}
