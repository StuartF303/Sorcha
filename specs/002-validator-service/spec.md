# Feature Specification: Validator Service - Distributed Transaction Validation and Consensus

**Feature Branch**: `002-validator-service`
**Created**: 2025-12-22
**Status**: Draft
**Input**: User description: "We need 1 more service the Validator Service, this service receives unvalidated transactions within a subscribed register from the peer service and adds them to the registers memory pool - a cached temporary store. The purpose of the validator service is to check the transaction abides by the rules of that register, if they do it creates a proposed next docket for the chain. This proposed docket is distributed via the peer network to other validator services for checking. Once the originating validator receives confirmation ( signatures of blocks signed by other validators ) greater than 50% of Validators Services serving that Register then it can confirm by writing to the local Register Service and distribute the new next docket to the peer service for distribution. SO only the Validator service can write new docket to the register Service. When a new docket is received by the peer service it must send it to the validator service for double checking before it can be written to the register. The validator service can use the same blueprint validation library as is used else where to validate the transaction. The docket can be built using some/any/all transactions sitting in the memory pool for the given register. All registers are their own validation context. The validator service will need access to its own Wallet, which is a system wallet for the whole installation."

## Clarifications

### Session 2025-12-22

- Q: When should the validator build a new docket from the memory pool? → A: Hybrid - Build docket when either time threshold OR size threshold is reached (whichever comes first)
- Q: When two validators create competing dockets for the same register at the same docket number (fork scenario), how should validators resolve which chain to follow? → A: Longest chain wins - Follow the chain with the most subsequent dockets (most work). Note: Rolling master approach considered for future enhancement.
- Q: When a proposed docket fails to achieve consensus (insufficient validator approvals), what should happen to the transactions that were included in the rejected docket? → A: Return to memory pool - Put transactions back in memory pool for inclusion in next docket
- Q: When the memory pool reaches capacity, which transactions should be evicted to make room for new ones? → A: FIFO with priority override - Normal FIFO processing, but high-priority transactions can bypass queue
- Q: How do validator instances discover and connect to other validators in the network? → A: Peer Service managed - Peer Service maintains and provides the list of active validators with reputation scores; validators with poor behavior (too many bad transactions) suffer reduced reputation and connectivity

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Transaction Submission and Validation (Priority: P1)

A system administrator or automated process submits a transaction representing a blueprint action execution to the validator service. The validator service receives the transaction, validates it against the blueprint rules, and adds it to the appropriate register's memory pool if valid.

**Why this priority**: This is the foundation of the entire validation system. Without the ability to receive, validate, and queue transactions, no other functionality can work.

**Independent Test**: Can be fully tested by submitting a valid transaction via the Peer Service, checking that it appears in the memory pool with validated status, and verifying invalid transactions are rejected with appropriate error messages.

**Acceptance Scenarios**:

1. **Given** a valid transaction for a known register, **When** the transaction is received from the Peer Service, **Then** the validator validates it against blueprint rules and adds it to the register's memory pool
2. **Given** a transaction with an invalid signature, **When** the transaction is received, **Then** the validator rejects it and returns a validation error
3. **Given** a transaction for an unknown blueprint, **When** the transaction is received, **Then** the validator rejects it with a "blueprint not found" error
4. **Given** a transaction that violates blueprint schema rules, **When** the transaction is received, **Then** the validator rejects it with specific schema validation errors
5. **Given** a memory pool that is full for a register, **When** a new transaction arrives, **Then** the validator evicts the oldest transaction (FIFO) unless the new transaction is high-priority, in which case it can bypass the queue and evict older low-priority transactions

---

### User Story 2 - Docket Proposal Creation (Priority: P1)

The validator service builds a proposed docket (block) from transactions in a register's memory pool using a hybrid trigger: either when a time threshold is reached OR when the memory pool reaches a configured size threshold (whichever occurs first). It selects one or more valid transactions, creates a docket structure linking to the previous docket, and prepares it for consensus voting.

