namespace NTDLS.FastMemoryCache
{
    /// <summary>
    /// Defines the configuration for a single memory cache instance.
    /// </summary>
    public class SingleCacheConfiguration
    {
        /// <summary>
        /// The number of seconds between attempts to sure-up the set memory limits. 0 = no scavenging.
        /// </summary>
        public int ScavengeIntervalSeconds { get; set; } = 10;

        /// <summary>
        /// The number of seconds between attempts to sure-up the set memory limits. 0 = no limit.
        /// </summary>
        public long MaxMemoryBytes { get; set; } = 1024 * 1024 * 32;

        /// <summary>
        /// Whether the cache keys are treated as case sensitive or not.
        /// </summary>
        public bool IsCaseSensitive { get; set; } = true;

        /// <summary>
        /// Whether or not the cache should track object size for memory limitations and cache ejections.
        /// </summary>
        public bool TrackObjectSize { get; set; } = true;

        /// <summary>
        /// Returns a copy of the configuration instance.
        /// </summary>
        public SingleCacheConfiguration Clone()
        {
            return new SingleCacheConfiguration()
            {
                MaxMemoryBytes = MaxMemoryBytes,
                ScavengeIntervalSeconds = ScavengeIntervalSeconds,
                IsCaseSensitive = IsCaseSensitive,
                TrackObjectSize = TrackObjectSize
            };
        }
    }
}
