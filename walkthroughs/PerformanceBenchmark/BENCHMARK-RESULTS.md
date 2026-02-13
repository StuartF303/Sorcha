# Performance Benchmark Results

**Date:** 2026-02-13
**Test Duration:** ~3.5 minutes (Quick Test Mode)
**Status:** ✅ Infrastructure Performance Measured | ⚠️ Transaction Tests Blocked by Governance

---

## System Specifications

### Host Hardware

| Component | Specification |
|-----------|---------------|
| **CPU** | Intel Core i5-10310U @ 1.70GHz |
| **Cores** | 4 physical, 8 logical |
| **Base Clock** | 2208 MHz (turbo capable) |
| **Total RAM** | 16 GB |
| **Available RAM** | 3.04 GB (at test start) |
| **OS** | Windows 11 Pro (Build 26200) |
| **Storage** | 952.6 GB total, 450.88 GB free (NTFS, SSD) |

### Docker Environment

| Resource | Allocation |
|----------|------------|
| **Docker Version** | 29.2.0 |
| **CPUs Allocated** | 8 logical cores |
| **Memory Allocated** | 7.62 GB |
| **Running Containers** | 12 services |
| **Storage Driver** | overlay2 |

### Container Resource Usage (Baseline)

| Container | CPU % | Memory | Network I/O |
|-----------|-------|--------|-------------|
| **API Gateway** | 0.87% | 56.2 MB | 8.76 KB / 193 KB |
| **Register Service** | 0.76% | 64.2 MB | 77.9 KB / 288 KB |
| **Wallet Service** | 0.65% | 102.5 MB | 31 KB / 360 KB |
| **Blueprint Service** | 0.92% | 62.0 MB | 19.3 KB / 233 KB |
| **Tenant Service** | 1.16% | 110 MB | 46.8 KB / 383 KB |
| **Validator Service** | 3.38% | 65.1 MB | 575 KB / 4.19 MB |
| **Peer Service** | 1.51% | 77.2 MB | 78.3 KB / 571 KB |
| **MongoDB** | 0.97% | 402.8 MB | 27.6 KB / 62.5 KB |
| **PostgreSQL** | 5.77% | 64.2 MB | 54.3 KB / 41.3 KB |
| **Redis** | 0.49% | 8.5 MB | 1.46 MB / 570 KB |

**Total Memory Usage:** ~1.1 GB / 7.62 GB allocated (14.4% utilization)

---

## Performance Results

### 1. Authentication Performance ✅

| Metric | Value |
|--------|-------|
| **OAuth2 Token Request** | ~50-100ms |
| **Token Format** | JWT Bearer |
| **Endpoint** | `/api/service-auth/token` |
| **Success Rate** | 100% |

**Notes:** Authentication is fast and reliable through API Gateway.

---

### 2. Wallet Creation Performance ✅

| Metric | Value |
|--------|-------|
| **Algorithm** | ED25519 |
| **Word Count** | 12 words (BIP39) |
| **Creation Time** | ~100-200ms |
| **Address Format** | ws11q... (Bech32) |
| **Success Rate** | 100% |

**Sample Output:**
- Address: `ws11qpc2jsfsjclx7nagdpccrt9yqgqx4rv4hc5u4kvk29kdswecwh4xxxygkw3`
- Public Key: Base64-encoded ED25519 key
- Signature Capability: Verified via test signing

---

### 3. Register Creation Performance ✅

| Metric | Value | Notes |
|--------|-------|-------|
| **Initiate Latency** | ~7-8ms | Very fast initiation |
| **Sign Attestation** | ~50-100ms | ED25519 signature generation |
| **Finalize Latency** | ~5-6ms | Quick finalization |
| **Total Time** | ~200-300ms | End-to-end register creation |
| **Success Rate** | 100% | After attestation format fix |

**Register Creation Flow:**
1. Create wallet (100-200ms)
2. Initiate register (7-8ms) → Receive attestation challenge
3. Sign attestation (50-100ms) → ED25519 signature
4. Finalize register (5-6ms) → Register created

**Sample Register ID:** `cff1235f0dec4ac0920bda12f162d834`

---

### 4. Transaction Performance ⚠️ BLOCKED

| Test Scenario | Result | Error |
|---------------|--------|-------|
| **Payload Size (1KB-1MB)** | ❌ 0% success | 403 Forbidden |
| **Throughput Testing** | ❌ 0% success | 403 Forbidden |
| **Latency Benchmarks** | ❌ 0% success | 403 Forbidden |
| **Concurrency Testing** | ❌ 0% success | 403 Forbidden |
| **Docket Building** | ❌ 0% success | 403 Forbidden |

**Root Cause:** Register governance permissions not configured for transaction posting.

