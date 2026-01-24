# FluentAssertions Workflows Reference

## Contents
- Test Structure Pattern
- Async Testing Workflow
- Validation Testing
- Event Verification
- Integration Test Patterns

## Test Structure Pattern

Sorcha tests follow Arrange-Act-Assert with FluentAssertions in the Assert phase:

```csharp
[Fact]
public async Task CreateWalletAsync_ShouldCreateWallet_WithValidParameters()
{
    // Arrange
    var name = "Test Wallet";
    var algorithm = "ED25519";
    var owner = "user123";
    var tenant = "tenant1";

    // Act
    var (wallet, mnemonic) = await _walletManager.CreateWalletAsync(
        name, algorithm, owner, tenant);

    // Assert
    wallet.Should().NotBeNull();
    wallet.Name.Should().Be(name);
    wallet.Algorithm.Should().Be(algorithm);
    wallet.Owner.Should().Be(owner);
    wallet.Status.Should().Be(WalletStatus.Active);
    mnemonic.WordCount.Should().Be(12);
}
```

## Async Testing Workflow

### Pattern for Async Operations

```csharp
[Fact]
public async Task SignAndVerify_ED25519_ShouldSucceed()
{
    // Arrange
    var keySetResult = await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ED25519);
    var keySet = keySetResult.Value!;
    byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes("test data"));

    // Act - Sign
    var signResult = await _cryptoModule.SignAsync(hash, (byte)WalletNetworks.ED25519, keySet.PrivateKey.Key!);

    // Assert - Sign
    signResult.IsSuccess.Should().BeTrue();
    signResult.Value.Should().NotBeNullOrEmpty();

    // Act - Verify
    var verifyResult = await _cryptoModule.VerifyAsync(signResult.Value!, hash, (byte)WalletNetworks.ED25519, keySet.PublicKey.Key!);

    // Assert - Verify
    verifyResult.Should().Be(CryptoStatus.Success);
}
```

### Encrypt/Decrypt Round-Trip

```csharp
[Fact]
public async Task EncryptAndDecrypt_ED25519_ShouldRoundTrip()
{
    // Arrange
    var keySetResult = await _cryptoModule.GenerateKeySetAsync(WalletNetworks.ED25519);
    var keySet = keySetResult.Value!;
    byte[] plaintext = Encoding.UTF8.GetBytes("Hello, Sorcha!");

    // Act - Encrypt
    var encryptResult = await _cryptoModule.EncryptAsync(plaintext, (byte)WalletNetworks.ED25519, keySet.PublicKey.Key!);

    // Assert - Encrypt
    encryptResult.IsSuccess.Should().BeTrue();
    encryptResult.Value.Should().NotBeNullOrEmpty();

    // Act - Decrypt
    var decryptResult = await _cryptoModule.DecryptAsync(encryptResult.Value!, (byte)WalletNetworks.ED25519, keySet.PrivateKey.Key!);

    // Assert - Decrypt
    decryptResult.IsSuccess.Should().BeTrue();
    decryptResult.Value.Should().Equal(plaintext);
}
```

## Validation Testing

### Model Validation Pattern

```csharp
[Theory]
[InlineData("")]
[InlineData(null)]
public void Docket_WithInvalidRegisterId_ShouldFailValidation(string? invalidRegisterId)
{
    // Arrange
    var docket = new Docket
    {
        RegisterId = invalidRegisterId!,
        Hash = "some-hash"
    };

    // Act
    var validationResults = ValidateModel(docket);

    // Assert
    validationResults.Should().ContainSingle(v => v.MemberNames.Contains("RegisterId"));
}

private static IList<ValidationResult> ValidateModel(object model)
{
    var validationResults = new List<ValidationResult>();
    var validationContext = new ValidationContext(model, null, null);
    Validator.TryValidateObject(model, validationContext, validationResults, true);
    return validationResults;
}
```

### Builder Validation Testing

