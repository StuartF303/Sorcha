// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Storage.Abstractions;
using Sorcha.Storage.InMemory;

namespace Sorcha.Storage.Abstractions.Tests;

/// <summary>
/// Runs the IWormStore contract tests against InMemoryWormStore.
/// </summary>
public class InMemoryWormStoreContractTests : WormStoreContractTests
{
    private readonly InMemoryWormStore<WormTestDocument, ulong> _store = new(d => d.Height);

    protected override IWormStore<WormTestDocument, ulong> CreateWormStore() => _store;
}
