using LiteDB;
using System;
using System.Linq;

namespace Baubit.Caching.LiteDB
{
    /// <summary>
    /// LiteDB-aware cache enumerator collection that tracks both active in-memory enumerators
    /// and persisted enumerator positions for session resumption support.
    /// </summary>
    /// <typeparam name="TId">The type of the unique identifier. Must be a value type that implements IComparable and IEquatable.</typeparam>
    public class CacheEnumeratorCollection<TId> : Baubit.Caching.CacheEnumeratorCollection<TId> where TId : struct, IComparable<TId>, IEquatable<TId>
    {        
        private readonly ILiteCollection<EnumeratorPosition<TId>> positionCollection;
        private readonly Configuration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheEnumeratorCollection{TId}"/> class.
        /// </summary>
        /// <param name="configuration">Configuration with ResumeSession setting.</param>
        /// <param name="database">LiteDB database containing persisted enumerator positions.</param>
        public CacheEnumeratorCollection(Configuration configuration, LiteDatabase database)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            if (database == null)
                throw new ArgumentNullException(nameof(database));
            
            this.positionCollection = database.GetCollection<EnumeratorPosition<TId>>(Configuration.PositionCollectionName);
        }

        /// <summary>
        /// Gets the count of enumerators. When ResumeSession is enabled and no active enumerators exist,
        /// returns the count of persisted positions instead.
        /// </summary>
        public new int Count
        {
            get => configuration.ResumeSession ? base.Count == 0 ? positionCollection.Count() : base.Count : base.Count;
        }

        /// <summary>
        /// Gets the lowest (earliest) read ID across all enumerators.
        /// When ResumeSession is enabled, considers BOTH active in-memory enumerators AND persisted positions;
        /// otherwise uses only active enumerators.
        /// Returns null if no enumerators exist or if none have read any entries yet.
        /// </summary>
        public new TId? LowestReadId
        {
            get
            {
                if (!configuration.ResumeSession)
                {
                    // Resume disabled: only consider active in-memory enumerators
                    return this.Min(e => e.CurrentId);
                }

                // Resume enabled: consider BOTH active and persisted enumerators
                var activeLowest = this.Min(e => e.CurrentId);
                var persistedLowest = GetLowestPersistedReadId();

                // Return the minimum of the two (or the one that exists if only one is non-null)
                if (activeLowest == null) return persistedLowest;
                if (persistedLowest == null) return activeLowest;

                return activeLowest.Value.CompareTo(persistedLowest.Value) < 0 ? activeLowest : persistedLowest;
            }
        }

        /// <summary>
        /// Queries LiteDB for the minimum CurrentId across all persisted enumerator positions.
        /// This ensures eviction respects all persisted sessions, not just active in-memory enumerators.
        /// </summary>
        /// <returns>The lowest persisted CurrentId, or null if no positions exist or all are null.</returns>
        private TId? GetLowestPersistedReadId()
        {
            var positions = positionCollection.FindAll().ToList();
            
            if (positions.Count == 0)
                return null;
            
            var validPositions = positions.Where(p => p.CurrentId.HasValue).ToList();
            
            if (validPositions.Count == 0)
                return null;
            
            return validPositions.Min(p => p.CurrentId.Value);
        }
    }
}
