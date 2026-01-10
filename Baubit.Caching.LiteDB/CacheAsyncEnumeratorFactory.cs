using System;
using System.Collections.Generic;
using System.Threading;
using LiteDB;

namespace Baubit.Caching.LiteDB
{
    /// <summary>
    /// Factory for creating cache async enumerators with LiteDB persistence support.
    /// Implements session resume functionality by checking for saved positions in LiteDB.
    /// Uses the same database as the store for position persistence.
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
        /// Enumerator positions will be persisted to the same database in a separate collection.
        /// </summary>
        /// <param name="database">The LiteDB database for storing enumerator positions (same as store database).</param>
        /// <param name="configuration">Configuration with ResumeSession and persistence settings.</param>
        public CacheAsyncEnumeratorFactory(LiteDatabase database, Configuration configuration)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database));
            
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _positionCollection = database.GetCollection<EnumeratorPosition<TId>>(PositionCollectionName);
            // SessionId is marked with BsonId attribute, so no need for explicit index
        }

        /// <summary>
        /// Gets the saved start position for the given session id, if ResumeSession is enabled.
        /// </summary>
        /// <param name="id">Unique identifier for this enumeration session.</param>
        /// <returns>Saved position or null if not found or ResumeSession is disabled.</returns>
        private TId? GetSavedStartPosition(string id)
        {
            if (_configuration.ResumeSession && !string.IsNullOrEmpty(id))
            {
                var savedPosition = _positionCollection.FindById(id);
                if (savedPosition != null)
                {
                    return savedPosition.CurrentId;
                }
            }
            return null;
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
            var startPosition = GetSavedStartPosition(id);
            return new CacheAsyncEnumerator<TId, TValue>(
                cache, onDispose, id, cancellationToken, _positionCollection, _configuration, startPosition);
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
            var startPosition = GetSavedStartPosition(id);
            return new CacheFutureAsyncEnumerator<TId, TValue>(
                cache, onDispose, id, cancellationToken, _positionCollection, _configuration, startPosition);
        }
    }
}
