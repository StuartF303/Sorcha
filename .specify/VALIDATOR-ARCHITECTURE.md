# Validator Service Architecture Design

**Task:** VAL-9.1
**Created:** 2026-01-26
**Status:** Draft
**Purpose:** Technical architecture for decentralized Validator Service with leader election and dual-role consensus

---

## Executive Summary

This document defines the technical architecture for rebuilding `Sorcha.Validator.Service` as a decentralized consensus participant. The design builds upon the existing implementation while adding:

1. **Leader Election** - Single rotating leader initiates docket builds
2. **Dual-Role Architecture** - Leader/Initiator + Confirmer roles
3. **Genesis Blueprint Integration** - Configuration from on-chain governance
4. **Consensus Failure Handling** - Abandon and retry mechanism
5. **Control Blueprint Versioning** - On-chain governance updates

---

## Current State Analysis

### Existing Components (To Retain/Refactor)

| Component | Lines | Status | Changes Needed |
|-----------|-------|--------|----------------|
| `ConsensusEngine` | 570 | Implemented | Refactor for leader-based model |
| `DocketBuilder` | 210 | Implemented | Add leader check, genesis config |
| `MemPoolManager` | 362 | Implemented | Add consensus failure return |
| `ValidatorOrchestrator` | N/A | Implemented | Add leader election integration |
| `ValidatorGrpcService` | N/A | Implemented | Add confirmer endpoints |

### Missing Components (To Create)

| Component | Purpose |
|-----------|---------|
| `Sorcha.Validator.Core` | Pure validation logic library |
| `LeaderElectionService` | Leader election mechanism |
| `GenesisConfigService` | Genesis blueprint configuration |
| `DocketConfirmer` | Confirmer role implementation |
| `ControlBlueprintProcessor` | Control docket handling |

---

## Architecture Overview

