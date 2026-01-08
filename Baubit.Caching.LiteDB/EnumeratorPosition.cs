using System;

namespace Baubit.Caching.LiteDB
{
    /// <summary>
    /// Represents a persisted enumerator position in LiteDB.
    /// Used to resume async enumeration sessions across application restarts.
    /// </summary>
    /// <typeparam name="TId">The type of the unique identifier.</typeparam>
    public class EnumeratorPosition<TId>
        where TId : struct, IComparable<TId>, IEquatable<TId>
    {
        /// <summary>
        /// Gets or sets the unique identifier for this enumerator session.
        /// This is the Id parameter passed to the enumerator factory.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the current position (entry ID) in the enumeration.
        /// Null if enumeration hasn't started yet.
        /// </summary>
        public TId? CurrentId { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when this position was last updated.
        /// </summary>
        public DateTime LastUpdatedUTC { get; set; }

        /// <summary>
        /// Parameterless constructor required for LiteDB serialization.
        /// </summary>
        public EnumeratorPosition()
        {
            LastUpdatedUTC = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a new enumerator position.
        /// </summary>
        /// <param name="id">The enumerator session ID.</param>
        /// <param name="currentId">The current position in enumeration.</param>
        public EnumeratorPosition(string id, TId? currentId)
        {
            Id = id;
            CurrentId = currentId;
            LastUpdatedUTC = DateTime.UtcNow;
        }
    }
}
