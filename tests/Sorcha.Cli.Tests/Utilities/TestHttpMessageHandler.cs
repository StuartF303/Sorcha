using System.Net;
using System.Text.Json;

namespace Sorcha.Cli.Tests.Utilities;

/// <summary>
/// Test HTTP message handler for mocking HTTP responses.
/// </summary>
public class TestHttpMessageHandler : HttpMessageHandler
{
    private HttpStatusCode _statusCode = HttpStatusCode.OK;
    private object? _responseContent;

    public void SetResponse(HttpStatusCode statusCode, object? content = null)
    {
        _statusCode = statusCode;
        _responseContent = content;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode);

        if (_responseContent != null)
        {
            var json = JsonSerializer.Serialize(_responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }

        return Task.FromResult(response);
    }
}
