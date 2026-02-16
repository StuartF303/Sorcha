// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Sorcha.Blueprint.Service.Models.Responses;
using Sorcha.Blueprint.Service.Storage;

namespace Sorcha.Blueprint.Service.Tests.Storage;

public class InMemoryActionStoreTests
{
    private readonly InMemoryActionStore _store = new();

    private static ActionDetailsResponse CreateAction(
        string txHash = "tx-1",
        string wallet = "0xAAA",
        string register = "reg-1") => new()
    {
        TransactionHash = txHash,
        BlueprintId = "bp-1",
        ActionId = "1",
        InstanceId = "inst-1",
        SenderWallet = wallet,
        RegisterAddress = register,
        Timestamp = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task StoreActionAsync_ReturnsStoredAction()
    {
        var action = CreateAction();

        var result = await _store.StoreActionAsync(action);

        result.Should().BeSameAs(action);
    }

    [Fact]
    public async Task GetActionAsync_ExistingHash_ReturnsAction()
    {
        await _store.StoreActionAsync(CreateAction("hash-1"));

        var result = await _store.GetActionAsync("hash-1");

        result.Should().NotBeNull();
        result!.TransactionHash.Should().Be("hash-1");
    }

    [Fact]
    public async Task GetActionAsync_NonExistent_ReturnsNull()
    {
        var result = await _store.GetActionAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActionsAsync_FiltersByWalletAndRegister()
    {
        await _store.StoreActionAsync(CreateAction("tx-1", "0xA", "reg-1"));
        await _store.StoreActionAsync(CreateAction("tx-2", "0xA", "reg-2")); // different register
        await _store.StoreActionAsync(CreateAction("tx-3", "0xB", "reg-1")); // different wallet
        await _store.StoreActionAsync(CreateAction("tx-4", "0xA", "reg-1")); // match

        var result = (await _store.GetActionsAsync("0xA", "reg-1")).ToList();

        result.Should().HaveCount(2);
        result.Should().OnlyContain(a => a.SenderWallet == "0xA" && a.RegisterAddress == "reg-1");
    }

    [Fact]
    public async Task GetActionsAsync_Pagination_RespectsSkipAndTake()
    {
        for (int i = 0; i < 5; i++)
            await _store.StoreActionAsync(CreateAction($"tx-{i}", "0xA", "reg-1"));

        var result = (await _store.GetActionsAsync("0xA", "reg-1", skip: 1, take: 2)).ToList();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetActionCountAsync_ReturnsCorrectCount()
    {
        await _store.StoreActionAsync(CreateAction("tx-1", "0xA", "reg-1"));
        await _store.StoreActionAsync(CreateAction("tx-2", "0xA", "reg-1"));
        await _store.StoreActionAsync(CreateAction("tx-3", "0xB", "reg-1"));

        var count = await _store.GetActionCountAsync("0xA", "reg-1");

        count.Should().Be(2);
    }

    [Fact]
    public async Task StoreAndGetFileMetadata_RoundTrips()
    {
        var metadata = new FileMetadata
        {
            FileId = "file-1",
            FileName = "document.pdf",
            ContentType = "application/pdf",
            Size = 1024
        };

        await _store.StoreFileMetadataAsync("tx-1", "file-1", metadata);
        var result = await _store.GetFileMetadataAsync("tx-1", "file-1");

        result.Should().NotBeNull();
        result!.FileName.Should().Be("document.pdf");
        result.Size.Should().Be(1024);
    }

    [Fact]
    public async Task GetFileMetadataAsync_NonExistentTransaction_ReturnsNull()
    {
        var result = await _store.GetFileMetadataAsync("nonexistent", "file-1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task StoreAndGetFileContent_RoundTrips()
    {
        var content = new byte[] { 1, 2, 3, 4, 5 };

        await _store.StoreFileContentAsync("file-1", content);
        var result = await _store.GetFileContentAsync("file-1");

        result.Should().BeEquivalentTo(content);
    }

    [Fact]
    public async Task GetFileContentAsync_NonExistent_ReturnsNull()
    {
        var result = await _store.GetFileContentAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task StoreIdempotencyKey_GetByKey_ReturnsTransactionHash()
    {
        await _store.StoreIdempotencyKeyAsync("key-1", "tx-hash-1", TimeSpan.FromMinutes(5));

        var result = await _store.GetByIdempotencyKeyAsync("key-1");

        result.Should().Be("tx-hash-1");
    }

    [Fact]
    public async Task GetByIdempotencyKeyAsync_NonExistent_ReturnsNull()
    {
        var result = await _store.GetByIdempotencyKeyAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdempotencyKeyAsync_ExpiredKey_ReturnsNull()
    {
        // Store with zero TTL (already expired)
        await _store.StoreIdempotencyKeyAsync("key-1", "tx-hash-1", TimeSpan.Zero);

        // Small delay to ensure expiry
        await Task.Delay(10);

        var result = await _store.GetByIdempotencyKeyAsync("key-1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task StoreActionAsync_SameHash_OverwritesPrevious()
    {
        var original = CreateAction("tx-1");
        await _store.StoreActionAsync(original);

        var updated = new ActionDetailsResponse
        {
            TransactionHash = "tx-1",
            BlueprintId = "bp-2",
            ActionId = "2",
            InstanceId = "inst-2",
            SenderWallet = "0xBBB",
            RegisterAddress = "reg-2"
        };
        await _store.StoreActionAsync(updated);

        var result = await _store.GetActionAsync("tx-1");
        result!.BlueprintId.Should().Be("bp-2");
    }
}
