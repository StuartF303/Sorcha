// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Sorcha.McpServer.Infrastructure;
using Sorcha.McpServer.Services;
using Sorcha.McpServer.Tools.Participant;

namespace Sorcha.McpServer.Tests.Tools.Participant;

public sealed class TransactionHistoryToolTests
{
    private readonly Mock<IMcpSessionService> _sessionServiceMock;
    private readonly Mock<IMcpAuthorizationService> _authServiceMock;
    private readonly Mock<IMcpErrorHandler> _errorHandlerMock;
    private readonly Mock<IServiceAvailabilityTracker> _availabilityTrackerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<TransactionHistoryTool>> _loggerMock;
    private readonly IConfiguration _configuration;
    private readonly TransactionHistoryTool _tool;

    public TransactionHistoryToolTests()
    {
        _sessionServiceMock = new Mock<IMcpSessionService>();
        _authServiceMock = new Mock<IMcpAuthorizationService>();
        _errorHandlerMock = new Mock<IMcpErrorHandler>();
        _availabilityTrackerMock = new Mock<IServiceAvailabilityTracker>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<TransactionHistoryTool>>();

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceClients:RegisterService:Address"] = "http://localhost:5290"
            })
            .Build();

        _tool = new TransactionHistoryTool(
            _sessionServiceMock.Object,
            _authServiceMock.Object,
            _errorHandlerMock.Object,
            _availabilityTrackerMock.Object,
            _httpClientFactoryMock.Object,
            _configuration,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_WhenUnauthorized_ReturnsUnauthorizedStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_transaction_history")).Returns(false);

        // Act
        var result = await _tool.GetTransactionHistoryAsync();

        // Assert
        result.Status.Should().Be("Unauthorized");
        result.Message.Should().Contain("sorcha:participant");
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_WhenServiceUnavailable_ReturnsUnavailableStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_transaction_history")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Register")).Returns(false);

        // Act
        var result = await _tool.GetTransactionHistoryAsync();

        // Assert
        result.Status.Should().Be("Unavailable");
        result.Message.Should().Contain("Register service");
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_WithSuccessfulResponse_ReturnsTransactions()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_transaction_history")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Register")).Returns(true);

        var response = new
        {
            items = new[]
            {
                new
                {
                    transactionId = "tx-1",
                    registerId = "register-123",
                    workflowInstanceId = "wf-1",
                    actionId = 1,
                    transactionType = "Action",
                    submitter = "addr-1",
                    timestamp = DateTimeOffset.UtcNow.AddHours(-2),
                    signature = "sig1..."
                },
                new
                {
                    transactionId = "tx-2",
                    registerId = "register-123",
                    workflowInstanceId = "wf-1",
                    actionId = 2,
                    transactionType = "Action",
                    submitter = "addr-2",
                    timestamp = DateTimeOffset.UtcNow.AddHours(-1),
                    signature = "sig2..."
                }
            },
            totalCount = 2,
            page = 1,
            pageSize = 20,
            totalPages = 1
        };

        SetupHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(response));

        // Act
        var result = await _tool.GetTransactionHistoryAsync();

        // Assert
        result.Status.Should().Be("Success");
        result.Transactions.Should().HaveCount(2);
        result.Transactions[0].TransactionId.Should().Be("tx-1");
        result.Transactions[0].TransactionType.Should().Be("Action");
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_WithEmptyHistory_ReturnsSuccessWithEmptyList()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_transaction_history")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Register")).Returns(true);

        var response = new { items = Array.Empty<object>(), totalCount = 0, page = 1, pageSize = 20, totalPages = 0 };
        SetupHttpClient(HttpStatusCode.OK, JsonSerializer.Serialize(response));

        // Act
        var result = await _tool.GetTransactionHistoryAsync();

        // Assert
        result.Status.Should().Be("Success");
        result.Transactions.Should().BeEmpty();
        result.Message.Should().Contain("0 transaction");
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_WithWorkflowFilter_PassesCorrectParameter()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_transaction_history")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Register")).Returns(true);

        var response = new { items = Array.Empty<object>(), totalCount = 0, page = 1, pageSize = 20, totalPages = 0 };
        var handlerMock = SetupHttpClientWithCapture(HttpStatusCode.OK, JsonSerializer.Serialize(response));

        // Act
        await _tool.GetTransactionHistoryAsync(workflowInstanceId: "wf-456");

        // Assert
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.ToString().Contains("workflowInstanceId=wf-456")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_WithRegisterFilter_PassesCorrectParameter()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_transaction_history")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Register")).Returns(true);

        var response = new { items = Array.Empty<object>(), totalCount = 0, page = 1, pageSize = 20, totalPages = 0 };
        var handlerMock = SetupHttpClientWithCapture(HttpStatusCode.OK, JsonSerializer.Serialize(response));

        // Act
        await _tool.GetTransactionHistoryAsync(registerId: "reg-789");

        // Assert
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.ToString().Contains("registerId=reg-789")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_WithPagination_PassesCorrectParameters()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_transaction_history")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Register")).Returns(true);

        var response = new { items = Array.Empty<object>(), totalCount = 0, page = 3, pageSize = 25, totalPages = 0 };
        var handlerMock = SetupHttpClientWithCapture(HttpStatusCode.OK, JsonSerializer.Serialize(response));

        // Act
        await _tool.GetTransactionHistoryAsync(page: 3, pageSize: 25);

        // Assert
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.RequestUri!.ToString().Contains("page=3") &&
                req.RequestUri.ToString().Contains("pageSize=25")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_WithTimeout_ReturnsTimeoutStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_transaction_history")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Register")).Returns(true);

        SetupHttpClientWithException(new TaskCanceledException());

        // Act
        var result = await _tool.GetTransactionHistoryAsync();

        // Assert
        result.Status.Should().Be("Timeout");
        _availabilityTrackerMock.Verify(x => x.RecordFailure("Register"), Times.Once);
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_WithHttpException_ReturnsErrorStatus()
    {
        // Arrange
        _authServiceMock.Setup(x => x.CanInvokeTool("sorcha_transaction_history")).Returns(true);
        _availabilityTrackerMock.Setup(x => x.IsServiceAvailable("Register")).Returns(true);

        SetupHttpClientWithException(new HttpRequestException("Connection refused"));

        // Act
        var result = await _tool.GetTransactionHistoryAsync();

        // Assert
        result.Status.Should().Be("Error");
        result.Message.Should().Contain("Connection refused");
    }

    private void SetupHttpClient(HttpStatusCode statusCode, string content)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });

        var client = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
    }

    private Mock<HttpMessageHandler> SetupHttpClientWithCapture(HttpStatusCode statusCode, string content)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });

        var client = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
        return handlerMock;
    }

    private void SetupHttpClientWithException(Exception exception)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(exception);

        var client = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
    }
}
