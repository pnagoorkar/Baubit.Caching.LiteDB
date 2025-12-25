# Baubit.Caching.LiteDB

[![CircleCI](https://dl.circleci.com/status-badge/img/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/master.svg?style=svg)](https://dl.circleci.com/status-badge/redirect/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/master)
[![codecov](https://codecov.io/gh/pnagoorkar/Baubit.Caching.LiteDB/branch/master/graph/badge.svg)](https://codecov.io/gh/pnagoorkar/Baubit.Caching.LiteDB)<br/>
[![NuGet](https://img.shields.io/nuget/v/Baubit.Caching.LiteDB.svg)](https://www.nuget.org/packages/Baubit.Caching.LiteDB/)
[![NuGet](https://img.shields.io/nuget/dt/Baubit.Caching.LiteDB.svg)](https://www.nuget.org/packages/Baubit.Caching.LiteDB) <br/>
![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4?logo=dotnet&logoColor=white)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)<br/>
[![Known Vulnerabilities](https://snyk.io/test/github/pnagoorkar/Baubit.Caching.LiteDB/badge.svg)](https://snyk.io/test/github/pnagoorkar/Baubit.Caching.LiteDB)

**DI Extension**: [Baubit.Caching.LiteDB.DI](https://github.com/pnagoorkar/Baubit.Caching.LiteDB.DI)

LiteDB-backed L2 store implementation for [Baubit.Caching](https://github.com/pnagoorkar/Baubit.Caching) with support for custom ID types.

## Installation

```bash
dotnet add package Baubit.Caching.LiteDB
```

## Features

- **Generic ID Support**: Use `long`, `int`, `Guid`, or any value type implementing `IComparable<TId>` and `IEquatable<TId>`
- **Persistent Storage**: File-based LiteDB storage for durable caching
- **Automatic ID Generation**: Built-in GuidV7 generation for `Store<TValue>` (backward compatible)
- **Thread-Safe**: All public APIs are thread-safe
- **Capacity Management**: Support for bounded and unbounded stores

## Usage

### Store with Custom ID Types

```csharp
using Baubit.Caching.LiteDB;
using Microsoft.Extensions.Logging;

// Store with long IDs
var storeLong = new Store<long, string>(
    "cache.db",
    "myCollection",
    loggerFactory);

storeLong.Add(1L, "value", out var entry);
storeLong.GetValueOrDefault(1L, out var value);

// Store with int IDs
var storeInt = new Store<int, string>(
    "cache.db",
    "intCollection",
    loggerFactory);

storeInt.Add(42, "value", out var entry);

// Store with Guid IDs - uses automatic GuidV7 generation
var storeGuid = new Store<string>(
    "cache.db",
    "guidCollection",
    loggerFactory);

// Provide explicit Guid if needed, or use identity generator for auto-generation
storeGuid.Add(Guid.NewGuid(), "value", out var entryGuid);
```

### Creating the Store

```csharp
using Baubit.Caching.LiteDB;
using Microsoft.Extensions.Logging;

// Uncapped store with automatic Guid generation
var store = new Store<string>(
    "cache.db",
    "myCollection",
    loggerFactory);

// Capped store
var cappedStore = new Store<string>(
    "cache.db",
    "myCollection",
    minCap: 100,
    maxCap: 1000,
    loggerFactory);

// Store with custom ID type (no automatic generation)
var longStore = new Store<long, string>(
    "cache.db",
    "longCollection",
    loggerFactory);

// Use with existing LiteDatabase instance
using var db = new LiteDatabase("cache.db");
var sharedStore = new Store<string>(db, "myCollection", loggerFactory);
```

### Basic Store Operations

```csharp
// Generic store with long IDs - explicit ID required
var storeLong = new Store<long, string>("cache.db", "col", loggerFactory);
storeLong.Add(1L, "value", out var entry);
storeLong.GetValueOrDefault(1L, out var value);
storeLong.Update(1L, "new value");
storeLong.Remove(1L, out var removed);
storeLong.GetCount(out var count);

// Guid store - explicit ID required
var storeGuid = new Store<string>("cache.db", "guidCol", loggerFactory);
storeGuid.Add(Guid.NewGuid(), "value", out var entryGuid);
storeGuid.GetCount(out var countGuid);

// Head and Tail IDs (smallest and largest)
var headId = storeLong.HeadId;
var tailId = storeLong.TailId;
```

## Performance

Using numeric ID types (`long`, `int`) provides performance benefits over `Guid`:
- Smaller memory footprint (8 bytes for `long` vs 16 bytes for `Guid`)
- Faster comparisons and indexing
- Better cache locality

Run benchmarks locally to see performance characteristics for your workload:

```bash
dotnet run -c Release --project Baubit.Caching.LiteDB.Benchmark
```

## License

MIT
