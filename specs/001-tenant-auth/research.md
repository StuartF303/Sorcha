# Research: Tenant Service Authentication Implementation

**Feature**: 001-tenant-auth
**Date**: 2025-11-22
**Phase**: 0 (Research & Technology Decisions)

## Overview

This document captures research findings and technology decisions for implementing the Tenant Service, which provides OAuth2/OIDC authentication, multi-organization support, and PassKey authentication for the Sorcha platform.

## Research Areas

### 1. OAuth2/OIDC Integration Patterns in .NET

**Decision**: Use Microsoft.AspNetCore.Authentication.OpenIdConnect with dynamic scheme registration

**Rationale**:
- Supports multiple external IDPs (Azure Entra, AWS Cognito, generic OIDC) with a single integration point
- Dynamic authentication scheme registration enables per-organization IDP configuration
- Built-in support for authorization code flow, token refresh, and OIDC discovery
- Well-maintained by Microsoft with security patches and .NET version updates
- Integrates seamlessly with ASP.NET Core authentication middleware

**Alternatives Considered**:
1. **IdentityServer/Duende IdentityServer**: Full-featured STS but heavyweight for our needs, licensing costs for Duende, over-engineered for our multi-tenant scenario
2. **Auth0 SDK**: Third-party dependency with vendor lock-in, costs per active user, doesn't fit our multi-IDP requirement
3. **Custom OAuth2 implementation**: High security risk, requires deep protocol expertise, reinvents well-tested wheel

**Implementation Approach**:
- Use `IAuthenticationSchemeProvider` to dynamically register schemes per organization
- Store IDP metadata (issuer, endpoints, client credentials) in PostgreSQL encrypted
- Cache authentication schemes in memory with Redis fallback for distributed scenarios
- Implement custom `IConfigureNamedOptions<OpenIdConnectOptions>` to load org-specific configuration

**Best Practices**:
- Use OIDC discovery (/.well-known/openid-configuration) to automatically fetch endpoints
- Implement PKCE (Proof Key for Code Exchange) for authorization code flow security
- Validate state and nonce parameters to prevent CSRF and replay attacks
- Handle token refresh proactively before expiration
- Log authentication events (successful/failed logins) for audit trail

