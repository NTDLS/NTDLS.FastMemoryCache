namespace NTDLS.FastMemoryCache
{
    /// <summary>
    /// Defines a cache item instance. This is the item that is stored in the cache. It keep track of the item and various metrics.
    /// </summary>
    public class SingleMemoryCacheItem
    {
        /// <summary>
        /// A reference to the items that was cached.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// The approximate size of the cached item in memory.
        /// </summary>
        public int ApproximateSizeInBytes { get; set; }

        /// <summary>
        /// The number of milliseconds from insertion, update or last read that the item should live in cache. 0 = infinite.
        /// </summary>
        public int TimeToLiveMilliseconds { get; set; } = 0;

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

        /// <summary>
        /// Creates an instance of the cache item using a reference to the to-be-cached object.
        /// </summary>
        /// <param name="value">The value to store in the cache.</param>
        /// <param name="timeToLiveMilliseconds">The number of milliseconds to keep the item in the cache.</param>
        public SingleMemoryCacheItem(object value, int timeToLiveMilliseconds)
        {
            Value = value;
            Created = DateTime.UtcNow;
            LastWrite = Created;
            LastRead = Created;
            Writes = 1;
            TimeToLiveMilliseconds = timeToLiveMilliseconds;
        }

        /// <summary>
        /// Creates an instance of the cache item using a reference to the to-be-cached object.
        /// </summary>
        /// <param name="value">The value to store in the cache.</param>
        /// <param name="timeToLiveMilliseconds">The number of milliseconds to keep the item in the cache.</param>
        /// <param name="approximateSizeInBytes">The approximate size of the object in bytes. If NULL, the size will estimated.</param>
        public SingleMemoryCacheItem(object value, int timeToLiveMilliseconds, int approximateSizeInBytes)
        {
            Value = value;
            Created = DateTime.UtcNow;
            LastWrite = Created;
            LastRead = Created;
            Writes = 1;
            ApproximateSizeInBytes = approximateSizeInBytes;
            TimeToLiveMilliseconds = timeToLiveMilliseconds;
        }

        /// <summary>
        /// Returns a clone of the cached item.
        /// </summary>
        public SingleMemoryCacheItem Clone()
        {
            return new SingleMemoryCacheItem(Value, ApproximateSizeInBytes)
            {
                Reads = Reads,
                Writes = Writes,
                Created = Created,
                LastWrite = LastWrite,
                LastRead = LastRead,
            };
        }

        /// <summary>
        /// Returns true if the cache item has expired according to its TimeToLiveSeconds.
        /// </summary>
        public bool IsExpired
        {
            get
            {
                if (TimeToLiveMilliseconds > 0)
                {
                    var greatestDate = LastWrite > LastRead ? LastWrite : LastRead;
                    if (greatestDate != null)
                    {
                        return (DateTime.UtcNow - ((DateTime)greatestDate)).TotalMilliseconds > TimeToLiveMilliseconds;
                    }
                }
                return false;
            }
        }

    }
}