### Component Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           SORCHA.VALIDATOR.SERVICE                               │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                  │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                         API LAYER                                        │   │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────┐  │   │
│  │  │  REST Endpoints │  │ gRPC Services   │  │  Admin Endpoints        │  │   │
│  │  │  /transactions  │  │ RequestVote     │  │  /validators/start      │  │   │
│  │  │  /mempool       │  │ ConfirmDocket   │  │  /validators/status     │  │   │
│  │  │  /dockets       │  │ Heartbeat       │  │  /leader/status         │  │   │
│  │  └─────────────────┘  └─────────────────┘  └─────────────────────────┘  │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                         │                                        │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                      ORCHESTRATION LAYER                                 │   │
│  │                                                                          │   │
│  │  ┌────────────────────────────────────────────────────────────────────┐ │   │
│  │  │                   ValidatorOrchestrator                             │ │   │
│  │  │  - Coordinates pipeline based on role (Leader vs Confirmer)        │ │   │
│  │  │  - Manages lifecycle and metrics                                   │ │   │
│  │  └────────────────────────────────────────────────────────────────────┘ │   │
│  │                              │                                           │   │
│  │              ┌───────────────┼───────────────┐                          │   │
│  │              ▼               ▼               ▼                          │   │
│  │  ┌──────────────────┐ ┌─────────────┐ ┌──────────────────┐             │   │
│  │  │ LeaderElection   │ │ RoleManager │ │ GenesisConfig    │             │   │
│  │  │ Service          │ │             │ │ Service          │             │   │
│  │  │ - Rotating       │ │ - IsLeader  │ │ - Thresholds     │             │   │
│  │  │ - Heartbeat      │ │ - Role      │ │ - Timeouts       │             │   │
│  │  │ - Failover       │ │   switch    │ │ - Validators     │             │   │
│  │  └──────────────────┘ └─────────────┘ └──────────────────┘             │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                         │                                        │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                        SERVICE LAYER                                     │   │
│  │                                                                          │   │
│  │  ┌─────────────────────────────┐  ┌─────────────────────────────────┐   │   │
│  │  │      LEADER ROLE            │  │      CONFIRMER ROLE             │   │   │
│  │  │  ┌───────────────────────┐  │  │  ┌───────────────────────────┐  │   │   │
│  │  │  │ DocketBuilder         │  │  │  │ DocketConfirmer           │  │   │   │
│  │  │  │ - Build from mempool  │  │  │  │ - Validate all txns       │  │   │   │
│  │  │  │ - Merkle tree         │  │  │  │ - Verify Merkle root      │  │   │   │
│  │  │  │ - Sign as initiator   │  │  │  │ - Verify initiator sig    │  │   │   │
│  │  │  └───────────────────────┘  │  │  │ - Sign and return         │  │   │   │
│  │  │  ┌───────────────────────┐  │  │  └───────────────────────────┘  │   │   │
│  │  │  │ ConsensusEngine       │  │  │  ┌───────────────────────────┐  │   │   │
│  │  │  │ - Broadcast docket    │  │  │  │ BadActorDetector          │  │   │   │
│  │  │  │ - Collect signatures  │  │  │  │ - Log rejections          │  │   │   │
│  │  │  │ - Threshold check     │  │  │  │ - Track patterns          │  │   │   │
│  │  │  └───────────────────────┘  │  │  └───────────────────────────┘  │   │   │
│  │  │  ┌───────────────────────┐  │  │                                  │   │   │
│  │  │  │ SignatureCollector    │  │  │                                  │   │   │
│  │  │  │ - Await responses     │  │  │                                  │   │   │
│  │  │  │ - Handle timeout      │  │  │                                  │   │   │
│  │  │  │ - Abandon/retry       │  │  │                                  │   │   │
│  │  │  └───────────────────────┘  │  │                                  │   │   │
│  │  └─────────────────────────────┘  └─────────────────────────────────┘   │   │
│  │                                                                          │   │
│  │  ┌─────────────────────────────────────────────────────────────────┐    │   │
│  │  │                    SHARED SERVICES                               │    │   │
│  │  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────────────────┐ │    │   │
│  │  │  │ MemPool      │ │ Validation   │ │ ControlBlueprint         │ │    │   │
│  │  │  │ Manager      │ │ Engine       │ │ Processor                │ │    │   │
│  │  │  └──────────────┘ └──────────────┘ └──────────────────────────┘ │    │   │
│  │  └─────────────────────────────────────────────────────────────────┘    │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                         │                                        │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                      VALIDATION LAYER (Sorcha.Validator.Core)           │   │
│  │  ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────────────┐ │   │
│  │  │ ITransaction     │ │ IDocket          │ │ IChain                   │ │   │
│  │  │ Validator        │ │ Validator        │ │ Validator                │ │   │
│  │  │ - Schema         │ │ - Structure      │ │ - PreviousId             │ │   │
│  │  │ - Signatures     │ │ - Merkle root    │ │ - Blueprint version      │ │   │
│  │  │ - JsonLogic      │ │ - Proposer sig   │ │ - Instance chain         │ │   │
│  │  └──────────────────┘ └──────────────────┘ └──────────────────────────┘ │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                         │                                        │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                         CACHE LAYER                                      │   │
│  │  ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────────────┐ │   │
│  │  │ Blueprint Cache  │ │ Genesis Config   │ │ Validator Registry       │ │   │
│  │  │ (Redis)          │ │ Cache (Redis)    │ │ Cache (Redis)            │ │   │
│  │  └──────────────────┘ └──────────────────┘ └──────────────────────────┘ │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
│                                         │                                        │
│  ┌─────────────────────────────────────────────────────────────────────────┐   │
│  │                      STORAGE LAYER                                       │   │
│  │  ┌──────────────────┐ ┌──────────────────┐ ┌──────────────────────────┐ │   │
│  │  │ Unverified Pool  │ │ Verified Queue   │ │ Pending Dockets          │ │   │
│  │  │ (Redis)          │ │ (In-Memory)      │ │ (In-Memory)              │ │   │
│  │  └──────────────────┘ └──────────────────┘ └──────────────────────────┘ │   │
│  └─────────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────────┘
                                          │
          ┌───────────────────────────────┼───────────────────────────────┐
          ▼                               ▼                               ▼
   ┌─────────────┐                ┌─────────────┐                ┌─────────────┐
   │    Peer     │                │  Register   │                │  Blueprint  │
   │   Service   │                │   Service   │                │   Service   │
   │   (gRPC)    │                │   (HTTP)    │                │   (HTTP)    │
   └─────────────┘                └─────────────┘                └─────────────┘
