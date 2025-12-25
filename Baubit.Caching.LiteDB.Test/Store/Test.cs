using Baubit.Caching.LiteDB;
using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Baubit.Caching.LiteDB.Test.Store
{
    /// <summary>
    /// Tests for <see cref="Baubit.Caching.LiteDB.Store{TValue}"/>
    /// </summary>
    public class Test : IDisposable
    {
        private readonly ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;
        private readonly List<string> _tempFiles = new List<string>();

        private string GetTempDbPath()
        {
            var path = Path.Combine(Path.GetTempPath(), $"litedb_test_{Guid.NewGuid()}.db");
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
                    // Also delete journal file if exists
                    var journalFile = file + "-journal";
                    if (File.Exists(journalFile))
                        File.Delete(journalFile);
                }
                catch { /* Ignore cleanup errors */ }
            }
        }

        [Fact]
        public void Store_Constructor_UncappedStore()
        {
            // Arrange
            var dbPath = GetTempDbPath();

            // Act
            using var store = new Store<string>(dbPath, "test", _loggerFactory);

            // Assert
            Assert.True(store.Uncapped);
            Assert.Null(store.MinCapacity);
            Assert.Null(store.MaxCapacity);
            Assert.Null(store.TargetCapacity);
            Assert.Null(store.CurrentCapacity);
            Assert.True(store.HasCapacity);
        }

        [Fact]
        public void Store_AddWithAutoGeneration_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<string>(dbPath, "test", _loggerFactory);

            // Act - Add without explicit ID uses auto-generated Guid
            var result = store.Add("test value", out var entry);

            // Assert
            Assert.True(result);
            Assert.NotEqual(Guid.Empty, entry.Id);
            Assert.Equal("test value", entry.Value);
        }

        [Fact]
        public void Store_AddWithAutoGeneration_MultipleEntries()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<string>(dbPath, "test", _loggerFactory);

            // Act - Add multiple values with auto-generated GuidV7s
            store.Add("first", out var entry1);
            store.Add("second", out var entry2);
            store.Add("third", out var entry3);

            // Assert - All should have unique IDs
            Assert.NotEqual(entry1.Id, entry2.Id);
            Assert.NotEqual(entry2.Id, entry3.Id);
            Assert.NotEqual(entry1.Id, entry3.Id);
            
            // GuidV7 should be sequential (later IDs are greater)
            Assert.True(entry1.Id < entry2.Id);
            Assert.True(entry2.Id < entry3.Id);
        }

        [Fact]
        public void Store_AddWithCustomIdentityGenerator_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();
            using var store = new Store<string>(dbPath, "test", identityGenerator, _loggerFactory);

            // Act
            var result = store.Add("test value", out var entry);

            // Assert
            Assert.True(result);
            Assert.NotEqual(Guid.Empty, entry.Id);
            Assert.Equal("test value", entry.Value);
        }

        [Fact]
        public void Store_Constructor_WithCapacity()
        {
            // Arrange
            var dbPath = GetTempDbPath();

            // Act
            using var store = new Store<string>(dbPath, "test", 10, 100, _loggerFactory);

            // Assert
            Assert.False(store.Uncapped);
            Assert.Equal(10, store.MinCapacity);
            Assert.Equal(100, store.MaxCapacity);
            Assert.Equal(10, store.TargetCapacity);
            Assert.Equal(10, store.CurrentCapacity);
            Assert.True(store.HasCapacity);
        }

        [Fact]
        public void Store_Constructor_WithCapacityAndIdentityGenerator()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();

            // Act
            using var store = new Store<string>(dbPath, "test", 10, 100, identityGenerator, _loggerFactory);

            // Assert
            Assert.False(store.Uncapped);
            Assert.Equal(10, store.MinCapacity);
            Assert.Equal(100, store.MaxCapacity);
        }

        [Fact]
        public void Store_Constructor_WithExistingDatabase_UncappedWithIdentityGenerator()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var db = new LiteDatabase(dbPath);
            var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();

            // Act
            using var store = new Store<string>(db, "test", identityGenerator, _loggerFactory);

            // Assert
            Assert.True(store.Uncapped);
        }

        [Fact]
        public void Store_Constructor_WithExistingDatabase_WithCapacityAndIdentityGenerator()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var db = new LiteDatabase(dbPath);
            var identityGenerator = Baubit.Identity.IdentityGenerator.CreateNew();

            // Act
            using var store = new Store<string>(db, "test", 10, 100, identityGenerator, _loggerFactory);

            // Assert
            Assert.False(store.Uncapped);
            Assert.Equal(10, store.MinCapacity);
            Assert.Equal(100, store.MaxCapacity);
        }

        [Fact]
        public void Store_Constructor_WithExistingDatabase()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var db = new LiteDatabase(dbPath);

            // Act
            using var store = new Store<string>(db, "test", _loggerFactory);

            // Assert
            Assert.True(store.Uncapped);
        }

        [Fact]
        public void Store_Constructor_WithExistingDatabase_WithCapacity()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var db = new LiteDatabase(dbPath);

            // Act
            using var store = new Store<string>(db, "test", 5, 50, _loggerFactory);

            // Assert
            Assert.False(store.Uncapped);
            Assert.Equal(5, store.MinCapacity);
            Assert.Equal(50, store.MaxCapacity);
        }

        [Fact]
        public void Store_Constructor_NullDatabase_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new Store<string>((LiteDatabase)null!, "test", _loggerFactory));
        }

        [Fact]
        public void Store_Add_Entry_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<string>(dbPath, "test", _loggerFactory);
            var id = Guid.NewGuid();
            var entry = new Entry<string>(id, "test");

            // Act
            var result = store.Add(entry);

            // Assert
            Assert.True(result);
            Assert.True(store.GetEntryOrDefault(id, out var retrieved));
            Assert.NotNull(retrieved);
            Assert.Equal(id, retrieved.Id);
            Assert.Equal("test", retrieved.Value);
        }

        [Fact]
        public void Store_Add_WithIdAndValue_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<int>(dbPath, "test", _loggerFactory);
            var id = Guid.NewGuid();

            // Act
            var result = store.Add(id, 42, out var entry);

            // Assert
            Assert.True(result);
            Assert.NotNull(entry);
            Assert.Equal(id, entry.Id);
            Assert.Equal(42, entry.Value);
        }

        [Fact]
        public void Store_Add_WhenCapacityExceeded_Fails()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<string>(dbPath, "test", 2, 2, _loggerFactory);
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var id3 = Guid.NewGuid();

            // Act
            var result1 = store.Add(id1, "first", out _);
            var result2 = store.Add(id2, "second", out _);
            var result3 = store.Add(id3, "third", out _);

            // Assert
            Assert.True(result1);
            Assert.True(result2);
            Assert.False(result3); // Should fail due to capacity
            Assert.False(store.HasCapacity);
        }

        [Fact]
        public void Store_Add_DuplicateId_Fails()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<string>(dbPath, "test", _loggerFactory);
            var id = Guid.NewGuid();

            // Act
            var result1 = store.Add(id, "first", out _);
            var result2 = store.Add(id, "second", out _);

            // Assert
            Assert.True(result1);
            Assert.False(result2); // Should fail due to duplicate
        }

        [Fact]
        public void Store_GetEntryOrDefault_ExistingId_ReturnsEntry()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<string>(dbPath, "test", _loggerFactory);
            var id = Guid.NewGuid();
            store.Add(id, "test value", out _);

            // Act
            var result = store.GetEntryOrDefault(id, out var entry);

            // Assert
            Assert.True(result);
            Assert.NotNull(entry);
            Assert.Equal("test value", entry.Value);
        }

        [Fact]
        public void Store_GetEntryOrDefault_NonExistingId_ReturnsNullEntry()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<string>(dbPath, "test", _loggerFactory);
            var id = Guid.NewGuid();

            // Act
            var result = store.GetEntryOrDefault(id, out var entry);

            // Assert - GetEntryOrDefault returns true but entry is null when not found
            Assert.True(result);
            Assert.Null(entry);
        }

        [Fact]
        public void Store_GetEntryOrDefault_NullId_ReturnsFalse()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<string>(dbPath, "test", _loggerFactory);

            // Act
            var result = store.GetEntryOrDefault(null, out var entry);

            // Assert
            Assert.False(result);
            Assert.Null(entry);
        }

        [Fact]
        public void Store_GetValueOrDefault_ExistingId_ReturnsValue()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<int>(dbPath, "test", _loggerFactory);
            var id = Guid.NewGuid();
            store.Add(id, 123, out _);

            // Act
            var result = store.GetValueOrDefault(id, out var value);

            // Assert
            Assert.True(result);
            Assert.Equal(123, value);
        }

        [Fact]
        public void Store_GetValueOrDefault_NonExistingId_ReturnsDefault()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<string>(dbPath, "test", _loggerFactory);
            var id = Guid.NewGuid();

            // Act
            var result = store.GetValueOrDefault(id, out var value);

            // Assert - GetValueOrDefault returns false when entry doesn't exist
            Assert.True(result);
            Assert.Null(value);
        }

        [Fact]
        public void Store_Update_ExistingEntry_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<string>(dbPath, "test", _loggerFactory);
            var id = Guid.NewGuid();
            store.Add(id, "original", out _);

            // Act
            var result = store.Update(id, "updated");

            // Assert
            Assert.True(result);
            store.GetValueOrDefault(id, out var value);
            Assert.Equal("updated", value);
        }

        [Fact]
        public void Store_Update_WithEntry_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<int>(dbPath, "test", _loggerFactory);
            var id = Guid.NewGuid();
            store.Add(id, 10, out _);
            var updatedEntry = new Entry<int>(id, 20);

            // Act
            var result = store.Update(updatedEntry);

            // Assert
            Assert.True(result);
            store.GetValueOrDefault(id, out var value);
            Assert.Equal(20, value);
        }

        [Fact]
        public void Store_Update_NonExistingEntry_Fails()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<string>(dbPath, "test", _loggerFactory);
            var id = Guid.NewGuid();
            var entry = new Entry<string>(id, "test");

            // Act
            var result = store.Update(entry);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Store_Update_NonExistingIdWithValue_Fails()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<string>(dbPath, "test", _loggerFactory);
            var id = Guid.NewGuid();

            // Act
            var result = store.Update(id, "value");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Store_Remove_ExistingEntry_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<string>(dbPath, "test", _loggerFactory);
            var id = Guid.NewGuid();
            store.Add(id, "test", out _);

            // Act
            var result = store.Remove(id, out var removed);

            // Assert
            Assert.True(result);
            Assert.NotNull(removed);
            Assert.Equal("test", removed.Value);
            // GetEntryOrDefault now returns true but entry is null for non-existent entries
            Assert.True(store.GetEntryOrDefault(id, out var entry));
            Assert.Null(entry);
        }

        [Fact]
        public void Store_Remove_NonExistingEntry_Fails()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<string>(dbPath, "test", _loggerFactory);
            var id = Guid.NewGuid();

            // Act
            var result = store.Remove(id, out var removed);

            // Assert
            Assert.False(result);
            Assert.Null(removed);
        }

        [Fact]
        public void Store_GetCount_ReturnsCorrectCount()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<string>(dbPath, "test", _loggerFactory);
            store.Add(Guid.NewGuid(), "first", out _);
            store.Add(Guid.NewGuid(), "second", out _);

            // Act
            var result = store.GetCount(out var count);

            // Assert
            Assert.True(result);
            Assert.Equal(2, count);
        }

        [Fact]
        public void Store_AddCapacity_IncreasesCapacity()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<string>(dbPath, "test", 10, 100, _loggerFactory);
            var initialCapacity = store.TargetCapacity;

            // Act
            var result = store.AddCapacity(20);

            // Assert
            Assert.True(result);
            Assert.Equal(initialCapacity + 20, store.TargetCapacity);
        }

        [Fact]
        public void Store_AddCapacity_RespectsMaxCapacity()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<string>(dbPath, "test", 10, 100, _loggerFactory);

            // Act
            var result = store.AddCapacity(200);

            // Assert
            Assert.True(result);
            Assert.Equal(100, store.TargetCapacity); // Should not exceed max
        }

        [Fact]
        public void Store_CutCapacity_DecreasesCapacity()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<string>(dbPath, "test", 10, 100, _loggerFactory);
            store.AddCapacity(40); // Set to 50
            var beforeCut = store.TargetCapacity;

            // Act
            var result = store.CutCapacity(20);

            // Assert
            Assert.True(result);
            Assert.Equal(beforeCut - 20, store.TargetCapacity);
        }

        [Fact]
        public void Store_CutCapacity_RespectsMinCapacity()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<string>(dbPath, "test", 10, 100, _loggerFactory);

            // Act
            var result = store.CutCapacity(20);

            // Assert
            Assert.True(result);
            Assert.Equal(10, store.TargetCapacity); // Should not go below min
        }

        [Fact]
        public void Store_CurrentCapacity_UpdatesAfterAddAndRemove()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<string>(dbPath, "test", 5, 10, _loggerFactory);
            Assert.Equal(5, store.CurrentCapacity);

            // Act - Add entries
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            store.Add(id1, "first", out _);
            store.Add(id2, "second", out _);

            // Assert after add
            Assert.Equal(3, store.CurrentCapacity);

            // Act - Remove entry
            store.Remove(id1, out _);

            // Assert after remove
            Assert.Equal(4, store.CurrentCapacity);
        }

        [Fact]
        public void Store_DataPersists_AfterReopen()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var id = Guid.NewGuid();

            // Act - Write data
            using (var store = new Store<string>(dbPath, "test", _loggerFactory))
            {
                store.Add(id, "persisted value", out _);
            }

            // Act - Read data after reopen
            using (var store = new Store<string>(dbPath, "test", _loggerFactory))
            {
                // Assert
                var result = store.GetValueOrDefault(id, out var value);
                Assert.True(result);
                Assert.Equal("persisted value", value);
            }
        }

        [Fact]
        public void Store_HeadTail_RestoresAfterReopen()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var id3 = Guid.NewGuid();

            // Act - Write data
            using (var store = new Store<string>(dbPath, "test", _loggerFactory))
            {
                store.Add(id1, "first", out _);
                store.Add(id2, "second", out _);
                store.Add(id3, "third", out _);
            }

            // Act - Read data after reopen
            using (var store = new Store<string>(dbPath, "test", _loggerFactory))
            {
                // Assert - verify all data persists
                Assert.True(store.GetEntryOrDefault(id1, out var entry1));
                Assert.Equal("first", entry1.Value);
                Assert.True(store.GetEntryOrDefault(id2, out var entry2));
                Assert.Equal("second", entry2.Value);
                Assert.True(store.GetEntryOrDefault(id3, out var entry3));
                Assert.Equal("third", entry3.Value);
            }
        }

        [Fact]
        public void Store_ComplexValueType_Works()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<TestComplexValue>(dbPath, "test", _loggerFactory);
            var id = Guid.NewGuid();
            var complexValue = new TestComplexValue
            {
                Name = "Test",
                Count = 42,
                Items = new List<string> { "a", "b", "c" }
            };

            // Act
            store.Add(id, complexValue, out _);
            store.GetValueOrDefault(id, out var retrieved);

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("Test", retrieved.Name);
            Assert.Equal(42, retrieved.Count);
            Assert.Equal(3, retrieved.Items.Count);
        }

        [Fact]
        public void Store_MultipleCollections_Independent()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var db = new LiteDatabase(dbPath);
            using var store1 = new Store<string>(db, "collection1", _loggerFactory);
            using var store2 = new Store<int>(db, "collection2", _loggerFactory);
            var id = Guid.NewGuid();

            // Act
            store1.Add(id, "string value", out _);
            store2.Add(id, 42, out _);

            // Assert
            store1.GetValueOrDefault(id, out var stringValue);
            store2.GetValueOrDefault(id, out var intValue);
            Assert.Equal("string value", stringValue);
            Assert.Equal(42, intValue);
        }

        [Fact]
        public void Store_Add_PreservesCreatedOnUTC()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<string>(dbPath, "test", _loggerFactory);
            var id = Guid.NewGuid();
            var createdOn = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
            var entry = new Entry<string>(id, "test") { CreatedOnUTC = createdOn };

            // Act
            store.Add(entry);
            store.GetEntryOrDefault(id, out var retrieved);

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal(createdOn, retrieved.CreatedOnUTC);
        }

        [Fact]
        public void Store_Constructor_WithCapacity_CreatesStore()
        {
            // Arrange
            var dbPath = GetTempDbPath();

            // Act
            using var store = new Store<string>(dbPath, "test", 10, 100, _loggerFactory);

            // Assert
            Assert.False(store.Uncapped);
            store.GetCount(out var count);
            Assert.Equal(0, count);
        }

        [Fact]
        public void Store_Add_WhenGenerateNextIdReturnsNull_Fails()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new TestStoreWithNullIdGenerator(dbPath, "test", _loggerFactory);

            // Act
            var result = store.Add("test value", out var entry);

            // Assert
            Assert.False(result);
            Assert.Null(entry);
        }

        private class TestComplexValue
        {
            public string Name { get; set; } = string.Empty;
            public int Count { get; set; }
            public List<string> Items { get; set; } = new List<string>();
        }

        /// <summary>
        /// Test store that returns null from GenerateNextId to simulate ID generation failure
        /// and test error handling in the Add(TValue, out IEntry) method.
        /// </summary>
        private class TestStoreWithNullIdGenerator : Baubit.Caching.LiteDB.Store<int, string>
        {
            public TestStoreWithNullIdGenerator(string databasePath, string collectionName, ILoggerFactory loggerFactory)
                : base(databasePath, collectionName, loggerFactory)
            {
            }

            protected override int? GenerateNextId(int? lastGeneratedId)
            {
                // Simulates ID generation failure to test error handling in Add method
                return null;
            }
        }
    }
}
