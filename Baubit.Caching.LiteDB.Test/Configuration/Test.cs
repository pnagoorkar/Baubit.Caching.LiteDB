using Baubit.Caching.LiteDB;

namespace Baubit.Caching.LiteDB.Test.ConfigurationTest
{
    /// <summary>
    /// Tests for Configuration class with LiteDB-specific settings
    /// </summary>
    public class Test
    {
        [Fact]
        public void Configuration_ResumeSession_DefaultIsFalse()
        {
            // Arrange & Act
            var config = new Baubit.Caching.LiteDB.Configuration();

            // Assert
            Assert.False(config.ResumeSession);
        }

        [Fact]
        public void Configuration_ResumeSession_CanBeSet()
        {
            // Arrange
            var config = new Baubit.Caching.LiteDB.Configuration();

            // Act
            config.ResumeSession = true;

            // Assert
            Assert.True(config.ResumeSession);
        }

        [Fact]
        public void Configuration_PersistPositionEveryXMoves_DefaultIsZero()
        {
            // Arrange & Act
            var config = new Baubit.Caching.LiteDB.Configuration();

            // Assert
            Assert.Equal(0, config.PersistPositionEveryXMoves);
        }

        [Fact]
        public void Configuration_PersistPositionEveryXMoves_CanBeSet()
        {
            // Arrange
            var config = new Baubit.Caching.LiteDB.Configuration();

            // Act
            config.PersistPositionEveryXMoves = 10;

            // Assert
            Assert.Equal(10, config.PersistPositionEveryXMoves);
        }

        [Fact]
        public void Configuration_PersistPositionBeforeMove_DefaultIsTrue()
        {
            // Arrange & Act
            var config = new Baubit.Caching.LiteDB.Configuration();

            // Assert
            Assert.True(config.PersistPositionBeforeMove);
        }

        [Fact]
        public void Configuration_PersistPositionBeforeMove_CanBeSet()
        {
            // Arrange
            var config = new Baubit.Caching.LiteDB.Configuration();

            // Act
            config.PersistPositionBeforeMove = false;

            // Assert
            Assert.False(config.PersistPositionBeforeMove);
        }

        [Fact]
        public void Configuration_InheritsFromBaseCachingConfiguration()
        {
            // Arrange & Act
            var config = new Baubit.Caching.LiteDB.Configuration
            {
                RunAdaptiveResizing = false,
                EvictAfterEveryX = 1000
            };

            // Assert
            Assert.False(config.RunAdaptiveResizing);
            Assert.Equal(1000, config.EvictAfterEveryX);
        }
    }
}