```

---

## Core Interfaces

### 1. Leader Election

```csharp
// Location: Sorcha.Validator.Service/Services/Interfaces/ILeaderElectionService.cs

public interface ILeaderElectionService
{
    /// <summary>Current leader validator ID</summary>
    string? CurrentLeaderId { get; }

    /// <summary>Whether this validator is the current leader</summary>
    bool IsLeader { get; }

    /// <summary>Current election term number</summary>
    long CurrentTerm { get; }

    /// <summary>Event raised when leadership changes</summary>
    event EventHandler<LeaderChangedEventArgs>? LeaderChanged;

    /// <summary>Start participating in leader election</summary>
    Task StartAsync(string registerId, CancellationToken ct);

    /// <summary>Stop participating in leader election</summary>
    Task StopAsync(CancellationToken ct);

    /// <summary>Send heartbeat (leader only)</summary>
    Task SendHeartbeatAsync(CancellationToken ct);

    /// <summary>Process heartbeat from leader (followers)</summary>
    Task ProcessHeartbeatAsync(string leaderId, long term, CancellationToken ct);

    /// <summary>Trigger new election (on leader failure)</summary>
    Task TriggerElectionAsync(CancellationToken ct);
}

public class LeaderChangedEventArgs : EventArgs
{
    public required string? PreviousLeaderId { get; init; }
    public required string? NewLeaderId { get; init; }
    public required long Term { get; init; }
    public required LeaderChangeReason Reason { get; init; }
}

public enum LeaderChangeReason
{
    InitialElection,
    TermExpired,
    LeaderTimeout,
    LeaderResigned,
    HigherTermReceived
}
```

### 2. Genesis Configuration

```csharp
// Location: Sorcha.Validator.Service/Services/Interfaces/IGenesisConfigService.cs

public interface IGenesisConfigService
{
    /// <summary>Get consensus configuration for register</summary>
    Task<ConsensusConfig> GetConsensusConfigAsync(string registerId);

    /// <summary>Get validator configuration for register</summary>
    Task<ValidatorConfig> GetValidatorConfigAsync(string registerId);

    /// <summary>Get leader election configuration for register</summary>
    Task<LeaderElectionConfig> GetLeaderElectionConfigAsync(string registerId);

    /// <summary>Check if configuration needs refresh</summary>
    Task<bool> IsConfigStaleAsync(string registerId);

    /// <summary>Force refresh configuration from genesis</summary>
    Task RefreshConfigAsync(string registerId);

    /// <summary>Event raised when configuration changes</summary>
    event EventHandler<ConfigChangedEventArgs>? ConfigChanged;
}

public record ConsensusConfig
{
    public required int SignatureThresholdMin { get; init; }
    public required int SignatureThresholdMax { get; init; }
    public required TimeSpan DocketTimeout { get; init; }
    public required int MaxSignaturesPerDocket { get; init; }
    public required int MaxTransactionsPerDocket { get; init; }
    public required TimeSpan DocketBuildInterval { get; init; }
}

public record ValidatorConfig
{
    public required string RegistrationMode { get; init; } // "public" or "consent"
    public required int MinValidators { get; init; }
    public required int MaxValidators { get; init; }
    public required bool RequireStake { get; init; }
}

public record LeaderElectionConfig
{
    public required string Mechanism { get; init; } // "rotating", "raft"
    public required TimeSpan HeartbeatInterval { get; init; }
    public required TimeSpan LeaderTimeout { get; init; }
}
```

### 3. Docket Confirmer (New)

```csharp
// Location: Sorcha.Validator.Service/Services/Interfaces/IDocketConfirmer.cs

public interface IDocketConfirmer
{
    /// <summary>Validate and sign a docket received from another validator</summary>
    Task<ConfirmationResult> ConfirmDocketAsync(
        Docket docket,
        CancellationToken ct);
}

