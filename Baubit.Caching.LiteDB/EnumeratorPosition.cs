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
        /// The setter ensures the DateTime.Kind is always UTC.
        /// </summary>
        [BsonIgnore]
        public DateTime LastUpdatedUTC
        {
            get => new DateTime(LastUpdatedUtcTicks, DateTimeKind.Utc);
            set => LastUpdatedUtcTicks = DateTime.SpecifyKind(value, DateTimeKind.Utc).Ticks;
        }

        /// <summary>
        /// Gets or sets the ticks value for LastUpdatedUTC.
        /// Used by LiteDB for serialization to preserve UTC kind.
        /// </summary>
        [BsonField(nameof(LastUpdatedUTC))]
        public long LastUpdatedUtcTicks { get; set; } = DateTime.UtcNow.Ticks;

        /// <summary>
        /// Parameterless constructor required for LiteDB serialization.
        /// </summary>
        public EnumeratorPosition()
        {
            SessionId = "";
            if (LastUpdatedUtcTicks == 0)
                LastUpdatedUTC = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a new enumerator position.
        /// </summary>
        /// <param name="sessionId">The enumerator session ID.</param>
        /// <param name="currentId">The current position in enumeration.</param>
        /// <param name="lastUpdatedUTC">The UTC timestamp for when this position was last updated. If null, uses current UTC time.</param>
        public EnumeratorPosition(string sessionId, TId? currentId, DateTime? lastUpdatedUTC = null)
        {
            SessionId = sessionId;
            CurrentId = currentId;
            LastUpdatedUTC = lastUpdatedUTC ?? DateTime.UtcNow;
        }
    }
}
