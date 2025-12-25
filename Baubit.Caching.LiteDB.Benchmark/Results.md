# Benchmark Results

**Date:** December 24, 2025  
**Runtime:** .NET 9.0 (RyuJIT, X64)  
**Hardware:** Intel Core Ultra 9 185H 2.50GHz, 1 CPU, 22 logical and 16 physical cores  
**OS:** Windows 11  
**GC:** (see BenchmarkDotNet output)

## Performance Summary

### Cache Size: 1,000 Entries

| Operation                      | Mean (ns)   | Ops/sec    | StdDev (ns) | Allocated |
|--------------------------------|------------:|-----------:|------------:|----------:|
| Read-Only: GetFirstOrDefault   |      43.93  | 22,755,857 |      2.954  |       0 B |
| Read-Only: GetNextOrDefault    |  10,659.29  |     93,857 |    322.965  |  23,568 B |
| Read-Only: GetEntryOrDefault   |   9,084.89  |    110,048 |  1,543.769  |  23,568 B |
| Write-Only: Update             |  32,806.97  |     30,471 |    555.366  |  43,671 B |
| Write-Only: Add                |  58,726.69  |     17,035 |  4,889.029  |  28,274 B |
| Mixed: 50% Read, 50% Write     |  73,530.16  |     13,600 |  4,193.284  |  56,908 B |
| Mixed: 80% Read, 20% Write     | 110,494.90  |      9,052 | 15,248.515  | 142,939 B |

### Cache Size: 10,000 Entries

| Operation                      | Mean (ns)   | Ops/sec    | StdDev (ns) | Allocated |
|--------------------------------|------------:|-----------:|------------:|----------:|
| Read-Only: GetFirstOrDefault   |      51.03  | 19,601,647 |      0.928  |       0 B |
| Read-Only: GetNextOrDefault    |  10,432.01  |     95,890 |    304.533  |  33,871 B |
| Read-Only: GetEntryOrDefault   |  12,082.00  |     82,749 |    810.943  |  33,871 B |
| Write-Only: Update             |  44,675.03  |     22,386 |    381.582  |  65,082 B |
| Write-Only: Add                |  52,631.83  |     19,009 |  1,814.444  |  22,922 B |
| Mixed: 50% Read, 50% Write     |  69,923.28  |     14,299 |  1,200.827  |  56,307 B |
| Mixed: 80% Read, 20% Write     | 105,050.41  |      9,520 |  1,311.261  | 156,142 B |

## Key Findings

### Read Performance
- **GetFirstOrDefault** remains the fastest operation at ~20-23 million ops/sec with zero allocations
- **GetEntryOrDefault** and **GetNextOrDefault** perform at ~80,000-110,000 ops/sec
- Read performance is consistent across both cache sizes

### Write Performance
- **Update** operations (~22,000-30,000 ops/sec) are faster than **Add** operations (~17,000-19,000 ops/sec)
- Add operations require more memory allocation
- Write performance is stable as cache size increases

### Mixed Workloads
- **80% Read, 20% Write** workload: ~9,000-9,500 ops/sec
- **50% Read, 50% Write** workload: ~13,600-14,300 ops/sec
- Mixed workloads show increased memory allocation

### Memory Characteristics
- GetFirstOrDefault has zero allocations
- Entry retrieval operations allocate ~23-34 KB per 10,000 operations
- Write operations allocate more memory for persistence
- Mixed workloads generate the most allocations (56-156 KB per 10,000 operations)

## Benchmark Configuration

- **Invocation Count:** 10,000 operations per iteration
- **Iteration Count:** 10
- **Warmup Count:** 3

---

*Generated from BenchmarkDotNet v0.15.6 results*

---

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