public record ConfirmationResult
{
    public required bool Confirmed { get; init; }
    public Signature? Signature { get; init; }
    public RejectionReason? RejectionReason { get; init; }
    public string? RejectionDetails { get; init; }
    public required TimeSpan ValidationDuration { get; init; }
}

public enum RejectionReason
{
    InvalidInitiatorSignature,
    InvalidMerkleRoot,
    InvalidTransaction,
    ChainValidationFailed,
    BlueprintNotFound,
    UnauthorizedInitiator,
    DocketStructureInvalid,
    Timeout
}
```

### 4. Validator Registry

```csharp
// Location: Sorcha.Validator.Service/Services/Interfaces/IValidatorRegistry.cs

public interface IValidatorRegistry
{
    /// <summary>Get all active validators for a register</summary>
    Task<IReadOnlyList<ValidatorInfo>> GetActiveValidatorsAsync(string registerId);

    /// <summary>Check if a validator is registered and active</summary>
    Task<bool> IsRegisteredAsync(string registerId, string validatorId);

    /// <summary>Get validator info by ID</summary>
    Task<ValidatorInfo?> GetValidatorAsync(string registerId, string validatorId);

    /// <summary>Register this validator (public mode)</summary>
    Task<RegistrationResult> RegisterAsync(string registerId, ValidatorRegistration registration);

    /// <summary>Get validators in order for rotating election</summary>
    Task<IReadOnlyList<string>> GetValidatorOrderAsync(string registerId);

    /// <summary>Refresh validator list from chain</summary>
    Task RefreshAsync(string registerId);
}

public record ValidatorInfo
{
    public required string ValidatorId { get; init; }
    public required string PublicKey { get; init; }
    public required string GrpcEndpoint { get; init; }
    public required ValidatorStatus Status { get; init; }
    public required DateTimeOffset RegisteredAt { get; init; }
    public int? OrderIndex { get; init; } // For rotating election
}

public enum ValidatorStatus
{
    Pending,
    Active,
    Suspended,
    Removed
}
```

### 5. Signature Collector (Refactored)

```csharp
// Location: Sorcha.Validator.Service/Services/Interfaces/ISignatureCollector.cs

public interface ISignatureCollector
{
    /// <summary>Collect signatures from confirming validators</summary>
    Task<SignatureCollectionResult> CollectSignaturesAsync(
        Docket docket,
        ConsensusConfig config,
        IReadOnlyList<ValidatorInfo> validators,
        CancellationToken ct);
}

public record SignatureCollectionResult
{
    public required IReadOnlyList<ValidatorSignature> Signatures { get; init; }
    public required bool ThresholdMet { get; init; }
    public required bool TimedOut { get; init; }
    public required int TotalValidators { get; init; }
    public required int ResponsesReceived { get; init; }
    public required int Approvals { get; init; }
    public required int Rejections { get; init; }
    public required TimeSpan Duration { get; init; }
}

