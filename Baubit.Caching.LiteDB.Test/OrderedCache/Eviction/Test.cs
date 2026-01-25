using Baubit.Caching.InMemory;
using LiteDB;
using Microsoft.Extensions.Logging.Abstractions;

namespace Baubit.Caching.LiteDB.Test.OrderedCache.Eviction
{
    /// <summary>
    /// Tests for OrderedCache eviction behavior with persisted enumerators.
    /// Verifies that eviction respects persisted enumerator positions when ResumeSession is enabled.
    /// </summary>
    public class Test : IDisposable
    {
        private readonly List<string> tempFiles = new List<string>();

        private string GetTempDbPath()
        {
            var path = Path.Combine(Path.GetTempPath(), $"litedb_eviction_test_{Guid.NewGuid()}.db");
            tempFiles.Add(path);
            return path;
        }

        public void Dispose()
        {
            foreach (var file in tempFiles)
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

        private static Caching.OrderedCache<Guid, string> CreateCache(
            Configuration config,
            LiteDatabase database,
            IEnumerable<Guid> existingIds = null)
        {
            var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();
            var metadata = new Metadata<Guid>(config, NullLoggerFactory.Instance, existingIds);
            var l2Store = new StoreGuid<string>(database, "test", identityGenerator, NullLoggerFactory.Instance);
            var enumeratorFactory = new CacheAsyncEnumeratorFactory<Guid, string>(database, config);
            
            Func<Baubit.Caching.CacheEnumeratorCollection<Guid>> enumeratorCollectionFactory = 
                () => new LiteDB.CacheEnumeratorCollection<Guid>(config, database);

            return new Caching.OrderedCache<Guid, string>(
                config, 
                null, // No L1 for these tests
                l2Store, 
                metadata, 
                NullLoggerFactory.Instance, 
                enumeratorCollectionFactory, 
                enumeratorFactory);
        }

        [Fact]
        public async Task Eviction_WhenResumeSessionEnabled_RespectsPersistedPositions()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            
            var config = new Configuration
            {
                ResumeSession = true,
                EvictAfterEveryX = 1,
                PersistPositionEveryXMoves = 1,
                PersistPositionBeforeMove = false
            };

            Guid entry1Id, entry2Id, entry3Id;

            // Session 1: Create cache with enumerators
            using (var db = new LiteDatabase(dbPath))
            {
                var cache = CreateCache(config, db);
                
                var enum1 = cache.GetFutureAsyncEnumerator("session-1", CancellationToken.None);
                
                cache.Add("value1", out var e1);
                cache.Add("value2", out var e2);
                cache.Add("value3", out var e3);
                
                entry1Id = e1.Id;
                entry2Id = e2.Id;
                entry3Id = e3.Id;
                
                // enum1 reads entry1 and entry2
                await enum1.MoveNextAsync();
                await enum1.MoveNextAsync();
                
                await enum1.DisposeAsync();
                cache.Dispose();
            }

            // Session 2: Resume enumerators and add data
            using (var db = new LiteDatabase(dbPath))
            {
                var collection = db.GetCollection<Entry<Guid, string>>("test");
                var existingIds = collection.FindAll().OrderBy(entry => entry.Id).Select(entry => entry.Id).ToList();
                
                var cache = CreateCache(config, db, existingIds);
                
                // IMPORTANT: Create resumed enumerator with same session ID
                // This will load the persisted position and make it an ACTIVE enumerator
                var enum1 = cache.GetFutureAsyncEnumerator("session-1", CancellationToken.None);
                
                // Add ONE entry to trigger ONE eviction
                cache.Add("value4", out var e4);

                // entry1 and entry2 should be evicted, entry3 protected by enum1's position
                cache.GetEntryOrDefault(entry1Id, out var retrieved1);
                cache.GetEntryOrDefault(entry2Id, out var retrieved2);
                cache.GetEntryOrDefault(entry3Id, out var retrieved3);

                Assert.Null(retrieved1); // Evicted
                Assert.Null(retrieved2); // Evicted
                Assert.NotNull(retrieved3); // Protected
                
                await enum1.DisposeAsync();
                cache.Dispose();
            }
        }

