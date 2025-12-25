using Baubit.Caching.LiteDB;
using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Baubit.Caching.LiteDB.Test.Store
{
    /// <summary>
    /// Tests for <see cref="Baubit.Caching.LiteDB.Store{TId, TValue}"/> with generic ID types (long, int)
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

        #region Store<long, TValue> Tests

        [Fact]
        public void StoreLong_Constructor_UncappedStore()
        {
            // Arrange & Act
            var dbPath = GetTempDbPath();
            using var store = new Store<long, string>(dbPath, "test", _loggerFactory);

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
            using var store = new Store<long, string>(dbPath, "test", _loggerFactory);
            long id = 12345L;

            // Act
            var result = store.Add(id, "test value", out var entry);

            // Assert
            Assert.True(result);
            Assert.Equal(id, entry.Id);
            Assert.Equal("test value", entry.Value);
        }

        [Fact]
        public void StoreLong_AddWithoutId_ThrowsNotSupportedException()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<long, string>(dbPath, "test", _loggerFactory);

            // Act & Assert
            Assert.Throws<NotSupportedException>(() =>
                store.Add("test value", out var entry));
        }

        [Fact]
        public void StoreLong_HeadId_ReturnsMinimumId()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<long, string>(dbPath, "test", _loggerFactory);

            // Act
            store.Add(100L, "first", out _);
            store.Add(50L, "second", out _);
            store.Add(200L, "third", out _);

            // Assert
            Assert.Equal(50L, store.HeadId);
        }

        [Fact]
        public void StoreLong_TailId_ReturnsMaximumId()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<long, string>(dbPath, "test", _loggerFactory);

            // Act
            store.Add(100L, "first", out _);
            store.Add(50L, "second", out _);
            store.Add(200L, "third", out _);

            // Assert
            Assert.Equal(200L, store.TailId);
        }

        [Fact]
        public void StoreLong_Remove_UpdatesHeadAndTail()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<long, string>(dbPath, "test", _loggerFactory);
            store.Add(1L, "first", out _);
            store.Add(2L, "second", out _);
            store.Add(3L, "third", out _);

            // Act
            store.Remove(1L, out _); // Remove head
            store.Remove(3L, out _); // Remove tail

            // Assert
            Assert.Equal(2L, store.HeadId);
            Assert.Equal(2L, store.TailId);
        }

        [Fact]
        public void StoreLong_GetEntryOrDefault_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<long, string>(dbPath, "test", _loggerFactory);
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
            using var store = new Store<long, string>(dbPath, "test", _loggerFactory);
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
            using (var store = new Store<long, string>(dbPath, "test", _loggerFactory))
            {
                store.Add(id, "persisted value", out _);
            }

            // Act - Read data after reopen
            using (var store = new Store<long, string>(dbPath, "test", _loggerFactory))
            {
                // Assert
                var result = store.GetValueOrDefault(id, out var value);
                Assert.True(result);
                Assert.Equal("persisted value", value);
                Assert.Equal(777L, store.HeadId);
                Assert.Equal(777L, store.TailId);
            }
        }

        [Fact]
        public void StoreLong_Capacity_Works()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<long, string>(dbPath, "test", 2, 2, _loggerFactory);

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

        #endregion

        #region Store<int, TValue> Tests

        [Fact]
        public void StoreInt_Constructor_UncappedStore()
        {
            // Arrange & Act
            var dbPath = GetTempDbPath();
            using var store = new Store<int, string>(dbPath, "test", _loggerFactory);

            // Assert
            Assert.True(store.Uncapped);
        }

        [Fact]
        public void StoreInt_Add_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<int, string>(dbPath, "test", _loggerFactory);
            int id = 42;

            // Act
            var result = store.Add(id, "test value", out var entry);

            // Assert
            Assert.True(result);
            Assert.Equal(id, entry.Id);
            Assert.Equal("test value", entry.Value);
        }

        [Fact]
        public void StoreInt_AddWithoutId_ThrowsNotSupportedException()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<int, string>(dbPath, "test", _loggerFactory);

            // Act & Assert
            Assert.Throws<NotSupportedException>(() =>
                store.Add("test value", out var entry));
        }

        [Fact]
        public void StoreInt_HeadId_ReturnsMinimumId()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<int, string>(dbPath, "test", _loggerFactory);

            // Act
            store.Add(100, "first", out _);
            store.Add(50, "second", out _);
            store.Add(200, "third", out _);

            // Assert
            Assert.Equal(50, store.HeadId);
        }

        [Fact]
        public void StoreInt_TailId_ReturnsMaximumId()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<int, string>(dbPath, "test", _loggerFactory);

            // Act
            store.Add(100, "first", out _);
            store.Add(50, "second", out _);
            store.Add(200, "third", out _);

            // Assert
            Assert.Equal(200, store.TailId);
        }

        [Fact]
        public void StoreInt_NegativeIds_Work()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<int, string>(dbPath, "test", _loggerFactory);

            // Act
            store.Add(-100, "negative", out _);
            store.Add(0, "zero", out _);
            store.Add(100, "positive", out _);

            // Assert
            Assert.Equal(-100, store.HeadId);
            Assert.Equal(100, store.TailId);
            store.GetValueOrDefault(-100, out var value);
            Assert.Equal("negative", value);
        }

        [Fact]
        public void StoreInt_Remove_UpdatesHeadAndTail()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<int, string>(dbPath, "test", _loggerFactory);
            store.Add(1, "first", out _);
            store.Add(2, "second", out _);
            store.Add(3, "third", out _);

            // Act
            store.Remove(1, out _); // Remove head
            store.Remove(3, out _); // Remove tail

            // Assert
            Assert.Equal(2, store.HeadId);
            Assert.Equal(2, store.TailId);
        }

        [Fact]
        public void StoreInt_GetCount_ReturnsCorrectCount()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<int, string>(dbPath, "test", _loggerFactory);
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
            using (var store = new Store<int, string>(dbPath, "test", _loggerFactory))
            {
                store.Add(id, "persisted value", out _);
            }

            // Act - Read data after reopen
            using (var store = new Store<int, string>(dbPath, "test", _loggerFactory))
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
            using var store = new Store<int, TestComplexValue>(dbPath, "test", _loggerFactory);
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

        #region Store<Guid, TValue> Tests (using generic Store directly)

        [Fact]
        public void StoreGuid_Add_Success()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<Guid, string>(dbPath, "test", _loggerFactory);
            var id = Guid.NewGuid();

            // Act
            var result = store.Add(id, "test value", out var entry);

            // Assert
            Assert.True(result);
            Assert.Equal(id, entry.Id);
            Assert.Equal("test value", entry.Value);
        }

        [Fact]
        public void StoreGuid_AddWithoutId_ThrowsNotSupportedException()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<Guid, string>(dbPath, "test", _loggerFactory);

            // Act & Assert
            Assert.Throws<NotSupportedException>(() =>
                store.Add("test value", out var entry));
        }

        [Fact]
        public void StoreGuid_HeadTail_Work()
        {
            // Arrange
            var dbPath = GetTempDbPath();
            using var store = new Store<Guid, string>(dbPath, "test", _loggerFactory);
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var id3 = Guid.NewGuid();
            var minId = new[] { id1, id2, id3 }.Min();
            var maxId = new[] { id1, id2, id3 }.Max();

            // Act
            store.Add(id1, "first", out _);
            store.Add(id2, "second", out _);
            store.Add(id3, "third", out _);

            // Assert
            Assert.Equal(minId, store.HeadId);
            Assert.Equal(maxId, store.TailId);
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
