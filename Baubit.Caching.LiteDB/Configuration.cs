namespace Baubit.Caching.LiteDB
{
    /// <summary>
    /// Extended configuration for LiteDB-backed caching with session resume support.
    /// Inherits from Baubit.Caching.Configuration and adds LiteDB-specific settings.
    /// </summary>
    public class Configuration : Baubit.Caching.Configuration
    {
        /// <summary>
        /// Gets or sets a value indicating whether to resume enumeration sessions from persisted state.
        /// When true, async enumerators will check LiteDB for saved positions and resume from there.
        /// When false, enumerators always start from the beginning.
        /// </summary>
        public bool ResumeSession { get; set; }

        /// <summary>
        /// Gets or sets the number of MoveNext operations before persisting position to LiteDB.
        /// Higher values improve performance but reduce reliability if application crashes.
        /// Default is 0 (do not persist position at all).
        /// Set to 1 for maximum reliability (persist after every move).
        /// Set to higher values (e.g., 10, 100) to reduce I/O overhead.
        /// </summary>
        public int PersistPositionEveryXMoves { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether to persist position before or after moving to next entry.
        /// When true (default): persists AFTER moving (better reliability, position always reflects last successfully read entry).
        /// When false: persists BEFORE moving (better performance, but may lose last entry on crash).
        /// </summary>
        public bool PersistPositionAfterMove { get; set; } = true;
    }
}
