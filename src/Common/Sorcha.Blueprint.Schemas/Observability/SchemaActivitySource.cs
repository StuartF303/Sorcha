// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using System.Diagnostics;

namespace Sorcha.Blueprint.Schemas.Observability;

/// <summary>
/// OpenTelemetry activity source for distributed tracing in Schema operations.
/// </summary>
/// <remarks>
/// Provides comprehensive tracing for:
/// - Schema retrieval (get by identifier, list)
/// - Schema CRUD operations (create, update, delete)
/// - Schema lifecycle (deprecate, activate, publish)
/// - External schema operations (search, import)
/// </remarks>
public sealed class SchemaActivitySource : IDisposable
{
    private readonly ActivitySource _activitySource;

    /// <summary>
    /// Activity source name for Schema operations.
    /// </summary>
    public const string ActivitySourceName = "Sorcha.Blueprint.Schemas";

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaActivitySource"/> class.
    /// </summary>
    public SchemaActivitySource()
    {
        _activitySource = new ActivitySource(ActivitySourceName, "1.0.0");
    }

    /// <summary>
    /// Starts an activity for retrieving system schemas.
    /// </summary>
    /// <returns>Activity instance or null if not sampled.</returns>
    public Activity? StartGetSystemSchemasActivity()
    {
        var activity = _activitySource.StartActivity(
            name: "schema.get.system",
            kind: ActivityKind.Internal);

        activity?.SetTag("schema.operation", "get_system");
        activity?.SetTag("schema.category", "system");

        return activity;
    }

    /// <summary>
    /// Starts an activity for retrieving a schema by identifier.
    /// </summary>
    /// <param name="identifier">Schema identifier.</param>
    /// <param name="organizationId">Organization ID.</param>
    /// <returns>Activity instance or null if not sampled.</returns>
    public Activity? StartGetByIdentifierActivity(string identifier, string? organizationId)
    {
        var activity = _activitySource.StartActivity(
            name: "schema.get.byidentifier",
            kind: ActivityKind.Internal);

        activity?.SetTag("schema.identifier", identifier);
        activity?.SetTag("schema.organization_id", organizationId ?? "none");
        activity?.SetTag("schema.operation", "get_by_identifier");

        return activity;
    }

    /// <summary>
    /// Starts an activity for listing schemas.
    /// </summary>
    /// <param name="category">Category filter.</param>
    /// <param name="status">Status filter.</param>
    /// <param name="search">Search term.</param>
    /// <param name="organizationId">Organization ID.</param>
    /// <returns>Activity instance or null if not sampled.</returns>
    public Activity? StartListActivity(
        string? category,
        string? status,
        string? search,
        string? organizationId)
    {
        var activity = _activitySource.StartActivity(
            name: "schema.list",
            kind: ActivityKind.Internal);

        activity?.SetTag("schema.operation", "list");
        activity?.SetTag("schema.category_filter", category ?? "all");
        activity?.SetTag("schema.status_filter", status ?? "all");
        activity?.SetTag("schema.has_search", !string.IsNullOrEmpty(search));
        activity?.SetTag("schema.organization_id", organizationId ?? "none");

        return activity;
    }

    /// <summary>
    /// Starts an activity for creating a schema.
    /// </summary>
    /// <param name="identifier">Schema identifier.</param>
    /// <param name="category">Schema category.</param>
    /// <param name="organizationId">Organization ID.</param>
    /// <returns>Activity instance or null if not sampled.</returns>
    public Activity? StartCreateActivity(string identifier, string category, string? organizationId)
    {
        var activity = _activitySource.StartActivity(
            name: "schema.create",
            kind: ActivityKind.Internal);

        activity?.SetTag("schema.identifier", identifier);
        activity?.SetTag("schema.category", category);
        activity?.SetTag("schema.organization_id", organizationId ?? "none");
        activity?.SetTag("schema.operation", "create");

        return activity;
    }

    /// <summary>
    /// Starts an activity for updating a schema.
    /// </summary>
    /// <param name="identifier">Schema identifier.</param>
    /// <returns>Activity instance or null if not sampled.</returns>
    public Activity? StartUpdateActivity(string identifier)
    {
        var activity = _activitySource.StartActivity(
            name: "schema.update",
            kind: ActivityKind.Internal);

        activity?.SetTag("schema.identifier", identifier);
        activity?.SetTag("schema.operation", "update");

        return activity;
    }

    /// <summary>
    /// Starts an activity for deleting a schema.
    /// </summary>
    /// <param name="identifier">Schema identifier.</param>
    /// <param name="organizationId">Organization ID.</param>
    /// <returns>Activity instance or null if not sampled.</returns>
    public Activity? StartDeleteActivity(string identifier, string organizationId)
    {
        var activity = _activitySource.StartActivity(
            name: "schema.delete",
            kind: ActivityKind.Internal);

        activity?.SetTag("schema.identifier", identifier);
        activity?.SetTag("schema.organization_id", organizationId);
        activity?.SetTag("schema.operation", "delete");

        return activity;
    }

