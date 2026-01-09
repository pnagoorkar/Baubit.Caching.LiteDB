using Baubit.Caching.LiteDB;
using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Baubit.Caching.LiteDB.Test.CacheFutureAsyncEnumerator
{
    /// <summary>
    /// Tests for CacheFutureAsyncEnumerator with LiteDB persistence
    /// </summary>
    public class Test : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;
        private readonly List<string> _tempFiles = new List<string>();

        private string GetTempDbPath()
        {
            var path = Path.Combine(Path.GetTempPath(), $"litedb_future_test_{Guid.NewGuid()}.db");
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
        public void CacheFutureAsyncEnumerator_CanBeCreated()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();
            var config = new Configuration { ResumeSession = false };
            
            using var store = new StoreGuid<string>(dbPath, "test", identityGenerator, _loggerFactory);
            var factory = new CacheAsyncEnumeratorFactory<Guid, string>(StoreTestHelper.GetDatabase(store), config);
            
            var metadata = new Baubit.Caching.InMemory.Metadata<Guid>(config, _loggerFactory);
            using var cache = new Baubit.Caching.OrderedCache<Guid, string>(config, null, store, metadata, _loggerFactory);

            // Act
            var enumerator = factory.CreateFutureEnumerator(cache, _ => { }, "test-future", CancellationToken.None);

            // Assert
            Assert.NotNull(enumerator);
        }

        [Fact]
        public void CacheFutureAsyncEnumerator_InitializesWithLastEntry()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();
            var config = new Configuration { ResumeSession = false };
            
            using var store = new StoreGuid<string>(dbPath, "test", identityGenerator, _loggerFactory);
            var factory = new CacheAsyncEnumeratorFactory<Guid, string>(StoreTestHelper.GetDatabase(store), config);
            
            var metadata = new Baubit.Caching.InMemory.Metadata<Guid>(config, _loggerFactory);
            using var cache = new Baubit.Caching.OrderedCache<Guid, string>(config, null, store, metadata, _loggerFactory);

            cache.Add("first", out _);
            cache.Add("second", out var lastEntry);

            // Act
            var enumerator = factory.CreateFutureEnumerator(cache, _ => { }, "test-future", CancellationToken.None);

            // Assert - Current should be initialized to last entry
            Assert.NotNull(enumerator.Current);
            Assert.Equal(lastEntry.Id, enumerator.Current.Id);
            Assert.Equal("second", enumerator.Current.Value);
        }

        [Fact]
        public void CacheFutureAsyncEnumerator_WithResumeSession_CanBeCreatedWithSavedPosition()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();
            var config = new Configuration 
            { 
                ResumeSession = true,
                PersistPositionEveryXMoves = 1  // Enable persistence for this test
            };
            
            using var store = new StoreGuid<string>(dbPath, "test", identityGenerator, _loggerFactory);
            var factory = new CacheAsyncEnumeratorFactory<Guid, string>(StoreTestHelper.GetDatabase(store), config);
            
            var metadata = new Baubit.Caching.InMemory.Metadata<Guid>(config, _loggerFactory);
            using var cache = new Baubit.Caching.OrderedCache<Guid, string>(config, null, store, metadata, _loggerFactory);

            // Pre-save a position
            var positionCollection = StoreTestHelper.GetDatabase(store).GetCollection<EnumeratorPosition<Guid>>("_enumerator_positions");
            cache.Add("first", out var entry1);
            positionCollection.Insert(new EnumeratorPosition<Guid>("test-future", entry1.Id));

            // Act - Create enumerator with saved position
            var enumerator = factory.CreateFutureEnumerator(cache, _ => { }, "test-future", CancellationToken.None);

            // Assert
            Assert.NotNull(enumerator);
        }
    }
}