public record ValidatorSignature
{
    public required string ValidatorId { get; init; }
    public required Signature Signature { get; init; }
    public required DateTimeOffset SignedAt { get; init; }
    public required bool IsInitiator { get; init; }
}
```

---

## Data Flow

### Leader Role Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           LEADER VALIDATOR FLOW                              │
└─────────────────────────────────────────────────────────────────────────────┘

1. HEARTBEAT LOOP (Background)
   ┌────────────────────────────────────────┐
   │ while (IsLeader && !cancelled)         │
   │ {                                      │
   │   await SendHeartbeatAsync();          │
   │   await Task.Delay(HeartbeatInterval); │
   │ }                                      │
   └────────────────────────────────────────┘

2. DOCKET BUILD LOOP (Background)
   ┌─────────────────────────────────────────────────────────────────┐
   │ while (IsLeader && !cancelled)                                  │
   │ {                                                               │
   │   if (ShouldBuildDocket())                                     │
   │   {                                                             │
   │     // Get config from genesis                                 │
   │     var config = await _genesisConfig.GetConsensusConfigAsync();│
   │                                                                 │
   │     // Build docket from mempool                               │
   │     var docket = await _docketBuilder.BuildDocketAsync();      │
   │                                                                 │
   │     // Sign as initiator                                       │
   │     await _signer.SignDocketAsync(docket);                     │
   │                                                                 │
   │     // Broadcast and collect signatures                        │
   │     var result = await _signatureCollector.CollectAsync(docket);│
   │                                                                 │
   │     if (result.ThresholdMet)                                   │
   │     {                                                           │
   │       // Commit to register                                    │
   │       await _registerClient.SubmitDocketAsync(docket);         │
   │       // Distribute to peers                                   │
   │       await _peerClient.DistributeDocketAsync(docket);         │
   │       // Cleanup mempool                                       │
   │       await _mempool.RemoveTransactionsAsync(docket.TxIds);    │
   │     }                                                           │
   │     else                                                        │
   │     {                                                           │
   │       // ABANDON: Return to mempool                            │
   │       await _mempool.ReturnTransactionsAsync(docket.TxIds);    │
   │       _metrics.DocketsAbandoned.Inc();                         │
   │     }                                                           │
   │   }                                                             │
   │   await Task.Delay(DocketBuildInterval);                       │
   │ }                                                               │
   └─────────────────────────────────────────────────────────────────┘
```

### Confirmer Role Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         CONFIRMER VALIDATOR FLOW                             │
└─────────────────────────────────────────────────────────────────────────────┘

1. HEARTBEAT MONITORING (Background)
   ┌────────────────────────────────────────────────────────────────┐
   │ while (!IsLeader && !cancelled)                                │
   │ {                                                              │
   │   var elapsed = DateTime.UtcNow - LastHeartbeatReceived;      │
   │   if (elapsed > LeaderTimeout)                                │
   │   {                                                            │
   │     // Leader failed - trigger election                       │
   │     await _leaderElection.TriggerElectionAsync();             │
   │   }                                                            │
   │   await Task.Delay(HeartbeatCheckInterval);                   │
   │ }                                                              │
   └────────────────────────────────────────────────────────────────┘

2. DOCKET CONFIRMATION (gRPC Handler)
   ┌────────────────────────────────────────────────────────────────┐
   │ public async Task<VoteResponse> ConfirmDocket(DocketRequest)  │
   │ {                                                              │
   │   // 1. Verify initiator signature                            │
   │   if (!await VerifyInitiatorSignature(docket))                │
   │     return Reject(InvalidInitiatorSignature);                 │
   │                                                                │
   │   // 2. Validate all transactions                             │
   │   foreach (var tx in docket.Transactions)                     │
   │   {                                                            │
   │     var result = await _validator.ValidateAsync(tx);          │
   │     if (!result.IsValid)                                      │
   │     {                                                          │
   │       _badActorDetector.Log(docket.InitiatorId, result);      │
   │       return Reject(InvalidTransaction, result.Errors);       │
   │     }                                                          │
   │   }                                                            │
   │                                                                │
   │   // 3. Verify Merkle root                                    │
   │   var computedRoot = MerkleTree.ComputeRoot(docket.TxIds);    │
   │   if (computedRoot != docket.MerkleRoot)                      │
   │     return Reject(InvalidMerkleRoot);                         │
   │                                                                │
   │   // 4. Sign and return                                       │
   │   var signature = await _signer.SignAsync(docket.Hash);       │
   │   return Approve(signature);                                   │
   │ }                                                              │
   └────────────────────────────────────────────────────────────────┘
