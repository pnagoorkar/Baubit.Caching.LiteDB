# Benchmark Results

**Date:** December 5, 2025  
**Runtime:** .NET 9.0.11 (9.0.11, 9.0.1125.51716), X64 RyuJIT x86-64-v3  
**Hardware:** Intel Core Ultra 9 185H 2.50GHz, 1 CPU, 22 logical and 16 physical cores  
**OS:** Windows 11 (10.0.26200.7309)  
**GC:** Non-concurrent Workstation

## Performance Summary

### Cache Size: 1,000 Entries

| Operation                      | Mean (?s) | Ops/sec    | StdDev (?s) | Allocated |
|--------------------------------|----------:|-----------:|------------:|----------:|
| Read-Only: GetFirstOrDefault   |     67.25 | 14,870,067 |       3.603 |       0 B |
| Read-Only: GetNextOrDefault    | 11,719.91 |     85,325 |   1,913.885 |  26,923 B |
| Read-Only: GetEntryOrDefault   | 12,332.35 |     81,088 |     845.296 |  26,923 B |
| Write-Only: Update             | 38,992.02 |     25,647 |     613.176 |  48,083 B |
| Write-Only: Add                | 79,368.86 |     12,599 |   1,894.366 |  89,719 B |
| Mixed: 50% Read, 50% Write     | 96,762.93 |     10,335 |   2,228.674 | 121,987 B |
| Mixed: 80% Read, 20% Write     |137,378.97 |      7,279 |   2,074.464 | 219,346 B |

### Cache Size: 10,000 Entries

| Operation                      | Mean (?s) | Ops/sec    | StdDev (?s) | Allocated |
|--------------------------------|----------:|-----------:|------------:|----------:|
| Read-Only: GetFirstOrDefault   |     64.37 | 15,535,367 |       3.133 |       0 B |
| Read-Only: GetEntryOrDefault   | 12,265.69 |     81,527 |     169.629 |  37,471 B |
| Read-Only: GetNextOrDefault    | 13,039.94 |     76,686 |     427.160 |  37,471 B |
| Write-Only: Update             | 45,954.95 |     21,760 |     229.767 |  70,049 B |
| Write-Only: Add                | 78,203.58 |     12,787 |   1,896.970 |  78,019 B |
| Mixed: 50% Read, 50% Write     |106,811.01 |      9,362 |   5,644.325 | 115,039 B |
| Mixed: 80% Read, 20% Write     |142,846.42 |      6,999 |   2,081.224 | 226,730 B |

## Key Findings

### Read Performance
- **GetFirstOrDefault** is the fastest operation at ~15 million ops/sec with zero allocations
- **GetEntryOrDefault** and **GetNextOrDefault** perform similarly at ~80,000 ops/sec
- Read performance is consistent across both cache sizes

### Write Performance
- **Update** operations (~22,000-26,000 ops/sec) are significantly faster than **Add** operations (~12,500 ops/sec)
- Add operations require more memory allocation due to index maintenance and head/tail tracking
- Write performance remains stable as cache size increases

### Mixed Workloads
- **80% Read, 20% Write** workload: ~7,000 ops/sec
- **50% Read, 50% Write** workload: ~10,000 ops/sec
- Mixed workloads show increased memory allocation due to GC pressure from combined operations

### Memory Characteristics
- GetFirstOrDefault has zero allocations, making it ideal for high-frequency head access
- Entry retrieval operations allocate ~27-37 KB per 10,000 operations
- Write operations allocate more memory for database persistence
- Mixed workloads generate the most allocations (115-227 KB per 10,000 operations)

## Benchmark Configuration

- **Invocation Count:** 10,000 operations per iteration
- **Iteration Count:** 10
- **Warmup Count:** 3
- **Confidence Interval:** 99.9%

## Detailed Results

For complete benchmark logs including warmup phases, GC statistics, and detailed timing breakdowns, see the BenchmarkDotNet artifacts in the `BenchmarkDotNet.Artifacts` directory.

---

*Generated from BenchmarkDotNet v0.15.6 results*
