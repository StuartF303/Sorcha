# Implementation Tasks: Sorcha MCP Server

**Feature Branch**: `001-mcp-server` | **Date**: 2026-01-29
**Plan**: [plan.md](plan.md) | **Spec**: [spec.md](spec.md)

## Task Overview

| Phase | Focus | Task Count | Status |
|-------|-------|------------|--------|
| 1 | Setup | 5 | ⬜ Pending |
| 2 | Foundational | 8 | ⬜ Pending |
| 3 | US1: Admin Health Monitoring (P1) | 4 | ⬜ Pending |
| 4 | US2: Designer Blueprint CRUD (P1) | 6 | ⬜ Pending |
| 5 | US3: Participant Actions (P1) | 5 | ⬜ Pending |
| 6 | US4: Admin Tenant/User Management (P2) | 4 | ⬜ Pending |
| 7 | US5: Designer Version Management (P2) | 4 | ⬜ Pending |
| 8 | US6: Participant History (P2) | 3 | ⬜ Pending |
| 9 | US7: Admin Logs & Metrics (P3) | 4 | ⬜ Pending |
| 10 | US8: Participant Wallet (P3) | 3 | ⬜ Pending |
| 11 | US9: MCP Resources (P3) | 6 | ⬜ Pending |
| 12 | Polish & Integration | 6 | ⬜ Pending |

**Total Tasks**: 58

---

## Phase 1: Setup

### 1.1 Create Project Structure
- [ ] Create `src/Apps/Sorcha.McpServer/Sorcha.McpServer.csproj` with .NET 10 target
- [ ] Add NuGet references: ModelContextProtocol (prerelease), Microsoft.Extensions.Hosting
- [ ] Add project references: Sorcha.ServiceClients, Sorcha.ServiceDefaults
- [ ] Add FluentValidation 11.10.0 and System.IdentityModel.Tokens.Jwt 8.3.0
- [ ] Create folder structure: Tools/{Admin,Designer,Participant}, Resources, Services, Infrastructure, Models/{ToolInputs,ToolOutputs}

**Acceptance**: Project builds with `dotnet build`

### 1.2 Create Test Project
- [ ] Create `tests/Sorcha.McpServer.Tests/Sorcha.McpServer.Tests.csproj`
- [ ] Add xUnit, FluentAssertions, Moq references
- [ ] Add project reference to Sorcha.McpServer
- [ ] Create folder structure: Tools/{Admin,Designer,Participant}, Resources, Services, Integration

**Acceptance**: Test project builds and runs empty test suite

### 1.3 Create Program.cs Entry Point
- [ ] Create `Program.cs` with MCP server host builder
- [ ] Configure stdio transport via `.WithStdioServerTransport()`
- [ ] Add tool auto-discovery via `.WithToolsFromAssembly()`
- [ ] Configure resource providers
- [ ] Add command-line argument parsing for `--jwt-token`

**Acceptance**: Server starts and responds to MCP `initialize` request

### 1.4 Create appsettings.json
- [ ] Create `appsettings.json` with ServiceClients configuration
- [ ] Add RateLimiting configuration section
- [ ] Add Logging configuration section
- [ ] Create `appsettings.Development.json` with local service addresses

**Acceptance**: Configuration loads and binds to strongly-typed options

### 1.5 Add Solution Integration
- [ ] Add Sorcha.McpServer.csproj to Sorcha.sln
- [ ] Add Sorcha.McpServer.Tests.csproj to Sorcha.sln
- [ ] Verify solution builds with `dotnet build Sorcha.sln`

**Acceptance**: Full solution builds without errors

---

## Phase 2: Foundational (Blocking Prerequisites)

### 2.1 Implement McpSessionService (FR-004, FR-005)
- [ ] Create `Services/McpSessionService.cs`
- [ ] Parse JWT token from initialization parameters
- [ ] Extract and cache claims: userId, tenantId, roles, walletAddress
- [ ] Implement `GetCurrentSession()` returning McpSession entity
- [ ] Implement `IsTokenExpired()` check
- [ ] Add unit tests for claim extraction and expiration

**Acceptance**: Session correctly extracts claims from valid JWT, rejects expired tokens

### 2.2 Implement JwtValidationHandler (FR-004)
- [ ] Create `Infrastructure/JwtValidationHandler.cs`
- [ ] Configure JWT validation parameters (issuer, audience, signing key)
- [ ] Integrate with Tenant Service for key validation
- [ ] Handle validation errors with clear messages
- [ ] Add unit tests for valid/invalid/expired tokens

