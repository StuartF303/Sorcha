// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Sorcha Contributors

using Refit;
using Sorcha.Cli.Models;

namespace Sorcha.Cli.Services;

/// <summary>
/// Refit client interface for the Blueprint Service API.
/// </summary>
public interface IBlueprintServiceClient
{
    /// <summary>
    /// Lists all blueprints.
    /// </summary>
    [Get("/api/blueprints")]
    Task<List<BlueprintSummary>> ListBlueprintsAsync([Header("Authorization")] string authorization);

    /// <summary>
    /// Gets a blueprint by ID.
    /// </summary>
    [Get("/api/blueprints/{id}")]
    Task<BlueprintDetail> GetBlueprintAsync(string id, [Header("Authorization")] string authorization);

    /// <summary>
    /// Creates a new blueprint from JSON.
    /// </summary>
    [Post("/api/blueprints")]
    Task<BlueprintDetail> CreateBlueprintAsync([Body] CreateBlueprintRequest request, [Header("Authorization")] string authorization);

    /// <summary>
    /// Deletes a blueprint.
    /// </summary>
    [Delete("/api/blueprints/{id}")]
    Task DeleteBlueprintAsync(string id, [Header("Authorization")] string authorization);

    /// <summary>
    /// Publishes a blueprint to a register.
    /// </summary>
    [Post("/api/blueprints/{id}/publish")]
    Task<PublishBlueprintResponse> PublishBlueprintAsync(string id, [Body] PublishBlueprintRequest request, [Header("Authorization")] string authorization);

    /// <summary>
    /// Lists blueprint versions.
    /// </summary>
    [Get("/api/blueprints/{id}/versions")]
    Task<List<BlueprintVersion>> ListBlueprintVersionsAsync(string id, [Header("Authorization")] string authorization);

    /// <summary>
    /// Lists instances of a blueprint.
    /// </summary>
    [Get("/api/instances")]
    Task<List<BlueprintInstance>> ListInstancesAsync([Query] string? blueprintId, [Header("Authorization")] string authorization);
}
