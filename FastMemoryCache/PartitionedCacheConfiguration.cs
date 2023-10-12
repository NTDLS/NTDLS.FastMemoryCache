namespace NTDLS.FastMemoryCache
{
    /// <summary>
    /// Defines the configuration for a partitioned memory cache instance.
    /// </summary>
    public class PartitionedCacheConfiguration
    {
        /// <summary>
        /// The number of partitions that the memory cache should be split into.
        /// </summary>
        public int PartitionCount { get; set; } = Environment.ProcessorCount * 4;

        /// <summary>
        /// The number of seconds between attempts to sure-up the set memory limits.
        /// </summary>
        public int ScavengeIntervalSeconds { get; set; } = 30;

        /// <summary>
        /// The maximum size of the memory cache. The cache will attempt to keep the cache to this size.
        /// </summary>
        public int MaxMemoryMegabytes { get; set; } = 4096;

        /// <summary>
        /// Whether the cache keys are treated as case sensitive or not.
        /// </summary>
        public bool IsCaseSensitive { get; set; } = true;

        /// <summary>
        /// Returns a copy of the configuration instance.
        /// </summary>
        /// <returns></returns>
        public PartitionedCacheConfiguration Clone()
        {
            return new PartitionedCacheConfiguration()
            {
                MaxMemoryMegabytes = MaxMemoryMegabytes,
                PartitionCount = PartitionCount,
                ScavengeIntervalSeconds = ScavengeIntervalSeconds,
                IsCaseSensitive = IsCaseSensitive
            };
        }
    }
}
