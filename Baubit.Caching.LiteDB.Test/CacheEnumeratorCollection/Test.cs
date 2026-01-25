using LiteDB;

namespace Baubit.Caching.LiteDB.Test.CacheEnumeratorCollection
{
    public class Test
    {
        [Fact]
        public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
        {
            // Arrange
            using var db = new LiteDatabase(":memory:");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new LiteDB.CacheEnumeratorCollection<Guid>(null, db));
        }

        [Fact]
        public void Constructor_WithNullDatabase_ThrowsArgumentNullException()
        {
            // Arrange
            var config = new Configuration { ResumeSession = false };

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new LiteDB.CacheEnumeratorCollection<Guid>(config, null));
        }

        [Fact]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var config = new Configuration { ResumeSession = false };

            // Act
            var collection = new LiteDB.CacheEnumeratorCollection<Guid>(config, db);

            // Assert
            Assert.NotNull(collection);
            Assert.Equal(0, collection.Count);
            Assert.Null(collection.LowestReadId);
        }

        [Fact]
        public void Count_WhenResumeSessionDisabled_ReturnsBaseCount()
        {
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var config = new Configuration { ResumeSession = false };
            var collection = new LiteDB.CacheEnumeratorCollection<Guid>(config, db);

            // Act
            var count = collection.Count;

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void Count_WhenResumeSessionEnabledAndNoActiveEnumerators_ReturnsPersistedCount()
        {
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var config = new Configuration { ResumeSession = true };
            var collection = new LiteDB.CacheEnumeratorCollection<Guid>(config, db);

            // Add persisted positions
            var positionCollection = db.GetCollection<EnumeratorPosition<Guid>>("_enumerator_positions");
            positionCollection.Insert(new EnumeratorPosition<Guid>("session-1", Guid.NewGuid()));
            positionCollection.Insert(new EnumeratorPosition<Guid>("session-2", Guid.NewGuid()));

            // Act
            var count = collection.Count;

            // Assert
            Assert.Equal(2, count);
        }

        [Fact]
        public void LowestReadId_WhenResumeSessionDisabled_UsesActiveEnumerators()
        {
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var config = new Configuration { ResumeSession = false };
            var collection = new LiteDB.CacheEnumeratorCollection<Guid>(config, db);

            // Act
            var lowestId = collection.LowestReadId;

            // Assert
            Assert.Null(lowestId);
        }

        [Fact]
        public void LowestReadId_WhenResumeSessionEnabledAndNoPersistedPositions_ReturnsNull()
        {
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var config = new Configuration { ResumeSession = true };
            var collection = new LiteDB.CacheEnumeratorCollection<Guid>(config, db);

            // Act
            var lowestId = collection.LowestReadId;

            // Assert
            Assert.Null(lowestId);
        }

        [Fact]
        public void LowestReadId_WhenResumeSessionEnabledWithPersistedPositions_ReturnsMinimum()
        {
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var config = new Configuration { ResumeSession = true };
            var collection = new LiteDB.CacheEnumeratorCollection<Guid>(config, db);

            var id1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var id2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var id3 = Guid.Parse("00000000-0000-0000-0000-000000000003");

            var positionCollection = db.GetCollection<EnumeratorPosition<Guid>>("_enumerator_positions");
            positionCollection.Insert(new EnumeratorPosition<Guid>("session-1", id2));
            positionCollection.Insert(new EnumeratorPosition<Guid>("session-2", id3));
            positionCollection.Insert(new EnumeratorPosition<Guid>("session-3", id1));

            // Act
            var lowestId = collection.LowestReadId;

            // Assert
            Assert.Equal(id1, lowestId);
        }

        [Fact]
        public void LowestReadId_WhenResumeSessionEnabledWithMixedNullAndValidPositions_ReturnsMinimumNonNull()
        {
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var config = new Configuration { ResumeSession = true };
            var collection = new LiteDB.CacheEnumeratorCollection<Guid>(config, db);

            var id1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var id2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

            var positionCollection = db.GetCollection<EnumeratorPosition<Guid>>("_enumerator_positions");
            positionCollection.Insert(new EnumeratorPosition<Guid>("session-1", id2));
            positionCollection.Insert(new EnumeratorPosition<Guid>("session-2", null));
            positionCollection.Insert(new EnumeratorPosition<Guid>("session-3", id1));

            // Act
            var lowestId = collection.LowestReadId;

            // Assert
            Assert.Equal(id1, lowestId);
        }

        [Fact]
        public void LowestReadId_WhenResumeSessionEnabledWithOnlyNullPositions_ReturnsNull()
        {
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var config = new Configuration { ResumeSession = true };
            var collection = new LiteDB.CacheEnumeratorCollection<Guid>(config, db);

            var positionCollection = db.GetCollection<EnumeratorPosition<Guid>>("_enumerator_positions");
            positionCollection.Insert(new EnumeratorPosition<Guid>("session-1", null));
            positionCollection.Insert(new EnumeratorPosition<Guid>("session-2", null));

            // Act
            var lowestId = collection.LowestReadId;

            // Assert
            Assert.Null(lowestId);
        }

        [Fact]
        public void LowestReadId_WithIntType_ReturnsCorrectMinimum()
        {
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var config = new Configuration { ResumeSession = true };
            var collection = new LiteDB.CacheEnumeratorCollection<int>(config, db);

            var positionCollection = db.GetCollection<EnumeratorPosition<int>>("_enumerator_positions");
            positionCollection.Insert(new EnumeratorPosition<int>("session-1", 100));
            positionCollection.Insert(new EnumeratorPosition<int>("session-2", 50));
            positionCollection.Insert(new EnumeratorPosition<int>("session-3", 75));

            // Act
            var lowestId = collection.LowestReadId;

            // Assert
            Assert.Equal(50, lowestId);
        }

        [Fact]
        public void LowestReadId_WithLongType_ReturnsCorrectMinimum()
        {
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var config = new Configuration { ResumeSession = true };
            var collection = new LiteDB.CacheEnumeratorCollection<long>(config, db);

            var positionCollection = db.GetCollection<EnumeratorPosition<long>>("_enumerator_positions");
            positionCollection.Insert(new EnumeratorPosition<long>("session-1", 1000000L));
            positionCollection.Insert(new EnumeratorPosition<long>("session-2", 500000L));
            positionCollection.Insert(new EnumeratorPosition<long>("session-3", 750000L));

            // Act
            var lowestId = collection.LowestReadId;

            // Assert
            Assert.Equal(500000L, lowestId);
        }

        [Fact]
        public void LowestReadId_AfterPositionsUpdated_ReturnsNewMinimum()
        {
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var config = new Configuration { ResumeSession = true };
            var collection = new LiteDB.CacheEnumeratorCollection<Guid>(config, db);

            var id1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var id2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var id3 = Guid.Parse("00000000-0000-0000-0000-000000000003");

            var positionCollection = db.GetCollection<EnumeratorPosition<Guid>>("_enumerator_positions");
            positionCollection.Insert(new EnumeratorPosition<Guid>("session-1", id2));
            positionCollection.Insert(new EnumeratorPosition<Guid>("session-2", id3));

            var initialLowest = collection.LowestReadId;

            // Act - Insert a new position with lower ID
            positionCollection.Insert(new EnumeratorPosition<Guid>("session-3", id1));
            var newLowest = collection.LowestReadId;

            // Assert
            Assert.Equal(id2, initialLowest);
            Assert.Equal(id1, newLowest);
        }

        [Fact]
        public void Count_AfterPositionsRemoved_ReturnsUpdatedCount()
        {
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var config = new Configuration { ResumeSession = true };
            var collection = new LiteDB.CacheEnumeratorCollection<Guid>(config, db);

            var positionCollection = db.GetCollection<EnumeratorPosition<Guid>>("_enumerator_positions");
            positionCollection.Insert(new EnumeratorPosition<Guid>("session-1", Guid.NewGuid()));
            positionCollection.Insert(new EnumeratorPosition<Guid>("session-2", Guid.NewGuid()));
            positionCollection.Insert(new EnumeratorPosition<Guid>("session-3", Guid.NewGuid()));

            var initialCount = collection.Count;

            // Act - Remove a position
            positionCollection.Delete("session-2");
            var newCount = collection.Count;

            // Assert
            Assert.Equal(3, initialCount);
            Assert.Equal(2, newCount);
        }

        [Fact]
        public void Integration_LowestReadId_WorksWithRealDatabase()
        {
            // This test verifies that GetLowestPersistedReadId() correctly queries LiteDB
            // Arrange
            using var db = new LiteDatabase(":memory:");
            var config = new Configuration { ResumeSession = true };
            var collection = new LiteDB.CacheEnumeratorCollection<Guid>(config, db);

            var id1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var id2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var id3 = Guid.Parse("00000000-0000-0000-0000-000000000003");

            var positionCollection = db.GetCollection<EnumeratorPosition<Guid>>("_enumerator_positions");
            positionCollection.Insert(new EnumeratorPosition<Guid>("session-1", id2));
            positionCollection.Insert(new EnumeratorPosition<Guid>("session-2", id3));
            positionCollection.Insert(new EnumeratorPosition<Guid>("session-3", id1));

            // Act - Query multiple times to ensure it's querying the database each time
            var lowest1 = collection.LowestReadId;
            var lowest2 = collection.LowestReadId;

            // Assert
            Assert.Equal(id1, lowest1);
            Assert.Equal(id1, lowest2);
            
            // Verify count also works
            Assert.Equal(3, collection.Count);
        }
    }
}

