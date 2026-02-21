# Tasks: Verifiable Credential Lifecycle & Presentations

**Input**: Design documents from `/specs/039-verifiable-presentations/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story. User stories are ordered by priority (P1 first) with dependency-driven reordering where needed (US5 before US3 because OID4VP requires DID resolution).

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add NuGet dependencies and create project scaffolding for the feature

- [x] T001 Add NuGet packages QRCoder and SimpleBase to `src/Apps/Sorcha.UI/Sorcha.UI.Core/Sorcha.UI.Core.csproj` for QR code generation and multicodec decoding
- [x] T002 [P] Create `src/Common/Sorcha.ServiceClients/Did/` directory and placeholder files for DID resolver interfaces
- [x] T003 [P] Create `src/Common/Sorcha.Blueprint.Models/Credentials/UsagePolicy.cs` enum (Reusable=0, SingleUse=1, LimitedUse=2) per data-model.md
- [x] T004 [P] Create `src/Common/Sorcha.Blueprint.Models/Credentials/CredentialStatusClaim.cs` model with Id, Type, StatusPurpose, StatusListIndex, StatusListCredential fields per data-model.md
- [x] T005 [P] Create `src/Common/Sorcha.Blueprint.Models/Credentials/CredentialDisplayConfig.cs` model with BackgroundColor, TextColor, Icon, CardLayout, HighlightClaims fields per data-model.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Extend core credential models and entity that ALL user stories depend on

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T006 Extend `src/Common/Sorcha.Wallet.Core/Domain/Entities/CredentialEntity.cs` — add UsagePolicy (string, default "Reusable"), MaxPresentations (int?), PresentationCount (int, default 0), StatusListUrl (string?), StatusListIndex (int?), DisplayConfigJson (string?) fields per data-model.md
- [x] T007 Extend `src/Common/Sorcha.Blueprint.Models/Credentials/CredentialIssuanceConfig.cs` — add UsagePolicy (UsagePolicy enum, default Reusable), MaxPresentations (int?), DisplayConfig (CredentialDisplayConfig?) properties per data-model.md
- [x] T008 Create `src/Common/Sorcha.ServiceClients/Did/DidDocument.cs` — W3C DID Core model with Id, VerificationMethod[], Authentication[], AssertionMethod[], Service[] per contracts/did-resolver.md
- [x] T009 Create `src/Common/Sorcha.ServiceClients/Did/IDidResolver.cs` — interface with `ResolveAsync(did, ct) → DidDocument?` and `CanResolve(didMethod) → bool` per contracts/did-resolver.md
- [x] T010 [P] Create `src/Common/Sorcha.ServiceClients/Did/IDidResolverRegistry.cs` — interface with `ResolveAsync(did, ct) → DidDocument?` and `Register(resolver) → void` per contracts/did-resolver.md
- [x] T011 Create `src/Common/Sorcha.ServiceClients/Did/DidResolverRegistry.cs` — implementation that parses DID method, delegates to registered resolver, returns null with warning if no resolver found
- [x] T012 [P] Create `tests/Sorcha.Blueprint.Models.Tests/Credentials/UsagePolicyTests.cs` — test enum values, JSON serialization round-trip for all three policies
- [x] T013 [P] Create `tests/Sorcha.Blueprint.Models.Tests/Credentials/CredentialDisplayConfigTests.cs` — test default values, JSON serialization, highlight claims dictionary

**Checkpoint**: Foundation ready — credential entity extended, DID interfaces defined, shared models in place. User story implementation can begin.

---

## Phase 3: User Story 1 — Credential Lifecycle Management (Priority: P1) MVP

**Goal**: Enable five credential lifecycle states (Active, Suspended, Revoked, Expired, Consumed) with usage policies and issuer/governance lifecycle operations.

**Independent Test**: Issue a credential via blueprint action, cycle through Active → Suspended → Reinstated → Revoked, verify status at each stage. Create a SingleUse credential, present it, verify it transitions to Consumed.

### Implementation for User Story 1

- [x] T014 [US1] Extend `src/Services/Sorcha.Wallet.Service/Credentials/CredentialStore.cs` — add support for Suspended and Consumed status values in status transition logic, add `UpdateStatusAsync` method with state machine validation per data-model.md state transitions
- [x] T015 [US1] Extend `src/Services/Sorcha.Wallet.Service/Endpoints/CredentialEndpoints.cs` — modify PATCH `/{credentialId}/status` endpoint to support new status values (Active, Suspended, Revoked, Consumed), add authorization check for original issuer wallet or governance roles per contracts/credential-lifecycle-endpoints.md
- [x] T016 [US1] Add suspend endpoint `POST /api/v1/credentials/{credentialId}/suspend` in `src/Services/Sorcha.Blueprint.Service/Endpoints/CredentialEndpoints.cs` — accepts issuerWallet + reason, validates Active state, updates wallet credential status per contracts/credential-lifecycle-endpoints.md
- [x] T017 [US1] Add reinstate endpoint `POST /api/v1/credentials/{credentialId}/reinstate` in `src/Services/Sorcha.Blueprint.Service/Endpoints/CredentialEndpoints.cs` — accepts issuerWallet + reason, validates Suspended state, restores to Active per contracts/credential-lifecycle-endpoints.md
- [x] T018 [US1] Extend revoke endpoint `POST /api/v1/credentials/{credentialId}/revoke` in `src/Services/Sorcha.Blueprint.Service/Endpoints/CredentialEndpoints.cs` — add reason field, return statusListUpdated flag per contracts/credential-lifecycle-endpoints.md
- [x] T019 [US1] Add refresh endpoint `POST /api/v1/credentials/{credentialId}/refresh` in `src/Services/Sorcha.Blueprint.Service/Endpoints/CredentialEndpoints.cs` — validates Expired state, consumes old credential, issues new with fresh expiry per contracts/credential-lifecycle-endpoints.md
- [x] T020 [US1] Implement usage policy enforcement in `src/Services/Sorcha.Wallet.Service/Credentials/CredentialStore.cs` — on successful presentation: increment PresentationCount, transition to Consumed if SingleUse or LimitedUse exhausted
- [x] T021 [US1] Implement automatic expiry check in `src/Services/Sorcha.Wallet.Service/Credentials/CredentialStore.cs` — when fetching credentials, check ExpiresAt and transition Active → Expired if past
- [x] T022 [P] [US1] Create `tests/Sorcha.Wallet.Service.Tests/Credentials/CredentialLifecycleTests.cs` — test all state transitions: Active→Suspended, Suspended→Active, Active→Revoked, Suspended→Revoked, Active→Expired, Active→Consumed; test invalid transitions return 400; test usage policy consumption
- [x] T023 [P] [US1] Create `tests/Sorcha.Blueprint.Models.Tests/Credentials/CredentialStatusClaimTests.cs` — test CredentialStatusClaim serialization, BitstringStatusListEntry type constant, statusPurpose values

**Checkpoint**: Credentials support full lifecycle with five states, usage policies, and issuer lifecycle endpoints. Can be tested independently by issuing and cycling credential states.

---

## Phase 4: User Story 2 — Bitstring Status List (Priority: P1) MVP

**Goal**: Implement W3C Bitstring Status List v1.0 for privacy-preserving revocation and suspension tracking, with register storage and cached HTTP endpoint.

**Independent Test**: Allocate a credential in a status list, flip its revocation bit, fetch the status list via cached endpoint, verify the bit is set. Verify compressed list size <20KB.

### Implementation for User Story 2

- [x] T024 [P] [US2] Create `src/Common/Sorcha.Blueprint.Models/Credentials/BitstringStatusList.cs` — model with Id, IssuerWallet, RegisterId, Purpose, EncodedList, Size (>=131072), NextAvailableIndex, Version, LastUpdated, RegisterTxId per data-model.md
- [x] T025 [US2] Create `src/Services/Sorcha.Blueprint.Service/Services/StatusListManager.cs` — implement bitstring creation (131072 entries, GZip+Base64), index allocation, bit set/clear, version increment, register Control TX storage via ITransactionBuilderService
- [x] T026 [US2] Create `src/Services/Sorcha.Blueprint.Service/Endpoints/StatusListEndpoints.cs` — implement GET `/api/v1/credentials/status-lists/{listId}` (public, Cache-Control: max-age=300), POST `/{listId}/allocate` (internal), PUT `/{listId}/bits/{index}` (internal) per contracts/status-list-endpoints.md
- [x] T027 [US2] Wire status list allocation into credential issuance in `src/Core/Sorcha.Blueprint.Engine/Credentials/CredentialIssuer.cs` — on credential issuance, allocate index from StatusListManager, embed credentialStatus claim in VC
- [x] T028 [US2] Wire status list bit updates into lifecycle operations in `src/Services/Sorcha.Blueprint.Service/Endpoints/CredentialEndpoints.cs` — when suspend/reinstate/revoke, update corresponding bitstring via StatusListManager
- [x] T029 [US2] Register StatusListManager in DI and add Redis cache for status list responses in `src/Services/Sorcha.Blueprint.Service/Program.cs`
- [x] T030 [P] [US2] Create `tests/Sorcha.Blueprint.Models.Tests/Credentials/BitstringStatusListTests.cs` — test model validation (minimum size, purpose values), EncodedList GZip+Base64 round-trip
- [x] T031 [P] [US2] Create `tests/Sorcha.Blueprint.Service.Tests/StatusList/StatusListManagerTests.cs` — test index allocation, bit set/clear, version increment, capacity overflow (409 response), list creation
- [x] T032 [P] [US2] Create `tests/Sorcha.Blueprint.Service.Tests/StatusList/StatusListEndpointTests.cs` — test public GET (unauthenticated), allocate (service auth), bits update (service auth), cache headers, W3C-compliant response format

**Checkpoint**: Status lists are created, allocated, and served via cached endpoint. Lifecycle operations update the bitstring. Can be tested independently by allocating and flipping bits.

---

## Phase 5: User Story 5 — Multi-Method DID Resolution (Priority: P2)

**Goal**: Pluggable DID resolver with `did:sorcha`, `did:web`, `did:key` implementations for cross-system credential verification.

**Independent Test**: Resolve each DID method independently — `did:sorcha:w:` against wallet service mock, `did:web:` against mock HTTPS endpoint, `did:key:` by multicodec decoding — verify each returns valid DidDocument.

**Note**: This story is ordered before US3/US4/US8 because they all depend on DID resolution for signature verification.

### Implementation for User Story 5

- [x] T033 [P] [US5] Create `src/Common/Sorcha.ServiceClients/Did/SorchaDidResolver.cs` — resolve `did:sorcha:w:{address}` by querying wallet service for public key, resolve `did:sorcha:r:{registerId}:t:{txId}` by fetching register TX, return DidDocument with verification method
- [x] T034 [P] [US5] Create `src/Common/Sorcha.ServiceClients/Did/WebDidResolver.cs` — resolve `did:web:{domain}` by HTTPS GET to `https://{domain}/.well-known/did.json`, support path-based `did:web:{domain}:{path}`, enforce HTTPS-only, 5-second timeout per contracts/did-resolver.md
- [x] T035 [P] [US5] Create `src/Common/Sorcha.ServiceClients/Did/KeyDidResolver.cs` — resolve `did:key:z6Mk...` by decoding multibase string, extracting multicodec-encoded public key (ED25519 + P-256), return DidDocument without network call per contracts/did-resolver.md
- [x] T036 [US5] Add `AddDidResolvers()` DI extension in `src/Common/Sorcha.ServiceClients/Extensions/ServiceCollectionExtensions.cs` — register IDidResolverRegistry + all 3 resolver implementations per contracts/did-resolver.md
- [x] T037 [P] [US5] Create `tests/Sorcha.ServiceClients.Tests/Did/SorchaDidResolverTests.cs` — test wallet DID resolution, register DID resolution, null for unknown address, CanResolve("sorcha") returns true
- [x] T038 [P] [US5] Create `tests/Sorcha.ServiceClients.Tests/Did/WebDidResolverTests.cs` — test domain resolution, path-based resolution, HTTPS enforcement, 5-second timeout, invalid TLS error, CanResolve("web") returns true
- [x] T039 [P] [US5] Create `tests/Sorcha.ServiceClients.Tests/Did/KeyDidResolverTests.cs` — test ED25519 key decoding, P-256 key decoding, invalid multibase error, CanResolve("key") returns true, no network call
- [x] T040 [P] [US5] Create `tests/Sorcha.ServiceClients.Tests/Did/DidResolverRegistryTests.cs` — test method delegation, unsupported method returns null with warning, register multiple resolvers

