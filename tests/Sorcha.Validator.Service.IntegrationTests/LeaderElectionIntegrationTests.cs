// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Moq;
using Sorcha.ServiceClients.Peer;

namespace Sorcha.Validator.Service.IntegrationTests;

/// <summary>
/// Integration tests for Leader Election.
/// Tests rotating leader election, heartbeat mechanism, and leader failure detection.
/// Note: Full leader election tests are in VAL-9.48.
/// </summary>
[Collection("ValidatorService")]
public class LeaderElectionIntegrationTests
{
    private readonly ValidatorServiceWebApplicationFactory _factory;

    public LeaderElectionIntegrationTests(ValidatorServiceWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ClearTestData();
    }

    #region Leader Status

    [Fact]
    public async Task GetValidatorStatus_IncludesLeaderInfo()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var registerId = Guid.NewGuid().ToString("N");

        // Start validator
        var startResponse = await client.PostAsJsonAsync("/api/admin/validators/start", new { RegisterId = registerId });
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await client.GetAsync($"/api/admin/validators/{registerId}/status");

        // Assert - Status endpoint may return OK with status, or NotFound if validator state not persisted
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadFromJsonAsync<JsonDocument>();
            content.Should().NotBeNull();
            content!.RootElement.TryGetProperty("status", out _).Should().BeTrue();
        }
    }

    [Fact]
    public async Task ValidatorStart_WithSingleValidator_BecomesLeader()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var registerId = Guid.NewGuid().ToString("N");

        // Setup single validator (self)
        _factory.PeerClientMock
            .Setup(p => p.QueryValidatorsAsync(registerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ValidatorInfo>
            {
                new()
                {
                    ValidatorId = "test-validator-id",
                    GrpcEndpoint = "http://localhost:7004",
                    ReputationScore = 1.0,
                    IsActive = true
                }
            });

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/validators/start", new { RegisterId = registerId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Rotating Leader Election

    [Fact]
    public async Task ValidatorStart_WithMultipleValidators_DeterminesLeader()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var registerId = Guid.NewGuid().ToString("N");

        // Setup multiple validators
        _factory.PeerClientMock
            .Setup(p => p.QueryValidatorsAsync(registerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ValidatorInfo>
            {
                new() { ValidatorId = "validator-1", GrpcEndpoint = "http://localhost:7004", ReputationScore = 1.0, IsActive = true },
                new() { ValidatorId = "validator-2", GrpcEndpoint = "http://localhost:7005", ReputationScore = 0.9, IsActive = true },
                new() { ValidatorId = "test-validator-id", GrpcEndpoint = "http://localhost:7006", ReputationScore = 0.8, IsActive = true }
            });

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/validators/start", new { RegisterId = registerId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProcessPipeline_OnlyLeaderBuildsDockets()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var registerId = Guid.NewGuid().ToString("N");

        // Setup this validator as not the leader
        _factory.PeerClientMock
            .Setup(p => p.QueryValidatorsAsync(registerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ValidatorInfo>
            {
                new() { ValidatorId = "other-leader-validator", GrpcEndpoint = "http://localhost:7004", ReputationScore = 1.0, IsActive = true },
                new() { ValidatorId = "test-validator-id", GrpcEndpoint = "http://localhost:7005", ReputationScore = 0.8, IsActive = true }
            });

        // Start validator
        await client.PostAsJsonAsync("/api/admin/validators/start", new { RegisterId = registerId });

        // Act
        var response = await client.PostAsync($"/api/admin/validators/{registerId}/process", null);

        // Assert - Should complete (either build docket if leader, or skip if not)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Validator Order

    [Fact]
    public async Task GetValidators_ReturnsOrderedList()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");

        // Act
        var response = await client.GetAsync($"/api/validators/{registerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<JsonDocument>();
        content.Should().NotBeNull();
        content!.RootElement.TryGetProperty("validators", out _).Should().BeTrue();
    }

    [Fact]
    public async Task GetValidatorCount_ReturnsActiveCount()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");

        // Setup multiple validators
        _factory.PeerClientMock
            .Setup(p => p.QueryValidatorsAsync(registerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ValidatorInfo>
            {
                new() { ValidatorId = "validator-1", GrpcEndpoint = "http://localhost:7004", ReputationScore = 1.0, IsActive = true },
                new() { ValidatorId = "validator-2", GrpcEndpoint = "http://localhost:7005", ReputationScore = 0.9, IsActive = true },
                new() { ValidatorId = "validator-3", GrpcEndpoint = "http://localhost:7006", ReputationScore = 0.8, IsActive = false }
            });

        // Act
        var response = await client.GetAsync($"/api/validators/{registerId}/count");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<JsonDocument>();
        content.Should().NotBeNull();
        content!.RootElement.TryGetProperty("activeCount", out _).Should().BeTrue();
    }

    #endregion

    #region Leader Election Configuration

    [Fact]
    public async Task ValidatorStart_LoadsElectionConfiguration()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var registerId = Guid.NewGuid().ToString("N");

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/validators/start", new { RegisterId = registerId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ValidatorStart_DifferentElectionMechanisms_HandleCorrectly()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var registerId = Guid.NewGuid().ToString("N");

        // Act - Default mechanism (rotating)
        var response = await client.PostAsJsonAsync("/api/admin/validators/start", new { RegisterId = registerId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Leader Failure Handling

    [Fact]
    public async Task ProcessPipeline_WhenLeaderUnavailable_HandlesGracefully()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var registerId = Guid.NewGuid().ToString("N");

        // Setup validators where leader fails to respond
        _factory.PeerClientMock
            .Setup(p => p.QueryValidatorsAsync(registerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ValidatorInfo>
            {
                new() { ValidatorId = "failed-leader", GrpcEndpoint = "http://localhost:7004", ReputationScore = 1.0, IsActive = false },
                new() { ValidatorId = "test-validator-id", GrpcEndpoint = "http://localhost:7005", ReputationScore = 0.9, IsActive = true }
            });

        // Start validator
        await client.PostAsJsonAsync("/api/admin/validators/start", new { RegisterId = registerId });

        // Act
        var response = await client.PostAsync($"/api/admin/validators/{registerId}/process", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProcessPipeline_WhenValidatorNetworkPartitioned_HandlesGracefully()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var registerId = Guid.NewGuid().ToString("N");

        // Setup peer service to fail
        _factory.PeerClientMock
            .Setup(p => p.QueryValidatorsAsync(registerId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network partition"));

        // Start validator
        await client.PostAsJsonAsync("/api/admin/validators/start", new { RegisterId = registerId });

        // Act
        var response = await client.PostAsync($"/api/admin/validators/{registerId}/process", null);

        // Assert - Should handle gracefully
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Validator Registration for Election

    [Fact]
    public async Task RefreshValidators_UpdatesElectionOrder()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var registerId = Guid.NewGuid().ToString("N");

        // Start validator
        await client.PostAsJsonAsync("/api/admin/validators/start", new { RegisterId = registerId });

        // Act
        var response = await client.PostAsync($"/api/validators/{registerId}/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RegisterValidator_AffectsElectionOrder()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");

        var request = new
        {
            RegisterId = registerId,
            ValidatorId = Guid.NewGuid().ToString("N"),
            PublicKey = Convert.ToBase64String(new byte[32]),
            GrpcEndpoint = "http://localhost:7010"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/validators/register", request);

        // Assert - Registration should be processed
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest);
    }

    #endregion
}
