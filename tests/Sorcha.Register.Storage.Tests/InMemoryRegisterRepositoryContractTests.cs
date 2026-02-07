// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.Register.Core.Storage;
using Sorcha.Register.Storage.InMemory;

namespace Sorcha.Register.Storage.Tests;

/// <summary>
/// Runs the IRegisterRepository contract tests against InMemoryRegisterRepository.
/// </summary>
public class InMemoryRegisterRepositoryContractTests : RegisterRepositoryContractTests
{
    private readonly InMemoryRegisterRepository _repository = new();

    protected override IRegisterRepository CreateRepository() => _repository;
}