**Checkpoint**: DID resolution works for all three methods. Can be tested independently with mocked wallet/register/HTTP services.

---

## Phase 6: User Story 3 — OID4VP Credential Presentation (Priority: P2)

**Goal**: Enable holders to present credentials to verifiers using OID4VP protocol with selective disclosure, signature verification via DID resolution, and status list checking.

**Depends on**: US5 (DID Resolution), US2 (Bitstring Status List)

**Independent Test**: Create presentation request, match to credential, generate SD-JWT presentation with selective disclosure, verify end-to-end including status list check and nonce validation.

### Implementation for User Story 3

- [x] T041 [US3] Create `src/Services/Sorcha.Wallet.Service/Models/PresentationRequest.cs` — model with Id, VerifierIdentity, CredentialType, AcceptedIssuers, RequiredClaims, Nonce (32 hex), CallbackUrl (HTTPS), TargetWalletAddress, Status (Pending/Submitted/Verified/Denied/Expired), VpToken, VerificationResult per data-model.md
- [x] T042 [US3] Create `src/Services/Sorcha.Wallet.Service/Services/PresentationRequestService.cs` — implement request creation (nonce generation, TTL), credential matching (type + issuer + claims), request storage, expiry management
- [x] T043 [US3] Implement presentation verification in `src/Services/Sorcha.Wallet.Service/Services/PresentationRequestService.cs` — verify SD-JWT signature via IDidResolverRegistry, check status list via HTTP fetch, validate required claims, verify nonce freshness
- [x] T044 [US3] Create `src/Services/Sorcha.Wallet.Service/Endpoints/PresentationEndpoints.cs` — implement POST `/api/v1/presentations/request` (create request, return requestId + nonce + requestUrl + qrCodeUrl) per contracts/presentation-endpoints.md
- [x] T045 [US3] Add GET `/api/v1/presentations/{requestId}` in `src/Services/Sorcha.Wallet.Service/Endpoints/PresentationEndpoints.cs` — return request details with matching credentials and disclosable claims per contracts/presentation-endpoints.md
- [x] T046 [US3] Add POST `/api/v1/presentations/{requestId}/submit` in `src/Services/Sorcha.Wallet.Service/Endpoints/PresentationEndpoints.cs` — accept credentialId + disclosedClaims + vpToken, run verification, return Verified or Denied result per contracts/presentation-endpoints.md
- [x] T047 [US3] Add POST `/api/v1/presentations/{requestId}/deny` and GET `/api/v1/presentations/{requestId}/result` in `src/Services/Sorcha.Wallet.Service/Endpoints/PresentationEndpoints.cs` — deny returns Denied status, result returns current state (200/202/404/410) per contracts/presentation-endpoints.md
- [x] T048 [US3] Wire presentation to usage policy — on successful Verified result, call CredentialStore to increment PresentationCount and check SingleUse/LimitedUse consumption
- [x] T049 [US3] Register PresentationRequestService in DI in `src/Services/Sorcha.Wallet.Service/Program.cs` and register IDidResolverRegistry via AddDidResolvers()
- [x] T050 [P] [US3] Create `tests/Sorcha.Wallet.Service.Tests/Presentations/PresentationEndpointTests.cs` — test request creation (nonce generation, TTL), credential matching, submission with valid/invalid VP token, deny flow, result polling, expired request (410), replay rejection (wrong nonce)

