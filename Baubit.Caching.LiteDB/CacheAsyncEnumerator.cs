using System;
using System.Threading;
using LiteDB;

namespace Baubit.Caching.LiteDB
{
    /// <summary>
    /// Async enumerator for ordered cache with LiteDB persistence.
    /// Enumerates from a starting position or from the beginning of the cache.
    /// </summary>
    /// <typeparam name="TId">The type of the unique identifier.</typeparam>
    /// <typeparam name="TValue">The type of value stored in the cache.</typeparam>
    public class CacheAsyncEnumerator<TId, TValue> : BaseCacheAsyncEnumerator<TId, TValue>
        where TId : struct, IComparable<TId>, IEquatable<TId>
    {
        /// <summary>
        /// Creates a new cache async enumerator.
        /// If startPosition is provided, starts from that position; otherwise starts from the beginning.
        /// </summary>
        /// <param name="cache">The ordered cache to enumerate.</param>
        /// <param name="onDispose">Action to invoke on dispose.</param>
        /// <param name="id">Unique identifier for this enumeration session.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="positionCollection">LiteDB collection for persisting positions.</param>
        /// <param name="configuration">Configuration with persistence settings.</param>
        /// <param name="startPosition">Optional position to start enumeration from. If null, starts from the beginning.</param>
        /// <param name="utcNow">Function to get current UTC time. Defaults to DateTime.UtcNow if not provided.</param>
        public CacheAsyncEnumerator(
            IOrderedCache<TId, TValue> cache,
            Action<ICacheEnumerator<TId>> onDispose,
            string id,
            CancellationToken cancellationToken,
            ILiteCollection<EnumeratorPosition<TId>> positionCollection,
            Configuration configuration,
            TId? startPosition = null,
            Func<DateTime> utcNow = null)
            : base(cache, onDispose, id, cancellationToken, positionCollection, configuration, utcNow)
        {
            // If start position is provided, set Current to that entry
            if (startPosition.HasValue)
            {
                if (cache.GetEntryOrDefault(startPosition.Value, out var entry) && entry != null)
                {
                    Current = entry;
                }
            }
        }
    }
}
