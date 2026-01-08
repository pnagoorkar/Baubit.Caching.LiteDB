using System;
using System.Threading;
using System.Threading.Tasks;
using LiteDB;

namespace Baubit.Caching.LiteDB
{
    /// <summary>
    /// Base async enumerator that persists enumeration position to LiteDB.
    /// Extends Baubit.Caching.BaseCacheAsyncEnumerator to add persistence capability.
    /// </summary>
    /// <typeparam name="TId">The type of the unique identifier.</typeparam>
    /// <typeparam name="TValue">The type of value stored in the cache.</typeparam>
    public abstract class BaseCacheAsyncEnumerator<TId, TValue> : Baubit.Caching.BaseCacheAsyncEnumerator<TId, TValue>
        where TId : struct, IComparable<TId>, IEquatable<TId>
    {
        private readonly ILiteCollection<EnumeratorPosition<TId>> _positionCollection;
        private readonly string _id;

        /// <summary>
        /// Creates a new base cache async enumerator with position persistence.
        /// </summary>
        /// <param name="cache">The ordered cache to enumerate.</param>
        /// <param name="onDispose">Action to invoke on dispose.</param>
        /// <param name="id">Unique identifier for this enumeration session.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="positionCollection">LiteDB collection for persisting positions.</param>
        protected BaseCacheAsyncEnumerator(
            IOrderedCache<TId, TValue> cache,
            Action<ICacheEnumerator<TId>> onDispose,
            string id,
            CancellationToken cancellationToken,
            ILiteCollection<EnumeratorPosition<TId>> positionCollection)
            : base(cache, onDispose, id, cancellationToken)
        {
            _positionCollection = positionCollection ?? throw new ArgumentNullException(nameof(positionCollection));
            _id = id;
        }

        /// <summary>
        /// Moves to the next element in the enumeration and persists the current position to LiteDB.
        /// </summary>
        /// <returns>True if the enumerator was successfully advanced to the next element; false if the end has been reached.</returns>
        public override async ValueTask<bool> MoveNextAsync()
        {
            // Call base implementation first to move to next element
            var result = await base.MoveNextAsync().ConfigureAwait(false);

            // Persist current position to LiteDB if we have a current entry
            if (result && Current != null)
            {
                var position = new EnumeratorPosition<TId>(_id, Current.Id)
                {
                    LastUpdatedUTC = DateTime.UtcNow
                };

                // Upsert: Insert if doesn't exist, update if exists
                _positionCollection.Upsert(position);
            }

            return result;
        }

        /// <summary>
        /// Disposes the enumerator and optionally cleans up persisted position.
        /// </summary>
        public override async ValueTask DisposeAsync()
        {
            // Call base dispose
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}
