using LiteDB;
using Microsoft.Extensions.Logging;

namespace Baubit.Caching.LiteDB
{
    /// <summary>
    /// LiteDB-backed store implementation for Baubit.Caching using long as the ID type.
    /// Provides persistent, file-based storage as an L2 backing store with sequential ID generation.
    /// </summary>
    /// <typeparam name="TValue">The type of value stored in the cache.</typeparam>
    public class StoreLong<TValue> : Store<long, TValue>
    {
        private long nextId = 1;

        /// <summary>
        /// Creates a new LiteDB-backed store with the specified database path.
        /// </summary>
        public StoreLong(string databasePath, string collectionName, long? minCap, long? maxCap, ILoggerFactory loggerFactory)
            : base(databasePath, collectionName, minCap, maxCap, loggerFactory) { }

        /// <summary>
        /// Creates a new uncapped LiteDB-backed store with the specified database path.
        /// </summary>
        public StoreLong(string databasePath, string collectionName, ILoggerFactory loggerFactory)
            : base(databasePath, collectionName, loggerFactory) { }

        /// <summary>
        /// Creates a new LiteDB-backed store using an existing database instance.
        /// </summary>
        public StoreLong(LiteDatabase database, string collectionName, long? minCap, long? maxCap, ILoggerFactory loggerFactory)
            : base(database, collectionName, minCap, maxCap, loggerFactory) { }

        /// <summary>
        /// Creates a new uncapped LiteDB-backed store using an existing database instance.
        /// </summary>
        public StoreLong(LiteDatabase database, string collectionName, ILoggerFactory loggerFactory)
            : base(database, collectionName, loggerFactory) { }

        /// <inheritdoc/>
        protected override long? GenerateNextId(long? lastGeneratedId)
        {
            if (lastGeneratedId.HasValue)
            {
                nextId = lastGeneratedId.Value + 1;
            }
            return nextId++;
        }
    }
}
