# Baubit.Caching.LiteDB

[![CircleCI](https://dl.circleci.com/status-badge/img/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/master.svg?style=svg)](https://dl.circleci.com/status-badge/redirect/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/master)
[![codecov](https://codecov.io/gh/pnagoorkar/Baubit.Caching.LiteDB/branch/master/graph/badge.svg)](https://codecov.io/gh/pnagoorkar/Baubit.Caching.LiteDB)<br/>
[![NuGet](https://img.shields.io/nuget/v/Baubit.Caching.LiteDB.svg)](https://www.nuget.org/packages/Baubit.Caching.LiteDB/)
[![NuGet](https://img.shields.io/nuget/dt/Baubit.Caching.LiteDB.svg)](https://www.nuget.org/packages/Baubit.Caching.LiteDB) <br/>
![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4?logo=dotnet&logoColor=white)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)<br/>
[![Known Vulnerabilities](https://snyk.io/test/github/pnagoorkar/Baubit.Caching.LiteDB/badge.svg)](https://snyk.io/test/github/pnagoorkar/Baubit.Caching.LiteDB)

**DI Extension**: [Baubit.Caching.LiteDB.DI](https://github.com/pnagoorkar/Baubit.Caching.LiteDB.DI)  
**Distributed cache samples**: [Samples](https://github.com/pnagoorkar/Baubit.Caching.DI/tree/master/Samples)

LiteDB-backed L2 store implementation for [Baubit.Caching](https://github.com/pnagoorkar/Baubit.Caching) with support for custom ID types.

## Installation

```bash
dotnet add package Baubit.Caching.LiteDB
```

## Features

- **Generic ID Support**: Use `long`, `int`, `Guid`, or any value type implementing `IComparable<TId>` and `IEquatable<TId>`
- **Performance Optimized**: Numeric IDs (long/int) deliver better performance than Guid - less memory, better cache locality
- **Persistent Storage**: File-based LiteDB storage for durable caching
- **Automatic ID Generation**: Built-in GuidV7 generation for `StoreGuid<TValue>` (backward compatible)
- **Thread-Safe**: All public APIs are thread-safe
- **Capacity Management**: Support for bounded and unbounded stores

## Backward Compatibility

The `StoreGuid<TValue>`, `StoreLong<TValue>`, and `StoreInt<TValue>` classes were added to maintain backward compatibility with code written against Baubit.Caching v2025.52+. These classes provide concrete implementations that handle ID generation internally, simplifying usage for common scenarios while the abstract `Store<TId, TValue>` base class enables custom ID type implementations.

## Performance

Benchmarks show numeric IDs (`long`, `int`) deliver better performance than `Guid`:

| Operation (ops/sec)             | GuidV7          | Long            | Long advantage |
|---------------------------------|----------------:|----------------:|---------------:|
| GetFirstOrDefault               | **14.9M–15.5M** | **19.6M–22.8M** | **+26–53%**    |
| GetEntryOrDefault               | **81.1k–81.5k** | **82.7k–110.0k**| **+1–36%**     |
| GetNextOrDefault                | **76.7k–85.3k** | **93.9k–95.9k** | **+10–25%**    |
| Update                          | **21.8k–25.6k** | **22.4k–30.5k** | **+3–19%**     |
| Add                             | **12.6k–12.8k** | **17.0k–19.0k** | **+35–49%**    |
| Mixed (50% read / 50% write)    | **9.4k–10.3k**  | **13.6k–14.3k** | **+32–53%**    |
| Mixed (80% read / 20% write)    | **7.0k–7.3k**   | **9.1k–9.5k**   | **+24–36%**    |
| Memory (ID size only)           | **16 bytes**    | **8 bytes**     | **50% less**   |

Long IDs are faster due to:
- Smaller key size (8 bytes vs 16) improves cache locality and reduces index overhead
- Faster comparisons and hashing for numeric keys
- Especially noticeable on write-heavy and mixed workloads

Run benchmarks locally:

```bash
dotnet run -c Release --project Baubit.Caching.LiteDB.Benchmark
```

See [Baubit.Caching.LiteDB.Benchmark/Results.md](Baubit.Caching.LiteDB.Benchmark/Results.md) for detailed benchmarks.

## Usage

### Store with Custom ID Types

```csharp
using Baubit.Caching.LiteDB;
using Microsoft.Extensions.Logging;

// Store with long IDs - recommended for best performance
var storeLong = new StoreLong<string>(
    "cache.db",
    "myCollection",
    loggerFactory);

storeLong.Add(1L, "value", out var entry);
storeLong.GetValueOrDefault(1L, out var value);

// Store with int IDs
var storeInt = new StoreInt<string>(
    "cache.db",
    "intCollection",
    loggerFactory);

storeInt.Add(42, "value", out var entry);

// Store with Guid IDs - uses automatic GuidV7 generation
var storeGuid = new StoreGuid<string>(
    "cache.db",
    "guidCollection",
    loggerFactory);

// Auto-generated ID
storeGuid.Add("value", out var entry1);

// Or provide explicit Guid
storeGuid.Add(Guid.NewGuid(), "value", out var entry2);
```

### Creating the Store

```csharp
using Baubit.Caching.LiteDB;
using Microsoft.Extensions.Logging;

// Uncapped store with automatic Guid generation
var store = new StoreGuid<string>(
    "cache.db",
    "myCollection",
    loggerFactory);

// Capped store
var cappedStore = new StoreGuid<string>(
    "cache.db",
    "myCollection",
    minCap: 100,
    maxCap: 1000,
    loggerFactory);

// Store with custom ID type - long
var longStore = new StoreLong<string>(
    "cache.db",
    "longCollection",
    loggerFactory);

// Use with existing LiteDatabase instance
using var db = new LiteDatabase("cache.db");
var sharedStore = new StoreGuid<string>(db, "myCollection", loggerFactory);
```

### Basic Store Operations

```csharp
// Store with long IDs - explicit ID required
var storeLong = new StoreLong<string>("cache.db", "col", loggerFactory);
storeLong.Add(1L, "value", out var entry);
storeLong.GetValueOrDefault(1L, out var value);
storeLong.Update(1L, "new value");
storeLong.Remove(1L, out var removed);
storeLong.GetCount(out var count);

// Store with Guid IDs - auto-generates IDs
var storeGuid = new StoreGuid<string>("cache.db", "guidCol", loggerFactory);
storeGuid.Add("value", out var entryGuid);  // ID auto-generated
storeGuid.GetCount(out var countGuid);
```

## License

MIT
