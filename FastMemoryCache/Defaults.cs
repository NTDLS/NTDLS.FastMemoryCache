namespace NTDLS.FastMemoryCache
{
    internal static class Defaults
    {
        /// <summary>
        /// The minimum amount of memory that can be allocated to a single partition.
        /// </summary>
        public static readonly int MinimumMemoryBytesPerPartition = 1024 * 10;

        /// <summary>
        /// The number of partitions that the memory cache should be split into.
        /// </summary>
        public static readonly int PartitionCount = Environment.ProcessorCount * 4;

        /// <summary>
        /// Gets or sets the maximum size of the cache in bytes. (0 = no limit)
        /// </summary>
        public static readonly long SizeLimitBytes = 0;

        /// <summary>
        /// Whether the cache keys are treated as case sensitive or not.
        /// </summary>
        public static readonly bool IsCaseSensitive = true;

        /// <summary>
        /// Whether or not the cache should track object size for memory limitations and cache evictions.
        /// </summary>
        public static readonly bool EstimateObjectSize = false;

        /// <summary>
        /// Gets or sets a value that indicates whether linked entries are tracked.
        /// </summary>
        public static readonly bool TrackLinkedCacheEntries = false;

        /// <summary>
        /// Gets or sets the amount the cache is compacted by when the maximum size is exceeded.
        /// </summary>
        public static readonly double CompactionPercentage = 0.05;

        /// <summary>
        /// Gets or sets the minimum length of time between successive scans for expired items.
        /// </summary>
        public static readonly TimeSpan ExpirationScanFrequency = TimeSpan.FromMinutes(1);
    }
}
