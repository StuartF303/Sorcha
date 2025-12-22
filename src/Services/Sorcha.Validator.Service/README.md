# Sorcha Validator Service

**Status:** MVP Implementation Complete (95%)

## Overview

The Validator Service is a distributed consensus coordinator for the Sorcha blockchain platform. It manages transaction validation, docket creation, and distributed consensus achievement across validator nodes.

## Key Features

- ✅ Memory Pool Management (FIFO + priority queues)
- ✅ Docket Building (hybrid triggers: time OR size)  
- ✅ Distributed Consensus (>50% threshold voting)
- ✅ Validator Orchestration (full pipeline coordination)
- ✅ gRPC Peer Communication (RequestVote, ValidateDocket RPCs)
- ✅ Admin REST API (start/stop validators, status queries)

## API Endpoints

**Validation:** `/api/v1/transactions/*`
**Admin:** `/api/admin/validators/*`
**gRPC:** Peer-to-peer consensus communication

See full documentation in [specs/002-validator-service/](../../../specs/002-validator-service/)
