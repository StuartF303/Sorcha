# Layouts Reference

## Contents
- Page Structure
- Responsive Breakpoints
- Grid System
- Sticky Elements
- Panel Layouts

## Page Structure

Sorcha uses a sidebar + main content layout with a sticky top navigation bar.

```
┌─────────────────────────────────────────────────┐
│              Top Navigation Bar                 │  height: 3.5rem
├───────────────┬─────────────────────────────────┤
│   Sidebar     │                                 │
│   (250px)     │      Main Content Area          │
│   position:   │      (flex: 1)                  │
│   sticky      │                                 │
│               │                                 │
└───────────────┴─────────────────────────────────┘
```

### MainLayout Pattern

```razor
@* MainLayout.razor *@
@inherits LayoutComponentBase

<MudThemeProvider />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />

<div class="page">
    <div class="sidebar">
        <NavMenu />
    </div>
    <main>
        <div class="top-row px-4">
            @* Top navigation content *@
        </div>
        <article class="content px-4">
            @Body
        </article>
    </main>
</div>
```

```css
/* MainLayout.razor.css */
.page {
    display: flex;
    flex-direction: row;
    min-height: 100vh;
}

.sidebar {
    width: 250px;
    background: linear-gradient(180deg, rgb(5, 39, 103) 0%, #3a0647 70%);
    position: sticky;
    top: 0;
    height: 100vh;
}

main {
    flex: 1;
    display: flex;
    flex-direction: column;
}

.top-row {
    height: 3.5rem;
    display: flex;
    align-items: center;
    position: sticky;
    top: 0;
    z-index: 1;
    background: white;
    border-bottom: 1px solid #e0e0e0;
}

.content {
    flex: 1;
    padding: 1rem;
}
```

## Responsive Breakpoints

Single breakpoint at 641px for mobile/desktop transition.

```css
/* Mobile-first: single column */
@media (max-width: 640.98px) {
    .page {
        flex-direction: column;
    }
    
    .sidebar {
        display: none; /* Hidden on mobile */
    }
    
    .top-row {
        justify-content: space-between;
    }
}

/* Desktop: sidebar visible */
@media (min-width: 641px) {
    .sidebar {
        display: block;
    }
}
```

### MudBlazor Breakpoints

```razor
@* Use Breakpoint enum for responsive components *@
<MudHidden Breakpoint="Breakpoint.SmAndDown">
    Desktop-only content
</MudHidden>

<MudHidden Breakpoint="Breakpoint.MdAndUp" Invert="true">
    Mobile-only content
</MudHidden>
```

| Breakpoint | Range |
|------------|-------|
| Xs | 0 - 599px |
| Sm | 600 - 959px |
| Md | 960 - 1279px |
| Lg | 1280 - 1919px |
| Xl | 1920px+ |

## Grid System

```razor
@* 12-column responsive grid *@
<MudGrid>
    <MudItem xs="12" md="6" lg="4">
        <MudPaper Class="pa-4">Column 1</MudPaper>
    </MudItem>
    <MudItem xs="12" md="6" lg="4">
        <MudPaper Class="pa-4">Column 2</MudPaper>
    </MudItem>
    <MudItem xs="12" md="12" lg="4">
        <MudPaper Class="pa-4">Column 3</MudPaper>
    </MudItem>
</MudGrid>
```

## Sticky Elements

```css
/* Sticky header within scrollable container */
.section-header {
    position: sticky;
    top: 0;
    background: #f5f5f5;
    z-index: 1;
    padding: 12px 16px;
    border-bottom: 1px solid #e0e0e0;
}

/* Sticky sidebar panel */
.properties-panel {
    position: sticky;
    top: 3.5rem; /* Below navbar */
    height: calc(100vh - 3.5rem);
    overflow-y: auto;
}
```

## Panel Layouts

### Designer Three-Column Layout

```razor
<div class="designer-layout">
    <div class="toolbox-panel">@* Left toolbox *@</div>
    <div class="canvas-area">@* Center canvas *@</div>
    <div class="properties-panel">@* Right properties *@</div>
</div>
```

```css
.designer-layout {
    display: flex;
    height: calc(100vh - 3.5rem);
}

.toolbox-panel {
    width: 60px;
    background: #f5f5f5;
    border-right: 1px solid #e0e0e0;
}

.canvas-area {
    flex: 1;
    background: #fafafa;
    overflow: auto;
}

.properties-panel {
    width: 350px;
    background: white;
    border-left: 1px solid #e0e0e0;
}
```

### WARNING: Fixed Heights in Flexbox

**The Problem:**

```css
/* BAD - Fixed height breaks flex layout */
.content { height: 500px; }
```

**Why This Breaks:**
- Content overflow issues
- Doesn't adapt to viewport
- Breaks scroll behavior

**The Fix:**

```css
/* GOOD - Use flex and calc */
.content {
    flex: 1;
    height: calc(100vh - 3.5rem);
    overflow-y: auto;
}