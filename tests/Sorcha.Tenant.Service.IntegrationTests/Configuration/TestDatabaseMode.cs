// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.Tenant.Service.IntegrationTests.Configuration;

/// <summary>
/// Database mode for integration tests.
/// </summary>
public enum TestDatabaseMode
{
    /// <summary>
    /// Use in-memory database (fast, isolated, no Docker required).
    /// Best for CI/CD and rapid development.
    /// </summary>
    InMemory,

    /// <summary>
    /// Use PostgreSQL via Testcontainers (realistic, requires Docker).
    /// Best for pre-production validation and debugging database-specific issues.
    /// </summary>
    PostgreSQL
}
