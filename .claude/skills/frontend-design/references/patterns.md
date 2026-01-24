# Patterns Reference

## Contents
- DO/DON'T Design Decisions
- Visual Anti-Patterns
- Component Composition
- Accessibility Patterns
- Project-Specific Conventions

## DO/DON'T Design Decisions

### Colors

| DO | DON'T |
|----|-------|
| Use `Color.Primary` for primary actions | Hardcode `#1976d2` in CSS |
| Use semantic colors (Success, Error) | Use red/green without meaning |
| Apply gradients to headers only | Apply gradients to body text |
| Use MudBlazor elevation for depth | Create custom shadow variables |

### Typography

| DO | DON'T |
|----|-------|
| Use `Typo.h6` for section headings | Use `<h1>` HTML tags directly |
| Use `Typo.body2` for secondary text | Reduce font-size below 12px |
| Apply Roboto font stack | Override with Arial or sans-serif |
| Use `Color.Secondary` for muted text | Use gray hex codes inline |

### Spacing

| DO | DON'T |
|----|-------|
| Use `Class="pa-4 mb-3"` for consistency | Use arbitrary pixel values |
| Use `gap-2` for flex children | Use margin on every child element |
| Keep padding consistent (16px base) | Mix rem and px units |

## Visual Anti-Patterns

### WARNING: Generic AI Aesthetics

**The Problem:**

```css
/* BAD - Cookie-cutter AI design */
.card {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    border-radius: 24px;
    box-shadow: 0 25px 50px -12px rgba(0,0,0,0.25);
}
```

**Why This Breaks:**
- Clashes with Material Design language
- Purple gradient conflicts with Sorcha brand (blue primary)
- Oversized border-radius looks out of place
- Excessive shadow depth

**The Fix:**

```css
/* GOOD - Matches project aesthetic */
.card {
    background: white;
    border: 1px solid #e0e0e0;
    border-radius: 10px;
    box-shadow: 0 2px 8px rgba(0,0,0,0.12);
}
```

### WARNING: Inconsistent Icon Sources

**The Problem:**

```razor
<!-- BAD - Mixing icon sources -->
<i class="fa fa-settings"></i>  <!-- FontAwesome -->
<MudIcon Icon="@Icons.Material.Filled.Home" />  <!-- MudBlazor -->
<img src="icons/custom.svg" />  <!-- Custom SVG -->
```

**Why This Breaks:**
- Visual inconsistency in icon weight/style
- Multiple HTTP requests
- Different sizing behaviors

**The Fix:**

```razor
<!-- GOOD - Consistent Material Icons -->
<MudIcon Icon="@Icons.Material.Filled.Settings" />
<MudIcon Icon="@Icons.Material.Filled.Home" />
<MudIcon Icon="@Icons.Material.Filled.Star" />
```

### WARNING: Inline Styles

**The Problem:**

```razor
<!-- BAD - Inline styles everywhere -->
<div style="display: flex; padding: 16px; margin-bottom: 12px; background: #f5f5f5;">
```

**Why This Breaks:**
- Can't be reused
- Doesn't respond to theming
- Hard to maintain
- Higher CSS specificity

**The Fix:**

```razor
<!-- GOOD - Use utility classes or CSS isolation -->
<div class="d-flex pa-4 mb-3" style="background: var(--mud-palette-surface);">

<!-- Or in component CSS file -->
<div class="content-panel">
```

## Component Composition

### Panel with Actions Pattern

```razor
<MudPaper Elevation="1" Class="d-flex flex-column">
    <div class="d-flex align-center justify-space-between pa-3" style="border-bottom: 1px solid #e0e0e0;">
        <MudText Typo="Typo.subtitle1">Panel Title</MudText>
        <MudIconButton Icon="@Icons.Material.Filled.Close" Size="Size.Small" />
    </div>
    <div class="pa-4">
        @* Panel content *@
    </div>
    <div class="d-flex justify-end gap-2 pa-3" style="border-top: 1px solid #e0e0e0;">
        <MudButton Variant="Variant.Text">Cancel</MudButton>
        <MudButton Color="Color.Primary">Save</MudButton>
    </div>
</MudPaper>
```

### Status Badge Pattern

```razor
@code {
    private (Color color, string icon) GetStatusStyle(string status) => status switch
    {
        "active" => (Color.Success, Icons.Material.Filled.CheckCircle),
        "pending" => (Color.Warning, Icons.Material.Filled.Schedule),
        "failed" => (Color.Error, Icons.Material.Filled.Error),
        _ => (Color.Default, Icons.Material.Filled.Help)
    };
}

<MudChip T="string" Size="Size.Small" Color="@GetStatusStyle(status).color"
         Icon="@GetStatusStyle(status).icon">
    @status
</MudChip>
```

## Accessibility Patterns

### Focus Indicators

```css
/* Don't remove focus outlines */
.interactive:focus {
    outline: 2px solid #1976d2;
    outline-offset: 2px;
}

/* Or use MudBlazor's built-in focus states */
```

### Color Contrast

- Primary text on white: `#212121` (AAA compliant)
- Secondary text: `#757575` (AA compliant)
- Primary blue on white: `#1976d2` (AA compliant for large text)

### Screen Reader Support

```razor
<MudIconButton Icon="@Icons.Material.Filled.Delete" 
               aria-label="Delete item"
               Title="Delete this item" />

<MudTooltip Text="More information">
    <MudIcon Icon="@Icons.Material.Filled.Info" aria-hidden="true" />
</MudTooltip>
```

## Project-Specific Conventions

### Sorcha Design Tokens

| Token | Value | Usage |
|-------|-------|-------|
| `--sidebar-width` | 250px | Navigation width |
| `--navbar-height` | 3.5rem | Top bar height |
| `--panel-width` | 350px | Properties panel |
| `--canvas-bg` | #fafafa | Diagram background |
| `--node-radius` | 10px | Card/node corners |

### Component Naming

- `*Widget.razor` - Canvas-rendered elements (ActionNodeWidget)
- `*Panel.razor` - Sidebar/overlay panels (PropertiesPanel)
- `*View.razor` - Full-page or major sections (BlueprintJsonView)
- `*Card.razor` - Standalone content blocks

### Z-Index Scale

| Layer | z-index | Usage |
|-------|---------|-------|
| Base | 0 | Content |
| Sticky | 1 | Headers, panels |
| Overlay | 10 | Dialogs (MudBlazor handles) |
| Tooltip | 1000+ | Tooltips (MudBlazor handles) |