// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Storage.Abstractions;
using Sorcha.Storage.InMemory;

namespace Sorcha.Storage.Abstractions.Tests;

/// <summary>
/// Runs the ICacheStore contract tests against InMemoryCacheStore.
/// </summary>
public class InMemoryCacheStoreContractTests : CacheStoreContractTests
{
    private readonly InMemoryCacheStore _cacheStore = new();

    protected override ICacheStore CreateCacheStore() => _cacheStore;
}