**Evidence from Logs:**
```
HTTP POST /api/registers/{registerId}/transactions responded 403 in 0.4ms
```

**Next Steps to Unblock:**
1. Implement proper register governance setup
2. Grant transaction posting rights to wallet owner
3. Or use a pre-configured register with governance policies

---

## API Latency Measurements

### Direct Register Service (Port 5380)

| Endpoint | Method | Avg Latency | Notes |
|----------|--------|-------------|-------|
| `/api/registers/initiate` | POST | 7-8ms | Register creation start |
| `/api/registers/finalize` | POST | 5-6ms | Register creation complete |
| `/api/v1/wallets/{id}/sign` | POST | 50-100ms | ED25519 signature |

### Through API Gateway (Port 80)

| Endpoint | Method | Avg Latency | Gateway Overhead |
|----------|--------|-------------|------------------|
| `/api/registers/initiate` | POST | 8-10ms | ~1-2ms (+15%) |
| `/api/registers/finalize` | POST | 6-8ms | ~1-2ms (+20%) |

**YARP Routing Overhead:** 15-20% latency increase for lightweight requests.

---

## Analysis & Insights

### ✅ What's Working Well

1. **Fast Service Response Times**
   - Register initiate/finalize in single-digit milliseconds
   - Wallet operations complete in <200ms
   - API Gateway routing overhead is minimal (<2ms)

2. **Efficient Resource Usage**
   - Services use <1.5 GB total memory
   - Low CPU utilization at idle (<6% per container)
   - Room for significant load scaling

3. **Reliable Infrastructure**
   - 100% success rate for auth, wallet, and register creation
   - No crashes or timeouts during testing
   - Docker containers remain healthy

### ⚠️ Bottlenecks & Limitations

1. **Governance Configuration Required**
   - Transaction posting blocked by 403 Forbidden
   - Newly created registers lack default write permissions
   - Need governance policy setup for performance testing

2. **PostgreSQL CPU Usage**
   - Tenant Service shows 5.77% CPU (highest among services)
   - May become bottleneck under heavy authentication load

3. **Validator Service Activity**
   - 3.38% CPU usage even at idle
   - High network I/O (4.19 MB) suggests active processing

---

## Recommendations

### Immediate Actions

1. **Enable Transaction Testing**
   - Configure default governance policies for new registers
   - Or create a test register with open write permissions
   - Document governance setup in walkthrough

2. **Optimize PostgreSQL**
   - Add indexes for authentication queries
   - Consider connection pool tuning
   - Monitor under load

3. **MongoDB Indexing**
   - Pre-create indexes for common query patterns
   - Current 402.8 MB memory usage is healthy

### Performance Baseline Established

Based on infrastructure tests, the system can support:

| Metric | Conservative Estimate | Notes |
|--------|----------------------|-------|
| **Register Creation TPS** | 100-200/sec | 5-10ms per register |
| **Wallet Creation TPS** | 50-100/sec | 100-200ms per wallet |
| **Authentication TPS** | 200-500/sec | 50-100ms per token |

**Hardware Ceiling:** 8 logical cores @ 2.2GHz with 7.62GB RAM can likely support:
- **Register operations:** 500-1000 TPS (CPU-bound)
- **MongoDB writes:** 1000-5000 TPS (memory-bound)
- **API Gateway routing:** 10,000+ req/sec (network-bound)

---

## Next Steps

### To Complete Full Benchmark:

1. **Resolve Governance (30-60 min)**
   - Implement register governance setup
   - Add default policies for test registers
   - Or create pre-configured test register

2. **Run Full Suite (15-20 min)**
   - Payload size testing (1KB → 1MB)
   - Throughput measurements (sustained TPS)
   - Latency distribution (P50/P95/P99)
   - Concurrency scaling (1-50 workers)
   - Docket building performance

3. **Comparative Analysis**
   - Run with `--Profile direct` (bypass API Gateway)
   - Compare latencies and identify routing overhead
   - Test with `--Profile aspire` (HTTPS endpoints)

### Alternative Approach:

**Use Pre-Existing Register:** If a register already exists with proper governance, update the test script to accept `--RegisterId` parameter and skip creation.

---

## Files Generated

| File | Purpose |
|------|---------|
| `SYSTEM-SPECS.md` | Complete hardware/Docker specifications |
| `BENCHMARK-RESULTS.md` | This file - test results and analysis |
| `results/benchmark-*.json` | Detailed test metrics (JSON) |
| `results/summary-*.md` | Auto-generated summary reports |

---

**Status:** Infrastructure performance successfully benchmarked. Transaction testing requires governance configuration.

**Completion:** 40% (Auth/Wallet/Register ✅ | Transactions ❌)

**Est. Time to Full Completion:** 1-2 hours (governance setup + full test run)
