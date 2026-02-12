// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Sorcha.Storage.Abstractions;
using Sorcha.Storage.InMemory;

namespace Sorcha.Storage.Abstractions.Tests;

/// <summary>
/// Runs the IRepository contract tests against InMemoryRepository.
/// </summary>
public class InMemoryRepositoryContractTests : RepositoryContractTests
{
    private readonly InMemoryRepository<RepoTestEntity, Guid> _repository = new(e => e.Id);

    protected override IRepository<RepoTestEntity, Guid> CreateRepository() => _repository;
}
