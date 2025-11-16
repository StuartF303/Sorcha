using Sorcha.Wallet.Service.Domain.ValueObjects;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Sorcha.Wallet.Service.Api.Tests.Controllers;

public class WalletsControllerTests
{
    private readonly Mock<WalletManager> _mockWalletManager;
    private readonly Mock<ILogger<WalletsController>> _mockLogger;
    private readonly WalletsController _controller;

    public WalletsControllerTests()
    {
        _mockWalletManager = new Mock<WalletManager>(
            Mock.Of<KeyManagementService>(),
            Mock.Of<TransactionService>(),
            Mock.Of<DelegationService>(),
            Mock.Of<IWalletRepository>(),
            Mock.Of<IEventPublisher>(),
            Mock.Of<ILogger<WalletManager>>());

        _mockLogger = new Mock<ILogger<WalletsController>>();
        _controller = new WalletsController(_mockWalletManager.Object, _mockLogger.Object);

        // Setup controller context with user claims
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user"),
            new Claim("tenant", "test-tenant")
        }));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    #region CreateWallet Tests

    [Fact]
    public async Task CreateWallet_ShouldReturnCreatedResult_WhenValidRequest()
    {
        // Arrange
        var request = new CreateWalletRequest
        {
            Name = "Test Wallet",
            Algorithm = "ED25519",
            WordCount = 12
        };

        var wallet = new Wallet
        {
            Address = "ws1test123",
            Name = "Test Wallet",
            Algorithm = "ED25519",
            Owner = "test-user",
            Tenant = "test-tenant",
            CreatedAt = DateTime.UtcNow
        };

        var mnemonic = new Mnemonic("word1 word2 word3 word4 word5 word6 word7 word8 word9 word10 word11 word12");

        _mockWalletManager
            .Setup(x => x.CreateWalletAsync(
                request.Name,
                request.Algorithm,
                "test-user",
                "test-tenant",
                request.WordCount,
                request.Passphrase,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((wallet, mnemonic));

        // Act
        var result = await _controller.CreateWallet(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);

        var response = createdResult.Value.Should().BeOfType<CreateWalletResponse>().Subject;
        response.Wallet.Address.Should().Be("ws1test123");
        response.MnemonicWords.Should().HaveCount(12);
        response.MnemonicWords[0].Should().Be("word1");
    }

    [Fact]
    public async Task CreateWallet_ShouldReturnBadRequest_WhenArgumentException()
    {
        // Arrange
        var request = new CreateWalletRequest
        {
            Name = "Test Wallet",
            Algorithm = "INVALID",
            WordCount = 12
        };

        _mockWalletManager
            .Setup(x => x.CreateWalletAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid algorithm"));

        // Act
        var result = await _controller.CreateWallet(request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
        var problemDetails = badRequestResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problemDetails.Detail.Should().Contain("Invalid algorithm");
    }

    [Fact]
    public async Task CreateWallet_ShouldReturnProblem_WhenUnexpectedException()
    {
        // Arrange
        var request = new CreateWalletRequest
        {
            Name = "Test Wallet",
            Algorithm = "ED25519",
            WordCount = 12
        };

        _mockWalletManager
            .Setup(x => x.CreateWalletAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateWallet(request);

        // Assert
        var objectResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region RecoverWallet Tests

    [Fact]
    public async Task RecoverWallet_ShouldReturnOk_WhenValidRequest()
    {
        // Arrange
        var request = new RecoverWalletRequest
        {
            MnemonicWords = new[] { "word1", "word2", "word3", "word4", "word5", "word6", "word7", "word8", "word9", "word10", "word11", "word12" },
            Name = "Recovered Wallet",
            Algorithm = "ED25519"
        };

        var wallet = new Wallet
        {
            Address = "ws1recovered",
            Name = "Recovered Wallet",
            Algorithm = "ED25519",
            Owner = "test-user",
            Tenant = "test-tenant",
            CreatedAt = DateTime.UtcNow
        };

        _mockWalletManager
            .Setup(x => x.RecoverWalletAsync(
                It.IsAny<Mnemonic>(),
                request.Name,
                request.Algorithm,
                "test-user",
                "test-tenant",
                request.Passphrase,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        // Act
        var result = await _controller.RecoverWallet(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<WalletDto>().Subject;
        dto.Address.Should().Be("ws1recovered");
        dto.Name.Should().Be("Recovered Wallet");
    }

    [Fact]
    public async Task RecoverWallet_ShouldReturnConflict_WhenWalletAlreadyExists()
    {
        // Arrange
        var request = new RecoverWalletRequest
        {
            MnemonicWords = new[] { "word1", "word2", "word3", "word4", "word5", "word6", "word7", "word8", "word9", "word10", "word11", "word12" },
            Name = "Recovered Wallet",
            Algorithm = "ED25519"
        };

        _mockWalletManager
            .Setup(x => x.RecoverWalletAsync(
                It.IsAny<Mnemonic>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Wallet already exists"));

        // Act
        var result = await _controller.RecoverWallet(request);

        // Assert
        var conflictResult = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task RecoverWallet_ShouldReturnBadRequest_WhenInvalidMnemonic()
    {
        // Arrange
        var request = new RecoverWalletRequest
        {
            MnemonicWords = new[] { "invalid", "mnemonic" },
            Name = "Recovered Wallet",
            Algorithm = "ED25519"
        };

        _mockWalletManager
            .Setup(x => x.RecoverWalletAsync(
                It.IsAny<Mnemonic>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Invalid mnemonic"));

        // Act
        var result = await _controller.RecoverWallet(request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region GetWallet Tests

    [Fact]
    public async Task GetWallet_ShouldReturnOk_WhenWalletExists()
    {
        // Arrange
        var address = "ws1test123";
        var wallet = new Wallet
        {
            Address = address,
            Name = "Test Wallet",
            Algorithm = "ED25519",
            Owner = "test-user",
            Tenant = "test-tenant",
            CreatedAt = DateTime.UtcNow
        };

        _mockWalletManager
            .Setup(x => x.GetWalletAsync(address, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        // Act
        var result = await _controller.GetWallet(address);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<WalletDto>().Subject;
        dto.Address.Should().Be(address);
    }

    [Fact]
    public async Task GetWallet_ShouldReturnNotFound_WhenWalletDoesNotExist()
    {
        // Arrange
        var address = "ws1nonexistent";

        _mockWalletManager
            .Setup(x => x.GetWalletAsync(address, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wallet?)null);

        // Act
        var result = await _controller.GetWallet(address);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region ListWallets Tests

    [Fact]
    public async Task ListWallets_ShouldReturnOk_WithWalletsList()
    {
        // Arrange
        var wallets = new List<Wallet>
        {
            new Wallet
            {
                Address = "ws1test1",
                Name = "Wallet 1",
                Algorithm = "ED25519",
                Owner = "test-user",
                Tenant = "test-tenant",
                CreatedAt = DateTime.UtcNow
            },
            new Wallet
            {
                Address = "ws1test2",
                Name = "Wallet 2",
                Algorithm = "SECP256K1",
                Owner = "test-user",
                Tenant = "test-tenant",
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockWalletManager
            .Setup(x => x.GetWalletsByOwnerAsync("test-user", "test-tenant", It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallets);

        // Act
        var result = await _controller.ListWallets();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dtos = okResult.Value.Should().BeAssignableTo<IEnumerable<WalletDto>>().Subject;
        dtos.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListWallets_ShouldReturnEmptyList_WhenNoWallets()
    {
        // Arrange
        _mockWalletManager
            .Setup(x => x.GetWalletsByOwnerAsync("test-user", "test-tenant", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Wallet>());

        // Act
        var result = await _controller.ListWallets();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dtos = okResult.Value.Should().BeAssignableTo<IEnumerable<WalletDto>>().Subject;
        dtos.Should().BeEmpty();
    }

    #endregion

    #region UpdateWallet Tests

    [Fact]
    public async Task UpdateWallet_ShouldReturnOk_WhenValidRequest()
    {
        // Arrange
        var address = "ws1test123";
        var request = new UpdateWalletRequest
        {
            Name = "Updated Wallet",
            Tags = new Dictionary<string, string> { { "env", "production" } }
        };

        var updatedWallet = new Wallet
        {
            Address = address,
            Name = "Updated Wallet",
            Algorithm = "ED25519",
            Owner = "test-user",
            Tenant = "test-tenant",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        _mockWalletManager
            .Setup(x => x.UpdateWalletAsync(
                address,
                request.Name,
                request.Tags,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedWallet);

        // Act
        var result = await _controller.UpdateWallet(address, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<WalletDto>().Subject;
        dto.Name.Should().Be("Updated Wallet");
    }

    [Fact]
    public async Task UpdateWallet_ShouldReturnNotFound_WhenWalletDoesNotExist()
    {
        // Arrange
        var address = "ws1nonexistent";
        var request = new UpdateWalletRequest { Name = "Updated" };

        _mockWalletManager
            .Setup(x => x.UpdateWalletAsync(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<Dictionary<string, string>?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Wallet not found"));

        // Act
        var result = await _controller.UpdateWallet(address, request);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region DeleteWallet Tests

    [Fact]
    public async Task DeleteWallet_ShouldReturnNoContent_WhenSuccessful()
    {
        // Arrange
        var address = "ws1test123";

        _mockWalletManager
            .Setup(x => x.DeleteWalletAsync(address, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteWallet(address);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteWallet_ShouldReturnNotFound_WhenWalletDoesNotExist()
    {
        // Arrange
        var address = "ws1nonexistent";

        _mockWalletManager
            .Setup(x => x.DeleteWalletAsync(address, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Wallet not found"));

        // Act
        var result = await _controller.DeleteWallet(address);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region SignTransaction Tests

    [Fact]
    public async Task SignTransaction_ShouldReturnOk_WhenValidRequest()
    {
        // Arrange
        var address = "ws1test123";
        var transactionData = "dGVzdCBkYXRh"; // "test data" in base64
        var request = new SignTransactionRequest { TransactionData = transactionData };
        var signature = new byte[] { 1, 2, 3, 4, 5 };

        _mockWalletManager
            .Setup(x => x.SignTransactionAsync(
                address,
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(signature);

        // Act
        var result = await _controller.SignTransaction(address, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<SignTransactionResponse>().Subject;
        response.Signature.Should().NotBeNullOrEmpty();
        response.SignedBy.Should().Be(address);
        response.SignedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SignTransaction_ShouldReturnBadRequest_WhenInvalidBase64()
    {
        // Arrange
        var address = "ws1test123";
        var request = new SignTransactionRequest { TransactionData = "not-valid-base64!!!" };

        // Act
        var result = await _controller.SignTransaction(address, request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task SignTransaction_ShouldReturnNotFound_WhenWalletDoesNotExist()
    {
        // Arrange
        var address = "ws1nonexistent";
        var request = new SignTransactionRequest { TransactionData = "dGVzdA==" };

        _mockWalletManager
            .Setup(x => x.SignTransactionAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Wallet not found"));

        // Act
        var result = await _controller.SignTransaction(address, request);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    #endregion

    #region DecryptPayload Tests

    [Fact]
    public async Task DecryptPayload_ShouldReturnOk_WhenValidRequest()
    {
        // Arrange
        var address = "ws1test123";
        var encryptedPayload = "ZW5jcnlwdGVk"; // "encrypted" in base64
        var request = new DecryptPayloadRequest { EncryptedPayload = encryptedPayload };
        var decryptedData = System.Text.Encoding.UTF8.GetBytes("decrypted data");

        _mockWalletManager
            .Setup(x => x.DecryptPayloadAsync(
                address,
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(decryptedData);

        // Act
        var result = await _controller.DecryptPayload(address, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<DecryptPayloadResponse>().Subject;
        response.DecryptedPayload.Should().NotBeNullOrEmpty();
        response.DecryptedBy.Should().Be(address);
    }

    [Fact]
    public async Task DecryptPayload_ShouldReturnBadRequest_WhenInvalidBase64()
    {
        // Arrange
        var address = "ws1test123";
        var request = new DecryptPayloadRequest { EncryptedPayload = "invalid!!!" };

        // Act
        var result = await _controller.DecryptPayload(address, request);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region EncryptPayload Tests

    [Fact]
    public async Task EncryptPayload_ShouldReturnOk_WhenValidRequest()
    {
        // Arrange
        var address = "ws1test123";
        var payload = "dGVzdCBkYXRh"; // "test data" in base64
        var request = new EncryptPayloadRequest { Payload = payload };
        var encryptedData = new byte[] { 1, 2, 3, 4, 5 };

        _mockWalletManager
            .Setup(x => x.EncryptPayloadAsync(
                address,
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(encryptedData);

        // Act
        var result = await _controller.EncryptPayload(address, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<EncryptPayloadResponse>().Subject;
        response.EncryptedPayload.Should().NotBeNullOrEmpty();
        response.RecipientAddress.Should().Be(address);
    }

    [Fact]
    public async Task EncryptPayload_ShouldUseRecipientAddress_WhenProvidedInRequest()
    {
        // Arrange
        var address = "ws1test123";
        var recipientAddress = "ws1recipient";
        var payload = "dGVzdA==";
        var request = new EncryptPayloadRequest
        {
            Payload = payload,
            RecipientAddress = recipientAddress
        };
        var encryptedData = new byte[] { 1, 2, 3 };

        _mockWalletManager
            .Setup(x => x.EncryptPayloadAsync(
                recipientAddress,
                It.IsAny<byte[]>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(encryptedData);

        // Act
        var result = await _controller.EncryptPayload(address, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<EncryptPayloadResponse>().Subject;
        response.RecipientAddress.Should().Be(recipientAddress);
    }

    #endregion

    #region GenerateAddress Tests

    [Fact]
    public async Task GenerateAddress_ShouldReturnNotImplemented()
    {
        // Arrange
        var address = "ws1test123";
        var request = new GenerateAddressRequest();

        // Act
        var result = await _controller.GenerateAddress(address, request);

        // Assert
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(501);
    }

    #endregion
}
