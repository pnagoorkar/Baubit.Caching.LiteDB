using LiteDB;
using Microsoft.Extensions.Logging;
using System;

namespace Baubit.Caching.LiteDB
{
    /// <summary>
    /// LiteDB-backed store implementation for Baubit.Caching using int as the ID type.
    /// Provides persistent, file-based storage as an L2 backing store with sequential ID generation.
    /// </summary>
    /// <typeparam name="TValue">The type of value stored in the cache.</typeparam>
    public class StoreInt<TValue> : Store<int, TValue>
    {
        /// <summary>
        /// Creates a new LiteDB-backed store with the specified database path and custom ID factory.
        /// </summary>
        /// <param name="databasePath">Path to the LiteDB database file.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="minCap">Minimum capacity (null for uncapped).</param>
        /// <param name="maxCap">Maximum capacity (null for uncapped).</param>
        /// <param name="nextIdFactory">Function to generate the next ID. Takes the last generated ID and returns the next ID.</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public StoreInt(string databasePath,
                        string collectionName,
                        long? minCap,
                        long? maxCap,
                        Func<int?, int?> nextIdFactory,
                        ILoggerFactory loggerFactory)
            : base(databasePath, collectionName, minCap, maxCap, nextIdFactory, loggerFactory)
        {
        }

        /// <summary>
        /// Creates a new uncapped LiteDB-backed store with the specified database path and custom ID factory.
        /// </summary>
        /// <param name="databasePath">Path to the LiteDB database file.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="nextIdFactory">Function to generate the next ID. Takes the last generated ID and returns the next ID.</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public StoreInt(string databasePath,
                        string collectionName,
                        Func<int?, int?> nextIdFactory,
                        ILoggerFactory loggerFactory)
            : base(databasePath, collectionName, nextIdFactory, loggerFactory)
        {
        }

        /// <summary>
        /// Creates a new LiteDB-backed store using an existing database instance and custom ID factory.
        /// </summary>
        /// <param name="database">Existing LiteDB database instance.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="minCap">Minimum capacity (null for uncapped).</param>
        /// <param name="maxCap">Maximum capacity (null for uncapped).</param>
        /// <param name="nextIdFactory">Function to generate the next ID. Takes the last generated ID and returns the next ID.</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public StoreInt(LiteDatabase database,
                        string collectionName,
                        long? minCap,
                        long? maxCap,
                        Func<int?, int?> nextIdFactory,
                        ILoggerFactory loggerFactory)
            : base(database, collectionName, minCap, maxCap, nextIdFactory, loggerFactory)
        {
        }

        /// <summary>
        /// Creates a new uncapped LiteDB-backed store using an existing database instance and custom ID factory.
        /// </summary>
        /// <param name="database">Existing LiteDB database instance.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="nextIdFactory">Function to generate the next ID. Takes the last generated ID and returns the next ID.</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public StoreInt(LiteDatabase database,
                        string collectionName,
                        Func<int?, int?> nextIdFactory,
                        ILoggerFactory loggerFactory)
            : base(database, collectionName, nextIdFactory, loggerFactory)
        {
        }

        // Backward-compatible constructors without nextIdFactory (uses default sequential generation)
        /// <summary>
        /// Creates a new LiteDB-backed store with the specified database path.
        /// Uses default sequential ID generation starting from 1.
        /// </summary>
        /// <param name="databasePath">Path to the LiteDB database file.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="minCap">Minimum capacity (null for uncapped).</param>
        /// <param name="maxCap">Maximum capacity (null for uncapped).</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public StoreInt(string databasePath,
                        string collectionName,
                        long? minCap,
                        long? maxCap,
                        ILoggerFactory loggerFactory)
            : base(databasePath, collectionName, minCap, maxCap, CreateDefaultNextIdFactory(), loggerFactory)
        {
        }

        /// <summary>
        /// Creates a new uncapped LiteDB-backed store with the specified database path.
        /// Uses default sequential ID generation starting from 1.
        /// </summary>
        /// <param name="databasePath">Path to the LiteDB database file.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public StoreInt(string databasePath,
                        string collectionName,
                        ILoggerFactory loggerFactory)
            : base(databasePath, collectionName, CreateDefaultNextIdFactory(), loggerFactory)
        {
        }

        /// <summary>
        /// Creates a new LiteDB-backed store using an existing database instance.
        /// Uses default sequential ID generation starting from 1.
        /// </summary>
        /// <param name="database">Existing LiteDB database instance.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="minCap">Minimum capacity (null for uncapped).</param>
        /// <param name="maxCap">Maximum capacity (null for uncapped).</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public StoreInt(LiteDatabase database,
                        string collectionName,
                        long? minCap,
                        long? maxCap,
                        ILoggerFactory loggerFactory)
            : base(database, collectionName, minCap, maxCap, CreateDefaultNextIdFactory(), loggerFactory)
        {
        }

        /// <summary>
        /// Creates a new uncapped LiteDB-backed store using an existing database instance.
        /// Uses default sequential ID generation starting from 1.
        /// </summary>
        /// <param name="database">Existing LiteDB database instance.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public StoreInt(LiteDatabase database,
                        string collectionName,
                        ILoggerFactory loggerFactory)
            : base(database, collectionName, CreateDefaultNextIdFactory(), loggerFactory)
        {
        }

        /// <summary>
        /// Creates the default sequential ID factory for int IDs.
        /// </summary>
        private static Func<int?, int?> CreateDefaultNextIdFactory()
        {
            int nextId = 1;
            return lastGeneratedId =>
            {
                if (lastGeneratedId.HasValue)
                {
                    nextId = lastGeneratedId.Value + 1;
                }
                return nextId++;
            };
        }
    }
}
