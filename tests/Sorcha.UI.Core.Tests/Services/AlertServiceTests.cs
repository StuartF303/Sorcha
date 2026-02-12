// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.UI.Core.Models.Admin;
using Sorcha.UI.Core.Services;
using Xunit;

namespace Sorcha.UI.Core.Tests.Services;

public class AlertServiceTests
{
    private static AlertService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };
        var logger = new Mock<ILogger<AlertService>>();
        return new AlertService(httpClient, logger.Object);
    }

    private static HttpMessageHandler CreateMockHandler(HttpStatusCode statusCode, object? content = null)
    {
        return new FakeHttpMessageHandler(statusCode, content);
    }

    [Fact]
    public async Task GetAlertsAsync_SuccessfulResponse_DeserializesCorrectly()
    {
        var expected = new AlertsResponse
        {
            Alerts =
            [
                new ServiceAlert
                {
                    Id = "validator-TotalFailed-warning",
                    Severity = AlertSeverity.Warning,
                    Source = "validator",
                    Message = "Validator has 15 failed validations",
                    MetricName = "TotalFailed",
                    CurrentValue = 15,
                    Threshold = 10,
                    Timestamp = DateTimeOffset.UtcNow
                }
            ],
            WarningCount = 1,
            TotalCount = 1,
            Timestamp = DateTimeOffset.UtcNow
        };

        var handler = CreateMockHandler(HttpStatusCode.OK, expected);
        var service = CreateService(handler);

        var result = await service.GetAlertsAsync();

        result.Alerts.Should().HaveCount(1);
        result.Alerts[0].Id.Should().Be("validator-TotalFailed-warning");
        result.Alerts[0].Severity.Should().Be(AlertSeverity.Warning);
    }

    [Fact]
    public async Task GetAlertsAsync_ServerError_ReturnsEmptyResponse()
    {
        var handler = CreateMockHandler(HttpStatusCode.InternalServerError);
        var service = CreateService(handler);

        var result = await service.GetAlertsAsync();

        result.Alerts.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAlertsAsync_DetectsNewAlerts_RaisesEvent()
    {
        var firstResponse = new AlertsResponse { Alerts = [], TotalCount = 0 };
        var secondResponse = new AlertsResponse
        {
            Alerts =
            [
                new ServiceAlert
                {
                    Id = "validator-TotalFailed-warning",
                    Severity = AlertSeverity.Warning,
                    Source = "validator",
                    Message = "test",
                    Timestamp = DateTimeOffset.UtcNow
                }
            ],
            WarningCount = 1,
            TotalCount = 1
        };

        var callCount = 0;
        var handler = new SequentialHttpMessageHandler(
        [
            (HttpStatusCode.OK, firstResponse),
            (HttpStatusCode.OK, secondResponse)
        ]);
        var service = CreateService(handler);

        AlertsChangedEventArgs? eventArgs = null;
        service.AlertsChanged += (_, args) => { eventArgs = args; callCount++; };

        // First call establishes baseline
        await service.GetAlertsAsync();
        // Second call should detect new alert
        await service.GetAlertsAsync();

        callCount.Should().Be(1);
        eventArgs.Should().NotBeNull();
        eventArgs!.NewAlerts.Should().HaveCount(1);
        eventArgs.NewAlerts[0].Id.Should().Be("validator-TotalFailed-warning");
    }

    [Fact]
    public async Task GetAlertsAsync_DetectsResolvedAlerts_RaisesEvent()
    {
        var firstResponse = new AlertsResponse
        {
            Alerts =
            [
                new ServiceAlert
                {
                    Id = "peer-unreachable",
                    Severity = AlertSeverity.Error,
                    Source = "peer",
                    Message = "Peer Service unreachable",
                    Timestamp = DateTimeOffset.UtcNow
                }
            ],
            ErrorCount = 1,
            TotalCount = 1
        };
        var secondResponse = new AlertsResponse { Alerts = [], TotalCount = 0 };

        var handler = new SequentialHttpMessageHandler(
        [
            (HttpStatusCode.OK, firstResponse),
            (HttpStatusCode.OK, secondResponse)
        ]);
        var service = CreateService(handler);

        AlertsChangedEventArgs? eventArgs = null;
        service.AlertsChanged += (_, args) => eventArgs = args;

        await service.GetAlertsAsync();
        await service.GetAlertsAsync();

        eventArgs.Should().NotBeNull();
        eventArgs!.ResolvedAlerts.Should().HaveCount(1);
        eventArgs.ResolvedAlerts[0].Id.Should().Be("peer-unreachable");
    }

    /// <summary>
    /// Simple fake handler that returns a canned response.
    /// </summary>
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string? _content;

        public FakeHttpMessageHandler(HttpStatusCode statusCode, object? content = null)
        {
            _statusCode = statusCode;
            _content = content != null ? JsonSerializer.Serialize(content) : null;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode);
            if (_content != null)
            {
                response.Content = new StringContent(_content, System.Text.Encoding.UTF8, "application/json");
            }
            return Task.FromResult(response);
        }
    }

    /// <summary>
    /// Handler that returns different responses on sequential calls.
    /// </summary>
    private class SequentialHttpMessageHandler : HttpMessageHandler
    {
        private readonly List<(HttpStatusCode StatusCode, object? Content)> _responses;
        private int _callIndex;

        public SequentialHttpMessageHandler(List<(HttpStatusCode, object?)> responses)
        {
            _responses = responses;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var (statusCode, content) = _responses[Math.Min(_callIndex, _responses.Count - 1)];
            _callIndex++;

            var response = new HttpResponseMessage(statusCode);
            if (content != null)
            {
                response.Content = new StringContent(
                    JsonSerializer.Serialize(content),
                    System.Text.Encoding.UTF8,
                    "application/json");
            }
            return Task.FromResult(response);
        }
    }
}