```

### Leader Election Flow (Rotating)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      ROTATING LEADER ELECTION                                │
└─────────────────────────────────────────────────────────────────────────────┘

Validators: [V1, V2, V3, V4, V5] (ordered by registration time)

Term 0: V1 is leader
        V1 sends heartbeats every 5s
        V2-V5 confirm dockets from V1

Term 1: (V1 term expires or fails)
        V2 becomes leader (next in rotation)
        V2 sends heartbeats
        V1, V3-V5 confirm dockets from V2

Term 2: V3 becomes leader
        ... and so on (wraps around)

┌────────────────────────────────────────────────────────────────────────────┐
│ Leader Determination:                                                       │
│   leaderIndex = currentTerm % validatorCount                               │
│   leaderId = orderedValidators[leaderIndex]                                │
│                                                                             │
│ Term Progression:                                                           │
│   - Time-based: term increments every TermDuration (configurable)          │
│   - Failure-based: term increments on leader timeout                       │
│                                                                             │
│ Heartbeat:                                                                  │
│   - Leader sends heartbeat every HeartbeatInterval (5s default)            │
│   - Contains: leaderId, term, timestamp                                    │
│   - Followers reset timeout on valid heartbeat                             │
│                                                                             │
│ Failure Detection:                                                          │
│   - Follower tracks LastHeartbeatReceived                                  │
│   - If elapsed > LeaderTimeout (15s default): trigger election             │
│   - New leader = nextValidator(currentLeader) in rotation                  │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## gRPC Service Extensions

### New Proto Definitions

```protobuf
// Location: Sorcha.Validator.Service/Protos/validator_consensus.proto

syntax = "proto3";

package sorcha.validator;

option csharp_namespace = "Sorcha.Validator.Service.Grpc";

// Extended Validator Service for consensus
service ValidatorConsensus {
  // Existing
  rpc RequestVote (VoteRequest) returns (VoteResponse);

  // New: Confirm docket (confirmer role)
  rpc ConfirmDocket (DocketConfirmationRequest) returns (DocketConfirmationResponse);

  // New: Leader heartbeat
  rpc Heartbeat (HeartbeatRequest) returns (HeartbeatResponse);

  // New: Get validator status
  rpc GetStatus (StatusRequest) returns (StatusResponse);
}

message DocketConfirmationRequest {
  string docket_id = 1;
  string register_id = 2;
  bytes docket_data = 3;  // Serialized Docket
  string initiator_id = 4;
  bytes initiator_signature = 5;
  int64 term = 6;
}

message DocketConfirmationResponse {
  bool confirmed = 1;
  bytes signature = 2;
  string rejection_reason = 3;
  string rejection_details = 4;
  int64 validation_duration_ms = 5;
}

message HeartbeatRequest {
  string leader_id = 1;
  int64 term = 2;
  int64 timestamp = 3;
  int64 latest_docket_number = 4;
}

message HeartbeatResponse {
  bool acknowledged = 1;
  string validator_id = 2;
  int64 timestamp = 3;
}

message StatusRequest {
  string register_id = 1;
}

