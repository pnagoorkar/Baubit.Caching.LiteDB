using Baubit.Caching.LiteDB;

namespace Baubit.Caching.LiteDB.Test.Entry
{
    /// <summary>
    /// Tests for <see cref="Baubit.Caching.LiteDB.Entry{TValue}"/>
    /// </summary>
    public class Test
    {
        [Fact]
        public void Entry_Constructor_WithIdAndValue_SetsProperties()
        {
            // Arrange
            var id = Guid.NewGuid();
            var value = "test value";

            // Act
            var entry = new Entry<string>(id, value);

            // Assert
            Assert.Equal(id, entry.Id);
            Assert.Equal(value, entry.Value);
            Assert.True(entry.CreatedOnUTC <= DateTime.UtcNow);
            Assert.True(entry.CreatedOnUTC > DateTime.UtcNow.AddMinutes(-1));
        }

        [Fact]
        public void Entry_ParameterlessConstructor_Works()
        {
            // Act
            var entry = new Entry<int>();

            // Assert
            Assert.Equal(Guid.Empty, entry.Id);
            Assert.Equal(0, entry.Value);
            Assert.True(entry.CreatedOnUTC <= DateTime.UtcNow);
        }

        [Fact]
        public void Entry_Properties_CanBeSet()
        {
            // Arrange
            var entry = new Entry<string>();
            var id = Guid.NewGuid();
            var createdOn = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

            // Act
            entry.Id = id;
            entry.Value = "updated";
            entry.CreatedOnUTC = createdOn;

            // Assert
            Assert.Equal(id, entry.Id);
            Assert.Equal("updated", entry.Value);
            Assert.Equal(createdOn, entry.CreatedOnUTC);
        }

        [Fact]
        public void Entry_ImplementsIEntry()
        {
            // Arrange
            var id = Guid.NewGuid();
            var entry = new Entry<string>(id, "test");

            // Act - Entry<string> implements IEntry<Guid, string>
            Baubit.Caching.IEntry<Guid, string> ientry = entry;

            // Assert
            Assert.Equal(id, ientry.Id);
            Assert.Equal("test", ientry.Value);
            Assert.NotEqual(default, ientry.CreatedOnUTC);
        }

        [Fact]
        public void Entry_WithComplexType_Works()
        {
            // Arrange
            var id = Guid.NewGuid();
            var complexValue = new List<int> { 1, 2, 3 };

            // Act
            var entry = new Entry<List<int>>(id, complexValue);

            // Assert
            Assert.Equal(id, entry.Id);
            Assert.Equal(3, entry.Value.Count);
            Assert.Contains(2, entry.Value);
        }

        [Fact]
        public void Entry_WithNullValue_Works()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var entry = new Entry<string?>(id, null);

            // Assert
            Assert.Equal(id, entry.Id);
            Assert.Null(entry.Value);
        }

        [Fact]
        public void Entry_ValueType_Works()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var entry = new Entry<int>(id, 42);

            // Assert
            Assert.Equal(id, entry.Id);
            Assert.Equal(42, entry.Value);
        }
    }
}
