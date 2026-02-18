# Contract: ISystemWalletSigningService

**Location**: `src/Common/Sorcha.ServiceClients/SystemWallet/`

## Interface

```csharp
public interface ISystemWalletSigningService
{
    /// <summary>
    /// Signs transaction data with the system wallet, enforcing security controls.
    /// </summary>
    /// <param name="registerId">Target register for rate limiting and audit</param>
    /// <param name="txId">Transaction ID being signed</param>
    /// <param name="payloadHash">Hex-encoded SHA-256 hash of the payload</param>
    /// <param name="derivationPath">Signing derivation path (must be in whitelist)</param>
    /// <param name="transactionType">Transaction type for audit logging (e.g. "Genesis", "Control")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Signing result with signature bytes, public key, and algorithm</returns>
    /// <exception cref="InvalidOperationException">Derivation path not in whitelist</exception>
    /// <exception cref="InvalidOperationException">Rate limit exceeded for register</exception>
    /// <exception cref="InvalidOperationException">System wallet unavailable after retries</exception>
    Task<SystemSignResult> SignAsync(
        string registerId,
        string txId,
        string payloadHash,
        string derivationPath,
        string transactionType,
        CancellationToken cancellationToken = default);
}
```

## Result Model

```csharp
public record SystemSignResult
{
    public required byte[] Signature { get; init; }
    public required byte[] PublicKey { get; init; }
    public required string Algorithm { get; init; }
    public required string WalletAddress { get; init; }
}
```

## Configuration

```csharp
public class SystemWalletSigningOptions
{
    public required string ValidatorId { get; set; }
    public string[] AllowedDerivationPaths { get; set; } =
        ["sorcha:register-control", "sorcha:docket-signing"];
    public int MaxSignsPerRegisterPerMinute { get; set; } = 10;
}
```

## DI Registration

```csharp
// Extension method — opt-in only
public static IServiceCollection AddSystemWalletSigning(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.Configure<SystemWalletSigningOptions>(
        configuration.GetSection("SystemWalletSigning"));
    services.AddSingleton<ISystemWalletSigningService, SystemWalletSigningService>();
    return services;
}
```

## Security Controls

1. **Whitelist check**: `derivationPath` must be in `AllowedDerivationPaths`
2. **Rate limit**: Sliding window per `registerId`, max `MaxSignsPerRegisterPerMinute`
3. **Audit log**: Structured log entry on every call (success and failure)
4. **Wallet recovery**: Auto-recreate on `InvalidOperationException` with "not found" / "401"

## Audit Log Format

```
[INF] SystemWalletSigning: {Outcome} | Register={RegisterId} TxId={TxId} Type={TransactionType} Path={DerivationPath} Wallet={WalletAddress} Caller={CallerService}
```
