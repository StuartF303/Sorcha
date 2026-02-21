# PerformanceBenchmark

**Category:** Advanced
**Purpose:** Benchmark Register Service performance — payload sizes, throughput, latency, concurrency, docket building.

---

## Quick Start

```powershell
# 1. Start services
docker-compose up -d

# 2. Generate secrets (first time only)
pwsh walkthroughs/initialize-secrets.ps1

# 3. Setup (bootstrap org, create wallet)
pwsh walkthroughs/PerformanceBenchmark/setup.ps1

# 4. Run benchmark
pwsh walkthroughs/PerformanceBenchmark/test-performance.ps1

# Quick test (shorter iterations)
pwsh walkthroughs/PerformanceBenchmark/test-performance.ps1 -QuickTest
```

---

## Parameters

### setup.ps1

| Parameter | Default | Description |
|-----------|---------|-------------|
| `-Profile` | `gateway` | `gateway`, `direct`, or `aspire` |
| `-SkipHealthCheck` | off | Skip Docker health check |

### test-performance.ps1

| Parameter | Default | Description |
|-----------|---------|-------------|
| `-QuickTest` | off | Shorter iterations for fast validation |
| `-MaxPayloadSize` | `1MB` | Largest payload to test |
| `-Iterations` | `100` | Latency test iterations |
| `-Concurrency` | `25` | Max concurrent workers |
| `-ShowJson` | off | Show JSON responses |

---

## Test Scenarios

| Test | What It Measures |
|------|------------------|
| **Payload Size** | Latency across 1KB–1MB payloads |
| **Throughput** | Sustained TPS over 60 seconds |
| **Latency** | P50/P95/P99 under light to heavy load |
| **Concurrency** | Parallel worker scaling (1–25+ workers) |
| **Docket Building** | Transaction batch submission rate |

---

## Files

| File | Purpose |
|------|---------|
| `config.json` | Walkthrough metadata |
| `setup.ps1` | Bootstrap org + wallet → `state.json` |
| `test-performance.ps1` | Main benchmark suite (5 tests) |
| `monitor-resources.ps1` | Docker container resource monitoring |
| `capture-system-specs.ps1` | System specification capture |
| `results/` | Generated benchmark data (JSON + markdown) |

---

## Results

Results are saved to `results/` with timestamps:
- `benchmark-{timestamp}.json` — Raw test data
- `summary-{timestamp}.md` — Human-readable summary

---

## Expected Baselines

| Metric | Conservative | Target | Stretch |
|--------|-------------|--------|---------|
| Throughput (TPS) | 50 | 100 | 200+ |
| Latency P95 (5KB) | <500ms | <200ms | <100ms |
| Max Payload | 1 MB | 5 MB | 10 MB |
| Concurrent Workers | 10 | 25 | 50+ |

---

## Troubleshooting

- **Services crash during testing** — Reduce concurrency or payload sizes. Check `docker stats`.
- **MongoDB connection errors** — Connection pool exhaustion. Increase MongoDB limits.
- **Timeouts on large payloads** — Increase Kestrel request timeouts.
- **Inconsistent results** — Stop other containers, increase Docker resources, run multiple times.
