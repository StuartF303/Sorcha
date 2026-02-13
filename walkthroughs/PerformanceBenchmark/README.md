# Performance Benchmark Walkthrough

**Purpose:** Benchmark Register Service performance with various payload sizes, throughput, and latency testing to identify operational bounds and optimization opportunities.

**Date Created:** 2026-02-13
**Status:** ✅ Complete
**Prerequisites:** Docker Desktop, PowerShell 7+, .NET 10 SDK

---

## Overview

This walkthrough provides comprehensive performance testing for the Sorcha Register Service, measuring:
- **Payload Performance:** Transaction sizes from 1KB to 1MB
- **Throughput Testing:** Sustained transaction rates (TPS)
- **Latency Benchmarks:** Response times under various loads
- **Concurrency Testing:** Multiple parallel transaction streams
- **Docket Building:** Consensus and blockchain performance
- **Resource Utilization:** Memory, CPU, and MongoDB performance

---

## Files in This Walkthrough

### Test Scripts
- **test-performance.ps1** - ⭐ Main performance testing script with automated benchmarks
- **generate-test-data.ps1** - Generate various payload sizes for testing
- **monitor-resources.ps1** - Monitor Docker container resource usage during tests

### Configuration
- **test-config.json** - Test parameters (payload sizes, iterations, concurrency)