**Why this priority**: This is the core consensus mechanism - without docket creation, transactions remain queued indefinitely and the distributed ledger cannot advance.

**Independent Test**: Can be tested by adding multiple valid transactions to the memory pool, triggering docket creation, and verifying the resulting docket contains the expected transactions, correct linkage to previous docket, and proper cryptographic signatures.

**Acceptance Scenarios**:

1. **Given** multiple valid transactions in a register's memory pool, **When** docket creation is triggered, **Then** a proposed docket is created containing selected transactions with correct previous hash linkage
2. **Given** an empty memory pool for a register, **When** docket creation is triggered, **Then** no docket is created and the system logs this condition
3. **Given** transactions with different priorities in the memory pool, **When** docket creation is triggered, **Then** higher priority transactions are selected first up to docket size limits
4. **Given** a register with no previous dockets (genesis state), **When** the first docket is created, **Then** it is marked as a genesis docket with a null previous hash
5. **Given** a proposed docket is created, **When** building the docket, **Then** the validator signs it with the system wallet
6. **Given** memory pool has 20 transactions and time threshold is 10 seconds, **When** 10 seconds elapse before reaching size threshold, **Then** docket is built with the 20 transactions
7. **Given** memory pool reaches size threshold of 50 transactions, **When** this occurs before time threshold expires, **Then** docket is built immediately with those 50 transactions

---

### User Story 3 - Distributed Consensus Achievement (Priority: P1)

When a validator creates a proposed docket, it distributes the docket to other validator services in the network for voting. Each validator independently validates the docket and signs a vote. Once greater than 50% of validators approve, consensus is achieved and the docket is confirmed.

**Why this priority**: This is the distributed trust mechanism that ensures no single validator can unilaterally add invalid data to the register. Essential for the security model.

**Independent Test**: Can be tested with a network of 3+ validator instances, having one create a proposed docket, observing vote collection from peers, and verifying consensus is achieved when >50% approve.

**Acceptance Scenarios**:

1. **Given** a validator needs to distribute a proposed docket, **When** it queries the Peer Service for active validators, **Then** it receives a list of validators with reputation scores
2. **Given** a proposed docket is created by a validator, **When** it is distributed to peer validators, **Then** each validator receives it via the Peer Service and validates it independently
3. **Given** a validator receives a proposed docket for validation, **When** it validates the docket, **Then** it signs a vote (approve/reject) using its system wallet and returns it to the originating validator
4. **Given** a validator has collected votes from peers, **When** votes exceed 50% approval threshold, **Then** consensus is achieved and the docket is marked as confirmed
5. **Given** a validator has collected votes from peers, **When** votes do not reach 50% approval within the timeout period, **Then** consensus fails, the docket is rejected, and all transactions from the rejected docket are returned to the memory pool
6. **Given** consensus is achieved on a docket, **When** writing to the Register Service, **Then** the docket includes all collected validator signatures as proof of consensus
7. **Given** a docket fails consensus, **When** its transactions are returned to the memory pool, **Then** they retain their original priority and timestamp for inclusion in the next docket build cycle
8. **Given** a validator consistently proposes invalid dockets, **When** the Peer Service detects this behavior, **Then** the validator's reputation score decreases and it receives reduced connectivity from the network

---

### User Story 4 - Confirmed Docket Persistence (Priority: P1)

Once consensus is achieved on a proposed docket, the validator service writes the confirmed docket to the Register Service, which persists it to the distributed ledger. The validator also broadcasts the confirmed docket to the Peer Service for network-wide distribution.

**Why this priority**: This is the final step in the validation pipeline - without persistence, all previous work is lost and the ledger cannot grow.

**Independent Test**: Can be tested by achieving consensus on a docket, verifying it is written to Register Service with all signatures, and confirming it appears in the blockchain when queried.

**Acceptance Scenarios**:

