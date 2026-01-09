using LiteDB;
using System;
using System.Reflection;

namespace Baubit.Caching.LiteDB.Test
{
    /// <summary>
    /// Helper class for accessing internal members of Store for testing purposes.
    /// </summary>
    internal static class StoreTestHelper
    {
        /// <summary>
        /// Gets the internal Database property from a Store instance using reflection.
        /// </summary>
        /// <typeparam name="TId">The type of the unique identifier.</typeparam>
        /// <typeparam name="TValue">The type of value stored in the cache.</typeparam>
        /// <param name="store">The store instance.</param>
        /// <returns>The LiteDatabase instance used by the store.</returns>
        public static LiteDatabase GetDatabase<TId, TValue>(Store<TId, TValue> store)
            where TId : struct, IComparable<TId>, IEquatable<TId>
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            var property = typeof(Store<TId, TValue>).GetProperty("Database", 
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            
            if (property == null)
                throw new InvalidOperationException("Database property not found on Store type.");

            var database = property.GetValue(store) as LiteDatabase;
            
            if (database == null)
                throw new InvalidOperationException("Failed to retrieve Database from Store.");

            return database;
        }
    }
}
