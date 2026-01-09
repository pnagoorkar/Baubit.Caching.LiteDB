using System;
using System.Threading;
using LiteDB;

namespace Baubit.Caching.LiteDB
{
    /// <summary>
    /// Async enumerator for future entries in ordered cache with LiteDB persistence.
    /// Waits for new entries to be added to the cache and enumerates them as they arrive.
    /// </summary>
    /// <typeparam name="TId">The type of the unique identifier.</typeparam>
    /// <typeparam name="TValue">The type of value stored in the cache.</typeparam>
    public class CacheFutureAsyncEnumerator<TId, TValue> : BaseCacheAsyncEnumerator<TId, TValue>
        where TId : struct, IComparable<TId>, IEquatable<TId>
    {
        /// <summary>
        /// Creates a new future cache async enumerator starting from the end of the cache.
        /// </summary>
        /// <param name="cache">The ordered cache to enumerate.</param>
        /// <param name="onDispose">Action to invoke on dispose.</param>
        /// <param name="id">Unique identifier for this enumeration session.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="positionCollection">LiteDB collection for persisting positions.</param>
        /// <param name="configuration">Configuration with persistence settings.</param>
        public CacheFutureAsyncEnumerator(
            IOrderedCache<TId, TValue> cache,
            Action<ICacheEnumerator<TId>> onDispose,
            string id,
            CancellationToken cancellationToken,
            ILiteCollection<EnumeratorPosition<TId>> positionCollection,
            Configuration configuration)
            : base(cache, onDispose, id, cancellationToken, positionCollection, configuration)
        {
            // Initialize Current to last entry like Baubit.Caching.CacheFutureAsyncEnumerator does
            if (cache.GetLastOrDefault(out var lastEntry) && lastEntry != null)
            {
                Current = lastEntry;
            }
        }

        /// <summary>
        /// Creates a new future cache async enumerator starting from a specific position.
        /// </summary>
        /// <param name="cache">The ordered cache to enumerate.</param>
        /// <param name="onDispose">Action to invoke on dispose.</param>
        /// <param name="id">Unique identifier for this enumeration session.</param>
        /// <param name="startPosition">The position to start enumeration from.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="positionCollection">LiteDB collection for persisting positions.</param>
        /// <param name="configuration">Configuration with persistence settings.</param>
        internal CacheFutureAsyncEnumerator(
            IOrderedCache<TId, TValue> cache,
            Action<ICacheEnumerator<TId>> onDispose,
            string id,
            TId? startPosition,
            CancellationToken cancellationToken,
            ILiteCollection<EnumeratorPosition<TId>> positionCollection,
            Configuration configuration)
            : base(cache, onDispose, id, cancellationToken, positionCollection, configuration)
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
