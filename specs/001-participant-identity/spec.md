# Feature Specification: Participant Identity Registry

**Feature Branch**: `001-participant-identity`
**Created**: 2026-01-24
**Status**: Draft
**Input**: User description: "Participant Identity Registry - A system to manage participant identities that bridge Tenant Service users with Blueprint workflow participants and their Wallet signing keys."

## Clarifications

### Session 2026-01-24

- Q: How should cross-organization wallet address linking be handled given platform-wide uniqueness? → A: Require explicit transfer - address can only be linked to one organization at a time; must unlink before linking to another.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Organization Admin Registers Participant (Priority: P1)

An organization administrator needs to register users as participants within their organization so they can be assigned to workflow roles in blueprints. The admin selects an existing user from the Tenant Service, assigns them a display name for participant contexts, and the system creates a participant identity linked to that user.

**Why this priority**: Core functionality - without participant registration, no workflows can be executed. This is the foundation for all other participant operations.

**Independent Test**: Can be fully tested by an admin creating a participant record for an existing user, then verifying the participant appears in the organization's participant directory.

**Acceptance Scenarios**:

1. **Given** an authenticated organization administrator and an existing user in the organization, **When** the admin registers the user as a participant with a display name, **Then** a participant identity is created and linked to that user account.
2. **Given** an authenticated organization administrator, **When** the admin attempts to register a user who is already a participant, **Then** the system displays an error indicating the user is already registered as a participant.
3. **Given** an authenticated organization administrator, **When** the admin attempts to register a user from a different organization, **Then** the system denies the operation with an authorization error.

---

### User Story 2 - Participant Links Wallet Address (Priority: P1)

A registered participant needs to link their wallet address to their participant identity so they can sign transactions in workflows. The participant selects from their available wallet addresses (from the Wallet Service), signs a challenge message to prove ownership, and the system records the verified link.

**Why this priority**: Critical for DAD model - participants must have verified signing keys to participate in workflows that require cryptographic signatures.

**Independent Test**: Can be fully tested by a participant selecting a wallet address, completing the signature challenge, and verifying the address appears as linked in their participant profile.

**Acceptance Scenarios**:

1. **Given** an authenticated participant with a wallet containing addresses, **When** the participant initiates wallet linking and selects an address, **Then** the system presents a unique challenge message to sign.
2. **Given** a participant with a pending challenge, **When** the participant signs the challenge with the correct private key, **Then** the wallet address is linked to their participant identity.
3. **Given** a participant with a pending challenge, **When** the participant provides an invalid signature, **Then** the system rejects the link and displays a verification failure message.
4. **Given** a participant with an already linked address, **When** the participant attempts to link the same address again, **Then** the system indicates the address is already linked.

---

### User Story 3 - Blueprint Designer Assigns Participant to Role (Priority: P2)

A blueprint designer needs to assign registered participants to roles within a blueprint workflow. The designer opens the participant assignment panel, searches or browses the organization's participant directory, and assigns participants to specific roles defined in the blueprint schema.

**Why this priority**: Enables workflow execution by mapping abstract roles (e.g., "Buyer", "Seller") to concrete participant identities with signing capabilities.

**Independent Test**: Can be fully tested by opening an existing blueprint, assigning a registered participant to a defined role, saving the assignment, and verifying the participant appears in the blueprint's participant configuration.

**Acceptance Scenarios**:

1. **Given** a blueprint with defined participant roles and registered participants in the organization, **When** the designer opens the participant assignment panel for a role, **Then** the system displays searchable participants from the organization.
2. **Given** a designer assigning a participant to a role, **When** the role requires signing capability and the participant has no linked wallet, **Then** the system warns that the participant cannot sign transactions until they link a wallet.
3. **Given** a designer with a completed participant assignment, **When** the designer saves the blueprint, **Then** the participant-to-role mappings are persisted with the blueprint.

---

### User Story 4 - Participant Self-Registration (Priority: P2)

