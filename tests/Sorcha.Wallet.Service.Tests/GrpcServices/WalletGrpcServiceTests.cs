// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Google.Protobuf;
using Grpc.Core;
using Sorcha.Wallet.Core.Repositories.Interfaces;
using Sorcha.Wallet.Service.GrpcServices;
using Sorcha.Wallet.Service.Protos;
using WalletEntity = Sorcha.Wallet.Core.Domain.Entities.Wallet;

namespace Sorcha.Wallet.Service.Tests.GrpcServices;

public class WalletGrpcServiceTests
{
    private readonly Mock<ILogger<WalletGrpcService>> _mockLogger;
    private readonly Mock<IWalletRepository> _mockRepository;
    private readonly Mock<IKeyManagementService> _mockKeyManagement;
    private readonly Mock<ICryptoModule> _mockCryptoModule;
    private readonly WalletGrpcService _service;
    private readonly ServerCallContext _context;

    private static readonly byte[] TestPublicKey = new byte[32];
    private static readonly byte[] TestPrivateKey = new byte[64];
    private static readonly byte[] TestDataHash = new byte[32];
    private static readonly byte[] TestSignature = new byte[64];

    public WalletGrpcServiceTests()
    {
        _mockLogger = new Mock<ILogger<WalletGrpcService>>();
        _mockRepository = new Mock<IWalletRepository>();
        _mockKeyManagement = new Mock<IKeyManagementService>();
        _mockCryptoModule = new Mock<ICryptoModule>();

        _service = new WalletGrpcService(
            _mockLogger.Object,
            _mockRepository.Object,
            _mockKeyManagement.Object,
            _mockCryptoModule.Object);

        _context = CreateTestContext();

        // Fill test data with non-zero values
        Random.Shared.NextBytes(TestPublicKey);
        Random.Shared.NextBytes(TestPrivateKey);
        Random.Shared.NextBytes(TestDataHash);
        Random.Shared.NextBytes(TestSignature);
    }

    private static ServerCallContext CreateTestContext()
    {
        return new TestServerCallContext();
    }

