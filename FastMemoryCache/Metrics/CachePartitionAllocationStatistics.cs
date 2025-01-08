namespace NTDLS.FastMemoryCache.Metrics
{
    /// <summary>
    /// Holds configuration and performance metrics information about the cache instance.
    /// </summary>
    public class CachePartitionAllocationStatistics
    {
        /// <summary>
        /// The configuration of the partitioned cache instance.
        /// </summary>
        public PartitionedCacheConfiguration Configuration { get; internal set; }

        /// <summary>
        /// Contains metrics about each cache partition.
        /// </summary>
        public List<CachePartitionAllocationStatistic> Partitions { get; private set; } = new();

        /// <summary>
        /// Instantiates a new instance of the allocation details.
        /// </summary>
        public CachePartitionAllocationStatistics(PartitionedCacheConfiguration configuration)
        {
            Configuration = configuration.Clone();
        }
    }
}