        [Fact]
        public async Task Eviction_WithMultipleEnumerators_AtDifferentPositions_RespectsLowest()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            
            var config = new Configuration
            {
                ResumeSession = true,
                EvictAfterEveryX = 1,
                PersistPositionEveryXMoves = 1,
                PersistPositionBeforeMove = false
            };

            Guid entry1Id, entry2Id, entry3Id, entry4Id;

            // Session 1: Multiple enumerators at different positions
            using (var db = new LiteDatabase(dbPath))
            {
                var cache = CreateCache(config, db);
                
                var enum1 = cache.GetFutureAsyncEnumerator("session-1", CancellationToken.None);
                var enum2 = cache.GetFutureAsyncEnumerator("session-2", CancellationToken.None);
                var enum3 = cache.GetFutureAsyncEnumerator("session-3", CancellationToken.None);
                
                cache.Add("value1", out var e1);
                cache.Add("value2", out var e2);
                cache.Add("value3", out var e3);
                cache.Add("value4", out var e4);
                
                entry1Id = e1.Id;
                entry2Id = e2.Id;
                entry3Id = e3.Id;
                entry4Id = e4.Id;
                
                // Move enumerators to different positions
                await enum1.MoveNextAsync(); // enum1 at entry1
                await enum1.MoveNextAsync(); // enum1 at entry2
                
                await enum2.MoveNextAsync(); // enum2 at entry1
                await enum2.MoveNextAsync(); // enum2 at entry2
                await enum2.MoveNextAsync(); // enum2 at entry3
                await enum2.MoveNextAsync(); // enum2 at entry4
                
                await enum3.MoveNextAsync(); // enum3 at entry1
                await enum3.MoveNextAsync(); // enum3 at entry2
                await enum3.MoveNextAsync(); // enum3 at entry3
                
                // Lowest position is entry2 (enum1)
                await enum1.DisposeAsync();
                await enum2.DisposeAsync();
                await enum3.DisposeAsync();
                cache.Dispose();
            }

            // Session 2: Resume enumerators and verify eviction
            using (var db = new LiteDatabase(dbPath))
            {
                var collection = db.GetCollection<Entry<Guid, string>>("test");
                var existingIds = collection.FindAll().OrderBy(entry => entry.Id).Select(entry => entry.Id).ToList();
                
                var cache = CreateCache(config, db, existingIds);
                
                // Resume all three enumerators
                var enum1 = cache.GetFutureAsyncEnumerator("session-1", CancellationToken.None);
                var enum2 = cache.GetFutureAsyncEnumerator("session-2", CancellationToken.None);
                var enum3 = cache.GetFutureAsyncEnumerator("session-3", CancellationToken.None);
                
                // Trigger ONE eviction
                cache.Add("value5", out var e5);
                
                // entry1 and entry2 should be evicted (lowest is entry2)
                cache.GetEntryOrDefault(entry1Id, out var r1);
                cache.GetEntryOrDefault(entry2Id, out var r2);
                cache.GetEntryOrDefault(entry3Id, out var r3);
                cache.GetEntryOrDefault(entry4Id, out var r4);

                Assert.Null(r1); // Evicted
                Assert.Null(r2); // Evicted (lowest position)
                Assert.NotNull(r3); // Protected (enum3 at entry3)
                Assert.NotNull(r4); // Protected (enum2 at entry4)
                
                await enum1.DisposeAsync();
                await enum2.DisposeAsync();
                await enum3.DisposeAsync();
                cache.Dispose();
            }
        }

        [Fact]
        public async Task Eviction_WithNullPersistedPositions_RespectOnlyNonNullPositions()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            
            var config = new Configuration
            {
                ResumeSession = true,
                EvictAfterEveryX = 1,
                PersistPositionEveryXMoves = 1,
                PersistPositionBeforeMove = false
            };