**Acceptance**: Valid Sorcha JWTs are accepted, invalid tokens rejected with clear error

### 2.3 Implement McpAuthorizationService (FR-005, SC-006)
- [ ] Create `Services/McpAuthorizationService.cs`
- [ ] Define role-to-tool mappings per spec (admin: FR-010-019, designer: FR-020-032, participant: FR-033-042)
- [ ] Implement `CanInvokeTool(toolName, session)` returning bool
- [ ] Return authorization errors within 500ms (SC-006)
- [ ] Add unit tests for each role-tool combination

**Acceptance**: Tools correctly filtered by role, unauthorized access denied in <500ms

### 2.4 Implement RateLimitService (FR-007)
- [ ] Create `Services/RateLimitService.cs`
- [ ] Implement sliding window algorithm with `System.Threading.RateLimiting`
- [ ] Configure per-user (100/min), per-tenant (1000/min), admin-tools (50/min) limits
- [ ] Implement `IsRateLimited(key, category)` check
- [ ] Return rate limit errors with retry-after information
- [ ] Add unit tests for rate limit enforcement

**Acceptance**: Rate limits enforced correctly, clear error messages with wait time

### 2.5 Implement McpErrorHandler (FR-008)
- [ ] Create `Infrastructure/McpErrorHandler.cs`
- [ ] Define error categories: Authentication, Authorization, Validation, ServiceUnavailable, RateLimited
- [ ] Format errors as MCP-compatible responses (`isError: true`)
- [ ] Ensure internal details are not leaked
- [ ] Add unit tests for each error category

**Acceptance**: All error types produce clear, actionable messages without internal details

### 2.6 Implement GracefulDegradation (SC-009)
- [ ] Create `Infrastructure/GracefulDegradation.cs`
- [ ] Implement health check per backend service
- [ ] Track service availability state
- [ ] Allow tools to query which services are available
- [ ] Report partial availability in MCP status
- [ ] Add unit tests for degraded scenarios

**Acceptance**: When backend service unavailable, tools report clear error, other tools continue working

### 2.7 Implement ToolAuditService (FR-009)
- [ ] Create `Services/ToolAuditService.cs`
- [ ] Record: invocation ID, session ID, tool name, input hash, success, duration, timestamp
- [ ] Use structured logging for audit trail
- [ ] Integrate with OpenTelemetry for traces
- [ ] Add unit tests for audit record creation

**Acceptance**: All tool invocations logged with required fields

### 2.8 Configure ServiceClients Integration (FR-006)
- [ ] Register all service clients from Sorcha.ServiceClients
- [ ] Configure service addresses from appsettings
- [ ] Add JWT token forwarding to backend calls
- [ ] Add unit tests for client registration

**Acceptance**: Service clients successfully call backend services with auth

---

## Phase 3: US1 - Administrator Monitors Platform Health (P1)

### 3.1 Implement HealthCheckTool (FR-010)
- [ ] Create `Tools/Admin/HealthCheckTool.cs` with `[McpServerToolType]`
- [ ] Create `sorcha_health_check` tool with `[McpServerTool]`
- [ ] Query health of all 7 services: Blueprint, Register, Wallet, Tenant, Validator, Peer, API Gateway
- [ ] Return aggregate status: Healthy, Degraded, or Unhealthy
- [ ] Include per-service response times and error messages
- [ ] Add unit tests with mocked service clients
- [ ] Add integration test against running services

**Acceptance**: Health status returned for all services within 5 seconds (SC-002)

### 3.2 Implement PeerStatusTool (FR-015)
- [ ] Create `Tools/Admin/PeerStatusTool.cs`
- [ ] Create `sorcha_peer_status` tool
- [ ] Query peer network via IPeerServiceClient
- [ ] Return: connected peers, network topology, replication state
- [ ] Add unit tests and integration test

**Acceptance**: Peer network status accurately reflects running peers

### 3.3 Implement ValidatorStatusTool (FR-016)
- [ ] Create `Tools/Admin/ValidatorStatusTool.cs`
- [ ] Create `sorcha_validator_status` tool
- [ ] Query validator via IValidatorServiceClient
- [ ] Return: consensus status, chain height, integrity check
- [ ] Add unit tests and integration test

**Acceptance**: Validator status shows consensus state and chain integrity

### 3.4 Implement RegisterStatsTool (FR-017)
- [ ] Create `Tools/Admin/RegisterStatsTool.cs`
- [ ] Create `sorcha_register_stats` tool
- [ ] Query register service for statistics
- [ ] Return: register count, total transactions, storage usage
- [ ] Add unit tests and integration test

