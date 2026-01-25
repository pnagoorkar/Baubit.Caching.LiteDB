using System;
using LiteDB;
using Xunit;

namespace Baubit.Caching.LiteDB.Test.EnumeratorPosition
{
    public class Test
    {
        [Fact]
        public void Constructor_DefaultParameters_CreatesInstanceWithDefaults()
        {
            // Act
            var position = new LiteDB.EnumeratorPosition<Guid>();

            // Assert
            Assert.Equal("", position.SessionId);
            Assert.Null(position.CurrentId);
            Assert.Equal(DateTimeKind.Utc, position.LastUpdatedUTC.Kind);
        }

        [Fact]
        public void Constructor_WithSessionIdAndCurrentId_CreatesInstanceCorrectly()
        {
            // Arrange
            var sessionId = "test-session";
            var currentId = Guid.NewGuid();

            // Act
            var position = new LiteDB.EnumeratorPosition<Guid>(sessionId, currentId);

            // Assert
            Assert.Equal(sessionId, position.SessionId);
            Assert.Equal(currentId, position.CurrentId);
            Assert.Equal(DateTimeKind.Utc, position.LastUpdatedUTC.Kind);
        }

        [Fact]
        public void Constructor_WithExplicitTimestamp_UsesProvidedTimestamp()
        {
            // Arrange
            var sessionId = "test-session";
            var currentId = Guid.NewGuid();
            var timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

            // Act
            var position = new LiteDB.EnumeratorPosition<Guid>(sessionId, currentId, timestamp);

            // Assert
            Assert.Equal(sessionId, position.SessionId);
            Assert.Equal(currentId, position.CurrentId);
            Assert.Equal(timestamp, position.LastUpdatedUTC);
            Assert.Equal(DateTimeKind.Utc, position.LastUpdatedUTC.Kind);
        }

        [Fact]
        public void LastUpdatedUTC_SetterConvertsToUtc_WhenKindIsUnspecified()
        {
            // Arrange
            var position = new LiteDB.EnumeratorPosition<Guid>();
            var unspecifiedTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Unspecified);

            // Act
            position.LastUpdatedUTC = unspecifiedTime;

            // Assert
            Assert.Equal(DateTimeKind.Utc, position.LastUpdatedUTC.Kind);
            Assert.Equal(unspecifiedTime.Ticks, position.LastUpdatedUTC.Ticks);
        }

        [Fact]
        public void LastUpdatedUTC_SetterConvertsToUtc_WhenKindIsLocal()
        {
            // Arrange
            var position = new LiteDB.EnumeratorPosition<Guid>();
            var localTime = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Local);

            // Act
            position.LastUpdatedUTC = localTime;

            // Assert
            Assert.Equal(DateTimeKind.Utc, position.LastUpdatedUTC.Kind);
            Assert.Equal(localTime.Ticks, position.LastUpdatedUTC.Ticks);
        }

        [Fact]
        public void LiteDB_Roundtrip_PreservesUtcKind()
        {
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var collection = db.GetCollection<LiteDB.EnumeratorPosition<Guid>>("test");
            
            var sessionId = "test-session";
            var currentId = Guid.NewGuid();
            var timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
            var original = new LiteDB.EnumeratorPosition<Guid>(sessionId, currentId, timestamp);

            // Act
            collection.Insert(original);
            var retrieved = collection.FindById(sessionId);

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal(original.SessionId, retrieved.SessionId);
            Assert.Equal(original.CurrentId, retrieved.CurrentId);
            Assert.Equal(original.LastUpdatedUTC, retrieved.LastUpdatedUTC);
            Assert.Equal(DateTimeKind.Utc, retrieved.LastUpdatedUTC.Kind);
        }

        [Fact]
        public void LiteDB_Roundtrip_WithNullCurrentId_PreservesUtcKind()
        {
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var collection = db.GetCollection<LiteDB.EnumeratorPosition<Guid>>("test");
            
            var sessionId = "test-session";
            var timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
            var original = new LiteDB.EnumeratorPosition<Guid>(sessionId, null, timestamp);

            // Act
            collection.Insert(original);
            var retrieved = collection.FindById(sessionId);

            // Assert
            Assert.NotNull(retrieved);
            Assert.Equal(original.SessionId, retrieved.SessionId);
            Assert.Null(retrieved.CurrentId);
            Assert.Equal(original.LastUpdatedUTC, retrieved.LastUpdatedUTC);
            Assert.Equal(DateTimeKind.Utc, retrieved.LastUpdatedUTC.Kind);
        }

        [Fact]
        public void LiteDB_Roundtrip_MultiplePositions_PreservesUtcKindForAll()
        {
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var collection = db.GetCollection<LiteDB.EnumeratorPosition<long>>("test");
            
            var positions = new[]
            {
                new LiteDB.EnumeratorPosition<long>("session1", 100L, new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc)),
                new LiteDB.EnumeratorPosition<long>("session2", 200L, new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc)),
                new LiteDB.EnumeratorPosition<long>("session3", 300L, new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc))
            };

            // Act
            foreach (var pos in positions)
            {
                collection.Insert(pos);
            }

            var retrieved = collection.FindAll();

            // Assert
            foreach (var pos in retrieved)
            {
                Assert.Equal(DateTimeKind.Utc, pos.LastUpdatedUTC.Kind);
            }
        }
    }
}
