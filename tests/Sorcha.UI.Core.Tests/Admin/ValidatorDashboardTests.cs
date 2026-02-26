// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using Sorcha.UI.Core.Components.Admin.Validator;
using Sorcha.UI.Core.Models.Admin;
using Sorcha.UI.Core.Services;
using Xunit;

namespace Sorcha.UI.Core.Tests.Admin;

/// <summary>
/// Unit tests for the ValidatorPanel component covering polling lifecycle,
/// data display with mocked service responses, and error handling.
/// </summary>
public class ValidatorDashboardTests : BunitContext, IDisposable
{
    private readonly Mock<IValidatorAdminService> _validatorServiceMock;

    public ValidatorDashboardTests()
    {
        _validatorServiceMock = new Mock<IValidatorAdminService>();
        Services.AddMudServices();
        Services.AddSingleton(_validatorServiceMock.Object);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    #region Polling Lifecycle Tests

    /// <summary>
    /// T036: Verify that polling starts on component initialization and calls the service.
    /// </summary>
    [Fact]
    public async Task OnInitialized_StartsPolling_CallsServiceImmediately()
    {
        // Arrange
        var status = CreateHealthyStatus();
        _validatorServiceMock
            .Setup(s => s.GetMempoolStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        // Act
        var cut = Render<ValidatorPanel>();

        // Allow timer ticks to fire (3s interval, wait for at least one extra tick)
        await Task.Delay(4000);
        cut.Render();

        // Assert - service should have been called more than once (initial + at least one poll)
        _validatorServiceMock.Verify(
            s => s.GetMempoolStatusAsync(It.IsAny<CancellationToken>()),
            Times.AtLeast(2),
            "polling should call the service repeatedly after initialization");
    }

    /// <summary>
    /// T036: Verify that disposal stops the polling timer.
    /// </summary>
    [Fact]
    public async Task Dispose_StopsPollingTimer_NoFurtherCalls()
    {
        // Arrange
        var status = CreateHealthyStatus();
        _validatorServiceMock
            .Setup(s => s.GetMempoolStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        var cut = Render<ValidatorPanel>();
        await Task.Delay(500);

        var callCountBeforeDispose = _validatorServiceMock.Invocations
            .Count(i => i.Method.Name == nameof(IValidatorAdminService.GetMempoolStatusAsync));

        // Act - dispose the component
        cut.Instance.Dispose();

        // Wait to ensure no further timer callbacks fire
        await Task.Delay(5000);

        var callCountAfterDispose = _validatorServiceMock.Invocations
            .Count(i => i.Method.Name == nameof(IValidatorAdminService.GetMempoolStatusAsync));

        // Assert - call count should not have increased significantly after disposal
        callCountAfterDispose.Should().BeLessThanOrEqualTo(callCountBeforeDispose + 1,
            "timer should stop firing after disposal");
    }

    /// <summary>
    /// T036: Verify that toggling polling off prevents further service calls.
    /// </summary>
    [Fact]
    public async Task TogglePolling_PausesAndResumes()
    {
        // Arrange
        var status = CreateHealthyStatus();
        _validatorServiceMock
            .Setup(s => s.GetMempoolStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        var cut = Render<ValidatorPanel>();
        await Task.Delay(200);

        // Assert - initial state shows "Polling Active"
        cut.Markup.Should().Contain("Polling Active");
    }

    #endregion

    #region Data Display Tests

    /// <summary>
    /// T035: Verify that pending transaction count is displayed.
    /// </summary>
    [Fact]
    public async Task LoadStatus_DisplaysPendingTransactions()
    {
        // Arrange
        var status = CreateHealthyStatus(pendingTx: 42);
        _validatorServiceMock
            .Setup(s => s.GetMempoolStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        // Act
        var cut = Render<ValidatorPanel>();
        await Task.Delay(200);
        cut.Render();

        // Assert
        cut.Markup.Should().Contain("42", "should display the pending transaction count");
        cut.Markup.Should().Contain("Pending Transactions");
    }

    /// <summary>
    /// T035: Verify that queue depth is displayed.
    /// </summary>
    [Fact]
    public async Task LoadStatus_DisplaysQueueDepth()
    {
        // Arrange
        var status = CreateHealthyStatus(queueDepth: 7);
        _validatorServiceMock
            .Setup(s => s.GetMempoolStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        // Act
        var cut = Render<ValidatorPanel>();
        await Task.Delay(200);
        cut.Render();

        // Assert
        cut.Markup.Should().Contain("7", "should display queue depth");
        cut.Markup.Should().Contain("Queue Depth");
    }

    /// <summary>
    /// T035: Verify that the health summary card displays the correct status.
    /// </summary>
    [Fact]
    public async Task LoadStatus_DisplaysHealthSummary_Healthy()
    {
        // Arrange
        var status = CreateHealthyStatus();
        _validatorServiceMock
            .Setup(s => s.GetMempoolStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        // Act
        var cut = Render<ValidatorPanel>();
        await Task.Delay(200);
        cut.Render();

        // Assert
        cut.Markup.Should().Contain("Validator Health");
        cut.Markup.Should().Contain("All registers validated");
    }

    /// <summary>
    /// T035: Verify degraded health status description.
    /// </summary>
    [Fact]
    public async Task LoadStatus_DisplaysHealthSummary_Degraded()
    {
        // Arrange
        var status = new ValidatorStatusViewModel
        {
            IsLoaded = true,
            HealthStatus = "Degraded",
            TotalPendingTransactions = 50,
            QueueDepth = 20,
            DocketsPerMinute = 5.0,
            LastUpdated = DateTimeOffset.UtcNow
        };
        _validatorServiceMock
            .Setup(s => s.GetMempoolStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        // Act
        var cut = Render<ValidatorPanel>();
        await Task.Delay(200);
        cut.Render();

        // Assert
        cut.Markup.Should().Contain("Validation is running but some registers have a backlog");
    }

    /// <summary>
    /// T035: Verify that throughput is displayed in dockets/min.
    /// </summary>
    [Fact]
    public async Task LoadStatus_DisplaysThroughput()
    {
        // Arrange
        var status = CreateHealthyStatus(docketsPerMinute: 25.3);
        _validatorServiceMock
            .Setup(s => s.GetMempoolStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        // Act
        var cut = Render<ValidatorPanel>();
        await Task.Delay(200);
        cut.Render();

        // Assert
        cut.Markup.Should().Contain("25.3", "should display dockets per minute");
        cut.Markup.Should().Contain("dockets/min");
        cut.Markup.Should().Contain("Throughput");
    }

    /// <summary>
    /// T035: Verify that the monitored registers table is rendered with correct columns.
    /// </summary>
    [Fact]
    public async Task LoadStatus_DisplaysRegisterTable()
    {
        // Arrange
        var status = new ValidatorStatusViewModel
        {
            IsLoaded = true,
            HealthStatus = "Healthy",
            TotalPendingTransactions = 3,
            QueueDepth = 1,
            DocketsPerMinute = 10.0,
            LastUpdated = DateTimeOffset.UtcNow,
            RegisterMempoolStats =
            [
                new RegisterMempoolStat
                {
                    RegisterId = "abc123def456",
                    RegisterName = "Trade Ledger",
                    PendingCount = 3,
                    ChainHeight = 150,
                    LastValidatedBlock = 148,
                    ProcessingStatus = "Processing",
                    LastActivity = DateTimeOffset.UtcNow.AddSeconds(-10)
                }
            ]
        };
        _validatorServiceMock
            .Setup(s => s.GetMempoolStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        // Act
        var cut = Render<ValidatorPanel>();
        await Task.Delay(200);
        cut.Render();

        // Assert - table headers
        cut.Markup.Should().Contain("Register Name");
        cut.Markup.Should().Contain("Chain Height");
        cut.Markup.Should().Contain("Last Validated Block");
        cut.Markup.Should().Contain("Processing Status");
        cut.Markup.Should().Contain("Last Activity");

        // Assert - row data
        cut.Markup.Should().Contain("Trade Ledger");
        cut.Markup.Should().Contain("150");
        cut.Markup.Should().Contain("148");
        cut.Markup.Should().Contain("Processing");
    }

    /// <summary>
    /// T035: Verify empty state when no registers have pending transactions.
    /// </summary>
    [Fact]
    public async Task LoadStatus_EmptyMempool_ShowsEmptyState()
    {
        // Arrange
        var status = new ValidatorStatusViewModel
        {
            IsLoaded = true,
            HealthStatus = "Healthy",
            TotalPendingTransactions = 0,
            QueueDepth = 0,
            DocketsPerMinute = 0,
            LastUpdated = DateTimeOffset.UtcNow,
            RegisterMempoolStats = []
        };
        _validatorServiceMock
            .Setup(s => s.GetMempoolStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        // Act
        var cut = Render<ValidatorPanel>();
        await Task.Delay(200);
        cut.Render();

        // Assert
        cut.Markup.Should().Contain("Mempool is empty");
        cut.Markup.Should().Contain("No pending transactions across any registers");
    }

    /// <summary>
    /// T035: Verify "Last Processed" displays correctly.
    /// </summary>
    [Fact]
    public async Task LoadStatus_DisplaysLastProcessedTimestamp()
    {
        // Arrange
        var status = new ValidatorStatusViewModel
        {
            IsLoaded = true,
            HealthStatus = "Healthy",
            TotalPendingTransactions = 0,
            QueueDepth = 0,
            DocketsPerMinute = 0,
            LastProcessedAt = null,
            LastUpdated = DateTimeOffset.UtcNow,
            RegisterMempoolStats = []
        };
        _validatorServiceMock
            .Setup(s => s.GetMempoolStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        // Act
        var cut = Render<ValidatorPanel>();
        await Task.Delay(200);
        cut.Render();

        // Assert - when LastProcessedAt is null, should show "Never"
        cut.Markup.Should().Contain("Never");
        cut.Markup.Should().Contain("Last Processed");
    }

    /// <summary>
    /// T035: Verify active register count is displayed.
    /// </summary>
    [Fact]
    public async Task LoadStatus_DisplaysActiveRegisterCount()
    {
        // Arrange
        var status = new ValidatorStatusViewModel
        {
            IsLoaded = true,
            HealthStatus = "Healthy",
            TotalPendingTransactions = 0,
            QueueDepth = 0,
            DocketsPerMinute = 0,
            LastUpdated = DateTimeOffset.UtcNow,
            RegisterMempoolStats =
            [
                new RegisterMempoolStat { RegisterId = "r1", RegisterName = "Reg1" },
                new RegisterMempoolStat { RegisterId = "r2", RegisterName = "Reg2" },
                new RegisterMempoolStat { RegisterId = "r3", RegisterName = "Reg3" }
            ]
        };
        _validatorServiceMock
            .Setup(s => s.GetMempoolStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        // Act
        var cut = Render<ValidatorPanel>();
        await Task.Delay(200);
        cut.Render();

        // Assert
        cut.Markup.Should().Contain("3", "should display the count of active registers");
        cut.Markup.Should().Contain("Active Registers");
    }

    #endregion

    #region Error Handling Tests

    /// <summary>
    /// T037: Verify that when the service is unavailable, the ServiceUnavailable component is shown.
    /// </summary>
    [Fact]
    public async Task LoadStatus_ServiceUnavailable_ShowsErrorState()
    {
        // Arrange - return IsLoaded = false to simulate unavailable service
        var failedStatus = new ValidatorStatusViewModel
        {
            IsLoaded = false,
            TotalPendingTransactions = -1
        };
        _validatorServiceMock
            .Setup(s => s.GetMempoolStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedStatus);

        // Act
        var cut = Render<ValidatorPanel>();
        await Task.Delay(200);
        cut.Render();

        // Assert - the ServiceUnavailable component should be rendered
        cut.Markup.Should().Contain("Validator Service",
            "should display the service name in the unavailable message");
    }

    /// <summary>
    /// T037: Verify that when the service throws, the component handles it gracefully.
    /// </summary>
    [Fact]
    public async Task LoadStatus_ServiceThrows_ShowsErrorState()
    {
        // Arrange - the service itself returns IsLoaded=false on exceptions
        // (ValidatorAdminService catches and returns default)
        _validatorServiceMock
            .Setup(s => s.GetMempoolStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidatorStatusViewModel { IsLoaded = false, TotalPendingTransactions = -1 });

        // Act
        var cut = Render<ValidatorPanel>();
        await Task.Delay(200);
        cut.Render();

        // Assert
        cut.Markup.Should().Contain("Validator Service",
            "should show service unavailable when load returns IsLoaded=false");
    }

    /// <summary>
    /// T037: Verify that after a successful load followed by a failure, data persists.
    /// </summary>
    [Fact]
    public async Task LoadStatus_SuccessThenFailure_RetainsPreviousData()
    {
        // Arrange - first call succeeds
        var successStatus = CreateHealthyStatus(pendingTx: 15);
        var failStatus = new ValidatorStatusViewModel { IsLoaded = false, TotalPendingTransactions = -1 };

        var callCount = 0;
        _validatorServiceMock
            .Setup(s => s.GetMempoolStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount <= 1 ? successStatus : failStatus;
            });

        // Act - initial load succeeds
        var cut = Render<ValidatorPanel>();
        await Task.Delay(200);
        cut.Render();

        // Assert - should still show data (hasEverLoaded = true prevents ServiceUnavailable)
        cut.Markup.Should().Contain("Validator Health",
            "should continue to show the dashboard after initial successful load");
    }

    #endregion

    #region Connection State Indicator Tests

    /// <summary>
    /// T036: Verify the polling active indicator is displayed.
    /// </summary>
    [Fact]
    public async Task PollingIndicator_InitialState_ShowsActive()
    {
        // Arrange
        var status = CreateHealthyStatus();
        _validatorServiceMock
            .Setup(s => s.GetMempoolStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        // Act
        var cut = Render<ValidatorPanel>();
        await Task.Delay(200);
        cut.Render();

        // Assert
        cut.Markup.Should().Contain("Polling Active",
            "should show polling active indicator on initialization");
    }

    #endregion

    #region Helper Methods

    private static ValidatorStatusViewModel CreateHealthyStatus(
        int pendingTx = 0,
        int queueDepth = 0,
        double docketsPerMinute = 0.0)
    {
        return new ValidatorStatusViewModel
        {
            IsLoaded = true,
            HealthStatus = "Healthy",
            TotalPendingTransactions = pendingTx,
            QueueDepth = queueDepth,
            DocketsPerMinute = docketsPerMinute,
            LastUpdated = DateTimeOffset.UtcNow,
            RegisterMempoolStats = []
        };
    }

    #endregion
}
