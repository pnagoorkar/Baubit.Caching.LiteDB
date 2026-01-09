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
        private readonly Func<DateTime> _utcNow;
        private int _movesSinceLastPersist;

        /// <summary>
        /// Gets whether persistence is enabled (PersistPositionEveryXMoves > 0).
        /// </summary>
        private bool IsPersistenceEnabled => _configuration.PersistPositionEveryXMoves > 0;

        /// <summary>
        /// Gets whether it's time to persist based on move count.
        /// Includes guard for when persistence is disabled to avoid division by zero.
        /// </summary>
        private bool ShouldPersist => IsPersistenceEnabled && _movesSinceLastPersist >= _configuration.PersistPositionEveryXMoves;

        /// <summary>
        /// Gets whether to persist before the move operation.
        /// </summary>
        private bool ShouldPersistBefore => IsPersistenceEnabled && _configuration.PersistPositionBeforeMove && ShouldPersist;

        /// <summary>
        /// Gets whether to persist after the move operation.
        /// </summary>
        private bool ShouldPersistAfter => IsPersistenceEnabled && !_configuration.PersistPositionBeforeMove && ShouldPersist;

        /// <summary>
        /// Creates a new base cache async enumerator with position persistence.
        /// </summary>
        /// <param name="cache">The ordered cache to enumerate.</param>
        /// <param name="onDispose">Action to invoke on dispose.</param>
        /// <param name="id">Unique identifier for this enumeration session.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="positionCollection">LiteDB collection for persisting positions.</param>
        /// <param name="configuration">Configuration with persistence settings.</param>
        /// <param name="utcNow">Function to get current UTC time. Defaults to DateTime.UtcNow if not provided.</param>
        protected BaseCacheAsyncEnumerator(
            IOrderedCache<TId, TValue> cache,
            Action<ICacheEnumerator<TId>> onDispose,
            string id,
            CancellationToken cancellationToken,
            ILiteCollection<EnumeratorPosition<TId>> positionCollection,
            Configuration configuration,
            Func<DateTime> utcNow = null)
            : base(cache, onDispose, id, cancellationToken)
        {
            _positionCollection = positionCollection ?? throw new ArgumentNullException(nameof(positionCollection));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _id = id;
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            _movesSinceLastPersist = 0;
        }

        /// <summary>
        /// Moves to the next element in the enumeration and persists the current position to LiteDB based on configuration.
        /// </summary>
        /// <returns>True if the enumerator was successfully advanced to the next element; false if the end has been reached.</returns>
        public override async ValueTask<bool> MoveNextAsync()
        {
            if (ShouldPersistBefore) PersistPosition();

            var result = await base.MoveNextAsync().ConfigureAwait(false);

            if (result && IsPersistenceEnabled) _movesSinceLastPersist++;

            if (result && ShouldPersistAfter) PersistPosition();

            return result;
        }

        /// <summary>
        /// Persists the current position to LiteDB.
        /// </summary>
        private void PersistPosition()
        {
            if (Current != null)
            {
                var position = new EnumeratorPosition<TId>(_id, Current.Id, _utcNow());

                // Upsert: Insert if doesn't exist, update if exists
                _positionCollection.Upsert(position);
                _movesSinceLastPersist = 0;
            }
        }

        /// <summary>
        /// Disposes the enumerator and persists position one last time before cleanup.
        /// </summary>
        public override async ValueTask DisposeAsync()
        {
            // Persist one last time before disposal if persistence is enabled
            if (IsPersistenceEnabled && Current != null)
            {
                PersistPosition();
            }

            // Call base dispose
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}