**References**:
- [RFC 6749 - OAuth 2.0](https://datatracker.ietf.org/doc/html/rfc6749)
- [OpenID Connect Core 1.0](https://openid.net/specs/openid-connect-core-1_0.html)
- [Microsoft.AspNetCore.Authentication.OpenIdConnect documentation](https://learn.microsoft.com/aspnet/core/security/authentication/social/microsoft-logins)

---

### 2. JWT Token Issuance and Validation Strategy

**Decision**: Use System.IdentityModel.Tokens.Jwt with RS256 asymmetric signing

**Rationale**:
- RS256 (RSA with SHA-256) enables public key distribution for token validation without sharing private keys
- Other services can validate tokens locally using public key (no network call to Tenant Service)
- Private key rotation doesn't require updating all consuming services (publish new public key to JWKS endpoint)
- Industry standard for distributed systems and microservices
- Supports token claims for identity, organization, roles, and custom permissions

**Alternatives Considered**:
1. **HS256 (HMAC with SHA-256)**: Symmetric signing requires sharing secret with all services, key rotation is complex, security risk if secret leaks
2. **ES256 (ECDSA with P-256)**: Smaller keys and signatures, faster verification, but less tooling support in .NET ecosystem, acceptable for future optimization
3. **Opaque tokens with introspection**: Requires network call to validate every token (latency), single point of failure, doesn't scale well

**Implementation Approach**:
- Generate RSA key pair (4096-bit) on first startup, store private key in Azure Key Vault (production) or local DPAPI (development)
- Publish public key via JWKS (JSON Web Key Set) endpoint at /.well-known/jwks.json
- Include standard claims: iss (issuer), sub (subject/user ID), aud (audience), exp (expiration), iat (issued at), jti (token ID)
- Include custom claims: org_id, roles (administrator, auditor, member), permitted_blockchains, can_create_blockchain, can_publish_blueprint
- Set token lifetime: 1-hour access token, 24-hour refresh token (configurable via appsettings.json)
- Implement token revocation using Redis-backed blacklist (store JTI of revoked tokens with TTL = token expiration)

**Best Practices**:
- Use short-lived access tokens (1 hour) to minimize exposure window
- Implement token refresh flow to avoid frequent re-authentication
- Include audience (aud) claim to prevent token reuse across services
- Validate token signature, expiration, issuer, and audience on every request
- Use clock skew tolerance (±5 minutes) for distributed system time sync issues
- Implement key rotation strategy (30-day rotation, publish both old and new keys during transition)

**Token Claims Structure**:
```json
{
  "iss": "https://tenant.sorcha.io",
  "sub": "user-uuid-here",
  "aud": ["https://blueprint.sorcha.io", "https://wallet.sorcha.io", "https://register.sorcha.io"],
  "exp": 1700000000,
  "iat": 1699996400,
  "jti": "token-uuid-here",
  "org_id": "organization-uuid",
  "org_name": "Acme Corp",
  "roles": ["member"],
  "permitted_blockchains": ["blockchain-id-1", "blockchain-id-2"],
  "can_create_blockchain": false,
  "can_publish_blueprint": true,
  "token_type": "user"
}
```

**References**:
- [RFC 7519 - JSON Web Token](https://datatracker.ietf.org/doc/html/rfc7519)
- [RFC 7517 - JSON Web Key](https://datatracker.ietf.org/doc/html/rfc7517)
- [System.IdentityModel.Tokens.Jwt documentation](https://learn.microsoft.com/dotnet/api/system.identitymodel.tokens.jwt)

---

### 3. PassKey/FIDO2 Implementation

**Decision**: Use Fido2NetLib (passwordless-lib) for WebAuthn server-side implementation

**Rationale**:
- Open-source .NET library implementing FIDO2/WebAuthn server specification
- Actively maintained with regular security updates
- Supports registration ceremony (creating credentials) and assertion ceremony (authentication)
- Handles attestation verification, challenge generation, and credential storage
- Works with all FIDO2-compliant authenticators (YubiKey, platform authenticators, biometrics)

**Alternatives Considered**:
1. **Custom WebAuthn implementation**: Complex cryptographic protocol, high security risk, requires deep understanding of CTAP2 and attestation formats
2. **Microsoft.AspNetCore.Identity with FIDO2**: Tied to ASP.NET Core Identity framework, doesn't fit our custom multi-tenant architecture
3. **Third-party SaaS (e.g., Auth0 Passwordless)**: Vendor lock-in, additional costs, external dependency for critical auth flow

**Implementation Approach**:
- Store FIDO2 configuration (RP ID, RP name, origins) in appsettings.json
- Generate random challenges (32 bytes) for each authentication attempt, store in Redis with 5-minute TTL
- Store credential ID, public key, counter, and device metadata in PostgreSQL (PublicIdentity table)
- Validate origin and RP ID hash during authentication to prevent phishing
- Increment and verify signature counter to detect cloned authenticators
- Support both platform authenticators (TouchID, Windows Hello) and roaming authenticators (YubiKey, security keys)

**Best Practices**:
- Use userVerification: "preferred" to encourage biometric/PIN verification
- Store credential public key in COSE format (CBOR Object Signing and Encryption)
- Implement attestation verification for high-security scenarios (optional for public users)
- Allow multiple credentials per user (backup authenticators)
- Provide clear fallback mechanisms if PassKey registration/authentication fails
- Log PassKey events (registration, authentication) for audit trail

**Registration Flow**:
1. User requests PassKey registration → generate challenge, store in Redis
2. Browser calls navigator.credentials.create() with challenge and RP info
3. Authenticator creates key pair, returns attestation object
4. Server validates attestation, challenge, origin → store credential in database
5. Issue JWT token for authenticated session

**Authentication Flow**:
1. User initiates login → generate challenge, store in Redis
2. Browser calls navigator.credentials.get() with challenge and credential IDs
3. Authenticator signs challenge with private key, returns assertion
4. Server validates signature, challenge, origin, counter → issue JWT token

**References**:
- [WebAuthn Specification](https://www.w3.org/TR/webauthn-2/)
- [FIDO2 CTAP Specification](https://fidoalliance.org/specs/fido-v2.0-ps-20190130/fido-client-to-authenticator-protocol-v2.0-ps-20190130.html)
- [Fido2NetLib GitHub](https://github.com/passwordless-lib/fido2-net-lib)

---

### 4. Multi-Tenancy Data Isolation Strategy

**Decision**: Database-per-tenant isolation using PostgreSQL schemas with shared TenantDbContext

**Rationale**:
- **Data Security**: Complete logical isolation between organizations, prevents accidental data leakage
- **Compliance**: Easier to meet data residency and isolation requirements (GDPR, SOC 2)
- **Performance**: Query performance doesn't degrade with more tenants (each schema is independent)
- **Scalability**: Can migrate specific tenants to separate databases or servers if needed
- **Backup/Restore**: Granular backup and restore per organization

**Alternatives Considered**:
1. **Single database with TenantId column (row-level isolation)**: Simpler implementation, risk of query bugs exposing cross-tenant data, complex query filtering, performance degrades with scale
2. **Separate database per tenant**: Maximum isolation but high operational overhead, expensive for 100+ tenants, difficult to manage migrations and upgrades
3. **NoSQL multi-tenant (MongoDB)**: Good for document-heavy data but our domain is relational (organizations, identities, permissions, audit logs)

**Implementation Approach**:
- Use PostgreSQL schemas: one schema per organization (e.g., org_acme, org_contoso)
- Shared tables across all tenants: Organizations, IdentityProviderConfigurations (metadata only)
- Per-tenant tables: UserIdentities, OrganizationPermissionConfigurations, AuditLogEntries
- Use EF Core with dynamic schema resolution based on organization context
- Implement `ITenantProvider` service to resolve current organization from JWT claims or API key
- Configure `TenantDbContext.OnModelCreating` to set schema dynamically via `modelBuilder.HasDefaultSchema(schema)`

**Migration Strategy**:
- Create initial migration with default schema (public)
- On organization creation, run migration to create new schema with all tables
- Use EF Core migration history table per schema to track versions
- Automate schema creation via `OrganizationService.CreateOrganization()` method

**Best Practices**:
- Always inject `ITenantProvider` into repositories to ensure correct schema context
- Implement middleware to extract organization from JWT claims early in request pipeline
- Use connection pooling per schema to avoid connection exhaustion
- Monitor schema sizes and archive old data to prevent bloat
- Implement soft deletes for organizations (don't drop schemas immediately)

**Schema Structure Example**:
```
Database: sorcha_tenant
├── public schema (shared metadata)
│   ├── Organizations
│   └── IdentityProviderConfigurations
├── org_acme schema
│   ├── UserIdentities
│   ├── OrganizationPermissionConfigurations
│   └── AuditLogEntries
└── org_contoso schema
    ├── UserIdentities
    ├── OrganizationPermissionConfigurations
    └── AuditLogEntries
```

**References**:
- [Multi-Tenancy Patterns in SaaS Applications](https://learn.microsoft.com/azure/architecture/guide/multitenant/considerations/tenancy-models)
- [PostgreSQL Schemas Documentation](https://www.postgresql.org/docs/current/ddl-schemas.html)
- [EF Core Multi-Tenancy](https://learn.microsoft.com/ef/core/miscellaneous/multitenancy)

---

### 5. Token Revocation and Distributed Caching

**Decision**: Use Redis for token blacklisting with TTL-based automatic cleanup

**Rationale**:
- **Performance**: In-memory data store with sub-millisecond latency for token validation
- **Distributed**: Multiple Tenant Service instances share revocation list (stateless services)
- **TTL Support**: Automatically removes revoked tokens from blacklist after expiration (no manual cleanup)
- **Scalability**: Handles high read/write throughput for token operations
- **Constitutional Alignment**: Redis is already the constitutional standard for Sorcha distributed caching

**Alternatives Considered**:
1. **Database-backed revocation list**: High latency (10-50ms per query), requires database connection pooling, doesn't scale well for high-frequency token validation
2. **In-memory cache (MemoryCache)**: Not distributed, doesn't work with multiple service instances, loses state on restart
3. **Event-driven revocation (publish/subscribe)**: Complex coordination, eventual consistency issues, overkill for simple blacklist

**Implementation Approach**:
- Store revoked token JTI (token ID) as Redis key: `revoked:token:{jti}`
- Set TTL = token expiration time (tokens auto-expire anyway, no need to blacklist after expiration)
- On token validation: check if `EXISTS revoked:token:{jti}` before accepting token
- On logout/permission change: add token JTI to Redis with TTL
- Use Redis SET with EX flag for atomic operation: `SET revoked:token:{jti} 1 EX {ttl_seconds}`

**Rate Limiting Strategy** (bonus use case for Redis):
- Store failed authentication attempts per user: `failed_auth:{user_id}` with 1-minute TTL
- Increment counter on failed attempt: `INCR failed_auth:{user_id}` then `EXPIRE failed_auth:{user_id} 60`
- Block login if counter exceeds 5 attempts within 1 minute
- Store token request rate limits per client: `token_rate:{client_id}` with 1-minute sliding window

**Best Practices**:
- Use Redis connection multiplexer (StackExchange.Redis) with connection pooling
- Implement circuit breaker for Redis failures (fallback: skip revocation check but log security event)
- Monitor Redis memory usage and eviction policy (configure maxmemory and volatile-ttl eviction)
- Use Redis Sentinel or Redis Cluster for high availability in production
- Encrypt Redis traffic using TLS

**Redis Key Examples**:
```
revoked:token:550e8400-e29b-41d4-a716-446655440000    # Revoked access token
refresh:token:660e9500-f39c-42e5-b827-557766551111    # Revoked refresh token
failed_auth:user-uuid-here                            # Failed login attempts counter
token_rate:service-blueprint                          # Token request rate limit
```

**References**:
- [Redis TTL Documentation](https://redis.io/commands/ttl/)
- [StackExchange.Redis](https://stackexchange.github.io/StackExchange.Redis/)
- [Token Revocation Best Practices](https://auth0.com/docs/secure/tokens/refresh-tokens/revoke-refresh-tokens)

---

### 6. Service-to-Service Authentication (Client Credentials Flow)

**Decision**: Implement OAuth2 client credentials flow with per-service credentials and scopes

**Rationale**:
- **Standard Protocol**: OAuth2 client credentials is the industry standard for service-to-service auth
- **Scoped Permissions**: Each service has specific allowed operations (e.g., Blueprint can request wallet signatures, Register can validate transactions)
- **Auditable**: All service-to-service calls are logged with service identity
- **Stateless**: Services can validate each other's tokens without calling Tenant Service (using JWKS public key)

**Implementation Approach**:
- Pre-configure service principals in database with client_id and client_secret (encrypted)
- Services: Blueprint, Wallet, Register, Peer, Action, Validator
- Each service calls `POST /api/auth/token` with grant_type=client_credentials, client_id, client_secret
- Tenant Service issues JWT token with:
  - `sub`: service identifier (e.g., "service-blueprint")
  - `token_type`: "service"
  - `scopes`: array of permitted operations (e.g., ["wallet:sign", "register:commit"])
- Service tokens have longer lifetime (8 hours) to reduce token refresh frequency
- Consuming service validates token signature, expiration, and required scope before processing request

**Delegated Authority Flow** (service acting on behalf of user):
1. User authenticates with Tenant Service → receives user token
2. User calls Blueprint Service with user token
3. Blueprint Service validates user token, extracts user context
4. Blueprint Service requests service token from Tenant Service (client credentials)
5. Blueprint Service calls Wallet Service with BOTH service token (authentication) + user context (authorization)
6. Wallet Service validates service token, applies user's organization restrictions from context

**Service Token Claims**:
```json
{
  "iss": "https://tenant.sorcha.io",
  "sub": "service-blueprint",
  "aud": ["https://wallet.sorcha.io", "https://register.sorcha.io"],
  "exp": 1700028800,
  "iat": 1700000000,
  "token_type": "service",
  "scopes": ["wallet:sign", "register:commit", "register:read"]
}
```

**Best Practices**:
- Rotate service credentials every 90 days
- Store service credentials in Azure Key Vault (production) or Kubernetes secrets
- Implement separate scopes per service operation (principle of least privilege)
- Log all service-to-service token requests for audit trail
- Implement rate limiting per service to prevent abuse

**References**:
- [RFC 6749 Section 4.4 - Client Credentials Grant](https://datatracker.ietf.org/doc/html/rfc6749#section-4.4)
- [OAuth2 for Microservices](https://www.oauth.com/oauth2-servers/access-tokens/client-credentials/)

---

### 7. Observability and Monitoring Strategy

**Decision**: Use Serilog structured logging with Seq, OpenTelemetry for distributed tracing, and custom Prometheus metrics

**Rationale**:
- **Constitutional Requirement**: Sorcha constitution mandates Serilog + Seq for logging, Zipkin for tracing
- **Structured Logging**: JSON-formatted logs with correlation IDs enable powerful querying in Seq
- **Distributed Tracing**: Track authentication flows across multiple services (Tenant → IDP → Blueprint → Wallet)
- **Metrics**: Track token issuance rate, validation latency, failed auth attempts, revocation operations

**Implementation Approach**:

**Logging (Serilog + Seq)**:
- Log levels: Debug (development only), Information (successful operations), Warning (recoverable errors), Error (failures)
- Correlation ID: Generate unique ID per request, include in all logs, propagate to downstream services
- Sensitive data: Never log passwords, client secrets, private keys, full tokens (log only JTI/first 8 chars)
- Log events:
  - Authentication success/failure (user ID, organization, IP address, user agent)
  - Token issuance (JTI, user/service ID, expiration)
  - Token validation (JTI, result, latency)
  - Token revocation (JTI, reason)
  - IDP configuration changes (organization, admin user)
  - Permission changes (organization, affected users)

**Distributed Tracing (OpenTelemetry + Zipkin)**:
- Instrument all HTTP endpoints with OpenTelemetry middleware
- Create spans for: IDP callback processing, token generation, database queries, Redis operations
- Include span attributes: organization_id, user_id, token_type, operation_result
- Trace authentication flow: User login → IDP redirect → Callback → Token issuance → Service call

**Metrics (Prometheus + Grafana)**:
- Custom counters:
  - `tenant_auth_attempts_total{result="success|failure", org_id="..."}`
  - `tenant_tokens_issued_total{token_type="user|service|delegated"}`
  - `tenant_tokens_validated_total{result="valid|invalid|revoked"}`
  - `tenant_tokens_revoked_total{reason="logout|permission_change|security"}`
- Custom histograms:
  - `tenant_token_issuance_duration_seconds` (buckets: 0.1, 0.5, 1, 5, 10)
  - `tenant_token_validation_duration_seconds` (buckets: 0.01, 0.05, 0.1, 0.5)
- Custom gauges:
  - `tenant_active_organizations` (current count)
  - `tenant_active_users` (current count)

**Health Checks**:
- Liveness probe: /health/live (service is running)
- Readiness probe: /health/ready (database + Redis reachable, JWKS keys loaded)
- Check external IDP connectivity (optional, may slow down health checks)

**Best Practices**:
- Use structured logging properties instead of string interpolation: `Log.Information("User {UserId} authenticated", userId)`
- Include correlation IDs in responses for troubleshooting: `X-Correlation-Id` header
- Set up alerts for: high failed auth rate, token validation latency >100ms, Redis connection failures
- Dashboard KPIs: Auth success rate, avg token issuance time, active sessions, revoked tokens

**References**:
- [Serilog Documentation](https://serilog.net/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [Prometheus .NET Client](https://github.com/prometheus-net/prometheus-net)

---

## Summary of Technology Stack

| Component | Technology | Justification |
|-----------|-----------|---------------|
| **Framework** | .NET 10, ASP.NET Core Minimal APIs | Constitutional requirement, modern async/await patterns |
| **External IDP Integration** | Microsoft.AspNetCore.Authentication.OpenIdConnect | Industry standard, dynamic scheme registration, maintained by Microsoft |
| **JWT Handling** | System.IdentityModel.Tokens.Jwt | Built-in .NET library, RS256 support, JWKS publishing |
| **PassKey/WebAuthn** | Fido2NetLib | Open-source, actively maintained, full FIDO2 spec compliance |
| **Database** | PostgreSQL + Entity Framework Core | Constitutional standard for relational data, multi-tenant schema support |
| **Caching/Revocation** | Redis (StackExchange.Redis) | Constitutional standard, distributed state, TTL support |
| **Logging** | Serilog + Seq | Constitutional requirement, structured logging |
| **Tracing** | OpenTelemetry + Zipkin | Constitutional requirement, distributed tracing |
| **Metrics** | Prometheus | Industry standard, integrates with Grafana dashboards |
| **API Documentation** | .NET 10 OpenAPI + Scalar.AspNetCore | Constitutional mandate (no Swagger/Swashbuckle) |
| **Testing** | xUnit, FluentAssertions, Moq, Testcontainers, NBomber | Constitutional standard, comprehensive test coverage |
| **Orchestration** | .NET Aspire | Constitutional standard for microservices |

---

## Open Questions Resolved

✅ **How to handle multiple external IDPs per organization?**
- Resolved: Use dynamic authentication scheme registration with `IAuthenticationSchemeProvider`
- Each organization gets a unique scheme name: `oidc-{org-id}`
- Load IDP configuration from database on demand, cache in memory

✅ **How to prevent token reuse across services?**
- Resolved: Include audience (aud) claim with specific service URLs
- Services validate their URL is in aud claim array before accepting token

✅ **How to handle token revocation in distributed system?**
- Resolved: Redis-backed blacklist with JTI keys and TTL matching token expiration
- Circuit breaker fallback if Redis unavailable (log security event, allow request)

✅ **How to store PassKey credentials securely?**
- Resolved: Store public key and credential ID in PostgreSQL (public data, safe to persist)
- Private key never leaves user's authenticator device

✅ **How to enforce organization-level permissions in other services?**
- Resolved: Include permitted_blockchains, can_create_blockchain, can_publish_blueprint as custom JWT claims
- Consuming services check claims before allowing operations

---

## Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| **External IDP downtime** | Implement retry with exponential backoff, cache successful authentications, display user-friendly error messages |
| **Token theft/replay** | Short token lifetimes (1 hour), HTTPS only, validate aud claim, implement token binding (future) |
| **PassKey device loss** | Allow multiple credentials per user, provide fallback to organization IDP |
| **Redis failure** | Circuit breaker pattern, graceful degradation (skip revocation check, log security event), Redis Cluster for HA |
| **JWKS key rotation** | Publish both old and new keys during transition period, validate tokens with either key |
| **Clock skew** | Allow ±5 minute tolerance for token expiration validation |

---

## Next Steps

Phase 1 will generate:
1. **data-model.md**: EF Core entities, relationships, database schema, migrations
2. **contracts/*.yaml**: OpenAPI specifications for all endpoints
3. **quickstart.md**: Local development setup, mock IDP configuration, testing guide