message StatusResponse {
  string validator_id = 1;
  bool is_leader = 2;
  string current_leader_id = 3;
  int64 current_term = 4;
  int64 latest_docket_number = 5;
  int32 mempool_size = 6;
  int32 pending_dockets = 7;
}
```

---

## Project Structure

```
src/
├── Common/
│   └── Sorcha.Validator.Core/                    # NEW: Pure validation library
│       ├── Validators/
│       │   ├── ITransactionValidator.cs
│       │   ├── TransactionValidator.cs
│       │   ├── IDocketValidator.cs
│       │   ├── DocketValidator.cs
│       │   ├── IChainValidator.cs
│       │   └── ChainValidator.cs
│       ├── Models/
│       │   ├── ValidationResult.cs
│       │   ├── ValidationError.cs
│       │   └── ValidationErrorType.cs
│       └── Sorcha.Validator.Core.csproj
│
└── Services/
    └── Sorcha.Validator.Service/
        ├── Configuration/
        │   ├── ValidatorConfiguration.cs          # Existing
        │   ├── ConsensusConfiguration.cs          # Existing (refactor)
        │   ├── MemPoolConfiguration.cs            # Existing
        │   ├── DocketBuildConfiguration.cs        # Existing
        │   └── LeaderElectionConfiguration.cs     # NEW
        │
        ├── Services/
        │   ├── Interfaces/
        │   │   ├── ILeaderElectionService.cs      # NEW
        │   │   ├── IGenesisConfigService.cs       # NEW
        │   │   ├── IDocketConfirmer.cs            # NEW
        │   │   ├── IValidatorRegistry.cs          # NEW
        │   │   ├── ISignatureCollector.cs         # NEW
        │   │   ├── IBadActorDetector.cs           # NEW
        │   │   ├── IConsensusEngine.cs            # Existing
        │   │   ├── IDocketBuilder.cs              # Existing
        │   │   ├── IMemPoolManager.cs             # Existing
        │   │   └── IValidatorOrchestrator.cs      # Existing
        │   │
        │   ├── LeaderElection/
        │   │   ├── RotatingLeaderElectionService.cs  # NEW
        │   │   └── LeaderHeartbeatService.cs         # NEW (background)
        │   │
        │   ├── Genesis/
        │   │   ├── GenesisConfigService.cs           # NEW
        │   │   └── ControlBlueprintProcessor.cs      # NEW
        │   │
        │   ├── Consensus/
        │   │   ├── ConsensusEngine.cs                # Existing (refactor)
        │   │   ├── SignatureCollector.cs             # NEW (extract from ConsensusEngine)
        │   │   └── DocketConfirmer.cs                # NEW
        │   │
        │   ├── Registry/
        │   │   ├── ValidatorRegistry.cs              # NEW
        │   │   └── ValidatorRegistryCache.cs         # NEW
        │   │
        │   ├── Detection/
        │   │   └── BadActorDetector.cs               # NEW
        │   │
        │   ├── DocketBuilder.cs                      # Existing (refactor)
        │   ├── MemPoolManager.cs                     # Existing (add return logic)
        │   ├── ValidatorOrchestrator.cs              # Existing (refactor for roles)
        │   └── ...
        │
        ├── GrpcServices/
        │   ├── ValidatorGrpcService.cs               # Existing (extend)
        │   └── ValidatorConsensusService.cs          # NEW
        │
        ├── Endpoints/
        │   ├── ValidationEndpoints.cs                # Existing
        │   ├── AdminEndpoints.cs                     # Existing
        │   └── LeaderEndpoints.cs                    # NEW
        │
        ├── Protos/
        │   ├── validator.proto                       # Existing
        │   └── validator_consensus.proto             # NEW
        │
        └── Program.cs                                # Update DI registration
```

---

## Configuration

### appsettings.json

```json
{
  "Validator": {
    "ValidatorId": "validator-001",
    "PrivateKeyPath": "/secrets/validator.key",
    "Roles": ["initiator", "confirmer"],
    "RegisterIds": ["register-001"]
  },

  "LeaderElection": {
    "Mechanism": "rotating",
    "HeartbeatIntervalSeconds": 5,
    "LeaderTimeoutSeconds": 15,
    "TermDurationSeconds": 300
  },

  "Consensus": {
    "DefaultApprovalThreshold": 0.51,
    "DefaultVoteTimeoutSeconds": 30,
    "MaxRetries": 3,
    "UseGenesisConfig": true
  },

  "MemPool": {
    "MaxSize": 1000,
    "HighPriorityQuota": 0.2,
    "ExpirationCheckIntervalSeconds": 60
  },

  "DocketBuild": {
    "TimeThresholdSeconds": 10,
    "SizeThreshold": 50,
    "MaxTransactionsPerDocket": 100
  },

  "Cache": {
    "BlueprintTtlSeconds": 300,
    "GenesisConfigTtlSeconds": 600,
    "ValidatorRegistryTtlSeconds": 60
  }
}
```

---

## Dependency Injection

```csharp
// Program.cs additions

// Leader Election
builder.Services.AddSingleton<ILeaderElectionService, RotatingLeaderElectionService>();
builder.Services.AddHostedService<LeaderHeartbeatService>();

// Genesis Configuration
builder.Services.AddScoped<IGenesisConfigService, GenesisConfigService>();
builder.Services.AddScoped<ControlBlueprintProcessor>();

// Consensus (refactored)
builder.Services.AddScoped<ISignatureCollector, SignatureCollector>();
builder.Services.AddScoped<IDocketConfirmer, DocketConfirmer>();
builder.Services.AddScoped<IConsensusEngine, ConsensusEngine>();

