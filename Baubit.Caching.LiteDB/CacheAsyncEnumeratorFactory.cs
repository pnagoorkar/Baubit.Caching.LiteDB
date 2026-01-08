using System;
using System.Collections.Generic;
using System.Threading;
using LiteDB;

namespace Baubit.Caching.LiteDB
{
    /// <summary>
    /// Factory for creating cache async enumerators with LiteDB persistence support.
    /// Implements session resume functionality by checking for saved positions in LiteDB.
    /// </summary>
    /// <typeparam name="TId">The type of the unique identifier.</typeparam>
    /// <typeparam name="TValue">The type of value stored in the cache.</typeparam>
    public class CacheAsyncEnumeratorFactory<TId, TValue> : ICacheAsyncEnumeratorFactory<TId, TValue>
        where TId : struct, IComparable<TId>, IEquatable<TId>
    {
        private readonly Configuration _configuration;
        private readonly ILiteCollection<EnumeratorPosition<TId>> _positionCollection;
        private const string PositionCollectionName = "_enumerator_positions";

        /// <summary>
        /// Creates a new cache async enumerator factory with the specified LiteDB database.
        /// </summary>
        /// <param name="database">The LiteDB database for storing enumerator positions.</param>
        /// <param name="configuration">Configuration with ResumeSession setting.</param>
        public CacheAsyncEnumeratorFactory(LiteDatabase database, Configuration configuration)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));
            
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _positionCollection = database.GetCollection<EnumeratorPosition<TId>>(PositionCollectionName);
            _positionCollection.EnsureIndex(x => x.Id, unique: true);
        }

        /// <summary>
        /// Creates a new cache async enumerator factory with the specified database path.
        /// </summary>
        /// <param name="databasePath">Path to the LiteDB database file.</param>
        /// <param name="configuration">Configuration with ResumeSession setting.</param>
        /// <param name="database">Output parameter for the created database (caller owns disposal).</param>
        public CacheAsyncEnumeratorFactory(string databasePath, Configuration configuration, out LiteDatabase database)
        {
            if (string.IsNullOrEmpty(databasePath))
                throw new ArgumentNullException(nameof(databasePath));

            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            
            database = new LiteDatabase(new ConnectionString
            {
                Filename = databasePath,
                Upgrade = true
            });
            
            _positionCollection = database.GetCollection<EnumeratorPosition<TId>>(PositionCollectionName);
            _positionCollection.EnsureIndex(x => x.Id, unique: true);
        }

        /// <summary>
        /// Creates an async enumerator for the cache.
        /// If ResumeSession is enabled and a saved position exists for the given id,
        /// the enumerator resumes from that position. Otherwise, starts from the beginning.
        /// </summary>
        /// <param name="cache">The ordered cache to enumerate.</param>
        /// <param name="onDispose">Action to invoke when the enumerator is disposed.</param>
        /// <param name="id">Unique identifier for this enumeration session.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An async enumerator for the cache entries.</returns>
        public IAsyncEnumerator<IEntry<TId, TValue>> CreateEnumerator(
            IOrderedCache<TId, TValue> cache,
            Action<ICacheEnumerator<TId>> onDispose,
            string id,
            CancellationToken cancellationToken)
        {
            TId? startPosition = null;

            // Check for saved position if ResumeSession is enabled
            if (_configuration.ResumeSession && !string.IsNullOrEmpty(id))
            {
                var savedPosition = _positionCollection.FindById(id);
                if (savedPosition != null)
                {
                    startPosition = savedPosition.CurrentId;
                }
            }

            // Create enumerator with or without start position
            if (startPosition.HasValue)
            {
                return new CacheAsyncEnumerator<TId, TValue>(
                    cache, onDispose, id, startPosition, cancellationToken, _positionCollection);
            }
            else
            {
                return new CacheAsyncEnumerator<TId, TValue>(
                    cache, onDispose, id, cancellationToken, _positionCollection);
            }
        }

        /// <summary>
        /// Creates an async enumerator for future entries in the cache.
        /// If ResumeSession is enabled and a saved position exists for the given id,
        /// the enumerator resumes from that position. Otherwise, starts from the end.
        /// </summary>
        /// <param name="cache">The ordered cache to enumerate.</param>
        /// <param name="onDispose">Action to invoke when the enumerator is disposed.</param>
        /// <param name="id">Unique identifier for this enumeration session.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An async enumerator for future cache entries.</returns>
        public IAsyncEnumerator<IEntry<TId, TValue>> CreateFutureEnumerator(
            IOrderedCache<TId, TValue> cache,
            Action<ICacheEnumerator<TId>> onDispose,
            string id,
            CancellationToken cancellationToken)
        {
            TId? startPosition = null;

            // Check for saved position if ResumeSession is enabled
            if (_configuration.ResumeSession && !string.IsNullOrEmpty(id))
            {
                var savedPosition = _positionCollection.FindById(id);
                if (savedPosition != null)
                {
                    startPosition = savedPosition.CurrentId;
                }
            }

            // Create future enumerator with or without start position
            if (startPosition.HasValue)
            {
                return new CacheFutureAsyncEnumerator<TId, TValue>(
                    cache, onDispose, id, startPosition, cancellationToken, _positionCollection);
            }
            else
            {
                return new CacheFutureAsyncEnumerator<TId, TValue>(
                    cache, onDispose, id, cancellationToken, _positionCollection);
            }
        }
    }
}
