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
        /// The number of milliseconds from insertion, update or last read that the item should live in cache. 0 = infinite.
        /// </summary>
        public int TimeToLiveMilliseconds { get; set; } = 0;

        /// <summary>
        /// The approximate size of the cached item in memory.
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
        /// Returns true if the cache item has expired according to its TimeToLiveSeconds.
        /// </summary>
        public bool IsExpired
        {
            get
            {
                if (TimeToLiveMilliseconds > 0)
                {
                    var greatestDate = LastSetDate > LastGetDate ? LastSetDate : LastGetDate;
                    if (greatestDate != null)
                    {
                        return (DateTime.UtcNow - ((DateTime)greatestDate)).TotalMilliseconds > TimeToLiveMilliseconds;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Creates an instance of the cache item using a reference to the to-be-cached object.
        /// </summary>
        /// <param name="value">The value to store in the cache.</param>
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
        /// <param name="value">The value to store in the cache.</param>
        /// <param name="approximateSizeInBytes">The approximate size of the object in bytes. If NULL, the size will estimated.</param>
        /// <param name="timeToLiveSeconds">The number of seconds from insertion, update or last read that the item should live in cache. 0 = infinite.</param>
        public SingleMemoryCacheItem(object value, int approximateSizeInBytes, int timeToLiveSeconds)
        {
            Value = value;
            Created = DateTime.UtcNow;
            LastSetDate = Created;
            LastGetDate = Created;
            SetCount = 1;
            TimeToLiveMilliseconds = timeToLiveSeconds;
            AproximateSizeInBytes = approximateSizeInBytes;
        }

        /// <summary>
        /// Returns a clone of the cached item.
        /// </summary>
        /// <returns></returns>
        public SingleMemoryCacheItem Clone()
        {
            return new SingleMemoryCacheItem(Value, AproximateSizeInBytes, TimeToLiveMilliseconds)
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
