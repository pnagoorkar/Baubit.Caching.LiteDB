# Benchmark Results

**Date:** December 24, 2025  
**Runtime:** .NET 9.0.11 (9.0.11, 9.0.1125.51716), X64 RyuJIT x86-64-v3  
**Hardware:** AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores  
**OS:** Linux Ubuntu 24.04.3 LTS (Noble Numbat)  
**GC:** Concurrent Workstation  
**ID Type:** long (8 bytes)

## Performance Summary

### Cache Size: 1,000 Entries

| Operation                      | Mean (μs) | Ops/sec    | StdDev (μs) | Allocated |
|--------------------------------|----------:|-----------:|------------:|----------:|
| Read-Only: GetFirstOrDefault   |      0.09 | 10,784,353 |       0.004 |       0 B |
| Read-Only: GetEntryOrDefault   |     14.78 |     67,663 |       0.274 |  23,568 B |
| Read-Only: GetNextOrDefault    |     15.19 |     65,832 |       0.150 |  23,568 B |
| Write-Only: Update             |     56.30 |     17,762 |       0.197 |  43,660 B |
| Write-Only: Add                |     63.64 |     15,713 |       1.749 |  28,334 B |
| Mixed: 50% Read, 50% Write     |     92.19 |     10,847 |       1.636 |  57,252 B |
| Mixed: 80% Read, 20% Write     |    149.23 |      6,701 |       1.869 | 143,117 B |

### Cache Size: 10,000 Entries

| Operation                      | Mean (μs) | Ops/sec    | StdDev (μs) | Allocated |
|--------------------------------|----------:|-----------:|------------:|----------:|
| Read-Only: GetFirstOrDefault   |      0.10 |  9,688,074 |       0.001 |       0 B |
| Read-Only: GetEntryOrDefault   |     19.60 |     51,029 |       0.363 |  34,022 B |
| Read-Only: GetNextOrDefault    |     19.28 |     51,861 |       0.141 |  33,871 B |
| Write-Only: Add                |     62.01 |     16,126 |       1.637 |  22,852 B |
| Write-Only: Update             |     67.79 |     14,752 |       0.229 |  65,074 B |
| Mixed: 50% Read, 50% Write     |     97.57 |     10,250 |       2.408 |  56,515 B |
| Mixed: 80% Read, 20% Write     |    160.25 |      6,240 |       2.642 | 156,192 B |

## Key Findings

### Read Performance
- **GetFirstOrDefault** is the fastest operation at ~10 million ops/sec with zero allocations
- **GetEntryOrDefault** and **GetNextOrDefault** perform similarly at ~50,000-67,000 ops/sec
- Read performance degrades slightly with larger cache sizes due to increased database query overhead

### Write Performance
- **Add** and **Update** operations perform similarly at ~15,000-18,000 ops/sec
- Write operations show consistent performance across cache sizes
- Memory allocation for writes is lower with long IDs compared to Guid IDs

### Mixed Workloads
- **80% Read, 20% Write** workload: ~6,200-6,700 ops/sec
- **50% Read, 50% Write** workload: ~10,200-10,800 ops/sec
- Mixed workloads show balanced performance with moderate memory allocation

### Memory Characteristics with Long IDs
- GetFirstOrDefault has zero allocations, making it ideal for high-frequency head access
- Entry retrieval operations allocate ~23-34 KB per 10,000 operations
- Write operations allocate 23-65 KB per 10,000 operations depending on operation type
- Mixed workloads generate moderate allocations (57-156 KB per 10,000 operations)
- **Long IDs (8 bytes) require less memory than Guid IDs (16 bytes), resulting in improved cache locality and reduced allocation overhead**

## Benchmark Configuration

- **Invocation Count:** 10,000 operations per iteration
- **Iteration Count:** 10
- **Warmup Count:** 3
- **Confidence Interval:** 99.9%
- **ID Type:** long (sequential generation starting from 1)

## Detailed Results

For complete benchmark logs including warmup phases, GC statistics, and detailed timing breakdowns, see the BenchmarkDotNet artifacts in the `BenchmarkDotNet.Artifacts` directory.

---

*Generated from BenchmarkDotNet v0.15.6 results using long ID type*
