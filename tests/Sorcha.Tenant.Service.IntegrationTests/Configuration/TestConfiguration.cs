// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

namespace Sorcha.Tenant.Service.IntegrationTests.Configuration;

/// <summary>
/// Configuration settings for integration tests.
/// </summary>
public static class TestConfiguration
{
    /// <summary>
    /// Gets the database mode for tests from environment variable TEST_DATABASE_MODE.
    /// Defaults to InMemory for fast tests.
    /// Set to "PostgreSQL" to use Testcontainers with real PostgreSQL.
    /// </summary>
    public static TestDatabaseMode DatabaseMode
    {
        get
        {
            var mode = Environment.GetEnvironmentVariable("TEST_DATABASE_MODE");

            if (string.IsNullOrWhiteSpace(mode))
                return TestDatabaseMode.InMemory;

            return Enum.TryParse<TestDatabaseMode>(mode, ignoreCase: true, out var result)
                ? result
                : TestDatabaseMode.InMemory;
        }
    }

    /// <summary>
    /// Gets whether to use PostgreSQL Testcontainers.
    /// </summary>
    public static bool UsePostgreSQL => DatabaseMode == TestDatabaseMode.PostgreSQL;

    /// <summary>
    /// Gets whether to use InMemory database.
    /// </summary>
    public static bool UseInMemory => DatabaseMode == TestDatabaseMode.InMemory;
}
