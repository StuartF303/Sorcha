// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Sorcha.UI.Core.Models.SchemaLibrary;
using Sorcha.UI.Core.Services;
using Xunit;

namespace Sorcha.UI.Core.Tests.Services;

public class SchemaLibraryApiServiceTests
{
    private static SchemaLibraryApiService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };
        var logger = new Mock<ILogger<SchemaLibraryApiService>>();
        return new SchemaLibraryApiService(httpClient, logger.Object);
    }

    private static HttpMessageHandler CreateMockHandler(HttpStatusCode statusCode, object? content = null)
    {
        return new FakeHttpMessageHandler(statusCode, content);
    }

    [Fact]
    public async Task SearchAsync_ReturnsResults()
    {
        var expected = new SchemaIndexSearchResponse(
            [new SchemaIndexEntryViewModel(
                "sc001", "SchemaStore", "http://example.com/schema.json",
                "Test Schema", "A test schema", ["general"],
                5, 2, "1.0.0", "Active", DateTimeOffset.UtcNow)],
            1, null, null);

        var handler = CreateMockHandler(HttpStatusCode.OK, expected);
        var service = CreateService(handler);

        var result = await service.SearchAsync(search: "test");

        result.Results.Should().HaveCount(1);
        result.Results[0].Title.Should().Be("Test Schema");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task SearchAsync_WithFilters_BuildsCorrectQueryString()
    {
        var expected = new SchemaIndexSearchResponse([], 0, null, null);
        var handler = new RequestCapturingHandler(HttpStatusCode.OK, expected);
        var service = CreateService(handler);

        await service.SearchAsync(search: "invoice", sectors: ["finance", "commerce"], provider: "SchemaStore", limit: 10);

        handler.LastRequestUri.Should().Contain("search=invoice");
        handler.LastRequestUri.Should().Contain("sectors=finance%2Ccommerce");
        handler.LastRequestUri.Should().Contain("provider=SchemaStore");
        handler.LastRequestUri.Should().Contain("limit=10");
    }

    [Fact]
    public async Task SearchAsync_WithCursor_IncludesCursorParam()
    {
        var expected = new SchemaIndexSearchResponse([], 0, null, null);
        var handler = new RequestCapturingHandler(HttpStatusCode.OK, expected);
        var service = CreateService(handler);

        await service.SearchAsync(cursor: "abc123");

        handler.LastRequestUri.Should().Contain("cursor=abc123");
    }

    [Fact]
    public async Task SearchAsync_Error_ReturnsEmpty()
    {
        var handler = CreateMockHandler(HttpStatusCode.InternalServerError);
        var service = CreateService(handler);

        var result = await service.SearchAsync();

        result.Results.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetDetailAsync_ReturnsDetail()
    {
        var expected = new SchemaIndexEntryDetailViewModel(
            "sc001", "SchemaStore", "http://example.com/schema.json",
            "Test Schema", "A test schema", ["general"],
            5, 2, "1.0.0", "Active", DateTimeOffset.UtcNow,
            ["name", "age"], ["name"], ["test"], null);

        var handler = CreateMockHandler(HttpStatusCode.OK, expected);
        var service = CreateService(handler);

        var result = await service.GetDetailAsync("sc001");

        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Schema");
        result.FieldNames.Should().Contain("name");
    }

    [Fact]
    public async Task GetDetailAsync_NotFound_ReturnsNull()
    {
        var handler = CreateMockHandler(HttpStatusCode.NotFound);
        var service = CreateService(handler);

        var result = await service.GetDetailAsync("unknown-sc");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetContentAsync_ReturnsJsonDocument()
    {
        var schema = JsonDocument.Parse("""{ "type": "object", "properties": { "name": { "type": "string" } } }""");
        var handler = CreateMockHandler(HttpStatusCode.OK, schema.RootElement);
        var service = CreateService(handler);

        var result = await service.GetContentAsync("sc001");

        result.Should().NotBeNull();
        result!.RootElement.GetProperty("type").GetString().Should().Be("object");
    }

    [Fact]
    public async Task GetSectorsAsync_ReturnsSectors()
    {
        var expected = new List<SchemaSectorViewModel>
        {
            new("finance", "Finance", "Financial services schemas", "finance"),
            new("healthcare", "Healthcare", "Healthcare schemas", "healthcare")
        };

        var handler = CreateMockHandler(HttpStatusCode.OK, expected);
        var service = CreateService(handler);

        var result = await service.GetSectorsAsync();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be("finance");
    }

    [Fact]
    public async Task GetSectorsAsync_Error_ReturnsEmpty()
    {
        var handler = CreateMockHandler(HttpStatusCode.InternalServerError);
        var service = CreateService(handler);

        var result = await service.GetSectorsAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProviderStatusesAsync_ReturnsStatuses()
    {
        var expected = new List<SchemaProviderStatusViewModel>
        {
            new("SchemaStore", true, "LiveApi", 1.0, 24,
                DateTimeOffset.UtcNow, null, null, 100, "Healthy", null)
        };

        var handler = CreateMockHandler(HttpStatusCode.OK, expected);
        var service = CreateService(handler);

        var result = await service.GetProviderStatusesAsync();

        result.Should().HaveCount(1);
        result[0].ProviderName.Should().Be("SchemaStore");
        result[0].HealthStatus.Should().Be("Healthy");
    }

    [Fact]
    public async Task RefreshProviderAsync_Success_ReturnsTrue()
    {
        var handler = CreateMockHandler(HttpStatusCode.Accepted);
        var service = CreateService(handler);

        var result = await service.RefreshProviderAsync("SchemaStore");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshProviderAsync_TooManyRequests_ReturnsFalse()
    {
        var handler = CreateMockHandler(HttpStatusCode.TooManyRequests);
        var service = CreateService(handler);

        var result = await service.RefreshProviderAsync("SchemaStore");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateSectorPreferencesAsync_Success_ReturnsTrue()
    {
        var handler = CreateMockHandler(HttpStatusCode.OK);
        var service = CreateService(handler);

        var result = await service.UpdateSectorPreferencesAsync(["finance", "healthcare"]);

        result.Should().BeTrue();
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
    /// Handler that captures the request URI for assertion.
    /// </summary>
    private class RequestCapturingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string? _content;

        public string? LastRequestUri { get; private set; }

        public RequestCapturingHandler(HttpStatusCode statusCode, object? content = null)
        {
            _statusCode = statusCode;
            _content = content != null ? JsonSerializer.Serialize(content) : null;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri?.ToString();
            var response = new HttpResponseMessage(_statusCode);
            if (_content != null)
            {
                response.Content = new StringContent(_content, System.Text.Encoding.UTF8, "application/json");
            }
            return Task.FromResult(response);
        }
    }
}
