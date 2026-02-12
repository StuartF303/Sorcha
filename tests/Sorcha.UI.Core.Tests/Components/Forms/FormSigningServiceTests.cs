// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.UI.Core.Models.Wallet;
using Sorcha.UI.Core.Services.Forms;
using Sorcha.UI.Core.Services.Wallet;
using Xunit;

namespace Sorcha.UI.Core.Tests.Components.Forms;

public class FormSigningServiceTests
{
    private readonly Mock<IWalletApiService> _walletMock = new();
    private readonly Mock<ILogger<FormSigningService>> _loggerMock = new();
    private readonly FormSigningService _sut;

    public FormSigningServiceTests()
    {
        _sut = new FormSigningService(_walletMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void SerializeFormData_SortedKeys_ProducesDeterministicJson()
    {
        var data1 = new Dictionary<string, object?>
        {
            ["/z"] = "last",
            ["/a"] = "first",
            ["/m"] = "middle"
        };

        var data2 = new Dictionary<string, object?>
        {
            ["/a"] = "first",
            ["/m"] = "middle",
            ["/z"] = "last"
        };

        var json1 = _sut.SerializeFormData(data1);
        var json2 = _sut.SerializeFormData(data2);

        json1.Should().Be(json2, "keys should be sorted regardless of insertion order");
    }

    [Fact]
    public void SerializeFormData_IncludesAllValues()
    {
        var data = new Dictionary<string, object?>
        {
            ["/name"] = "John",
            ["/amount"] = 42.5
        };

        var json = _sut.SerializeFormData(data);

        json.Should().Contain("John");
        json.Should().Contain("42.5");
    }

    [Fact]
    public void HashData_ProducesBase64String()
    {
        var hash = _sut.HashData("test data");

        hash.Should().NotBeNullOrEmpty();
        // SHA-256 produces 32 bytes → 44 base64 chars
        Convert.FromBase64String(hash).Should().HaveCount(32);
    }

    [Fact]
    public void HashData_SameInput_ProducesSameHash()
    {
        var hash1 = _sut.HashData("{\"a\":\"b\"}");
        var hash2 = _sut.HashData("{\"a\":\"b\"}");

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void HashData_DifferentInput_ProducesDifferentHash()
    {
        var hash1 = _sut.HashData("{\"a\":\"b\"}");
        var hash2 = _sut.HashData("{\"a\":\"c\"}");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public async Task SignWithWallet_Success_ReturnsSignature()
    {
        _walletMock.Setup(w => w.SignDataAsync(
                "ws1abc",
                It.Is<SignTransactionRequest>(r => r.IsPreHashed),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignTransactionResponse
            {
                Signature = "base64signature==",
                SignedBy = "ws1abc",
                SignedAt = DateTime.UtcNow
            });

        var data = new Dictionary<string, object?> { ["/name"] = "John" };
        var sig = await _sut.SignWithWallet("ws1abc", data);

        sig.Should().Be("base64signature==");
    }

    [Fact]
    public async Task SignWithWallet_WalletUnavailable_ReturnsNull()
    {
        _walletMock.Setup(w => w.SignDataAsync(
                It.IsAny<string>(),
                It.IsAny<SignTransactionRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

        var data = new Dictionary<string, object?> { ["/name"] = "John" };
        var sig = await _sut.SignWithWallet("ws1abc", data);

        sig.Should().BeNull();
    }

    [Fact]
    public async Task SignWithWallet_CallsSignDataAsync_WithPreHashed()
    {
        _walletMock.Setup(w => w.SignDataAsync(
                "ws1abc",
                It.IsAny<SignTransactionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignTransactionResponse
            {
                Signature = "sig",
                SignedBy = "ws1abc",
                SignedAt = DateTime.UtcNow
            });

        var data = new Dictionary<string, object?> { ["/field"] = "value" };
        await _sut.SignWithWallet("ws1abc", data);

        _walletMock.Verify(w => w.SignDataAsync(
            "ws1abc",
            It.Is<SignTransactionRequest>(r =>
                r.IsPreHashed == true &&
                !string.IsNullOrEmpty(r.TransactionData)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
