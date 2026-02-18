// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Sorcha.UI.Core.Models.Common;
using Sorcha.UI.Core.Models.Workflows;

namespace Sorcha.UI.Core.Services;

/// <summary>
/// Implementation of <see cref="IWorkflowService"/> calling the Blueprint Service orchestration API.
/// </summary>
public class WorkflowService : IWorkflowService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(HttpClient httpClient, ILogger<WorkflowService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PaginatedList<WorkflowInstanceViewModel>> GetMyWorkflowsAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/instances?page={page}&pageSize={pageSize}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch workflows: {StatusCode}", response.StatusCode);
                return new PaginatedList<WorkflowInstanceViewModel>();
            }

            var result = await response.Content.ReadFromJsonAsync<PaginatedList<WorkflowInstanceViewModel>>(cancellationToken: cancellationToken);
            return result ?? new PaginatedList<WorkflowInstanceViewModel>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching workflows");
            return new PaginatedList<WorkflowInstanceViewModel>();
        }
    }

    public async Task<WorkflowInstanceViewModel?> GetWorkflowAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<WorkflowInstanceViewModel>(
                $"/api/instances/{instanceId}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching workflow {InstanceId}", instanceId);
            return null;
        }
    }

    public async Task<List<PendingActionViewModel>> GetPendingActionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/instances?status=active", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            var instances = await response.Content.ReadFromJsonAsync<PaginatedList<WorkflowInstanceViewModel>>(cancellationToken: cancellationToken);
            if (instances?.Items is null || instances.Items.Count == 0) return [];

            var actions = new List<PendingActionViewModel>();
            foreach (var instance in instances.Items.Where(i => i.Status == "active"))
            {
                try
                {
                    var nextActions = await _httpClient.GetFromJsonAsync<List<PendingActionViewModel>>(
                        $"/api/instances/{instance.InstanceId}/next-actions", cancellationToken);
                    if (nextActions != null)
                    {
                        actions.AddRange(nextActions);
                    }
                }
                catch
                {
                    // Continue with other instances if one fails
                }
            }

            return actions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching pending actions");
            return [];
        }
    }

    public async Task<WorkflowInstanceViewModel?> CreateInstanceAsync(string blueprintId, string registerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "/api/instances",
                new { blueprintId, registerId },
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to create instance for blueprint {BlueprintId}: {StatusCode}", blueprintId, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<WorkflowInstanceViewModel>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating instance for blueprint {BlueprintId}", blueprintId);
            return null;
        }
    }

    public async Task<ActionSubmissionResultViewModel?> SubmitActionExecuteAsync(ActionExecuteRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var body = new
            {
                blueprintId = request.BlueprintId,
                actionId = request.ActionId,
                instanceId = request.InstanceId,
                senderWallet = request.SenderWallet,
                registerAddress = request.RegisterAddress,
                payloadData = request.PayloadData
            };

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post,
                $"/api/instances/{request.InstanceId}/actions/{request.ActionId}/execute");
            httpRequest.Content = JsonContent.Create(body);
            httpRequest.Headers.Add("X-Delegation-Token", "delegate");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to execute action {ActionId} on instance {InstanceId}: {StatusCode}",
                    request.ActionId, request.InstanceId, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ActionSubmissionResultViewModel>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing action {ActionId} on instance {InstanceId}", request.ActionId, request.InstanceId);
            return null;
        }
    }

    public async Task<bool> SubmitActionAsync(ActionSubmissionViewModel submission, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/instances/{submission.InstanceId}/actions/{submission.ActionId}/execute",
                new { data = submission.Data },
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting action {ActionId}", submission.ActionId);
            return false;
        }
    }

    public async Task<bool> RejectActionAsync(string instanceId, string actionId, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/instances/{instanceId}/actions/{actionId}/reject",
                new { reason },
                cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting action {ActionId}", actionId);
            return false;
        }
    }
}
