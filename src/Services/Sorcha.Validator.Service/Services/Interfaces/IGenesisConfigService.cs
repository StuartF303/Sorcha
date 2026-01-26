// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Validator.Service.Services.Interfaces;

/// <summary>
/// Provides access to register configuration from the genesis block's control blueprint.
/// Configuration is cached and refreshed when control blueprint updates occur.
/// </summary>
public interface IGenesisConfigService
{
    /// <summary>
    /// Get consensus configuration for a register
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Consensus configuration from genesis</returns>
    Task<ConsensusConfig> GetConsensusConfigAsync(string registerId, CancellationToken ct = default);

    /// <summary>
    /// Get validator configuration for a register
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validator configuration from genesis</returns>
    Task<ValidatorConfig> GetValidatorConfigAsync(string registerId, CancellationToken ct = default);

    /// <summary>
    /// Get leader election configuration for a register
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Leader election configuration from genesis</returns>
    Task<LeaderElectionConfig> GetLeaderElectionConfigAsync(string registerId, CancellationToken ct = default);

    /// <summary>
    /// Get the full genesis configuration for a register
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Complete genesis configuration</returns>
    Task<GenesisConfiguration> GetFullConfigAsync(string registerId, CancellationToken ct = default);

    /// <summary>
    /// Check if cached configuration is stale and needs refresh
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if configuration needs refresh</returns>
    Task<bool> IsConfigStaleAsync(string registerId, CancellationToken ct = default);

    /// <summary>
    /// Force refresh configuration from the genesis/control blueprint
    /// </summary>
    /// <param name="registerId">Register ID</param>
    /// <param name="ct">Cancellation token</param>
    Task RefreshConfigAsync(string registerId, CancellationToken ct = default);

    /// <summary>
    /// Event raised when configuration changes (control blueprint updated)
    /// </summary>
    event EventHandler<ConfigChangedEventArgs>? ConfigChanged;
}

/// <summary>
/// Consensus configuration from genesis blueprint
/// </summary>
public record ConsensusConfig
{
    /// <summary>Minimum signatures required to commit a docket</summary>
    public required int SignatureThresholdMin { get; init; }

    /// <summary>Maximum signatures to collect (prevents bloat)</summary>
    public required int SignatureThresholdMax { get; init; }

    /// <summary>Time to wait for signature collection</summary>
    public required TimeSpan DocketTimeout { get; init; }

    /// <summary>Hard cap on signatures per docket</summary>
    public required int MaxSignaturesPerDocket { get; init; }

    /// <summary>Maximum transactions per docket</summary>
    public required int MaxTransactionsPerDocket { get; init; }

    /// <summary>Interval between docket builds</summary>
    public required TimeSpan DocketBuildInterval { get; init; }
}

/// <summary>
/// Validator registration configuration from genesis blueprint
/// </summary>
public record ValidatorConfig
{
    /// <summary>Registration mode: "public" (volunteer) or "consent" (approval required)</summary>
    public required string RegistrationMode { get; init; }

    /// <summary>Minimum validators required for consensus</summary>
    public required int MinValidators { get; init; }

    /// <summary>Maximum validators allowed</summary>
    public required int MaxValidators { get; init; }

    /// <summary>Whether validators must stake tokens</summary>
    public required bool RequireStake { get; init; }

    /// <summary>Required stake amount if enabled (null if not required)</summary>
    public decimal? StakeAmount { get; init; }

    /// <summary>Whether registration mode is public (convenience property)</summary>
    public bool IsPublicRegistration => RegistrationMode.Equals("public", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Leader election configuration from genesis blueprint
/// </summary>
public record LeaderElectionConfig
{
    /// <summary>Election mechanism: "rotating", "raft", "stake-weighted"</summary>
    public required string Mechanism { get; init; }

    /// <summary>Interval between leader heartbeats</summary>
    public required TimeSpan HeartbeatInterval { get; init; }

    /// <summary>Time before leader is considered failed</summary>
    public required TimeSpan LeaderTimeout { get; init; }

    /// <summary>Duration of each leadership term (for rotating election)</summary>
    public TimeSpan? TermDuration { get; init; }
}

/// <summary>
/// Complete genesis configuration
/// </summary>
public record GenesisConfiguration
{
    /// <summary>Register ID</summary>
    public required string RegisterId { get; init; }

    /// <summary>Genesis transaction ID</summary>
    public required string GenesisTransactionId { get; init; }

    /// <summary>Current control blueprint version transaction ID</summary>
    public required string ControlBlueprintVersionId { get; init; }

    /// <summary>Consensus configuration</summary>
    public required ConsensusConfig Consensus { get; init; }

    /// <summary>Validator configuration</summary>
    public required ValidatorConfig Validators { get; init; }

    /// <summary>Leader election configuration</summary>
    public required LeaderElectionConfig LeaderElection { get; init; }

    /// <summary>When this configuration was loaded</summary>
    public required DateTimeOffset LoadedAt { get; init; }

    /// <summary>Cache TTL for this configuration</summary>
    public required TimeSpan CacheTtl { get; init; }
}

/// <summary>
/// Event arguments for configuration change events
/// </summary>
public class ConfigChangedEventArgs : EventArgs
{
    /// <summary>Register ID</summary>
    public required string RegisterId { get; init; }

    /// <summary>Previous control blueprint version ID</summary>
    public required string PreviousVersionId { get; init; }

    /// <summary>New control blueprint version ID</summary>
    public required string NewVersionId { get; init; }

    /// <summary>What changed</summary>
    public required IReadOnlyList<string> ChangedProperties { get; init; }

    /// <summary>Timestamp of the change</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
