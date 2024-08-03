using NTDLS.Semaphore;
using System.Diagnostics.CodeAnalysis;

namespace NTDLS.FastMemoryCache
{
    /// <summary>
    /// Defines a single memory cache instance.
    /// </summary>
    public class SingleMemoryCache : IDisposable
    {
        /// <summary>
        /// The minimum amount of memory that can be allocated to a single partition.
        /// </summary>
        public const int MinimumMemorySizePerPartition = 1024 * 512;


        private readonly PessimisticCriticalResource<CacheContainer> _container = new();
        private readonly Timer? _timer;
        private readonly SingleCacheConfiguration _configuration;
        private bool _currentlyCleaning = false;

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
                    _container.Use((obj) => obj.Clear());
                    _timer?.Dispose();
                }
                _disposed = true;
            }
        }

        #endregion

        /// <summary>
        /// Returns a copy of all of the lookup keys defined in the cache.
        /// </summary>
        public List<string> CloneCacheKeys() => _container.Use((obj) => obj.MemMache.Select(o => o.Key).ToList());

        /// <summary>
        /// Returns copies of all items contained in the cache.
        /// </summary>
        public Dictionary<string, SingleMemoryCacheItem> CloneCacheItems() =>
            _container.Use((obj) => obj.MemMache.ToDictionary(
                kvp => kvp.Key,
                kvp => ((SingleMemoryCacheItem)kvp.Value).Clone(),
                (_configuration.IsCaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase)
            ));

        #region CTor.

        /// <summary>
        /// Initializes a new memory cache with the default configuration.
        /// </summary>
        public SingleMemoryCache()
        {
            _configuration = new SingleCacheConfiguration();

            int minMemoryPerPartition = MinimumMemorySizePerPartition;
            if (_configuration.MaxMemoryBytes < minMemoryPerPartition)
            {
                _configuration.MaxMemoryBytes = minMemoryPerPartition;
            }

            _container.Use((obj) => obj.Initialize(_configuration));

            if (_configuration.ScavengeIntervalSeconds > 0)
            {
                _timer = new Timer(TimerTickCallback, this,
                    TimeSpan.FromSeconds(_configuration.ScavengeIntervalSeconds),
                    TimeSpan.FromSeconds(_configuration.ScavengeIntervalSeconds));
            }
        }

        /// <summary>
        /// Initializes a new memory cache with the given configuration.
        /// </summary>
        public SingleMemoryCache(SingleCacheConfiguration configuration)
        {
            _configuration = configuration.Clone();

            int minMemoryPerPartition = MinimumMemorySizePerPartition;
            if (_configuration.MaxMemoryBytes < minMemoryPerPartition)
            {
                _configuration.MaxMemoryBytes = minMemoryPerPartition;
            }

            _container.Use((obj) => obj.Initialize(_configuration));

            if (_configuration.ScavengeIntervalSeconds > 0)
            {
                _timer = new Timer(TimerTickCallback, this,
                    TimeSpan.FromSeconds(_configuration.ScavengeIntervalSeconds),
                    TimeSpan.FromSeconds(_configuration.ScavengeIntervalSeconds));
            }
        }

        #endregion

        private void TimerTickCallback(object? state)
        {
            if (_timer == null)
            {
                return;
            }

            lock (_timer)
            {
                if (_currentlyCleaning == true)
                {
                    return;
                }
                _currentlyCleaning = true;
            }

            try
            {
                if (_configuration.MaxMemoryBytes <= 0)
                {
                    return;
                }

                var totalSizeInBytes = ApproximateSizeInBytes();

                _container.TryUse(50, (obj) =>
                {
                    //When we reach our set memory pressure, we will remove the least recently hit items from cache.
                    //TODO: since we have the hit count, update count, etc. maybe we can make this more intelligent?

                    var expiredItems = obj.MemMache
                            .Select(o => new { Key = o.Key, Value = (SingleMemoryCacheItem)o.Value })
                            .Where(o => o.Value.IsExpired)
                            .Select(o => new ItemToRemove(o.Key, o.Value.ApproximateSizeInBytes, true));

                    if (expiredItems.Any())
                    {
                    }

                    //Remove expired objects:
                    foreach (var item in expiredItems)
                    {
                        Remove(item.Key);
                        totalSizeInBytes -= item.ApproximateSizeInBytes;
                    }

                    if (_configuration.TrackObjectSize)
                    {
                        double spaceNeededToClear = (totalSizeInBytes - _configuration.MaxMemoryBytes);
                        double objectSizeSummation = 0;

                        //If we are still over memory limit, remove items until we are under the memory limit:
                        if (totalSizeInBytes > _configuration.MaxMemoryBytes)
                        {
                            var itemsToRemove = obj.MemMache
                                    .OrderBy(o => ((SingleMemoryCacheItem)o.Value).LastRead)
                                    .Select(o => new ItemToRemove(o.Key, ((SingleMemoryCacheItem)o.Value).ApproximateSizeInBytes));

                            foreach (var item in itemsToRemove)
                            {
                                Remove(item.Key);
                                objectSizeSummation += item.ApproximateSizeInBytes;
                                if (item.Expired)
                                {
                                    continue; //We want to remove all expired items before we check spaceNeededToClear.
                                }

                                if (objectSizeSummation >= spaceNeededToClear)
                                {
                                    break;
                                }
                            }
                        }
                    }
                });
            }
            finally
            {
                lock (_timer)
                {
                    _currentlyCleaning = false;
                }
            }
        }

        #region Metrics.

        /// <summary>
        /// Returns the count of items stored in the cache.
        /// </summary>
        public long Count() => _container.Use((obj) => obj.MemMache.GetCount());

        /// <summary>
        /// The number of times that all items in the cache have been retrieved.
        /// </summary>
        public ulong TotalGetCount() => (ulong)_container.Use((obj)
            => obj.MemMache.Sum(o => (decimal)((SingleMemoryCacheItem)o.Value).Reads));

        /// <summary>
        /// The number of times that all items have been updated in cache.
        /// </summary>
        public ulong TotalSetCount() => (ulong)_container.Use((obj)
            => obj.MemMache.Sum(o => (decimal)((SingleMemoryCacheItem)o.Value).Writes));

        /// <summary>
        /// Returns the size of all items stored in the cache.
        /// </summary>
        public int ApproximateSizeInBytes() => _container.Use((obj)
            => obj.MemMache.Sum(o => ((SingleMemoryCacheItem)o.Value).ApproximateSizeInBytes));


        #endregion

        #region Getters.

        /// <summary>
        /// Returns true if the suppled key is found in the cache.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        public bool Contains(string key)
            => _container.Use((obj) => obj.MemMache.Contains(key));

        /// <summary>
        /// Gets the cache item with the supplied key value, throws an exception if it is not found.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        public object Get(string key)
        {
            return _container.Use((obj) =>
            {
                var result = (SingleMemoryCacheItem)obj.MemMache[key];
                result.Reads++;
                result.LastRead = DateTime.UtcNow;
                return result.Value;
            });
        }

        /// <summary>
        /// Gets the cache item with the supplied key value, throws an exception if it is not found.
        /// </summary>
        /// <typeparam name="T">The type of the object that is stored in cache.</typeparam>
        /// <param name="key">The unique cache key used to identify the item.</param>
        public T Get<T>(string key)
        {
            return (T)_container.Use((obj) =>
            {
                var result = (SingleMemoryCacheItem)obj.MemMache[key];
                result.Reads++;
                result.LastRead = DateTime.UtcNow;
                return result.Value;
            });
        }

        #endregion

        #region TryGetters.

        /// <summary>
        /// Attempts to get the cache item with the supplied key value, returns true of found otherwise false.
        /// </summary>
        /// <typeparam name="T">The type of the object that is stored in cache.</typeparam>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <param name="cachedObject"></param>
        public bool TryGet<T>(string key, [NotNullWhen(true)] out T? cachedObject)
        {
            var cachedItem = _container.Use((obj) =>
            {
                var result = obj.Get(key);
                if (result != null)
                {
                    result.Reads++;
                    result.LastRead = DateTime.UtcNow;
                }
                return result;
            });

            if (cachedItem != null)
            {
                cachedObject = (T)cachedItem.Value;
                return true;
            }
            else
            {
                cachedObject = default;
                return false;
            }
        }

        /// <summary>
        /// Attempts to get the cache item with the supplied key value, returns true of found otherwise false.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        public object? TryGet(string key)
        {
            return _container.Use((obj) =>
            {
                var result = obj.Get(key);
                if (result != null)
                {
                    result.Reads++;
                    result.LastRead = DateTime.UtcNow;
                    return result?.Value;
                }
                return null;
            });
        }

        #endregion

        #region Upserters.

        /// <summary>
        /// Inserts an item into the memory cache. If it already exists, then it will be updated. The size of the object will be estimated.
        /// </summary>
        /// <typeparam name="T">The type of the object that is stored in cache.</typeparam>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <param name="value">The value to store in the cache.</param>
        /// <param name="approximateSizeInBytes">The approximate size of the object in bytes. If NULL, the size will estimated.</param>
        /// <param name="timeToLive">The amount of time from insertion, update or last read that the item should live in cache. 0 = infinite.</param>
        public void Upsert<T>(string key, T value, int? approximateSizeInBytes, TimeSpan? timeToLive)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (_configuration.TrackObjectSize)
            {
                approximateSizeInBytes ??= Estimations.ObjectSize(value);
            }
            else
            {
                approximateSizeInBytes = 0;
            }

            _container.Use(obj =>
            {
                obj.Upsert(key, value, approximateSizeInBytes, timeToLive);
            });
        }

        /// <summary>
        /// Inserts an item into the memory cache. If it already exists, then it will be updated. The size of the object will be estimated.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <param name="value">The value to store in the cache.</param>
        /// <param name="approximateSizeInBytes">The approximate size of the object in bytes. If NULL, the size will estimated.</param>
        /// <param name="timeToLive">The amount of time from insertion, update or last read that the item should live in cache. 0 = infinite.</param>
        public void Upsert(string key, object value, int? approximateSizeInBytes, TimeSpan? timeToLive)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (_configuration.TrackObjectSize)
            {
                approximateSizeInBytes ??= Estimations.ObjectSize(value);
            }
            else
            {
                approximateSizeInBytes = 0;
            }

            _container.Use(obj =>
            {
                obj.Upsert(key, value, approximateSizeInBytes, timeToLive);
            });
        }

        /// <summary>
        /// Inserts an item into the memory cache. If it already exists, then it will be updated. The size of the object will be estimated.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <param name="value">The value to store in the cache.</param>
        public void Upsert<T>(string key, T value) => Upsert<T>(key, value, null, null);

        /// <summary>
        /// Inserts an item into the memory cache. If it already exists, then it will be updated. The size of the object will be estimated.
        /// </summary>
        /// <typeparam name="T">The type of the object that is stored in cache.</typeparam>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <param name="value">The value to store in the cache.</param>
        /// <param name="approximateSizeInBytes">The approximate size of the object in bytes. If NULL, the size will estimated.</param>
        public void Upsert<T>(string key, T value, int? approximateSizeInBytes) => Upsert<T>(key, value, approximateSizeInBytes, null);

        /// <summary>
        /// Inserts an item into the memory cache. If it already exists, then it will be updated. The size of the object will be estimated.
        /// </summary>
        /// <typeparam name="T">The type of the object that is stored in cache.</typeparam>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <param name="value">The value to store in the cache.</param>
        /// <param name="timeToLive">The amount of time from insertion, update or last read that the item should live in cache. 0 = infinite.</param>
        public void Upsert<T>(string key, T value, TimeSpan? timeToLive) => Upsert<T>(key, value, null, timeToLive);

        /// <summary>
        /// Inserts an item into the memory cache. If it already exists, then it will be updated. The size of the object will be estimated.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <param name="value">The value to store in the cache.</param>
        public void Upsert(string key, object value) => Upsert(key, value, null, null);

        /// <summary>
        /// Inserts an item into the memory cache. If it already exists, then it will be updated. The size of the object will be estimated.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <param name="value">The value to store in the cache.</param>
        /// <param name="approximateSizeInBytes">The approximate size of the object in bytes. If NULL, the size will estimated.</param>
        public void Upsert(string key, object value, int? approximateSizeInBytes) => Upsert(key, value, approximateSizeInBytes, null);

        /// <summary>
        /// Inserts an item into the memory cache. If it already exists, then it will be updated. The size of the object will be estimated.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <param name="value">The value to store in the cache.</param>
        /// <param name="timeToLive">The amount of time from insertion, update or last read that the item should live in cache. 0 = infinite.</param>
        public void Upsert(string key, object value, TimeSpan? timeToLive) => Upsert(key, value, null, timeToLive);

        #endregion

        #region Removers / Clear.

        /// <summary>
        /// Removes an item from the cache if it is found, returns true if found and removed.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <returns>True of the item was removed from cache.</returns>
        public bool Remove(string key)
            => _container.Use((obj) => obj.Remove(key));

        /// <summary>
        /// Removes all items from the cache that start with the given string, returns the count of items found and removed.
        /// </summary>
        /// <param name="prefix">The beginning of the cache key to look for when removing cache items.</param>
        /// <returns>The number of items that were removed from cache.</returns>
        public int RemoveItemsWithPrefix(string prefix)
        {
            int itemsRemoved = 0;

            var comparison = _configuration.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            _container.Use(obj =>
            {
                var keysToRemove = CloneCacheKeys().Where(entry => entry.StartsWith(prefix, comparison)).ToList();

                foreach (var key in keysToRemove)
                {
                    if (obj.Remove(key))
                    {
                        itemsRemoved++;
                    }
                }
            });

            return itemsRemoved;
        }

        /// <summary>
        /// Removes all items from the cache.
        /// </summary>
        public void Clear() => _container.Use((obj) => obj.Clear());

        #endregion
    }
}
