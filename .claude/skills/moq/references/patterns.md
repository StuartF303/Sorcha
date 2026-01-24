# Moq Patterns Reference

## Contents
- Setup Patterns
- Async Method Mocking
- Callback and Capture
- Verification Patterns
- Argument Matching
- Anti-Patterns

---

## Setup Patterns

### Constructor-Based Mock Setup

Sorcha uses constructor-level mock initialization with `private readonly` fields:

```csharp
// From tests/Sorcha.Wallet.Service.Tests/Services/WalletManagerTests.cs
public class WalletManagerTests : IDisposable
{
    private readonly Mock<ICryptoModule> _mockCryptoModule;
    private readonly Mock<IHashProvider> _mockHashProvider;
    private readonly Mock<IWalletUtilities> _mockWalletUtilities;

    public WalletManagerTests()
    {
        _mockCryptoModule = new Mock<ICryptoModule>();
        _mockHashProvider = new Mock<IHashProvider>();
        _mockWalletUtilities = new Mock<IWalletUtilities>();
        
        SetupDefaultCryptoModule();
    }
}
```

### Mock.Of<T>() for Simple Dependencies

Use `Mock.Of<T>()` when you need a dependency but won't verify or configure it:

```csharp
// GOOD - Concise for loggers and simple deps
_encryptionProvider = new LocalEncryptionProvider(Mock.Of<ILogger<LocalEncryptionProvider>>());

// BAD - Verbose when you don't need the mock reference
var loggerMock = new Mock<ILogger<LocalEncryptionProvider>>();
_encryptionProvider = new LocalEncryptionProvider(loggerMock.Object);
```

### Options Pattern with Mock

```csharp
// From tests/Sorcha.Peer.Service.Tests/PeerServiceTests.cs
_configMock = new Mock<IOptions<PeerServiceConfiguration>>();
_configuration = new PeerServiceConfiguration { Enabled = true, ListenPort = 5001 };
_configMock.Setup(x => x.Value).Returns(_configuration);
```

---

## Async Method Mocking

### ReturnsAsync with Lambda

For deterministic behavior based on input:

```csharp
// From tests/Sorcha.Wallet.Service.Tests/Services/WalletManagerTests.cs:72-100
_mockCryptoModule
    .Setup(x => x.GenerateKeySetAsync(
        It.IsAny<WalletNetworks>(),
        It.IsAny<byte[]>(),
        It.IsAny<CancellationToken>()))
    .ReturnsAsync((WalletNetworks network, byte[] seed, CancellationToken ct) =>
    {
        // Generate deterministic keys from seed for consistent test behavior
        var privateKey = new byte[32];
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(seed);
        Array.Copy(hash, privateKey, 32);

        return CryptoResult<KeySet>.Success(new KeySet { PrivateKey = new CryptoKey(network, privateKey) });
    });
```

### Simple ReturnsAsync

```csharp
_mockValidatorClient
    .Setup(v => v.SubmitGenesisTransactionAsync(It.IsAny<GenesisTransactionSubmission>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(true);
```

---

## Callback and Capture

### Capturing Arguments for Verification

```csharp
// From tests/Sorcha.Register.Service.Tests/Unit/RegisterCreationOrchestratorTests.cs:153-170
byte[]? capturedBytes = null;
_mockHashProvider
    .Setup(h => h.ComputeHash(It.IsAny<byte[]>(), HashType.SHA256))
    .Callback<byte[], HashType>((data, _) => capturedBytes = data)
    .Returns(new byte[32]);

// Act
await _orchestrator.InitiateAsync(request);

// Assert
capturedBytes.Should().NotBeNull();
var json = Encoding.UTF8.GetString(capturedBytes!);
json.Should().Contain("\"registerId\"");
```

### Multi-Parameter Callback

```csharp
byte? capturedNetwork = null;
_mockCryptoModule
    .Setup(c => c.VerifyAsync(
        It.IsAny<byte[]>(),
        It.IsAny<byte[]>(),
        It.IsAny<byte>(),
        It.IsAny<byte[]>(),
        It.IsAny<CancellationToken>()))
    .Callback<byte[], byte[], byte, byte[], CancellationToken>((_, _, network, _, _) =>
        capturedNetwork = network)
    .ReturnsAsync(CryptoStatus.Success);
```