// Validator Registry
builder.Services.AddSingleton<IValidatorRegistry, ValidatorRegistry>();
builder.Services.AddSingleton<ValidatorRegistryCache>();

// Bad Actor Detection
builder.Services.AddSingleton<IBadActorDetector, BadActorDetector>();

// Core Validators (from Sorcha.Validator.Core)
builder.Services.AddScoped<ITransactionValidator, TransactionValidator>();
builder.Services.AddScoped<IDocketValidator, DocketValidator>();
builder.Services.AddScoped<IChainValidator, ChainValidator>();

// gRPC Services
builder.Services.AddGrpc();
```

---

## Metrics

```csharp
// Prometheus metrics to add

// Leader Election
validator_is_leader (gauge) - 1 if leader, 0 if follower
validator_current_term (gauge) - Current election term
validator_leader_elections_total (counter) - Total elections participated
validator_heartbeats_sent_total (counter) - Heartbeats sent (leader)
validator_heartbeats_received_total (counter) - Heartbeats received (follower)
validator_leader_timeouts_total (counter) - Leader timeout detections

// Consensus
validator_dockets_built_total (counter) - Dockets built
validator_dockets_committed_total (counter) - Dockets committed
validator_dockets_abandoned_total (counter) - Dockets abandoned (threshold not met)
validator_signatures_collected_total (counter) - Signatures collected
validator_signature_collection_duration_seconds (histogram) - Collection time
validator_confirmations_processed_total (counter) - Confirmations processed (confirmer)
validator_confirmations_approved_total (counter) - Approved confirmations
validator_confirmations_rejected_total (counter) - Rejected confirmations

// Validation
validator_transactions_validated_total (counter) - Transactions validated
validator_validation_errors_total (counter) - Validation errors by type
validator_validation_duration_seconds (histogram) - Validation time
```

---

## Implementation Order

### Phase 1: Core Infrastructure (Sprint 9A)
1. Create `Sorcha.Validator.Core` project
2. Implement `ITransactionValidator`, `IDocketValidator`, `IChainValidator`
3. Implement `GenesisConfigService`
4. Implement `ValidatorRegistry`

### Phase 2: Leader Election (Sprint 9C)
1. Implement `ILeaderElectionService` interface
2. Implement `RotatingLeaderElectionService`
3. Implement `LeaderHeartbeatService`
4. Add leader status endpoints

### Phase 3: Consensus Refactor (Sprint 9C-9D)
1. Extract `SignatureCollector` from `ConsensusEngine`
2. Implement `DocketConfirmer`
3. Add consensus failure handling (abandon/retry)
4. Update `ConsensusEngine` for leader-based model

### Phase 4: Service Integration (Sprint 9E)
1. Extend Peer Service gRPC for docket broadcast
2. Extend Peer Service gRPC for signature exchange
3. Implement confirmed docket distribution

### Phase 5: Testing (Sprint 9G)
1. Unit tests for all new components
2. Integration tests for leader election
3. Integration tests for multi-validator consensus
4. Performance testing

---

## Open Design Questions

1. **Term Duration**: Should term duration be time-based or docket-count-based?
   - **Recommendation**: Time-based (configurable in genesis)

2. **Partial Signatures**: If we get more than threshold but less than max before timeout, commit early?
   - **Recommendation**: Yes, commit as soon as threshold met

3. **Validator Suspension**: How does a suspended validator re-join?
   - **Recommendation**: Must wait for control docket approval

4. **Concurrent Dockets**: Can a new docket build start before previous commits?
   - **Recommendation**: No, sequential docket building only

---

## Related Documents

- [VALIDATOR-SERVICE-REQUIREMENTS.md](VALIDATOR-SERVICE-REQUIREMENTS.md)
- [GENESIS-BLUEPRINT-SPEC.md](GENESIS-BLUEPRINT-SPEC.md)
- [MASTER-TASKS.md](MASTER-TASKS.md) - Sprint 9 tasks

---

**Created:** 2026-01-26
**Author:** Claude (VAL-9.1)
**Status:** Ready for Review