**Acceptance**: Register statistics returned accurately

---

## Phase 4: US2 - Designer Creates and Validates Blueprint (P1)

### 4.1 Implement BlueprintListTool (FR-020)
- [ ] Create `Tools/Designer/BlueprintListTool.cs`
- [ ] Create `sorcha_blueprint_list` tool
- [ ] Support filtering: status, title search, date range
- [ ] Support pagination: page, pageSize
- [ ] Return BlueprintSummary array
- [ ] Add unit tests with various filter combinations

**Acceptance**: Blueprints listed with correct filtering and pagination

### 4.2 Implement BlueprintGetTool (FR-021)
- [ ] Create `Tools/Designer/BlueprintGetTool.cs`
- [ ] Create `sorcha_blueprint_get` tool
- [ ] Retrieve full blueprint definition by ID
- [ ] Return complete JSON structure
- [ ] Add unit tests for existing/non-existing blueprints

**Acceptance**: Full blueprint definition returned for valid ID

### 4.3 Implement BlueprintCreateTool (FR-022)
- [ ] Create `Tools/Designer/BlueprintCreateTool.cs`
- [ ] Create `sorcha_blueprint_create` tool
- [ ] Accept JSON or YAML definition
- [ ] Parse and validate before saving
- [ ] Return created blueprint ID and validation result
- [ ] Add unit tests for valid/invalid definitions

**Acceptance**: Blueprint created from valid definition, errors for invalid

### 4.4 Implement BlueprintValidateTool (FR-024)
- [ ] Create `Tools/Designer/BlueprintValidateTool.cs`
- [ ] Create `sorcha_blueprint_validate` tool
- [ ] Validate syntax and semantic correctness
- [ ] Return detailed errors and warnings with JSON paths
- [ ] Complete within 3 seconds (SC-003)
- [ ] Add unit tests for various validation scenarios

**Acceptance**: Validation returns detailed results in <3 seconds for blueprints with up to 50 actions

### 4.5 Implement BlueprintSimulateTool (FR-025)
- [ ] Create `Tools/Designer/BlueprintSimulateTool.cs`
- [ ] Create `sorcha_blueprint_simulate` tool
- [ ] Execute dry-run with mock data
- [ ] Return expected flow, calculated values, disclosures
- [ ] Add unit tests for simulation scenarios

**Acceptance**: Simulation shows complete workflow execution path

### 4.6 Implement DisclosureAnalysisTool (FR-026)
- [ ] Create `Tools/Designer/DisclosureAnalysisTool.cs`
- [ ] Create `sorcha_disclosure_analyze` tool
- [ ] Analyze blueprint disclosure rules
- [ ] Return per-action, per-participant disclosure breakdown
- [ ] Show which fields each participant can see
- [ ] Add unit tests for complex disclosure scenarios

**Acceptance**: Analysis clearly shows data visibility per participant per action

---

## Phase 5: US3 - Participant Processes Pending Actions (P1)

### 5.1 Implement InboxListTool (FR-033)
- [ ] Create `Tools/Participant/InboxListTool.cs`
- [ ] Create `sorcha_inbox_list` tool
- [ ] Query pending actions for current user
- [ ] Return: action IDs, workflow names, sender info, due dates
- [ ] Support pagination
- [ ] Add unit tests for empty and populated inbox

**Acceptance**: Inbox correctly shows pending actions for authenticated participant

### 5.2 Implement ActionDetailsTool (FR-034)
- [ ] Create `Tools/Participant/ActionDetailsTool.cs`
- [ ] Create `sorcha_action_details` tool
- [ ] Retrieve specific pending action
- [ ] Return disclosed data based on user's permissions
- [ ] Return required response schema and instructions
- [ ] Add unit tests for accessible and inaccessible actions

**Acceptance**: Action details include disclosed data and response requirements

### 5.3 Implement ActionSubmitTool (FR-035)
- [ ] Create `Tools/Participant/ActionSubmitTool.cs`
- [ ] Create `sorcha_action_submit` tool
- [ ] Validate response data against schema
- [ ] Record action and advance workflow
- [ ] Return transaction ID and next step info
- [ ] Add unit tests for valid and invalid submissions

**Acceptance**: Valid submission creates transaction, invalid returns clear errors