---

## Verification Patterns

### Times Verification

```csharp
// Exactly once
_mockRegisterManager.Verify(m => m.CreateRegisterAsync(...), Times.Once);

// Never called (cache hit scenario)
_mockWormStore.Verify(w => w.GetAsync(It.IsAny<ulong>(), It.IsAny<CancellationToken>()), Times.Never);

// At least once
_loggerMock.Verify(x => x.Log(...), Times.AtLeastOnce);

// Exact count
_mockCacheStore.Verify(c => c.SetAsync(...), Times.Exactly(3));
```

### Complex Argument Verification

```csharp
// From tests/Sorcha.Register.Service.Tests/Unit/RegisterCreationOrchestratorTests.cs:256-263
_mockValidatorClient.Verify(
    v => v.SubmitGenesisTransactionAsync(
        It.Is<GenesisTransactionSubmission>(s =>
            s.RegisterId == initiateResponse.RegisterId &&
            s.RegisterName == "Test Register" &&
            s.TenantId == "tenant-123"),
        It.IsAny<CancellationToken>()),
    Times.Once);
```

### Logger Verification

```csharp
// From tests/Sorcha.Peer.Service.Tests/PeerServiceTests.cs:138-145
_loggerMock.Verify(
    x => x.Log(
        LogLevel.Information,
        It.IsAny<EventId>(),
        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("disabled")),
        It.IsAny<Exception>(),
        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
    Times.AtLeastOnce);
```

---

## Argument Matching

| Matcher | Use Case |
|---------|----------|
| `It.IsAny<T>()` | Accept any value |
| `It.Is<T>(predicate)` | Custom predicate |
| `It.IsIn(values)` | Value in collection |
| `It.IsRegex(pattern)` | String matches regex |
| `It.Ref<T>.IsAny` | Match ref parameters |

---

## Anti-Patterns

### WARNING: Verify Without Setup

**The Problem:**

```csharp
// BAD - Method not setup, verification passes even if implementation is broken
var mock = new Mock<IService>();
await sut.DoWork();
mock.Verify(x => x.Process(It.IsAny<Data>()), Times.Once);
```

**Why This Breaks:** If the method signature changes or implementation stops calling it, verification still passes because the mock allows any call by default.

**The Fix:**

```csharp
// GOOD - Setup enforces the expected behavior
var mock = new Mock<IService>(MockBehavior.Strict);
mock.Setup(x => x.Process(It.IsAny<Data>())).Returns(Task.CompletedTask);
await sut.DoWork();
mock.Verify(x => x.Process(It.IsAny<Data>()), Times.Once);
```

### WARNING: Over-Mocking

**The Problem:**

```csharp
// BAD - Mocking what you're testing
var mock = new Mock<WalletManager>();
mock.Setup(x => x.CreateWalletAsync(...)).ReturnsAsync(wallet);
var result = await mock.Object.CreateWalletAsync(...);
result.Should().Be(wallet); // This tests nothing!
```

**Why This Breaks:** You're testing Moq, not your code. The real implementation is never exercised.

**The Fix:**

```csharp
// GOOD - Mock dependencies, test real implementation
var mockCrypto = new Mock<ICryptoModule>();
mockCrypto.Setup(...).ReturnsAsync(...);
var walletManager = new WalletManager(mockCrypto.Object, ...);
var result = await walletManager.CreateWalletAsync(...);
```

### WARNING: Ignoring CancellationToken

**The Problem:**

```csharp
// BAD - Setup ignores cancellation token
mock.Setup(x => x.ProcessAsync(It.IsAny<Data>()))
    .ReturnsAsync(result);
```

**Why This Breaks:** Real async methods should respect cancellation. Tests won't catch cancellation handling bugs.

**The Fix:**

```csharp
// GOOD - Include CancellationToken in setup
mock.Setup(x => x.ProcessAsync(It.IsAny<Data>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(result);