// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Validator.Service.Services;

/// <summary>
/// Registry for tracking registers that should be monitored for docket building
/// </summary>
public interface IRegisterMonitoringRegistry
{
    /// <summary>
    /// Registers a register for docket building monitoring
    /// </summary>
    void RegisterForMonitoring(string registerId);

    /// <summary>
    /// Unregisters a register from docket building monitoring
    /// </summary>
    void UnregisterFromMonitoring(string registerId);

    /// <summary>
    /// Gets all registered register IDs
    /// </summary>
    IEnumerable<string> GetAll();

    /// <summary>
    /// Checks if a register is registered for monitoring
    /// </summary>
    bool IsRegistered(string registerId);
}