            Guid entry1Id, entry2Id, entry3Id;

            // Session 1
            using (var db = new LiteDatabase(dbPath))
            {
                var cache = CreateCache(config, db);
                
                // enum1 will move, enum2 won't (stays at null)
                var enum1 = cache.GetFutureAsyncEnumerator("session-1", CancellationToken.None);
                var enum2 = cache.GetFutureAsyncEnumerator("session-2", CancellationToken.None);
                
                cache.Add("value1", out var e1);
                cache.Add("value2", out var e2);
                cache.Add("value3", out var e3);
                
                entry1Id = e1.Id;
                entry2Id = e2.Id;
                entry3Id = e3.Id;
                
                // Only enum1 moves
                await enum1.MoveNextAsync();
                await enum1.MoveNextAsync(); // enum1 at entry2
                
                // enum2 doesn't move - its position stays null
                
                await enum1.DisposeAsync();
                await enum2.DisposeAsync();
                cache.Dispose();
            }

            // Session 2
            using (var db = new LiteDatabase(dbPath))
            {
                var collection = db.GetCollection<Entry<Guid, string>>("test");
                var existingIds = collection.FindAll().OrderBy(entry => entry.Id).Select(entry => entry.Id).ToList();
                
                var cache = CreateCache(config, db, existingIds);
                
                // Resume both enumerators
                var enum1 = cache.GetFutureAsyncEnumerator("session-1", CancellationToken.None);
                var enum2 = cache.GetFutureAsyncEnumerator("session-2", CancellationToken.None);
                
                // Add ONE entry
                cache.Add("value4", out var e4);
                
                // With enum2 at null position (ignored) and enum1 at entry2,
                // eviction should be through entry2
                cache.GetEntryOrDefault(entry1Id, out var r1);
                cache.GetEntryOrDefault(entry2Id, out var r2);
                cache.GetEntryOrDefault(entry3Id, out var r3);

                Assert.Null(r1); // Evicted (enum1 past it)
                Assert.Null(r2); // Evicted (enum1 at it)
                Assert.NotNull(r3); // Protected
                
                await enum1.DisposeAsync();
                await enum2.DisposeAsync();
                cache.Dispose();
            }
        }

        [Fact]
        public async Task Eviction_WhenResumeSessionDisabled_IgnoresPersistedPositions()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            
            var config = new Configuration
            {
                ResumeSession = false, // DISABLED
                EvictAfterEveryX = 1,
                PersistPositionEveryXMoves = 1,
                PersistPositionBeforeMove = false
            };

            Guid entry1Id, entry2Id;

            // Session 1: Create persisted positions
            using (var db = new LiteDatabase(dbPath))
            {
                var cache = CreateCache(config, db);
                
                var enum1 = cache.GetFutureAsyncEnumerator("session-1", CancellationToken.None);
                
                cache.Add("value1", out var e1);
                cache.Add("value2", out var e2);
                
                entry1Id = e1.Id;
                entry2Id = e2.Id;
                
                await enum1.MoveNextAsync(); // At entry1
                await enum1.DisposeAsync();
                cache.Dispose();
            }

            // Session 2: ResumeSession disabled - persisted positions should be ignored
            using (var db = new LiteDatabase(dbPath))
            {
                var collection = db.GetCollection<Entry<Guid, string>>("test");
                var existingIds = collection.FindAll().OrderBy(entry => entry.Id).Select(entry => entry.Id).ToList();
                
                var cache = CreateCache(config, db, existingIds);
                
                // No active enumerators - should evict everything
                cache.Add("value3", out var e3);
                
                cache.GetEntryOrDefault(entry1Id, out var r1);
                cache.GetEntryOrDefault(entry2Id, out var r2);

                Assert.Null(r1); // Evicted (ResumeSession disabled)
                Assert.Null(r2); // Evicted (ResumeSession disabled)
                
                cache.Dispose();
            }
        }
    }
}
