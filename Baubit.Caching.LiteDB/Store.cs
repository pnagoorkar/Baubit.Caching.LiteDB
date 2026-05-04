using LiteDB;
using Microsoft.Extensions.Logging;
using System;

namespace Baubit.Caching.LiteDB
{
    /// <summary>
    /// LiteDB-backed store implementation for Baubit.Caching.
    /// Provides persistent, file-based storage as an L2 backing store.
    /// </summary>
    /// <typeparam name="TId">The type of the unique identifier. Must be a value type that implements IComparable and IEquatable.</typeparam>
    /// <typeparam name="TValue">The type of value stored in the cache.</typeparam>
    public class Store<TId, TValue> : Baubit.Caching.Store<TId, TValue>
        where TId : struct, IComparable<TId>, IEquatable<TId>
    {
        private readonly LiteDatabase database;
        private readonly ILiteCollection<Entry<TId, TValue>> collection;
        private readonly ILogger<Store<TId, TValue>> logger;
        private readonly bool ownsDatabase;
        private readonly Func<TId?, TId?> nextIdFactory;
        private TId? lastGeneratedId;

        /// <summary>
        /// Gets or sets the last ID that was added to the store.
        /// Used to maintain ID continuity across store operations.
        /// </summary>
        public override TId? LastAddedId
        {
            get => lastGeneratedId;
            protected set => lastGeneratedId = value;
        }

        /// <summary>
        /// Creates a new LiteDB-backed store with the specified database path.
        /// </summary>
        /// <param name="databasePath">Path to the LiteDB database file.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="minCap">Minimum capacity (null for uncapped).</param>
        /// <param name="maxCap">Maximum capacity (null for uncapped).</param>
        /// <param name="nextIdFactory">Function to generate the next ID. Takes the last generated ID and returns the next ID.</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public Store(string databasePath,
                     string collectionName,
                     long? minCap,
                     long? maxCap,
                     Func<TId?, TId?> nextIdFactory,
                     ILoggerFactory loggerFactory)
            : base(minCap, maxCap, loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<Store<TId, TValue>>();
            this.nextIdFactory = nextIdFactory ?? throw new ArgumentNullException(nameof(nextIdFactory));

            this.database = new LiteDatabase(new ConnectionString
            {
                Filename = databasePath,
                Upgrade = true
            });
            this.ownsDatabase = true;
            this.collection = database.GetCollection<Entry<TId, TValue>>(collectionName);
            InitializeHeadTail();
        }

        /// <summary>
        /// Creates a new uncapped LiteDB-backed store with the specified database path.
        /// </summary>
        /// <param name="databasePath">Path to the LiteDB database file.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="nextIdFactory">Function to generate the next ID. Takes the last generated ID and returns the next ID.</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public Store(string databasePath,
                     string collectionName,
                     Func<TId?, TId?> nextIdFactory,
                     ILoggerFactory loggerFactory)
            : this(databasePath, collectionName, null, null, nextIdFactory, loggerFactory)
        {
        }

        /// <summary>
        /// Creates a new LiteDB-backed store using an existing database instance.
        /// </summary>
        /// <param name="database">Existing LiteDB database instance.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="minCap">Minimum capacity (null for uncapped).</param>
        /// <param name="maxCap">Maximum capacity (null for uncapped).</param>
        /// <param name="nextIdFactory">Function to generate the next ID. Takes the last generated ID and returns the next ID.</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public Store(LiteDatabase database,
                     string collectionName,
                     long? minCap,
                     long? maxCap,
                     Func<TId?, TId?> nextIdFactory,
                     ILoggerFactory loggerFactory)
            : base(minCap, maxCap, loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<Store<TId, TValue>>();
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            this.nextIdFactory = nextIdFactory ?? throw new ArgumentNullException(nameof(nextIdFactory));
            this.ownsDatabase = false;
            this.collection = database.GetCollection<Entry<TId, TValue>>(collectionName);
            InitializeHeadTail();
        }

        /// <summary>
        /// Creates a new uncapped LiteDB-backed store using an existing database instance.
        /// </summary>
        /// <param name="database">Existing LiteDB database instance.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="nextIdFactory">Function to generate the next ID. Takes the last generated ID and returns the next ID.</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public Store(LiteDatabase database,
                     string collectionName,
                     Func<TId?, TId?> nextIdFactory,
                     ILoggerFactory loggerFactory)
            : this(database, collectionName, null, null, nextIdFactory, loggerFactory)
        {
        }

        private void InitializeHeadTail()
        {
            var head = collection.Query().OrderBy(x => x.Id).FirstOrDefault();
            var tail = collection.Query().OrderByDescending(x => x.Id).FirstOrDefault();

            // Initialize lastGeneratedId from the tail (most recent) entry
            if (tail != null)
            {
                lastGeneratedId = tail.Id;
            }
        }

        /// <inheritdoc />
        public override bool Add(IEntry<TId, TValue> entry)
        {
            if (!HasCapacity) return false;

            var liteEntry = new Entry<TId, TValue>(entry.Id, entry.Value)
            {
                CreatedOnUTC = entry.CreatedOnUTC
            };

            try
            {
                collection.Insert(liteEntry);
                LastAddedId = entry.Id;
            }
            catch (LiteException ex) when (ex.ErrorCode == LiteException.INDEX_DUPLICATE_KEY)
            {
                // Duplicate key - return false
                return false;
            }
            return true;
        }

        /// <inheritdoc />
        public override bool Add(TId id, TValue value, out IEntry<TId, TValue> entry)
        {
            entry = new Entry<TId, TValue>(id, value);
            return Add(entry);
        }

        /// <inheritdoc />
        public override bool Add(TValue value, out IEntry<TId, TValue> entry)
        {
            var nextId = nextIdFactory(lastGeneratedId);
            if (nextId == null)
            {
                entry = default;
                return false;
            }
            lastGeneratedId = nextId;
            return Add(nextId.Value, value, out entry);
        }

        /// <inheritdoc />
        public override bool GetCount(out long count)
        {
            count = collection.Count();
            return true;
        }

        /// <inheritdoc />
        public override bool GetEntryOrDefault(TId? id, out IEntry<TId, TValue> entry)
        {
            entry = null;
            if (!id.HasValue) return false;

            entry = collection.FindById(new BsonValue(id.Value));

            return true;
        }

        /// <inheritdoc />
        public override bool GetValueOrDefault(TId? id, out TValue value)
        {
            value = default;
            if (GetEntryOrDefault(id, out var entry))
            {
                if (entry != null)
                {
                    value = entry.Value;
                }
                else
                {
                    value = default;
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        /// <inheritdoc />
        public override bool Remove(TId id, out IEntry<TId, TValue> entry)
        {
            var found = collection.FindById(new BsonValue(id));
            if (found == null)
            {
                entry = null;
                return false;
            }

            collection.Delete(new BsonValue(id));
            entry = found;
            return true;
        }

        /// <inheritdoc />
        public override bool Update(IEntry<TId, TValue> entry)
        {
            var existing = collection.FindById(new BsonValue(entry.Id));
            if (existing == null) return false;

            existing.Value = entry.Value;
            return collection.Update(existing);
        }

        /// <inheritdoc />
        public override bool Update(TId id, TValue value)
        {
            var existing = collection.FindById(new BsonValue(id));
            if (existing == null) return false;

            existing.Value = value;
            return collection.Update(existing);
        }

        /// <inheritdoc />
        protected override void DisposeInternal()
        {
            if (ownsDatabase)
            {
                database?.Dispose();
            }
        }
    }
}