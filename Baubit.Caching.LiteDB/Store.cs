using Baubit.Caching;
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
        private readonly LiteDatabase _database;
        private readonly ILiteCollection<Entry<TId, TValue>> _collection;
        private readonly ILogger<Store<TId, TValue>> _logger;
        private readonly bool _ownsDatabase;
        private TId? _headId;
        private TId? _tailId;

        /// <summary>
        /// Gets the identifier of the first (head/oldest) entry in the store.
        /// </summary>
        public TId? HeadId => _headId;

        /// <summary>
        /// Gets the identifier of the last (tail/newest) entry in the store.
        /// </summary>
        public TId? TailId => _tailId;

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
            _headId = head?.Id;
            _tailId = tail?.Id;
        }

        /// <inheritdoc />
        public override bool Add(IEntry<TId, TValue> entry)
        {
            if (!HasCapacity) return false;
            if (_collection.Exists(x => x.Id.Equals(entry.Id))) return false;

            var liteEntry = new Entry<TId, TValue>(entry.Id, entry.Value)
            {
                CreatedOnUTC = entry.CreatedOnUTC
            };

            _collection.Insert(liteEntry);
            UpdateHeadTailOnAdd(entry.Id);
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
            throw new NotSupportedException(
                $"Automatic ID generation is not supported for Store<{typeof(TId).Name}, {typeof(TValue).Name}>. " +
                "Use Add(TId id, TValue value, out IEntry<TId, TValue> entry) instead to provide an explicit ID.");
        }

        private void UpdateHeadTailOnAdd(TId id)
        {
            if (!_headId.HasValue || id.CompareTo(_headId.Value) < 0)
            {
                _headId = id;
            }
            if (!_tailId.HasValue || id.CompareTo(_tailId.Value) > 0)
            {
                _tailId = id;
            }
        }

        private void UpdateHeadTailOnRemove(TId id)
        {
            var count = _collection.Count();
            if (count == 0)
            {
                _headId = null;
                _tailId = null;
                return;
            }

            if (_headId.HasValue && id.CompareTo(_headId.Value) == 0)
            {
                var head = _collection.Query().OrderBy(x => x.Id).FirstOrDefault();
                _headId = head != null ? (TId?)head.Id : null;
            }
            if (_tailId.HasValue && id.CompareTo(_tailId.Value) == 0)
            {
                var tail = _collection.Query().OrderByDescending(x => x.Id).FirstOrDefault();
                _tailId = tail != null ? (TId?)tail.Id : null;
            }
        }

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

            var found = _collection.FindOne(x => x.Id.Equals(id.Value));
            if (found == null) return false;

            entry = found;
            return true;
        }

        /// <inheritdoc />
        public override bool GetValueOrDefault(TId? id, out TValue value)
        {
            value = default;
            if (!GetEntryOrDefault(id, out var entry)) return false;
            value = entry.Value;
            return true;
        }

        /// <inheritdoc />
        public override bool Remove(TId id, out IEntry<TId, TValue> entry)
        {
            var found = _collection.FindOne(x => x.Id.Equals(id));
            if (found == null)
            {
                entry = null;
                return false;
            }

            _collection.Delete(new BsonValue(id));
            UpdateHeadTailOnRemove(id);
            entry = found;
            return true;
        }

        /// <inheritdoc />
        public override bool Update(IEntry<TId, TValue> entry)
        {
            var existing = _collection.FindOne(x => x.Id.Equals(entry.Id));
            if (existing == null) return false;

            existing.Value = entry.Value;
            return _collection.Update(existing);
        }

        /// <inheritdoc />
        public override bool Update(TId id, TValue value)
        {
            var existing = _collection.FindOne(x => x.Id.Equals(id));
            if (existing == null) return false;

            existing.Value = value;
            return _collection.Update(existing);
        }

        /// <inheritdoc />
        protected override void DisposeInternal()
        {
            _headId = null;
            _tailId = null;
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
        private readonly Baubit.Identity.IIdentityGenerator _identityGenerator;

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
            _identityGenerator = identityGenerator ?? throw new ArgumentNullException(nameof(identityGenerator));
            InitializeIdentityGenerator();
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
            _identityGenerator = identityGenerator ?? throw new ArgumentNullException(nameof(identityGenerator));
            InitializeIdentityGenerator();
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
            _identityGenerator = identityGenerator ?? throw new ArgumentNullException(nameof(identityGenerator));
            InitializeIdentityGenerator();
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
            _identityGenerator = identityGenerator ?? throw new ArgumentNullException(nameof(identityGenerator));
            InitializeIdentityGenerator();
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

        private void InitializeIdentityGenerator()
        {
            // Initialize the identity generator from the last (tail) ID if it exists
            if (TailId.HasValue)
            {
                _identityGenerator.InitializeFrom(TailId.Value);
            }
        }

        /// <inheritdoc />
        public override bool Add(TValue value, out IEntry<Guid, TValue> entry)
        {
            var id = _identityGenerator.GetNext();
            return Add(id, value, out entry);
        }
    }
}
