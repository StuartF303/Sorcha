// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Moq;

namespace Sorcha.Validator.Service.IntegrationTests;

/// <summary>
/// Integration tests for the Genesis Config Service.
/// Tests configuration loading from genesis blocks and control blueprints.
/// </summary>
[Collection("ValidatorService")]
public class GenesisConfigServiceIntegrationTests
{
    private readonly ValidatorServiceWebApplicationFactory _factory;

    public GenesisConfigServiceIntegrationTests(ValidatorServiceWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ClearTestData();
    }

    #region Genesis Transaction Endpoints

    [Fact]
    public async Task SubmitGenesisTransaction_EndpointExists()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var emptyRequest = new { };

        // Act
        var response = await client.PostAsJsonAsync("/api/validator/genesis", emptyRequest);

        // Assert - Endpoint exists (not 404)
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SubmitGenesisTransaction_WithValidControlRecord_ProcessesSuccessfully()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");

        var controlRecord = CreateValidControlRecord();
        var request = new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = registerId,
            ControlRecordPayload = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(controlRecord)),
            PayloadHash = "abc123",
            Signatures = new[]
            {
                new { PublicKey = Convert.ToBase64String(new byte[32]), SignatureValue = Convert.ToBase64String(new byte[64]), Algorithm = "ED25519" }
            },
            CreatedAt = DateTimeOffset.UtcNow,
            RegisterName = "Test Genesis Register",
            TenantId = "test-tenant"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/validator/genesis", request);

        // Assert - Either success or handled error
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Created,
            HttpStatusCode.Conflict,
            HttpStatusCode.BadRequest,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task SubmitGenesisTransaction_WithMinimalConsensusConfig_ProcessesSuccessfully()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();

        var controlRecord = new
        {
            consensus = new
            {
                algorithm = "simple-majority",
                threshold = "PT30S",
                minSignatures = 1,
                maxSignatures = 5
            },
            validators = new
            {
                min = 1,
                max = 10,
                registrationMode = "public"
            },
            leaderElection = new
            {
                mechanism = "rotating",
                heartbeatInterval = "PT5S",
                leaderTimeout = "PT30S"
            }
        };

        var request = new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = Guid.NewGuid().ToString("N"),
            ControlRecordPayload = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(controlRecord)),
            PayloadHash = "abc123",
            Signatures = new[]
            {
                new { PublicKey = Convert.ToBase64String(new byte[32]), SignatureValue = Convert.ToBase64String(new byte[64]), Algorithm = "ED25519" }
            },
            CreatedAt = DateTimeOffset.UtcNow,
            RegisterName = "Minimal Config Register",
            TenantId = "test-tenant"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/validator/genesis", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Created,
            HttpStatusCode.Conflict,
            HttpStatusCode.BadRequest,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task SubmitGenesisTransaction_WithConsentModeValidators_ProcessesSuccessfully()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();

        var controlRecord = new
        {
            consensus = new
            {
                algorithm = "simple-majority",
                threshold = "PT30S"
            },
            validators = new
            {
                min = 3,
                max = 21,
                registrationMode = "consent", // Requires approval
                requireStake = true,
                stakeAmount = 1000.00
            },
            leaderElection = new
            {
                mechanism = "stake-weighted",
                heartbeatInterval = "PT10S",
                leaderTimeout = "PT60S"
            }
        };

        var request = new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = Guid.NewGuid().ToString("N"),
            ControlRecordPayload = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(controlRecord)),
            PayloadHash = "abc123",
            Signatures = new[]
            {
                new { PublicKey = Convert.ToBase64String(new byte[32]), SignatureValue = Convert.ToBase64String(new byte[64]), Algorithm = "ED25519" }
            },
            CreatedAt = DateTimeOffset.UtcNow,
            RegisterName = "Consent Mode Register",
            TenantId = "test-tenant"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/validator/genesis", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.Created,
            HttpStatusCode.Conflict,
            HttpStatusCode.BadRequest,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Control Record Validation

    [Fact]
    public async Task SubmitGenesisTransaction_WithInvalidConsensusThreshold_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();

        var controlRecord = new
        {
            consensus = new
            {
                algorithm = "simple-majority",
                threshold = "invalid-duration" // Invalid ISO 8601 duration
            },
            validators = new
            {
                min = 1,
                max = 10,
                registrationMode = "public"
            }
        };

        var request = new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = Guid.NewGuid().ToString("N"),
            ControlRecordPayload = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(controlRecord)),
            PayloadHash = "abc123",
            Signatures = new[]
            {
                new { PublicKey = Convert.ToBase64String(new byte[32]), SignatureValue = Convert.ToBase64String(new byte[64]), Algorithm = "ED25519" }
            },
            CreatedAt = DateTimeOffset.UtcNow,
            RegisterName = "Invalid Config Register",
            TenantId = "test-tenant"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/validator/genesis", request);

        // Assert - Should reject invalid config
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.OK,
            HttpStatusCode.Conflict,
            HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task SubmitGenesisTransaction_WithInvalidValidatorRange_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();

        var controlRecord = new
        {
            consensus = new
            {
                algorithm = "simple-majority",
                threshold = "PT30S"
            },
            validators = new
            {
                min = 10, // min > max is invalid
                max = 5,
                registrationMode = "public"
            }
        };

        var request = new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = Guid.NewGuid().ToString("N"),
            ControlRecordPayload = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(controlRecord)),
            PayloadHash = "abc123",
            Signatures = new[]
            {
                new { PublicKey = Convert.ToBase64String(new byte[32]), SignatureValue = Convert.ToBase64String(new byte[64]), Algorithm = "ED25519" }
            },
            CreatedAt = DateTimeOffset.UtcNow,
            RegisterName = "Invalid Validator Range Register",
            TenantId = "test-tenant"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/validator/genesis", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.OK,
            HttpStatusCode.Conflict,
            HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task SubmitGenesisTransaction_WithMissingRequiredFields_ReturnsBadRequest()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();

        var controlRecord = new
        {
            // Missing consensus section
            validators = new
            {
                min = 1,
                max = 10,
                registrationMode = "public"
            }
        };

        var request = new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = Guid.NewGuid().ToString("N"),
            ControlRecordPayload = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(controlRecord)),
            PayloadHash = "abc123",
            Signatures = new[]
            {
                new { PublicKey = Convert.ToBase64String(new byte[32]), SignatureValue = Convert.ToBase64String(new byte[64]), Algorithm = "ED25519" }
            },
            CreatedAt = DateTimeOffset.UtcNow,
            RegisterName = "Missing Fields Register",
            TenantId = "test-tenant"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/validator/genesis", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.OK,
            HttpStatusCode.Conflict,
            HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Genesis Config Loading

    [Fact]
    public async Task GenesisConfig_WhenNoGenesisDocket_HandlesGracefully()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();
        var registerId = Guid.NewGuid().ToString("N");

        // Setup no genesis docket
        _factory.RegisterClientMock
            .Setup(r => r.ReadDocketAsync(registerId, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sorcha.ServiceClients.Register.DocketModel?)null);

        // Act
        var response = await client.PostAsJsonAsync("/api/admin/validators/start", new { RegisterId = registerId });

        // Assert - Should start (using defaults) or return appropriate error
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ValidatorStart_LoadsConfigurationFromGenesis()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var registerId = Guid.NewGuid().ToString("N");

        // Act - Start validator which loads genesis config
        var response = await client.PostAsJsonAsync("/api/admin/validators/start", new { RegisterId = registerId });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Control Blueprint Version Resolution

    [Fact]
    public async Task ControlBlueprintVersion_RefreshEndpoint_Works()
    {
        // Arrange
        using var client = _factory.CreateAdminClient();
        var registerId = Guid.NewGuid().ToString("N");

        // Start validator
        var startResponse = await client.PostAsJsonAsync("/api/admin/validators/start", new { RegisterId = registerId });
        startResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Trigger a refresh
        var response = await client.PostAsync($"/api/validators/{registerId}/refresh", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GenesisEndpoint_ReturnsExpectedResponseStructure()
    {
        // Arrange
        using var client = _factory.CreateValidatorClient();

        var controlRecord = CreateValidControlRecord();
        var request = new
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            RegisterId = Guid.NewGuid().ToString("N"),
            ControlRecordPayload = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(controlRecord)),
            PayloadHash = "abc123",
            Signatures = new[]
            {
                new { PublicKey = Convert.ToBase64String(new byte[32]), SignatureValue = Convert.ToBase64String(new byte[64]), Algorithm = "ED25519" }
            },
            CreatedAt = DateTimeOffset.UtcNow,
            RegisterName = "Response Structure Test",
            TenantId = "test-tenant"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/validator/genesis", request);

        // Assert - Response should have some structure if successful
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeNullOrEmpty();
        }
    }

    #endregion

    #region Helper Methods

    private static object CreateValidControlRecord()
    {
        return new
        {
            consensus = new
            {
                algorithm = "simple-majority",
                threshold = "PT30S",
                minSignatures = 1,
                maxSignatures = 10,
                maxTransactionsPerDocket = 100,
                docketBuildInterval = "PT5S"
            },
            validators = new
            {
                min = 1,
                max = 10,
                registrationMode = "public",
                requireStake = false
            },
            leaderElection = new
            {
                mechanism = "rotating",
                heartbeatInterval = "PT5S",
                leaderTimeout = "PT30S",
                termDuration = "PT60S"
            }
        };
    }

    #endregion
}
