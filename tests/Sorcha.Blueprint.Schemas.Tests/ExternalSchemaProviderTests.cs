// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Sorcha.Blueprint.Schemas.DTOs;
using Sorcha.Blueprint.Schemas.Services;
using Xunit;

namespace Sorcha.Blueprint.Schemas.Tests;

/// <summary>
/// Tests for SchemaStoreOrgProvider.
/// </summary>
public class ExternalSchemaProviderTests
{
    private readonly Mock<ILogger<SchemaStoreOrgProvider>> _loggerMock;

    public ExternalSchemaProviderTests()
    {
        _loggerMock = new Mock<ILogger<SchemaStoreOrgProvider>>();
    }

    [Fact]
    public async Task SearchAsync_ReturnsMatchingSchemas()
    {
        // Arrange
        var catalogJson = """
        {
            "schemas": [
                {
                    "name": "package.json",
                    "description": "NPM package manifest",
                    "url": "https://json.schemastore.org/package.json",
                    "fileMatch": ["package.json"]
                },
                {
                    "name": "tsconfig.json",
                    "description": "TypeScript configuration",
                    "url": "https://json.schemastore.org/tsconfig.json",
                    "fileMatch": ["tsconfig.json"]
                },
                {
                    "name": "eslintrc",
                    "description": "ESLint configuration",
                    "url": "https://json.schemastore.org/eslintrc.json",
                    "fileMatch": [".eslintrc", ".eslintrc.json"]
                }
            ]
        }
        """;

        var httpClient = CreateMockHttpClient(catalogJson);
        var provider = new SchemaStoreOrgProvider(httpClient, _loggerMock.Object);

        // Act
        var result = await provider.SearchAsync("package");

        // Assert
        result.Should().NotBeNull();
        result.Provider.Should().Be("SchemaStore.org");
        result.Query.Should().Be("package");
        result.Results.Should().HaveCount(1);
        result.Results[0].Name.Should().Be("package.json");
        result.Results[0].Url.Should().Be("https://json.schemastore.org/package.json");
    }

    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ReturnsEmptyResults()
    {
        // Arrange
        var httpClient = CreateMockHttpClient("{}");
        var provider = new SchemaStoreOrgProvider(httpClient, _loggerMock.Object);

        // Act
        var result = await provider.SearchAsync("");

        // Assert
        result.Should().NotBeNull();
        result.Results.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_MatchesDescription()
    {
        // Arrange
        var catalogJson = """
        {
            "schemas": [
                {
                    "name": "package.json",
                    "description": "NPM package manifest for Node.js",
                    "url": "https://json.schemastore.org/package.json",
                    "fileMatch": ["package.json"]
                },
                {
                    "name": "appsettings.json",
                    "description": ".NET Core application settings",
                    "url": "https://json.schemastore.org/appsettings.json"
                }
            ]
        }
        """;

        var httpClient = CreateMockHttpClient(catalogJson);
        var provider = new SchemaStoreOrgProvider(httpClient, _loggerMock.Object);

        // Act
        var result = await provider.SearchAsync("Node.js");

        // Assert
        result.Results.Should().HaveCount(1);
        result.Results[0].Name.Should().Be("package.json");
    }

    [Fact]
    public async Task SearchAsync_MatchesFilePattern()
    {
        // Arrange
        var catalogJson = """
        {
            "schemas": [
                {
                    "name": "ESLint Configuration",
                    "description": "Config for ESLint",
                    "url": "https://json.schemastore.org/eslintrc.json",
                    "fileMatch": [".eslintrc", ".eslintrc.json"]
                }
            ]
        }
        """;

        var httpClient = CreateMockHttpClient(catalogJson);
        var provider = new SchemaStoreOrgProvider(httpClient, _loggerMock.Object);

        // Act
        var result = await provider.SearchAsync(".eslintrc");

        // Assert
        result.Results.Should().HaveCount(1);
        result.Results[0].Name.Should().Be("ESLint Configuration");
    }

    [Fact]
    public async Task GetSchemaAsync_ReturnsSchemaWithContent()
    {
        // Arrange
        var schemaContent = """
        {
            "$schema": "http://json-schema.org/draft-07/schema#",
            "title": "Test Schema",
            "type": "object",
            "properties": {
                "name": { "type": "string" }
            }
        }
        """;

        var catalogJson = """
        {
            "schemas": [
                {
                    "name": "test-schema",
                    "description": "A test schema",
                    "url": "https://json.schemastore.org/test.json"
                }
            ]
        }
        """;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("catalog.json")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(catalogJson)
            });

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains("test.json")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(schemaContent)
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://www.schemastore.org/")
        };

        var provider = new SchemaStoreOrgProvider(httpClient, _loggerMock.Object);

        // Act
        var result = await provider.GetSchemaAsync("https://json.schemastore.org/test.json");

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("test-schema");
        result.Content.Should().NotBeNull();
        result.Content!.RootElement.GetProperty("title").GetString().Should().Be("Test Schema");
    }

    [Fact]
    public async Task GetSchemaAsync_WithInvalidUrl_ReturnsNull()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://www.schemastore.org/")
        };

        var provider = new SchemaStoreOrgProvider(httpClient, _loggerMock.Object);

        // Act
        var result = await provider.GetSchemaAsync("https://invalid-url.com/schema.json");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCatalogAsync_ReturnsAllSchemas()
    {
        // Arrange
        var catalogJson = """
        {
            "schemas": [
                { "name": "schema1", "description": "First schema", "url": "https://example.com/1.json" },
                { "name": "schema2", "description": "Second schema", "url": "https://example.com/2.json" },
                { "name": "schema3", "description": "Third schema", "url": "https://example.com/3.json" }
            ]
        }
        """;

        var httpClient = CreateMockHttpClient(catalogJson);
        var provider = new SchemaStoreOrgProvider(httpClient, _loggerMock.Object);

        // Act
        var result = await provider.GetCatalogAsync();

        // Assert
        var schemas = result.ToList();
        schemas.Should().HaveCount(3);
        schemas.Should().Contain(s => s.Name == "schema1");
        schemas.Should().Contain(s => s.Name == "schema2");
        schemas.Should().Contain(s => s.Name == "schema3");
    }

    [Fact]
    public async Task IsAvailableAsync_ReturnsTrueWhenReachable()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Head),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://www.schemastore.org/")
        };

        var provider = new SchemaStoreOrgProvider(httpClient, _loggerMock.Object);

        // Act
        var result = await provider.IsAvailableAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAvailableAsync_ReturnsFalseWhenUnreachable()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://www.schemastore.org/")
        };

        var provider = new SchemaStoreOrgProvider(httpClient, _loggerMock.Object);

        // Act
        var result = await provider.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SearchAsync_UsesCachedCatalog()
    {
        // Arrange
        var catalogJson = """
        {
            "schemas": [
                { "name": "cached-schema", "description": "Test", "url": "https://example.com/1.json" }
            ]
        }
        """;

        var callCount = 0;
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(catalogJson)
                };
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://www.schemastore.org/")
        };

        var provider = new SchemaStoreOrgProvider(httpClient, _loggerMock.Object, TimeSpan.FromMinutes(5));

        // Act - Call search multiple times
        await provider.SearchAsync("cached");
        await provider.SearchAsync("schema");
        await provider.SearchAsync("test");

        // Assert - Should only fetch catalog once due to caching
        callCount.Should().Be(1);
    }

    [Fact]
    public async Task SearchAsync_FallsBackToCacheWhenOffline()
    {
        // Arrange
        var catalogJson = """
        {
            "schemas": [
                { "name": "offline-schema", "description": "Test", "url": "https://example.com/1.json" }
            ]
        }
        """;

        var firstCall = true;
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                if (firstCall)
                {
                    firstCall = false;
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(catalogJson)
                    };
                }
                throw new HttpRequestException("Network unavailable");
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://www.schemastore.org/")
        };

        // Use very short cache TTL to force refresh attempt
        var provider = new SchemaStoreOrgProvider(httpClient, _loggerMock.Object, TimeSpan.FromMilliseconds(1));

        // First call - loads cache
        var result1 = await provider.SearchAsync("offline");
        result1.Results.Should().HaveCount(1);

        // Wait for cache to expire
        await Task.Delay(10);

        // Second call - should fall back to cache
        var result2 = await provider.SearchAsync("offline");
        result2.Results.Should().HaveCount(1);
        result2.IsPartialResult.Should().BeTrue();
    }

    [Fact]
    public void ProviderName_ReturnsSchemaStoreOrg()
    {
        // Arrange
        var httpClient = CreateMockHttpClient("{}");
        var provider = new SchemaStoreOrgProvider(httpClient, _loggerMock.Object);

        // Act & Assert
        provider.ProviderName.Should().Be("SchemaStore.org");
    }

    private HttpClient CreateMockHttpClient(string responseContent)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent)
            });

        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://www.schemastore.org/")
        };
    }
}
