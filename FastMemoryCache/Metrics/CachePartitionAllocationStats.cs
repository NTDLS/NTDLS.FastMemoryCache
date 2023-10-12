namespace NTDLS.FastMemoryCache.Metrics
{
    /// <summary>
    /// Holds configration and performance metrics information about the cache instance.
    /// </summary>
    public class CachePartitionAllocationStats
    {
        /// <summary>
        /// The configuration of the partitoned cache instance.
        /// </summary>
        public PartitionedCacheConfiguration Configuration { get; internal set; }

        /// <summary>
        /// Contains metrics about each cache partiton.
        /// </summary>
        public List<CachePartitionAllocationStat> Partitions { get; private set; } = new();

        /// <summary>
        /// Instanciates a new instance of the allocation details.
        /// </summary>
        /// <param name="configuration"></param>
        public CachePartitionAllocationStats(PartitionedCacheConfiguration configuration)
        {
            Configuration = configuration.Clone();
        }

        /// <summary>
        /// Contains metrics about a single cache partition.
        /// </summary>
        public class CachePartitionAllocationStat
        {
            /// <summary>
            /// The configuration of the partitoned cache instance.
            /// </summary>
            public SingleCacheConfiguration Configuration { get; internal set; }

            /// <summary>
            /// The cache partition number.
            /// </summary>
            public int Partition { get; set; }

            /// <summary>
            /// The count of items in the cache.
            /// </summary>
            public int Count { get; set; }

            /// <summary>
            /// The number of times that the cache item has been retreived from cache.
            /// </summary>
            public ulong GetCount { get; set; } = 0;

            /// <summary>
            /// The number of times that the cache item has been updated in cache.
            /// </summary>
            public ulong SetCount { get; set; } = 0;

            /// <summary>
            /// The total size of the items in the cache partition.
            /// </summary>
            public double SizeInKilobytes { get; set; }

            /// <summary>
            /// Instanciates a new instance of the single cache metrics.
            /// </summary>
            /// <param name="configuration"></param>
            public CachePartitionAllocationStat(SingleCacheConfiguration configuration)
            {
                Configuration = configuration.Clone();
            }
        }
    }
}
