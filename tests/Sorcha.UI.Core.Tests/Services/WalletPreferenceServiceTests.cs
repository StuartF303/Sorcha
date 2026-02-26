// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Blazored.LocalStorage;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sorcha.UI.Core.Models.Wallet;
using Sorcha.UI.Core.Services;
using Xunit;

namespace Sorcha.UI.Core.Tests.Services;

public class WalletPreferenceServiceTests
{
    private const string LegacyStorageKey = "sorcha:preferences:defaultWallet";
    private readonly Mock<IUserPreferencesService> _userPrefsMock;
    private readonly Mock<ILocalStorageService> _localStorageMock;
    private readonly WalletPreferenceService _sut;

    public WalletPreferenceServiceTests()
    {
        _userPrefsMock = new Mock<IUserPreferencesService>();
        _localStorageMock = new Mock<ILocalStorageService>();
        var logger = NullLogger<WalletPreferenceService>.Instance;
        _sut = new WalletPreferenceService(
            _userPrefsMock.Object,
            _localStorageMock.Object,
            logger);
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

    // -------------------------------------------------------
    // GetSmartDefaultAsync
    // -------------------------------------------------------

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
        _userPrefsMock
            .Setup(x => x.GetDefaultWalletAsync())
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
        _userPrefsMock
            .Setup(x => x.GetDefaultWalletAsync())
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
        _userPrefsMock
            .Setup(x => x.GetDefaultWalletAsync())
            .ReturnsAsync("deleted-wallet");

        var wallets = new List<WalletDto>
        {
            CreateWallet("wallet-X"),
            CreateWallet("wallet-Y")
        };

        var result = await _sut.GetSmartDefaultAsync(wallets);

        result.Should().Be("wallet-X");
        _userPrefsMock.Verify(x => x.ClearDefaultWalletAsync(), Times.Once);
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

    // -------------------------------------------------------
    // SetDefaultWalletAsync / ClearDefaultWalletAsync
    // -------------------------------------------------------

    [Fact]
    public async Task SetDefaultWalletAsync_DelegatesToServerPreferences()
    {
        await _sut.SetDefaultWalletAsync("my-wallet");

        _userPrefsMock.Verify(
            x => x.SetDefaultWalletAsync("my-wallet"),
            Times.Once);
    }

    [Fact]
    public async Task ClearDefaultWalletAsync_DelegatesToServerPreferences()
    {
        await _sut.ClearDefaultWalletAsync();

        _userPrefsMock.Verify(
            x => x.ClearDefaultWalletAsync(),
            Times.Once);
    }

    // -------------------------------------------------------
    // GetDefaultWalletAsync (with server-side delegation)
    // -------------------------------------------------------

    [Fact]
    public async Task GetDefaultWalletAsync_ReturnsServerValue()
    {
        _userPrefsMock
            .Setup(x => x.GetDefaultWalletAsync())
            .ReturnsAsync("server-wallet");

        var result = await _sut.GetDefaultWalletAsync();

        result.Should().Be("server-wallet");
    }

    [Fact]
    public async Task GetDefaultWalletAsync_ServerThrows_ReturnsNull()
    {
        _userPrefsMock
            .Setup(x => x.GetDefaultWalletAsync())
            .ThrowsAsync(new HttpRequestException("Server unavailable"));

        var result = await _sut.GetDefaultWalletAsync();

        result.Should().BeNull();
    }

    // -------------------------------------------------------
    // Migration from localStorage to server
    // -------------------------------------------------------

    [Fact]
    public async Task GetDefaultWalletAsync_MigratesLegacyLocalStorageValue()
    {
        // Legacy localStorage has a value
        _localStorageMock
            .Setup(x => x.GetItemAsStringAsync(LegacyStorageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync("legacy-wallet");

        // Server returns the migrated value after set
        _userPrefsMock
            .Setup(x => x.GetDefaultWalletAsync())
            .ReturnsAsync("legacy-wallet");

        var result = await _sut.GetDefaultWalletAsync();

        result.Should().Be("legacy-wallet");

        // Verify migration: value was sent to server
        _userPrefsMock.Verify(x => x.SetDefaultWalletAsync("legacy-wallet"), Times.Once);

        // Verify legacy key was removed
        _localStorageMock.Verify(
            x => x.RemoveItemAsync(LegacyStorageKey, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetDefaultWalletAsync_NoLegacyValue_DoesNotMigrate()
    {
        _localStorageMock
            .Setup(x => x.GetItemAsStringAsync(LegacyStorageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _userPrefsMock
            .Setup(x => x.GetDefaultWalletAsync())
            .ReturnsAsync("server-wallet");

        var result = await _sut.GetDefaultWalletAsync();

        result.Should().Be("server-wallet");
        _userPrefsMock.Verify(x => x.SetDefaultWalletAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetDefaultWalletAsync_MigrationOnlyRunsOnce()
    {
        _localStorageMock
            .Setup(x => x.GetItemAsStringAsync(LegacyStorageKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync("legacy-wallet");

        _userPrefsMock
            .Setup(x => x.GetDefaultWalletAsync())
            .ReturnsAsync("legacy-wallet");

        // Call twice
        await _sut.GetDefaultWalletAsync();
        await _sut.GetDefaultWalletAsync();

        // Migration should only trigger on the first call
        _localStorageMock.Verify(
            x => x.GetItemAsStringAsync(LegacyStorageKey, It.IsAny<CancellationToken>()),
            Times.Once);
        _userPrefsMock.Verify(x => x.SetDefaultWalletAsync("legacy-wallet"), Times.Once);
    }

    [Fact]
    public async Task GetDefaultWalletAsync_MigrationFailure_StillReturnsServerValue()
    {
        _localStorageMock
            .Setup(x => x.GetItemAsStringAsync(LegacyStorageKey, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("JS interop failed"));

        _userPrefsMock
            .Setup(x => x.GetDefaultWalletAsync())
            .ReturnsAsync("server-wallet");

        var result = await _sut.GetDefaultWalletAsync();

        result.Should().Be("server-wallet");
    }
}
