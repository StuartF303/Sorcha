// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Validator.Service.Configuration;

/// <summary>
/// Configuration for the DocketDistributor service.
/// </summary>
public class DocketDistributorConfiguration
{
    /// <summary>
    /// Configuration section name in appsettings.
    /// </summary>
    public const string SectionName = "DocketDistributor";

    /// <summary>
    /// Timeout for broadcast operations.
    /// Default: 30 seconds.
    /// </summary>
    public TimeSpan BroadcastTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Maximum retry attempts for failed broadcasts.
    /// Default: 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay between retry attempts.
    /// Default: 1 second.
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Whether to wait for acknowledgment from peers after broadcast.
    /// Default: false.
    /// </summary>
    public bool WaitForAcknowledgment { get; set; } = false;

    /// <summary>
    /// Minimum number of peers that must acknowledge for broadcast to be considered successful.
    /// Only used if WaitForAcknowledgment is true.
    /// Default: 1.
    /// </summary>
    public int MinAcknowledgments { get; set; } = 1;

    /// <summary>
    /// Whether to automatically submit confirmed dockets to Register Service.
    /// Default: true.
    /// </summary>
    public bool AutoSubmitToRegisterService { get; set; } = true;

    /// <summary>
    /// Maximum size of serialized docket in bytes.
    /// Default: 10 MB.
    /// </summary>
    public int MaxDocketSizeBytes { get; set; } = 10 * 1024 * 1024;
}
