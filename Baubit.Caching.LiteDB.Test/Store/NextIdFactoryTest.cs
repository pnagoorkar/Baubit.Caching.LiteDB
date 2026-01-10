using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Baubit.Caching.LiteDB.Test.Store
{
    /// <summary>
    /// Tests for nextIdFactory functionality in Store classes
    /// </summary>
    public class NextIdFactoryTest : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;
        private readonly List<string> _tempFiles = new List<string>();

        private string GetTempDbPath()
        {
            var path = Path.Combine(Path.GetTempPath(), $"litedb_nextid_test_{Guid.NewGuid()}.db");
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

        #region StoreGuid Tests with nextIdFactory

        [Fact]
        public void StoreGuid_WithCustomNextIdFactory_GeneratesCustomIds()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var customIds = new Queue<Guid>(new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() });
            Func<Guid?, Guid?> customFactory = lastId => customIds.Count > 0 ? customIds.Dequeue() : (Guid?)null;
            
            using var store = new StoreGuid<string>(dbPath, "test", customFactory, _loggerFactory);

            // Act
            store.Add("first", out var entry1);
            store.Add("second", out var entry2);
            store.Add("third", out var entry3);

            // Assert - All IDs should be unique and from our custom factory
            Assert.NotEqual(entry1.Id, entry2.Id);
            Assert.NotEqual(entry2.Id, entry3.Id);
            Assert.NotEqual(entry1.Id, entry3.Id);
        }

        [Fact]
        public void StoreGuid_WithCustomNextIdFactory_WithCapacity_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var idCounter = 0;
            Func<Guid?, Guid?> customFactory = lastId => new Guid(++idCounter, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            
            using var store = new StoreGuid<string>(dbPath, "test", 10, 100, customFactory, _loggerFactory);

            // Act
            store.Add("test", out var entry);

            // Assert
            Assert.False(store.Uncapped);
            Assert.Equal(10, store.MinCapacity);
            Assert.Equal(100, store.MaxCapacity);
            Assert.NotEqual(Guid.Empty, entry.Id);
        }

        [Fact]
        public void StoreGuid_WithCustomNextIdFactory_ExistingDatabase_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var db = new LiteDatabase(dbPath);
            var idCounter = 0;
            Func<Guid?, Guid?> customFactory = lastId => new Guid(++idCounter, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            
            using var store = new StoreGuid<string>(db, "test", customFactory, _loggerFactory);

            // Act
            store.Add("test", out var entry);

            // Assert
            Assert.NotEqual(Guid.Empty, entry.Id);
        }

        [Fact]
        public void StoreGuid_WithCustomNextIdFactory_ExistingDatabase_WithCapacity_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var db = new LiteDatabase(dbPath);
            var idCounter = 0;
            Func<Guid?, Guid?> customFactory = lastId => new Guid(++idCounter, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            
            using var store = new StoreGuid<string>(db, "test", 5, 50, customFactory, _loggerFactory);

            // Act
            store.Add("test", out var entry);

            // Assert
            Assert.False(store.Uncapped);
            Assert.Equal(5, store.MinCapacity);
            Assert.Equal(50, store.MaxCapacity);
            Assert.NotEqual(Guid.Empty, entry.Id);
        }

        [Fact]
        public void StoreGuid_NullNextIdFactory_ThrowsArgumentNullException()
        {
            // Arrange
            var dbPath = GetTempDbPath();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new StoreGuid<string>(dbPath, "test", (Func<Guid?, Guid?>)null!, _loggerFactory));
        }

        [Fact]
        public void StoreGuid_NullNextIdFactory_WithCapacity_ThrowsArgumentNullException()
        {
            // Arrange
            var dbPath = GetTempDbPath();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new StoreGuid<string>(dbPath, "test", 10, 100, (Func<Guid?, Guid?>)null!, _loggerFactory));
        }

        #endregion

        #region StoreInt Tests with nextIdFactory

        [Fact]
        public void StoreInt_WithCustomNextIdFactory_GeneratesCustomIds()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var idCounter = 100;
            Func<int?, int?> customFactory = lastId => idCounter += 10; // IDs: 110, 120, 130, ...
            
            using var store = new StoreInt<string>(dbPath, "test", customFactory, _loggerFactory);

            // Act
            store.Add("first", out var entry1);
            store.Add("second", out var entry2);
            store.Add("third", out var entry3);

            // Assert
            Assert.Equal(110, entry1.Id);
            Assert.Equal(120, entry2.Id);
            Assert.Equal(130, entry3.Id);
        }

        [Fact]
        public void StoreInt_WithCustomNextIdFactory_WithCapacity_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var idCounter = 0;
            Func<int?, int?> customFactory = lastId => ++idCounter;
            
            using var store = new StoreInt<string>(dbPath, "test", 10, 100, customFactory, _loggerFactory);

            // Act
            store.Add("test", out var entry);

            // Assert
            Assert.False(store.Uncapped);
            Assert.Equal(10, store.MinCapacity);
            Assert.Equal(100, store.MaxCapacity);
            Assert.Equal(1, entry.Id);
        }

        [Fact]
        public void StoreInt_WithCustomNextIdFactory_ExistingDatabase_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var db = new LiteDatabase(dbPath);
            var idCounter = 1000;
            Func<int?, int?> customFactory = lastId => ++idCounter;
            
            using var store = new StoreInt<string>(db, "test", customFactory, _loggerFactory);

            // Act
            store.Add("test", out var entry);

            // Assert
            Assert.Equal(1001, entry.Id);
        }

        [Fact]
        public void StoreInt_WithCustomNextIdFactory_ExistingDatabase_WithCapacity_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var db = new LiteDatabase(dbPath);
            var idCounter = 5000;
            Func<int?, int?> customFactory = lastId => ++idCounter;
            
            using var store = new StoreInt<string>(db, "test", 5, 50, customFactory, _loggerFactory);

            // Act
            store.Add("test", out var entry);

            // Assert
            Assert.False(store.Uncapped);
            Assert.Equal(5, store.MinCapacity);
            Assert.Equal(50, store.MaxCapacity);
            Assert.Equal(5001, entry.Id);
        }

        [Fact]
        public void StoreInt_NullNextIdFactory_ThrowsArgumentNullException()
        {
            // Arrange
            var dbPath = GetTempDbPath();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new StoreInt<string>(dbPath, "test", (Func<int?, int?>)null!, _loggerFactory));
        }

        [Fact]
        public void StoreInt_NextIdFactory_RespectsLastGeneratedId()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            Func<int?, int?> factory = lastId => lastId.HasValue ? lastId.Value + 5 : 1;
            
            using var store = new StoreInt<string>(dbPath, "test", factory, _loggerFactory);

            // Act
            store.Add("first", out var entry1);
            store.Add("second", out var entry2);
            store.Add("third", out var entry3);

            // Assert - Should increment by 5 each time
            Assert.Equal(1, entry1.Id);
            Assert.Equal(6, entry2.Id);
            Assert.Equal(11, entry3.Id);
        }

        #endregion

        #region StoreLong Tests with nextIdFactory

        [Fact]
        public void StoreLong_WithCustomNextIdFactory_GeneratesCustomIds()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var idCounter = 1000000L;
            Func<long?, long?> customFactory = lastId => idCounter += 100; // IDs: 1000100, 1000200, ...
            
            using var store = new StoreLong<string>(dbPath, "test", customFactory, _loggerFactory);

            // Act
            store.Add("first", out var entry1);
            store.Add("second", out var entry2);
            store.Add("third", out var entry3);

            // Assert
            Assert.Equal(1000100L, entry1.Id);
            Assert.Equal(1000200L, entry2.Id);
            Assert.Equal(1000300L, entry3.Id);
        }

        [Fact]
        public void StoreLong_WithCustomNextIdFactory_WithCapacity_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var idCounter = 0L;
            Func<long?, long?> customFactory = lastId => ++idCounter;
            
            using var store = new StoreLong<string>(dbPath, "test", 10, 100, customFactory, _loggerFactory);

            // Act
            store.Add("test", out var entry);

            // Assert
            Assert.False(store.Uncapped);
            Assert.Equal(10, store.MinCapacity);
            Assert.Equal(100, store.MaxCapacity);
            Assert.Equal(1L, entry.Id);
        }

        [Fact]
        public void StoreLong_WithCustomNextIdFactory_ExistingDatabase_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var db = new LiteDatabase(dbPath);
            var idCounter = 9999999L;
            Func<long?, long?> customFactory = lastId => ++idCounter;
            
            using var store = new StoreLong<string>(db, "test", customFactory, _loggerFactory);

            // Act
            store.Add("test", out var entry);

            // Assert
            Assert.Equal(10000000L, entry.Id);
        }

        [Fact]
        public void StoreLong_WithCustomNextIdFactory_ExistingDatabase_WithCapacity_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var db = new LiteDatabase(dbPath);
            var idCounter = 50000L;
            Func<long?, long?> customFactory = lastId => ++idCounter;
            
            using var store = new StoreLong<string>(db, "test", 5, 50, customFactory, _loggerFactory);

            // Act
            store.Add("test", out var entry);

            // Assert
            Assert.False(store.Uncapped);
            Assert.Equal(5, store.MinCapacity);
            Assert.Equal(50, store.MaxCapacity);
            Assert.Equal(50001L, entry.Id);
        }

        [Fact]
        public void StoreLong_NullNextIdFactory_ThrowsArgumentNullException()
        {
            // Arrange
            var dbPath = GetTempDbPath();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new StoreLong<string>(dbPath, "test", (Func<long?, long?>)null!, _loggerFactory));
        }

        [Fact]
        public void StoreLong_NextIdFactory_RespectsLastGeneratedId()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            Func<long?, long?> factory = lastId => lastId.HasValue ? lastId.Value + 100 : 1;
            
            using var store = new StoreLong<string>(dbPath, "test", factory, _loggerFactory);

            // Act
            store.Add("first", out var entry1);
            store.Add("second", out var entry2);
            store.Add("third", out var entry3);

            // Assert - Should increment by 100 each time
            Assert.Equal(1L, entry1.Id);
            Assert.Equal(101L, entry2.Id);
            Assert.Equal(201L, entry3.Id);
        }

        #endregion

        #region Cross-Store Tests

        [Fact]
        public void Store_NextIdFactory_ReturnsNull_AddFails()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            Func<int?, int?> factoryThatReturnsNull = lastId => null;
            
            using var store = new StoreInt<string>(dbPath, "test", factoryThatReturnsNull, _loggerFactory);

            // Act
            var result = store.Add("test value", out var entry);

            // Assert
            Assert.False(result);
            Assert.Null(entry);
        }

        [Fact]
        public void StoreLong_NextIdFactory_ReturnsNull_AddFails()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            Func<long?, long?> factoryThatReturnsNull = lastId => null;
            
            using var store = new StoreLong<string>(dbPath, "test", factoryThatReturnsNull, _loggerFactory);

            // Act
            var result = store.Add("test value", out var entry);

            // Assert
            Assert.False(result);
            Assert.Null(entry);
        }

        [Fact]
        public void StoreGuid_NextIdFactory_ReturnsNull_AddFails()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            Func<Guid?, Guid?> factoryThatReturnsNull = lastId => null;
            
            using var store = new StoreGuid<string>(dbPath, "test", factoryThatReturnsNull, _loggerFactory);

            // Act
            var result = store.Add("test value", out var entry);

            // Assert
            Assert.False(result);
            Assert.Null(entry);
        }

        [Fact]
        public void StoreInt_DefaultFactory_WorksAfterReopen()
        {
            // Arrange
            var dbPath = GetTempDbPath();

            // Act - Create store, add entries, close
            using (var store = new StoreInt<string>(dbPath, "test", _loggerFactory))
            {
                store.Add("first", out var entry1);
                store.Add("second", out var entry2);
                Assert.Equal(1, entry1.Id);
                Assert.Equal(2, entry2.Id);
            }

            // Act - Reopen and add more entries
            using (var store = new StoreInt<string>(dbPath, "test", _loggerFactory))
            {
                store.Add("third", out var entry3);
                
                // Assert - Should continue from last ID in database
                Assert.True(entry3.Id > 2);
            }
        }

        [Fact]
        public void StoreLong_DefaultFactory_WorksAfterReopen()
        {
            // Arrange
            var dbPath = GetTempDbPath();

            // Act - Create store, add entries, close
            using (var store = new StoreLong<string>(dbPath, "test", _loggerFactory))
            {
                store.Add("first", out var entry1);
                store.Add("second", out var entry2);
                Assert.Equal(1L, entry1.Id);
                Assert.Equal(2L, entry2.Id);
            }

            // Act - Reopen and add more entries
            using (var store = new StoreLong<string>(dbPath, "test", _loggerFactory))
            {
                store.Add("third", out var entry3);
                
                // Assert - Should continue from last ID in database
                Assert.True(entry3.Id > 2L);
            }
        }

        #endregion

        #region Backward Compatibility Tests

        [Fact]
        public void StoreGuid_BackwardCompatibility_IdentityGenerator_Works()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();

            // Act
            using var store = new StoreGuid<string>(dbPath, "test", identityGenerator, _loggerFactory);
            store.Add("test", out var entry);

            // Assert
            Assert.NotEqual(Guid.Empty, entry.Id);
        }

        [Fact]
        public void StoreGuid_BackwardCompatibility_IdentityGenerator_WithCapacity_Works()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();

            // Act
            using var store = new StoreGuid<string>(dbPath, "test", 10, 100, identityGenerator, _loggerFactory);
            store.Add("test", out var entry);

            // Assert
            Assert.NotEqual(Guid.Empty, entry.Id);
            Assert.Equal(10, store.MinCapacity);
        }

        [Fact]
        public void StoreGuid_BackwardCompatibility_NoIdentityGenerator_Works()
        {
            // Arrange
            var dbPath = GetTempDbPath();

            // Act - Old constructor without identityGenerator should still work
            using var store = new StoreGuid<string>(dbPath, "test", _loggerFactory);
            store.Add("test", out var entry);

            // Assert
            Assert.NotEqual(Guid.Empty, entry.Id);
        }

        [Fact]
        public void StoreInt_BackwardCompatibility_DefaultFactory_Works()
        {
            // Arrange
            var dbPath = GetTempDbPath();

            // Act - Old constructor should use default sequential factory
            using var store = new StoreInt<string>(dbPath, "test", _loggerFactory);
            store.Add("first", out var entry1);
            store.Add("second", out var entry2);

            // Assert
            Assert.Equal(1, entry1.Id);
            Assert.Equal(2, entry2.Id);
        }

        [Fact]
        public void StoreLong_BackwardCompatibility_DefaultFactory_Works()
        {
            // Arrange
            var dbPath = GetTempDbPath();

            // Act - Old constructor should use default sequential factory
            using var store = new StoreLong<string>(dbPath, "test", _loggerFactory);
            store.Add("first", out var entry1);
            store.Add("second", out var entry2);

            // Assert
            Assert.Equal(1L, entry1.Id);
            Assert.Equal(2L, entry2.Id);
        }

        [Fact]
        public void StoreGuid_NullIdentityGenerator_ThrowsArgumentNullException()
        {
            // Arrange
            var dbPath = GetTempDbPath();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new StoreGuid<string>(dbPath, "test", (Baubit.Identity.IIdentityGenerator)null!, _loggerFactory));
        }

        [Fact]
        public void StoreGuid_NullIdentityGenerator_WithCapacity_ThrowsArgumentNullException()
        {
            // Arrange
            var dbPath = GetTempDbPath();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new StoreGuid<string>(dbPath, "test", 10, 100, (Baubit.Identity.IIdentityGenerator)null!, _loggerFactory));
        }

        [Fact]
        public void Store_LastAddedId_PropertyGetter_Works()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new StoreInt<string>(dbPath, "test", _loggerFactory);

            // Act
            store.Add("first", out var entry1);
            var lastId = store.LastAddedId;

            // Assert
            Assert.NotNull(lastId);
            Assert.Equal(entry1.Id, lastId.Value);
        }

        [Fact]
        public void Store_GetValueOrDefault_WithNullId_ReturnsFalse()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new StoreInt<string>(dbPath, "test", _loggerFactory);

            // Act
            var result = store.GetValueOrDefault(null, out var value);

            // Assert
            Assert.False(result);
            Assert.Null(value);
        }

        #endregion
    }
}
