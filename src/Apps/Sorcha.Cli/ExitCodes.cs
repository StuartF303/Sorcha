// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors
namespace Sorcha.Cli;

/// <summary>
/// Standard exit codes for the Sorcha CLI.
/// Following POSIX conventions and common CLI practices.
/// </summary>
public static class ExitCodes
{
    /// <summary>
    /// Success - command completed successfully.
    /// </summary>
    public const int Success = 0;

    /// <summary>
    /// General error - unspecified error occurred.
    /// </summary>
    public const int GeneralError = 1;

    /// <summary>
    /// Authentication error - login failed or credentials invalid.
    /// </summary>
    public const int AuthenticationError = 2;

    /// <summary>
    /// Authorization error - user does not have permission.
    /// </summary>
    public const int AuthorizationError = 3;

    /// <summary>
    /// Validation error - input validation failed.
    /// </summary>
    public const int ValidationError = 4;

    /// <summary>
    /// Not found - requested resource does not exist.
    /// </summary>
    public const int NotFound = 5;

    /// <summary>
    /// Configuration error - invalid configuration or profile.
    /// </summary>
    public const int ConfigurationError = 6;

    /// <summary>
    /// Network error - connection failed or timeout.
    /// </summary>
    public const int NetworkError = 7;

    /// <summary>
    /// Service error - remote service returned an error.
    /// </summary>
    public const int ServiceError = 8;
}