### Results
- **PERFORMANCE-REPORT.md** - Complete test results with analysis and recommendations
- **results/** - Individual test run data (JSON/CSV)

---

## Quick Start

### 1. Start Sorcha Services
```bash
# From repository root
docker-compose up -d

# Wait for services to be healthy (30 seconds)
Start-Sleep -Seconds 30
```

### 2. Bootstrap Platform (if not done)
```powershell
powershell -ExecutionPolicy Bypass -File scripts/bootstrap-sorcha.ps1 `
  -Profile docker-direct `
  -NonInteractive `
  -OrgName "Performance Testing" `
  -Subdomain "perf" `
  -AdminEmail "admin@perf.local" `
  -AdminName "Performance Admin" `
  -AdminPassword "PerfTest2026!"
```

### 3. Run Performance Tests
```powershell
# Full benchmark suite (15-20 minutes)
./walkthroughs/PerformanceBenchmark/test-performance.ps1

# Quick test (payload sizes only, 5 minutes)
./walkthroughs/PerformanceBenchmark/test-performance.ps1 -QuickTest

# Custom configuration
./walkthroughs/PerformanceBenchmark/test-performance.ps1 `
  -MaxPayloadSize 512KB `
  -Iterations 100 `
  -Concurrency 10
```

### 4. View Results
Results are saved to `walkthroughs/PerformanceBenchmark/results/` with timestamp:
- `benchmark-{timestamp}.json` - Raw test data
- `summary-{timestamp}.md` - Human-readable summary
- `PERFORMANCE-REPORT.md` - Aggregated analysis with recommendations

---

## Test Scenarios

### 1. Payload Size Testing
**Objective:** Measure performance across transaction payload sizes

| Payload Size | Test Count | Metrics Captured |
|--------------|------------|------------------|
| 1 KB | 100 | Latency, throughput |
| 10 KB | 100 | Latency, throughput |
| 50 KB | 50 | Latency, throughput |
| 100 KB | 50 | Latency, throughput |
| 500 KB | 25 | Latency, throughput |
| 1 MB | 10 | Latency, throughput |

**Expected:** Linear latency increase with payload size, throughput decline at >100KB

### 2. Throughput Testing
**Objective:** Maximum sustained transaction rate

| Test | Duration | Payload Size | Target TPS |
|------|----------|--------------|------------|
| Baseline | 60s | 5 KB | Max achievable |
| Sustained | 300s | 5 KB | 90% of max |
| Burst | 10s | 5 KB | Peak capacity |

**Metrics:** Transactions/second, error rate, latency p50/p95/p99

### 3. Latency Benchmarks
**Objective:** Response time distribution under normal and stressed conditions

| Scenario | Concurrency | Payload | Duration |
|----------|-------------|---------|----------|
| Single-threaded | 1 | 5 KB | 60s |
| Light load | 5 | 5 KB | 60s |
| Moderate load | 20 | 5 KB | 60s |
| Heavy load | 50 | 5 KB | 60s |

**Metrics:** Min, Max, Mean, Median, P95, P99 latencies

### 4. Concurrency Testing
**Objective:** Performance under parallel transaction streams

| Workers | Transactions Each | Payload | Total Transactions |
|---------|-------------------|---------|-------------------|
| 1 | 100 | 5 KB | 100 |
| 5 | 100 | 5 KB | 500 |
| 10 | 100 | 5 KB | 1,000 |
| 25 | 100 | 5 KB | 2,500 |
| 50 | 100 | 5 KB | 5,000 |

**Metrics:** Total time, TPS, error rate, resource usage

### 5. Docket Building Performance
**Objective:** Consensus and blockchain construction speed

| Docket Size | Test Runs | Metrics |
|-------------|-----------|---------|
| 10 txs | 20 | Build time, validation time |
| 50 txs | 10 | Build time, validation time |
| 100 txs | 5 | Build time, validation time |
| 500 txs | 2 | Build time, validation time |

**Metrics:** Time to docket, signature verification time, MongoDB write time

---

## Expected Performance Baselines

Based on architecture analysis, expected performance ranges:

| Metric | Conservative | Target | Stretch |
|--------|-------------|--------|---------|
| **Throughput (TPS)** | 50 | 100 | 200+ |
| **Latency (P95, 5KB)** | <500ms | <200ms | <100ms |
| **Max Payload** | 1 MB | 5 MB | 10 MB |
| **Concurrent Workers** | 10 | 25 | 50+ |
| **Docket Build (100 tx)** | <5s | <2s | <1s |

---

## Key Results (Template)

**✅ Performance Achievements:**
- [ ] Throughput: ___ TPS sustained
- [ ] Latency P95: ___ ms at normal load
- [ ] Max payload: ___ MB without errors
- [ ] Concurrent workers: ___ without degradation
- [ ] Docket building: ___ transactions/second

**⚠️ Performance Bottlenecks:**
- [ ] _Bottleneck 1 description_
- [ ] _Bottleneck 2 description_

**🔧 Quick Wins Identified:**
- [ ] _Easy optimization 1_
- [ ] _Easy optimization 2_

---

## Architecture Impact Analysis

The test results will identify bottlenecks in:

1. **API Gateway (YARP):** Request routing overhead
2. **Register Service:** Transaction validation logic
3. **MongoDB:** Write performance, indexing
4. **Wallet Service:** Signature verification
5. **Validator Service:** Consensus algorithm
6. **Network:** Docker bridge, serialization

---

## Credentials

**Performance Test User:**
- Email: `admin@perf.local`
- Password: `PerfTest2026!`
- Organization: Performance Testing (subdomain: perf)

---

## Next Steps

After completing the benchmark:

1. **Review Results:** Check `PERFORMANCE-REPORT.md` for analysis
2. **Identify Bottlenecks:** Focus on P95/P99 latencies and error rates
3. **Apply Quick Wins:** Implement easy optimizations (caching, indexing)
4. **Re-test:** Validate improvements
5. **Production Planning:** Use results for capacity planning

---

## Troubleshooting

### Services crash during testing
- **Cause:** Resource exhaustion (memory/CPU)
- **Fix:** Reduce concurrency or payload sizes
- **Check:** `docker stats` for container resource usage

### MongoDB connection errors
- **Cause:** Connection pool exhaustion
- **Fix:** Increase MongoDB connection limits in Register Service
- **Check:** `docker logs sorcha-register-mongodb-1`

### Timeouts during large payloads
- **Cause:** Request timeout too low
- **Fix:** Increase Kestrel request timeouts
- **Check:** Register Service configuration

### Inconsistent results
- **Cause:** Background processes, Docker resource limits
- **Fix:** Stop other containers, increase Docker resources
- **Run:** Tests multiple times and average results

---

## Related Documentation

- [Register Service API](../../src/Services/Sorcha.Register.Service/)
- [Architecture Overview](../../docs/architecture.md)
- [Docker Configuration](../../docker-compose.yml)
- [MongoDB Setup](../../docs/MONGODB-SETUP.md)

---

**Benchmark Complete!** You now have comprehensive performance data for the Sorcha Register Service with actionable optimization recommendations.