A user who is already a member of an organization wants to self-register as a participant. The user navigates to the participant section, confirms their intent to become a participant, and optionally links a wallet address during registration.

**Why this priority**: Reduces admin burden by allowing users to opt-in to participant status, while maintaining organizational context.

**Independent Test**: Can be fully tested by a user navigating to participant self-registration, completing the flow, and verifying they appear in the participant directory.

**Acceptance Scenarios**:

1. **Given** an authenticated user who is a member of an organization but not yet a participant, **When** the user initiates self-registration, **Then** the system creates a participant identity using their user profile information.
2. **Given** a user completing self-registration, **When** the user chooses to link a wallet during registration, **Then** the wallet linking flow is presented inline.
3. **Given** a user who is already a participant, **When** the user attempts to self-register, **Then** the system redirects to their existing participant profile.

---

### User Story 5 - Search and Discover Participants (Priority: P2)

A user needs to find participants by name, email, organization, or public key/address. The user enters search criteria, and the system returns matching participants with their key information (display name, organization, linked wallet status).

**Why this priority**: Supports workflow assignment and audit capabilities by enabling participant discovery across the platform.

**Independent Test**: Can be fully tested by searching for a known participant by various criteria and verifying correct results are returned.

**Acceptance Scenarios**:

1. **Given** registered participants across multiple organizations, **When** an admin searches by participant display name, **Then** the system returns matching participants the admin has permission to view.
2. **Given** registered participants with linked wallets, **When** a user searches by wallet address or public key, **Then** the system returns the participant linked to that address.
3. **Given** a user without admin privileges, **When** the user searches for participants, **Then** only participants within the user's organization(s) are returned.

---

### User Story 6 - Manage Linked Wallet Addresses (Priority: P3)

A participant needs to manage their linked wallet addresses - viewing current links, adding new addresses, or revoking previously linked addresses. The participant accesses their profile, views linked addresses, and can modify their linked addresses as needed.

**Why this priority**: Supports key rotation and recovery scenarios where participants need to update their signing keys.

**Independent Test**: Can be fully tested by viewing a participant's linked addresses, adding a new address, and revoking an old address.

**Acceptance Scenarios**:

1. **Given** a participant with linked wallet addresses, **When** the participant views their profile, **Then** all linked addresses are displayed with their verification status and link date.
2. **Given** a participant wanting to add a new address, **When** the participant completes the wallet linking flow for a new address, **Then** the new address is added while preserving existing links.
3. **Given** a participant with multiple linked addresses, **When** the participant revokes an address, **Then** the address is marked as revoked with a timestamp but historical records remain for audit.

---

### User Story 7 - Multi-Organization Participant (Priority: P3)

A user who belongs to multiple organizations needs to manage their participant identities across those organizations. Each organization maintains its own participant record for the user, but the user can view and manage all their participant identities from a unified profile view.

**Why this priority**: Supports real-world scenarios where individuals participate in workflows across multiple organizations.

**Independent Test**: Can be fully tested by a user belonging to two organizations, registering as a participant in both, and viewing both identities from a unified view.

**Acceptance Scenarios**:

1. **Given** a user who is a member of two organizations, **When** the user registers as a participant in both, **Then** two separate participant identities are created, each scoped to its organization.
2. **Given** a multi-organization participant, **When** the participant views their unified profile, **Then** all their participant identities are displayed grouped by organization.
3. **Given** a participant with a wallet address linked in one organization, **When** the participant wants to use that address in a different organization, **Then** they must first unlink it from the current organization before linking it to the new one.

---

### Edge Cases

