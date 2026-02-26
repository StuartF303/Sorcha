// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sorcha.Blueprint.Service.Endpoints;
using Sorcha.Blueprint.Service.Models;
using Sorcha.Blueprint.Service.Services.Interfaces;

namespace Sorcha.Blueprint.Service.Tests.Endpoints;

/// <summary>
/// Unit tests for EventEndpoints verifying HTTP contract, authentication,
/// authorization, query parameter handling, and response structure.
/// Uses a lightweight TestServer with mocked IEventService.
/// </summary>
public class EventEndpointTests : IDisposable
{
    private readonly Mock<IEventService> _mockEventService;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _orgId = Guid.NewGuid();
    private readonly HttpClient _client;
    private readonly WebApplication _app;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EventEndpointTests()
    {
        _mockEventService = new Mock<IEventService>();

        var userId = _userId;
        var orgId = _orgId;

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing"
        });

        builder.WebHost.UseTestServer();

        // Register mocked event service
        builder.Services.AddSingleton(_mockEventService.Object);

        // Configure test authentication
        builder.Services.AddAuthentication("TestScheme")
            .AddScheme<EventTestAuthOptions, EventTestAuthHandler>("TestScheme", opts =>
            {
                opts.UserId = userId;
                opts.OrganizationId = orgId;
                opts.Role = "Administrator";
            });

        builder.Services.AddAuthorization();

        _app = builder.Build();

        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapEventEndpoints();

        _app.StartAsync().GetAwaiter().GetResult();
        _client = _app.GetTestClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _app.StopAsync().GetAwaiter().GetResult();
        _app.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    #region GET /api/events

    [Fact]
    public async Task GetEvents_AuthenticatedUser_ReturnsOkWithPaginatedEvents()
    {
        // Arrange
        var events = new List<ActivityEvent>
        {
            MakeEvent("Event 1"),
            MakeEvent("Event 2")
        };

        _mockEventService
            .Setup(s => s.GetEventsAsync(
                _userId, 1, 50, false, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((events.AsReadOnly(), 2));

        // Act
        var response = await _client.GetAsync("/api/events");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaginatedEventsResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.TotalCount.Should().Be(2);
        body.Page.Should().Be(1);
        body.PageSize.Should().Be(50);
        body.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetEvents_WithCustomPagination_PassesCorrectParameters()
    {
        // Arrange
        _mockEventService
            .Setup(s => s.GetEventsAsync(
                _userId, 3, 10, false, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<ActivityEvent>().AsReadOnly(), 0));

        // Act
        var response = await _client.GetAsync("/api/events?page=3&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mockEventService.Verify(s => s.GetEventsAsync(
            _userId, 3, 10, false, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEvents_WithUnreadOnlyFilter_PassesFilterToService()
    {
        // Arrange
        _mockEventService
            .Setup(s => s.GetEventsAsync(
                _userId, 1, 50, true, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<ActivityEvent>().AsReadOnly(), 0));

        // Act
        var response = await _client.GetAsync("/api/events?unreadOnly=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mockEventService.Verify(s => s.GetEventsAsync(
            _userId, 1, 50, true, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEvents_WithSeverityFilter_ParsesSeverityCorrectly()
    {
        // Arrange
        _mockEventService
            .Setup(s => s.GetEventsAsync(
                _userId, 1, 50, false, EventSeverity.Error, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<ActivityEvent>().AsReadOnly(), 0));

        // Act
        var response = await _client.GetAsync("/api/events?severity=Error");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mockEventService.Verify(s => s.GetEventsAsync(
            _userId, 1, 50, false, EventSeverity.Error, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEvents_WithInvalidSeverity_TreatsAsNull()
    {
        // Arrange
        _mockEventService
            .Setup(s => s.GetEventsAsync(
                _userId, 1, 50, false, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<ActivityEvent>().AsReadOnly(), 0));

        // Act
        var response = await _client.GetAsync("/api/events?severity=InvalidValue");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mockEventService.Verify(s => s.GetEventsAsync(
            _userId, 1, 50, false, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEvents_WithSinceFilter_PassesDateTimeToService()
    {
        // Arrange
        var since = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        _mockEventService
            .Setup(s => s.GetEventsAsync(
                _userId, 1, 50, false, null, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<ActivityEvent>().AsReadOnly(), 0));

        // Act
        var response = await _client.GetAsync($"/api/events?since={since:O}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mockEventService.Verify(s => s.GetEventsAsync(
            _userId, 1, 50, false, null, It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEvents_ResponseIncludesMappedDtoFields()
    {
        // Arrange
        var evt = MakeEvent("Test Title");
        evt.Severity = EventSeverity.Warning;
        evt.IsRead = true;
        evt.EntityId = "entity-123";
        evt.EntityType = "Blueprint";

        _mockEventService
            .Setup(s => s.GetEventsAsync(
                _userId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(),
                It.IsAny<EventSeverity?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ActivityEvent> { evt }.AsReadOnly(), 1));

        // Act
        var response = await _client.GetAsync("/api/events");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var item = doc.RootElement.GetProperty("items").EnumerateArray().First();

        item.GetProperty("id").GetGuid().Should().Be(evt.Id);
        item.GetProperty("eventType").GetString().Should().Be(evt.EventType);
        item.GetProperty("severity").GetString().Should().Be("Warning");
        item.GetProperty("title").GetString().Should().Be("Test Title");
        item.GetProperty("message").GetString().Should().Be(evt.Message);
        item.GetProperty("sourceService").GetString().Should().Be(evt.SourceService);
        item.GetProperty("entityId").GetString().Should().Be("entity-123");
        item.GetProperty("entityType").GetString().Should().Be("Blueprint");
        item.GetProperty("isRead").GetBoolean().Should().BeTrue();
    }

    #endregion

    #region GET /api/events/unread-count

    [Fact]
    public async Task GetUnreadCount_AuthenticatedUser_ReturnsCount()
    {
        // Arrange
        _mockEventService
            .Setup(s => s.GetUnreadCountAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(7);

        // Act
        var response = await _client.GetAsync("/api/events/unread-count");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("count").GetInt32().Should().Be(7);
    }

    [Fact]
    public async Task GetUnreadCount_ZeroUnread_ReturnsZero()
    {
        // Arrange
        _mockEventService
            .Setup(s => s.GetUnreadCountAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var response = await _client.GetAsync("/api/events/unread-count");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("count").GetInt32().Should().Be(0);
    }

    #endregion

    #region POST /api/events/mark-read

    [Fact]
    public async Task MarkRead_WithSpecificEventIds_ReturnsMarkedCount()
    {
        // Arrange
        var eventIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        _mockEventService
            .Setup(s => s.MarkReadAsync(_userId, eventIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var request = new MarkReadRequest(eventIds);

        // Act
        var response = await _client.PostAsJsonAsync("/api/events/mark-read", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("markedCount").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task MarkRead_WithNullEventIds_MarksAllRead()
    {
        // Arrange
        _mockEventService
            .Setup(s => s.MarkReadAsync(_userId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var request = new MarkReadRequest(null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/events/mark-read", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("markedCount").GetInt32().Should().Be(5);
    }

    [Fact]
    public async Task MarkRead_WithEmptyEventIds_MarksAllRead()
    {
        // Arrange - empty array is treated as null (mark all) per endpoint logic
        _mockEventService
            .Setup(s => s.MarkReadAsync(_userId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(10);

        var request = new MarkReadRequest(Array.Empty<Guid>());

        // Act
        var response = await _client.PostAsJsonAsync("/api/events/mark-read", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("markedCount").GetInt32().Should().Be(10);
    }

    #endregion

    #region POST /api/events (CreateEvent)

    [Fact]
    public async Task CreateEvent_ValidRequest_Returns201WithLocation()
    {
        // Arrange
        var createdEvent = MakeEvent("New Event");

        _mockEventService
            .Setup(s => s.CreateEventAsync(It.IsAny<ActivityEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdEvent);

        var request = new CreateEventRequest(
            OrganizationId: _orgId,
            UserId: _userId,
            EventType: "blueprint.created",
            Severity: "Info",
            Title: "New Event",
            Message: "A new blueprint was created",
            SourceService: "blueprint-service"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/events", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/api/events/{createdEvent.Id}");

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("id").GetGuid().Should().Be(createdEvent.Id);
        doc.RootElement.GetProperty("title").GetString().Should().Be("New Event");
    }

    [Fact]
    public async Task CreateEvent_MapsAllFieldsFromRequest()
    {
        // Arrange
        ActivityEvent? capturedEvent = null;
        _mockEventService
            .Setup(s => s.CreateEventAsync(It.IsAny<ActivityEvent>(), It.IsAny<CancellationToken>()))
            .Callback<ActivityEvent, CancellationToken>((e, _) => capturedEvent = e)
            .ReturnsAsync((ActivityEvent e, CancellationToken _) => e);

        var request = new CreateEventRequest(
            OrganizationId: _orgId,
            UserId: _userId,
            EventType: "wallet.linked",
            Severity: "Warning",
            Title: "Wallet Linked",
            Message: "A wallet was linked to participant",
            SourceService: "wallet-service",
            EntityId: "wallet-abc",
            EntityType: "Wallet"
        );

        // Act
        await _client.PostAsJsonAsync("/api/events", request);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.OrganizationId.Should().Be(_orgId);
        capturedEvent.UserId.Should().Be(_userId);
        capturedEvent.EventType.Should().Be("wallet.linked");
        capturedEvent.Severity.Should().Be(EventSeverity.Warning);
        capturedEvent.Title.Should().Be("Wallet Linked");
        capturedEvent.Message.Should().Be("A wallet was linked to participant");
        capturedEvent.SourceService.Should().Be("wallet-service");
        capturedEvent.EntityId.Should().Be("wallet-abc");
        capturedEvent.EntityType.Should().Be("Wallet");
    }

    [Fact]
    public async Task CreateEvent_InvalidSeverity_DefaultsToInfo()
    {
        // Arrange
        ActivityEvent? capturedEvent = null;
        _mockEventService
            .Setup(s => s.CreateEventAsync(It.IsAny<ActivityEvent>(), It.IsAny<CancellationToken>()))
            .Callback<ActivityEvent, CancellationToken>((e, _) => capturedEvent = e)
            .ReturnsAsync((ActivityEvent e, CancellationToken _) => e);

        var request = new CreateEventRequest(
            OrganizationId: _orgId,
            UserId: _userId,
            EventType: "test.event",
            Severity: "NotARealSeverity",
            Title: "Test",
            Message: "Test message",
            SourceService: "test"
        );

        // Act
        await _client.PostAsJsonAsync("/api/events", request);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Severity.Should().Be(EventSeverity.Info);
    }

    [Fact]
    public async Task CreateEvent_WithOptionalEntityFields_MapsCorrectly()
    {
        // Arrange
        ActivityEvent? capturedEvent = null;
        _mockEventService
            .Setup(s => s.CreateEventAsync(It.IsAny<ActivityEvent>(), It.IsAny<CancellationToken>()))
            .Callback<ActivityEvent, CancellationToken>((e, _) => capturedEvent = e)
            .ReturnsAsync((ActivityEvent e, CancellationToken _) => e);

        var request = new CreateEventRequest(
            OrganizationId: _orgId,
            UserId: _userId,
            EventType: "test.event",
            Severity: "Info",
            Title: "Test",
            Message: "Test message",
            SourceService: "test"
            // EntityId and EntityType omitted (defaults to null)
        );

        // Act
        await _client.PostAsJsonAsync("/api/events", request);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.EntityId.Should().BeNull();
        capturedEvent.EntityType.Should().BeNull();
    }

    #endregion

    #region GET /api/events/admin

    [Fact]
    public async Task GetAdminEvents_AsAdministrator_ReturnsOk()
    {
        // Arrange - default test user is Administrator
        var events = new List<ActivityEvent>
        {
            MakeEvent("Admin Event 1"),
            MakeEvent("Admin Event 2")
        };

        _mockEventService
            .Setup(s => s.GetAdminEventsAsync(
                _orgId, 1, 50, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((events.AsReadOnly(), 2));

        // Act
        var response = await _client.GetAsync("/api/events/admin");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PaginatedEventsResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.TotalCount.Should().Be(2);
        body.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAdminEvents_WithUserIdFilter_PassesFilterToService()
    {
        // Arrange
        var filterUserId = Guid.NewGuid();
        _mockEventService
            .Setup(s => s.GetAdminEventsAsync(
                _orgId, 1, 50, filterUserId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<ActivityEvent>().AsReadOnly(), 0));

        // Act
        var response = await _client.GetAsync($"/api/events/admin?userId={filterUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mockEventService.Verify(s => s.GetAdminEventsAsync(
            _orgId, 1, 50, filterUserId, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAdminEvents_WithSeverityFilter_ParsesCorrectly()
    {
        // Arrange
        _mockEventService
            .Setup(s => s.GetAdminEventsAsync(
                _orgId, 1, 50, null, EventSeverity.Error, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<ActivityEvent>().AsReadOnly(), 0));

        // Act
        var response = await _client.GetAsync("/api/events/admin?severity=Error");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mockEventService.Verify(s => s.GetAdminEventsAsync(
            _orgId, 1, 50, null, EventSeverity.Error, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAdminEvents_WithPagination_PassesParameters()
    {
        // Arrange
        _mockEventService
            .Setup(s => s.GetAdminEventsAsync(
                _orgId, 2, 25, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<ActivityEvent>().AsReadOnly(), 0));

        // Act
        var response = await _client.GetAsync("/api/events/admin?page=2&pageSize=25");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mockEventService.Verify(s => s.GetAdminEventsAsync(
            _orgId, 2, 25, null, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DELETE /api/events/{id}

    [Fact]
    public async Task DeleteEvent_ExistingOwnEvent_Returns204()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _mockEventService
            .Setup(s => s.DeleteEventAsync(eventId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var response = await _client.DeleteAsync($"/api/events/{eventId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteEvent_NonExistentEvent_Returns404()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        _mockEventService
            .Setup(s => s.DeleteEventAsync(eventId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var response = await _client.DeleteAsync($"/api/events/{eventId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEvent_OtherUsersEvent_Returns404()
    {
        // Arrange - service returns false when user doesn't own the event
        var eventId = Guid.NewGuid();
        _mockEventService
            .Setup(s => s.DeleteEventAsync(eventId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var response = await _client.DeleteAsync($"/api/events/{eventId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteEvent_InvalidGuidFormat_Returns404()
    {
        // Act - non-GUID path should not match the route constraint {id:guid}
        var response = await _client.DeleteAsync("/api/events/not-a-guid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Authentication / Unauthorized Tests

    [Fact]
    public async Task GetEvents_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange - create a separate test host with no auth user
        using var noAuthClient = CreateClientWithClaims(Array.Empty<Claim>());

        // Act
        var response = await noAuthClient.GetAsync("/api/events");

        // Assert
        // With empty claims, GetUserId returns Guid.Empty => Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUnreadCount_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        using var noAuthClient = CreateClientWithClaims(Array.Empty<Claim>());

        // Act
        var response = await noAuthClient.GetAsync("/api/events/unread-count");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MarkRead_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        using var noAuthClient = CreateClientWithClaims(Array.Empty<Claim>());
        var request = new MarkReadRequest(null);

        // Act
        var response = await noAuthClient.PostAsJsonAsync("/api/events/mark-read", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteEvent_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange
        using var noAuthClient = CreateClientWithClaims(Array.Empty<Claim>());

        // Act
        var response = await noAuthClient.DeleteAsync($"/api/events/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAdminEvents_NonAdminUser_ReturnsForbidden()
    {
        // Arrange - create a client with a regular user role (not Administrator)
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim("org_id", _orgId.ToString()),
            new Claim(ClaimTypes.Role, "User") // not Administrator or SystemAdmin
        };
        using var regularClient = CreateClientWithClaims(claims);

        // Act
        var response = await regularClient.GetAsync("/api/events/admin");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAdminEvents_SystemAdminRole_ReturnsOk()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
            new Claim("org_id", _orgId.ToString()),
            new Claim(ClaimTypes.Role, "SystemAdmin")
        };

        _mockEventService
            .Setup(s => s.GetAdminEventsAsync(
                _orgId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<Guid?>(),
                It.IsAny<EventSeverity?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<ActivityEvent>().AsReadOnly(), 0));

        using var adminClient = CreateClientWithClaims(claims);

        // Act
        var response = await adminClient.GetAsync("/api/events/admin");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAdminEvents_AdminWithNoOrgClaim_ReturnsUnauthorized()
    {
        // Arrange - admin role but no org_id claim
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
            new Claim(ClaimTypes.Role, "Administrator")
            // Missing org_id claim
        };
        using var noOrgClient = CreateClientWithClaims(claims);

        // Act
        var response = await noOrgClient.GetAsync("/api/events/admin");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task GetEvents_WithSeverityCaseInsensitive_ParsesCorrectly()
    {
        // Arrange - test case-insensitive parsing: "warning" vs "Warning"
        _mockEventService
            .Setup(s => s.GetEventsAsync(
                _userId, 1, 50, false, EventSeverity.Warning, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<ActivityEvent>().AsReadOnly(), 0));

        // Act
        var response = await _client.GetAsync("/api/events?severity=warning");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mockEventService.Verify(s => s.GetEventsAsync(
            _userId, 1, 50, false, EventSeverity.Warning, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateEvent_SeverityCaseInsensitive_ParsesCorrectly()
    {
        // Arrange
        ActivityEvent? capturedEvent = null;
        _mockEventService
            .Setup(s => s.CreateEventAsync(It.IsAny<ActivityEvent>(), It.IsAny<CancellationToken>()))
            .Callback<ActivityEvent, CancellationToken>((e, _) => capturedEvent = e)
            .ReturnsAsync((ActivityEvent e, CancellationToken _) => e);

        var request = new CreateEventRequest(
            OrganizationId: _orgId,
            UserId: _userId,
            EventType: "test.event",
            Severity: "error",  // lowercase
            Title: "Test",
            Message: "Test message",
            SourceService: "test"
        );

        // Act
        await _client.PostAsJsonAsync("/api/events", request);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Severity.Should().Be(EventSeverity.Error);
    }

    [Fact]
    public async Task GetEvents_WithSubClaimInsteadOfNameIdentifier_ParsesUserId()
    {
        // Arrange - the endpoint checks both ClaimTypes.NameIdentifier and "sub"
        var altUserId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim("sub", altUserId.ToString()),
            new Claim("org_id", _orgId.ToString()),
            new Claim(ClaimTypes.Role, "User")
        };

        _mockEventService
            .Setup(s => s.GetEventsAsync(
                altUserId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(),
                It.IsAny<EventSeverity?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<ActivityEvent>().AsReadOnly(), 0));

        using var subClient = CreateClientWithClaims(claims);

        // Act
        var response = await subClient.GetAsync("/api/events");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _mockEventService.Verify(s => s.GetEventsAsync(
            altUserId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(),
            It.IsAny<EventSeverity?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetEvents_AllSeverityValues_ParseCorrectly()
    {
        // Arrange & Act & Assert - verify all enum values parse
        foreach (var severity in Enum.GetValues<EventSeverity>())
        {
            _mockEventService
                .Setup(s => s.GetEventsAsync(
                    _userId, 1, 50, false, severity, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Array.Empty<ActivityEvent>().AsReadOnly(), 0));

            var response = await _client.GetAsync($"/api/events?severity={severity}");
            response.StatusCode.Should().Be(HttpStatusCode.OK,
                $"severity '{severity}' should parse successfully");
        }
    }

    #endregion

    #region Helpers

    private ActivityEvent MakeEvent(string title = "Test Event") => new()
    {
        OrganizationId = _orgId,
        UserId = _userId,
        EventType = "test.created",
        Severity = EventSeverity.Info,
        Title = title,
        Message = "A test event occurred",
        SourceService = "test",
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddDays(90)
    };

    /// <summary>
    /// Creates a new lightweight test host with a custom set of claims.
    /// The caller is responsible for disposing the returned HttpClient.
    /// </summary>
    private HttpClient CreateClientWithClaims(Claim[] claims)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing"
        });

        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(_mockEventService.Object);

        builder.Services.AddAuthentication("TestScheme")
            .AddScheme<EventTestAuthOptions, EventTestAuthHandler>("TestScheme", opts =>
            {
                opts.CustomClaims = claims;
            });
        builder.Services.AddAuthorization();

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapEventEndpoints();
        app.StartAsync().GetAwaiter().GetResult();

        var client = app.GetTestClient();
        return client;
    }

    #endregion

    #region Response DTOs for Deserialization

    private class PaginatedEventsResponse
    {
        public EventDto[] Items { get; set; } = Array.Empty<EventDto>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    private class EventDto
    {
        public Guid Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string SourceService { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? EntityType { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    #endregion
}

#region Test Authentication Infrastructure

/// <summary>
/// Authentication options that allow per-test configuration of claims.
/// </summary>
public class EventTestAuthOptions : AuthenticationSchemeOptions
{
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Role { get; set; } = "User";
    public Claim[]? CustomClaims { get; set; }
}

/// <summary>
/// Test authentication handler that authenticates requests with configurable claims.
/// When CustomClaims is set, those are used directly. Otherwise, claims are built
/// from UserId, OrganizationId, and Role properties.
/// </summary>
public class EventTestAuthHandler : AuthenticationHandler<EventTestAuthOptions>
{
    public EventTestAuthHandler(
        IOptionsMonitor<EventTestAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        Claim[] claims;

        if (Options.CustomClaims is not null)
        {
            claims = Options.CustomClaims;
            if (claims.Length == 0)
            {
                // No claims = unauthenticated user simulation.
                // Still succeed auth (to pass RequireAuthorization) but with
                // a principal that has no NameIdentifier, so GetUserId returns Guid.Empty.
                claims = new[] { new Claim(ClaimTypes.Name, "anonymous") };
            }
        }
        else
        {
            var claimsList = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, Options.UserId.ToString()),
                new(ClaimTypes.Name, "Test User"),
                new(ClaimTypes.Role, Options.Role)
            };

            if (Options.OrganizationId != Guid.Empty)
                claimsList.Add(new Claim("org_id", Options.OrganizationId.ToString()));

            claims = claimsList.ToArray();
        }

        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

#endregion
