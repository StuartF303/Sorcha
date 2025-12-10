using System.Net;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using Refit;
using Sorcha.Cli.Models;

namespace Sorcha.Cli.Services;

/// <summary>
/// Factory for creating HTTP clients with Polly resilience policies.
/// </summary>
public class HttpClientFactory
{
    private readonly IConfigurationService _configService;

    public HttpClientFactory(IConfigurationService configService)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
    }

    /// <summary>
    /// Creates a Tenant Service client for the specified profile.
    /// </summary>
    public async Task<ITenantServiceClient> CreateTenantServiceClientAsync(string profileName)
    {
        var profile = await _configService.GetProfileAsync(profileName);
        if (profile == null)
        {
            throw new InvalidOperationException($"Profile '{profileName}' does not exist.");
        }

        var httpClient = CreateHttpClient(profile, profile.TenantServiceUrl);
        return RestService.For<ITenantServiceClient>(httpClient);
    }

    /// <summary>
    /// Creates a Register Service client for the specified profile.
    /// </summary>
    public async Task<IRegisterServiceClient> CreateRegisterServiceClientAsync(string profileName)
    {
        var profile = await _configService.GetProfileAsync(profileName);
        if (profile == null)
        {
            throw new InvalidOperationException($"Profile '{profileName}' does not exist.");
        }

        var httpClient = CreateHttpClient(profile, profile.RegisterServiceUrl);
        return RestService.For<IRegisterServiceClient>(httpClient);
    }

    /// <summary>
    /// Creates a Wallet Service client for the specified profile.
    /// </summary>
    public async Task<IWalletServiceClient> CreateWalletServiceClientAsync(string profileName)
    {
        var profile = await _configService.GetProfileAsync(profileName);
        if (profile == null)
        {
            throw new InvalidOperationException($"Profile '{profileName}' does not exist.");
        }

        var httpClient = CreateHttpClient(profile, profile.WalletServiceUrl);
        return RestService.For<IWalletServiceClient>(httpClient);
    }

    /// <summary>
    /// Creates an HTTP client with resilience policies.
    /// </summary>
    private HttpClient CreateHttpClient(Profile profile, string baseUrl)
    {
        var handler = new HttpClientHandler();

        // Disable SSL verification for dev profiles if specified
        if (!profile.VerifySsl)
        {
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        }

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(profile.TimeoutSeconds)
        };

        return httpClient;
    }

    /// <summary>
    /// Gets a retry policy for transient HTTP errors.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError() // 408, 5xx errors
            .Or<TimeoutRejectedException>() // Timeout errors
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    Console.Error.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds}s due to: {outcome.Exception?.Message ?? outcome.Result.StatusCode.ToString()}");
                });
    }

    /// <summary>
    /// Gets a circuit breaker policy.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, timespan) =>
                {
                    Console.Error.WriteLine($"Circuit breaker opened for {timespan.TotalSeconds}s due to: {outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()}");
                },
                onReset: () =>
                {
                    Console.Error.WriteLine("Circuit breaker reset");
                });
    }

    /// <summary>
    /// Gets a timeout policy.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(int timeoutSeconds)
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(
            timeout: TimeSpan.FromSeconds(timeoutSeconds),
            timeoutStrategy: TimeoutStrategy.Optimistic);
    }

    /// <summary>
    /// Creates a complete resilience pipeline combining all policies.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetResiliencePipeline(int timeoutSeconds)
    {
        // Order: Timeout -> Retry -> Circuit Breaker
        return Policy.WrapAsync(
            GetTimeoutPolicy(timeoutSeconds),
            GetRetryPolicy(),
            GetCircuitBreakerPolicy());
    }
}
