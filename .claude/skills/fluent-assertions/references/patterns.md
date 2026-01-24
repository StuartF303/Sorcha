# FluentAssertions Patterns Reference

## Contents
- Basic Assertions
- Collection Assertions
- Exception Assertions
- Object Comparison
- Dictionary Assertions
- Anti-Patterns

## Basic Assertions

### String Assertions

```csharp
// Equality and content
blueprint.Title.Should().Be("Purchase Order Workflow");
error.Message.Should().Contain("validation");
wallet.Address.Should().StartWith("ws1");

// Null/empty checks
wallet.Address.Should().NotBeNullOrEmpty();
result.Description.Should().BeNullOrWhiteSpace();
```

### Numeric Assertions

```csharp
blueprint.Version.Should().Be(5);
docket.Id.Should().Be(42ul);
result.Count.Should().BeGreaterThan(0);
Convert.ToDouble(result.ProcessedData["totalPrice"]).Should().Be(50.0);
```

### Boolean Assertions

```csharp
result.IsSuccess.Should().BeTrue();
result.Validation.IsValid.Should().BeFalse();
keySetResult.IsSuccess.Should().BeTrue("key generation must succeed");
```

## Collection Assertions

### Count and Content

```csharp
// Count assertions
blueprint.Participants.Should().HaveCount(2);
blueprint.Actions.Should().NotBeEmpty();
docket.TransactionIds.Should().HaveCount(1000);

// Content assertions
schemas.Should().Contain(s => s.Metadata.Id == "person");
events.Should().ContainSingle(e => e is WalletCreatedEvent);
result.CalculatedValues.Should().ContainKey("totalPrice");
```

### Ordering and Sequences

```csharp
docket.TransactionIds.Should().ContainInOrder("tx1", "tx2", "tx3");
docket.TransactionIds.First().Should().Be("tx-00001");
docket.TransactionIds.Last().Should().Be("tx-01000");
```

### AllSatisfy for Bulk Validation

```csharp
wallets.Should().AllSatisfy(w => w.Owner.Should().Be(owner));
validationResults.Should().BeEmpty();  // No validation errors
```

## Exception Assertions

### Sync Exception Testing

```csharp
// Using Invoking pattern (preferred for sync)
builder.Invoking(b => b.Build())
    .Should().Throw<InvalidOperationException>()
    .WithMessage("*title*");  // Wildcard matching

// Direct Assert.Throws (xUnit style)
Assert.Throws<ArgumentNullException>(
    () => new ActionProcessor(null!, evaluator, disclosure, routing)
);
```

### Async Exception Testing

```csharp
// Using xUnit's Assert.ThrowsAsync
await Assert.ThrowsAsync<InvalidOperationException>(async () =>
    await _walletManager.RecoverWalletAsync(
        mnemonic, "Test2", "ED25519", "user1", "tenant1"));

// Using FluentAssertions Awaiting
var act = () => asyncObject.ThrowAsync<ArgumentException>();
await act.Should().ThrowAsync<InvalidOperationException>();
```

### WARNING: Mixing Async/Sync Patterns

**The Problem:**

```csharp
// BAD - Blocks thread, can deadlock
builder.Invoking(b => b.BuildAsync().Result)
    .Should().Throw<InvalidOperationException>();
```

**Why This Breaks:**
1. `.Result` blocks the thread synchronously
2. Can cause deadlocks in certain synchronization contexts
3. Exception gets wrapped in `AggregateException`

**The Fix:**

```csharp
// GOOD - Use async-aware assertions
await Assert.ThrowsAsync<InvalidOperationException>(
    () => builder.BuildAsync());
```

## Object Comparison

### Property-by-Property

```csharp
var buyer = blueprint.Participants.FirstOrDefault(p => p.Id == "buyer");
buyer.Should().NotBeNull();
buyer!.Name.Should().Be("Buyer Organization");
buyer.Organisation.Should().Be("ORG-123");
```

### Dictionary Assertions

```csharp
blueprint.Metadata.Should().NotBeNull();
blueprint.Metadata.Should().HaveCount(2);
blueprint.Metadata.Should().ContainKey("author").WhoseValue.Should().Be("John Doe");
blueprint.Metadata.Should().Contain("environment", "production");
```

### Nested Object Access

```csharp
var firstAction = blueprint.Actions.FirstOrDefault(a => a.Id == 0);
firstAction.Should().NotBeNull();
firstAction!.Title.Should().Be("Submit Application");
firstAction.Description.Should().Be("Submit the application for review");
```

## Anti-Patterns

### WARNING: Using Be() for Collections

**The Problem:**

```csharp
// BAD - Checks reference equality
list1.Should().Be(list2);
```

**Why This Breaks:**
1. `Be()` checks reference equality, not content
2. Two lists with same items will fail
3. Confusing test failures

**The Fix:**

```csharp
// GOOD - Check content equivalence
list1.Should().BeEquivalentTo(list2);

// Or for exact order
list1.Should().Equal(list2);
```

### WARNING: Forgetting Null Checks

**The Problem:**

```csharp
// BAD - Crashes if null
wallet.Address.Should().Be("expected");
```

**Why This Breaks:**
1. NullReferenceException before assertion runs
2. Unhelpful error message
3. Masks the real problem

**The Fix:**

```csharp
// GOOD - Explicit null check first
wallet.Should().NotBeNull();
wallet!.Address.Should().Be("expected");
```

### WARNING: Overly Complex Assertions

**The Problem:**

```csharp
// BAD - Hard to debug which part failed
result.Should().NotBeNull()
    .And.Subject.As<WalletResult>().Address.Should().NotBeNullOrEmpty()
    .And.Subject.Owner.Should().Be("user1");
```

**The Fix:**

```csharp
// GOOD - Clear, separate assertions
result.Should().NotBeNull();
result.Address.Should().NotBeNullOrEmpty();
result.Owner.Should().Be("user1");