### 5.4 Implement ActionValidateTool (FR-036)
- [ ] Create `Tools/Participant/ActionValidateTool.cs`
- [ ] Create `sorcha_action_validate` tool
- [ ] Validate without submitting (dry-run)
- [ ] Return same validation result format as submit
- [ ] Add unit tests for validation scenarios

**Acceptance**: Validation-only mode returns accurate results without side effects

### 5.5 Add Participant Integration Tests
- [ ] Create `Integration/ParticipantToolsIntegrationTests.cs`
- [ ] Test full flow: list inbox → view details → validate → submit
- [ ] Verify workflow advances correctly
- [ ] Verify disclosed data filtering
- [ ] Test SC-004: Complete flow under 30 seconds

**Acceptance**: Full participant flow completes successfully in integration test

---

## Phase 6: US4 - Administrator Manages Tenants and Users (P2)

### 6.1 Implement TenantManagementTools (FR-013)
- [ ] Create `Tools/Admin/TenantManagementTools.cs`
- [ ] Create `sorcha_tenant_list` tool
- [ ] Create `sorcha_tenant_create` tool
- [ ] Create `sorcha_tenant_update` tool (includes suspend)
- [ ] Add validation for tenant names and statuses
- [ ] Add unit tests for each operation

**Acceptance**: Tenants can be listed, created, and suspended

### 6.2 Implement UserManagementTools (FR-014)
- [ ] Create `Tools/Admin/UserManagementTools.cs`
- [ ] Create `sorcha_user_list` tool
- [ ] Create `sorcha_user_manage` tool (roles, permissions)
- [ ] Return users with their role assignments
- [ ] Add unit tests for user management

**Acceptance**: Users can be listed and their roles modified

### 6.3 Implement TokenRevocationTool (FR-019)
- [ ] Create `Tools/Admin/TokenRevocationTool.cs`
- [ ] Create `sorcha_token_revoke` tool
- [ ] Revoke all tokens for specified user
- [ ] Return confirmation of revocation
- [ ] Add unit tests for revocation flow

**Acceptance**: Token revocation invalidates all active tokens for user

### 6.4 Add Admin Tenant Integration Tests
- [ ] Create integration tests for tenant CRUD operations
- [ ] Test user management flows
- [ ] Test token revocation effectiveness

**Acceptance**: Admin tenant/user management works end-to-end

---

## Phase 7: US5 - Designer Manages Blueprint Versions (P2)

### 7.1 Implement BlueprintUpdateTool (FR-023)
- [ ] Create `Tools/Designer/BlueprintUpdateTool.cs`
- [ ] Create `sorcha_blueprint_update` tool
- [ ] Create new version on update (preserve history)
- [ ] Return new version number
- [ ] Add unit tests for version creation

**Acceptance**: Updates create new versions, preserving previous versions

### 7.2 Implement BlueprintDiffTool (FR-027)
- [ ] Create `Tools/Designer/BlueprintDiffTool.cs`
- [ ] Create `sorcha_blueprint_diff` tool
- [ ] Compare two blueprint versions
- [ ] Return: added, removed, modified elements
- [ ] Add unit tests for diff scenarios

**Acceptance**: Diff clearly shows changes between versions

### 7.3 Implement BlueprintExportTool (FR-028)
- [ ] Create `Tools/Designer/BlueprintExportTool.cs`
- [ ] Create `sorcha_blueprint_export` tool
- [ ] Support formats: JSON, YAML, Markdown documentation
- [ ] Add unit tests for each export format

**Acceptance**: Blueprint exported correctly in all three formats

### 7.4 Implement WorkflowInstancesTool (FR-032)
- [ ] Create `Tools/Designer/WorkflowInstancesTool.cs`
- [ ] Create `sorcha_workflow_instances` tool
- [ ] List active workflow instances of a blueprint
- [ ] Return instance IDs, statuses, participants
- [ ] Add unit tests for instance listing

**Acceptance**: Active workflow instances listed with correct status

---

## Phase 8: US6 - Participant Views Transaction History (P2)

### 8.1 Implement TransactionHistoryTool (FR-037)
- [ ] Create `Tools/Participant/TransactionHistoryTool.cs`
- [ ] Create `sorcha_transaction_history` tool
- [ ] Query user's past actions
- [ ] Return: timestamps, workflow names, action summaries
- [ ] Support date range filtering and pagination
- [ ] Add unit tests for history queries

**Acceptance**: Transaction history shows user's completed actions

