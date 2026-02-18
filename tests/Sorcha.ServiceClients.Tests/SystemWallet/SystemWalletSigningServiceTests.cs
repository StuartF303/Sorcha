// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sorcha.ServiceClients.SystemWallet;
using Sorcha.ServiceClients.Wallet;

namespace Sorcha.ServiceClients.Tests.SystemWallet;

public class SystemWalletSigningServiceTests
{
    private readonly Mock<IWalletServiceClient> _walletClientMock;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<SystemWalletSigningService>> _loggerMock;
    private readonly SystemWalletSigningOptions _options;

    private const string TestValidatorId = "test-validator-001";
    private const string TestWalletAddress = "sys-wallet-addr-001";
    private const string TestRegisterId = "register-abc123";
    private const string TestTxId = "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2";
    private const string TestPayloadHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

    public SystemWalletSigningServiceTests()
    {
        _walletClientMock = new Mock<IWalletServiceClient>();
        _loggerMock = new Mock<ILogger<SystemWalletSigningService>>();

        // Default wallet creation setup
        _walletClientMock
            .Setup(w => w.CreateOrRetrieveSystemWalletAsync(TestValidatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestWalletAddress);

        // Default signing setup
        _walletClientMock
            .Setup(w => w.SignTransactionAsync(
                TestWalletAddress,
                It.IsAny<byte[]>(),
                It.IsAny<string?>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WalletSignResult
            {
                Signature = [1, 2, 3, 4],
                PublicKey = [5, 6, 7, 8],
                SignedBy = TestWalletAddress,
                Algorithm = "ED25519"
            });

        // Wire up IServiceScopeFactory → IServiceScope → IServiceProvider → IWalletServiceClient
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(p => p.GetService(typeof(IWalletServiceClient)))
            .Returns(_walletClientMock.Object);

        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);

        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _scopeFactoryMock
            .Setup(f => f.CreateScope())
            .Returns(scopeMock.Object);

        _options = new SystemWalletSigningOptions
        {
            ValidatorId = TestValidatorId,
            AllowedDerivationPaths = ["sorcha:register-control", "sorcha:docket-signing"],
            MaxSignsPerRegisterPerMinute = 10
        };
    }

    private SystemWalletSigningService CreateService() =>
        new(_scopeFactoryMock.Object, _options, _loggerMock.Object);

    // =========================================================================
    // Successful signing
    // =========================================================================

    [Fact]
    public async Task SignAsync_ValidRequest_ReturnsSignResult()
    {
        var service = CreateService();

        var result = await service.SignAsync(
            TestRegisterId, TestTxId, TestPayloadHash,
            "sorcha:register-control", "Genesis");

        result.Should().NotBeNull();
        result.Signature.Should().Equal([1, 2, 3, 4]);
        result.PublicKey.Should().Equal([5, 6, 7, 8]);
        result.Algorithm.Should().Be("ED25519");
        result.WalletAddress.Should().Be(TestWalletAddress);
    }

    [Fact]
    public async Task SignAsync_ValidRequest_CallsWalletServiceWithCorrectParams()
    {
        var service = CreateService();

        await service.SignAsync(
            TestRegisterId, TestTxId, TestPayloadHash,
            "sorcha:register-control", "Genesis");

        _walletClientMock.Verify(w => w.SignTransactionAsync(
            TestWalletAddress,
            It.IsAny<byte[]>(),
            "sorcha:register-control",
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // Whitelist enforcement
    // =========================================================================

    [Fact]
    public async Task SignAsync_UnrecognisedDerivationPath_ThrowsInvalidOperation()
    {
        var service = CreateService();

        var act = () => service.SignAsync(
            TestRegisterId, TestTxId, TestPayloadHash,
            "sorcha:unknown-path", "Genesis");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not in the operation whitelist*");
    }

    [Fact]
    public async Task SignAsync_WhitelistRejection_DoesNotCallWalletService()
    {
        var service = CreateService();

        try
        {
            await service.SignAsync(
                TestRegisterId, TestTxId, TestPayloadHash,
                "sorcha:unknown-path", "Genesis");
        }
        catch (InvalidOperationException) { }

        _walletClientMock.Verify(
            w => w.SignTransactionAsync(
                It.IsAny<string>(), It.IsAny<byte[]>(),
                It.IsAny<string?>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // =========================================================================
    // Rate limiting
    // =========================================================================

    [Fact]
    public async Task SignAsync_ExceedsRateLimit_ThrowsInvalidOperation()
    {
        _options.MaxSignsPerRegisterPerMinute = 3;
        var service = CreateService();

        // First 3 should succeed
        for (var i = 0; i < 3; i++)
        {
            await service.SignAsync(
                TestRegisterId, TestTxId, TestPayloadHash,
                "sorcha:register-control", "Genesis");
        }

        // 4th should be rate-limited
        var act = () => service.SignAsync(
            TestRegisterId, TestTxId, TestPayloadHash,
            "sorcha:register-control", "Genesis");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Rate limit exceeded*");
    }

    [Fact]
    public async Task SignAsync_DifferentRegisters_IndependentRateLimits()
    {
        _options.MaxSignsPerRegisterPerMinute = 1;
        var service = CreateService();

        // First register
        await service.SignAsync(
            "register-1", TestTxId, TestPayloadHash,
            "sorcha:register-control", "Genesis");

        // Second register — should not be rate-limited
        var act = () => service.SignAsync(
            "register-2", TestTxId, TestPayloadHash,
            "sorcha:register-control", "Genesis");

        await act.Should().NotThrowAsync();
    }

    // =========================================================================
    // Wallet lifecycle
    // =========================================================================

    [Fact]
    public async Task SignAsync_FirstCall_CreatesOrRetrievesWallet()
    {
        var service = CreateService();

        await service.SignAsync(
            TestRegisterId, TestTxId, TestPayloadHash,
            "sorcha:register-control", "Genesis");

        _walletClientMock.Verify(
            w => w.CreateOrRetrieveSystemWalletAsync(TestValidatorId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SignAsync_SubsequentCalls_CachesWalletAddress()
    {
        var service = CreateService();

        await service.SignAsync(
            TestRegisterId, TestTxId, TestPayloadHash,
            "sorcha:register-control", "Genesis");
        await service.SignAsync(
            TestRegisterId, TestTxId, TestPayloadHash,
            "sorcha:register-control", "Control");

        // Wallet creation called only once (cached after first call)
        _walletClientMock.Verify(
            w => w.CreateOrRetrieveSystemWalletAsync(TestValidatorId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SignAsync_WalletUnavailable_RecreatesAndRetries()
    {
        var callCount = 0;
        _walletClientMock
            .Setup(w => w.SignTransactionAsync(
                TestWalletAddress, It.IsAny<byte[]>(),
                It.IsAny<string?>(), true, It.IsAny<CancellationToken>()))
            .Returns<string, byte[], string?, bool, CancellationToken>((_, _, _, _, _) =>
            {
                callCount++;
                if (callCount == 1)
                    throw new InvalidOperationException("Wallet not found");
                return Task.FromResult(new WalletSignResult
                {
                    Signature = [1, 2, 3],
                    PublicKey = [4, 5, 6],
                    SignedBy = TestWalletAddress,
                    Algorithm = "ED25519"
                });
            });

        var service = CreateService();

        var result = await service.SignAsync(
            TestRegisterId, TestTxId, TestPayloadHash,
            "sorcha:register-control", "Genesis");

        result.Should().NotBeNull();
        // Wallet creation called twice (initial + recreation)
        _walletClientMock.Verify(
            w => w.CreateOrRetrieveSystemWalletAsync(TestValidatorId, It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task SignAsync_WalletUnavailableAfterRetry_ThrowsInvalidOperation()
    {
        _walletClientMock
            .Setup(w => w.SignTransactionAsync(
                TestWalletAddress, It.IsAny<byte[]>(),
                It.IsAny<string?>(), true, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Wallet not found"));

        var service = CreateService();

        var act = () => service.SignAsync(
            TestRegisterId, TestTxId, TestPayloadHash,
            "sorcha:register-control", "Genesis");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*unavailable after retry*");
    }

    // =========================================================================
    // Audit logging
    // =========================================================================

    [Fact]
    public async Task SignAsync_Success_EmitsAuditLog()
    {
        var service = CreateService();

        await service.SignAsync(
            TestRegisterId, TestTxId, TestPayloadHash,
            "sorcha:register-control", "Genesis");

        VerifyAuditLogEmitted("Success");
    }

    [Fact]
    public async Task SignAsync_WhitelistRejection_EmitsAuditLog()
    {
        var service = CreateService();

        try
        {
            await service.SignAsync(
                TestRegisterId, TestTxId, TestPayloadHash,
                "sorcha:unknown-path", "Genesis");
        }
        catch (InvalidOperationException) { }

        VerifyAuditLogEmitted("WhitelistRejected");
    }

    [Fact]
    public async Task SignAsync_RateLimit_EmitsAuditLog()
    {
        _options.MaxSignsPerRegisterPerMinute = 1;
        var service = CreateService();

        await service.SignAsync(
            TestRegisterId, TestTxId, TestPayloadHash,
            "sorcha:register-control", "Genesis");

        try
        {
            await service.SignAsync(
                TestRegisterId, TestTxId, TestPayloadHash,
                "sorcha:register-control", "Genesis");
        }
        catch (InvalidOperationException) { }

        VerifyAuditLogEmitted("RateLimited");
    }

    // =========================================================================
    // Concurrency
    // =========================================================================

    [Fact]
    public async Task SignAsync_ConcurrentCalls_ThreadSafe()
    {
        var service = CreateService();

        var tasks = Enumerable.Range(0, 5)
            .Select(i => service.SignAsync(
                $"register-{i}", TestTxId, TestPayloadHash,
                "sorcha:register-control", "Genesis"))
            .ToList();

        var results = await Task.WhenAll(tasks);

        results.Should().AllSatisfy(r =>
        {
            r.Should().NotBeNull();
            r.WalletAddress.Should().Be(TestWalletAddress);
        });

        // Wallet creation should be called exactly once despite concurrent access
        _walletClientMock.Verify(
            w => w.CreateOrRetrieveSystemWalletAsync(TestValidatorId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================================
    // Derivation path case insensitivity
    // =========================================================================

    [Fact]
    public async Task SignAsync_DerivationPathCaseInsensitive_Succeeds()
    {
        var service = CreateService();

        var act = () => service.SignAsync(
            TestRegisterId, TestTxId, TestPayloadHash,
            "SORCHA:REGISTER-CONTROL", "Genesis");

        await act.Should().NotThrowAsync();
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private void VerifyAuditLogEmitted(string expectedOutcome)
    {
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(expectedOutcome)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}