**Checkpoint**: Full OID4VP presentation flow works — request creation, credential matching, selective disclosure submission, verification with DID + status list + nonce. Can be tested independently.

---

## Phase 7: User Story 4 — QR Code In-Person Presentation (Priority: P2)

**Goal**: Enable in-person credential presentation via QR code scanning — terminal displays QR, holder scans, wallet completes OID4VP flow over HTTPS.

**Depends on**: US3 (OID4VP Presentations)

**Independent Test**: Generate QR code containing presentation request URL, simulate scan, complete OID4VP flow, verify terminal receives result. Test expired QR rejection.

### Implementation for User Story 4

- [x] T051 [US4] Create `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/QrPresentationService.cs` — generate QR code containing `openid4vp://authorize?request_uri={url}&nonce={nonce}` using QRCoder, support SVG and PNG output formats
- [x] T052 [US4] Create `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Credentials/QrPresentationDisplay.razor` — MudCard displaying QR code for verifier terminal, auto-polls GET `/api/v1/presentations/{requestId}/result` every 2 seconds, shows success/failure when result arrives
- [x] T053 [US4] Add QR URL generation to presentation request response in `src/Services/Sorcha.Wallet.Service/Endpoints/PresentationEndpoints.cs` — include qrCodeUrl field in POST request response per contracts/presentation-endpoints.md
- [x] T054 [P] [US4] Create `tests/Sorcha.UI.Core.Tests/Credentials/QrPresentationServiceTests.cs` — test QR generation with valid URL, nonce embedding, expired request handling, SVG/PNG output

