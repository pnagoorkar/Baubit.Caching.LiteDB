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
        private readonly Configuration _configuration;
        private readonly string _id;
        private int _movesSinceLastPersist;

        /// <summary>
        /// Creates a new base cache async enumerator with position persistence.
        /// </summary>
        /// <param name="cache">The ordered cache to enumerate.</param>
        /// <param name="onDispose">Action to invoke on dispose.</param>
        /// <param name="id">Unique identifier for this enumeration session.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="positionCollection">LiteDB collection for persisting positions.</param>
        /// <param name="configuration">Configuration with persistence settings.</param>
        protected BaseCacheAsyncEnumerator(
            IOrderedCache<TId, TValue> cache,
            Action<ICacheEnumerator<TId>> onDispose,
            string id,
            CancellationToken cancellationToken,
            ILiteCollection<EnumeratorPosition<TId>> positionCollection,
            Configuration configuration)
            : base(cache, onDispose, id, cancellationToken)
        {
            _positionCollection = positionCollection ?? throw new ArgumentNullException(nameof(positionCollection));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _id = id;
            _movesSinceLastPersist = 0;
        }

        /// <summary>
        /// Moves to the next element in the enumeration and persists the current position to LiteDB based on configuration.
        /// </summary>
        /// <returns>True if the enumerator was successfully advanced to the next element; false if the end has been reached.</returns>
        public override async ValueTask<bool> MoveNextAsync()
        {
            // Increment move counter
            _movesSinceLastPersist++;

            // Persist BEFORE moving if configured to do so
            if (!_configuration.PersistPositionAfterMove && _movesSinceLastPersist >= _configuration.PersistPositionEveryXMoves)
            {
                PersistPosition();
                _movesSinceLastPersist = 0;
            }

            // Call base implementation to move to next element
            var result = await base.MoveNextAsync().ConfigureAwait(false);

            // Persist AFTER moving if configured to do so (default behavior)
            if (_configuration.PersistPositionAfterMove && result && _movesSinceLastPersist >= _configuration.PersistPositionEveryXMoves)
            {
                PersistPosition();
                _movesSinceLastPersist = 0;
            }

            return result;
        }

        /// <summary>
        /// Persists the current position to LiteDB.
        /// </summary>
        private void PersistPosition()
        {
            if (Current != null)
            {
                var position = new EnumeratorPosition<TId>(_id, Current.Id)
                {
                    LastUpdatedUTC = DateTime.UtcNow
                };

                // Upsert: Insert if doesn't exist, update if exists
                _positionCollection.Upsert(position);
            }
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
