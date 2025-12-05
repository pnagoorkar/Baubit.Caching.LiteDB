# Baubit.Caching.LiteDB

[![CircleCI](https://dl.circleci.com/status-badge/img/circleci/TpM4QUH8Djox7cjDaNpup5/BwRtQ3wVYEJLRxGmwPwmZW/tree/master.svg?style=svg)](https://dl.circleci.com/status-badge/redirect/circleci/TpM4QUH8Djox7cjDaNpup5/BwRtQ3wVYEJLRxGmwPwmZW/tree/master)
[![codecov](https://codecov.io/gh/pnagoorkar/Baubit.Caching.LiteDB/branch/master/graph/badge.svg)](https://codecov.io/gh/pnagoorkar/Baubit.Caching.LiteDB)<br/>
[![NuGet](https://img.shields.io/nuget/v/Baubit.Caching.LiteDB.svg)](https://www.nuget.org/packages/Baubit.Caching.LiteDB/)
![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4?logo=dotnet&logoColor=white)<br/>
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Known Vulnerabilities](https://snyk.io/test/github/pnagoorkar/Baubit.Caching.LiteDB/badge.svg)](https://snyk.io/test/github/pnagoorkar/Baubit.Caching.LiteDB)

LiteDB-backed L2 store implementation for [Baubit.Caching](https://github.com/pnagoorkar/Baubit.Caching).

## Installation

```bash
dotnet add package Baubit.Caching.LiteDB
```

## Usage

### Creating the Store

```csharp
using Baubit.Caching.LiteDB;
using Microsoft.Extensions.Logging;

// Create an uncapped store
var store = new Store<string>(
    "cache.db",           // Database file path
    "myCollection",       // Collection name
    loggerFactory);

// Create a capped store
var cappedStore = new Store<string>(
    "cache.db",
    "myCollection",
    minCap: 100,          // Minimum capacity
    maxCap: 1000,         // Maximum capacity
    loggerFactory);

// Use with existing LiteDatabase instance
using var db = new LiteDatabase("cache.db");
var sharedStore = new Store<string>(db, "myCollection", loggerFactory);
```

### Creating IOrderedCache with LiteDB L2 Store

```csharp
using Baubit.Caching;
using Baubit.Caching.InMemory;
using Baubit.Caching.LiteDB;
using Microsoft.Extensions.Logging;

var config = new Configuration { EvictAfterEveryX = 100 };
var metadata = new Metadata { Configuration = config };

// L1: In-memory store (bounded, fast)
var l1Store = new Baubit.Caching.InMemory.Store<string>(100, 1000, loggerFactory);

// L2: LiteDB store (unbounded, persistent)
var l2Store = new Baubit.Caching.LiteDB.Store<string>("cache.db", "entries", loggerFactory);

using var cache = new OrderedCache<string>(config, l1Store, l2Store, metadata, loggerFactory);

// Add entries
cache.Add("value", out var entry);

// Read entries
cache.GetEntryOrDefault(entry.Id, out var retrieved);

// Async enumeration
await foreach (var item in cache)
{
    Console.WriteLine(item.Value);
}
```

### Basic Store Operations

```csharp
// Add
store.Add(Guid.NewGuid(), "value", out var entry);

// Get
store.GetValueOrDefault(id, out var value);
store.GetEntryOrDefault(id, out var entry);

// Update
store.Update(id, "new value");

// Remove
store.Remove(id, out var removed);

// Count
store.GetCount(out var count);
```

## Features

- Persistent file-based storage via LiteDB
- Thread-safe operations
- Capacity management (min/max/target)
- GuidV7 time-ordered identifier support
- Head/tail tracking for ordered iteration

## License

MIT