**Checkpoint**: QR code in-person flow works end-to-end. Terminal displays QR, holder can scan and complete presentation via HTTPS.

---

## Phase 8: User Story 8 — Cross-Blueprint Credential Flows (Priority: P2)

**Goal**: Enable credentials issued by one blueprint to be required by another, with status list verification across registers.

**Depends on**: US2 (Bitstring Status List), US5 (DID Resolution)

**Independent Test**: Configure two blueprint templates — one issues a credential, another requires it — run both, verify credential flows across and revocation in issuing blueprint blocks the requiring blueprint.

### Implementation for User Story 8

- [x] T055 [US8] Create `src/Core/Sorcha.Blueprint.Engine/Credentials/BitstringStatusListChecker.cs` — implement IRevocationChecker that fetches status list from issuer's register endpoint, decodes GZip+Base64 bitstring, checks bit at credential's index for revocation and suspension
- [x] T056 [US8] Extend `src/Core/Sorcha.Blueprint.Engine/Credentials/CredentialVerifier.cs` — integrate BitstringStatusListChecker for status checks, add DID-based signature verification via IDidResolverRegistry, add nonce validation, add usage policy check
- [x] T057 [US8] Extend `src/Core/Sorcha.Blueprint.Engine/Credentials/CredentialIssuer.cs` — add display config embedding, usage policy setting, status list allocation on issuance per plan.md
- [x] T058 [US8] Wire IDidResolverRegistry and BitstringStatusListChecker in Blueprint Engine DI registration in `src/Core/Sorcha.Blueprint.Engine/` configuration
- [x] T059 [P] [US8] Create `tests/Sorcha.Blueprint.Engine.Tests/Credentials/StatusListCheckerTests.cs` — test bitstring decoding, bit check at specific index, revoked credential detection, suspended credential detection, unreachable status list with FailClosed/FailOpen policies
- [x] T060 [P] [US8] Create `tests/Sorcha.Blueprint.Engine.Tests/Credentials/UsagePolicyVerificationTests.cs` — test Reusable (unlimited), SingleUse (one then rejected), LimitedUse (N then rejected), cross-register credential verification

