# Feature Specification: Tenant Service Authentication & Multi-Organization Identity Management

**Feature Branch**: `001-tenant-auth`
**Created**: 2025-11-22
**Status**: Draft
**Input**: User description: "Tenant Service which acts as the Secure Token Service provider for external identities that belong to a given organisation. It can be configured for multiple organisations, each one organisation having its own branding and identity provider - be that Azure Entra, Amazon or any other external Identity Provider (IDP). We can also have a public identity provider that allows for an identity to access the service based on a PassKey or other provided device token."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Organization Administrator Configures External IDP (Priority: P1)

An organization administrator needs to configure their organization's identity provider (Azure Entra, AWS Cognito, or other OIDC-compliant IDP) so that employees can sign in using their existing corporate credentials.

**Why this priority**: This is the foundational capability - without IDP integration, organizations cannot use the platform with their existing identity infrastructure. This enables SSO and eliminates the need for separate credential management.

**Independent Test**: Can be fully tested by configuring a test organization with Azure Entra, authenticating a user, and verifying a valid JWT token is issued. Delivers immediate value by allowing enterprise authentication.

**Acceptance Scenarios**:

1. **Given** an organization administrator is logged in, **When** they navigate to organization settings and provide IDP configuration (issuer URL, client ID, client secret, scopes), **Then** the system validates the configuration and saves it successfully
2. **Given** an organization has configured Azure Entra as their IDP, **When** a user from that organization attempts to sign in, **Then** they are redirected to Azure Entra login page and upon successful authentication receive a Sorcha JWT token
3. **Given** an organization has configured custom branding (logo, colors, company name), **When** a user visits the login page for that organization, **Then** the branded login experience is displayed
4. **Given** invalid IDP credentials are provided, **When** the administrator attempts to save the configuration, **Then** the system displays clear error messages indicating which configuration values are incorrect

---

### User Story 2 - User Authenticates with Organization Credentials (Priority: P1)

A user needs to sign in using their organization's credentials to access Sorcha services, with the authentication flow being seamless and following standard OAuth2/OIDC patterns.

**Why this priority**: This is the core user experience - users must be able to authenticate before they can use any feature. Without this, the platform is inaccessible.

**Independent Test**: Can be fully tested by attempting login with valid organization credentials, receiving a token, and using that token to access a protected service endpoint. Delivers the complete authentication flow.

**Acceptance Scenarios**:

1. **Given** a user visits the Sorcha platform with their organization identifier, **When** they click "Sign In", **Then** they are redirected to their organization's configured IDP for authentication
2. **Given** a user successfully authenticates with their IDP, **When** they are redirected back to Sorcha, **Then** they receive a JWT token containing their identity, organization, and roles
3. **Given** a user has a valid token, **When** they make requests to Sorcha services, **Then** the token is validated and their requests are authorized based on their organization and role
4. **Given** a user's token expires, **When** they attempt to access a service, **Then** they receive a 401 Unauthorized response and are prompted to re-authenticate
5. **Given** a user logs out, **When** their session is terminated, **Then** their token is invalidated and they cannot access protected resources

---

### User Story 3 - Public User Authenticates with PassKey (Priority: P2)

A public user (not affiliated with an organization) needs to access public blockchain data using a PassKey, FIDO2, or device-based authentication without requiring traditional username/password credentials.

**Why this priority**: This enables public access to the platform without requiring organizational affiliation, expanding the user base. It's P2 because organizational access is more critical for the MVD.

**Independent Test**: Can be fully tested by registering a PassKey, authenticating with it, and accessing public blockchain data. Delivers standalone value for public access scenarios.

**Acceptance Scenarios**:

1. **Given** a new public user visits the platform, **When** they choose "Sign in with PassKey", **Then** they are prompted to register a PassKey using their device (biometric, security key, or PIN)
2. **Given** a public user has registered a PassKey, **When** they return to the platform and authenticate with their PassKey, **Then** they receive a JWT token with public user permissions
3. **Given** a public user is authenticated, **When** they attempt to access public blockchain data, **Then** they can successfully read transactions and blocks marked as public
4. **Given** a public user attempts to access organization-restricted resources, **When** authorization is checked, **Then** they receive a 403 Forbidden response

---

### User Story 4 - Organization Administrator Manages User Permissions (Priority: P2)

An organization administrator needs to configure which blockchains/gateways their users can access and whether users can create blockchains or publish blueprints.

