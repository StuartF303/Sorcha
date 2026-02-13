# System Specifications - Performance Benchmark

**Date:** 2026-02-13 20:00:52
**Profile:** Performance Benchmark Walkthrough

---

## Host System

### Hardware

| Component | Specification |
|-----------|---------------|
| **CPU** | Intel(R) Core(TM) i5-10310U CPU @ 1.70GHz |
| **Cores** | 4 physical, 8 logical |
| **Base Clock** | 2208 MHz |
| **Total RAM** | 16 GB |
| **Available RAM** | 3.04 GB |

### Operating System

| Component | Specification |
|-----------|---------------|
| **OS** | Microsoft Windows 11 Pro |
| **Version** | 10.0.26200 |
| **Build** | 26200 |
| **Architecture** | 64-bit |

### Storage (C: Drive)

| Component | Specification |
|-----------|---------------|
| **Total Size** | 952.6 GB |
| **Free Space** | 450.88 GB |
| **File System** | NTFS |

---

## Docker Environment

### Docker Version

| Component | Version |
|-----------|---------|
| **Docker Engine** | 29.2.0 |
| **Total Containers** | 12 |
| **Running** | 12 |
| **Paused** | 0 |
| **Stopped** | 0 |

### Docker Resources

| Resource | Allocation |
|----------|------------|
| **CPUs** | 8 |
| **Total Memory** | 7.62 GB |
| **Driver** | overlay2 |
| **Storage Driver** |  |

---

## Running Containers (at time of capture)

| Container | CPU | Memory Usage | Network I/O | Block I/O |
|-----------|-----|--------------|-------------|-----------|
| sorcha-api-gateway | 0.87% | 56.2MiB / 7.619GiB | 8.76kB / 193kB | 4.45MB / 0B |
| sorcha-aspire-dashboard | 0.11% | 64.04MiB / 7.619GiB | 4.75MB / 113kB | 74MB / 4.1kB |
| sorcha-blueprint-service | 0.92% | 61.99MiB / 7.619GiB | 19.3kB / 233kB | 16.5MB / 0B |
| sorcha-mongodb | 0.97% | 402.8MiB / 7.619GiB | 27.6kB / 62.5kB | 253MB / 5.14MB |
| sorcha-peer-service | 1.51% | 77.18MiB / 7.619GiB | 78.3kB / 571kB | 2.08MB / 0B |
| sorcha-postgres | 5.77% | 64.2MiB / 7.619GiB | 54.3kB / 41.3kB | 39MB / 98.2MB |
| sorcha-redis | 0.49% | 8.527MiB / 7.619GiB | 1.46MB / 570kB | 26MB / 0B |
| sorcha-register-service | 0.76% | 64.2MiB / 7.619GiB | 77.9kB / 288kB | 803kB / 0B |
| sorcha-tenant-service | 1.16% | 110MiB / 7.619GiB | 46.8kB / 383kB | 23MB / 0B |
| sorcha-ui-web | 0.02% | 20.79MiB / 7.619GiB | 3.33kB / 126B | 10.9MB / 4.1kB |
| sorcha-validator-service | 3.38% | 65.05MiB / 7.619GiB | 575kB / 4.19MB | 1.92MB / 0B |
| sorcha-wallet-service | 0.65% | 102.5MiB / 7.619GiB | 31kB / 360kB | 37.8MB / 8.19kB |

---

## Performance Characteristics

### Expected Baseline Performance

Based on hardware specifications:

| Metric | Conservative | Target | Notes |
|--------|-------------|--------|-------|
| **Register TPS** | 50-100 | 100-200 | Depends on payload size |
| **API Latency (P95)** | <500ms | <200ms | 5KB payloads via gateway |
| **Max Concurrent Workers** | 10 | 25 | Before significant degradation |
| **MongoDB Write TPS** | 100-500 | 500-1000 | Document insert rate |

### Bottleneck Predictions

| Component | Likely Constraint | Mitigation |
|-----------|------------------|------------|
| **CPU** | 8 logical cores | Optimize signature verification, parallel processing |
| **Memory** | 16 GB RAM | Connection pooling, efficient caching |
| **Disk I/O** | Docker volumes | Use SSD, optimize MongoDB indexes |
| **Network** | Docker bridge | Direct service calls, compression |

---

## Benchmark Environment

### Test Configuration

- **Profile:** Gateway (through API Gateway on port 80)
- **Test Duration:** 15-20 minutes (full suite)
- **Payload Sizes:** 1KB, 10KB, 50KB, 100KB, 500KB, 1MB
- **Concurrency Levels:** 1, 5, 10, 25, 50 workers
- **Services Tested:** Register Service, API Gateway, Wallet Service, Validator Service

### Network Topology

```
[Test Script] → [API Gateway :80] → [Services :5000-5999]
                     ↓
                [MongoDB :27017]
                [PostgreSQL :5432]
                [Redis :6379]
```

---

## Reproducibility

To reproduce this benchmark on a comparable system:

1. **Minimum Requirements:**
   - CPU: 4+ cores
   - RAM: 8+ GB
   - Disk: 20+ GB free SSD storage
   - Docker Desktop with 4GB+ RAM allocation

2. **Setup:**
   ```bash
   docker-compose up -d
   ./walkthroughs/PerformanceBenchmark/bootstrap-perf-org.ps1
   ./walkthroughs/PerformanceBenchmark/test-performance.ps1
   ```

3. **Compare Results:**
   - Normalize TPS by CPU core count
   - Compare P95 latencies at same payload sizes
   - Analyze degradation patterns

---

**Captured by:** capture-system-specs.ps1
**Location:** C:\projects\Sorcha\walkthroughs\PerformanceBenchmark\SYSTEM-SPECS.md
