# Baubit.Caching.LiteDB

[![NuGet](https://img.shields.io/nuget/v/Baubit.Caching.LiteDB.svg)](https://www.nuget.org/packages/Baubit.Caching.LiteDB/)
![.NET Standard 2.0](https://img.shields.io/badge/.NET%20Standard-2.0-512BD4?logo=dotnet&logoColor=white)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

LiteDB-backed L2 store implementation for [Baubit.Caching](https://github.com/pnagoorkar/Baubit.Caching).

## Installation

```bash
dotnet add package Baubit.Caching.LiteDB
```

## Usage

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

### Basic Operations

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