    /// <summary>
    /// Starts an activity for deprecating a schema.
    /// </summary>
    /// <param name="identifier">Schema identifier.</param>
    /// <returns>Activity instance or null if not sampled.</returns>
    public Activity? StartDeprecateActivity(string identifier)
    {
        var activity = _activitySource.StartActivity(
            name: "schema.lifecycle.deprecate",
            kind: ActivityKind.Internal);

        activity?.SetTag("schema.identifier", identifier);
        activity?.SetTag("schema.operation", "deprecate");

        return activity;
    }

    /// <summary>
    /// Starts an activity for activating a schema.
    /// </summary>
    /// <param name="identifier">Schema identifier.</param>
    /// <returns>Activity instance or null if not sampled.</returns>
    public Activity? StartActivateActivity(string identifier)
    {
        var activity = _activitySource.StartActivity(
            name: "schema.lifecycle.activate",
            kind: ActivityKind.Internal);

        activity?.SetTag("schema.identifier", identifier);
        activity?.SetTag("schema.operation", "activate");

        return activity;
    }

    /// <summary>
    /// Starts an activity for publishing a schema globally.
    /// </summary>
    /// <param name="identifier">Schema identifier.</param>
    /// <param name="organizationId">Organization ID.</param>
    /// <returns>Activity instance or null if not sampled.</returns>
    public Activity? StartPublishGloballyActivity(string identifier, string organizationId)
    {
        var activity = _activitySource.StartActivity(
            name: "schema.lifecycle.publish",
            kind: ActivityKind.Internal);

        activity?.SetTag("schema.identifier", identifier);
        activity?.SetTag("schema.organization_id", organizationId);
        activity?.SetTag("schema.operation", "publish_globally");

        return activity;
    }

    /// <summary>
    /// Starts an activity for searching external schemas.
    /// </summary>
    /// <param name="query">Search query.</param>
    /// <param name="provider">External provider name.</param>
    /// <returns>Activity instance or null if not sampled.</returns>
    public Activity? StartExternalSearchActivity(string query, string provider)
    {
        var activity = _activitySource.StartActivity(
            name: "schema.external.search",
            kind: ActivityKind.Client);

        activity?.SetTag("schema.operation", "external_search");
        activity?.SetTag("schema.external_provider", provider);
        activity?.SetTag("schema.search_query", query);

        return activity;
    }

    /// <summary>
    /// Starts an activity for importing an external schema.
    /// </summary>
    /// <param name="schemaUrl">External schema URL.</param>
    /// <param name="provider">External provider name.</param>
    /// <returns>Activity instance or null if not sampled.</returns>
    public Activity? StartExternalImportActivity(string schemaUrl, string provider)
    {
        var activity = _activitySource.StartActivity(
            name: "schema.external.import",
            kind: ActivityKind.Client);

        activity?.SetTag("schema.operation", "external_import");
        activity?.SetTag("schema.external_provider", provider);
        activity?.SetTag("schema.external_url", schemaUrl);

        return activity;
    }

    /// <summary>
    /// Records a successful operation outcome on the activity.
    /// </summary>
    /// <param name="activity">Activity to update.</param>
    /// <param name="resultCount">Number of results (optional).</param>
    public void RecordSuccess(Activity? activity, int? resultCount = null)
    {
        if (activity is null) return;

        activity.SetTag("schema.success", true);

        if (resultCount.HasValue)
        {
            activity.SetTag("schema.result_count", resultCount.Value);
        }

        activity.SetStatus(ActivityStatusCode.Ok);
    }

    /// <summary>
    /// Records a "not found" outcome on the activity.
    /// </summary>
    /// <param name="activity">Activity to update.</param>
    public void RecordNotFound(Activity? activity)
    {
        if (activity is null) return;

        activity.SetTag("schema.success", false);
        activity.SetTag("schema.not_found", true);
        activity.SetStatus(ActivityStatusCode.Ok, "Not found");
    }

    /// <summary>
    /// Records a failed operation outcome on the activity.
    /// </summary>
    /// <param name="activity">Activity to update.</param>
    /// <param name="exception">Exception that caused the failure.</param>
    public void RecordFailure(Activity? activity, Exception exception)
    {
        if (activity is null) return;

        activity.SetTag("schema.success", false);
        activity.SetTag("error.type", exception.GetType().FullName);
        activity.SetTag("error.message", exception.Message);

        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
    }

    /// <summary>
    /// Disposes the activity source.
    /// </summary>
    public void Dispose()
    {
        _activitySource.Dispose();
    }
}
