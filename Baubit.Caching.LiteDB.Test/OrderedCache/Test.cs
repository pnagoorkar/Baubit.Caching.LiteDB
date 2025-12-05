using Baubit.Caching.InMemory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Baubit.Caching.LiteDB.Test.OrderedCache
{
    /// <summary>
    /// Tests for <see cref="Baubit.Caching.OrderedCache{TValue}"/> with LiteDB L2 Store
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

        private Caching.OrderedCache<string> CreateTestCache(
            Caching.Configuration? config = null,
            long? l1MinCap = null,
            long? l1MaxCap = null)
        {
            config ??= new Caching.Configuration();
            var metadata = new Metadata { Configuration = config };
            var dbPath = GetTempDbPath();
            var l2Store = new Store<string>(dbPath, "test", _loggerFactory);
            var l1Store = l1MinCap.HasValue ? new Baubit.Caching.InMemory.Store<string>(l1MinCap, l1MaxCap, _loggerFactory) : null;

            return new Caching.OrderedCache<string>(config, l1Store, l2Store, metadata, _loggerFactory);
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
        public void OrderedCache_GetEntryOrDefault_NonExistingId_ReturnsNull()
        {
            // Arrange
            using var cache = CreateTestCache();
            var nonExistingId = Guid.NewGuid();

            // Act
            var result = cache.GetEntryOrDefault(nonExistingId, out var entry);

            // Assert
            Assert.True(result);
            Assert.Null(entry);
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
        public void OrderedCache_GetFirstIdOrDefault_ReturnsFirstId()
        {
            // Arrange
            using var cache = CreateTestCache();
            cache.Add("first", out var entry);
            cache.Add("second", out _);

            // Act
            var result = cache.GetFirstIdOrDefault(out var firstId);

            // Assert
            Assert.True(result);
            Assert.NotNull(firstId);
            Assert.Equal(entry.Id, firstId);
        }

        [Fact]
        public void OrderedCache_GetLastIdOrDefault_ReturnsLastId()
        {
            // Arrange
            using var cache = CreateTestCache();
            cache.Add("first", out _);
            cache.Add("last", out var entry);

            // Act
            var result = cache.GetLastIdOrDefault(out var lastId);

            // Assert
            Assert.True(result);
            Assert.NotNull(lastId);
            Assert.Equal(entry.Id, lastId);
        }

        [Fact]
        public async Task OrderedCache_GetNextAsync_ExistingNext_ReturnsImmediately()
        {
            // Arrange
            using var cache = CreateTestCache();
            cache.Add("first", out var first);
            cache.Add("second", out var second);

            // Act
            var next = await cache.GetNextAsync(first.Id);

            // Assert
            Assert.NotNull(next);
            Assert.Equal(second.Id, next.Id);
        }

        [Fact]
        public async Task OrderedCache_GetNextAsync_WaitsForNew()
        {
            // Arrange
            using var cache = CreateTestCache();
            cache.Add("first", out var first);

            // Act
            var nextTask = cache.GetNextAsync(first.Id);
            await Task.Delay(50);
            cache.Add("second", out var second);

            var next = await nextTask.WaitAsync(TimeSpan.FromSeconds(1));

            // Assert
            Assert.NotNull(next);
            Assert.Equal(second.Id, next.Id);
        }

        [Fact]
        public async Task OrderedCache_GetFutureFirstOrDefaultAsync_WaitsForNewEntry()
        {
            // Arrange
            using var cache = CreateTestCache();

            // Act
            var futureTask = cache.GetFutureFirstOrDefaultAsync();
            await Task.Delay(50);
            cache.Add("new entry", out var entry);

            var future = await futureTask.WaitAsync(TimeSpan.FromSeconds(1));

            // Assert
            Assert.NotNull(future);
            Assert.Equal(entry.Id, future.Id);
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
        public async Task OrderedCache_GetAsyncEnumerator_EnumeratesEntries()
        {
            // Arrange
            using var cache = CreateTestCache();
            cache.Add("first", out _);
            cache.Add("second", out _);
            cache.Add("third", out _);

            // Act
            var entries = new List<IEntry<string>>();
            await foreach (var entry in cache.WithCancellation(CancellationToken.None))
            {
                entries.Add(entry);
                if (entries.Count >= 3) break;
            }

            // Assert
            Assert.Equal(3, entries.Count);
        }

        [Fact]
        public void OrderedCache_ConcurrentAdd_AllSucceed()
        {
            // Arrange
            using var cache = CreateTestCache();
            const int threadCount = 10;
            const int itemsPerThread = 100;
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
            Task.WaitAll(tasks);

            // Assert
            Assert.All(addResults, result => Assert.True(result));
            Assert.Equal(threadCount * itemsPerThread, cache.Count);
        }

        [Fact]
        public void OrderedCache_ConcurrentRead_AllSucceed()
        {
            // Arrange
            using var cache = CreateTestCache();
            var entries = new List<IEntry<string>>();
            for (int i = 0; i < 100; i++)
            {
                cache.Add($"item-{i}", out var entry);
                entries.Add(entry);
            }

            const int threadCount = 10;
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
            Task.WaitAll(tasks);

            // Assert
            Assert.All(readResults, result => Assert.True(result));
        }

        [Fact]
        public void OrderedCache_ConcurrentMixedReadWrite_NoDeadlock()
        {
            // Arrange
            using var cache = CreateTestCache();
            const int operationCount = 500;
            var tasks = new List<Task>();
            var allSuccessful = true;

            // Act
            for (int i = 0; i < 5; i++)
            {
                int writerId = i;
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < operationCount / 5; j++)
                    {
                        if (!cache.Add($"writer-{writerId}-item-{j}", out _))
                            allSuccessful = false;
                    }
                }));
            }

            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < operationCount; j++)
                    {
                        cache.GetFirstOrDefault(out _);
                        cache.GetLastOrDefault(out _);
                    }
                }));
            }

            var completed = Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(10));

            // Assert
            Assert.True(completed, "Operations should complete without deadlock");
            Assert.True(allSuccessful, "All write operations should succeed");
        }

        [Fact]
        public void OrderedCache_ConcurrentRemove_HandlesCorrectly()
        {
            // Arrange
            using var cache = CreateTestCache();
            var entries = new List<IEntry<string>>();
            for (int i = 0; i < 100; i++)
            {
                cache.Add($"item-{i}", out var entry);
                entries.Add(entry);
            }

            const int threadCount = 10;
            var tasks = new Task[threadCount];
            var removeResults = new System.Collections.Concurrent.ConcurrentBag<bool>();

            // Act
            for (int i = 0; i < threadCount; i++)
            {
                int startIdx = i * 10;
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < 10; j++)
                    {
                        var result = cache.Remove(entries[startIdx + j].Id, out _);
                        removeResults.Add(result);
                    }
                });
            }
            Task.WaitAll(tasks);

            // Assert
            Assert.All(removeResults, result => Assert.True(result));
            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void OrderedCache_ConcurrentUpdate_AllSucceed()
        {
            // Arrange
            using var cache = CreateTestCache();
            var entries = new List<IEntry<string>>();
            for (int i = 0; i < 50; i++)
            {
                cache.Add($"original-{i}", out var entry);
                entries.Add(entry);
            }

            const int threadCount = 5;
            var tasks = new Task[threadCount];
            var updateResults = new System.Collections.Concurrent.ConcurrentBag<bool>();

            // Act
            for (int i = 0; i < threadCount; i++)
            {
                int threadId = i;
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < entries.Count; j++)
                    {
                        var result = cache.Update(entries[j].Id, $"updated-by-{threadId}-{j}");
                        updateResults.Add(result);
                    }
                });
            }
            Task.WaitAll(tasks);

            // Assert
            Assert.All(updateResults, result => Assert.True(result));
        }

        [Fact]
        public async Task OrderedCache_ConcurrentGetNextAsync_MultipleWaiters()
        {
            // Arrange
            using var cache = CreateTestCache();
            cache.Add("first", out var first);

            const int waiterCount = 10;
            var tasks = new Task<IEntry<string>>[waiterCount];

            // Act
            for (int i = 0; i < waiterCount; i++)
            {
                tasks[i] = cache.GetNextAsync(first.Id);
            }

            await Task.Delay(50);
            cache.Add("second", out var second);

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, entry =>
            {
                Assert.NotNull(entry);
                Assert.Equal(second.Id, entry.Id);
                Assert.Equal("second", entry.Value);
            });
        }

        [Fact]
        public async Task OrderedCache_ConcurrentGetFutureFirstOrDefaultAsync_MultipleWaiters()
        {
            // Arrange
            using var cache = CreateTestCache();
            const int waiterCount = 10;
            var tasks = new Task<IEntry<string>>[waiterCount];

            // Act
            for (int i = 0; i < waiterCount; i++)
            {
                tasks[i] = cache.GetFutureFirstOrDefaultAsync();
            }

            await Task.Delay(50);
            cache.Add("first-item", out var first);

            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.All(results, entry =>
            {
                Assert.NotNull(entry);
                Assert.Equal(first.Id, entry.Id);
                Assert.Equal("first-item", entry.Value);
            });
        }

        [Fact]
        public async Task OrderedCache_ConcurrentAsyncEnumerators_AllEnumerateCorrectly()
        {
            // Arrange
            using var cache = CreateTestCache();
            const int itemCount = 20;
            const int enumeratorCount = 5;

            for (int i = 0; i < itemCount; i++)
            {
                cache.Add($"item-{i}", out _);
            }

            var enumeratorTasks = new List<Task<int>>();

            // Act
            for (int i = 0; i < enumeratorCount; i++)
            {
                enumeratorTasks.Add(Task.Run(async () =>
                {
                    int count = 0;
                    await foreach (var entry in cache.WithCancellation(CancellationToken.None))
                    {
                        count++;
                        if (count >= itemCount) break;
                    }
                    return count;
                }));
            }

            var counts = await Task.WhenAll(enumeratorTasks);

            // Assert
            Assert.All(counts, count => Assert.Equal(itemCount, count));
        }

        [Fact]
        public void OrderedCache_ConfigurationProperty_ReturnsCorrectValue()
        {
            // Arrange
            var config = new Caching.Configuration
            {
                EvictAfterEveryX = 123,
                RunAdaptiveResizing = true
            };

            using var cache = CreateTestCache(config: config);

            // Act & Assert
            Assert.NotNull(cache.Configuration);
            Assert.Equal(123, cache.Configuration.EvictAfterEveryX);
            Assert.True(cache.Configuration.RunAdaptiveResizing);
        }

        [Fact]
        public void OrderedCache_AfterDispose_OperationsThrowOrReturnFalse()
        {
            // Arrange
            var cache = CreateTestCache();
            cache.Add("test", out var entry);
            cache.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => cache.Add("after-dispose", out _));
            Assert.Throws<ObjectDisposedException>(() => cache.Update(entry.Id, "updated"));
            Assert.Throws<ObjectDisposedException>(() => cache.GetEntryOrDefault(entry.Id, out _));
            Assert.Throws<ObjectDisposedException>(() => cache.GetNextOrDefault(entry.Id, out _));
            Assert.Throws<ObjectDisposedException>(() => cache.GetFirstOrDefault(out _));
            Assert.Throws<ObjectDisposedException>(() => cache.GetLastOrDefault(out _));
            Assert.Throws<ObjectDisposedException>(() => cache.GetFirstIdOrDefault(out _));
            Assert.Throws<ObjectDisposedException>(() => cache.GetLastIdOrDefault(out _));
            Assert.Throws<ObjectDisposedException>(() => cache.Remove(entry.Id, out _));
            Assert.Throws<ObjectDisposedException>(() => cache.Clear());
        }

        [Fact]
        public void OrderedCache_GetNextOrDefault_FromNullId_GetsFirst()
        {
            // Arrange
            using var cache = CreateTestCache();
            cache.Add("first", out var first);
            cache.Add("second", out _);

            // Act
            var result = cache.GetNextOrDefault(null, out var entry);

            // Assert
            Assert.True(result);
            Assert.NotNull(entry);
            Assert.Equal(first.Id, entry.Id);
        }

        [Fact]
        public void OrderedCache_GetEntryOrDefault_WithNullId_ReturnsNull()
        {
            // Arrange
            using var cache = CreateTestCache();
            cache.Add("test", out _);

            // Act
            var result = cache.GetEntryOrDefault(null, out var entry);

            // Assert
            Assert.True(result);
            Assert.Null(entry);
        }

        [Fact]
        public void OrderedCache_Update_WithL1Store_UpdatesBothStores()
        {
            // Arrange
            using var cache = CreateTestCache(l1MinCap: 10, l1MaxCap: 100);
            cache.Add("original", out var entry);

            // Act
            var result = cache.Update(entry.Id, "updated");

            // Assert
            Assert.True(result);
            cache.GetEntryOrDefault(entry.Id, out var retrieved);
            Assert.Equal("updated", retrieved?.Value);
        }

        [Fact]
        public void OrderedCache_Remove_NonExistentEntry_ReturnsFalse()
        {
            // Arrange
            using var cache = CreateTestCache();
            var nonExistentId = Guid.NewGuid();

            // Act
            var result = cache.Remove(nonExistentId, out var entry);

            // Assert
            Assert.False(result);
            Assert.Null(entry);
        }

        [Fact]
        public void OrderedCache_Remove_EntryInL1AndL2_RemovesFromBoth()
        {
            // Arrange
            using var cache = CreateTestCache(l1MinCap: 10, l1MaxCap: 100);
            cache.Add("test", out var entry);

            // Act
            var result = cache.Remove(entry.Id, out var removed);

            // Assert
            Assert.True(result);
            Assert.NotNull(removed);
            cache.GetEntryOrDefault(entry.Id, out var retrieved);
            Assert.Null(retrieved);
        }

        [Fact]
        public void OrderedCache_Clear_EmptyCache_ReturnsTrue()
        {
            // Arrange
            using var cache = CreateTestCache();

            // Act
            var result = cache.Clear();

            // Assert
            Assert.True(result);
            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void OrderedCache_Eviction_WithNoActiveEnumerators_Succeeds()
        {
            // Arrange
            var config = new Caching.Configuration { EvictAfterEveryX = 5 };
            using var cache = CreateTestCache(config: config);

            // Act
            for (int i = 0; i < 10; i++)
            {
                cache.Add($"item-{i}", out _);
            }

            // Assert
            Assert.Equal(10, cache.Count);
        }

        [Fact]
        public async Task OrderedCache_GetNextAsync_WithExistingNext_ReturnsImmediately()
        {
            // Arrange
            using var cache = CreateTestCache();
            cache.Add("first", out var first);
            cache.Add("second", out var second);

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var next = await cache.GetNextAsync(first.Id);
            stopwatch.Stop();

            // Assert
            Assert.NotNull(next);
            Assert.Equal(second.Id, next.Id);
            Assert.True(stopwatch.ElapsedMilliseconds < 100, "Should return immediately");
        }

        [Fact]
        public async Task OrderedCache_GetNextAsync_FromNullId_ReturnsFirst()
        {
            // Arrange
            using var cache = CreateTestCache();
            cache.Add("first", out var first);

            // Act
            var next = await cache.GetNextAsync(null);

            // Assert
            Assert.NotNull(next);
            Assert.Equal(first.Id, next.Id);
        }

        [Fact]
        public async Task OrderedCache_GetNextAsync_Cancelled_ThrowsTaskCanceledException()
        {
            // Arrange
            using var cache = CreateTestCache();
            cache.Add("first", out var first);
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await cache.GetNextAsync(first.Id, cts.Token);
            });
        }

        [Fact]
        public async Task OrderedCache_GetFutureFirstOrDefaultAsync_Cancelled_ThrowsTaskCanceledException()
        {
            // Arrange
            using var cache = CreateTestCache();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await cache.GetFutureFirstOrDefaultAsync(cts.Token);
            });
        }

        [Fact]
        public void OrderedCache_L1StoreReplenishment_AfterRemoval()
        {
            // Arrange
            using var cache = CreateTestCache(l1MinCap: 2, l1MaxCap: 2);
            cache.Add("first", out var first);
            cache.Add("second", out _);
            cache.Add("third", out _);

            // Act
            cache.Remove(first.Id, out _);

            // Assert
            Assert.Equal(2, cache.Count);
            cache.GetFirstOrDefault(out var newFirst);
            Assert.NotNull(newFirst);
        }

        [Fact]
        public void OrderedCache_GetEntryOrDefault_FromL2_WhenNotInL1()
        {
            // Arrange
            using var cache = CreateTestCache(l1MinCap: 1, l1MaxCap: 1);
            cache.Add("first", out _);
            cache.Add("second", out var second);

            // Act
            var result = cache.GetEntryOrDefault(second.Id, out var retrieved);

            // Assert
            Assert.True(result);
            Assert.NotNull(retrieved);
            Assert.Equal(second.Id, retrieved.Id);
        }

        [Fact]
        public void OrderedCache_EmptyCache_GetNextOrDefault_ReturnsNull()
        {
            // Arrange
            using var cache = CreateTestCache();

            // Act
            var result = cache.GetNextOrDefault(Guid.NewGuid(), out var entry);

            // Assert
            Assert.True(result);
            Assert.Null(entry);
        }

        [Fact]
        public void OrderedCache_GetLastEntry_AfterMultipleAdds()
        {
            // Arrange
            using var cache = CreateTestCache();
            cache.Add("first", out _);
            cache.Add("second", out _);
            cache.Add("third", out var third);

            // Act
            var result = cache.GetLastOrDefault(out var last);

            // Assert
            Assert.True(result);
            Assert.NotNull(last);
            Assert.Equal(third.Id, last.Id);
        }

        [Fact]
        public void OrderedCache_MultipleDispose_IsSafe()
        {
            // Arrange
            var cache = CreateTestCache();
            cache.Add("test", out _);

            // Act
            cache.Dispose();
            cache.Dispose();
            cache.Dispose();

            // Assert - No exception thrown
        }

        [Fact]
        public void OrderedCache_DataPersistsInLiteDB()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var id = Guid.Empty;
            
            // Act - Create cache, add data, dispose
            {
                var config = new Caching.Configuration();
                var metadata = new Metadata { Configuration = config };
                var l2Store = new Store<string>(dbPath, "test", _loggerFactory);
                using var cache = new Caching.OrderedCache<string>(config, null, l2Store, metadata, _loggerFactory);
                
                cache.Add("persisted value", out var entry);
                id = entry.Id;
            }
            
            // Assert - Data should still be in LiteDB
            using var store = new Store<string>(dbPath, "test", _loggerFactory);
            store.GetValueOrDefault(id, out var value);
            Assert.Equal("persisted value", value);
        }
    }
}