1. **Given** consensus is achieved on a proposed docket, **When** the validator writes to Register Service, **Then** the docket is persisted to the blockchain with all validator signatures attached
2. **Given** a confirmed docket is written to Register Service, **When** the write succeeds, **Then** the validator broadcasts the confirmed docket to Peer Service for network distribution
3. **Given** a confirmed docket is written, **When** the write operation fails, **Then** the validator retries with exponential backoff and logs the error
4. **Given** transactions are included in a confirmed docket, **When** the docket is persisted, **Then** those transactions are removed from the memory pool
5. **Given** a validator receives a confirmed docket from a peer, **When** it validates the docket and consensus signatures, **Then** it writes the docket to its local Register Service only if validation succeeds

---

### User Story 5 - Peer Docket Validation (Priority: P2)

When a validator receives a newly confirmed docket from the Peer Service (created by another validator), it must validate the docket and consensus signatures before writing it to the local Register Service. This prevents malicious or compromised validators from polluting the distributed ledger.

**Why this priority**: This is a critical security check but depends on the core validation and consensus mechanisms being established first.

**Independent Test**: Can be tested by having a validator receive a confirmed docket from a peer, verifying it validates all aspects (transaction validity, previous hash linkage, consensus signatures), and confirming it only writes valid dockets to Register Service.

**Acceptance Scenarios**:

1. **Given** a confirmed docket is received from the Peer Service, **When** the validator checks it, **Then** it validates transaction signatures, blueprint compliance, and previous hash linkage
2. **Given** a confirmed docket has valid structure but invalid consensus signatures, **When** the validator checks it, **Then** it rejects the docket and logs a security warning
3. **Given** a confirmed docket has fewer than required validator signatures, **When** the validator checks it, **Then** it rejects the docket as not meeting consensus threshold
4. **Given** a confirmed docket passes all validations, **When** the validator accepts it, **Then** it writes the docket to local Register Service
5. **Given** a confirmed docket creates a fork (conflicts with local chain), **When** the validator detects the conflict, **Then** it applies longest-chain resolution rules: if the competing chain has more subsequent dockets than the local chain, switch to the longer chain; otherwise, reject the conflicting docket

---

### User Story 6 - Register-Specific Validation Contexts (Priority: P2)

Each register maintains its own independent validation context, meaning transactions and dockets are validated according to the specific blueprint rules and chain state of that register. Multiple registers can operate simultaneously without interference.

**Why this priority**: This enables multi-tenancy and register isolation but can be implemented incrementally as the core single-register validation works.

**Independent Test**: Can be tested by creating two separate registers with different blueprints, submitting transactions to each, and verifying they validate independently without cross-contamination.

**Acceptance Scenarios**:

1. **Given** multiple registers are active, **When** transactions arrive for different registers, **Then** each is validated against its own register's blueprint rules
2. **Given** two registers with different blueprints, **When** transactions are submitted simultaneously, **Then** memory pools remain isolated and dockets are built independently
3. **Given** a register's blueprint rules change, **When** validating new transactions, **Then** the new rules apply without affecting other registers
4. **Given** one register's consensus fails, **When** other registers are processing transactions, **Then** they continue operating normally
5. **Given** a register is deleted or archived, **When** transactions arrive for it, **Then** the validator rejects them with "register not found" errors

---

### User Story 7 - System Wallet Management (Priority: P2)

The validator service uses a system wallet (managed by the Wallet Service) to sign votes and dockets. This wallet represents the validator instance's identity in the network and is used for all cryptographic operations.

**Why this priority**: Essential for security and identity but depends on the Wallet Service integration being functional first.

**Independent Test**: Can be tested by starting a validator service, verifying it initializes or retrieves its system wallet from Wallet Service, and confirming all signatures use this wallet's keys.

**Acceptance Scenarios**:

1. **Given** a validator service starts for the first time, **When** it initializes, **Then** it creates or retrieves a system wallet from the Wallet Service
2. **Given** the validator needs to sign a vote, **When** it calls the Wallet Service, **Then** the signature is created using the system wallet's private keys
3. **Given** the validator's system wallet is compromised, **When** an administrator rotates the keys, **Then** the validator updates its identity and notifies peers
4. **Given** multiple validator instances are running, **When** they operate, **Then** each uses its own distinct system wallet
5. **Given** the Wallet Service is unavailable, **When** the validator attempts signing operations, **Then** it retries with backoff and logs errors without crashing

---

### User Story 8 - Memory Pool Management (Priority: P3)

The validator service manages memory pools efficiently using FIFO processing with priority override: transactions are normally processed in order of arrival, but high-priority transactions can bypass the queue. The system also implements transaction expiration and size limits to prevent resource exhaustion.

**Why this priority**: Important for production resilience but basic memory pool functionality can work initially without sophisticated policies.

**Independent Test**: Can be tested by filling a memory pool to capacity, observing eviction behavior, setting transaction TTLs and observing expiration, and verifying priority ordering.

**Acceptance Scenarios**:

1. **Given** the memory pool has size limits configured, **When** it reaches capacity with normal-priority transactions, **Then** the oldest transaction is evicted (FIFO)
2. **Given** the memory pool is at capacity, **When** a high-priority transaction arrives, **Then** it can bypass the queue and evict an older low-priority transaction
3. **Given** transactions have expiration timestamps, **When** they expire, **Then** they are automatically removed from the memory pool
4. **Given** transactions have priority levels, **When** building a docket, **Then** higher priority transactions are selected first
5. **Given** a transaction has been in the memory pool for extended time, **When** checking status, **Then** the validator reports estimated time until inclusion in a docket
6. **Given** a memory pool cleanup cycle runs, **When** it executes, **Then** expired and invalid transactions are removed to free space

---

### Edge Cases

