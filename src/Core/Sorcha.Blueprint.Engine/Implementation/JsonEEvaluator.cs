// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.JsonE;
using Json.More;
using Sorcha.Blueprint.Engine.Interfaces;
using JsonObject = System.Text.Json.Nodes.JsonObject;

namespace Sorcha.Blueprint.Engine.Implementation;

/// <summary>
/// JSON-e template evaluator implementation using JsonE.Net
/// </summary>
public class JsonEEvaluator : IJsonEEvaluator
{
    private readonly JsonSerializerOptions _serializerOptions;

    public JsonEEvaluator()
    {
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    /// <inheritdoc />
    public async Task<JsonNode> EvaluateAsync(
        JsonNode template,
        Dictionary<string, object> context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(context);

        return await Task.Run(() =>
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                // Convert context dictionary to JsonObject
                var contextObject = ConvertContextToJsonObject(context);

                // Evaluate the template using JsonE.Net
                var result = JsonE.Evaluate(template, contextObject);

                return result ?? JsonNode.Parse("{}")!;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new InvalidOperationException(
                    $"Failed to evaluate JSON-e template: {ex.Message}",
                    ex);
            }
        }, ct);
    }

    /// <inheritdoc />
    public async Task<T?> EvaluateAsync<T>(
        JsonNode template,
        Dictionary<string, object> context,
        CancellationToken ct = default) where T : class
    {
        var result = await EvaluateAsync(template, context, ct);

        try
        {
            return result.Deserialize<T>(_serializerOptions);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize evaluated template to type {typeof(T).Name}: {ex.Message}",
                ex);
        }
    }

    /// <inheritdoc />
    public Task<TemplateValidationResult> ValidateTemplateAsync(JsonNode template)
    {
        ArgumentNullException.ThrowIfNull(template);

        return Task.Run(() =>
        {
            try
            {
                // Basic validation: try to parse and evaluate with empty context
                var emptyContext = JsonNode.Parse("{}")!.AsObject();
                _ = JsonE.Evaluate(template, emptyContext);

                return TemplateValidationResult.Success();
            }
            catch (Exception ex)
            {
                return TemplateValidationResult.Failure($"Template validation failed: {ex.Message}");
            }
        });
    }

    /// <inheritdoc />
    public async Task<EvaluationTrace> EvaluateWithTraceAsync(
        JsonNode template,
        Dictionary<string, object> context,
        CancellationToken ct = default)
    {
        var trace = new EvaluationTrace();
        var stopwatch = Stopwatch.StartNew();
        var steps = new List<TraceStep>();

        try
        {
            // Step 1: Convert context
            var stepStopwatch = Stopwatch.StartNew();
            var contextObject = ConvertContextToJsonObject(context);
            stepStopwatch.Stop();

            steps.Add(new TraceStep
            {
                Step = 1,
                Description = "Convert context to JsonObject",
                Input = JsonSerializer.Serialize(context, _serializerOptions),
                Output = contextObject.ToJsonString(),
                Duration = stepStopwatch.Elapsed
            });

            ct.ThrowIfCancellationRequested();

            // Step 2: Evaluate template
            stepStopwatch = Stopwatch.StartNew();
            var result = await EvaluateAsync(template, context, ct);
            stepStopwatch.Stop();

            steps.Add(new TraceStep
            {
                Step = 2,
                Description = "Evaluate JSON-e template",
                Input = template.ToJsonString(),
                Output = result.ToJsonString(),
                Duration = stepStopwatch.Elapsed
            });

            stopwatch.Stop();

            trace.Result = result;
            trace.Steps = steps;
            trace.Duration = stopwatch.Elapsed;
            trace.Success = true;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            trace.Success = false;
            trace.Error = ex.Message;
            trace.Steps = steps;
            trace.Duration = stopwatch.Elapsed;
        }

        return trace;
    }

    /// <summary>
    /// Convert a context dictionary to JsonObject for template evaluation
    /// </summary>
    private JsonObject ConvertContextToJsonObject(Dictionary<string, object> context)
    {
        var json = JsonSerializer.Serialize(context, _serializerOptions);
        return JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
    }
}
