# Performance Report Template

This file will be populated with actual test results after running the benchmark suite.

---

## How to Generate Report

Run the performance test suite:

```powershell
./walkthroughs/PerformanceBenchmark/test-performance.ps1
```

The script will automatically generate:
- **benchmark-{timestamp}.json** - Detailed results in JSON format
- **summary-{timestamp}.md** - Human-readable summary with tables and analysis

---

## Expected Metrics

### Payload Size Performance
- Small payloads (1-10KB): <100ms P95 latency
- Medium payloads (50-100KB): <200ms P95 latency
- Large payloads (500KB-1MB): <500ms P95 latency

### Throughput
- Target: 100+ TPS sustained (5KB payloads)
- Error rate: <1%

### Latency
- P95: <200ms under normal load
- P99: <500ms under normal load

### Concurrency
- 10 workers: No degradation
- 25 workers: <20% degradation
- 50 workers: <50% degradation

### Docket Building
- 100 transactions: <2 seconds
- 500 transactions: <10 seconds

---

## Performance Optimization Checklist

After running tests, check these areas for quick wins:

### Database (MongoDB)
- [ ] Add indexes for frequent queries
- [ ] Increase connection pool size
- [ ] Enable write concern optimization
- [ ] Review transaction size limits

### API Layer
- [ ] Compare 'gateway' vs 'direct' profile (YARP overhead)
- [ ] Enable response compression
- [ ] Implement request batching
- [ ] Review authentication overhead

### Register Service
- [ ] Profile transaction validation logic
- [ ] Optimize signature verification
- [ ] Cache frequently accessed data
- [ ] Review serialization performance

### Infrastructure
- [ ] Increase Docker memory limits
- [ ] Review CPU allocation
- [ ] Check network I/O bottlenecks
- [ ] Monitor Redis performance

---

## Bottleneck Analysis Framework

### 1. Identify Hotspots
- Review P99 latencies for outliers
- Check error rates during high concurrency
- Monitor resource usage spikes

### 2. Isolate Components
- Run 'direct' profile to bypass API Gateway
- Test individual services
- Profile with dotnet-trace

### 3. Apply Fixes
- Start with high-impact, low-effort changes
- Measure improvements incrementally
- Re-run affected test scenarios

### 4. Validate
- Full benchmark suite after major changes
- Compare before/after metrics
- Document improvements

---

## Baseline Performance (To Be Updated)

| Metric | Baseline | Current | Target | Status |
|--------|----------|---------|--------|--------|
| TPS (sustained) | - | - | 100+ | ⏳ Pending |
| P95 Latency (5KB) | - | - | <200ms | ⏳ Pending |
| Max Payload | - | - | 1MB | ⏳ Pending |
| Concurrent Workers | - | - | 25+ | ⏳ Pending |
| Docket Build (100 tx) | - | - | <2s | ⏳ Pending |

---

## Historical Results

### Run 1: [Date]
- Profile: [gateway/direct/aspire]
- Environment: [OS, Docker version]
- Key findings: [Summary]

---

**Last Updated:** [Will be updated after first run]
