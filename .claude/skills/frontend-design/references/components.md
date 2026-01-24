# Components Reference

## Contents
- MudBlazor Fundamentals
- Spacing & Layout Classes
- Custom Component Patterns
- Icon System
- Form Components

## MudBlazor Fundamentals

### Button Variants

```razor
@* Contained (default) *@
<MudButton Color="Color.Primary" Variant="Variant.Filled">Save</MudButton>

@* Outlined *@
<MudButton Color="Color.Primary" Variant="Variant.Outlined">Cancel</MudButton>

@* Text only *@
<MudButton Color="Color.Primary" Variant="Variant.Text">Learn More</MudButton>

@* With icon *@
<MudButton Color="Color.Primary" StartIcon="@Icons.Material.Filled.Add">
    Add Item
</MudButton>
```

### Paper & Elevation

```razor
@* Elevation levels: 0 (flat) to 24 (highest) *@
<MudPaper Elevation="0" Class="pa-4">Flat surface</MudPaper>
<MudPaper Elevation="1" Class="pa-4">Subtle shadow</MudPaper>
<MudPaper Elevation="2" Class="pa-4">Standard card</MudPaper>
<MudPaper Elevation="4" Class="pa-4">Raised panel</MudPaper>
```

## Spacing & Layout Classes

MudBlazor uses Bootstrap-style utility classes:

| Class Pattern | Property | Sizes |
|---------------|----------|-------|
| `pa-{n}` | padding-all | 0-16 (0.25rem increments) |
| `ma-{n}` | margin-all | 0-16 |
| `pt-{n}` | padding-top | 0-16 |
| `mb-{n}` | margin-bottom | 0-16 |
| `gap-{n}` | gap (flex) | 0-16 |

```razor
@* Common spacing patterns *@
<div class="pa-4 mb-3">Content with padding and bottom margin</div>
<div class="d-flex gap-2">Items with consistent gap</div>
<div class="mt-auto">Push to bottom</div>
```

### Flex Utilities

```razor
<div class="d-flex align-center justify-space-between">
    <MudText>Left content</MudText>
    <MudSpacer />
    <MudButton>Right action</MudButton>
</div>

<div class="d-flex flex-column gap-3">
    <div>Stacked item 1</div>
    <div>Stacked item 2</div>
</div>
```

## Custom Component Patterns

### Action Node (Blueprint Canvas)

```razor
<div class="action-node @(IsSelected ? "selected" : "")">
    <div class="action-node-header">
        <MudIcon Icon="@ActionIcon" Size="Size.Small" Class="mr-2" />
        <span>@Title</span>
    </div>
    <div class="action-node-body">
        @foreach (var field in Fields)
        {
            <div class="node-field">
                <span class="field-label">@field.Label</span>
                <span class="field-value">@field.Value</span>
            </div>
        }
    </div>
</div>
```

```css
/* ActionNodeWidget.razor.css */
.action-node {
    background: white;
    border: 2px solid #1976d2;
    border-radius: 10px;
    min-width: 280px;
    max-width: 320px;
    box-shadow: 0 2px 8px rgba(0,0,0,0.12);
    transition: all 0.2s ease;
}

.action-node.selected {
    border-color: #0d47a1;
    border-width: 3px;
    box-shadow: 0 6px 16px rgba(13, 71, 161, 0.3);
}

.action-node-header {
    background: linear-gradient(135deg, #1976d2 0%, #1565c0 100%);
    color: white;
    padding: 10px 12px;
    border-radius: 8px 8px 0 0;
    display: flex;
    align-items: center;
    font-weight: 600;
}
```

### Properties Panel

```razor
<div class="properties-panel">
    <div class="properties-panel-header">
        <MudText Typo="Typo.subtitle1">Properties</MudText>
    </div>
    <div class="properties-panel-content">
        @ChildContent
    </div>
</div>
```

```css
.properties-panel {
    width: 350px;
    height: 100%;
    background: white;
    border-left: 1px solid #e0e0e0;
    display: flex;
    flex-direction: column;
}

.properties-panel-content {
    flex: 1;
    overflow-y: auto;
    padding: 16px;
}
```

## Icon System

Use Material Icons from MudBlazor:

```razor
@* Filled icons (default, bold) *@
<MudIcon Icon="@Icons.Material.Filled.Settings" />
<MudIcon Icon="@Icons.Material.Filled.PlayArrow" />
<MudIcon Icon="@Icons.Material.Filled.Visibility" />

@* Outlined icons (lighter weight) *@
<MudIcon Icon="@Icons.Material.Outlined.Info" />

@* With color and size *@
<MudIcon Icon="@Icons.Material.Filled.Check" Color="Color.Success" Size="Size.Large" />

@* Brand icons *@
<MudIcon Icon="@Icons.Custom.Brands.GitHub" />
```

## Form Components

```razor
<MudTextField @bind-Value="@Name" Label="Name" Variant="Variant.Outlined" />

<MudSelect T="string" @bind-Value="@Selected" Label="Type" Variant="Variant.Outlined">
    <MudSelectItem Value="@("option1")">Option 1</MudSelectItem>
    <MudSelectItem Value="@("option2")">Option 2</MudSelectItem>
</MudSelect>

<MudSwitch @bind-Value="@IsEnabled" Color="Color.Primary" Label="Enable feature" />
```

### WARNING: Mixing Styling Approaches

**The Problem:**

```razor
<!-- BAD - Inline styles override MudBlazor theming -->
<MudButton style="background-color: red;">Delete</MudButton>
```

**Why This Breaks:**
- Bypasses theme system
- Won't adapt to dark mode
- Inconsistent with other components

**The Fix:**

```razor
<!-- GOOD - Use Color enum -->
<MudButton Color="Color.Error">Delete</MudButton>