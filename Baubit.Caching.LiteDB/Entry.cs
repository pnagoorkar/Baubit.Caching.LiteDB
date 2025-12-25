using System;
using LiteDB;

namespace Baubit.Caching.LiteDB
{
    /// <summary>
    /// Entry class for LiteDB storage that implements <see cref="IEntry{TId, TValue}"/>.
    /// </summary>
    /// <typeparam name="TId">The type of the unique identifier. Must be a value type that implements IComparable and IEquatable.</typeparam>
    /// <typeparam name="TValue">The type of value stored in the entry.</typeparam>
    public class Entry<TId, TValue> : IEntry<TId, TValue>
        where TId : struct, IComparable<TId>, IEquatable<TId>
    {
        /// <summary>
        /// Gets or sets the unique identifier for this entry.
        /// </summary>
        public TId Id { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when this entry was created.
        /// </summary>
        [BsonIgnore]
        public DateTime CreatedOnUTC
        {
            get => new DateTime(CreatedOnUtcTicks, DateTimeKind.Utc);
            set
            {
                DateTime utc;
                switch (value.Kind)
                {
                    case DateTimeKind.Utc: utc = value; break;
                    case DateTimeKind.Local: utc = value.ToUniversalTime(); break;
                    case DateTimeKind.Unspecified: utc = DateTime.SpecifyKind(value, DateTimeKind.Utc); break;
                    default: utc = value; break;
                }
                CreatedOnUtcTicks = utc.Ticks;
            }
        }

        [BsonField(nameof(CreatedOnUTC))]
        public long CreatedOnUtcTicks { get; set; } = DateTime.UtcNow.Ticks;

        /// <summary>
        /// Gets or sets the value stored in this entry.
        /// </summary>
        public TValue Value { get; set; }

        /// <summary>
        /// Parameterless constructor required for LiteDB serialization.
        /// </summary>
        public Entry()
        {
            // Ensure CreatedOnUTC has a sensible default even when LiteDB
            // instantiates via parameterless ctor
            if (CreatedOnUtcTicks == 0)
                CreatedOnUTC = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a new entry with the specified id and value.
        /// </summary>
        /// <param name="id">The unique identifier for this entry.</param>
        /// <param name="value">The value to store.</param>
        public Entry(TId id, TValue value)
        {
            Id = id;
            Value = value;
            CreatedOnUTC = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Entry class for LiteDB storage that implements <see cref="IEntry{TValue}"/> using Guid as the ID type.
    /// </summary>
    /// <typeparam name="TValue">The type of value stored in the entry.</typeparam>
    public class Entry<TValue> : Entry<Guid, TValue>
    {
        /// <summary>
        /// Parameterless constructor required for LiteDB serialization.
        /// </summary>
        public Entry() : base()
        {
        }

        /// <summary>
        /// Creates a new entry with the specified id and value.
        /// </summary>
        /// <param name="id">The unique identifier for this entry.</param>
        /// <param name="value">The value to store.</param>
        public Entry(Guid id, TValue value) : base(id, value)
        {
        }
    }
}
