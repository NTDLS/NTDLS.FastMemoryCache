namespace NTDLS.FastMemoryCache.Metrics
{
    /// <summary>
    /// Contains metrics about a single cache partition.
    /// </summary>
    public class CachePartitionAllocationStatistic
    {
        /// <summary>
        /// The configuration of the partitioned cache instance.
        /// </summary>
        public SingleCacheConfiguration Configuration { get; internal set; }

        /// <summary>
        /// The cache partition number.
        /// </summary>
        public int Partition { get; set; }

        /// <summary>
        /// The count of items in the cache.
        /// </summary>
        public long Count { get; set; }

        /// <summary>
        /// The number of times that the cache item has been retrieved from cache.
        /// </summary>
        public long Hits { get; set; } = 0;

        /// <summary>
        /// The number of times that the cache item has been updated in cache.
        /// </summary>
        public long Misses { get; set; } = 0;

        /// <summary>
        /// The total size of the items in the cache partition.
        /// </summary>
        public double SizeInBytes { get; set; }

        /// <summary>
        /// Instantiates a new instance of the single cache metrics.
        /// </summary>
        public CachePartitionAllocationStatistic(SingleCacheConfiguration configuration)
        {
            Configuration = configuration.Clone();
        }
    }
}
