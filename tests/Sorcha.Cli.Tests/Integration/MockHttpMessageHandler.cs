using System.Net;
using System.Text.Json;

namespace Sorcha.Cli.Tests.Integration;

/// <summary>
/// Mock HTTP message handler for testing HTTP client interactions.
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, Func<HttpRequestMessage, HttpResponseMessage>> _handlers = new();
    private readonly List<HttpRequestMessage> _requests = new();

    /// <summary>
    /// Gets all requests that have been sent through this handler.
    /// </summary>
    public IReadOnlyList<HttpRequestMessage> Requests => _requests.AsReadOnly();

    /// <summary>
    /// Configures a response for a specific HTTP method and path pattern.
    /// </summary>
    public void SetupResponse(HttpMethod method, string pathPattern, HttpStatusCode statusCode, object? responseBody = null)
    {
        var key = $"{method}:{pathPattern}";
        _handlers[key] = request =>
        {
            var response = new HttpResponseMessage(statusCode);
            if (responseBody != null)
            {
                var json = JsonSerializer.Serialize(responseBody);
                response.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            }
            return response;
        };
    }

    /// <summary>
    /// Configures a custom response handler for a specific HTTP method and path pattern.
    /// </summary>
    public void SetupCustomResponse(HttpMethod method, string pathPattern, Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        var key = $"{method}:{pathPattern}";
        _handlers[key] = handler;
    }

    /// <summary>
    /// Sends an HTTP request through this mock handler.
    /// </summary>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _requests.Add(request);

        // Try to find exact match first
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var key = $"{request.Method}:{path}";

        if (_handlers.TryGetValue(key, out var handler))
        {
            return Task.FromResult(handler(request));
        }

        // Try to find pattern match
        foreach (var kvp in _handlers)
        {
            var parts = kvp.Key.Split(':');
            if (parts.Length == 2 && parts[0] == request.Method.ToString())
            {
                var pattern = parts[1];
                if (IsMatch(path, pattern))
                {
                    return Task.FromResult(kvp.Value(request));
                }
            }
        }

        // No handler found, return 404
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent($"No mock response configured for {request.Method} {path}")
        });
    }

    /// <summary>
    /// Simple pattern matching for paths (supports wildcards like /api/organizations/*).
    /// </summary>
    private static bool IsMatch(string path, string pattern)
    {
        if (pattern == path)
            return true;

        if (pattern.Contains('*'))
        {
            var regex = "^" + System.Text.RegularExpressions.Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            return System.Text.RegularExpressions.Regex.IsMatch(path, regex);
        }

        return false;
    }

    /// <summary>
    /// Clears all configured handlers and request history.
    /// </summary>
    public void Reset()
    {
        _handlers.Clear();
        _requests.Clear();
    }
}
