using Microsoft.Extensions.Caching.Memory;

namespace NTDLS.FastMemoryCache
{
    /// <summary>
    /// Defines a single memory cache instance.
    /// </summary>
    public class SingleMemoryCache : IDisposable
    {
        private readonly MemoryCache _memoryCache;
        private readonly SingleCacheConfiguration _configuration;

        /// <summary>
        /// Returns a cloned copy of the configuration.
        /// </summary>
        public SingleCacheConfiguration Configuration => _configuration.Clone();

        #region IDisposable

        private bool _disposed = false;

        /// <summary>
        /// Cleans up the memory cache instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Cleans up the memory cache instance.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _memoryCache.Dispose();
                }
                _disposed = true;
            }
        }

        #endregion

        /// <summary>
        /// Returns a copy of all of the lookup keys defined in the cache.
        /// </summary>
        public IEnumerable<string?> CacheKeys()
            => _memoryCache.Keys.Select(key => key.ToString());

        #region CTor.

        /// <summary>
        /// Initializes a new memory cache with the default configuration.
        /// </summary>
        public SingleMemoryCache()
        {
            _configuration = new SingleCacheConfiguration();

            if (_configuration.SizeLimitBytes < Defaults.MinimumMemoryBytesPerPartition)
            {
                _configuration.SizeLimitBytes = Defaults.MinimumMemoryBytesPerPartition;
            }

            _memoryCache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = _configuration.SizeLimitBytes,
                TrackStatistics = true,
                TrackLinkedCacheEntries = _configuration.TrackLinkedCacheEntries,
                CompactionPercentage = _configuration.CompactionPercentage,
                ExpirationScanFrequency = _configuration.ExpirationScanFrequency
            });
        }

        /// <summary>
        /// Initializes a new memory cache with the given configuration.
        /// </summary>
        public SingleMemoryCache(SingleCacheConfiguration configuration)
        {
            _configuration = configuration.Clone();

            if (_configuration.SizeLimitBytes < Defaults.MinimumMemoryBytesPerPartition)
            {
                _configuration.SizeLimitBytes = Defaults.MinimumMemoryBytesPerPartition;
            }

            _memoryCache = new MemoryCache(new MemoryCacheOptions
            {
                SizeLimit = _configuration.SizeLimitBytes,
                TrackStatistics = true
            });
        }

        #endregion

        #region Metrics.

        /// <summary>
        /// Returns the count of items stored in the cache.
        /// </summary>
        public long Count()
            => _memoryCache.GetCurrentStatistics()?.CurrentEntryCount ?? 0;

        /// <summary>
        /// Gets the total number of cache hits.
        /// </summary>
        public long TotalHits()
            => _memoryCache.GetCurrentStatistics()?.TotalHits ?? 0;

        /// <summary>
        /// Gets the total number of cache misses.
        /// </summary>
        public long TotalMisses()
            => _memoryCache.GetCurrentStatistics()?.TotalMisses ?? 0;

        /// <summary>
        /// Returns the size of all items stored in the cache.
        /// </summary>
        public long CurrentEstimatedSize()
            => _memoryCache.GetCurrentStatistics()?.CurrentEstimatedSize ?? 0;

        #endregion

        #region Getters.

        /// <summary>
        /// Returns true if the suppled key is found in the cache.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        public bool Contains(string key)
            => _memoryCache.TryGetValue(key, out _);

        /// <summary>
        /// Gets the cache item with the supplied key value, throws an exception if it is not found.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        public object? Get(string key)
        {
            if (!_configuration.IsCaseSensitive)
            {
                key = key.ToLowerInvariant();
            }
            return _memoryCache.Get(key);
        }

        /// <summary>
        /// Gets the cache item with the supplied key value, throws an exception if it is not found.
        /// </summary>
        /// <typeparam name="T">The type of the object that is stored in cache.</typeparam>
        /// <param name="key">The unique cache key used to identify the item.</param>
        public T? Get<T>(string key)
        {
            if (!_configuration.IsCaseSensitive)
            {
                key = key.ToLowerInvariant();
            }
            return (T?)_memoryCache.Get(key);
        }

        #endregion

        #region TryGetters.

        /// <summary>
        /// Attempts to get the cache item with the supplied key value, returns true of found otherwise false.
        /// </summary>
        /// <typeparam name="T">The type of the object that is stored in cache.</typeparam>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <param name="result">The value associated with the given key.</param>
        public bool TryGet<T>(string key, out T? result)
        {
            if (!_configuration.IsCaseSensitive)
            {
                key = key.ToLowerInvariant();
            }
            return _memoryCache.TryGetValue(key, out result);
        }

        /// <summary>
        /// Attempts to get the cache item with the supplied key value, returns true of found otherwise false.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <param name="result">The value associated with the given key.</param>
        public bool TryGet(string key, out object? result)
        {
            if (!_configuration.IsCaseSensitive)
            {
                key = key.ToLowerInvariant();
            }
            return _memoryCache.TryGetValue(key, out result);
        }

        #endregion

        #region Upserters.

        /// <summary>
        /// Inserts an item into the memory cache. If it already exists, then it will be updated. The size of the object will be estimated.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <param name="value">The value to store in the cache.</param>
        /// <param name="approximateSizeInBytes">The approximate size of the object in bytes. If NULL, the size will estimated.</param>
        /// <param name="timeToLive">The amount of time from insertion, update or last read that the item should live in cache.</param>
        public void Upsert(string key, object? value, int? approximateSizeInBytes, TimeSpan? timeToLive)
        {
            if (_configuration.EstimateObjectSize)
            {
                approximateSizeInBytes ??= Estimations.ObjectSize(value);
            }
            else
            {
                approximateSizeInBytes = 0;
            }

            if (!_configuration.IsCaseSensitive)
            {
                key = key.ToLowerInvariant();
            }

            _memoryCache.Set(key, value, new MemoryCacheEntryOptions
            {
                Size = approximateSizeInBytes,
                SlidingExpiration = timeToLive
            });
        }

        /// <summary>
        /// Inserts an item into the memory cache. If it already exists, then it will be updated. The size of the object will be estimated.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <param name="value">The value to store in the cache.</param>
        public void Upsert(string key, object value)
            => Upsert(key, value, null, null);

        /// <summary>
        /// Inserts an item into the memory cache. If it already exists, then it will be updated. The size of the object will be estimated.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <param name="value">The value to store in the cache.</param>
        /// <param name="approximateSizeInBytes">The approximate size of the object in bytes. If NULL, the size will estimated.</param>
        public void Upsert(string key, object value, int? approximateSizeInBytes)
            => Upsert(key, value, approximateSizeInBytes, null);

        /// <summary>
        /// Inserts an item into the memory cache. If it already exists, then it will be updated. The size of the object will be estimated.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <param name="value">The value to store in the cache.</param>
        /// <param name="timeToLive">The amount of time from insertion, update or last read that the item should live in cache. 0 = infinite.</param>
        public void Upsert(string key, object value, TimeSpan? timeToLive)
            => Upsert(key, value, null, timeToLive);

        #endregion

        #region Removers / Clear.

        /// <summary>
        /// Removes an item from the cache if it is found, returns true if found and removed.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        public void Remove(string key)
        {
            if (!_configuration.IsCaseSensitive)
            {
                key = key.ToLowerInvariant();
            }
            _memoryCache.Remove(key);
        }

        /// <summary>
        /// Removes all items from the cache that start with the given string, returns the count of items found and removed.
        /// </summary>
        /// <param name="prefix">The beginning of the cache key to look for when removing cache items.</param>
        /// <returns>The number of items that were removed from cache.</returns>
        public int RemoveItemsWithPrefix(string prefix)
        {
            int itemsRemoved = 0;

            var comparison = _configuration.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var keysToRemove = _memoryCache.Keys.Where(entry => entry.ToString()?.StartsWith(prefix, comparison) == true).ToList();

            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
                itemsRemoved++;
            }

            return itemsRemoved;
        }

        /// <summary>
        /// Removes all items from the cache.
        /// </summary>
        public void Clear()
            => _memoryCache.Clear();

        #endregion
    }
}
