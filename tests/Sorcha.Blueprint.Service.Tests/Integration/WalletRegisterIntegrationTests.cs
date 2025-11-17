// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Sorcha.Blueprint.Service.Clients;
using Sorcha.Register.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace Sorcha.Blueprint.Service.Tests.Integration;

/// <summary>
/// Integration tests for Wallet and Register service client integration
/// </summary>
public class WalletRegisterIntegrationTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WalletServiceClient> _walletLogger;
    private readonly ILogger<RegisterServiceClient> _registerLogger;

    public WalletRegisterIntegrationTests()
    {
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object)
        {
            BaseAddress = new Uri("http://localhost")
        };
        _walletLogger = Mock.Of<ILogger<WalletServiceClient>>();
        _registerLogger = Mock.Of<ILogger<RegisterServiceClient>>();
    }

    #region Wallet Service Client Tests

    [Fact]
    public async Task WalletServiceClient_EncryptPayload_Success()
    {
        // Arrange
        var client = new WalletServiceClient(_httpClient, _walletLogger);
        var recipientWallet = "wallet123";
        var payload = Encoding.UTF8.GetBytes("test payload");

        var expectedResponse = new
        {
            EncryptedPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes("encrypted-data")),
            RecipientAddress = recipientWallet,
            EncryptedAt = DateTime.UtcNow
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await client.EncryptPayloadAsync(recipientWallet, payload);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        Encoding.UTF8.GetString(result).Should().Be("encrypted-data");
    }

    [Fact]
    public async Task WalletServiceClient_EncryptPayload_ThrowsOnInvalidWallet()
    {
        // Arrange
        var client = new WalletServiceClient(_httpClient, _walletLogger);
        var payload = Encoding.UTF8.GetBytes("test payload");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.EncryptPayloadAsync("", payload));
    }

    [Fact]
    public async Task WalletServiceClient_DecryptPayload_Success()
    {
        // Arrange
        var client = new WalletServiceClient(_httpClient, _walletLogger);
        var walletAddress = "wallet123";
        var encryptedPayload = Encoding.UTF8.GetBytes("encrypted-data");

        var expectedResponse = new
        {
            DecryptedPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes("decrypted-data")),
            DecryptedBy = walletAddress,
            DecryptedAt = DateTime.UtcNow
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await client.DecryptPayloadAsync(walletAddress, encryptedPayload);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        Encoding.UTF8.GetString(result).Should().Be("decrypted-data");
    }

    [Fact]
    public async Task WalletServiceClient_SignTransaction_Success()
    {
        // Arrange
        var client = new WalletServiceClient(_httpClient, _walletLogger);
        var walletAddress = "wallet123";
        var transactionData = Encoding.UTF8.GetBytes("transaction-to-sign");

        var expectedResponse = new
        {
            Signature = Convert.ToBase64String(Encoding.UTF8.GetBytes("signature-data")),
            SignedBy = walletAddress,
            SignedAt = DateTime.UtcNow
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedResponse);

        // Act
        var result = await client.SignTransactionAsync(walletAddress, transactionData);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        Encoding.UTF8.GetString(result).Should().Be("signature-data");
    }

    [Fact]
    public async Task WalletServiceClient_GetWallet_Success()
    {
        // Arrange
        var client = new WalletServiceClient(_httpClient, _walletLogger);
        var walletAddress = "wallet123";

        var expectedWallet = new WalletInfo
        {
            Address = walletAddress,
            Name = "Test Wallet",
            PublicKey = "public-key-hex",
            Algorithm = "NIST P-256",
            Status = "Active",
            Owner = "user123",
            Tenant = "tenant123",
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            UpdatedAt = DateTime.UtcNow
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedWallet);

        // Act
        var result = await client.GetWalletAsync(walletAddress);

        // Assert
        result.Should().NotBeNull();
        result!.Address.Should().Be(walletAddress);
        result.Name.Should().Be("Test Wallet");
        result.Status.Should().Be("Active");
    }

    [Fact]
    public async Task WalletServiceClient_GetWallet_NotFound_ReturnsNull()
    {
        // Arrange
        var client = new WalletServiceClient(_httpClient, _walletLogger);
        var walletAddress = "nonexistent-wallet";

        SetupHttpResponse(HttpStatusCode.NotFound, null);

        // Act
        var result = await client.GetWalletAsync(walletAddress);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Register Service Client Tests

    [Fact]
    public async Task RegisterServiceClient_SubmitTransaction_Success()
    {
        // Arrange
        var client = new RegisterServiceClient(_httpClient, _registerLogger);
        var registerId = "register123";
        var transaction = new TransactionModel
        {
            TxId = "tx-123",
            RegisterId = registerId,
            SenderWallet = "wallet-sender",
            TimeStamp = DateTime.UtcNow
        };

        SetupHttpResponse(HttpStatusCode.Created, transaction);

        // Act
        var result = await client.SubmitTransactionAsync(registerId, transaction);

        // Assert
        result.Should().NotBeNull();
        result.TxId.Should().Be("tx-123");
        result.RegisterId.Should().Be(registerId);
    }

    [Fact]
    public async Task RegisterServiceClient_GetTransaction_Success()
    {
        // Arrange
        var client = new RegisterServiceClient(_httpClient, _registerLogger);
        var registerId = "register123";
        var transactionId = "tx-123";

        var expectedTransaction = new TransactionModel
        {
            TxId = transactionId,
            RegisterId = registerId,
            SenderWallet = "wallet-sender",
            TimeStamp = DateTime.UtcNow
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedTransaction);

        // Act
        var result = await client.GetTransactionAsync(registerId, transactionId);

        // Assert
        result.Should().NotBeNull();
        result!.TxId.Should().Be(transactionId);
        result.RegisterId.Should().Be(registerId);
    }

    [Fact]
    public async Task RegisterServiceClient_GetTransaction_NotFound_ReturnsNull()
    {
        // Arrange
        var client = new RegisterServiceClient(_httpClient, _registerLogger);
        var registerId = "register123";
        var transactionId = "nonexistent-tx";

        SetupHttpResponse(HttpStatusCode.NotFound, null);

        // Act
        var result = await client.GetTransactionAsync(registerId, transactionId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RegisterServiceClient_GetTransactions_Success()
    {
        // Arrange
        var client = new RegisterServiceClient(_httpClient, _registerLogger);
        var registerId = "register123";

        var expectedPage = new TransactionPage
        {
            Page = 1,
            PageSize = 20,
            Total = 50,
            Transactions = new List<TransactionModel>
            {
                new() { TxId = "tx-1", RegisterId = registerId, SenderWallet = "wallet1" },
                new() { TxId = "tx-2", RegisterId = registerId, SenderWallet = "wallet2" }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedPage);

        // Act
        var result = await client.GetTransactionsAsync(registerId, page: 1, pageSize: 20);

        // Assert
        result.Should().NotBeNull();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.Total.Should().Be(50);
        result.Transactions.Should().HaveCount(2);
    }

    [Fact]
    public async Task RegisterServiceClient_GetTransactionsByWallet_Success()
    {
        // Arrange
        var client = new RegisterServiceClient(_httpClient, _registerLogger);
        var registerId = "register123";
        var walletAddress = "wallet123";

        var expectedPage = new TransactionPage
        {
            Page = 1,
            PageSize = 20,
            Total = 10,
            Transactions = new List<TransactionModel>
            {
                new() { TxId = "tx-1", RegisterId = registerId, SenderWallet = walletAddress }
            }
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedPage);

        // Act
        var result = await client.GetTransactionsByWalletAsync(registerId, walletAddress);

        // Assert
        result.Should().NotBeNull();
        result.Total.Should().Be(10);
        result.Transactions.Should().HaveCount(1);
        result.Transactions[0].SenderWallet.Should().Be(walletAddress);
    }

    [Fact]
    public async Task RegisterServiceClient_GetRegister_Success()
    {
        // Arrange
        var client = new RegisterServiceClient(_httpClient, _registerLogger);
        var registerId = "register123";

        var expectedRegister = new Register.Models.Register
        {
            Id = registerId,
            Name = "Test Register",
            TenantId = "tenant123",
            Status = Register.Models.Enums.RegisterStatus.Active
        };

        SetupHttpResponse(HttpStatusCode.OK, expectedRegister);

        // Act
        var result = await client.GetRegisterAsync(registerId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(registerId);
        result.Name.Should().Be("Test Register");
    }

    [Fact]
    public async Task RegisterServiceClient_GetRegister_NotFound_ReturnsNull()
    {
        // Arrange
        var client = new RegisterServiceClient(_httpClient, _registerLogger);
        var registerId = "nonexistent-register";

        SetupHttpResponse(HttpStatusCode.NotFound, null);

        // Act
        var result = await client.GetRegisterAsync(registerId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region End-to-End Integration Tests

    [Fact]
    public async Task EndToEnd_PayloadEncryptionAndDecryption_WorksCorrectly()
    {
        // Arrange
        var walletClient = new WalletServiceClient(_httpClient, _walletLogger);
        var walletAddress = "wallet123";
        var originalPayload = Encoding.UTF8.GetBytes("sensitive data");

        // Mock encryption response
        var encryptedData = Convert.ToBase64String(Encoding.UTF8.GetBytes("encrypted-sensitive-data"));
        SetupHttpResponse(HttpStatusCode.OK, new
        {
            EncryptedPayload = encryptedData,
            RecipientAddress = walletAddress,
            EncryptedAt = DateTime.UtcNow
        });

        // Act 1: Encrypt
        var encrypted = await walletClient.EncryptPayloadAsync(walletAddress, originalPayload);

        // Mock decryption response
        SetupHttpResponse(HttpStatusCode.OK, new
        {
            DecryptedPayload = Convert.ToBase64String(originalPayload),
            DecryptedBy = walletAddress,
            DecryptedAt = DateTime.UtcNow
        });

        // Act 2: Decrypt
        var decrypted = await walletClient.DecryptPayloadAsync(walletAddress, encrypted);

        // Assert
        decrypted.Should().NotBeNull();
        decrypted.Should().BeEquivalentTo(originalPayload);
    }

    [Fact]
    public async Task EndToEnd_SubmitAndRetrieveTransaction_WorksCorrectly()
    {
        // Arrange
        var registerClient = new RegisterServiceClient(_httpClient, _registerLogger);
        var registerId = "register123";
        var transaction = new TransactionModel
        {
            TxId = "tx-456",
            RegisterId = registerId,
            SenderWallet = "wallet-sender",
            TimeStamp = DateTime.UtcNow
        };

        // Mock submission response
        SetupHttpResponse(HttpStatusCode.Created, transaction);

        // Act 1: Submit
        var submitted = await registerClient.SubmitTransactionAsync(registerId, transaction);

        // Mock retrieval response
        SetupHttpResponse(HttpStatusCode.OK, transaction);

        // Act 2: Retrieve
        var retrieved = await registerClient.GetTransactionAsync(registerId, "tx-456");

        // Assert
        submitted.Should().NotBeNull();
        retrieved.Should().NotBeNull();
        retrieved!.TxId.Should().Be("tx-456");
        retrieved.RegisterId.Should().Be(registerId);
    }

    #endregion

    #region Helper Methods

    private void SetupHttpResponse<T>(HttpStatusCode statusCode, T? content)
    {
        var response = new HttpResponseMessage(statusCode);

        if (content != null)
        {
            var json = JsonSerializer.Serialize(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    #endregion
}
