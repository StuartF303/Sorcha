// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Diagnostics;
using Sorcha.Cli.UI;

namespace Sorcha.Cli.Services;

/// <summary>
/// HTTP message handler that logs all requests and responses to the activity log
/// </summary>
public class LoggingHttpHandler : DelegatingHandler
{
    private readonly ActivityLog _activityLog;

    public LoggingHttpHandler(ActivityLog activityLog)
    {
        _activityLog = activityLog;
        InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var method = request.Method.ToString();
        var url = request.RequestUri?.ToString() ?? "unknown";

        string? requestBody = null;
        if (request.Content != null)
        {
            requestBody = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        _activityLog.LogRequest(method, url, requestBody);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            stopwatch.Stop();

            string? responseBody = null;
            try
            {
                responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch
            {
                // Ignore read errors
            }

            _activityLog.LogResponse((int)response.StatusCode, responseBody, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _activityLog.LogError($"Request failed: {ex.Message}", ex);
            throw;
        }
    }
}
