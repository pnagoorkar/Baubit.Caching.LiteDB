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
    /// <typeparam name="TValue">The type of value stored in the cache.</typeparam>
    public class Store<TValue> : AStore<TValue>
    {
        private readonly LiteDatabase _database;
        private readonly ILiteCollection<Entry<TValue>> _collection;
        private readonly ILogger<Store<TValue>> _logger;
        private readonly bool _ownsDatabase;
        private Guid? _headId;
        private Guid? _tailId;

        /// <summary>
        /// Gets the identifier of the first (head/oldest) entry in the store.
        /// </summary>
        public override Guid? HeadId => _headId;

        /// <summary>
        /// Gets the identifier of the last (tail/newest) entry in the store.
        /// </summary>
        public override Guid? TailId => _tailId;

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
            _logger = loggerFactory.CreateLogger<Store<TValue>>();

            _database = new LiteDatabase(new ConnectionString
            {
                Filename = databasePath,
                Upgrade = true
            });
            _ownsDatabase = true;
            _collection = _database.GetCollection<Entry<TValue>>(collectionName);
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
            _logger = loggerFactory.CreateLogger<Store<TValue>>();
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _ownsDatabase = false;
            _collection = _database.GetCollection<Entry<TValue>>(collectionName);
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
        public override bool Add(IEntry<TValue> entry)
        {
            if (!HasCapacity) return false;
            if (_collection.Exists(x => x.Id == entry.Id)) return false;

            var liteEntry = new Entry<TValue>(entry.Id, entry.Value)
            {
                CreatedOnUTC = entry.CreatedOnUTC
            };

            _collection.Insert(liteEntry);
            UpdateHeadTailOnAdd(entry.Id);
            return true;
        }

        /// <inheritdoc />
        public override bool Add(Guid id, TValue value, out IEntry<TValue> entry)
        {
            entry = new Entry<TValue>(id, value);
            return Add(entry);
        }

        private void UpdateHeadTailOnAdd(Guid id)
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

        private void UpdateHeadTailOnRemove(Guid id)
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
                _headId = head?.Id;
            }
            if (_tailId.HasValue && id.CompareTo(_tailId.Value) == 0)
            {
                var tail = _collection.Query().OrderByDescending(x => x.Id).FirstOrDefault();
                _tailId = tail?.Id;
            }
        }

        /// <inheritdoc />
        public override bool GetCount(out long count)
        {
            count = _collection.Count();
            return true;
        }

        /// <inheritdoc />
        public override bool GetEntryOrDefault(Guid? id, out IEntry<TValue> entry)
        {
            entry = null;
            if (!id.HasValue) return false;

            var found = _collection.FindOne(x => x.Id == id.Value);
            if (found == null) return false;

            entry = found;
            return true;
        }

        /// <inheritdoc />
        public override bool GetValueOrDefault(Guid? id, out TValue value)
        {
            value = default;
            if (!GetEntryOrDefault(id, out var entry)) return false;
            value = entry.Value;
            return true;
        }

        /// <inheritdoc />
        public override bool Remove(Guid id, out IEntry<TValue> entry)
        {
            var found = _collection.FindOne(x => x.Id == id);
            if (found == null)
            {
                entry = null;
                return false;
            }

            _collection.Delete(id);
            UpdateHeadTailOnRemove(id);
            entry = found;
            return true;
        }

        /// <inheritdoc />
        public override bool Update(IEntry<TValue> entry)
        {
            var existing = _collection.FindOne(x => x.Id == entry.Id);
            if (existing == null) return false;

            existing.Value = entry.Value;
            return _collection.Update(existing);
        }

        /// <inheritdoc />
        public override bool Update(Guid id, TValue value)
        {
            var existing = _collection.FindOne(x => x.Id == id);
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
}