**Why this priority**: This provides essential access control for organizations to manage their users' capabilities. It's P2 because basic authentication must work first (P1), but access control is required before production use.

**Independent Test**: Can be fully tested by configuring blockchain access restrictions, attempting user actions, and verifying permissions are enforced. Delivers organizational policy enforcement.

**Acceptance Scenarios**:

1. **Given** an organization administrator is in organization settings, **When** they specify which blockchains are approved for their organization, **Then** only those blockchains appear in users' available blockchain lists
2. **Given** an organization has disabled blockchain creation, **When** a user from that organization attempts to create a new blockchain, **Then** they receive a permission denied error
3. **Given** an organization has enabled blueprint publishing for specific users, **When** those users attempt to publish a blueprint, **Then** the operation succeeds, and when non-authorized users attempt it, they are denied
4. **Given** an administrator updates permissions, **When** affected users next authenticate or refresh their tokens, **Then** their new permissions are reflected immediately

---

### User Story 5 - Organization Auditor Reviews Activity (Priority: P3)

An organization auditor needs read-only access to view organizational settings, user activities, and reports without the ability to modify any configurations.

**Why this priority**: This supports compliance and audit requirements but is not critical for initial platform operation. P3 because the system can function without audit capabilities initially.

**Independent Test**: Can be fully tested by logging in as an auditor, viewing reports and settings, and verifying all modification operations are blocked. Delivers compliance capability.

**Acceptance Scenarios**:

1. **Given** an auditor logs in with their organization credentials, **When** they navigate to organization settings, **Then** they can view all configuration but all edit controls are disabled
2. **Given** an auditor accesses activity reports, **When** they filter by date range or user, **Then** they can view detailed logs of user actions and authentication events
3. **Given** an auditor attempts to modify any organizational setting, **When** the system checks their role, **Then** the operation is denied with a clear "auditor role is read-only" message

---

### User Story 6 - Service-to-Service Authentication (Priority: P1)

Internal Sorcha services (Blueprint Service, Wallet Service, Register Service) need to authenticate with each other using service principals or client credentials, and all communication must be encrypted.

**Why this priority**: This is foundational security architecture - services cannot communicate without secure authentication. Without this, the microservices architecture cannot function securely.

**Independent Test**: Can be fully tested by having Blueprint Service request a service token and call Register Service to commit a transaction. Delivers secure inter-service communication.

**Acceptance Scenarios**:

1. **Given** a service (e.g., Blueprint Service) needs to call another service (e.g., Wallet Service), **When** it requests a service token from Tenant Service with its client credentials, **Then** it receives a JWT token with service-level claims
2. **Given** a service token is issued, **When** the service presents it to another service, **Then** the receiving service validates the token and authorizes the request based on service-level permissions
3. **Given** service-to-service communication occurs, **When** data is transmitted, **Then** all traffic is encrypted using TLS 1.3 or higher
4. **Given** delegated authority is required (e.g., Blueprint executing an action on behalf of a user), **When** the service presents both service token and user context, **Then** the receiving service validates both and applies appropriate authorization (user's organization restrictions + service permissions)

---

### Edge Cases

- What happens when an external IDP is temporarily unavailable during user authentication?
- How does the system handle token refresh when a user's permissions have changed mid-session?
- What happens when an organization's IDP configuration changes while users are actively authenticated?
- How does the system handle rate limiting when automated services make high-frequency token requests?
- What happens when a user belongs to multiple organizations?
- How does the system handle PassKey registration failure or lost device scenarios?
- What happens when delegated authority is used but the user's permissions have been revoked?
- How does the system handle clock skew between services when validating JWT token expiration?
- What happens when an organization is deleted while users are authenticated?
- How does the system handle concurrent login attempts from different devices for the same user?

## Clarifications

### Session 2025-11-22

- Q: What should be the default lifetime for access tokens issued by the Tenant Service? → A: 1 hour access token, 24-hour refresh token
- Q: What approach should the peer reputation system use to evaluate and manage remote peer trustworthiness? → A: Threshold-based scoring with manual override

## Requirements *(mandatory)*

### Functional Requirements

#### Core Authentication & Token Management

- **FR-001**: System MUST act as an OAuth2/OIDC Secure Token Service (STS) issuing JWT tokens for authenticated identities
- **FR-002**: System MUST support multiple organizations, each with independent configuration, branding, and identity provider
- **FR-003**: System MUST integrate with external identity providers including Azure Entra ID, AWS Cognito, and any OIDC-compliant IDP
- **FR-004**: System MUST support public user authentication using PassKeys (FIDO2/WebAuthn) and device tokens
- **FR-005**: Issued tokens MUST contain claims for: identity (user ID), organization ID, roles, permitted blockchains, and permissions (can create blockchain, can publish blueprint)
- **FR-006**: System MUST support token refresh flows to extend sessions without re-authentication
- **FR-007**: System MUST validate tokens presented by services and extract claims for authorization decisions
- **FR-008**: System MUST support immediate token revocation (logout, permission changes, security events)

#### Multi-Organization Configuration

- **FR-009**: Each organization MUST have a unique identifier and tenant configuration
- **FR-010**: System MUST allow organization-specific branding (logo, colors, company name) displayed during authentication flows
- **FR-011**: Organization configuration MUST specify which external IDP to use (Azure Entra, AWS, etc.) with required connection parameters (issuer URL, client ID, client secret, scopes)
- **FR-012**: System MUST validate IDP configuration during setup by performing a test authentication flow
- **FR-013**: Organization creators MUST automatically receive administrator role for their organization

#### Role-Based Access Control

- **FR-014**: System MUST support organization-level roles: Administrator, Auditor, and Member (standard user)
- **FR-015**: Administrators MUST be able to modify organizational settings and view reports
- **FR-016**: Auditors MUST have read-only access to organizational settings and reports with no modification capabilities
- **FR-017**: Members MUST have access only to services and resources permitted by their organization configuration
- **FR-018**: System MUST enforce role permissions at the token level (roles encoded in JWT claims)

#### Organization-Level Permissions

- **FR-019**: System MUST allow administrators to configure which blockchains or gateways organization members can access
- **FR-020**: System MUST allow administrators to control whether organization members can create new blockchains
- **FR-021**: System MUST allow administrators to control whether organization members can publish blueprints
- **FR-022**: User tokens MUST include organization-specific permissions so downstream services can enforce restrictions
- **FR-023**: Public users MUST only access blockchains explicitly marked as public, regardless of organization settings

#### Service-to-Service Authentication

- **FR-024**: System MUST issue service tokens for inter-service communication using client credentials flow
- **FR-025**: Service tokens MUST identify the calling service and include service-level permissions
- **FR-026**: System MUST support delegated authority where a service acts on behalf of a user (service token + user context)
- **FR-027**: All intra-service communications MUST be authenticated using valid service tokens
- **FR-028**: All intra-service communications MUST be encrypted using TLS 1.3 or higher
- **FR-029**: System MUST validate both service identity and user context when delegated authority is used (e.g., Blueprint Service committing transaction for a user)

#### Security & Compliance

- **FR-030**: System MUST log all authentication events (successful logins, failed attempts, token issuance, token revocation) for audit purposes
- **FR-031**: System MUST implement rate limiting on token endpoints to prevent abuse (max 100 token requests per minute per client)
- **FR-032**: System MUST validate token signatures and expiration before accepting any token
- **FR-033**: System MUST support token expiration with configurable lifetimes (default: 1-hour access token, 24-hour refresh token)
- **FR-034**: System MUST protect against common OAuth2 vulnerabilities (CSRF, token replay, authorization code interception)
- **FR-035**: Sensitive configuration data (IDP client secrets, signing keys) MUST be encrypted at rest

#### Peer-to-Peer Authentication (Future Scope)

- **FR-036**: System MUST support peer installation authentication where remote Peer Services authenticate with local installation
- **FR-037**: System MUST implement threshold-based reputation scoring algorithm with manual override capability to allow or blacklist remote peer sites (peers earn/lose reputation points based on behavior; below threshold triggers auto-block, administrators can whitelist/blacklist peers manually)
- **FR-038**: Peer reputation data MUST persist across sessions and be configurable by administrators

### Key Entities

- **Organization (Tenant)**: Represents a company or group using the platform
  - Attributes: unique identifier, name, branding configuration (logo, colors), IDP configuration, creator identity, created date, status (active/suspended)
  - Relationships: has many users, has one IDP configuration, has one permission configuration

- **Identity Provider Configuration**: External authentication provider settings for an organization
  - Attributes: IDP type (Azure Entra, AWS, OIDC generic), issuer URL, client ID, client secret (encrypted), authorization endpoint, token endpoint, scopes, metadata discovery URL
  - Relationships: belongs to one organization

- **User Identity**: Represents an authenticated user within an organization
  - Attributes: unique identifier, organization identifier, external IDP user ID, email, display name, roles (administrator, auditor, member), status (active/suspended), created date, last login date
  - Relationships: belongs to one organization (or none for public users), has many tokens (current and revoked)

- **Public Identity**: Represents a user authenticated via PassKey without organizational affiliation
  - Attributes: unique identifier, PassKey credential ID, public key, device type, registered date, last used date
  - Relationships: has many tokens

- **Organization Permission Configuration**: Defines what members of an organization can do
  - Attributes: organization identifier, approved blockchain identifiers (list), can create blockchain (boolean), can publish blueprint (boolean), custom permission rules
  - Relationships: belongs to one organization

- **JWT Token (issued)**: Authentication token issued by the service
  - Attributes: token ID (JTI), issuer, subject (user/service identity), organization ID, audience (target services), issued at, expires at, roles, permissions (blockchain access, create blockchain, publish blueprint), token type (user, service, delegated)
  - Relationships: issued for one identity, revocable

- **Service Principal**: Represents an internal Sorcha service for service-to-service authentication
  - Attributes: service identifier, service name (Blueprint Service, Wallet Service, etc.), client credentials, allowed operations, status
  - Relationships: can have many service tokens

- **Audit Log Entry**: Records authentication and authorization events
  - Attributes: timestamp, event type (login, logout, token issued, token revoked, permission denied), identity identifier, organization identifier, IP address, user agent, success/failure, details
  - Relationships: associated with one identity, one organization

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can complete organization-based authentication in under 10 seconds from clicking "Sign In" to receiving a valid token
- **SC-002**: Public users can register and authenticate with PassKey in under 30 seconds
- **SC-003**: System supports at least 1,000 concurrent user authentications without token issuance delays exceeding 500ms
- **SC-004**: Service-to-service token validation completes in under 50ms for 95% of requests
- **SC-005**: Token refresh operations complete in under 200ms
- **SC-006**: 100% of authentication events are logged with complete audit trail (user, timestamp, outcome, IP address)
- **SC-007**: Organization administrators can configure external IDP and test authentication within 5 minutes
- **SC-008**: Permission changes (blockchain access, role modifications) take effect within 5 minutes of configuration (next token refresh)
- **SC-009**: System maintains 99.9% uptime for authentication services
- **SC-010**: Zero authentication bypass vulnerabilities detected in security audit
- **SC-011**: 95% of users successfully authenticate on first attempt without errors
- **SC-012**: Failed authentication attempts are rate-limited, preventing more than 5 failed attempts in 1 minute per user

### User Satisfaction Metrics

- **SC-013**: Organization administrators report authentication setup as "straightforward" (average setup time under 10 minutes)
- **SC-014**: Users report seamless SSO experience when using organizational credentials
- **SC-015**: Auditors can locate required authentication logs and reports within 2 minutes

## Dependencies & Assumptions

### Dependencies

- External identity providers (Azure Entra, AWS Cognito) must support OAuth2/OIDC standard flows
- Services consuming tokens (Blueprint, Wallet, Register) must implement token validation logic
- Infrastructure must support TLS 1.3 for encrypted service-to-service communication
- FIDO2/WebAuthn support in user browsers for PassKey authentication

### Assumptions

- Organizations will provide valid IDP configuration parameters (client ID, secret, endpoints)
- Users have modern browsers supporting PassKey/WebAuthn for public authentication
- Token expiration and refresh intervals will be configurable by deployment administrators (confirmed default: 1-hour access token lifetime, 24-hour refresh token lifetime per FR-033)
- Organization identifiers can be determined from user context (subdomain, login page selection, or email domain)
- Services will include organization context when making delegated authority requests
- Clock synchronization across services is maintained within 30 seconds for accurate token expiration validation
- Initial implementation will support up to 100 organizations; horizontal scaling may be required beyond this
- Peer reputation algorithm will use threshold-based scoring with manual override as clarified in FR-037

### Out of Scope (Not Included)

- User profile management (name changes, email updates) - handled by external IDPs
- Password reset flows - delegated to external IDPs
- Multi-factor authentication (MFA) - delegated to external IDPs, not enforced by Tenant Service
- Fine-grained permission models beyond blockchain access and blueprint publishing
- Social login providers (Google, Facebook, GitHub) - may be added in future iterations
- Advanced audit features (anomaly detection, alerting) - basic logging only in MVD
- User provisioning/deprovisioning automation - manual administration in MVD
- Cross-organization user access (user in multiple tenants simultaneously)
