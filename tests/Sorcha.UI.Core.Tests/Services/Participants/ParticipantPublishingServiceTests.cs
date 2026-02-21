// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Moq;
using Moq.Protected;
using Sorcha.UI.Core.Models.Participants;
using Sorcha.UI.Core.Services.Participants;
using Xunit;

namespace Sorcha.UI.Core.Tests.Services.Participants;

/// <summary>
/// Tests for ParticipantPublishingService HTTP calls.
/// </summary>
public class ParticipantPublishingServiceTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;
    private readonly ParticipantPublishingService _service;

    public ParticipantPublishingServiceTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost")
        };
        _service = new ParticipantPublishingService(_httpClient);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task PublishAsync_Success_ReturnsResult()
    {
        // Arrange
        var orgId = Guid.NewGuid();
        var expectedResult = new ParticipantPublishResultViewModel
        {
            TransactionId = "tx123",
            RegisterId = "reg456",
            ParticipantId = Guid.NewGuid(),
            Version = 1,
            Status = "Published"
        };

        SetupHandler(HttpStatusCode.OK, expectedResult);

        var request = new PublishParticipantViewModel
        {
            ParticipantId = expectedResult.ParticipantId,
            RegisterId = "reg456",
            Addresses = [new PublishAddressViewModel { WalletAddress = "w1", Primary = true }],
            SignerWalletAddress = "w1"
        };

        // Act
        var result = await _service.PublishAsync(orgId, request);

        // Assert
        result.TransactionId.Should().Be("tx123");
        result.Version.Should().Be(1);
    }

    [Fact]
    public async Task PublishAsync_ServerError_Throws()
    {
        // Arrange
        SetupHandler(HttpStatusCode.InternalServerError);

        var request = new PublishParticipantViewModel
        {
            ParticipantId = Guid.NewGuid(),
            RegisterId = "reg",
            Addresses = [],
            SignerWalletAddress = "w"
        };

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => _service.PublishAsync(Guid.NewGuid(), request));
    }

    [Fact]
    public async Task UpdatePublishedAsync_Success_ReturnsResult()
    {
        // Arrange
        var expectedResult = new ParticipantPublishResultViewModel
        {
            TransactionId = "tx-updated",
            Version = 2,
            Status = "Updated"
        };

        SetupHandler(HttpStatusCode.OK, expectedResult);

        var request = new PublishParticipantViewModel
        {
            ParticipantId = Guid.NewGuid(),
            RegisterId = "reg",
            Addresses = [],
            SignerWalletAddress = "w"
        };

        // Act
        var result = await _service.UpdatePublishedAsync(Guid.NewGuid(), Guid.NewGuid(), request);

        // Assert
        result.TransactionId.Should().Be("tx-updated");
        result.Version.Should().Be(2);
    }

    [Fact]
    public async Task RevokeAsync_Success_ReturnsTrue()
    {
        // Arrange
        SetupHandler(HttpStatusCode.OK);

        // Act
        var result = await _service.RevokeAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeAsync_NotFound_ReturnsFalse()
    {
        // Arrange
        SetupHandler(HttpStatusCode.NotFound);

        // Act
        var result = await _service.RevokeAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    private void SetupHandler(HttpStatusCode statusCode, object? content = null)
    {
        var response = new HttpResponseMessage(statusCode);
        if (content != null)
        {
            response.Content = new StringContent(
                JsonSerializer.Serialize(content),
                System.Text.Encoding.UTF8,
                "application/json");
        }

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }
}
