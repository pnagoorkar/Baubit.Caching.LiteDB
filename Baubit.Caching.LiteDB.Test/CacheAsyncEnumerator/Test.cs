using Baubit.Caching.LiteDB;
using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Baubit.Caching.LiteDB.Test.CacheAsyncEnumerator
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
            var path = Path.Combine(Path.GetTempPath(), $"litedb_enumerator_test_{Guid.NewGuid()}.db");
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

        private Baubit.Caching.OrderedCache<Guid, string> CreateTestCache(
            Configuration? config = null,
            long? l1MinCap = null,
            long? l1MaxCap = null,
            string? enumeratorDbPath = null)
        {
            config ??= new Configuration();
            var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();
            var metadata = new Baubit.Caching.InMemory.Metadata<Guid>(config, NullLoggerFactory.Instance);
            var dbPath = GetTempDbPath();
            var l2Store = new StoreGuid<string>(dbPath, "test", identityGenerator, _loggerFactory);
            
            var l1Store = l1MinCap.HasValue 
                ? new Baubit.Caching.InMemory.Store<Guid, string>(
                    l1MinCap, 
                    l1MaxCap, 
                    lastId => identityGenerator.GetNext(), 
                    _loggerFactory) 
                : null;

            // Create enumerator factory if path provided
            ICacheAsyncEnumeratorFactory<Guid, string>? enumeratorFactory = null;
            LiteDatabase? enumeratorDb = null;
            if (enumeratorDbPath != null)
            {
                enumeratorFactory = new CacheAsyncEnumeratorFactory<Guid, string>(enumeratorDbPath, config, out enumeratorDb);
            }

            return new Baubit.Caching.OrderedCache<Guid, string>(config, l1Store, l2Store, metadata, _loggerFactory, null, enumeratorFactory);
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
        public void CacheAsyncEnumeratorFactory_Constructor_WithDatabasePath_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var config = new Configuration { ResumeSession = false };

            // Act
            var factory = new CacheAsyncEnumeratorFactory<Guid, string>(dbPath, config, out var db);

            // Assert
            Assert.NotNull(factory);
            Assert.NotNull(db);
            db.Dispose();
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
        public async Task CacheAsyncEnumerator_PartialEnumeration_PersistsCorrectly()
        {
            // Arrange
            using var cache = CreateTestCache();
            var dbPath = GetTempDbPath();
            using var db = new LiteDatabase(dbPath);
            var config = new Configuration { ResumeSession = true };
            var factory = new CacheAsyncEnumeratorFactory<Guid, string>(db, config);

            // Add test data
            cache.Add("first", out var entry1);
            cache.Add("second", out var entry2);
            cache.Add("third", out var entry3);

            // Act - Enumerate first entry only
            var enumerator = factory.CreateEnumerator(cache, _ => { }, "test-enum-1", CancellationToken.None);
            
            await enumerator.MoveNextAsync();
            Assert.Equal("first", enumerator.Current.Value);
            
            await enumerator.DisposeAsync();

            // Assert - Check position was saved
            var positionCollection = db.GetCollection<EnumeratorPosition<Guid>>("_enumerator_positions");
            var savedPosition = positionCollection.FindById("test-enum-1");
            Assert.NotNull(savedPosition);
            Assert.Equal(entry1.Id, savedPosition.CurrentId);
        }

        [Fact]
        public async Task CacheAsyncEnumerator_TwoSteps_SavesCorrectly()
        {
            // Arrange
            using var cache = CreateTestCache();
            var dbPath = GetTempDbPath();
            using var db = new LiteDatabase(dbPath);
            var config = new Configuration { ResumeSession = true };
            var factory = new CacheAsyncEnumeratorFactory<Guid, string>(db, config);

            // Add test data
            cache.Add("first", out var entry1);
            cache.Add("second", out var entry2);
            cache.Add("third", out var entry3);

            // Act - Enumerate two entries
            var enumerator = factory.CreateEnumerator(cache, _ => { }, "test-enum-2", CancellationToken.None);
            
            await enumerator.MoveNextAsync();
            await enumerator.MoveNextAsync();
            Assert.Equal("second", enumerator.Current.Value);
            
            await enumerator.DisposeAsync();

            // Assert - Check second position was saved
            var positionCollection = db.GetCollection<EnumeratorPosition<Guid>>("_enumerator_positions");
            var savedPosition = positionCollection.FindById("test-enum-2");
            Assert.NotNull(savedPosition);
            Assert.Equal(entry2.Id, savedPosition.CurrentId);
        }

        [Fact]
        public async Task CacheAsyncEnumerator_ResumeSession_ResumesFromSavedPosition()
        {
            // Arrange
            using var cache = CreateTestCache();
            var dbPath = GetTempDbPath();
            using var db = new LiteDatabase(dbPath);
            var config = new Configuration { ResumeSession = true };
            var factory = new CacheAsyncEnumeratorFactory<Guid, string>(db, config);

            // Add test data
            cache.Add("first", out var entry1);
            cache.Add("second", out var entry2);
            cache.Add("third", out var entry3);

            // Act - First enumeration: move to second entry
            var enumerator1 = factory.CreateEnumerator(cache, _ => { }, "test-enum-3", CancellationToken.None);
            
            await enumerator1.MoveNextAsync(); // first
            await enumerator1.MoveNextAsync(); // second
            
            await enumerator1.DisposeAsync();

            // Act - Second enumeration: should resume from second entry
            var enumerator2 = factory.CreateEnumerator(cache, _ => { }, "test-enum-3", CancellationToken.None);
            
            // The next MoveNextAsync should give us third because we resume from second
            await enumerator2.MoveNextAsync();
            
            // Assert
            Assert.Equal("third", enumerator2.Current.Value);
            
            await enumerator2.DisposeAsync();
        }

        [Fact]
        public async Task CacheAsyncEnumerator_Persistence_SavesPosition()
        {
            // Arrange
            using var cache = CreateTestCache();
            var dbPath = GetTempDbPath();
            using var db = new LiteDatabase(dbPath);
            var config = new Configuration { ResumeSession = true };
            var factory = new CacheAsyncEnumeratorFactory<Guid, string>(db, config);

            // Add test data
            cache.Add("first", out var entry1);
            cache.Add("second", out var entry2);

            // Act - Enumerate first entry
            var enumerator = factory.CreateEnumerator(cache, _ => { }, "test-enum-4", CancellationToken.None);
            await enumerator.MoveNextAsync();
            var currentId = enumerator.Current.Id;
            await enumerator.DisposeAsync();

            // Assert - Check that position was persisted
            var positionCollection = db.GetCollection<EnumeratorPosition<Guid>>("_enumerator_positions");
            var savedPosition = positionCollection.FindById("test-enum-4");
            
            Assert.NotNull(savedPosition);
            Assert.Equal("test-enum-4", savedPosition.Id);
            Assert.Equal(currentId, savedPosition.CurrentId);
        }

        [Fact]
        public void CacheFutureAsyncEnumerator_CanBeCreated()
        {
            // Arrange
            using var cache = CreateTestCache();
            var dbPath = GetTempDbPath();
            using var db = new LiteDatabase(dbPath);
            var config = new Configuration { ResumeSession = false };
            var factory = new CacheAsyncEnumeratorFactory<Guid, string>(db, config);

            // Act
            var enumerator = factory.CreateFutureEnumerator(cache, _ => { }, "test-future-1", CancellationToken.None);

            // Assert
            Assert.NotNull(enumerator);
        }

        [Fact]
        public void CacheFutureAsyncEnumerator_ResumeSession_CanBeCreatedWithSavedPosition()
        {
            // Arrange
            using var cache = CreateTestCache();
            var dbPath = GetTempDbPath();
            using var db = new LiteDatabase(dbPath);
            var config = new Configuration { ResumeSession = true };
            var factory = new CacheAsyncEnumeratorFactory<Guid, string>(db, config);

            // Pre-save a position
            var positionCollection = db.GetCollection<EnumeratorPosition<Guid>>("_enumerator_positions");
            cache.Add("first", out var entry1);
            positionCollection.Insert(new EnumeratorPosition<Guid>("test-future-2", entry1.Id));

            // Act - Create enumerator with saved position
            var enumerator = factory.CreateFutureEnumerator(cache, _ => { }, "test-future-2", CancellationToken.None);

            // Assert
            Assert.NotNull(enumerator);
        }

        [Fact]
        public void Configuration_ResumeSession_DefaultIsFalse()
        {
            // Arrange & Act
            var config = new Configuration();

            // Assert
            Assert.False(config.ResumeSession);
        }

        [Fact]
        public void Configuration_ResumeSession_CanBeSet()
        {
            // Arrange
            var config = new Configuration();

            // Act
            config.ResumeSession = true;

            // Assert
            Assert.True(config.ResumeSession);
        }

        [Fact]
        public void Configuration_InheritsFromBaseCachingConfiguration()
        {
            // Arrange & Act
            var config = new Configuration
            {
                RunAdaptiveResizing = false,
                EvictAfterEveryX = 1000
            };

            // Assert
            Assert.False(config.RunAdaptiveResizing);
            Assert.Equal(1000, config.EvictAfterEveryX);
        }

        [Fact]
        public async Task CacheAsyncEnumerator_DifferentIds_IndependentPositions()
        {
            // Arrange
            using var cache = CreateTestCache();
            var dbPath = GetTempDbPath();
            using var db = new LiteDatabase(dbPath);
            var config = new Configuration { ResumeSession = true };
            var factory = new CacheAsyncEnumeratorFactory<Guid, string>(db, config);

            // Add test data
            cache.Add("first", out _);
            cache.Add("second", out _);
            cache.Add("third", out _);

            // Act - Create two enumerators with different IDs
            var enumerator1 = factory.CreateEnumerator(cache, _ => { }, "enum-id-1", CancellationToken.None);
            var enumerator2 = factory.CreateEnumerator(cache, _ => { }, "enum-id-2", CancellationToken.None);

            // Advance first enumerator by 1
            await enumerator1.MoveNextAsync();
            await enumerator1.DisposeAsync();

            // Advance second enumerator by 2
            await enumerator2.MoveNextAsync();
            await enumerator2.MoveNextAsync();
            await enumerator2.DisposeAsync();

            // Check saved positions
            var positionCollection = db.GetCollection<EnumeratorPosition<Guid>>("_enumerator_positions");
            var pos1 = positionCollection.FindById("enum-id-1");
            var pos2 = positionCollection.FindById("enum-id-2");

            // Assert - Each enumerator should have its own saved position
            Assert.NotNull(pos1);
            Assert.NotNull(pos2);
            Assert.NotEqual(pos1.CurrentId, pos2.CurrentId);
        }

        [Fact]
        public async Task CacheAsyncEnumerator_NoResumeSession_StartsFromBeginning()
        {
            // Arrange
            using var cache = CreateTestCache();
            var dbPath = GetTempDbPath();
            using var db = new LiteDatabase(dbPath);
            var config = new Configuration { ResumeSession = false };
            var factory = new CacheAsyncEnumeratorFactory<Guid, string>(db, config);

            cache.Add("first", out var entry1);
            cache.Add("second", out var entry2);

            // Act - First enum: move once
            var enumerator1 = factory.CreateEnumerator(cache, _ => { }, "test-enum-noreg", CancellationToken.None);
            await enumerator1.MoveNextAsync();
            await enumerator1.DisposeAsync();

            // Act - Second enum: should start from beginning despite previous session
            var enumerator2 = factory.CreateEnumerator(cache, _ => { }, "test-enum-noreg", CancellationToken.None);
            await enumerator2.MoveNextAsync();

            // Assert - Should be back at first
            Assert.Equal("first", enumerator2.Current.Value);
            
            await enumerator2.DisposeAsync();
        }

        /// <summary>
        /// Helper class to wrap IAsyncEnumerator for use with await foreach
        /// </summary>
        private class AsyncEnumeratorWrapper<TId, TValue>
            where TId : struct, IComparable<TId>, IEquatable<TId>
        {
            private readonly IAsyncEnumerator<IEntry<TId, TValue>> _enumerator;

            public AsyncEnumeratorWrapper(IAsyncEnumerator<IEntry<TId, TValue>> enumerator)
            {
                _enumerator = enumerator;
            }

            public IAsyncEnumerator<IEntry<TId, TValue>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return _enumerator;
            }
        }
    }
}
