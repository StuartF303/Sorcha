# PingPong Walkthrough

**Purpose:** Simple ping-pong workflow demonstrating the full pipeline from org bootstrap through action execution.

**Category:** Single-Org
**Status:** Active
**Prerequisites:** Docker Desktop, PowerShell 7+

---

## Quick Start

```bash
pwsh walkthroughs/initialize-secrets.ps1     # First time only
pwsh walkthroughs/PingPong/setup.ps1          # Bootstrap everything
pwsh walkthroughs/PingPong/run.ps1            # Execute rounds
pwsh walkthroughs/PingPong/run.ps1 -RoundTrips 20  # More rounds
```

## What It Tests

1. Organization bootstrap with fallback login
2. ED25519 wallet creation
3. Participant registration + wallet challenge-sign-verify linking
4. 2-phase register creation (initiate/sign/finalize)
5. Blueprint template loading, wallet patching, publish (with cycle warnings)
6. Cyclic workflow execution (alternating ping/pong actions)

## Files

| File | Purpose |
|------|---------|
| `config.json` | Walkthrough metadata |
| `setup.ps1` | Full environment setup |
| `run.ps1` | Execute ping-pong rounds |
| `templates/ping-pong-template.json` | Blueprint template |
| `state.json` | Runtime state (gitignored) |
