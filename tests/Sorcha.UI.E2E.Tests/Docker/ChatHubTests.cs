// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Sorcha.UI.E2E.Tests.Infrastructure;

namespace Sorcha.UI.E2E.Tests.Docker;

/// <summary>
/// E2E tests for the ChatHub SignalR connection against Docker infrastructure.
/// Tests the AI-assisted blueprint design chat functionality.
/// </summary>
[Parallelizable(ParallelScope.Self)]
[TestFixture]
[Category("Docker")]
[Category("SignalR")]
[Category("ChatHub")]
public class ChatHubTests : DockerTestBase
{
    private string? _accessToken;
    private readonly List<HubConnection> _connections = [];
    private static readonly HttpClient _httpClient = new();

    [SetUp]
    public override async Task BaseSetUp()
    {
        await base.BaseSetUp();

        // Get access token directly from the auth API
        await GetAccessTokenAsync();
    }

    [TearDown]
    public async Task CleanupConnections()
    {
        foreach (var connection in _connections)
        {
            if (connection.State == HubConnectionState.Connected)
            {
                try
                {
                    await connection.StopAsync();
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            await connection.DisposeAsync();
        }
        _connections.Clear();
    }

    #region Connection Tests

    [Test]
    [Retry(3)]
    public async Task ChatHub_CanConnect_ThroughApiGateway()
    {
        // Arrange
        Assert.That(_accessToken, Is.Not.Null.And.Not.Empty,
            "Access token must be obtained before connecting to ChatHub");

        var connection = CreateChatHubConnection();

        // Act
        await connection.StartAsync();

        // Assert
        Assert.That(connection.State, Is.EqualTo(HubConnectionState.Connected),
            "Should be able to connect to ChatHub through API Gateway");
    }

    [Test]
    [Retry(2)]
    public async Task ChatHub_CanDisconnect_Successfully()
    {
        // Arrange
        var connection = CreateChatHubConnection();
        await connection.StartAsync();

        // Act
        await connection.StopAsync();

        // Assert
        Assert.That(connection.State, Is.EqualTo(HubConnectionState.Disconnected),
            "Should disconnect cleanly");
    }

    #endregion

    #region Session Tests

    [Test]
    [Retry(3)]
    public async Task ChatHub_StartSession_ReturnsSessionId()
    {
        // Arrange
        var connection = CreateChatHubConnection();
        await connection.StartAsync();

        // Act
        var sessionId = await connection.InvokeAsync<string>("StartSession", (string?)null);

        // Assert
        Assert.That(sessionId, Is.Not.Null.And.Not.Empty,
            "StartSession should return a valid session ID");
    }

    [Test]
    [Retry(2)]
    public async Task ChatHub_StartSession_TriggersSessionStartedEvent()
    {
        // Arrange
        var connection = CreateChatHubConnection();
        var sessionStartedReceived = new TaskCompletionSource<string>();

        connection.On<string, object?, int>("SessionStarted", (sessionId, blueprint, messageCount) =>
        {
            sessionStartedReceived.TrySetResult(sessionId);
        });

        await connection.StartAsync();

        // Act
        var returnedSessionId = await connection.InvokeAsync<string>("StartSession", (string?)null);

        // Assert
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        cts.Token.Register(() => sessionStartedReceived.TrySetCanceled());

        var eventSessionId = await sessionStartedReceived.Task;
        Assert.That(eventSessionId, Is.EqualTo(returnedSessionId),
            "SessionStarted event should contain the same session ID");
    }

    [Test]
    [Retry(2)]
    public async Task ChatHub_EndSession_CompletesSuccessfully()
    {
        // Arrange
        var connection = CreateChatHubConnection();
        await connection.StartAsync();
        var sessionId = await connection.InvokeAsync<string>("StartSession", (string?)null);

        // Act & Assert - should not throw
        Assert.DoesNotThrowAsync(async () =>
        {
            await connection.InvokeAsync("EndSession", sessionId);
        }, "EndSession should complete without error");
    }

    #endregion

    #region Message Validation Tests

    [Test]
    [Retry(2)]
    public async Task ChatHub_SendMessage_WithEmptyMessage_ThrowsError()
    {
        // Arrange
        var connection = CreateChatHubConnection();
        await connection.StartAsync();
        var sessionId = await connection.InvokeAsync<string>("StartSession", (string?)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<HubException>(async () =>
        {
            await connection.InvokeAsync("SendMessage", sessionId, "");
        });

        Assert.That(ex?.Message, Does.Contain("empty").IgnoreCase,
            "Should reject empty messages");
    }

    [Test]
    [Retry(2)]
    public async Task ChatHub_SendMessage_WithTooLongMessage_ThrowsError()
    {
        // Arrange
        var connection = CreateChatHubConnection();
        await connection.StartAsync();
        var sessionId = await connection.InvokeAsync<string>("StartSession", (string?)null);
        var longMessage = new string('x', 10001);

        // Act & Assert
        var ex = Assert.ThrowsAsync<HubException>(async () =>
        {
            await connection.InvokeAsync("SendMessage", sessionId, longMessage);
        });

        Assert.That(ex?.Message, Does.Contain("long").IgnoreCase,
            "Should reject messages over 10000 characters");
    }

    #endregion

    #region Blueprint Operations Tests

    [Test]
    [Retry(2)]
    public async Task ChatHub_SaveBlueprint_WithoutDraft_ThrowsError()
    {
        // Arrange
        var connection = CreateChatHubConnection();
        await connection.StartAsync();
        var sessionId = await connection.InvokeAsync<string>("StartSession", (string?)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<HubException>(async () =>
        {
            await connection.InvokeAsync<string>("SaveBlueprint", sessionId);
        });

        Assert.That(ex?.Message, Does.Contain("blueprint").IgnoreCase,
            "Should reject save when no blueprint draft exists");
    }

    [Test]
    [Retry(2)]
    public async Task ChatHub_ExportBlueprint_WithoutDraft_ThrowsError()
    {
        // Arrange
        var connection = CreateChatHubConnection();
        await connection.StartAsync();
        var sessionId = await connection.InvokeAsync<string>("StartSession", (string?)null);

        // Act & Assert
        var ex = Assert.ThrowsAsync<HubException>(async () =>
        {
            await connection.InvokeAsync<string>("ExportBlueprint", sessionId, "json");
        });

        Assert.That(ex?.Message, Does.Contain("blueprint").IgnoreCase,
            "Should reject export when no blueprint draft exists");
    }

    #endregion

    #region Error Handling Tests

    [Test]
    [Retry(2)]
    public async Task ChatHub_SessionError_EventIsReceived()
    {
        // Arrange
        var connection = CreateChatHubConnection();
        var errorReceived = new TaskCompletionSource<(string Code, string Message)>();

        connection.On<string, string>("SessionError", (errorCode, message) =>
        {
            errorReceived.TrySetResult((errorCode, message));
        });

        await connection.StartAsync();
        var sessionId = await connection.InvokeAsync<string>("StartSession", (string?)null);

        // Act - Trigger an error by trying to save without a blueprint
        try
        {
            await connection.InvokeAsync<string>("SaveBlueprint", sessionId);
        }
        catch (HubException)
        {
            // Expected
        }

        // Assert
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        cts.Token.Register(() => errorReceived.TrySetCanceled());

        var error = await errorReceived.Task;
        Assert.That(error.Code, Is.EqualTo("SAVE_FAILED"),
            "Should receive SAVE_FAILED error code");
    }

    #endregion

    #region Reconnection Tests

    [Test]
    [Retry(2)]
    public async Task ChatHub_CanReconnect_AfterDisconnect()
    {
        // Arrange
        var connection = CreateChatHubConnection();
        await connection.StartAsync();
        await connection.InvokeAsync<string>("StartSession", (string?)null);

        // Act - Disconnect and reconnect
        await connection.StopAsync();
        await connection.StartAsync();

        // Start new session after reconnect
        var newSessionId = await connection.InvokeAsync<string>("StartSession", (string?)null);

        // Assert
        Assert.That(connection.State, Is.EqualTo(HubConnectionState.Connected),
            "Should reconnect successfully");
        Assert.That(newSessionId, Is.Not.Null.And.Not.Empty,
            "Should be able to start new session after reconnect");
    }

    #endregion

    #region Helper Methods

    private async Task GetAccessTokenAsync()
    {
        var loginUrl = $"{TestConstants.ApiGatewayUrl}/api/auth/login";

        var loginRequest = new
        {
            email = TestConstants.TestEmail,
            password = TestConstants.TestPassword
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(loginUrl, loginRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Assert.Fail($"Login failed with status {response.StatusCode}: {errorContent}");
                return;
            }

            var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>();
            // API returns snake_case (access_token) not camelCase (accessToken)
            if (jsonDoc?.RootElement.TryGetProperty("access_token", out var tokenProp) == true)
            {
                _accessToken = tokenProp.GetString();
            }
            else if (jsonDoc?.RootElement.TryGetProperty("accessToken", out tokenProp) == true)
            {
                _accessToken = tokenProp.GetString();
            }

            Assert.That(_accessToken, Is.Not.Null.And.Not.Empty,
                "Should receive access token from login API");
        }
        catch (HttpRequestException ex)
        {
            Assert.Fail($"Failed to connect to auth API at {loginUrl}: {ex.Message}");
        }
    }

    private HubConnection CreateChatHubConnection()
    {
        var hubUrl = $"{TestConstants.ApiGatewayUrl}/hubs/chat";

        var connectionBuilder = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                if (!string.IsNullOrEmpty(_accessToken))
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(_accessToken);
                }
            })
            .WithAutomaticReconnect();

        var connection = connectionBuilder.Build();
        _connections.Add(connection);
        return connection;
    }

    #endregion
}
