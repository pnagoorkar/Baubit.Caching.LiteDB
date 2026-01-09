using Baubit.Caching.LiteDB;
using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Baubit.Caching.LiteDB.Test.CacheAsyncEnumerator
{
    /// <summary>
    /// Tests for CacheAsyncEnumerator with LiteDB persistence
    /// </summary>
    public class Test : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;
        private readonly List<string> _tempFiles = new List<string>();

        private string GetTempDbPath()
        {
            var path = Path.Combine(Path.GetTempPath(), $"litedb_enum_test_{Guid.NewGuid()}.db");
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

        [Fact]
        public async Task CacheAsyncEnumerator_PartialEnumeration_PersistsCorrectly()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();
            var config = new Configuration { ResumeSession = true };
            
            using var store = new StoreGuid<string>(dbPath, "test", identityGenerator, _loggerFactory);
            var factory = new CacheAsyncEnumeratorFactory<Guid, string>(store.Database, config);
            
            var metadata = new Baubit.Caching.InMemory.Metadata<Guid>(config, _loggerFactory);
            using var cache = new Baubit.Caching.OrderedCache<Guid, string>(config, null, store, metadata, _loggerFactory);

            // Add test data
            cache.Add("first", out var entry1);
            cache.Add("second", out var entry2);
            cache.Add("third", out var entry3);

            // Act - Enumerate first entry only
            var enumerator = factory.CreateEnumerator(cache, _ => { }, "test-session", CancellationToken.None);
            
            await enumerator.MoveNextAsync();
            Assert.Equal("first", enumerator.Current.Value);
            
            await enumerator.DisposeAsync();

            // Assert - Check position was saved
            var positionCollection = store.Database.GetCollection<EnumeratorPosition<Guid>>("_enumerator_positions");
            var savedPosition = positionCollection.FindById("test-session");
            Assert.NotNull(savedPosition);
            Assert.Equal(entry1.Id, savedPosition.CurrentId);
        }

        [Fact]
        public async Task CacheAsyncEnumerator_TwoSteps_SavesCorrectly()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();
            var config = new Configuration { ResumeSession = true };
            
            using var store = new StoreGuid<string>(dbPath, "test", identityGenerator, _loggerFactory);
            var factory = new CacheAsyncEnumeratorFactory<Guid, string>(store.Database, config);
            
            var metadata = new Baubit.Caching.InMemory.Metadata<Guid>(config, _loggerFactory);
            using var cache = new Baubit.Caching.OrderedCache<Guid, string>(config, null, store, metadata, _loggerFactory);

            // Add test data
            cache.Add("first", out var entry1);
            cache.Add("second", out var entry2);
            cache.Add("third", out var entry3);

            // Act - Enumerate two entries
            var enumerator = factory.CreateEnumerator(cache, _ => { }, "test-session", CancellationToken.None);
            
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();
            Assert.Equal("second", enumerator.Current.Value);
            
            await enumerator.DisposeAsync();

            // Assert - Check second position was saved
            var positionCollection = store.Database.GetCollection<EnumeratorPosition<Guid>>("_enumerator_positions");
            var savedPosition = positionCollection.FindById("test-session");
            Assert.NotNull(savedPosition);
            Assert.Equal(entry2.Id, savedPosition.CurrentId);
        }

        [Fact]
        public async Task CacheAsyncEnumerator_NoResumeSession_StartsFromBeginning()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();
            var config = new Configuration { ResumeSession = false };
            
            using var store = new StoreGuid<string>(dbPath, "test", identityGenerator, _loggerFactory);
            var factory = new CacheAsyncEnumeratorFactory<Guid, string>(store.Database, config);
            
            var metadata = new Baubit.Caching.InMemory.Metadata<Guid>(config, _loggerFactory);
            using var cache = new Baubit.Caching.OrderedCache<Guid, string>(config, null, store, metadata, _loggerFactory);

            cache.Add("first", out var entry1);
            cache.Add("second", out var entry2);

            // Act - First enum: move once
            var enumerator1 = factory.CreateEnumerator(cache, _ => { }, "test-session", CancellationToken.None);
            await enumerator1.MoveNextAsync();
            await enumerator1.DisposeAsync();

            // Act - Second enum: should start from beginning despite previous session
            var enumerator2 = factory.CreateEnumerator(cache, _ => { }, "test-session", CancellationToken.None);
            await enumerator2.MoveNextAsync();

            // Assert - Should be back at first
            Assert.Equal("first", enumerator2.Current.Value);
            
            await enumerator2.DisposeAsync();
        }

        [Fact]
        public async Task CacheAsyncEnumerator_PersistEveryXMoves_OnlyPersistsAtInterval()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();
            var config = new Configuration 
            { 
                ResumeSession = true,
                PersistPositionEveryXMoves = 3 // Only persist every 3 moves
            };
            
            using var store = new StoreGuid<string>(dbPath, "test", identityGenerator, _loggerFactory);
            var factory = new CacheAsyncEnumeratorFactory<Guid, string>(store.Database, config);
            
            var metadata = new Baubit.Caching.InMemory.Metadata<Guid>(config, _loggerFactory);
            using var cache = new Baubit.Caching.OrderedCache<Guid, string>(config, null, store, metadata, _loggerFactory);

            // Add test data
            for (int i = 1; i <= 10; i++)
            {
                cache.Add($"entry-{i}", out _);
            }

            // Act - Move twice (should not persist yet)
            var enumerator = factory.CreateEnumerator(cache, _ => { }, "test-session", CancellationToken.None);
            await enumerator.MoveNextAsync(); // Move 1
            await enumerator.MoveNextAsync(); // Move 2
            
            var positionCollection = store.Database.GetCollection<EnumeratorPosition<Guid>>("_enumerator_positions");
            var savedPosition1 = positionCollection.FindById("test-session");
            Assert.Null(savedPosition1); // Should not be saved yet

            // Act - Move one more time (should persist now)
            await enumerator.MoveNextAsync(); // Move 3
            var savedPosition2 = positionCollection.FindById("test-session");
            Assert.NotNull(savedPosition2); // Should be saved now
            Assert.Equal("entry-3", enumerator.Current.Value);
            
            await enumerator.DisposeAsync();
        }
    }
}
