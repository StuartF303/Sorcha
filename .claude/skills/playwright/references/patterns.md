# Playwright Patterns Reference

## Contents
- Locator Strategies
- Assertions
- Waiting Patterns
- MudBlazor Component Testing
- Anti-Patterns

---

## Locator Strategies

### Role-Based Locators (Preferred)

```csharp
// GOOD - Semantic, resilient to styling changes
await Page.GetByRole(AriaRole.Button, new() { Name = "Submit" }).ClickAsync();
await Page.GetByRole(AriaRole.Link, new() { Name = "Designer" }).ClickAsync();
await Page.GetByLabel("User Name").FillAsync("John");
```

### Test ID Locators (Most Stable)

```csharp
// GOOD - Explicit test contracts, won't break on text changes
Page.Locator("[data-testid='blueprint-canvas']")
Page.Locator("[data-testid='new-blueprint-btn']")
```

Add test IDs to Blazor components:
```razor
<MudButton data-testid="submit-btn" OnClick="Submit">Submit</MudButton>
```

### Text-Based Locators

```csharp
// Project pattern - used for navigation
var designerLink = Page.Locator("a:has-text('Designer')");
await designerLink.First.ClickAsync();

// Button text
var newButton = Page.Locator("button:has-text('New')");
```

### MudBlazor Component Selectors

```csharp
// CSS class selectors for MudBlazor
Page.Locator(".mud-button")
Page.Locator(".mud-table")
Page.Locator(".mud-dialog")

// Verify MudBlazor loaded
var mudStyles = await Page.Locator("link[href*='MudBlazor']").CountAsync();
Assert.That(mudStyles, Is.GreaterThan(0));
```

---

## Assertions

### Page-Level Assertions

```csharp
// URL patterns
await Expect(Page).ToHaveURLAsync(new Regex("/designer"));
await Expect(Page).ToHaveTitleAsync(new Regex("Sorcha|Blueprint"));

// Content assertions
var pageContent = await Page.TextContentAsync("body");
Assert.That(pageContent, Does.Contain("Library").Or.Contain("Blueprint"));
```

### Element Assertions

```csharp
// Visibility
await Expect(Page.GetByText("Welcome")).ToBeVisibleAsync();

// Dialog/canvas presence
var hasDialog = await Page.Locator("[role='dialog']").CountAsync() > 0;
var hasCanvas = await Page.Locator("canvas, svg").CountAsync() > 0;
Assert.That(hasDialog || hasCanvas, Is.True);
```

---

## Waiting Patterns

### Built-in Auto-Wait

```csharp
// GOOD - Playwright auto-waits for actionability
await Page.GotoAsync(_blazorUrl!);
await Page.WaitForLoadStateAsync();
await designerLink.ClickAsync(); // Waits for element to be clickable
```

### Explicit Waits

```csharp
// Wait for specific element
var orderSent = Page.Locator("#order-sent");
await orderSent.WaitForAsync();

// Wait for state
await orderSent.WaitForAsync(new() { State = WaitForSelectorState.Visible });
```

### WARNING: Fixed Timeouts

**The Problem:**

```csharp
// BAD - Arbitrary delay, flaky in CI
await Page.WaitForTimeoutAsync(2000);
```

**Why This Breaks:**
1. Tests become slow (waiting full duration even when ready)
2. Flaky in CI where timing varies
3. No guarantee element is actually ready

**The Fix:**

```csharp
// GOOD - Wait for specific condition
await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
await Page.Locator(".service-status").WaitForAsync();
```

---

## MudBlazor Component Testing

### Dialog Detection

```csharp
[Test]
public async Task Designer_CanOpenNewBlueprint()
{
    await Page.GotoAsync($"{_blazorUrl}/designer");
    await Page.WaitForLoadStateAsync();

    var newButton = Page.Locator("button:has-text('New')");
    if (await newButton.CountAsync() > 0)
    {
        await newButton.First.ClickAsync();
        
        // MudBlazor dialog has role="dialog"
        var hasDialog = await Page.Locator("[role='dialog']").CountAsync() > 0;
        Assert.That(hasDialog, Is.True);
    }
}
```

### Table Verification

```csharp
var tableRows = await Page.Locator(".mud-table-row").CountAsync();
Assert.That(tableRows, Is.GreaterThan(0));
```

---

## Anti-Patterns

### WARNING: Long CSS/XPath Chains

**The Problem:**

```csharp
// BAD - Breaks on any DOM restructuring
await Page.Locator("#tsf > div:nth-child(2) > div.A8SBwf > div > input").ClickAsync();
```

**Why This Breaks:**
1. Extremely fragile to any layout change
2. Impossible to understand intent
3. Fails silently when structure changes

**The Fix:**

```csharp
// GOOD - Use semantic selectors
await Page.GetByRole(AriaRole.Searchbox).ClickAsync();
await Page.Locator("[data-testid='search-input']").ClickAsync();
```

### WARNING: Missing Element Count Check

**The Problem:**

```csharp
// BAD - Throws if element doesn't exist
await Page.Locator("button:has-text('New')").ClickAsync();
```

**Why This Breaks:**
1. Hard crash if element conditionally rendered
2. No graceful fallback

**The Fix:**

```csharp
// GOOD - Check before interacting
var newButton = Page.Locator("button:has-text('New')");
if (await newButton.CountAsync() > 0)
{
    await newButton.First.ClickAsync();
}
```

### WARNING: Ignoring Blazor WASM Load Time

**The Problem:**

```csharp
// BAD - Assumes immediate readiness
await Page.GotoAsync(_blazorUrl!);
await Page.ClickAsync("button"); // May fail during WASM init
```

**Why This Breaks:**
1. Blazor WASM has significant initialization time
2. Elements exist in DOM but aren't interactive yet

**The Fix:**

```csharp
// GOOD - Wait for load state
await Page.GotoAsync(_blazorUrl!);
await Page.WaitForLoadStateAsync();
await Page.WaitForLoadStateAsync(LoadState.NetworkIdle); // For API calls
```