### 8.2 Implement WorkflowStatusTool (FR-038)
- [ ] Create `Tools/Participant/WorkflowStatusTool.cs`
- [ ] Create `sorcha_workflow_status` tool
- [ ] Return: current state, pending participants, completed actions
- [ ] Only show workflows user participates in
- [ ] Add unit tests for status queries

**Acceptance**: Workflow status correctly shows current state and progress

### 8.3 Implement DisclosedDataTool (FR-039)
- [ ] Create `Tools/Participant/DisclosedDataTool.cs`
- [ ] Create `sorcha_disclosed_data` tool
- [ ] Retrieve data disclosed to user across workflows
- [ ] Filter by disclosure permissions
- [ ] Add unit tests for disclosure filtering

**Acceptance**: Only data user has permission to see is returned

---

## Phase 9: US7 - Administrator Queries Logs and Metrics (P3)

### 9.1 Implement LogQueryTool (FR-011)
- [ ] Create `Tools/Admin/LogQueryTool.cs`
- [ ] Create `sorcha_log_query` tool
- [ ] Support filters: service, level, time range, correlation ID
- [ ] Limit results (max 1000)
- [ ] Return chronological log entries
- [ ] Add unit tests for filter combinations

**Acceptance**: Logs queried with correct filtering

### 9.2 Implement MetricsTool (FR-012)
- [ ] Create `Tools/Admin/MetricsTool.cs`
- [ ] Create `sorcha_metrics` tool
- [ ] Return: latency, throughput, error rates
- [ ] Support time period selection
- [ ] Add unit tests for metrics aggregation

**Acceptance**: Performance metrics returned for specified period

### 9.3 Implement AuditLogTool (FR-018)
- [ ] Create `Tools/Admin/AuditLogTool.cs`
- [ ] Create `sorcha_audit_query` tool
- [ ] Query security-relevant events
- [ ] Return: auth attempts, permission changes, admin actions
- [ ] Add unit tests for audit queries

**Acceptance**: Audit log returns security events

### 9.4 Add Admin Observability Integration Tests
- [ ] Create integration tests for log queries
- [ ] Test metrics collection
- [ ] Test audit log accuracy

**Acceptance**: Admin observability tools return accurate data

---

## Phase 10: US8 - Participant Manages Wallet (P3)

### 10.1 Implement WalletInfoTool (FR-040)
- [ ] Create `Tools/Participant/WalletInfoTool.cs`
- [ ] Create `sorcha_wallet_info` tool
- [ ] Return: wallet address, algorithm, linked identities
- [ ] Query via IWalletServiceClient
- [ ] Add unit tests for wallet info retrieval

**Acceptance**: Wallet info returned for authenticated user

### 10.2 Implement WalletSignTool (FR-041)
- [ ] Create `Tools/Participant/WalletSignTool.cs`
- [ ] Create `sorcha_wallet_sign` tool
- [ ] Accept message in base64 or UTF-8
- [ ] Return signature, public key, algorithm
- [ ] Add unit tests for signing operations

**Acceptance**: Messages signed with user's wallet key

### 10.3 Implement RegisterQueryTool (FR-042)
- [ ] Create `Tools/Participant/RegisterQueryTool.cs`
- [ ] Create `sorcha_register_query` tool
- [ ] Query registers user has access to
- [ ] Filter by disclosure permissions
- [ ] Add unit tests for register queries

**Acceptance**: Register data returned filtered by permissions

---

## Phase 11: US9 - MCP Resources (P3)

### 11.1 Implement BlueprintsResource (FR-043)
- [ ] Create `Resources/BlueprintsResource.cs`
- [ ] Implement `sorcha://blueprints` resource
- [ ] Return list of accessible blueprints
- [ ] Filter by user permissions
- [ ] Add unit tests for resource access

**Acceptance**: Resource returns blueprint list for authorized users

### 11.2 Implement BlueprintResource (FR-044)
- [ ] Create `Resources/BlueprintResource.cs`
- [ ] Implement `sorcha://blueprints/{id}` resource
- [ ] Return full blueprint definition
- [ ] Enforce read permission
- [ ] Add unit tests for resource access

**Acceptance**: Resource returns blueprint if user has permission

### 11.3 Implement InboxResource (FR-045)
- [ ] Create `Resources/InboxResource.cs`
- [ ] Implement `sorcha://inbox` resource
- [ ] Return pending actions for participant
- [ ] Add unit tests for resource access

**Acceptance**: Resource returns user's pending actions

### 11.4 Implement WorkflowResource (FR-046)
- [ ] Create `Resources/WorkflowResource.cs`
- [ ] Implement `sorcha://workflows/{id}` resource
- [ ] Return workflow state if user participates
- [ ] Add unit tests for resource access

