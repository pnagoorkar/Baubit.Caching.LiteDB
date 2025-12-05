using Baubit.Caching;
using System;

namespace Baubit.Caching.LiteDB
{
    /// <summary>
    /// Entry class for LiteDB storage that implements <see cref="IEntry{TValue}"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of value stored in the entry.</typeparam>
    public class Entry<TValue> : IEntry<TValue>
    {
        /// <summary>
        /// Gets or sets the unique identifier for this entry.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp when this entry was created.
        /// </summary>
        public DateTime CreatedOnUTC { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the value stored in this entry.
        /// </summary>
        public TValue Value { get; set; }

        /// <summary>
        /// Parameterless constructor required for LiteDB serialization.
        /// </summary>
        public Entry()
        {
        }

        /// <summary>
        /// Creates a new entry with the specified id and value.
        /// </summary>
        /// <param name="id">The unique identifier for this entry.</param>
        /// <param name="value">The value to store.</param>
        public Entry(Guid id, TValue value)
        {
            Id = id;
            Value = value;
        }
    }
}
