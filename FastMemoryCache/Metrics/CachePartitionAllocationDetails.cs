namespace NTDLS.FastMemoryCache.Metrics
{
    /// <summary>
    /// Holds configuration and performance metrics information about each item in the cache.
    /// </summary>
    public class CachePartitionAllocationDetails
    {
        /// <summary>
        /// The configuration of the partitioned cache instance.
        /// </summary>
        public PartitionedCacheConfiguration Configuration { get; internal set; }

        /// <summary>
        /// Contains a list of all cached items and their metrics.
        /// </summary>
        public List<CachePartitionAllocationDetailItem> Items { get; private set; } = new();

        /// <summary>
        /// Instantiates a new instance of the allocation details.
        /// </summary>
        /// <param name="configuration"></param>
        public CachePartitionAllocationDetails(PartitionedCacheConfiguration configuration)
        {
            Configuration = configuration.Clone();
        }

        /// <summary>
        /// Contains metrics about each item in the cache.
        /// </summary>
        public class CachePartitionAllocationDetailItem
        {
            /// <summary>
            /// Instantiates a new instance of the detail metric.
            /// </summary>
            /// <param name="key">The unique cache key used to identify the item.</param>
            public CachePartitionAllocationDetailItem(string key)
            {
                Key = key;
            }

            /// <summary>
            /// The lookup ket of the value in the cache.
            /// </summary>
            public string Key { get; set; }

            /// <summary>
            /// The cache partition number that contains the cache item.
            /// </summary>
            public int Partition { get; set; }

            /// <summary>
            /// The approximate memory size of the cache item.
            /// </summary>
            public int ApproximateSizeInBytes { get; set; }

            /// <summary>
            /// The number of times that the cache item has been retrieved from cache.
            /// </summary>
            public ulong Reads { get; set; } = 0;

            /// <summary>
            /// The number of times that the cache item has been updated in cache.
            /// </summary>
            public ulong Writes { get; set; } = 0;

            /// <summary>
            /// The UTC date/time that the item was created in cache.
            /// </summary>
            public DateTime? Created { get; set; }

            /// <summary>
            /// The UTC date/time that the item was last updated in cache.
            /// </summary>
            public DateTime? LastWrite { get; set; }

            /// <summary>
            /// The UTC date/time that the item was last retrieved from cache.
            /// </summary>
            public DateTime? LastRead { get; set; }
        }
    }
}
