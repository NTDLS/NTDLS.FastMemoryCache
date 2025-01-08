namespace NTDLS.FastMemoryCache
{
    /// <summary>
    /// Defines the configuration for a single memory cache instance.
    /// </summary>
    public class SingleCacheConfiguration
    {
        /// <summary>
        /// Gets or sets the maximum size of the cache in bytes. (0 = no limit)
        /// </summary>
        public long SizeLimitBytes { get; set; } = Defaults.SizeLimitBytes;

        /// <summary>
        /// Whether the cache keys are treated as case sensitive or not.
        /// </summary>
        public bool IsCaseSensitive { get; set; } = Defaults.IsCaseSensitive;

        /// <summary>
        /// Whether or not the cache should track object size for memory limitations and cache evictions.
        /// </summary>
        public bool EstimateObjectSize { get; set; } = Defaults.EstimateObjectSize;

        /// <summary>
        /// Gets or sets a value that indicates whether linked entries are tracked.
        /// </summary>
        public bool TrackLinkedCacheEntries { get; set; } = Defaults.TrackLinkedCacheEntries;

        /// <summary>
        /// Gets or sets the amount the cache is compacted by when the maximum size is exceeded.
        /// </summary>
        public double CompactionPercentage { get; set; } = Defaults.CompactionPercentage;

        /// <summary>
        /// Gets or sets the minimum length of time between successive scans for expired items.
        /// </summary>
        public TimeSpan ExpirationScanFrequency { get; set; } = Defaults.ExpirationScanFrequency;

        /// <summary>
        /// Returns a copy of the configuration instance.
        /// </summary>
        public SingleCacheConfiguration Clone()
        {
            return new SingleCacheConfiguration()
            {
                CompactionPercentage = CompactionPercentage,
                EstimateObjectSize = EstimateObjectSize,
                ExpirationScanFrequency = ExpirationScanFrequency,
                IsCaseSensitive = IsCaseSensitive,
                SizeLimitBytes = SizeLimitBytes,
                TrackLinkedCacheEntries = TrackLinkedCacheEntries
            };
        }
    }
}