- What happens when a user is removed from an organization but has active workflow assignments? The participant identity should be marked inactive but preserved for audit; active assignments should be flagged for reassignment.
- How does the system handle wallet address collision (same address linked to different participants)? A wallet address can only be linked to one participant identity at a time across the entire platform.
- What happens when a linked wallet is deleted from the Wallet Service? The link remains but is marked as "wallet unavailable" - participant cannot sign until they link a new address.
- How are participant identities handled when organizations merge? Administrative tooling should support bulk reassignment of participants to a new organization.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow organization administrators to register existing organization users as participants.
- **FR-002**: System MUST allow users to self-register as participants within organizations they belong to.
- **FR-003**: System MUST support linking wallet addresses to participant identities through cryptographic signature verification.
- **FR-004**: System MUST enforce that a wallet address can only be linked to one participant identity at a time (platform-wide uniqueness); users must explicitly unlink an address from one organization before linking it to another.
- **FR-005**: System MUST allow participants to link multiple wallet addresses to their identity.
- **FR-006**: System MUST allow participants to revoke linked wallet addresses while preserving audit history.
- **FR-007**: System MUST provide participant search by display name, email, organization, and wallet address/public key.
- **FR-008**: System MUST enforce organization-scoped visibility for participant searches (users see only participants in their organizations unless they have system admin privileges).
- **FR-009**: System MUST allow blueprint designers to assign registered participants to workflow roles.
- **FR-010**: System MUST warn designers when assigning a participant without linked wallet to a role requiring signing capability.
- **FR-011**: System MUST support participants belonging to multiple organizations with separate participant identities per organization.
- **FR-012**: System MUST log all participant identity changes (creation, wallet linking, revocation) for audit purposes.
- **FR-013**: System MUST validate participant existence and wallet link status during blueprint execution before allowing transaction signing.
- **FR-014**: System MUST mark participant identities as inactive when the underlying user is removed from an organization.
- **FR-015**: System MUST generate unique, time-limited challenge messages for wallet address verification.

### Key Entities

- **ParticipantIdentity**: Represents a user's participant status within an organization. Links a UserIdentity (from Tenant Service) to an organization context with a display name and status. A user may have multiple ParticipantIdentity records (one per organization they participate in).

- **LinkedWalletAddress**: Represents a verified link between a ParticipantIdentity and a wallet address. Contains the wallet address, public key, verification timestamp, and status (active/revoked). Each address can only be linked to one participant across the platform.

- **WalletLinkChallenge**: Temporary record for wallet verification flow. Contains the participant ID, requested wallet address, challenge message, expiration timestamp, and completion status.

- **ParticipantRoleAssignment**: Links a ParticipantIdentity to a specific role within a blueprint. Contains the blueprint ID, role identifier, participant ID, and assignment timestamp.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Organization administrators can register a new participant in under 30 seconds.
- **SC-002**: Participants can complete wallet address linking (including signature verification) in under 60 seconds.
- **SC-003**: Participant search returns results in under 2 seconds for directories with up to 10,000 participants.
- **SC-004**: Blueprint designers can assign all required participants to a 5-role workflow in under 2 minutes.
- **SC-005**: 100% of wallet address links are cryptographically verified before activation.
- **SC-006**: All participant identity changes are recorded in the audit log with complete change details.
- **SC-007**: Zero unauthorized cross-organization participant data access (enforced through access control testing).
- **SC-008**: System supports participants with up to 10 linked wallet addresses without performance degradation.

## Assumptions

- Users must already exist in the Tenant Service before they can be registered as participants.
- Wallet addresses must already exist in the Wallet Service before they can be linked.
- The signature challenge uses a standard message format that all supported wallet types can sign.
- Participant display names default to the user's display name from Tenant Service but can be customized.
- Revoked wallet links are soft-deleted to maintain audit trail integrity.
- Challenge messages expire after 5 minutes if not completed.

## Dependencies

- **Tenant Service**: Provides user identity and organization membership data.
- **Wallet Service**: Provides wallet addresses and signature verification capabilities.
- **Blueprint Service**: Consumes participant role assignments for workflow execution.
- **Register Service**: Validates participant signatures during transaction recording.

## Out of Scope

- Federated participant identity across different Sorcha deployments.
- External identity provider (IdP) integration for participant verification.
- Automated participant role recommendations based on historical assignments.
- Participant reputation or trust scoring systems.
