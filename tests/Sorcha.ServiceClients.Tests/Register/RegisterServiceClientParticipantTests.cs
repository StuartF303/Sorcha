// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Sorcha.ServiceClients.Auth;
using Sorcha.ServiceClients.Register;
using Sorcha.ServiceClients.Register.Models;

namespace Sorcha.ServiceClients.Tests.Register;

public class RegisterServiceClientParticipantTests
{
    private readonly Mock<IServiceAuthClient> _serviceAuthMock;
    private readonly Mock<ILogger<RegisterServiceClient>> _loggerMock;
    private readonly IConfiguration _configuration;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public RegisterServiceClientParticipantTests()
    {
        _serviceAuthMock = new Mock<IServiceAuthClient>();
        _serviceAuthMock.Setup(a => a.GetTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-token");

        _loggerMock = new Mock<ILogger<RegisterServiceClient>>();

        var configData = new Dictionary<string, string?>
        {
            ["ServiceClients:RegisterService:Address"] = "http://localhost:5290"
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    private RegisterServiceClient CreateClient(Mock<HttpMessageHandler> handlerMock)
    {
        var httpClient = new HttpClient(handlerMock.Object);
        return new RegisterServiceClient(httpClient, _serviceAuthMock.Object, _configuration, _loggerMock.Object);
    }

    private static Mock<HttpMessageHandler> CreateMockHandler(HttpStatusCode statusCode, object? responseBody = null)
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        var response = new HttpResponseMessage(statusCode);
        if (responseBody != null)
        {
            response.Content = new StringContent(
                JsonSerializer.Serialize(responseBody, JsonOptions),
                System.Text.Encoding.UTF8,
                "application/json");
        }

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        return handlerMock;
    }

    #region GetPublishedParticipantsAsync Tests

    [Fact]
    public async Task GetPublishedParticipantsAsync_SuccessfulQuery_ReturnsParticipantPage()
    {
        // Arrange
        var responseBody = new
        {
            page = 1,
            pageSize = 20,
            total = 1,
            participants = new[]
            {
                new
                {
                    participantId = "part-1",
                    organizationName = "Acme Corp",
                    participantName = "Alice",
                    status = "Active",
                    version = 1,
                    latestTxId = "tx-1",
                    addresses = new[]
                    {
                        new { walletAddress = "addr-1", publicKey = "key-1", algorithm = "ED25519", primary = true }
                    }
                }
            }
        };
        var handlerMock = CreateMockHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(handlerMock);

        // Act
        var result = await client.GetPublishedParticipantsAsync("reg-1");

        // Assert
        result.Should().NotBeNull();
        result.Total.Should().Be(1);
        result.Participants.Should().HaveCount(1);
        result.Participants[0].ParticipantId.Should().Be("part-1");
        result.Participants[0].ParticipantName.Should().Be("Alice");
    }

    [Fact]
    public async Task GetPublishedParticipantsAsync_EmptyRegister_ReturnsEmptyPage()
    {
        // Arrange
        var responseBody = new { page = 1, pageSize = 20, total = 0, participants = Array.Empty<object>() };
        var handlerMock = CreateMockHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(handlerMock);

        // Act
        var result = await client.GetPublishedParticipantsAsync("reg-1");

        // Assert
        result.Should().NotBeNull();
        result.Total.Should().Be(0);
        result.Participants.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPublishedParticipantsAsync_ServerError_ReturnsEmptyPage()
    {
        // Arrange
        var handlerMock = CreateMockHandler(HttpStatusCode.InternalServerError);
        var client = CreateClient(handlerMock);

        // Act
        var result = await client.GetPublishedParticipantsAsync("reg-1");

        // Assert
        result.Should().NotBeNull();
        result.Participants.Should().BeEmpty();
    }

    #endregion

    #region GetPublishedParticipantByAddressAsync Tests

    [Fact]
    public async Task GetPublishedParticipantByAddressAsync_Found_ReturnsRecord()
    {
        // Arrange
        var responseBody = new
        {
            participantId = "part-1",
            organizationName = "Acme Corp",
            participantName = "Alice",
            status = "Active",
            version = 1,
            latestTxId = "tx-1",
            addresses = new[]
            {
                new { walletAddress = "addr-1", publicKey = "key-1", algorithm = "ED25519", primary = true }
            }
        };
        var handlerMock = CreateMockHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(handlerMock);

        // Act
        var result = await client.GetPublishedParticipantByAddressAsync("reg-1", "addr-1");

        // Assert
        result.Should().NotBeNull();
        result!.ParticipantId.Should().Be("part-1");
        result.Addresses.Should().HaveCount(1);
        result.Addresses[0].WalletAddress.Should().Be("addr-1");
    }

    [Fact]
    public async Task GetPublishedParticipantByAddressAsync_NotFound_ReturnsNull()
    {
        // Arrange
        var handlerMock = CreateMockHandler(HttpStatusCode.NotFound);
        var client = CreateClient(handlerMock);

        // Act
        var result = await client.GetPublishedParticipantByAddressAsync("reg-1", "unknown");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPublishedParticipantByAddressAsync_HttpError_ReturnsNull()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));
        var client = CreateClient(handlerMock);

        // Act
        var result = await client.GetPublishedParticipantByAddressAsync("reg-1", "addr-1");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetPublishedParticipantByIdAsync Tests

    [Fact]
    public async Task GetPublishedParticipantByIdAsync_Found_ReturnsRecord()
    {
        // Arrange
        var responseBody = new
        {
            participantId = "part-1",
            organizationName = "Acme Corp",
            participantName = "Alice",
            status = "Active",
            version = 2,
            latestTxId = "tx-2",
            addresses = new[]
            {
                new { walletAddress = "addr-1", publicKey = "key-1", algorithm = "ED25519", primary = true },
                new { walletAddress = "addr-2", publicKey = "key-2", algorithm = "P-256", primary = false }
            }
        };
        var handlerMock = CreateMockHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(handlerMock);

        // Act
        var result = await client.GetPublishedParticipantByIdAsync("reg-1", "part-1");

        // Assert
        result.Should().NotBeNull();
        result!.ParticipantId.Should().Be("part-1");
        result.Version.Should().Be(2);
        result.Addresses.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPublishedParticipantByIdAsync_NotFound_ReturnsNull()
    {
        // Arrange
        var handlerMock = CreateMockHandler(HttpStatusCode.NotFound);
        var client = CreateClient(handlerMock);

        // Act
        var result = await client.GetPublishedParticipantByIdAsync("reg-1", "unknown");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPublishedParticipantByIdAsync_HttpError_ReturnsNull()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));
        var client = CreateClient(handlerMock);

        // Act
        var result = await client.GetPublishedParticipantByIdAsync("reg-1", "part-1");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ResolvePublicKeyAsync Tests

    [Fact]
    public async Task ResolvePublicKeyAsync_Found_ReturnsResolution()
    {
        // Arrange
        var responseBody = new
        {
            participantId = "part-1",
            participantName = "Alice",
            walletAddress = "addr-1",
            publicKey = "key-1",
            algorithm = "ED25519",
            status = "Active"
        };
        var handlerMock = CreateMockHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(handlerMock);

        // Act
        var result = await client.ResolvePublicKeyAsync("reg-1", "addr-1");

        // Assert
        result.Should().NotBeNull();
        result!.ParticipantId.Should().Be("part-1");
        result.PublicKey.Should().Be("key-1");
        result.Algorithm.Should().Be("ED25519");
    }

    [Fact]
    public async Task ResolvePublicKeyAsync_NotFound_ReturnsNull()
    {
        // Arrange
        var handlerMock = CreateMockHandler(HttpStatusCode.NotFound);
        var client = CreateClient(handlerMock);

        // Act
        var result = await client.ResolvePublicKeyAsync("reg-1", "unknown");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolvePublicKeyAsync_Revoked_ThrowsInvalidOperationException()
    {
        // Arrange
        var handlerMock = CreateMockHandler(HttpStatusCode.Gone);
        var client = CreateClient(handlerMock);

        // Act
        var act = async () => await client.ResolvePublicKeyAsync("reg-1", "addr-1");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*revoked*");
    }

    #endregion
}
