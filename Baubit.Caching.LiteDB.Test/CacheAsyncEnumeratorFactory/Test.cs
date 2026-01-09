using Baubit.Caching.LiteDB;
using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Baubit.Caching.LiteDB.Test.CacheAsyncEnumeratorFactory
{
    /// <summary>
    /// Tests for CacheAsyncEnumeratorFactory with LiteDB persistence and ResumeSession functionality
    /// </summary>
    public class Test : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;
        private readonly List<string> _tempFiles = new List<string>();

        private string GetTempDbPath()
        {
            var path = Path.Combine(Path.GetTempPath(), $"litedb_factory_test_{Guid.NewGuid()}.db");
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
        public void CacheAsyncEnumeratorFactory_Constructor_WithDatabase_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var db = new LiteDatabase(dbPath);
            var config = new Configuration { ResumeSession = false };

            // Act
            var factory = new CacheAsyncEnumeratorFactory<Guid, string>(db, config);

            // Assert
            Assert.NotNull(factory);
        }

        [Fact]
        public void CacheAsyncEnumeratorFactory_Constructor_NullDatabase_ThrowsArgumentNullException()
        {
            // Arrange
            var config = new Configuration();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CacheAsyncEnumeratorFactory<Guid, string>((LiteDatabase)null!, config));
        }

        [Fact]
        public void CacheAsyncEnumeratorFactory_Constructor_NullConfiguration_ThrowsArgumentNullException()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var db = new LiteDatabase(dbPath);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new CacheAsyncEnumeratorFactory<Guid, string>(db, null!));
        }

        [Fact]
        public async Task CacheAsyncEnumeratorFactory_CreateEnumerator_WithoutResumeSession_StartsFromBeginning()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();
            var config = new Configuration { ResumeSession = false };
            
            using var store = new StoreGuid<string>(dbPath, "test", identityGenerator, _loggerFactory);
            var factory = new CacheAsyncEnumeratorFactory<Guid, string>(store.Database, config);
            
            var metadata = new Baubit.Caching.InMemory.Metadata<Guid>(config, _loggerFactory);
            using var cache = new Baubit.Caching.OrderedCache<Guid, string>(config, null, store, metadata, _loggerFactory);

            // Add test data
            cache.Add("first", out var entry1);
            cache.Add("second", out var entry2);

            // Act
            var enumerator = factory.CreateEnumerator(cache, _ => { }, "test-session", CancellationToken.None);
            await enumerator.MoveNextAsync();

            // Assert - Should start from beginning
            Assert.Equal("first", enumerator.Current.Value);
            
            await enumerator.DisposeAsync();
        }

        [Fact]
        public async Task CacheAsyncEnumeratorFactory_CreateEnumerator_WithResumeSession_ResumesFromSavedPosition()
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

            // Act - First session: move to second entry
            var enumerator1 = factory.CreateEnumerator(cache, _ => { }, "test-session", CancellationToken.None);
            await enumerator1.MoveNextAsync(); // first
            await enumerator1.MoveNextAsync(); // second
            await enumerator1.DisposeAsync();

            // Act - Second session: should resume from second entry
            var enumerator2 = factory.CreateEnumerator(cache, _ => { }, "test-session", CancellationToken.None);
            await enumerator2.MoveNextAsync(); // Should get third

            // Assert
            Assert.Equal("third", enumerator2.Current.Value);
            
            await enumerator2.DisposeAsync();
        }

        [Fact]
        public async Task CacheAsyncEnumeratorFactory_DifferentSessionIds_IndependentPositions()
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
            cache.Add("first", out _);
            cache.Add("second", out _);
            cache.Add("third", out _);

            // Act - Create two enumerators with different session IDs
            var enum1 = factory.CreateEnumerator(cache, _ => { }, "session-1", CancellationToken.None);
            var enum2 = factory.CreateEnumerator(cache, _ => { }, "session-2", CancellationToken.None);

            // Advance first enumerator by 1
            await enum1.MoveNextAsync();
            await enum1.DisposeAsync();

            // Advance second enumerator by 2
            await enum2.MoveNextAsync();
            await enum2.MoveNextAsync();
            await enum2.DisposeAsync();

            // Verify positions are saved independently
            var positionCollection = store.Database.GetCollection<EnumeratorPosition<Guid>>("_enumerator_positions");
            var pos1 = positionCollection.FindById("session-1");
            var pos2 = positionCollection.FindById("session-2");

            // Assert
            Assert.NotNull(pos1);
            Assert.NotNull(pos2);
            Assert.NotEqual(pos1.CurrentId, pos2.CurrentId);
        }
    }
}
