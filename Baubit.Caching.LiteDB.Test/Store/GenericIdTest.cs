using Baubit.Caching.LiteDB;
using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Baubit.Caching.LiteDB.Test.Store
{
    /// <summary>
    /// Tests for <see cref="Baubit.Caching.LiteDB.StoreLong{TValue}"/> and <see cref="Baubit.Caching.LiteDB.StoreInt{TValue}"/>
    /// </summary>
    public class GenericIdTest : IDisposable
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

        #region StoreLong<TValue> Tests

        [Fact]
        public void StoreLong_Constructor_UncappedStore()
        {
            // Arrange & Act
            var dbPath = GetTempDbPath();
            using var store = new StoreLong<string>(dbPath, "test", _loggerFactory);

            // Assert
            Assert.True(store.Uncapped);
            Assert.Null(store.MinCapacity);
            Assert.Null(store.MaxCapacity);
        }

        [Fact]
        public void StoreLong_Add_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new StoreLong<string>(dbPath, "test", _loggerFactory);
            long id = 12345L;

            // Act
            var result = store.Add(id, "test value", out var entry);

            // Assert
            Assert.True(result);
            Assert.Equal(id, entry.Id);
            Assert.Equal("test value", entry.Value);
        }

        [Fact]
        public void StoreLong_AddWithAutoGeneration_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new StoreLong<string>(dbPath, "test", _loggerFactory);

            // Act - Add without explicit ID uses GenerateNextId
            var result = store.Add("test value", out var entry);

            // Assert
            Assert.True(result);
            Assert.True(entry.Id > 0);
            Assert.Equal("test value", entry.Value);
        }

        [Fact]
        public void StoreLong_GetEntryOrDefault_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new StoreLong<string>(dbPath, "test", _loggerFactory);
            store.Add(42L, "test value", out _);

            // Act
            var result = store.GetEntryOrDefault(42L, out var entry);

            // Assert
            Assert.True(result);
            Assert.NotNull(entry);
            Assert.Equal(42L, entry.Id);
            Assert.Equal("test value", entry.Value);
        }

        [Fact]
        public void StoreLong_Update_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new StoreLong<string>(dbPath, "test", _loggerFactory);
            store.Add(99L, "original", out _);

            // Act
            var result = store.Update(99L, "updated");

            // Assert
            Assert.True(result);
            store.GetValueOrDefault(99L, out var value);
            Assert.Equal("updated", value);
        }

        [Fact]
        public void StoreLong_DataPersists_AfterReopen()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            long id = 777L;

            // Act - Write data
            using (var store = new StoreLong<string>(dbPath, "test", _loggerFactory))
            {
                store.Add(id, "persisted value", out _);
            }

            // Act - Read data after reopen
            using (var store = new StoreLong<string>(dbPath, "test", _loggerFactory))
            {
                // Assert
                var result = store.GetValueOrDefault(id, out var value);
                Assert.True(result);
                Assert.Equal("persisted value", value);
            }
        }

        [Fact]
        public void StoreLong_Capacity_Works()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new StoreLong<string>(dbPath, "test", 2, 2, _loggerFactory);

            // Act
            var result1 = store.Add(1L, "first", out _);
            var result2 = store.Add(2L, "second", out _);
            var result3 = store.Add(3L, "third", out _);

            // Assert
            Assert.True(result1);
            Assert.True(result2);
            Assert.False(result3); // Should fail due to capacity
            Assert.False(store.HasCapacity);
        }

        [Fact]
        public void StoreLong_Remove_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new StoreLong<string>(dbPath, "test", _loggerFactory);
            store.Add(1L, "first", out _);
            store.Add(2L, "second", out _);
            store.Add(3L, "third", out _);

            // Act
            var result1 = store.Remove(1L, out var entry1);
            var result2 = store.Remove(3L, out var entry2);

            // Assert
            Assert.True(result1);
            Assert.Equal("first", entry1.Value);
            Assert.True(result2);
            Assert.Equal("third", entry2.Value);
            
            // Verify removal
            Assert.False(store.GetEntryOrDefault(1L, out _));
            Assert.False(store.GetEntryOrDefault(3L, out _));
            Assert.True(store.GetEntryOrDefault(2L, out _));
        }

        #endregion

        #region StoreInt<TValue> Tests

        [Fact]
        public void StoreInt_Constructor_UncappedStore()
        {
            // Arrange & Act
            var dbPath = GetTempDbPath();
            using var store = new StoreInt<string>(dbPath, "test", _loggerFactory);

            // Assert
            Assert.True(store.Uncapped);
        }

        [Fact]
        public void StoreInt_Add_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new StoreInt<string>(dbPath, "test", _loggerFactory);
            int id = 42;

            // Act
            var result = store.Add(id, "test value", out var entry);

            // Assert
            Assert.True(result);
            Assert.Equal(id, entry.Id);
            Assert.Equal("test value", entry.Value);
        }

        [Fact]
        public void StoreInt_AddWithAutoGeneration_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new StoreInt<string>(dbPath, "test", _loggerFactory);

            // Act - Add without explicit ID uses GenerateNextId
            var result = store.Add("test value", out var entry);

            // Assert
            Assert.True(result);
            Assert.True(entry.Id > 0);
            Assert.Equal("test value", entry.Value);
        }

        [Fact]
        public void StoreInt_NegativeIds_Work()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new StoreInt<string>(dbPath, "test", _loggerFactory);

            // Act - Add with explicit IDs
            var result1 = store.Add(-100, "negative", out _);
            var result3 = store.Add(100, "positive", out _);

            // Assert - verify all were added and can be retrieved
            Assert.True(result1);
            Assert.True(result3);
            
            Assert.True(store.GetValueOrDefault(-100, out var value1));
            Assert.Equal("negative", value1);
            Assert.True(store.GetValueOrDefault(100, out var value3));
            Assert.Equal("positive", value3);
        }

        [Fact]
        public void StoreInt_Remove_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new StoreInt<string>(dbPath, "test", _loggerFactory);
            store.Add(1, "first", out _);
            store.Add(2, "second", out _);
            store.Add(3, "third", out _);

            // Act
            store.Remove(1, out _);
            store.Remove(3, out _);

            // Assert - verify only middle item remains
            Assert.False(store.GetEntryOrDefault(1, out _));
            Assert.True(store.GetEntryOrDefault(2, out _));
            Assert.False(store.GetEntryOrDefault(3, out _));
        }

        [Fact]
        public void StoreInt_GetCount_ReturnsCorrectCount()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new StoreInt<string>(dbPath, "test", _loggerFactory);
            store.Add(1, "first", out _);
            store.Add(2, "second", out _);
            store.Add(3, "third", out _);

            // Act
            var result = store.GetCount(out var count);

            // Assert
            Assert.True(result);
            Assert.Equal(3, count);
        }

        [Fact]
        public void StoreInt_DataPersists_AfterReopen()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            int id = 123;

            // Act - Write data
            using (var store = new StoreInt<string>(dbPath, "test", _loggerFactory))
            {
                store.Add(id, "persisted value", out _);
            }

            // Act - Read data after reopen
            using (var store = new StoreInt<string>(dbPath, "test", _loggerFactory))
            {
                // Assert
                var result = store.GetValueOrDefault(id, out var value);
                Assert.True(result);
                Assert.Equal("persisted value", value);
            }
        }

        [Fact]
        public void StoreInt_ComplexValueType_Works()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new StoreInt<TestComplexValue>(dbPath, "test", _loggerFactory);
            var complexValue = new TestComplexValue
            {
                Name = "Test",
                Count = 42,
                Items = new List<string> { "a", "b", "c" }
            };

            // Act
            store.Add(999, complexValue, out _);
            store.GetValueOrDefault(999, out var retrieved);

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal("Test", retrieved.Name);
            Assert.Equal(42, retrieved.Count);
            Assert.Equal(3, retrieved.Items.Count);
        }

        #endregion

        private class TestComplexValue
        {
            public string Name { get; set; } = string.Empty;
            public int Count { get; set; }
            public List<string> Items { get; set; } = new List<string>();
        }
    }
}
