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

LiteDB-backed L2 store implementation for [Baubit.Caching](https://github.com/pnagoorkar/Baubit.Caching) with support for custom ID types and custom ID generation strategies.

## Installation

```bash
dotnet add package Baubit.Caching.LiteDB
```

## Features

- **Generic ID Support**: Use `long`, `int`, `Guid`, or any value type implementing `IComparable<TId>` and `IEquatable<TId>`
- **Custom ID Generation**: Provide custom `nextIdFactory` functions for any ID generation strategy
- **Performance Optimized**: Numeric IDs (long/int) deliver better performance than Guid - less memory, better cache locality
- **Persistent Storage**: File-based LiteDB storage for durable caching
- **Default ID Factories**: Built-in sequential generation for int/long, GuidV7 generation for Guid
- **Resumable Enumeration**: Resume async enumeration sessions across application restarts with configurable persistence
- **Capacity Management**: Support for bounded and unbounded stores

## Backward Compatibility

The `StoreGuid<TValue>`, `StoreLong<TValue>`, and `StoreInt<TValue>` classes maintain backward compatibility with existing code. They provide default ID generation strategies while also supporting custom `nextIdFactory` functions for advanced scenarios. The base `Store<TId, TValue>` class requires a `nextIdFactory` parameter, enabling any custom ID generation logic.

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

### Store with Default ID Generation

```csharp
using Baubit.Caching.LiteDB;
using Microsoft.Extensions.Logging;

// Store with long IDs - sequential generation (1, 2, 3...)
var storeLong = new StoreLong<string>(
    "cache.db",
    "myCollection",
    loggerFactory);

storeLong.Add("value", out var entry);  // Auto-generated ID: 1
storeLong.GetValueOrDefault(1L, out var value);

// Store with int IDs - sequential generation
var storeInt = new StoreInt<string>(
    "cache.db",
    "intCollection",
    loggerFactory);

storeInt.Add("value", out var entry);  // Auto-generated ID: 1

// Store with Guid IDs - automatic GuidV7 generation
var storeGuid = new StoreGuid<string>(
    "cache.db",
    "guidCollection",
    loggerFactory);

storeGuid.Add("value", out var entry);  // Auto-generated GuidV7
```

### Store with Custom ID Generation

```csharp
using Baubit.Caching.LiteDB;
using Microsoft.Extensions.Logging;

// Custom long ID generation - start from 1000, increment by 10
var storeLong = new StoreLong<string>(
    "cache.db",
    "customCollection",
    lastId => lastId.HasValue ? lastId.Value + 10 : 1000,
    loggerFactory);

storeLong.Add("value", out var entry);  // ID: 1000
storeLong.Add("value2", out var entry2); // ID: 1010

// Custom Guid ID generation with custom identity generator
var customGenerator = Baubit.Identity.IdentityGenerator.CreateNew();
var storeGuid = new StoreGuid<string>(
    "cache.db",
    "guidCollection",
    customGenerator,
    loggerFactory);

// Direct use of Store<TId, TValue> with fully custom ID type and factory
var customStore = new Store<long, string>(
    "cache.db",
    "fullCustom",
    lastId => lastId.HasValue ? lastId.Value * 2 : 1,
    loggerFactory);
```

**Future Enumeration:**

For scenarios where you want to wait for new entries to be added to the cache:

```csharp
// Create a future enumerator that starts from the end of the cache
var futureEnumerator = cache.GetFutureAsyncEnumerator("future-session", CancellationToken.None);

// This waits for new entries to be added
while (await futureEnumerator.MoveNextAsync())
{
    // Process newly added entries
    Console.WriteLine($"New entry: {futureEnumerator.Current.Value}");
}
```

Future enumerators also support session resumption when `ResumeSession` is enabled.

### Resumable Enumeration

Enumeration sessions can be persisted to LiteDB and resumed after application restarts:

```csharp
using Baubit.Caching;
using Baubit.Caching.InMemory;
using Baubit.Caching.LiteDB;
using LiteDB;
using Microsoft.Extensions.Logging;

// Configure resumable enumeration
var config = new Baubit.Caching.LiteDB.Configuration
{
    ResumeSession = true,
    PersistPositionEveryXMoves = 10,  // Persist every 10 moves (0 = disabled)
    PersistPositionBeforeMove = true  // Persist before move (crash recovery)
};

// Create database shared between store and enumerator factory
using var database = new LiteDatabase("cache.db");
var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();

// Create store and metadata
var store = new StoreGuid<string>(database, "myCollection", identityGenerator, loggerFactory);
var metadata = new Metadata<Guid>(config, loggerFactory);

// Create enumerator factory with same database and configuration
var enumeratorFactory = new CacheAsyncEnumeratorFactory<Guid, string>(database, config);
Func<Baubit.Caching.CacheEnumeratorCollection<Guid>> enumCollFactory =
                () => new LiteDB.CacheEnumeratorCollection<Guid>(config, database);

// Create cache with enumerator factory
var cache = new OrderedCache<Guid, string>(
    config,
    l1Store: null,  // Optional L1 cache
    l2Store: store,
    metadata: metadata,
    loggerFactory: loggerFactory,
    enumeratorCollectionFactory: enumCollFactory, // required if ResumeSession = true; null ok otherwise
    enumeratorFactory: enumeratorFactory);

// First enumeration session
var enumerator = cache.GetFutureAsyncEnumerator("my-session", CancellationToken.None);
await enumerator.MoveNextAsync();  // First entry
await enumerator.MoveNextAsync();  // Second entry
cache.Dispose();   // Position persisted

// Resume from saved position (even after application restart)
var enumerator = cache.GetFutureAsyncEnumerator("my-session", CancellationToken.None);
await enumerator.MoveNextAsync();  // Continues from third entry
```

**Configuration Options:**
- `ResumeSession` - Enable/disable session resumption (default: `false`)
- `PersistPositionEveryXMoves` - Persist every X moves, 0 to disable (default: `0`)
- `PersistPositionBeforeMove` - Persist before move for crash recovery, false for after move (default: `true`)

**How It Works:**
- Enumerator positions are persisted to a `_enumerator_positions` collection in the same LiteDB database
- Each enumeration session is identified by a unique session ID string
- When `ResumeSession` is enabled, the factory checks for saved positions and resumes from there
- Persistence frequency is controlled by `PersistPositionEveryXMoves` to balance reliability vs. I/O overhead
- `PersistPositionBeforeMove` determines whether position is saved before or after moving to the next entry

**Eviction Protection:**
- When `ResumeSession` is enabled, the cache eviction respects both active and persisted enumerator positions
- Entries will not be evicted if any enumerator (active or persisted) is still positioned at or before them
- This ensures safe resumption even if the application restarts while enumeration is in progress
- Disable `ResumeSession` if you want eviction to only consider active in-memory enumerators

### Creating the Store

```csharp
using Baubit.Caching.LiteDB;
using Microsoft.Extensions.Logging;

// Uncapped store with default Guid generation
var store = new StoreGuid<string>(
    "cache.db",
    "myCollection",
    loggerFactory);

// Capped store with custom ID factory
var cappedStore = new StoreGuid<string>(
    "cache.db",
    "myCollection",
    minCap: 100,
    maxCap: 1000,
    Baubit.Identity.IdentityGenerator.CreateNew(),
    loggerFactory);

// Store with custom ID type and custom next ID factory
var longStore = new StoreLong<string>(
    "cache.db",
    "longCollection",
    lastId => lastId.HasValue ? lastId.Value + 5 : 100,
    loggerFactory);

// Use with existing LiteDatabase instance
using var db = new LiteDatabase("cache.db");
var sharedStore = new StoreGuid<string>(db, "myCollection", loggerFactory);
```

### Basic Store Operations

```csharp
// Store with long IDs - auto-generates sequential IDs
var storeLong = new StoreLong<string>("cache.db", "col", loggerFactory);
storeLong.Add("value", out var entry);  // Auto-generated ID
storeLong.GetValueOrDefault(entry.Id, out var value);
storeLong.Update(entry.Id, "new value");
storeLong.Remove(entry.Id, out var removed);
storeLong.GetCount(out var count);

// Store with Guid IDs - auto-generates GuidV7 IDs
var storeGuid = new StoreGuid<string>("cache.db", "guidCol", loggerFactory);
storeGuid.Add("value", out var entryGuid);  // ID auto-generated
storeGuid.GetCount(out var countGuid);

// Explicit ID still supported
storeLong.Add(999L, "explicit", out var explicitEntry);
storeGuid.Add(Guid.NewGuid(), "explicit", out var explicitGuidEntry);
```

## License

MIT
