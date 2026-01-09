using System;
using LiteDB;

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
        /// BsonId attribute marks this as the document ID for LiteDB.
        /// </summary>
        [BsonId]
        public string SessionId { get; set; }

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
        /// <param name="sessionId">The enumerator session ID.</param>
        /// <param name="currentId">The current position in enumeration.</param>
        public EnumeratorPosition(string sessionId, TId? currentId)
        {
            SessionId = sessionId;
            CurrentId = currentId;
            LastUpdatedUTC = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a new enumerator position with explicit timestamp.
        /// </summary>
        /// <param name="sessionId">The enumerator session ID.</param>
        /// <param name="currentId">The current position in enumeration.</param>
        /// <param name="lastUpdatedUtc">The UTC timestamp for when this position was last updated.</param>
        public EnumeratorPosition(string sessionId, TId? currentId, DateTime lastUpdatedUtc)
        {
            SessionId = sessionId;
            CurrentId = currentId;
            LastUpdatedUTC = lastUpdatedUtc;
        }
    }
}
