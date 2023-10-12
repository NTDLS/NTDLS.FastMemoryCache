namespace NTDLS.FastMemoryCache.Metrics
{
    /// <summary>
    /// Holds configration and performance metrics information about each item in the cache.
    /// </summary>
    public class CachePartitionAllocationDetails
    {
        /// <summary>
        /// The configuration of the partitoned cache instance.
        /// </summary>
        public PartitionedCacheConfiguration Configuration { get; internal set; }

        /// <summary>
        /// Contains a list of all cached items and their metrics.
        /// </summary>
        public List<CachePartitionAllocationDetailItem> Items { get; private set; } = new();

        /// <summary>
        /// Instanciates a new instance of the allocation details.
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
            /// Instanciates a new instance of the detail metric.
            /// </summary>
            /// <param name="key"></param>
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
            /// The aproximate memory size of the cache item.
            /// </summary>
            public int AproximateSizeInBytes { get; set; }

            /// <summary>
            /// The number of times that the cache item has been retreived from cache.
            /// </summary>
            public ulong GetCount { get; set; } = 0;

            /// <summary>
            /// The number of times that the cache item has been updated in cache.
            /// </summary>
            public ulong SetCount { get; set; } = 0;

            /// <summary>
            /// The UTC date/time that the item was created in cache.
            /// </summary>
            public DateTime? Created { get; set; }

            /// <summary>
            /// The UTC date/time that the item was last updated in cache.
            /// </summary>
            public DateTime? LastSetDate { get; set; }

            /// <summary>
            /// The UTC date/time that the item was last retreived from cache.
            /// </summary>
            public DateTime? LastGetDate { get; set; }
        }
    }
}