**Acceptance**: Resource returns workflow status for participants

### 11.5 Implement RegisterResource (FR-047)
- [ ] Create `Resources/RegisterResource.cs`
- [ ] Implement `sorcha://registers/{id}` resource
- [ ] Return register data filtered by disclosure
- [ ] Add unit tests for resource access

**Acceptance**: Resource returns register data respecting disclosures

### 11.6 Implement SchemaResource (FR-048)
- [ ] Create `Resources/SchemaResource.cs`
- [ ] Implement `sorcha://schemas/{name}` resource
- [ ] Return JSON Schema definitions
- [ ] Add unit tests for resource access

**Acceptance**: Resource returns schema definitions

---

## Phase 12: Polish & Integration

### 12.1 Implement Schema Tools (FR-029, FR-030, FR-031)
- [ ] Create `Tools/Designer/SchemaValidateTool.cs` - `sorcha_schema_validate`
- [ ] Create `Tools/Designer/SchemaGenerateTool.cs` - `sorcha_schema_generate`
- [ ] Create `Tools/Designer/JsonLogicTestTool.cs` - `sorcha_jsonlogic_test`
- [ ] Add unit tests for schema operations

**Acceptance**: Schema validation, generation, and JSON Logic testing work correctly

### 12.2 Add ToolMetrics Instrumentation
- [ ] Create `Infrastructure/ToolMetrics.cs`
- [ ] Add OpenTelemetry spans for all tool invocations
- [ ] Add counters: invocations, errors, rate limits
- [ ] Add histograms: latency distribution
- [ ] Verify metrics appear in Aspire dashboard

**Acceptance**: All tool invocations instrumented with traces and metrics

### 12.3 Add HTTP/SSE Transport Support (FR-003)
- [ ] Add `.WithHttpServerTransport()` configuration option
- [ ] Configure port from appsettings
- [ ] Add transport selection via command-line argument
- [ ] Add integration test for HTTP/SSE transport

**Acceptance**: Server can run in HTTP/SSE mode for remote access

### 12.4 Create Docker Configuration
- [ ] Create `Dockerfile` for MCP server
- [ ] Add to docker-compose.yml (optional service)
- [ ] Configure environment variables for settings
- [ ] Document Docker deployment

**Acceptance**: Server runs correctly in Docker container

### 12.5 Complete End-to-End Tests
- [ ] Create comprehensive integration test suite
- [ ] Test SC-001: Auth + tool invocation < 2 seconds
- [ ] Test SC-005: 95% success rate under load
- [ ] Test SC-007: All personas accomplish tasks via AI
- [ ] Test SC-008: Tool descriptions sufficient for discovery

**Acceptance**: All success criteria verified in automated tests

### 12.6 Documentation and README
- [ ] Create `src/Apps/Sorcha.McpServer/README.md`
- [ ] Document all tools with examples
- [ ] Document all resources with URI patterns
- [ ] Update quickstart.md with verified instructions
- [ ] Add troubleshooting section

**Acceptance**: README provides complete usage documentation

---

## Cross-Cutting Concerns

Applied throughout all phases:

- **Security**: JWT validation, role checks, no credential logging
- **Observability**: Structured logging, OpenTelemetry traces, metrics
- **Testing**: Unit tests per component, integration tests per user story
- **Error Handling**: MCP-compatible errors, actionable messages
- **Rate Limiting**: Applied to all tool invocations
- **Graceful Degradation**: Tools report backend unavailability clearly

---

## Definition of Done

Each task is complete when:

1. ✅ Implementation matches spec requirements
2. ✅ Unit tests pass with >85% coverage
3. ✅ Integration tests pass (where applicable)
4. ✅ No build warnings
5. ✅ Code follows Sorcha coding standards
6. ✅ XML documentation on public APIs
7. ✅ License header present

---

## Implementation Order

Recommended sequence respecting dependencies:

```
Phase 1 (Setup)
    ↓
Phase 2 (Foundational) ─── Blocks all tool development
    ↓
Phase 3-5 (P1 User Stories) ─── Can be parallelized
    ↓
Phase 6-8 (P2 User Stories) ─── Can be parallelized
    ↓
Phase 9-11 (P3 User Stories) ─── Can be parallelized
    ↓
Phase 12 (Polish)
```

**Critical Path**: Phase 1 → Phase 2 → Phase 3.1 (first working tool)
