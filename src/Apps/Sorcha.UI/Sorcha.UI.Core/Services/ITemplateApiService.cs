// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using Sorcha.UI.Core.Models.Blueprints;
using Sorcha.UI.Core.Models.Templates;

namespace Sorcha.UI.Core.Services;

/// <summary>
/// Service for backend-driven template library.
/// </summary>
public interface ITemplateApiService
{
    Task<List<TemplateListItemViewModel>> GetTemplatesAsync(string? category = null, CancellationToken cancellationToken = default);
    Task<TemplateListItemViewModel?> GetTemplateAsync(string id, CancellationToken cancellationToken = default);
    Task<BlueprintListItemViewModel?> EvaluateTemplateAsync(string id, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
    Task<bool> ValidateParametersAsync(string id, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
}