```csharp
[Fact]
public void Build_WithLessThanTwoParticipants_ShouldThrowInvalidOperationException()
{
    // Arrange
    var builder = BlueprintBuilder.Create()
        .WithTitle("Test Blueprint")
        .WithDescription("Test description")
        .AddParticipant("p1", p => p.Named("Participant 1"));

    // Act & Assert
    builder.Invoking(b => b.Build())
        .Should().Throw<InvalidOperationException>()
        .WithMessage("*at least 2 participants*");
}
```

## Event Verification

### Domain Event Testing

```csharp
[Fact]
public async Task CreateWalletAsync_ShouldPublishWalletCreatedEvent()
{
    // Arrange & Act
    var (wallet, _) = await _walletManager.CreateWalletAsync(
        "Test", "ED25519", "user1", "tenant1");

    // Assert - Event was published
    var events = _eventPublisher.GetPublishedEvents();
    events.Should().ContainSingle(e => e is WalletCreatedEvent);

    // Assert - Event properties
    var createdEvent = events.OfType<WalletCreatedEvent>().First();
    createdEvent.WalletAddress.Should().Be(wallet.Address);
    createdEvent.Owner.Should().Be("user1");
    createdEvent.Algorithm.Should().Be("ED25519");
}
```

### Multiple Event Assertions

```csharp
[Fact]
public async Task DeleteWalletAsync_ShouldPublishStatusChangedEvent()
{
    // Arrange
    var (wallet, _) = await _walletManager.CreateWalletAsync(
        "Test", "ED25519", "user1", "tenant1");

    // Act
    await _walletManager.DeleteWalletAsync(wallet.Address);

    // Assert
    var events = _eventPublisher.GetPublishedEvents();
    events.Should().Contain(e => e is WalletStatusChangedEvent);
}
```

## Integration Test Patterns

### Fluent API Builder Testing

```csharp
[Fact]
public void CreateBlueprintWithFluentAPI_ShouldSucceed()
{
    // Arrange & Act
    var blueprint = BlueprintBuilder.Create()
        .WithTitle("Purchase Order Workflow")
        .WithDescription("A complete purchase order workflow")
        .AddParticipant("buyer", p => p.Named("Buyer Organization"))
        .AddParticipant("seller", p => p.Named("Seller Organization"))
        .AddAction(0, a => a
            .WithTitle("Submit Purchase Order")
            .SentBy("buyer")
            .RouteToNext("seller"))
        .Build();

    // Assert
    blueprint.Should().NotBeNull();
    blueprint.Title.Should().Be("Purchase Order Workflow");
    blueprint.Participants.Should().HaveCount(2);
    blueprint.Actions.Should().HaveCount(1);
    blueprint.Actions[0].Sender.Should().Be("buyer");
}
```

### Workflow Processing Test

```csharp
[Fact]
public async Task ProcessAsync_WithCalculations_AppliesCorrectly()
{
    // Arrange
    var blueprint = CreateBlueprintWithCalculations();
    var action = blueprint.Actions[0];

    var context = new ExecutionContext
    {
        Blueprint = blueprint,
        Action = action,
        ActionData = new Dictionary<string, object>
        {
            ["quantity"] = 5,
            ["unitPrice"] = 10.0
        },
        ParticipantId = "user1",
        WalletAddress = "0x123"
    };

    // Act
    var result = await _processor.ProcessAsync(context);

    // Assert
    result.Success.Should().BeTrue();
    result.ProcessedData.Should().ContainKey("totalPrice");
    result.CalculatedValues.Should().ContainKey("totalPrice");
    Convert.ToDouble(result.ProcessedData["totalPrice"]).Should().Be(50.0);
}
```

## Test Writing Checklist

Copy this checklist when writing new tests:

- [ ] Arrange: Set up test data and dependencies
- [ ] Act: Execute the method under test
- [ ] Assert: Verify results with FluentAssertions
- [ ] Null check before property access (`Should().NotBeNull()`)
- [ ] Use `!` after null assertion for null-forgiving
- [ ] Async methods use `await` properly
- [ ] Exception tests use appropriate pattern (Invoking/ThrowsAsync)
- [ ] Collection assertions use `HaveCount`/`Contain`/`BeEquivalentTo`
- [ ] Error messages in `Should().BeTrue("reason")` for debugging