**Checkpoint**: Cross-blueprint credential flows work — credentials issued in one workflow are verified in another, with status list checks against the issuing register.

---

## Phase 9: User Story 6 — Wallet Credential Card UI (Priority: P3)

**Goal**: Visual card-based display of credentials in the wallet UI with issuer-defined styling, status indicators, and credential actions.

**Depends on**: US1 (Credential Lifecycle for status states)

**Independent Test**: Load wallet with credentials in all five states, render card list, verify visual treatment and action availability for each state.

### Implementation for User Story 6

- [x] T061 [P] [US6] Create `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Credentials/CredentialCardViewModel.cs` — view model with CredentialId, Type, IssuerName, Status, ExpiresAt, HighlightClaims, DisplayConfig (colors/icon/layout), AvailableActions list
- [x] T062 [P] [US6] Create `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/ICredentialApiService.cs` — interface with GetCredentialsAsync, GetCredentialDetailAsync, UpdateCredentialStatusAsync, DeleteCredentialAsync methods
- [x] T063 [US6] Create `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/CredentialApiService.cs` — HttpClient implementation calling Wallet Service endpoints, maps responses to CredentialCardViewModel
- [x] T064 [US6] Create `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Credentials/CredentialCard.razor` — MudCard with issuer-defined background/text color, type icon, key claims, status chip (green=Active, amber=Suspended, red=Revoked, grey=Expired, strikethrough=Consumed), expiry countdown for within-30-days
- [x] T065 [US6] Create `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Credentials/CredentialCardList.razor` — MudGrid of CredentialCard components with status filter (MudChipSet), search by type/issuer, empty state message
- [x] T066 [US6] Create `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Credentials/CredentialDetailView.razor` — MudDialog showing all claims with disclosure indicators, metadata (issuer, issued date, expiry, usage policy, presentation count), action buttons: Present (disabled if not Active), Export, Delete, Renew (if expired and refreshable)
- [x] T067 [US6] Create `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/MyCredentials.razor` — page at `/credentials` with CredentialCardList, wired to ICredentialApiService, add navigation link in MainLayout.razor
- [x] T068 [P] [US6] Create `tests/Sorcha.UI.Core.Tests/Credentials/CredentialCardTests.cs` — test card rendering for all five states, status chip colors, action button availability per state, default display config generation
- [x] T069 [P] [US6] Create `tests/Sorcha.UI.Core.Tests/Credentials/CredentialApiServiceTests.cs` — test HTTP client calls, response mapping to view models, error handling

