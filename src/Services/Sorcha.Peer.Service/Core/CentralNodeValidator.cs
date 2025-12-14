// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.RegularExpressions;

namespace Sorcha.Peer.Service.Core;

/// <summary>
/// Validator for central node hostname patterns
/// </summary>
public static class CentralNodeValidator
{
    private static readonly Regex HostnamePattern = new(@"^n[0-2]\.sorcha\.dev$", RegexOptions.Compiled);

    /// <summary>
    /// Validates if a hostname matches the central node pattern (n0.sorcha.dev, n1.sorcha.dev, n2.sorcha.dev)
    /// </summary>
    /// <param name="hostname">Hostname to validate</param>
    /// <returns>True if hostname matches the pattern</returns>
    public static bool IsValidCentralNodeHostname(string? hostname)
    {
        if (string.IsNullOrWhiteSpace(hostname))
            return false;

        return HostnamePattern.IsMatch(hostname);
    }

    /// <summary>
    /// Validates hostname and throws if invalid
    /// </summary>
    /// <param name="hostname">Hostname to validate</param>
    /// <exception cref="ArgumentException">Thrown when hostname is invalid</exception>
    public static void ValidateHostname(string? hostname)
    {
        if (string.IsNullOrWhiteSpace(hostname))
        {
            throw new ArgumentException("Hostname cannot be null or empty", nameof(hostname));
        }

        if (!IsValidCentralNodeHostname(hostname))
        {
            throw new ArgumentException(
                $"Invalid central node hostname: '{hostname}'. Must match pattern: n0.sorcha.dev, n1.sorcha.dev, or n2.sorcha.dev",
                nameof(hostname));
        }
    }
}
