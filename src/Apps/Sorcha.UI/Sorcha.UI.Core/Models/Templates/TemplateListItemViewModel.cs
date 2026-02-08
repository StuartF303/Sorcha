// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

namespace Sorcha.UI.Core.Models.Templates;

/// <summary>
/// View model for template list items.
/// </summary>
public record TemplateListItemViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public int UsageCount { get; init; }
    public List<TemplateParameter> Parameters { get; init; } = [];
}

/// <summary>
/// A configurable parameter for a template.
/// </summary>
public record TemplateParameter
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Type { get; init; } = "string";
    public string? DefaultValue { get; init; }
    public bool Required { get; init; }
}