**Checkpoint**: Wallet UI displays credentials as visual cards with appropriate styling and actions. All five states render correctly.

---

## Phase 10: User Story 7 — Presentation Request Inbox (Priority: P3)

**Goal**: Notification and approval/denial UI for incoming presentation requests in the wallet.

**Depends on**: US3 (OID4VP Presentations), US6 (Wallet Card UI for credential display)

**Independent Test**: Create presentation request targeting a wallet, verify notification appears, approve/deny and confirm correct result.

### Implementation for User Story 7

- [x] T070 [P] [US7] Create `src/Apps/Sorcha.UI/Sorcha.UI.Core/Models/Credentials/PresentationRequestViewModel.cs` — view model with RequestId, VerifierIdentity, CredentialType, RequestedClaims, MatchingCredentials (list with disclosable claims), ExpiresAt, Status
- [x] T071 [US7] Add GetPresentationRequestsAsync and GetPresentationRequestDetailAsync methods to `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/ICredentialApiService.cs` and implementation in `src/Apps/Sorcha.UI/Sorcha.UI.Core/Services/CredentialApiService.cs`
- [x] T072 [US7] Create `src/Apps/Sorcha.UI/Sorcha.UI.Core/Components/Credentials/PresentationRequestDialog.razor` — MudDialog showing verifier identity, requested credential type, required claims list, matching credential selector (if multiple), claim disclosure checkboxes (requested pre-checked, optional selectable), Approve and Deny buttons
- [x] T073 [US7] Add presentation inbox section to `src/Apps/Sorcha.UI/Sorcha.UI.Web.Client/Pages/MyCredentials.razor` — MudBadge notification count on tab, list of pending requests with verifier name and expiry countdown, tap to open PresentationRequestDialog
- [x] T074 [US7] Wire Approve action in PresentationRequestDialog to POST `/api/v1/presentations/{requestId}/submit` — create SD-JWT presentation with selected disclosures via ISdJwtService, submit vp_token, show Verified/Denied result
- [x] T075 [US7] Wire Deny action in PresentationRequestDialog to POST `/api/v1/presentations/{requestId}/deny` — show confirmation, send denial, update inbox

**Checkpoint**: Holders can see, review, approve, or deny presentation requests from within the wallet UI. Matching credentials and disclosure scope are clearly shown.

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Integration, documentation, and cross-cutting improvements across all stories

