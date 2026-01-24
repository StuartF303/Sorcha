# Aesthetics Reference

## Contents
- Brand Colors
- Typography System
- Color Application
- Dark Mode
- Visual Identity

## Brand Colors

Sorcha uses Material Design primary blue with a purple accent for PWA branding.

| Token | Hex | Usage |
|-------|-----|-------|
| Primary | `#1976d2` | Buttons, links, primary actions |
| Primary Dark | `#1565c0` | Hover states, selected items |
| Primary Darker | `#0d47a1` | Active/focused states |
| Accent | `#594ae2` | PWA theme, special highlights |
| Sidebar Gradient | `rgb(5,39,103)` → `#3a0647` | Navigation background |

### Semantic Colors

```razor
@* Use MudBlazor Color enum for consistency *@
<MudButton Color="Color.Primary">Primary Action</MudButton>
<MudButton Color="Color.Success">Confirm</MudButton>
<MudButton Color="Color.Error">Delete</MudButton>
<MudButton Color="Color.Warning">Caution</MudButton>
<MudButton Color="Color.Info">Information</MudButton>
```

### Neutral Palette

| Token | Hex | Usage |
|-------|-----|-------|
| Background Light | `#fafafa` | Canvas, content areas |
| Surface | `#f5f5f5` | Cards, panels |
| Border | `#e0e0e0` | Dividers, outlines |
| Text Secondary | `#9e9e9e` | Muted text, captions |
| Dark Background | `#1e1e1e` | Code editors, dark mode |

## Typography System

**Font Family:** Roboto (300, 400, 500, 700 weights)

```html
<!-- Loaded in index.html -->
<link href="https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap" rel="stylesheet" />
```

### Typography Scale

```razor
<MudText Typo="Typo.h1">Display Heading</MudText>
<MudText Typo="Typo.h4">Page Title</MudText>
<MudText Typo="Typo.h6">Section Title</MudText>
<MudText Typo="Typo.subtitle1">Subsection</MudText>
<MudText Typo="Typo.body1">Primary body text</MudText>
<MudText Typo="Typo.body2">Secondary body text</MudText>
<MudText Typo="Typo.caption">Small labels</MudText>
```

### WARNING: Font Substitution

**The Problem:**

```css
/* BAD - Overriding Roboto with generic fonts */
body { font-family: Arial, sans-serif; }
```

**Why This Breaks:**
- Destroys Material Design visual consistency
- Roboto is optimized for the typography scale
- Weights 300/500 don't exist in Arial

**The Fix:**

```css
/* GOOD - Use project font stack */
body { font-family: 'Roboto', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; }
```

## Color Application

### Status Indicators

```razor
@* Use chips for status badges *@
<MudChip T="string" Size="Size.Small" Color="Color.Success">Active</MudChip>
<MudChip T="string" Size="Size.Small" Color="Color.Warning">Pending</MudChip>
<MudChip T="string" Size="Size.Small" Color="Color.Error">Failed</MudChip>
```

### Gradient Headers

```css
/* Action node header gradient */
.header-gradient {
    background: linear-gradient(135deg, #1976d2 0%, #1565c0 100%);
    color: white;
}

/* Sidebar gradient */
.sidebar-gradient {
    background: linear-gradient(180deg, rgb(5, 39, 103) 0%, #3a0647 70%);
}
```

## Dark Mode

Dark mode is configured but not fully implemented. Use these tokens for dark-compatible components:

```css
/* Dark mode ready component */
.code-view {
    background: #1e1e1e;
    color: #d4d4d4;
    font-family: 'Consolas', 'Courier New', monospace;
}

/* Check for dark mode preference */
@media (prefers-color-scheme: dark) {
    .adaptive-panel {
        background: #2d2d2d;
        border-color: #404040;
    }
}
```

## Visual Identity

### What Makes Sorcha Distinctive

1. **Blockchain-inspired nodes** - Rounded rectangles with port indicators
2. **Gradient sidebar** - Deep blue to purple transition
3. **Workflow canvas** - Light gray background with subtle elevation
4. **Status timeline** - Color-coded activity log with icons

### DO

- Use elevation shadows for depth hierarchy
- Apply primary blue for interactive elements
- Use gradients sparingly for emphasis

### DON'T

- Use flat design without shadows
- Mix multiple bright accent colors
- Apply gradients to body text or content areas