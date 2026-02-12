// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Storage.Abstractions;
using Sorcha.Storage.InMemory;

namespace Sorcha.Storage.Abstractions.Tests;

/// <summary>
/// Runs the IDocumentStore contract tests against InMemoryDocumentStore.
/// </summary>
public class InMemoryDocumentStoreContractTests : DocumentStoreContractTests
{
    private readonly InMemoryDocumentStore<DocTestDocument, string> _store = new(d => d.Id);

    protected override IDocumentStore<DocTestDocument, string> CreateDocumentStore() => _store;
}