- What happens when a validator receives a proposed docket but cannot reach the originating validator to return a vote?
- How does the system handle a validator that votes on a docket but then goes offline before consensus completes?
- What happens when two validators simultaneously create competing dockets for the same register? → Validators apply longest-chain resolution: follow the chain with more subsequent dockets
- What happens when a docket fails to achieve consensus? → Transactions are returned to memory pool with original priority/timestamp for inclusion in next docket
- How does the system detect and handle a malicious validator that signs conflicting votes? → Peer Service tracks validator behavior and reduces connectivity for validators with poor reputation scores
- What happens when consensus is achieved but Register Service write fails on some validators but succeeds on others?
- How does a new validator joining the network catch up on historical dockets?
- What happens when a validator's system wallet is compromised and needs rotation?
- How does the system handle clock skew between validators affecting timestamp validation?
- What happens when a docket references a previous hash that doesn't exist in the local chain?
- How does the system handle partial network partitions where some validators can communicate but others cannot?
- How does a validator's reputation score recover after temporary issues are resolved?
- What happens when Peer Service is temporarily unavailable and validators cannot discover peers?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST receive transactions from the Peer Service and add them to the appropriate register's memory pool
- **FR-002**: System MUST validate each incoming transaction against the blueprint rules before adding to memory pool
- **FR-003**: System MUST use the existing blueprint validation library for transaction payload validation
- **FR-004**: System MUST maintain separate memory pools for each register with independent validation contexts, using FIFO processing with priority override (high-priority transactions can bypass the queue)
- **FR-005**: System MUST create proposed dockets from memory pool transactions using hybrid triggering: when either a configurable time interval elapses OR when memory pool size reaches a configurable transaction count threshold (whichever occurs first)
- **FR-006**: System MUST link each proposed docket to the previous docket via cryptographic hash
- **FR-007**: System MUST distribute proposed dockets to peer validator services via the Peer Service
- **FR-008**: System MUST validate received proposed dockets independently before voting
- **FR-009**: System MUST sign votes on proposed dockets using the system wallet
- **FR-010**: System MUST collect votes from peer validators and determine when consensus threshold (>50%) is achieved
- **FR-011**: System MUST write confirmed dockets to the Register Service only after achieving consensus
- **FR-012**: System MUST broadcast confirmed dockets to the Peer Service for network distribution
- **FR-013**: System MUST validate confirmed dockets received from peers before writing to local Register Service
- **FR-014**: System MUST enforce that only the Validator Service can write dockets to the Register Service
- **FR-015**: System MUST use a system wallet (one per validator instance) for all cryptographic signing operations
- **FR-016**: System MUST integrate with the Wallet Service for signature creation and verification
- **FR-017**: System MUST integrate with the Peer Service for docket distribution, vote collection, and discovery of other validator instances; Peer Service maintains reputation scores for validators based on behavior
- **FR-029**: System MUST query Peer Service for list of active validators with reputation scores when participating in consensus
- **FR-030**: System MUST report validator behavior to Peer Service (e.g., invalid dockets proposed, consensus violations) to inform reputation scoring
- **FR-031**: Peer Service MUST reduce connectivity to validators with poor reputation scores (validators that submit too many invalid transactions or malicious dockets)
- **FR-018**: System MUST integrate with the Register Service for reading previous dockets and writing confirmed dockets
- **FR-019**: System MUST integrate with the Blueprint Service for retrieving blueprint definitions and validation rules
- **FR-020**: System MUST remove transactions from memory pool once they are included in a confirmed docket
- **FR-021**: System MUST handle genesis dockets (first docket in a register) with null previous hash
- **FR-022**: System MUST validate that docket sequence numbers are consecutive
- **FR-023**: System MUST validate that docket timestamps are monotonically increasing
- **FR-024**: System MUST verify consensus signatures on received confirmed dockets
- **FR-025**: System MUST support configurable consensus thresholds (default >50% but configurable)
- **FR-026**: System MUST resolve blockchain forks using longest-chain strategy: when detecting competing dockets at the same docket number, follow the chain with the most subsequent dockets (most accumulated work)
- **FR-027**: System MUST return all transactions from a rejected docket (failed consensus) to the memory pool, preserving their original priority and timestamp for inclusion in subsequent docket builds
- **FR-028**: System MUST evict oldest transaction (FIFO) when memory pool reaches capacity, unless the incoming transaction is high-priority, in which case it may evict older low-priority transactions to make room

### Non-Functional Requirements

- **NFR-001**: System MUST validate transactions within 500ms on average (P95)
- **NFR-002**: System MUST support at least 100 transactions per second throughput per register
- **NFR-003**: System MUST achieve consensus within 30 seconds for networks with up to 10 validators
- **NFR-004**: System MUST handle memory pools with up to 10,000 pending transactions per register
- **NFR-005**: System MUST maintain high availability with automatic recovery from transient failures
- **NFR-006**: System MUST use gRPC for internal service-to-service communication (as per architecture standards)
- **NFR-007**: System MUST provide comprehensive logging of all validation decisions
- **NFR-008**: System MUST expose metrics for monitoring memory pool size, validation throughput, and consensus success rate
- **NFR-009**: System MUST be horizontally scalable (multiple validator instances per register)
- **NFR-010**: System MUST persist memory pool state to survive service restarts
- **NFR-011**: Docket creation time threshold MUST be configurable per register (default: 10 seconds)
- **NFR-012**: Docket creation size threshold MUST be configurable per register (default: 50 transactions)

### Key Entities

- **Transaction**: A signed record of a blueprint action execution that needs validation and inclusion in the distributed ledger. Contains transaction ID, register ID, blueprint ID, payload, timestamp, signatures, metadata, and priority level (used for queue management in memory pool).

- **Docket (Block)**: A container of validated transactions that forms a link in the blockchain for a specific register. Contains docket number, previous hash, timestamp, list of transactions, validator signature, and consensus signatures.

