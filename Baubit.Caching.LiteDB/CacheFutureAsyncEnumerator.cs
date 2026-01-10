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
        /// Creates a new future cache async enumerator.
        /// If startPosition is provided, starts from that position; otherwise starts from the end of the cache.
        /// </summary>
        /// <param name="cache">The ordered cache to enumerate.</param>
        /// <param name="onDispose">Action to invoke on dispose.</param>
        /// <param name="id">Unique identifier for this enumeration session.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="positionCollection">LiteDB collection for persisting positions.</param>
        /// <param name="configuration">Configuration with persistence settings.</param>
        /// <param name="startPosition">Optional position to start enumeration from. If null, starts from the end.</param>
        /// <param name="utcNow">Function to get current UTC time. Defaults to DateTime.UtcNow if not provided.</param>
        public CacheFutureAsyncEnumerator(
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
            
            // If Current is still null, initialize it to the tail
            if (Current == null)
            {
                if (cache.GetLastOrDefault(out var lastEntry) && lastEntry != null)
                {
                    Current = lastEntry;
                }
            }
        }
    }
}
