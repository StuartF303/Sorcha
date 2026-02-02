// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Polly;
using Polly.Extensions.Http;
using Sorcha.Blueprint.Service.Services;
using Sorcha.Blueprint.Service.Services.Interfaces;

namespace Sorcha.Blueprint.Service.Extensions;

/// <summary>
/// Extension methods for registering chat services.
/// </summary>
public static class ChatServicesExtensions
{
    /// <summary>
    /// Adds AI-assisted blueprint chat services to the service collection.
    /// </summary>
    public static IServiceCollection AddChatServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure AI provider options
        services.Configure<AIProviderOptions>(configuration.GetSection(AIProviderOptions.SectionName));

        // Register session store (Redis-backed)
        services.AddScoped<IChatSessionStore, RedisChatSessionStore>();

        // Register tool executor
        services.AddScoped<IBlueprintToolExecutor, BlueprintToolExecutor>();

        // Register orchestration service
        services.AddScoped<IChatOrchestrationService, ChatOrchestrationService>();

        // Register AI provider with Polly retry policy
        services.AddScoped<IAIProviderService, AnthropicProviderService>();

        return services;
    }

    /// <summary>
    /// Creates a retry policy for AI provider HTTP calls.
    /// 3 retries with exponential backoff: 2s, 4s, 8s (per FR-016).
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    // Log retry attempts
                    context.TryGetValue("logger", out var logger);
                    (logger as ILogger)?.LogWarning(
                        "AI provider request failed, retrying in {Delay}s. Attempt {Attempt}/3",
                        timespan.TotalSeconds, retryAttempt);
                });
    }
}
