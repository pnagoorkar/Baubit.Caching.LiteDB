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
    }
}
