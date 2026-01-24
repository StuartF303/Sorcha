# Motion Reference

## Contents
- Transition Standards
- Hover Effects
- State Changes
- Loading States
- Performance

## Transition Standards

All interactive elements use a consistent 0.2s ease timing:

```css
/* Standard transition */
.interactive-element {
    transition: all 0.2s ease;
}

/* Specific properties for performance */
.optimized-element {
    transition: transform 0.2s ease, box-shadow 0.2s ease;
}
```

### Timing Functions

| Function | Use Case |
|----------|----------|
| `ease` | General interactions |
| `ease-in-out` | Progress indicators |
| `ease-out` | Exit animations |

## Hover Effects

### Lift Effect (Cards/Nodes)

```css
.hoverable-card {
    transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.hoverable-card:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 12px rgba(0,0,0,0.18);
}
```

### Border Color Transition

```css
.action-node {
    border: 2px solid #1976d2;
    transition: border-color 0.2s ease;
}

.action-node:hover {
    border-color: #1565c0;
}

.action-node.selected {
    border-color: #0d47a1;
    border-width: 3px;
}
```

### Icon Rotation

```css
.expandable-header .expand-icon {
    transition: transform 0.2s ease;
}

.expandable-header.expanded .expand-icon {
    transform: rotate(180deg);
}
```

## State Changes

### Selection State

```css
.selectable-item {
    background: white;
    border: 2px solid transparent;
    transition: all 0.2s ease;
}

.selectable-item:hover {
    background: #f5f5f5;
}

.selectable-item.selected {
    border-color: #1976d2;
    background: rgba(25, 118, 210, 0.08);
    box-shadow: 0 6px 16px rgba(13, 71, 161, 0.3);
}
```

### Disabled State

```css
.action-button:disabled {
    opacity: 0.5;
    cursor: not-allowed;
    transition: opacity 0.2s ease;
}
```

## Loading States

### Progress Circle (CSS Animation)

```css
.progress-circle {
    stroke-dasharray: 0, 100;
    transition: stroke-dasharray 0.05s ease-in-out;
}

/* Animate via Blazor binding */
.progress-circle[data-progress="50"] {
    stroke-dasharray: 50, 100;
}
```

### Skeleton Loading

```razor
<MudSkeleton Animation="Animation.Wave" Width="100%" Height="40px" />
<MudSkeleton Animation="Animation.Pulse" SkeletonType="SkeletonType.Circle" Width="48px" Height="48px" />
```

### Button Loading State

```razor
<MudButton Color="Color.Primary" Disabled="@_isLoading">
    @if (_isLoading)
    {
        <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
        <span>Processing...</span>
    }
    else
    {
        <span>Submit</span>
    }
</MudButton>
```

## Performance

### WARNING: Animating Layout Properties

**The Problem:**

```css
/* BAD - Triggers layout recalculation */
.animated-panel {
    transition: width 0.3s, height 0.3s, margin 0.3s;
}
```

**Why This Breaks:**
- Forces browser reflow on every frame
- Janky animation on low-end devices
- High CPU usage

**The Fix:**

```css
/* GOOD - Use transform and opacity only */
.animated-panel {
    transition: transform 0.3s ease, opacity 0.3s ease;
}

.animated-panel.collapsed {
    transform: scaleY(0);
    opacity: 0;
}
```

### will-change Usage

```css
/* Use sparingly for complex animations */
.diagram-node {
    will-change: transform;
}

/* Remove after animation completes */
.diagram-node.idle {
    will-change: auto;
}
```

### Reduced Motion

```css
@media (prefers-reduced-motion: reduce) {
    * {
        transition-duration: 0.01ms !important;
        animation-duration: 0.01ms !important;
    }
}
```

## Animation Checklist

Copy this checklist when adding animations:

- [ ] Uses transform/opacity only (not width/height/margin)
- [ ] Has 0.2s or shorter duration for micro-interactions
- [ ] Respects prefers-reduced-motion
- [ ] Doesn't animate on page load unless necessary
- [ ] Has clear purpose (feedback, not decoration)