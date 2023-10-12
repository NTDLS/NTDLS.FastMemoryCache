namespace NTDLS.FastMemoryCache
{
    /// <summary>
    /// Defines a cache item instance. This is the item that is stored in the cahce. It keep track of the item and various metrics.
    /// </summary>
    public class SingleMemoryCacheItem
    {
        /// <summary>
        /// A reference to the items that was cached.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// The aproximate size of the cached item in memory.
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

        /// <summary>
        /// Creates an instance of the cache item using a reference to the to-be-cached object.
        /// </summary>
        /// <param name="value"></param>
        public SingleMemoryCacheItem(object value)
        {
            Value = value;
            Created = DateTime.UtcNow;
            LastSetDate = Created;
            LastGetDate = Created;
            SetCount = 1;
        }

        /// <summary>
        /// Creates an instance of the cache item using a reference to the to-be-cached object.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="aproximateSizeInBytes"></param>
        public SingleMemoryCacheItem(object value, int aproximateSizeInBytes)
        {
            Value = value;
            Created = DateTime.UtcNow;
            LastSetDate = Created;
            LastGetDate = Created;
            SetCount = 1;
            AproximateSizeInBytes = aproximateSizeInBytes;
        }

        /// <summary>
        /// Returns a clone of the cached item.
        /// </summary>
        /// <returns></returns>
        public SingleMemoryCacheItem Clone()
        {
            return new SingleMemoryCacheItem(Value, AproximateSizeInBytes)
            {
                GetCount = GetCount,
                SetCount = SetCount,
                Created = Created,
                LastSetDate = LastSetDate,
                LastGetDate = LastGetDate,
            };
        }
    }
}
