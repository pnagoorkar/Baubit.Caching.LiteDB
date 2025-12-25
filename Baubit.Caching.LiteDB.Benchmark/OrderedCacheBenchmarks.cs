using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using LiteDB;

namespace Baubit.Caching.LiteDB.Benchmark;

// Helper classes for long ID support - must be defined first
internal class InMemoryStoreLong : Baubit.Caching.InMemory.Store<long, string>
{
    private long _nextId = 1;

    public InMemoryStoreLong(long? minCap, long? maxCap, ILoggerFactory loggerFactory)
        : base(minCap, maxCap, loggerFactory)
    {
    }

    protected override long? GenerateNextId(long? lastGeneratedId)
    {
        return lastGeneratedId.HasValue ? lastGeneratedId.Value + 1 : _nextId++;
    }
}

internal class LiteDBStoreLong : Baubit.Caching.LiteDB.Store<long, string>
{
    public LiteDBStoreLong(string databasePath, string collectionName, ILoggerFactory loggerFactory) : base(databasePath, collectionName, loggerFactory)
    {
    }

    public LiteDBStoreLong(LiteDatabase database, string collectionName, ILoggerFactory loggerFactory) : base(database, collectionName, loggerFactory)
    {
    }

    public LiteDBStoreLong(string databasePath, string collectionName, long? minCap, long? maxCap, ILoggerFactory loggerFactory) : base(databasePath, collectionName, minCap, maxCap, loggerFactory)
    {
    }

    public LiteDBStoreLong(LiteDatabase database, string collectionName, long? minCap, long? maxCap, ILoggerFactory loggerFactory) : base(database, collectionName, minCap, maxCap, loggerFactory)
    {
    }

    protected override long? GenerateNextId(long? lastGeneratedId)
    {
        return lastGeneratedId + 1;
    }
}

internal class OrderedCacheLong : Baubit.Caching.OrderedCache<long, string>
{
    public OrderedCacheLong(
        Configuration cacheConfiguration,
        IStore<long, string> l1Store,
        IStore<long, string> l2Store,
        IMetadata<long> metadata,
        ILoggerFactory loggerFactory)
        : base(cacheConfiguration, l1Store, l2Store, metadata, loggerFactory)
    {
    }
}

/// <summary>
/// Benchmarks OrderedCache throughput with LiteDB L2 store using long IDs.
/// Reports operations per second for practical performance assessment.
/// Demonstrates performance characteristics of using long vs Guid for ID type.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10, invocationCount: 10000)]
[RankColumn]
public class OrderedCacheBenchmarks
{
    private OrderedCacheLong? _cache;
    private LiteDB.Store<long, string>? _l2Store;
    private readonly List<long> _entryIds = new();
    private int _readIndex = 0;
    private string? _dbPath;
    private long _nextId = 1;

    [Params(1_000, 10_000)]
    public int CacheSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"benchmark_long_{Guid.NewGuid()}.db");

        var config = new Configuration
        {
            RunAdaptiveResizing = false,
            EvictAfterEveryX = int.MaxValue
        };

        var metadata = new InMemory.Metadata<long>(config, NullLoggerFactory.Instance);
        var l1Store = new InMemoryStoreLong(CacheSize / 10, CacheSize / 10, NullLoggerFactory.Instance);
        _l2Store = new LiteDBStoreLong(_dbPath, "benchmark", NullLoggerFactory.Instance);

        _cache = new OrderedCacheLong(
            config,
            l1Store,
            _l2Store,
            metadata,
            NullLoggerFactory.Instance
        );

        // Pre-populate for read tests
        _entryIds.Clear();
        for (int i = 0; i < CacheSize; i++)
        {
            if (_cache.Add($"Value_{i}", out var entry))
            {
                _entryIds.Add(entry.Id);
            }
        }
    }

    [Benchmark(Description = "Read-Only: GetEntryOrDefault")]
    public void ReadOnly_Get()
    {
        var id = _entryIds[_readIndex++ % _entryIds.Count];
        _cache!.GetEntryOrDefault(id, out _);
    }

    [Benchmark(Description = "Read-Only: GetFirstOrDefault")]
    public void ReadOnly_GetFirst()
    {
        _cache!.GetFirstOrDefault(out _);
    }

    [Benchmark(Description = "Read-Only: GetNextOrDefault")]
    public void ReadOnly_GetNext()
    {
        var id = _entryIds[_readIndex++ % _entryIds.Count];
        _cache!.GetNextOrDefault(id, out _);
    }

    [Benchmark(Description = "Write-Only: Add")]
    public void WriteOnly_Add()
    {
        _cache!.Add($"NewValue_{_nextId++}", out _);
    }

    [Benchmark(Description = "Write-Only: Update")]
    public void WriteOnly_Update()
    {
        var id = _entryIds[_readIndex++ % _entryIds.Count];
        _cache!.Update(id, $"Updated_{_nextId++}");
    }

    [Benchmark(Description = "Mixed: 80% Read, 20% Write")]
    public void Mixed_80Read_20Write()
    {
        // 4 reads
        for (int i = 0; i < 4; i++)
        {
            var id = _entryIds[_readIndex++ % _entryIds.Count];
            _cache!.GetEntryOrDefault(id, out _);
        }

        // 1 write
        _cache!.Add($"Mixed_{_nextId++}", out _);
    }

    [Benchmark(Description = "Mixed: 50% Read, 50% Write")]
    public void Mixed_50Read_50Write()
    {
        // 1 read
        var id = _entryIds[_readIndex++ % _entryIds.Count];
        _cache!.GetEntryOrDefault(id, out _);

        // 1 write
        _cache!.Add($"Mixed_{_nextId++}", out _);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _cache?.Dispose();
        _l2Store?.Dispose();
        
        // Clean up database file
        if (_dbPath != null && File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { }
        }
    }
}