- [x] T076 Add YARP routes for presentation endpoints in `src/Services/Sorcha.ApiGateway/` configuration — route `/api/v1/presentations/*` to wallet service, `/api/v1/credentials/status-lists/*` to blueprint service
- [x] T077 [P] Add structured logging for credential lifecycle operations (issuance, suspend, reinstate, revoke, consume) in Wallet Service and Blueprint Service using ILogger with operation-specific log templates
- [x] T078 [P] Add ActivitySource tracing for DID resolution in `src/Common/Sorcha.ServiceClients/Did/DidResolverRegistry.cs` — trace each resolution attempt with method, duration, success/failure
- [x] T079 [P] Add OpenAPI/Scalar documentation with `.WithName()`, `.WithSummary()`, `.WithDescription()` for all new endpoints in PresentationEndpoints.cs, StatusListEndpoints.cs, and modified CredentialEndpoints.cs
- [x] T080 [P] Add health check for status list cache availability in Blueprint Service `src/Services/Sorcha.Blueprint.Service/Program.cs`
- [x] T081 Update `docs/development-status.md` and `.specify/MASTER-TASKS.md` with feature completion status
- [x] T082 Run quickstart.md validation — build solution, run all test projects listed in quickstart.md, verify zero new test failures beyond pre-existing baselines

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion — BLOCKS all user stories
- **US1 Lifecycle (Phase 3)**: Depends on Phase 2 — no story dependencies
- **US2 Status List (Phase 4)**: Depends on Phase 2 — no story dependencies
- **US5 DID Resolution (Phase 5)**: Depends on Phase 2 — no story dependencies
- **US3 OID4VP (Phase 6)**: Depends on US5 (DID resolution for verification) and US2 (status list checking)
- **US4 QR Code (Phase 7)**: Depends on US3 (OID4VP protocol)
- **US8 Cross-Blueprint (Phase 8)**: Depends on US2 (status list) and US5 (DID resolution)
- **US6 Card UI (Phase 9)**: Depends on US1 (lifecycle states for display)
- **US7 Inbox (Phase 10)**: Depends on US3 (presentation protocol) and US6 (credential display)
- **Polish (Phase 11)**: Depends on all user stories being complete

### User Story Dependencies

```
Phase 2 (Foundation)
  ├── US1 (Lifecycle) ──────────────── US6 (Card UI) ────┐
  ├── US2 (Status List) ──┬── US3 (OID4VP) ── US4 (QR) ──├── US7 (Inbox)
  └── US5 (DID Resolution) ┘       │                      │
                            └── US8 (Cross-Blueprint) ────┘
```

### Within Each User Story

- Models before services
- Services before endpoints
- Core implementation before integration
- Tests alongside implementation (same phase)
- Story complete before dependent stories can begin

### Parallel Opportunities

**After Phase 2 completes, these can run in parallel:**
- US1 (Lifecycle), US2 (Status List), and US5 (DID Resolution) are fully independent

**After US2 + US5 complete:**
- US3 (OID4VP) and US8 (Cross-Blueprint) can run in parallel

**After US1 completes:**
- US6 (Card UI) can start (independent of US2/US3/US5)

---

## Parallel Example: Phase 2 Foundation

```bash
# Launch all parallel foundation tasks together:
Task: T003 "Create UsagePolicy.cs enum"
Task: T004 "Create CredentialStatusClaim.cs model"
Task: T005 "Create CredentialDisplayConfig.cs model"
Task: T010 "Create IDidResolverRegistry.cs interface"
Task: T012 "Test UsagePolicy enum"
Task: T013 "Test CredentialDisplayConfig model"
```

## Parallel Example: After Phase 2 — Three Independent Stories

```bash
# Three stories can start simultaneously:
Story US1: T014-T023 (Credential Lifecycle)
Story US2: T024-T032 (Bitstring Status List)
Story US5: T033-T040 (DID Resolution)
```

---

## Implementation Strategy

### MVP First (US1 + US2)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: US1 (Lifecycle) — credentials have states
4. Complete Phase 4: US2 (Status List) — states are verifiable
5. **STOP and VALIDATE**: Issue credential, cycle states, check status list
6. This is the minimum viable trust model

### Incremental Delivery

1. Setup + Foundation → Core models ready
2. US1 + US2 → Credential lifecycle + verifiable status (MVP!)
3. US5 → DID resolution unlocks external interoperability
4. US3 → OID4VP presentations unlock credential exchange
5. US4 + US8 → Physical-world and cross-blueprint flows
6. US6 + US7 → User-facing wallet experience
7. Polish → Documentation, observability, hardening

### Parallel Team Strategy

With multiple developers after Phase 2:

- **Developer A**: US1 (Lifecycle) → US6 (Card UI)
- **Developer B**: US2 (Status List) → US8 (Cross-Blueprint)
- **Developer C**: US5 (DID Resolution) → US3 (OID4VP) → US4 (QR) → US7 (Inbox)

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable after its dependencies
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Pre-existing test baselines from MEMORY.md: Engine 323 pass, Blueprint Service 300 pass, Wallet 251 pass, UI Core 517 pass
