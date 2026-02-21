# Contract: DID Resolver

**Type**: Internal library interface (not REST)
**Location**: `src/Common/Sorcha.ServiceClients/Did/`

## IDidResolver

```
ResolveAsync(did: string, ct: CancellationToken) → DidDocument?
CanResolve(didMethod: string) → bool
```

**Behavior**:
- Returns `null` if DID not found or resolution fails
- Throws `TimeoutException` if resolution exceeds 5 seconds (did:web)
- `CanResolve` returns `true` for the method string (e.g., "sorcha", "web", "key")

## IDidResolverRegistry

```
ResolveAsync(did: string, ct: CancellationToken) → DidDocument?
Register(resolver: IDidResolver) → void
```

**Behavior**:
- Parses DID to extract method
- Delegates to registered resolver for that method
- Returns `null` with warning log if no resolver registered for method

## Resolution Rules

| DID Format | Method | Resolution |
|------------|--------|-----------|
| `did:sorcha:w:{address}` | sorcha | Query wallet service → public key → DID Document |
| `did:sorcha:r:{regId}:t:{txId}` | sorcha | Query register service → TX payload → public key → DID Document |
| `did:web:{domain}` | web | HTTPS GET `https://{domain}/.well-known/did.json` → parse DID Document |
| `did:web:{domain}:{path}` | web | HTTPS GET `https://{domain}/{path}/did.json` → parse DID Document |
| `did:key:{multibase}` | key | Decode multicodec from multibase string → public key → DID Document |

## DI Registration

```
services.AddDidResolvers()  // Registers IDidResolverRegistry + all 3 resolvers
```
