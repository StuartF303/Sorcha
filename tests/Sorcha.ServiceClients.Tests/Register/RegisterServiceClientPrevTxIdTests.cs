// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Sorcha.Register.Models;
using Sorcha.ServiceClients.Auth;
using Sorcha.ServiceClients.Register;

namespace Sorcha.ServiceClients.Tests.Register;

public class RegisterServiceClientPrevTxIdTests
{
    private readonly Mock<IServiceAuthClient> _serviceAuthMock;
    private readonly Mock<ILogger<RegisterServiceClient>> _loggerMock;
    private readonly IConfiguration _configuration;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public RegisterServiceClientPrevTxIdTests()
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

    [Fact]
    public async Task GetTransactionsByPrevTxIdAsync_SuccessfulQuery_ReturnsTransactionPage()
    {
        // Arrange
        var registerId = "test-register";
        var prevTxId = "prev-tx-001";
        var responseBody = new
        {
            items = new[]
            {
                new { txId = "tx-1", registerId, prevTxId, timeStamp = DateTime.UtcNow }
            },
            page = 1,
            pageSize = 20,
            totalCount = 1,
            totalPages = 1
        };

        var handlerMock = CreateMockHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(handlerMock);

        // Act
        var result = await client.GetTransactionsByPrevTxIdAsync(registerId, prevTxId);

        // Assert
        result.Should().NotBeNull();
        result.Total.Should().Be(1);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetTransactionsByPrevTxIdAsync_EmptyResult_ReturnsEmptyPage()
    {
        // Arrange
        var responseBody = new
        {
            items = Array.Empty<object>(),
            page = 1,
            pageSize = 20,
            totalCount = 0,
            totalPages = 0
        };

        var handlerMock = CreateMockHandler(HttpStatusCode.OK, responseBody);
        var client = CreateClient(handlerMock);

        // Act
        var result = await client.GetTransactionsByPrevTxIdAsync("test-register", "nonexistent-prev-tx");

        // Assert
        result.Should().NotBeNull();
        result.Total.Should().Be(0);
        result.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTransactionsByPrevTxIdAsync_NotFound_ReturnsEmptyPage()
    {
        // Arrange
        var handlerMock = CreateMockHandler(HttpStatusCode.NotFound);
        var client = CreateClient(handlerMock);

        // Act
        var result = await client.GetTransactionsByPrevTxIdAsync("test-register", "nonexistent-prev-tx");

        // Assert
        result.Should().NotBeNull();
        result.Total.Should().Be(0);
        result.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTransactionsByPrevTxIdAsync_ServerError_ThrowsHttpRequestException()
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
        var act = async () => await client.GetTransactionsByPrevTxIdAsync("test-register", "some-prev-tx");

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
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
}
