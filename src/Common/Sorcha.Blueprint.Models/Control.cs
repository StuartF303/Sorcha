// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Sorcha Contributors

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DataAnnotations = System.ComponentModel.DataAnnotations;

namespace Sorcha.Blueprint.Models;

/// <summary>
/// A UI control element for form display
/// Links render element type to data scope
/// </summary>
public class Control
{
    /// <summary>
    /// The display type for this element
    /// </summary>
    [DataAnnotations.Required]
    [JsonPropertyName("type")]
    public ControlTypes ControlType { get; set; } = ControlTypes.Label;

    /// <summary>
    /// Label for this control
    /// </summary>
    [DataAnnotations.MaxLength(100)]
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the data values element (likely a DID)
    /// </summary>
    [DataAnnotations.MaxLength(250)]
    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    /// <summary>
    /// Determines the layout of this and embedded controls
    /// </summary>
    [JsonPropertyName("layout")]
    public LayoutTypes Layout { get; set; } = LayoutTypes.VerticalLayout;

    /// <summary>
    /// Additional properties for the control
    /// </summary>
    [JsonPropertyName("properties")]
    public JsonDocument? Properties { get; set; }

    /// <summary>
    /// Sub-elements or sub-controls
    /// </summary>
    [JsonPropertyName("elements")]
    public List<Control> Elements { get; set; } = [];

    /// <summary>
    /// Conditional display rules based on JSON Logic
    /// </summary>
    [JsonPropertyName("conditions")]
    public List<JsonNode> Conditions { get; set; } = [];
}

/// <summary>
/// Layout types for controls
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LayoutTypes
{
    VerticalLayout,
    HorizontalLayout,
    Group,          // many
    Categorization  // one of
}

/// <summary>
/// Control types for form elements
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ControlTypes
{
    [DataAnnotations.Display(Name = "Layout")]
    Layout,

    [DataAnnotations.Display(Name = "Label")]
    Label,

    [DataAnnotations.Display(Name = "Single Line Text")]
    TextLine,

    [DataAnnotations.Display(Name = "Text Area")]
    TextArea,

    [DataAnnotations.Display(Name = "Number")]
    Numeric,

    [DataAnnotations.Display(Name = "Date/Time")]
    DateTime,

    [DataAnnotations.Display(Name = "File")]
    File,

    [DataAnnotations.Display(Name = "Multiple Choice")]
    Choice,

    [DataAnnotations.Display(Name = "Checkbox")]
    Checkbox,

    [DataAnnotations.Display(Name = "Selection")]
    Selection
}
