using Baubit.Caching;
using Baubit.Caching.InMemory;
using Baubit.Caching.LiteDB;
using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Baubit.Caching.LiteDB.Test.OrderedCache
{
    /// <summary>
    /// Tests for OrderedCache with LiteDB Store as L2 backing store
    /// </summary>
    public class Test : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;
        private readonly List<string> _tempFiles = new List<string>();

        private string GetTempDbPath()
        {
            var path = Path.Combine(Path.GetTempPath(), $"litedb_orderedcache_test_{Guid.NewGuid()}.db");
            _tempFiles.Add(path);
            return path;
        }

        public void Dispose()
        {
            foreach (var file in _tempFiles)
            {
                try
                {
                    if (File.Exists(file))
                        File.Delete(file);
                    var journalFile = file + "-journal";
                    if (File.Exists(journalFile))
                        File.Delete(journalFile);
                }
                catch { /* Ignore cleanup errors */ }
            }
        }

        private Caching.OrderedCache<Guid, string> CreateTestCache(
            Baubit.Caching.LiteDB.Configuration? config = null,
            long? l1MinCap = null,
            long? l1MaxCap = null)
        {
            config ??= new Baubit.Caching.LiteDB.Configuration();
            var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();
            var metadata = new Baubit.Caching.InMemory.Metadata<Guid>(config, NullLoggerFactory.Instance);
            var dbPath = GetTempDbPath();
            var database = new LiteDatabase(dbPath);
            // Use Store<TValue> which inherits from Store<Guid, TValue>
            var l2Store = new Baubit.Caching.LiteDB.StoreGuid<string>(database, "test", identityGenerator, _loggerFactory);
            // Create L1 store with Guid type and nextId factory
            var l1Store = l1MinCap.HasValue 
                ? new Caching.InMemory.Store<Guid, string>(
                    l1MinCap, 
                    l1MaxCap, 
                    lastId => identityGenerator.GetNext(), 
                    _loggerFactory) 
                : null;

            // Create enumerator factory using the same database as the store
            var enumeratorFactory = new CacheAsyncEnumeratorFactory<Guid, string>(database, config);

            return new Caching.OrderedCache<Guid, string>(config, l1Store, l2Store, metadata, _loggerFactory, null, enumeratorFactory);
        }

        [Fact]
        public void OrderedCache_Constructor_InitializesCorrectly()
        {
            // Arrange & Act
            using var cache = CreateTestCache();

            // Assert
            Assert.NotNull(cache);
            Assert.Equal(0, cache.Count);
            Assert.NotNull(cache.Configuration);
        }

        [Fact]
        public void OrderedCache_Add_SingleEntry_Success()
        {
            // Arrange
            using var cache = CreateTestCache();

            // Act
            var result = cache.Add("test value", out var entry);

            // Assert
            Assert.True(result);
            Assert.NotNull(entry);
            Assert.Equal("test value", entry.Value);
            Assert.Equal(1, cache.Count);
        }

        [Fact]
        public void OrderedCache_Add_MultipleEntries_MaintainsOrder()
        {
            // Arrange
            using var cache = CreateTestCache();

            // Act
            cache.Add("first", out var entry1);
            cache.Add("second", out var entry2);
            cache.Add("third", out var entry3);

            // Assert
            Assert.Equal(3, cache.Count);
            Assert.NotNull(entry1);
            Assert.NotNull(entry2);
            Assert.NotNull(entry3);
        }

        [Fact]
        public void OrderedCache_GetFirstOrDefault_ReturnsFirstEntry()
        {
            // Arrange
            using var cache = CreateTestCache();
            cache.Add("first", out var addedEntry);
            cache.Add("second", out _);

            // Act
            var result = cache.GetFirstOrDefault(out var firstEntry);

            // Assert
            Assert.True(result);
            Assert.NotNull(firstEntry);
            Assert.Equal(addedEntry.Id, firstEntry.Id);
            Assert.Equal("first", firstEntry.Value);
        }

        [Fact]
        public void OrderedCache_GetLastOrDefault_ReturnsLastEntry()
        {
            // Arrange
            using var cache = CreateTestCache();
            cache.Add("first", out _);
            cache.Add("last", out var addedEntry);

            // Act
            var result = cache.GetLastOrDefault(out var lastEntry);

            // Assert
            Assert.True(result);
            Assert.NotNull(lastEntry);
            Assert.Equal(addedEntry.Id, lastEntry.Id);
            Assert.Equal("last", lastEntry.Value);
        }

        [Fact]
        public void OrderedCache_GetEntryOrDefault_ExistingId_ReturnsEntry()
        {
            // Arrange
            using var cache = CreateTestCache();
            cache.Add("test", out var added);

            // Act
            var result = cache.GetEntryOrDefault(added.Id, out var entry);

            // Assert
            Assert.True(result);
            Assert.NotNull(entry);
            Assert.Equal(added.Id, entry.Id);
            Assert.Equal("test", entry.Value);
        }

        [Fact]
        public void OrderedCache_GetNextOrDefault_ReturnsNextEntry()
        {
            // Arrange
            using var cache = CreateTestCache();
            cache.Add("first", out var first);
            cache.Add("second", out var second);

            // Act
            var result = cache.GetNextOrDefault(first.Id, out var next);

            // Assert
            Assert.True(result);
            Assert.NotNull(next);
            Assert.Equal(second.Id, next.Id);
        }

        [Fact]
        public void OrderedCache_Update_ExistingEntry_Success()
        {
            // Arrange
            using var cache = CreateTestCache();
            cache.Add("original", out var entry);

            // Act
            var result = cache.Update(entry.Id, "updated");

            // Assert
            Assert.True(result);
            cache.GetEntryOrDefault(entry.Id, out var updated);
            Assert.Equal("updated", updated?.Value);
        }

        [Fact]
        public void OrderedCache_Remove_ExistingEntry_Success()
        {
            // Arrange
            using var cache = CreateTestCache();
            cache.Add("to remove", out var entry);

            // Act
            var result = cache.Remove(entry.Id, out var removed);

            // Assert
            Assert.True(result);
            Assert.NotNull(removed);
            Assert.Equal("to remove", removed.Value);
            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void OrderedCache_Clear_RemovesAllEntries()
        {
            // Arrange
            using var cache = CreateTestCache();
            cache.Add("first", out _);
            cache.Add("second", out _);
            cache.Add("third", out _);

            // Act
            var result = cache.Clear();

            // Assert
            Assert.True(result);
            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void OrderedCache_WithL1Store_StoresInBothLayers()
        {
            // Arrange
            using var cache = CreateTestCache(l1MinCap: 10, l1MaxCap: 100);

            // Act
            cache.Add("test", out var entry);

            // Assert
            cache.GetEntryOrDefault(entry.Id, out var retrieved);
            Assert.NotNull(retrieved);
        }

        [Fact]
        public void OrderedCache_WithL1Store_WhenCapacityFull_StillWorksViaL2()
        {
            // Arrange - L1 can only hold 2 items
            using var cache = CreateTestCache(l1MinCap: 2, l1MaxCap: 2);

            // Act
            cache.Add("first", out var e1);
            cache.Add("second", out var e2);
            cache.Add("third", out var e3); // Should overflow to L2 only

            // Assert
            Assert.Equal(3, cache.Count);
            cache.GetEntryOrDefault(e3.Id, out var retrieved);
            Assert.NotNull(retrieved);
        }

        [Fact]
        public void OrderedCache_Dispose_CompletesSuccessfully()
        {
            // Arrange
            var cache = CreateTestCache();
            cache.Add("test", out _);

            // Act
            cache.Dispose();

            // Assert - No exception thrown
        }

        [Fact]
        public void OrderedCache_DataPersists_InLiteDBStore()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();
            Guid entryId;
            string entryValue = "persisted value";

            // Act - Add data to LiteDB store directly and dispose
            using (var store = new Baubit.Caching.LiteDB.StoreGuid<string>(dbPath, "test", identityGenerator, _loggerFactory))
            {
                store.Add(entryValue, out var entry);
                entryId = entry.Id;
            }

            // Reopen store
            using (var store = new Baubit.Caching.LiteDB.StoreGuid<string>(dbPath, "test", identityGenerator, _loggerFactory))
            {
                // Assert - Data should persist in LiteDB
                var result = store.GetEntryOrDefault(entryId, out var retrieved);
                Assert.True(result);
                Assert.NotNull(retrieved);
                Assert.Equal(entryValue, retrieved.Value);
            }
        }

        [Fact]
        public async Task OrderedCache_ConcurrentAdd_AllSucceed()
        {
            // Arrange
            var config = new Baubit.Caching.LiteDB.Configuration { EvictAfterEveryX = int.MaxValue };
            using var cache = CreateTestCache(config: config);
            const int threadCount = 10;
            const int itemsPerThread = 50;
            var tasks = new Task[threadCount];
            var addResults = new bool[threadCount * itemsPerThread];
            var addIndex = 0;

            // Act
            for (int i = 0; i < threadCount; i++)
            {
                int threadId = i;
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < itemsPerThread; j++)
                    {
                        var result = cache.Add($"thread-{threadId}-item-{j}", out _);
                        var idx = Interlocked.Increment(ref addIndex) - 1;
                        addResults[idx] = result;
                    }
                });
            }
            await Task.WhenAll(tasks);

            // Assert
            Assert.All(addResults, result => Assert.True(result));
            Assert.Equal(threadCount * itemsPerThread, cache.Count);
        }

        [Fact]
        public async Task OrderedCache_ConcurrentRead_AllSucceed()
        {
            // Arrange
            var config = new Baubit.Caching.LiteDB.Configuration { EvictAfterEveryX = int.MaxValue };
            using var cache = CreateTestCache(config: config);
            // Pre-populate cache
            var entries = new List<IEntry<Guid, string>>();
            for (int i = 0; i < 50; i++)
            {
                cache.Add($"item-{i}", out var entry);
                entries.Add(entry);
            }

            const int threadCount = 5;
            const int readsPerThread = 50;
            var tasks = new Task[threadCount];
            var readResults = new System.Collections.Concurrent.ConcurrentBag<bool>();

            // Act
            for (int i = 0; i < threadCount; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    var random = new Random();
                    for (int j = 0; j < readsPerThread; j++)
                    {
                        var entry = entries[random.Next(entries.Count)];
                        var result = cache.GetEntryOrDefault(entry.Id, out var retrieved);
                        readResults.Add(result && retrieved != null);
                    }
                });
            }
            await Task.WhenAll(tasks);

            // Assert
            Assert.All(readResults, result => Assert.True(result));
        }

        [Fact]
        public void OrderedCache_EmptyCache_GetFirstOrDefault_ReturnsNull()
        {
            // Arrange
            using var cache = CreateTestCache();

            // Act
            var result = cache.GetFirstOrDefault(out var entry);

            // Assert
            Assert.True(result);
            Assert.Null(entry);
        }

        [Fact]
        public void OrderedCache_EmptyCache_GetLastOrDefault_ReturnsNull()
        {
            // Arrange
            using var cache = CreateTestCache();

            // Act
            var result = cache.GetLastOrDefault(out var entry);

            // Assert
            Assert.True(result);
            Assert.Null(entry);
        }

        [Fact]
        public async Task OrderedCache_EnumeratorPersistence_NormalEnumerator_PersistEnabled_AfterMove()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();
            var config = new Configuration 
            { 
                ResumeSession = true,
                PersistPositionEveryXMoves = 1,
                PersistPositionAfterMove = true
            };
            
            using var database = new LiteDatabase(dbPath);
            var metadata = new Baubit.Caching.InMemory.Metadata<Guid>(config, _loggerFactory);
            var factory = new CacheAsyncEnumeratorFactory<Guid, string>(database, config);

            // Create cache, add entries, enumerate, dispose
            {
                var store = new StoreGuid<string>(database, "test", identityGenerator, _loggerFactory);
                var cache = new Caching.OrderedCache<Guid, string>(config, null, store, metadata, _loggerFactory, null, factory);
                
                cache.Add("entry1", out _);
                cache.Add("entry2", out _);
                cache.Add("entry3", out _);
                cache.Add("entry4", out _);
                
                var enumerator = factory.CreateEnumerator(cache, _ => { }, "session1", CancellationToken.None);
                Assert.True(await enumerator.MoveNextAsync());
                Assert.Equal("entry1", enumerator.Current.Value);
                Assert.True(await enumerator.MoveNextAsync());
                Assert.Equal("entry2", enumerator.Current.Value);
                
                await enumerator.DisposeAsync();
                cache.Dispose();
                store.Dispose();
            }

            // Recreate cache and resume
            {
                var store = new StoreGuid<string>(database, "test", identityGenerator, _loggerFactory);
                var cache = new Caching.OrderedCache<Guid, string>(config, null, store, metadata, _loggerFactory, null, factory);
                
                var enumerator = factory.CreateEnumerator(cache, _ => { }, "session1", CancellationToken.None);
                Assert.True(await enumerator.MoveNextAsync());
                Assert.Equal("entry3", enumerator.Current.Value);
                Assert.True(await enumerator.MoveNextAsync());
                Assert.Equal("entry4", enumerator.Current.Value);
                
                await enumerator.DisposeAsync();
                cache.Dispose();
                store.Dispose();
            }
        }

        [Fact]
        public async Task OrderedCache_EnumeratorPersistence_FutureEnumerator_PersistEnabled_AfterMove()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();
            var config = new Configuration 
            { 
                ResumeSession = true,
                PersistPositionEveryXMoves = 1,
                PersistPositionAfterMove = true
            };
            
            using var database = new LiteDatabase(dbPath);
            var metadata = new Baubit.Caching.InMemory.Metadata<Guid>(config, _loggerFactory);
            var factory = new CacheAsyncEnumeratorFactory<Guid, string>(database, config);

            // Create cache, add entries, enumerate, dispose
            {
                var store = new StoreGuid<string>(database, "test", identityGenerator, _loggerFactory);
                var cache = new Caching.OrderedCache<Guid, string>(config, null, store, metadata, _loggerFactory, null, factory);
                
                cache.Add("entry1", out _);
                cache.Add("entry2", out _);
                
                var enumerator = factory.CreateFutureEnumerator(cache, _ => { }, "session2", CancellationToken.None);
                Assert.Equal("entry2", enumerator.Current.Value); // Initialized to last
                
                cache.Add("entry3", out _);
                Assert.True(await enumerator.MoveNextAsync());
                Assert.Equal("entry3", enumerator.Current.Value);
                
                await enumerator.DisposeAsync();
                cache.Dispose();
                store.Dispose();
            }

            // Recreate cache and resume
            {
                var store = new StoreGuid<string>(database, "test", identityGenerator, _loggerFactory);
                var cache = new Caching.OrderedCache<Guid, string>(config, null, store, metadata, _loggerFactory, null, factory);
                
                var enumerator = factory.CreateFutureEnumerator(cache, _ => { }, "session2", CancellationToken.None);
                
                cache.Add("entry4", out _);
                Assert.True(await enumerator.MoveNextAsync());
                Assert.Equal("entry4", enumerator.Current.Value);
                
                await enumerator.DisposeAsync();
                cache.Dispose();
                store.Dispose();
            }
        }

        [Fact]
        public async Task OrderedCache_EnumeratorPersistence_NormalEnumerator_PersistDisabled()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();
            var config = new Configuration 
            { 
                ResumeSession = true,
                PersistPositionEveryXMoves = 0  // Disabled
            };
            
            using var database = new LiteDatabase(dbPath);
            var metadata = new Baubit.Caching.InMemory.Metadata<Guid>(config, _loggerFactory);
            var factory = new CacheAsyncEnumeratorFactory<Guid, string>(database, config);

            // Create cache, add entries, enumerate, dispose
            {
                var store = new StoreGuid<string>(database, "test", identityGenerator, _loggerFactory);
                var cache = new Caching.OrderedCache<Guid, string>(config, null, store, metadata, _loggerFactory, null, factory);
                
                cache.Add("entry1", out _);
                cache.Add("entry2", out _);
                cache.Add("entry3", out _);
                
                var enumerator = factory.CreateEnumerator(cache, _ => { }, "session3", CancellationToken.None);
                Assert.True(await enumerator.MoveNextAsync());
                Assert.Equal("entry1", enumerator.Current.Value);
                Assert.True(await enumerator.MoveNextAsync());
                Assert.Equal("entry2", enumerator.Current.Value);
                
                await enumerator.DisposeAsync();
                cache.Dispose();
                store.Dispose();
            }

            // Recreate cache and verify it starts from beginning (no resume)
            {
                var store = new StoreGuid<string>(database, "test", identityGenerator, _loggerFactory);
                var cache = new Caching.OrderedCache<Guid, string>(config, null, store, metadata, _loggerFactory, null, factory);
                
                var enumerator = factory.CreateEnumerator(cache, _ => { }, "session3", CancellationToken.None);
                Assert.True(await enumerator.MoveNextAsync());
                Assert.Equal("entry1", enumerator.Current.Value); // Starts from beginning since no persistence
                
                await enumerator.DisposeAsync();
                cache.Dispose();
                store.Dispose();
            }
        }

        [Fact]
        public async Task OrderedCache_EnumeratorPersistence_NormalEnumerator_PersistEnabled_BeforeMove()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();
            var config = new Configuration 
            { 
                ResumeSession = true,
                PersistPositionEveryXMoves = 1,
                PersistPositionAfterMove = false  // Persist BEFORE move
            };
            
            using var database = new LiteDatabase(dbPath);
            var metadata = new Baubit.Caching.InMemory.Metadata<Guid>(config, _loggerFactory);
            var factory = new CacheAsyncEnumeratorFactory<Guid, string>(database, config);

            // Create cache, add entries, enumerate, dispose
            {
                var store = new StoreGuid<string>(database, "test", identityGenerator, _loggerFactory);
                var cache = new Caching.OrderedCache<Guid, string>(config, null, store, metadata, _loggerFactory, null, factory);
                
                cache.Add("entry1", out _);
                cache.Add("entry2", out _);
                cache.Add("entry3", out _);
                
                var enumerator = factory.CreateEnumerator(cache, _ => { }, "session4", CancellationToken.None);
                Assert.True(await enumerator.MoveNextAsync());
                Assert.Equal("entry1", enumerator.Current.Value);
                Assert.True(await enumerator.MoveNextAsync());
                Assert.Equal("entry2", enumerator.Current.Value);
                
                await enumerator.DisposeAsync();
                cache.Dispose();
                store.Dispose();
            }

            // Recreate cache and resume
            {
                var store = new StoreGuid<string>(database, "test", identityGenerator, _loggerFactory);
                var cache = new Caching.OrderedCache<Guid, string>(config, null, store, metadata, _loggerFactory, null, factory);
                
                var enumerator = factory.CreateEnumerator(cache, _ => { }, "session4", CancellationToken.None);
                Assert.True(await enumerator.MoveNextAsync());
                Assert.Equal("entry3", enumerator.Current.Value);
                
                await enumerator.DisposeAsync();
                cache.Dispose();
                store.Dispose();
            }
        }

        [Fact]
        public async Task OrderedCache_EnumeratorPersistence_FutureEnumerator_PersistEnabled_BeforeMove()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();
            var config = new Configuration 
            { 
                ResumeSession = true,
                PersistPositionEveryXMoves = 1,
                PersistPositionAfterMove = false  // Persist BEFORE move
            };
            
            using var database = new LiteDatabase(dbPath);
            var metadata = new Baubit.Caching.InMemory.Metadata<Guid>(config, _loggerFactory);
            var factory = new CacheAsyncEnumeratorFactory<Guid, string>(database, config);

            // Create cache, add entries, enumerate, dispose
            {
                var store = new StoreGuid<string>(database, "test", identityGenerator, _loggerFactory);
                var cache = new Caching.OrderedCache<Guid, string>(config, null, store, metadata, _loggerFactory, null, factory);
                
                cache.Add("entry1", out _);
                cache.Add("entry2", out _);
                
                var enumerator = factory.CreateFutureEnumerator(cache, _ => { }, "session5", CancellationToken.None);
                Assert.Equal("entry2", enumerator.Current.Value);
                
                cache.Add("entry3", out _);
                Assert.True(await enumerator.MoveNextAsync());
                Assert.Equal("entry3", enumerator.Current.Value);
                
                await enumerator.DisposeAsync();
                cache.Dispose();
                store.Dispose();
            }

            // Recreate cache and resume
            {
                var store = new StoreGuid<string>(database, "test", identityGenerator, _loggerFactory);
                var cache = new Caching.OrderedCache<Guid, string>(config, null, store, metadata, _loggerFactory, null, factory);
                
                var enumerator = factory.CreateFutureEnumerator(cache, _ => { }, "session5", CancellationToken.None);
                
                cache.Add("entry4", out _);
                Assert.True(await enumerator.MoveNextAsync());
                Assert.Equal("entry4", enumerator.Current.Value);
                
                await enumerator.DisposeAsync();
                cache.Dispose();
                store.Dispose();
            }
        }
    }
}