    private WalletEntity CreateTestWallet(
        string address = "ws11qtest123",
        string algorithm = "ED25519",
        WalletStatus status = WalletStatus.Active)
    {
        return new WalletEntity
        {
            Address = address,
            Algorithm = algorithm,
            EncryptedPrivateKey = "encrypted-key-data",
            EncryptionKeyId = "key-1",
            Owner = "user-1",
            Tenant = "tenant-1",
            Name = "Test Wallet",
            PublicKey = Convert.ToBase64String(TestPublicKey),
            Status = status,
            CreatedAt = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            Version = 1,
            Metadata = new Dictionary<string, string>
            {
                ["DerivationPath"] = "m/44'/0'/0'/0/0"
            }
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new WalletGrpcService(
            null!, _mockRepository.Object, _mockKeyManagement.Object, _mockCryptoModule.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        var act = () => new WalletGrpcService(
            _mockLogger.Object, null!, _mockKeyManagement.Object, _mockCryptoModule.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("repository");
    }

    [Fact]
    public void Constructor_NullKeyManagement_ThrowsArgumentNullException()
    {
        var act = () => new WalletGrpcService(
            _mockLogger.Object, _mockRepository.Object, null!, _mockCryptoModule.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("keyManagement");
    }

    [Fact]
    public void Constructor_NullCryptoModule_ThrowsArgumentNullException()
    {
        var act = () => new WalletGrpcService(
            _mockLogger.Object, _mockRepository.Object, _mockKeyManagement.Object, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("cryptoModule");
    }

    #endregion

    #region GetWalletDetails Tests

    [Fact]
    public async Task GetWalletDetails_ValidWallet_ReturnsDetails()
    {
        var wallet = CreateTestWallet();
        _mockRepository
            .Setup(r => r.GetByAddressAsync("ws11qtest123", false, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        var request = new GetWalletDetailsRequest { WalletId = "ws11qtest123" };

        var response = await _service.GetWalletDetails(request, _context);

        response.WalletId.Should().Be("ws11qtest123");
        response.Address.Should().Be("ws11qtest123");
        response.Algorithm.Should().Be(WalletAlgorithm.Ed25519);
        response.Version.Should().Be(1);
        response.DerivationPath.Should().Be("m/44'/0'/0'/0/0");
        response.PublicKey.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetWalletDetails_EmptyWalletId_ThrowsInvalidArgument()
    {
        var request = new GetWalletDetailsRequest { WalletId = "" };

        var act = async () => await _service.GetWalletDetails(request, _context);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task GetWalletDetails_WalletNotFound_ThrowsNotFound()
    {
        _mockRepository
            .Setup(r => r.GetByAddressAsync("nonexistent", false, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WalletEntity?)null);

        var request = new GetWalletDetailsRequest { WalletId = "nonexistent" };

        var act = async () => await _service.GetWalletDetails(request, _context);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Fact]
    public async Task GetWalletDetails_DatabaseFailure_ThrowsUnavailable()
    {
        _mockRepository
            .Setup(r => r.GetByAddressAsync(It.IsAny<string>(), false, false, false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Connection refused"));

        var request = new GetWalletDetailsRequest { WalletId = "ws11qtest123" };

        var act = async () => await _service.GetWalletDetails(request, _context);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.Unavailable);
    }

    [Fact]
    public async Task GetWalletDetails_NullPublicKey_ReturnsEmptyPublicKey()
    {
        var wallet = CreateTestWallet();
        wallet.PublicKey = null;
        _mockRepository
            .Setup(r => r.GetByAddressAsync("ws11qtest123", false, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        var request = new GetWalletDetailsRequest { WalletId = "ws11qtest123" };

        var response = await _service.GetWalletDetails(request, _context);

        response.PublicKey.Should().BeEmpty();
    }

    #endregion

    #region SignData Tests

    [Fact]
    public async Task SignData_ValidRequest_ReturnsSignature()
    {
        var wallet = CreateTestWallet();
        _mockRepository
            .Setup(r => r.GetByAddressAsync("ws11qtest123", false, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _mockKeyManagement
            .Setup(k => k.DecryptPrivateKeyAsync("encrypted-key-data", "key-1"))
            .ReturnsAsync(TestPrivateKey);

        _mockCryptoModule
            .Setup(c => c.SignAsync(It.IsAny<byte[]>(), (byte)WalletNetworks.ED25519, TestPrivateKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CryptoResult<byte[]>.Success(TestSignature));

        var request = new SignDataRequest
        {
            WalletId = "ws11qtest123",
            DataHash = ByteString.CopyFrom(TestDataHash)
        };

        var response = await _service.SignData(request, _context);

        response.Signature.Should().NotBeEmpty();
        response.Signature.ToByteArray().Should().BeEquivalentTo(TestSignature);
        response.Algorithm.Should().Be(WalletAlgorithm.Ed25519);
        response.Version.Should().Be(1);
        response.PublicKey.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SignData_WithDerivationPath_UsesDerivedKey()
    {
        var wallet = CreateTestWallet();
        var derivedPrivate = new byte[64];
        var derivedPublic = new byte[32];
        Random.Shared.NextBytes(derivedPrivate);
        Random.Shared.NextBytes(derivedPublic);

        _mockRepository
            .Setup(r => r.GetByAddressAsync("ws11qtest123", false, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _mockKeyManagement
            .Setup(k => k.DecryptPrivateKeyAsync("encrypted-key-data", "key-1"))
            .ReturnsAsync(TestPrivateKey);

        _mockKeyManagement
            .Setup(k => k.DeriveKeyAtPathAsync(TestPrivateKey, It.IsAny<DerivationPath>(), "ED25519"))
            .ReturnsAsync((derivedPrivate, derivedPublic));

        _mockCryptoModule
            .Setup(c => c.SignAsync(It.IsAny<byte[]>(), (byte)WalletNetworks.ED25519, derivedPrivate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CryptoResult<byte[]>.Success(TestSignature));

        var request = new SignDataRequest
        {
            WalletId = "ws11qtest123",
            DataHash = ByteString.CopyFrom(TestDataHash),
            DerivationPath = "m/44'/0'/0'/0/5"
        };

        var response = await _service.SignData(request, _context);

        response.Signature.Should().NotBeEmpty();
        response.PublicKey.ToByteArray().Should().BeEquivalentTo(derivedPublic);
        _mockKeyManagement.Verify(k => k.DeriveKeyAtPathAsync(
            TestPrivateKey, It.IsAny<DerivationPath>(), "ED25519"), Times.Once);
    }

    [Fact]
    public async Task SignData_EmptyWalletId_ThrowsInvalidArgument()
    {
        var request = new SignDataRequest
        {
            WalletId = "",
            DataHash = ByteString.CopyFrom(TestDataHash)
        };

        var act = async () => await _service.SignData(request, _context);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task SignData_InvalidHashLength_ThrowsInvalidArgument()
    {
        var request = new SignDataRequest
        {
            WalletId = "ws11qtest123",
            DataHash = ByteString.CopyFrom(new byte[16]) // Not 32 bytes
        };

        var act = async () => await _service.SignData(request, _context);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
        ex.Which.Status.Detail.Should().Contain("32 bytes");
    }

    [Fact]
    public async Task SignData_WalletNotFound_ThrowsNotFound()
    {
        _mockRepository
            .Setup(r => r.GetByAddressAsync("nonexistent", false, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WalletEntity?)null);

        var request = new SignDataRequest
        {
            WalletId = "nonexistent",
            DataHash = ByteString.CopyFrom(TestDataHash)
        };

        var act = async () => await _service.SignData(request, _context);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Theory]
    [InlineData(WalletStatus.Locked)]
    [InlineData(WalletStatus.Deleted)]
    [InlineData(WalletStatus.Archived)]
    public async Task SignData_InactiveWallet_ThrowsFailedPrecondition(WalletStatus status)
    {
        var wallet = CreateTestWallet(status: status);
        _mockRepository
            .Setup(r => r.GetByAddressAsync("ws11qtest123", false, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        var request = new SignDataRequest
        {
            WalletId = "ws11qtest123",
            DataHash = ByteString.CopyFrom(TestDataHash)
        };

        var act = async () => await _service.SignData(request, _context);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.FailedPrecondition);
    }

    [Fact]
    public async Task SignData_SigningFails_ThrowsInternal()
    {
        var wallet = CreateTestWallet();
        _mockRepository
            .Setup(r => r.GetByAddressAsync("ws11qtest123", false, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _mockKeyManagement
            .Setup(k => k.DecryptPrivateKeyAsync("encrypted-key-data", "key-1"))
            .ReturnsAsync(TestPrivateKey);

        _mockCryptoModule
            .Setup(c => c.SignAsync(It.IsAny<byte[]>(), It.IsAny<byte>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CryptoResult<byte[]>.Failure(CryptoStatus.SigningFailed, "Key error"));

        var request = new SignDataRequest
        {
            WalletId = "ws11qtest123",
            DataHash = ByteString.CopyFrom(TestDataHash)
        };

        var act = async () => await _service.SignData(request, _context);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.Internal);
    }

    [Fact]
    public async Task SignData_InvalidDerivationPath_ThrowsInvalidArgument()
    {
        var wallet = CreateTestWallet();
        _mockRepository
            .Setup(r => r.GetByAddressAsync("ws11qtest123", false, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _mockKeyManagement
            .Setup(k => k.DecryptPrivateKeyAsync("encrypted-key-data", "key-1"))
            .ReturnsAsync(TestPrivateKey);

        var request = new SignDataRequest
        {
            WalletId = "ws11qtest123",
            DataHash = ByteString.CopyFrom(TestDataHash),
            DerivationPath = "invalid/path/format"
        };

        var act = async () => await _service.SignData(request, _context);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    #endregion

    #region VerifySignature Tests

    [Fact]
    public async Task VerifySignature_ValidSignature_ReturnsTrue()
    {
        _mockCryptoModule
            .Setup(c => c.VerifyAsync(
                It.IsAny<byte[]>(), It.IsAny<byte[]>(),
                (byte)WalletNetworks.ED25519, It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CryptoStatus.Success);

        var request = new VerifySignatureRequest
        {
            Signature = ByteString.CopyFrom(TestSignature),
            DataHash = ByteString.CopyFrom(TestDataHash),
            PublicKey = ByteString.CopyFrom(TestPublicKey),
            Algorithm = WalletAlgorithm.Ed25519
        };

        var response = await _service.VerifySignature(request, _context);

        response.IsValid.Should().BeTrue();
        response.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task VerifySignature_InvalidSignature_ReturnsFalse()
    {
        _mockCryptoModule
            .Setup(c => c.VerifyAsync(
                It.IsAny<byte[]>(), It.IsAny<byte[]>(),
                (byte)WalletNetworks.ED25519, It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CryptoStatus.InvalidSignature);

        var request = new VerifySignatureRequest
        {
            Signature = ByteString.CopyFrom(TestSignature),
            DataHash = ByteString.CopyFrom(TestDataHash),
            PublicKey = ByteString.CopyFrom(TestPublicKey),
            Algorithm = WalletAlgorithm.Ed25519
        };

        var response = await _service.VerifySignature(request, _context);

        response.IsValid.Should().BeFalse();
        response.ErrorMessage.Should().NotBeEmpty();
    }

    [Fact]
    public async Task VerifySignature_EmptySignature_ThrowsInvalidArgument()
    {
        var request = new VerifySignatureRequest
        {
            Signature = ByteString.Empty,
            DataHash = ByteString.CopyFrom(TestDataHash),
            PublicKey = ByteString.CopyFrom(TestPublicKey),
            Algorithm = WalletAlgorithm.Ed25519
        };

        var act = async () => await _service.VerifySignature(request, _context);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task VerifySignature_InvalidHashLength_ThrowsInvalidArgument()
    {
        var request = new VerifySignatureRequest
        {
            Signature = ByteString.CopyFrom(TestSignature),
            DataHash = ByteString.CopyFrom(new byte[16]),
            PublicKey = ByteString.CopyFrom(TestPublicKey),
            Algorithm = WalletAlgorithm.Ed25519
        };

        var act = async () => await _service.VerifySignature(request, _context);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
        ex.Which.Status.Detail.Should().Contain("32 bytes");
    }

    [Fact]
    public async Task VerifySignature_EmptyPublicKey_ThrowsInvalidArgument()
    {
        var request = new VerifySignatureRequest
        {
            Signature = ByteString.CopyFrom(TestSignature),
            DataHash = ByteString.CopyFrom(TestDataHash),
            PublicKey = ByteString.Empty,
            Algorithm = WalletAlgorithm.Ed25519
        };

        var act = async () => await _service.VerifySignature(request, _context);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task VerifySignature_UnspecifiedAlgorithm_ThrowsInvalidArgument()
    {
        var request = new VerifySignatureRequest
        {
            Signature = ByteString.CopyFrom(TestSignature),
            DataHash = ByteString.CopyFrom(TestDataHash),
            PublicKey = ByteString.CopyFrom(TestPublicKey),
            Algorithm = WalletAlgorithm.Unspecified
        };

        var act = async () => await _service.VerifySignature(request, _context);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task VerifySignature_CryptoModuleThrows_ThrowsInternal()
    {
        _mockCryptoModule
            .Setup(c => c.VerifyAsync(
                It.IsAny<byte[]>(), It.IsAny<byte[]>(),
                It.IsAny<byte>(), It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Crypto engine failure"));

        var request = new VerifySignatureRequest
        {
            Signature = ByteString.CopyFrom(TestSignature),
            DataHash = ByteString.CopyFrom(TestDataHash),
            PublicKey = ByteString.CopyFrom(TestPublicKey),
            Algorithm = WalletAlgorithm.Ed25519
        };

        var act = async () => await _service.VerifySignature(request, _context);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.Internal);
    }

    #endregion

    #region GetDerivedKey Tests

    [Fact]
    public async Task GetDerivedKey_ValidRequest_ReturnsDerivedKey()
    {
        var wallet = CreateTestWallet();
        var derivedPrivate = new byte[64];
        var derivedPublic = new byte[32];
        Random.Shared.NextBytes(derivedPrivate);
        Random.Shared.NextBytes(derivedPublic);

        _mockRepository
            .Setup(r => r.GetByAddressAsync("ws11qtest123", false, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        _mockKeyManagement
            .Setup(k => k.DecryptPrivateKeyAsync("encrypted-key-data", "key-1"))
            .ReturnsAsync(TestPrivateKey);

        _mockKeyManagement
            .Setup(k => k.DeriveKeyAtPathAsync(TestPrivateKey, It.IsAny<DerivationPath>(), "ED25519"))
            .ReturnsAsync((derivedPrivate, derivedPublic));

        var request = new GetDerivedKeyRequest
        {
            WalletId = "ws11qtest123",
            DerivationPath = "m/44'/0'/0'/0/0"
        };

        var response = await _service.GetDerivedKey(request, _context);

        response.PrivateKey.ToByteArray().Should().BeEquivalentTo(derivedPrivate);
        response.PublicKey.ToByteArray().Should().BeEquivalentTo(derivedPublic);
        response.Algorithm.Should().Be(WalletAlgorithm.Ed25519);
        response.DerivationPath.Should().Contain("44");
    }

    [Fact]
    public async Task GetDerivedKey_EmptyWalletId_ThrowsInvalidArgument()
    {
        var request = new GetDerivedKeyRequest
        {
            WalletId = "",
            DerivationPath = "m/44'/0'/0'/0/0"
        };

        var act = async () => await _service.GetDerivedKey(request, _context);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task GetDerivedKey_EmptyDerivationPath_ThrowsInvalidArgument()
    {
        var request = new GetDerivedKeyRequest
        {
            WalletId = "ws11qtest123",
            DerivationPath = ""
        };

        var act = async () => await _service.GetDerivedKey(request, _context);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task GetDerivedKey_InvalidPathFormat_ThrowsInvalidArgument()
    {
        var request = new GetDerivedKeyRequest
        {
            WalletId = "ws11qtest123",
            DerivationPath = "not-a-valid-path!!!"
        };

        var act = async () => await _service.GetDerivedKey(request, _context);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task GetDerivedKey_WalletNotFound_ThrowsNotFound()
    {
        _mockRepository
            .Setup(r => r.GetByAddressAsync("nonexistent", false, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WalletEntity?)null);

        var request = new GetDerivedKeyRequest
        {
            WalletId = "nonexistent",
            DerivationPath = "m/44'/0'/0'/0/0"
        };

        var act = async () => await _service.GetDerivedKey(request, _context);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Theory]
    [InlineData(WalletStatus.Locked)]
    [InlineData(WalletStatus.Deleted)]
    [InlineData(WalletStatus.Archived)]
    public async Task GetDerivedKey_InactiveWallet_ThrowsFailedPrecondition(WalletStatus status)
    {
        var wallet = CreateTestWallet(status: status);
        _mockRepository
            .Setup(r => r.GetByAddressAsync("ws11qtest123", false, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        var request = new GetDerivedKeyRequest
        {
            WalletId = "ws11qtest123",
            DerivationPath = "m/44'/0'/0'/0/0"
        };

        var act = async () => await _service.GetDerivedKey(request, _context);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.FailedPrecondition);
    }

    [Fact]
    public async Task GetDerivedKey_NoEncryptedKey_ThrowsFailedPrecondition()
    {
        var wallet = CreateTestWallet();
        wallet.EncryptedPrivateKey = "";
        _mockRepository
            .Setup(r => r.GetByAddressAsync("ws11qtest123", false, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        var request = new GetDerivedKeyRequest
        {
            WalletId = "ws11qtest123",
            DerivationPath = "m/44'/0'/0'/0/0"
        };

        var act = async () => await _service.GetDerivedKey(request, _context);

        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.FailedPrecondition);
    }

    #endregion

    #region Algorithm Mapping Tests

    [Fact]
    public async Task GetWalletDetails_NistP256Algorithm_MapsCorrectly()
    {
        var wallet = CreateTestWallet(algorithm: "NISTP256");
        _mockRepository
            .Setup(r => r.GetByAddressAsync("ws11qtest123", false, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        var request = new GetWalletDetailsRequest { WalletId = "ws11qtest123" };

        var response = await _service.GetWalletDetails(request, _context);

        response.Algorithm.Should().Be(WalletAlgorithm.Nistp256);
    }

    [Fact]
    public async Task GetWalletDetails_Rsa4096Algorithm_MapsCorrectly()
    {
        var wallet = CreateTestWallet(algorithm: "RSA4096");
        _mockRepository
            .Setup(r => r.GetByAddressAsync("ws11qtest123", false, false, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        var request = new GetWalletDetailsRequest { WalletId = "ws11qtest123" };

        var response = await _service.GetWalletDetails(request, _context);

        response.Algorithm.Should().Be(WalletAlgorithm.Rsa4096);
    }

    #endregion
}

/// <summary>
/// Minimal ServerCallContext implementation for unit testing gRPC services.
/// </summary>
internal class TestServerCallContext : ServerCallContext
{
    protected override string MethodCore => "TestMethod";
    protected override string HostCore => "localhost";
    protected override string PeerCore => "test-peer";
    protected override DateTime DeadlineCore => DateTime.MaxValue;
    protected override Metadata RequestHeadersCore => new();
    protected override CancellationToken CancellationTokenCore => CancellationToken.None;
    protected override Metadata ResponseTrailersCore => new();
    protected override Status StatusCore { get; set; }
    protected override WriteOptions? WriteOptionsCore { get; set; }
    protected override AuthContext AuthContextCore => new(string.Empty, new Dictionary<string, List<AuthProperty>>());

    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
        throw new NotImplementedException();

    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) =>
        Task.CompletedTask;
}
