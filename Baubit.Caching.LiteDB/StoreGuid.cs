using LiteDB;
using Microsoft.Extensions.Logging;
using System;

namespace Baubit.Caching.LiteDB
{
    /// <summary>
    /// LiteDB-backed store implementation for Baubit.Caching using Guid as the ID type.
    /// Provides persistent, file-based storage as an L2 backing store with automatic ID generation.
    /// </summary>
    /// <typeparam name="TValue">The type of value stored in the cache.</typeparam>
    public class StoreGuid<TValue> : Store<Guid, TValue>
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
        public StoreGuid(string databasePath,
                     string collectionName,
                     long? minCap,
                     long? maxCap,
                     Func<Guid?, Guid?> nextIdFactory,
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
        public StoreGuid(string databasePath,
                     string collectionName,
                     Func<Guid?, Guid?> nextIdFactory,
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
        public StoreGuid(LiteDatabase database,
                     string collectionName,
                     long? minCap,
                     long? maxCap,
                     Func<Guid?, Guid?> nextIdFactory,
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
        public StoreGuid(LiteDatabase database,
                     string collectionName,
                     Func<Guid?, Guid?> nextIdFactory,
                     ILoggerFactory loggerFactory)
            : base(database, collectionName, nextIdFactory, loggerFactory)
        {
        }

        // Constructors with identityGenerator for backward compatibility
        /// <summary>
        /// Creates a new LiteDB-backed store with the specified database path.
        /// </summary>
        /// <param name="databasePath">Path to the LiteDB database file.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="minCap">Minimum capacity (null for uncapped).</param>
        /// <param name="maxCap">Maximum capacity (null for uncapped).</param>
        /// <param name="identityGenerator">Identity generator for creating entry IDs.</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public StoreGuid(string databasePath,
                     string collectionName,
                     long? minCap,
                     long? maxCap,
                     Baubit.Identity.IIdentityGenerator identityGenerator,
                     ILoggerFactory loggerFactory)
            : base(databasePath, collectionName, minCap, maxCap, CreateNextIdFactory(identityGenerator), loggerFactory)
        {
        }

        /// <summary>
        /// Creates a new uncapped LiteDB-backed store with the specified database path.
        /// </summary>
        /// <param name="databasePath">Path to the LiteDB database file.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="identityGenerator">Identity generator for creating entry IDs.</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public StoreGuid(string databasePath,
                     string collectionName,
                     Baubit.Identity.IIdentityGenerator identityGenerator,
                     ILoggerFactory loggerFactory)
            : base(databasePath, collectionName, CreateNextIdFactory(identityGenerator), loggerFactory)
        {
        }

        /// <summary>
        /// Creates a new LiteDB-backed store using an existing database instance.
        /// </summary>
        /// <param name="database">Existing LiteDB database instance.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="minCap">Minimum capacity (null for uncapped).</param>
        /// <param name="maxCap">Maximum capacity (null for uncapped).</param>
        /// <param name="identityGenerator">Identity generator for creating entry IDs.</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public StoreGuid(LiteDatabase database,
                     string collectionName,
                     long? minCap,
                     long? maxCap,
                     Baubit.Identity.IIdentityGenerator identityGenerator,
                     ILoggerFactory loggerFactory)
            : base(database, collectionName, minCap, maxCap, CreateNextIdFactory(identityGenerator), loggerFactory)
        {
        }

        /// <summary>
        /// Creates a new uncapped LiteDB-backed store using an existing database instance.
        /// </summary>
        /// <param name="database">Existing LiteDB database instance.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="identityGenerator">Identity generator for creating entry IDs.</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public StoreGuid(LiteDatabase database,
                     string collectionName,
                     Baubit.Identity.IIdentityGenerator identityGenerator,
                     ILoggerFactory loggerFactory)
            : base(database, collectionName, CreateNextIdFactory(identityGenerator), loggerFactory)
        {
        }

        // Backward-compatible constructors without identityGenerator (creates default)
        /// <summary>
        /// Creates a new LiteDB-backed store with the specified database path.
        /// Uses default GuidV7Generator for ID generation.
        /// </summary>
        /// <param name="databasePath">Path to the LiteDB database file.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="minCap">Minimum capacity (null for uncapped).</param>
        /// <param name="maxCap">Maximum capacity (null for uncapped).</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public StoreGuid(string databasePath,
                     string collectionName,
                     long? minCap,
                     long? maxCap,
                     ILoggerFactory loggerFactory)
            : this(databasePath, collectionName, minCap, maxCap, Baubit.Identity.IdentityGenerator.CreateNew(), loggerFactory)
        {
        }

        /// <summary>
        /// Creates a new uncapped LiteDB-backed store with the specified database path.
        /// Uses default GuidV7Generator for ID generation.
        /// </summary>
        /// <param name="databasePath">Path to the LiteDB database file.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public StoreGuid(string databasePath,
                     string collectionName,
                     ILoggerFactory loggerFactory)
            : this(databasePath, collectionName, Baubit.Identity.IdentityGenerator.CreateNew(), loggerFactory)
        {
        }

        /// <summary>
        /// Creates a new LiteDB-backed store using an existing database instance.
        /// Uses default GuidV7Generator for ID generation.
        /// </summary>
        /// <param name="database">Existing LiteDB database instance.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="minCap">Minimum capacity (null for uncapped).</param>
        /// <param name="maxCap">Maximum capacity (null for uncapped).</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public StoreGuid(LiteDatabase database,
                     string collectionName,
                     long? minCap,
                     long? maxCap,
                     ILoggerFactory loggerFactory)
            : this(database, collectionName, minCap, maxCap, Baubit.Identity.IdentityGenerator.CreateNew(), loggerFactory)
        {
        }

        /// <summary>
        /// Creates a new uncapped LiteDB-backed store using an existing database instance.
        /// Uses default GuidV7Generator for ID generation.
        /// </summary>
        /// <param name="database">Existing LiteDB database instance.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public StoreGuid(LiteDatabase database,
                     string collectionName,
                     ILoggerFactory loggerFactory)
            : this(database, collectionName, Baubit.Identity.IdentityGenerator.CreateNew(), loggerFactory)
        {
        }

        /// <summary>
        /// Creates a nextIdFactory function from an identityGenerator.
        /// </summary>
        private static Func<Guid?, Guid?> CreateNextIdFactory(Baubit.Identity.IIdentityGenerator identityGenerator)
        {
            if (identityGenerator == null)
                throw new ArgumentNullException(nameof(identityGenerator));

            return lastGeneratedId =>
            {
                // Initialize from last generated ID if available to ensure monotonicity
                if (lastGeneratedId.HasValue)
                {
                    identityGenerator.InitializeFrom(lastGeneratedId.Value);
                }
                return identityGenerator.GetNext();
            };
        }
    }
}
