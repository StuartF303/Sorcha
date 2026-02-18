// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Blazored.LocalStorage;
using FluentAssertions;
using Moq;
using Sorcha.UI.Core.Models.Wallet;
using Sorcha.UI.Core.Services;
using Xunit;

namespace Sorcha.UI.Core.Tests.Services;

public class WalletPreferenceServiceTests
{
    private const string StorageKey = "sorcha:preferences:defaultWallet";
    private readonly Mock<ILocalStorageService> _localStorageMock;
    private readonly WalletPreferenceService _sut;

    public WalletPreferenceServiceTests()
    {
        _localStorageMock = new Mock<ILocalStorageService>();
        _sut = new WalletPreferenceService(_localStorageMock.Object);
    }

    private static WalletDto CreateWallet(string address, string name = "Test") => new()
    {
        Address = address,
        Name = name,
        PublicKey = "abc123",
        Algorithm = "ED25519",
        Status = "Active",
        Owner = "user1",
        Tenant = "tenant1"
    };

    [Fact]
    public async Task GetSmartDefaultAsync_SingleWallet_ReturnsItsAddress()
    {
        var wallets = new List<WalletDto> { CreateWallet("wallet-1") };

        var result = await _sut.GetSmartDefaultAsync(wallets);

        result.Should().Be("wallet-1");
    }

    [Fact]
    public async Task GetSmartDefaultAsync_MultipleWallets_StoredDefault_ReturnsStoredDefault()
    {
        _localStorageMock
            .Setup(x => x.GetItemAsStringAsync(StorageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync("wallet-2");

        var wallets = new List<WalletDto>
        {
            CreateWallet("wallet-1"),
            CreateWallet("wallet-2"),
            CreateWallet("wallet-3")
        };

        var result = await _sut.GetSmartDefaultAsync(wallets);

        result.Should().Be("wallet-2");
    }

    [Fact]
    public async Task GetSmartDefaultAsync_MultipleWallets_NoDefault_ReturnsFirstWallet()
    {
        _localStorageMock
            .Setup(x => x.GetItemAsStringAsync(StorageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var wallets = new List<WalletDto>
        {
            CreateWallet("wallet-A"),
            CreateWallet("wallet-B")
        };

        var result = await _sut.GetSmartDefaultAsync(wallets);

        result.Should().Be("wallet-A");
    }

    [Fact]
    public async Task GetSmartDefaultAsync_StoredDefaultNotInList_ClearsAndFallsBack()
    {
        _localStorageMock
            .Setup(x => x.GetItemAsStringAsync(StorageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync("deleted-wallet");

        var wallets = new List<WalletDto>
        {
            CreateWallet("wallet-X"),
            CreateWallet("wallet-Y")
        };

        var result = await _sut.GetSmartDefaultAsync(wallets);

        result.Should().Be("wallet-X");
        _localStorageMock.Verify(x => x.RemoveItemAsync(StorageKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSmartDefaultAsync_EmptyList_ReturnsNull()
    {
        var result = await _sut.GetSmartDefaultAsync([]);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetSmartDefaultAsync_NullList_ReturnsNull()
    {
        var result = await _sut.GetSmartDefaultAsync(null!);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetDefaultWalletAsync_PersistsToLocalStorage()
    {
        await _sut.SetDefaultWalletAsync("my-wallet");

        _localStorageMock.Verify(
            x => x.SetItemAsStringAsync(StorageKey, "my-wallet", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ClearDefaultWalletAsync_RemovesFromLocalStorage()
    {
        await _sut.ClearDefaultWalletAsync();

        _localStorageMock.Verify(
            x => x.RemoveItemAsync(StorageKey, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetDefaultWalletAsync_ReturnsStoredValue()
    {
        _localStorageMock
            .Setup(x => x.GetItemAsStringAsync(StorageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync("stored-wallet");

        var result = await _sut.GetDefaultWalletAsync();

        result.Should().Be("stored-wallet");
    }

    [Fact]
    public async Task GetDefaultWalletAsync_StorageThrows_ReturnsNull()
    {
        _localStorageMock
            .Setup(x => x.GetItemAsStringAsync(StorageKey, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("JS interop failed"));

        var result = await _sut.GetDefaultWalletAsync();

        result.Should().BeNull();
    }
}
