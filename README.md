# Baubit.Caching.LiteDB

[![CircleCI](https://dl.circleci.com/status-badge/img/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/master.svg?style=svg)](https://dl.circleci.com/status-badge/redirect/circleci/TpM4QUH8Djox7cjDaNpup5/2zTgJzKbD2m3nXCf5LKvqS/tree/master)
[![codecov](https://codecov.io/gh/pnagoorkar/Baubit.Caching.LiteDB/branch/master/graph/badge.svg)](https://codecov.io/gh/pnagoorkar/Baubit.Caching.LiteDB)<br/>
[![NuGet](https://img.shields.io/nuget/v/Baubit.Caching.LiteDB.svg)](https://www.nuget.org/packages/Baubit.Caching.LiteDB/)
[![NuGet](https://img.shields.io/nuget/dt/Baubit.Caching.LiteDB.svg)](https://www.nuget.org/packages/Baubit.Caching.LiteDB) <br/>
![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4?logo=dotnet&logoColor=white)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)<br/>
[![Known Vulnerabilities](https://snyk.io/test/github/pnagoorkar/Baubit.Caching.LiteDB/badge.svg)](https://snyk.io/test/github/pnagoorkar/Baubit.Caching.LiteDB)

LiteDB-backed persistent store for [Baubit.Caching](https://github.com/pnagoorkar/Baubit.Caching) with support for custom ID types and resumable async enumeration.

## Installation

```bash
dotnet add package Baubit.Caching.LiteDB
```

## Features

- **Generic ID Support**: Use `long`, `int`, `Guid`, or any value type implementing `IComparable<TId>` and `IEquatable<TId>`
- **Persistent Storage**: File-based LiteDB storage for durable caching
- **Resumable Enumeration**: Resume async enumeration sessions across application restarts with configurable persistence
- **Thread-Safe**: All public APIs are thread-safe
- **Performance**: Numeric IDs (long/int) deliver 25-50% better performance than Guid

## Quick Start

```csharp
using Baubit.Caching.LiteDB;
using Microsoft.Extensions.Logging;

// Store with long IDs - best performance
var store = new StoreLong<string>("cache.db", "myCollection", loggerFactory);
store.Add(1L, "value", out var entry);

// Store with Guid IDs - auto-generates IDs
var storeGuid = new StoreGuid<string>("cache.db", "guidCollection", loggerFactory);
storeGuid.Add("value", out var entry);

// Resumable enumeration
using var database = new LiteDatabase("cache.db");
var config = new Baubit.Caching.LiteDB.Configuration 
{ 
    ResumeSession = true,
    PersistPositionEveryXMoves = 10  // Persist every 10 moves
};
var factory = new CacheAsyncEnumeratorFactory<Guid, string>(database, config);
var cache = new OrderedCache<Guid, string>(store, factory, loggerFactory);

// Enumerate and resume later
var enumerator = factory.CreateEnumerator(cache, _ => {}, "session-id", CancellationToken.None);
await enumerator.MoveNextAsync();
await enumerator.DisposeAsync();  // Position saved

// Resume from saved position (even after restart)
var enumerator2 = factory.CreateEnumerator(cache, _ => {}, "session-id", CancellationToken.None);
await enumerator2.MoveNextAsync();  // Continues from saved position
```

## Performance

Numeric IDs (`long`, `int`) deliver 25-50% better performance than `Guid`:

| Operation | Long | GuidV7 | Advantage |
|-----------|------|--------|-----------|
| Add | 17.0k-19.0k ops/sec | 12.6k-12.8k ops/sec | **+35-49%** |
| GetFirstOrDefault | 19.6M-22.8M ops/sec | 14.9M-15.5M ops/sec | **+26-53%** |
| Mixed (50/50) | 13.6k-14.3k ops/sec | 9.4k-10.3k ops/sec | **+32-53%** |

Run benchmarks: `dotnet run -c Release --project Baubit.Caching.LiteDB.Benchmark`

## Documentation

- **[DI Extension](https://github.com/pnagoorkar/Baubit.Caching.LiteDB.DI)**: Dependency injection support
- **[Samples](https://github.com/pnagoorkar/Baubit.Caching.DI/tree/master/Samples)**: Distributed cache examples
- **[Benchmark Results](Baubit.Caching.LiteDB.Benchmark/Results.md)**: Detailed performance data

## License

MIT
