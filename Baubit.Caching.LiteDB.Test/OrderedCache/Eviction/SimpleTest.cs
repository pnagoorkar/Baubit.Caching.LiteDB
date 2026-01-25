using Baubit.Caching.InMemory;
using LiteDB;
using Microsoft.Extensions.Logging.Abstractions;

namespace Baubit.Caching.LiteDB.Test.OrderedCache.Eviction
{
    public class SimpleTest : IDisposable
    {
        private readonly string dbPath;

        public SimpleTest()
        {
            dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"simple_eviction_test_{Guid.NewGuid()}.db");
        }

        public void Dispose()
        {
            try
            {
                if (System.IO.File.Exists(dbPath))
                    System.IO.File.Delete(dbPath);
            }
            catch { }
        }

        private static Caching.OrderedCache<Guid, string> CreateCache(
            Configuration config,
            LiteDatabase database,
            IEnumerable<Guid> existingIds = null)
        {
            var generator = Baubit.Identity.IdentityGenerator.CreateNew();
            var metadata = new Metadata<Guid>(config, NullLoggerFactory.Instance, existingIds);
            var l2Store = new StoreGuid<string>(database, "test", generator, NullLoggerFactory.Instance);
            var enumFactory = new CacheAsyncEnumeratorFactory<Guid, string>(database, config);
            
            Func<Baubit.Caching.CacheEnumeratorCollection<Guid>> enumCollFactory =
                () => new LiteDB.CacheEnumeratorCollection<Guid>(config, database);

            return new Caching.OrderedCache<Guid, string>(
                config, null, l2Store, metadata, NullLoggerFactory.Instance, 
                enumCollFactory, enumFactory);
        }

        [Fact]
        public async Task ProperEvictionTest_WithResumedEnumerators()
        {
            var config = new Configuration
            {
                ResumeSession = true,
                EvictAfterEveryX = 1, // Evict after every add
                PersistPositionEveryXMoves = 1, // Persist after every move
                PersistPositionBeforeMove = false, // Persist after moving (so position reflects last read entry)
            };

            Guid entry1Id, entry2Id, entry3Id;

            // Session 1: Create cache, add data, create enumerators, move them, dispose
            using (var db = new LiteDatabase(dbPath))
            {
                var cache = CreateCache(config, db);

                // Create FUTURE enumerators BEFORE adding data (they wait for new entries)
                var enum1 = cache.GetFutureAsyncEnumerator("enum-session-1", System.Threading.CancellationToken.None);
                var enum2 = cache.GetFutureAsyncEnumerator("enum-session-2", System.Threading.CancellationToken.None);

                // Add data
                cache.Add("value1", out var e1);
                cache.Add("value2", out var e2);
                cache.Add("value3", out var e3);

                entry1Id = e1.Id;
                entry2Id = e2.Id;
                entry3Id = e3.Id;

                // Move enumerators - enum1 reads entry1, enum2 reads entry1
                await enum1.MoveNextAsync(); // enum1 at entry1
                await enum2.MoveNextAsync(); // enum2 at entry1

                // Dispose enumerators to persist positions
                await enum1.DisposeAsync();
                await enum2.DisposeAsync();

                // Verify positions were persisted
                var posCol = db.GetCollection<EnumeratorPosition<Guid>>("_enumerator_positions");
                var pos1 = posCol.FindById("enum-session-1");
                var pos2 = posCol.FindById("enum-session-2");
                Assert.NotNull(pos1);
                Assert.NotNull(pos2);
                Assert.Equal(entry1Id, pos1.CurrentId);
                Assert.Equal(entry1Id, pos2.CurrentId);

                // IMPORTANT: Dispose cache before reopening database
                cache.Dispose();
            }

            // Session 2: Reopen cache (simulating restart), create enumerators with same IDs, move them forward
            using (var db = new LiteDatabase(dbPath))
            {
                // Load existing IDs from database (ResumeSession scenario)
                var collection = db.GetCollection<Entry<Guid, string>>("test");
                var existingIds = collection.FindAll().OrderBy(entry => entry.Id).Select(entry => entry.Id).ToList();
                
                Assert.Equal(3, existingIds.Count);

                var cache = CreateCache(config, db, existingIds);

                // All 3 entries should exist before we do anything
                Assert.Equal(3, cache.Count);
                cache.GetEntryOrDefault(entry1Id, out var check1);
                Assert.NotNull(check1);

                // Create enumerators with SAME session IDs - they should resume from entry1
                var enum1 = cache.GetFutureAsyncEnumerator("enum-session-1", System.Threading.CancellationToken.None);
                var enum2 = cache.GetFutureAsyncEnumerator("enum-session-2", System.Threading.CancellationToken.None);

                // Move enum1 forward to entry2 (reads entry2, persists position at entry2)
                await enum1.MoveNextAsync(); // enum1 now at entry2
                
                // enum2 is still at entry1 (hasn't moved yet)
                // So entry1 cannot be evicted yet
                cache.GetEntryOrDefault(entry1Id, out var stillThere);
                Assert.NotNull(stillThere); // entry1 protected by enum2

                // Now move enum2 forward to entry2 as well
                await enum2.MoveNextAsync(); // enum2 now at entry2

                // Both enumerators have moved past entry1 (both now at entry2)
                // But we need to trigger eviction by adding a new entry
                cache.Add("value4", out var e4);

                // NOW entry1 should be evicted (both enumerators have moved past it)
                // entry2 should also be evicted (both enumerators are AT entry2, which means they've read it)
                cache.GetEntryOrDefault(entry1Id, out var afterEvict1);
                cache.GetEntryOrDefault(entry2Id, out var afterEvict2);
                cache.GetEntryOrDefault(entry3Id, out var afterEvict3);
                cache.GetEntryOrDefault(e4.Id, out var afterEvict4);

                Assert.Null(afterEvict1); // Evicted (both enumerators past it)
                Assert.Null(afterEvict2); // Evicted (both enumerators at it)
                Assert.NotNull(afterEvict3); // Protected (enumerators haven't reached it)
                Assert.NotNull(afterEvict4); // Protected (just added)

                // Cleanup
                await enum1.DisposeAsync();
                await enum2.DisposeAsync();
                cache.Dispose();
            }
        }
    }
}