- **Memory Pool**: A temporary cache of pending transactions for a specific register waiting to be included in a docket. Organized by register using FIFO processing with priority override (high-priority transactions can bypass the queue). Implements eviction policies (oldest first for normal priority), expiration based on TTL, and configurable size limits.

- **Consensus Vote**: A signed approval or rejection of a proposed docket by a validator. Contains docket hash, vote decision, validator signature, and timestamp.

- **Register Validation Context**: The isolated validation environment for a specific register including its blueprint rules, chain state, and memory pool.

- **System Wallet**: A cryptographic wallet representing the validator service instance's identity, used for signing votes and dockets.

- **Validator Instance**: A running instance of the validator service that participates in distributed consensus for one or more registers.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Transactions are validated and added to memory pool within 500ms (P95)
- **SC-002**: Dockets achieve consensus within 30 seconds in networks with 3-10 validators (P95)
- **SC-003**: System successfully handles 100 transactions per second per register without memory pool overflow
- **SC-004**: Invalid transactions are rejected with clear error messages in 100% of test cases
- **SC-005**: Consensus mechanism correctly rejects dockets when less than 50% of validators approve, and transactions from rejected dockets are successfully returned to memory pool for retry
- **SC-006**: Confirmed dockets written to Register Service include all required consensus signatures
- **SC-007**: Malicious dockets (invalid signatures, broken chains) are detected and rejected in 100% of test cases
- **SC-008**: Multiple registers operate independently with isolated validation contexts without cross-contamination
- **SC-009**: System maintains 99.9% uptime with automatic recovery from transient failures
- **SC-010**: All dockets maintain correct hash chain linkage with no gaps or forks (except in explicit fork resolution scenarios)
- **SC-011**: Validators that consistently submit invalid dockets or malicious transactions experience measurable reduction in network connectivity through Peer Service reputation management

## Assumptions

1. The Peer Service provides reliable message delivery for docket distribution and vote collection, and maintains reputation scores for all validators in the network
2. The Wallet Service is available and performant enough for high-frequency signing operations
3. The Register Service enforces write access control allowing only Validator Service to write dockets
4. The Blueprint Service provides fast access to blueprint definitions and validation schemas
5. Network latency between validators is low enough for 30-second consensus target (assume <5 seconds RTT)
6. System clocks across validators are synchronized within reasonable bounds (NTP or similar)
7. gRPC is used for all inter-service communication as per Sorcha architecture standards
8. Redis or similar is available for distributed caching and pub/sub if needed for coordination
9. Each validator instance has exclusive write access to its local Register Service instance
10. The memory pool persists to storage to survive service restarts (implementation detail TBD)

## Dependencies

- **Peer Service**: Must be operational for docket distribution, vote collection, validator discovery, and reputation management; maintains reputation scores for validators and reduces connectivity to poorly-behaving validators
- **Wallet Service**: Must be operational for signature creation and verification
- **Register Service**: Must be operational for reading chain state and writing confirmed dockets
- **Blueprint Service**: Must be operational for retrieving blueprint validation rules
- **Redis/Cache**: May be needed for distributed coordination and pub/sub messaging
- **gRPC Infrastructure**: Required for inter-service communication
- **.NET Aspire**: Required for service orchestration and discovery

## Out of Scope

- Advanced consensus mechanisms beyond simple majority voting (e.g., BFT, PoW, PoS)
- Smart contract execution within the validator (handled by Blueprint Engine)
- Cross-register transaction atomicity (each register is independent)
- Historical docket pruning or archiving (register management responsibility)
- Validator reputation or staking mechanisms
- Geographic distribution or multi-region consensus coordination
- Deep blockchain reorganization beyond immediate fork resolution (MVP uses simple longest-chain for recent forks only)
- Custom validation plugins or extensions (MVP uses standard blueprint validation)
