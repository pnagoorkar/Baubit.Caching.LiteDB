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
    public abstract class Store<TId, TValue> : Baubit.Caching.Store<TId, TValue>
        where TId : struct, IComparable<TId>, IEquatable<TId>
    {
        private readonly LiteDatabase _database;
        private readonly ILiteCollection<Entry<TId, TValue>> _collection;
        private readonly ILogger<Store<TId, TValue>> _logger;
        private readonly bool _ownsDatabase;
        private TId? lastGeneratedId;

        /// <summary>
        /// Creates a new LiteDB-backed store with the specified database path.
        /// </summary>
        /// <param name="databasePath">Path to the LiteDB database file.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="minCap">Minimum capacity (null for uncapped).</param>
        /// <param name="maxCap">Maximum capacity (null for uncapped).</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public Store(string databasePath,
                     string collectionName,
                     long? minCap,
                     long? maxCap,
                     ILoggerFactory loggerFactory)
            : base(minCap, maxCap, loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Store<TId, TValue>>();

            _database = new LiteDatabase(new ConnectionString
            {
                Filename = databasePath,
                Upgrade = true
            });
            _ownsDatabase = true;
            _collection = _database.GetCollection<Entry<TId, TValue>>(collectionName);
            _collection.EnsureIndex(x => x.Id, unique: true);
            InitializeHeadTail();
        }

        /// <summary>
        /// Creates a new uncapped LiteDB-backed store with the specified database path.
        /// </summary>
        /// <param name="databasePath">Path to the LiteDB database file.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public Store(string databasePath,
                     string collectionName,
                     ILoggerFactory loggerFactory)
            : this(databasePath, collectionName, null, null, loggerFactory)
        {
        }

        /// <summary>
        /// Creates a new LiteDB-backed store using an existing database instance.
        /// </summary>
        /// <param name="database">Existing LiteDB database instance.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="minCap">Minimum capacity (null for uncapped).</param>
        /// <param name="maxCap">Maximum capacity (null for uncapped).</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public Store(LiteDatabase database,
                     string collectionName,
                     long? minCap,
                     long? maxCap,
                     ILoggerFactory loggerFactory)
            : base(minCap, maxCap, loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Store<TId, TValue>>();
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _ownsDatabase = false;
            _collection = _database.GetCollection<Entry<TId, TValue>>(collectionName);
            _collection.EnsureIndex(x => x.Id, unique: true);
            InitializeHeadTail();
        }

        /// <summary>
        /// Creates a new uncapped LiteDB-backed store using an existing database instance.
        /// </summary>
        /// <param name="database">Existing LiteDB database instance.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public Store(LiteDatabase database,
                     string collectionName,
                     ILoggerFactory loggerFactory)
            : this(database, collectionName, null, null, loggerFactory)
        {
        }

        private void InitializeHeadTail()
        {
            var head = _collection.Query().OrderBy(x => x.Id).FirstOrDefault();
            var tail = _collection.Query().OrderByDescending(x => x.Id).FirstOrDefault();
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
                _collection.Insert(liteEntry);
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
            var nextId = GenerateNextId(lastGeneratedId);
            if (nextId == null)
            {
                entry = default;
                return false;
            }
            lastGeneratedId = nextId;
            return Add(nextId.Value, value, out entry);
        }
        protected abstract TId? GenerateNextId(TId? lastGeneratedId);

        /// <inheritdoc />
        public override bool GetCount(out long count)
        {
            count = _collection.Count();
            return true;
        }

        /// <inheritdoc />
        public override bool GetEntryOrDefault(TId? id, out IEntry<TId, TValue> entry)
        {
            entry = null;
            if (!id.HasValue) return false;

            entry = _collection.FindById(new BsonValue(id.Value));

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
            var found = _collection.FindById(new BsonValue(id));
            if (found == null)
            {
                entry = null;
                return false;
            }

            _collection.Delete(new BsonValue(id));
            entry = found;
            return true;
        }

        /// <inheritdoc />
        public override bool Update(IEntry<TId, TValue> entry)
        {
            var existing = _collection.FindById(new BsonValue(entry.Id));
            if (existing == null) return false;

            existing.Value = entry.Value;
            return _collection.Update(existing);
        }

        /// <inheritdoc />
        public override bool Update(TId id, TValue value)
        {
            var existing = _collection.FindById(new BsonValue(id));
            if (existing == null) return false;

            existing.Value = value;
            return _collection.Update(existing);
        }

        /// <inheritdoc />
        protected override void DisposeInternal()
        {
            if (_ownsDatabase)
            {
                _database?.Dispose();
            }
        }
    }

    /// <summary>
    /// LiteDB-backed store implementation for Baubit.Caching using Guid as the ID type.
    /// Provides persistent, file-based storage as an L2 backing store with automatic ID generation.
    /// </summary>
    /// <typeparam name="TValue">The type of value stored in the cache.</typeparam>
    public class Store<TValue> : Store<Guid, TValue>
    {
        private readonly Baubit.Identity.IIdentityGenerator identityGenerator;

        /// <summary>
        /// Creates a new LiteDB-backed store with the specified database path.
        /// </summary>
        /// <param name="databasePath">Path to the LiteDB database file.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="minCap">Minimum capacity (null for uncapped).</param>
        /// <param name="maxCap">Maximum capacity (null for uncapped).</param>
        /// <param name="identityGenerator">Identity generator for creating entry IDs.</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public Store(string databasePath,
                     string collectionName,
                     long? minCap,
                     long? maxCap,
                     Baubit.Identity.IIdentityGenerator identityGenerator,
                     ILoggerFactory loggerFactory)
            : base(databasePath, collectionName, minCap, maxCap, loggerFactory)
        {
            this.identityGenerator = identityGenerator ?? throw new ArgumentNullException(nameof(identityGenerator));
        }

        /// <summary>
        /// Creates a new uncapped LiteDB-backed store with the specified database path.
        /// </summary>
        /// <param name="databasePath">Path to the LiteDB database file.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="identityGenerator">Identity generator for creating entry IDs.</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public Store(string databasePath,
                     string collectionName,
                     Baubit.Identity.IIdentityGenerator identityGenerator,
                     ILoggerFactory loggerFactory)
            : base(databasePath, collectionName, loggerFactory)
        {
            this.identityGenerator = identityGenerator ?? throw new ArgumentNullException(nameof(identityGenerator));
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
        public Store(LiteDatabase database,
                     string collectionName,
                     long? minCap,
                     long? maxCap,
                     Baubit.Identity.IIdentityGenerator identityGenerator,
                     ILoggerFactory loggerFactory)
            : base(database, collectionName, minCap, maxCap, loggerFactory)
        {
            this.identityGenerator = identityGenerator ?? throw new ArgumentNullException(nameof(identityGenerator));
        }

        /// <summary>
        /// Creates a new uncapped LiteDB-backed store using an existing database instance.
        /// </summary>
        /// <param name="database">Existing LiteDB database instance.</param>
        /// <param name="collectionName">Name of the collection to use for storage.</param>
        /// <param name="identityGenerator">Identity generator for creating entry IDs.</param>
        /// <param name="loggerFactory">Logger factory for creating loggers.</param>
        public Store(LiteDatabase database,
                     string collectionName,
                     Baubit.Identity.IIdentityGenerator identityGenerator,
                     ILoggerFactory loggerFactory)
            : base(database, collectionName, loggerFactory)
        {
            this.identityGenerator = identityGenerator ?? throw new ArgumentNullException(nameof(identityGenerator));
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
        public Store(string databasePath,
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
        public Store(string databasePath,
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
        public Store(LiteDatabase database,
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
        public Store(LiteDatabase database,
                     string collectionName,
                     ILoggerFactory loggerFactory)
            : this(database, collectionName, Baubit.Identity.IdentityGenerator.CreateNew(), loggerFactory)
        {
        }

        /// <inheritdoc/>
        protected override Guid? GenerateNextId(Guid? lastGeneratedId)
        {
            if (identityGenerator == null) return null;
            // Initialize from last generated ID if available to ensure monotonicity
            if (lastGeneratedId.HasValue)
            {
                identityGenerator.InitializeFrom(lastGeneratedId.Value);
            }

            return identityGenerator.GetNext();
        }
    }

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

    /// <summary>
    /// LiteDB-backed store implementation for Baubit.Caching using int as the ID type.
    /// Provides persistent, file-based storage as an L2 backing store with sequential ID generation.
    /// </summary>
    /// <typeparam name="TValue">The type of value stored in the cache.</typeparam>
    public class StoreInt<TValue> : Store<int, TValue>
    {
        private int nextId = 1;

        /// <summary>
        /// Creates a new LiteDB-backed store with the specified database path.
        /// </summary>
        public StoreInt(string databasePath, string collectionName, long? minCap, long? maxCap, ILoggerFactory loggerFactory)
            : base(databasePath, collectionName, minCap, maxCap, loggerFactory) { }

        /// <summary>
        /// Creates a new uncapped LiteDB-backed store with the specified database path.
        /// </summary>
        public StoreInt(string databasePath, string collectionName, ILoggerFactory loggerFactory)
            : base(databasePath, collectionName, loggerFactory) { }

        /// <summary>
        /// Creates a new LiteDB-backed store using an existing database instance.
        /// </summary>
        public StoreInt(LiteDatabase database, string collectionName, long? minCap, long? maxCap, ILoggerFactory loggerFactory)
            : base(database, collectionName, minCap, maxCap, loggerFactory) { }

        /// <summary>
        /// Creates a new uncapped LiteDB-backed store using an existing database instance.
        /// </summary>
        public StoreInt(LiteDatabase database, string collectionName, ILoggerFactory loggerFactory)
            : base(database, collectionName, loggerFactory) { }

        /// <inheritdoc/>
        protected override int? GenerateNextId(int? lastGeneratedId)
        {
            if (lastGeneratedId.HasValue)
            {
                nextId = lastGeneratedId.Value + 1;
            }
            return nextId++;
        }
    }
}
