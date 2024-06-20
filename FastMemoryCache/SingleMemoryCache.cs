using NTDLS.Semaphore;
using System.Diagnostics.CodeAnalysis;

namespace NTDLS.FastMemoryCache
{
    /// <summary>
    /// Defines a single memory cache instance.
    /// </summary>
    public class SingleMemoryCache : IDisposable
    {
        private readonly PessimisticCriticalResource<Dictionary<string, SingleMemoryCacheItem>> _collection = new();
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
                    _collection.Use((obj) => obj.Clear());
                    _timer?.Dispose();
                }
                _disposed = true;
            }
        }

        #endregion

        /// <summary>
        /// Returns a copy of all of the lookup keys defined in the cache.
        /// </summary>
        /// <returns></returns>
        public List<string> CloneCacheKeys() => _collection.Use((obj) => obj.Select(o => o.Key).ToList());

        /// <summary>
        /// Returns copies of all items contained in the cache.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, SingleMemoryCacheItem> CloneCacheItems() =>
            _collection.Use((obj) => obj.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Clone()
            ));

        #region CTor.

        /// <summary>
        /// Initializes a new memory cache with the default configuration.
        /// </summary>
        public SingleMemoryCache()
        {
            _configuration = new SingleCacheConfiguration();
            if (_configuration.ScavengeIntervalSeconds > 0)
            {
                _timer = new Timer(TimerTickCallback, this, TimeSpan.FromSeconds(_configuration.ScavengeIntervalSeconds), TimeSpan.FromSeconds(_configuration.ScavengeIntervalSeconds));
            }
        }

        /// <summary>
        /// Initializes a new memory cache with the default configuration.
        /// </summary>
        public SingleMemoryCache(SingleCacheConfiguration configuration)
        {
            _configuration = configuration.Clone();
            if (_configuration.ScavengeIntervalSeconds > 0)
            {
                _timer = new Timer(TimerTickCallback, this, TimeSpan.FromSeconds(_configuration.ScavengeIntervalSeconds), TimeSpan.FromSeconds(_configuration.ScavengeIntervalSeconds));
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
                if (_configuration.MaxMemoryMegabytes <= 0)
                {
                    return;
                }

                var sizeInMegabytes = SizeInMegabytes();

                _collection.TryUse(50, (obj) =>
                {
                    //When we reach our set memory pressure, we will remove the least recently hit items from cache.
                    //TODO: since we have the hit count, update count, etc. maybe we can make this more intelligent?

                    double objectSizeSummation = 0;
                    double spaceNeededToClear = (sizeInMegabytes - _configuration.MaxMemoryMegabytes) * 1024.0 * 1024.0;

                    //Remove expired objects:
                    foreach (var item in obj.Where(o => o.Value.IsExpired).Select(o => new ItemToRemove(o.Key, o.Value.AproximateSizeInBytes, true)))
                    {
                        Remove(item.Key);
                        sizeInMegabytes -= item.AproximateSizeInBytes / 1024 / 1024;
                    }

                    //If we are still over memory limit, remove items until we are under the memory limit:
                    if (sizeInMegabytes > _configuration.MaxMemoryMegabytes)
                    {
                        foreach (var item in obj.OrderBy(o => o.Value.LastGetDate).Select(o => new ItemToRemove(o.Key, o.Value.AproximateSizeInBytes)))
                        {
                            Remove(item.Key);
                            objectSizeSummation += item.AproximateSizeInBytes;
                            if (item.Expired)
                            {
                                continue; //We want tp remove all expired items before we check spaceNeededToClear.
                            }

                            if (objectSizeSummation >= spaceNeededToClear)
                            {
                                break;
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
        /// <returns></returns>
        public int Count() => _collection.Use((obj) => obj.Count);

        /// <summary>
        /// The number of times that all items in the cache have been retrieved.
        /// </summary>
        /// <returns></returns>
        public ulong TotalGetCount() => (ulong)_collection.Use((obj) => obj.Sum(o => (decimal)o.Value.GetCount));

        /// <summary>
        /// The number of times that all items have been updated in cache.
        /// </summary>
        public ulong TotalSetCount() => (ulong)_collection.Use((obj) => obj.Sum(o => (decimal)o.Value.SetCount));

        /// <summary>
        /// Returns the size of all items stored in the cache.
        /// </summary>
        /// <returns></returns>
        public double SizeInMegabytes() => _collection.Use((obj) => obj.Sum(o => o.Value.AproximateSizeInBytes / 1024.0 / 1024.0));
        /// <summary>
        /// Returns the size of all items stored in the cache.
        /// </summary>
        /// <returns></returns>
        public double SizeInKilobytes() => _collection.Use((obj) => obj.Sum(o => o.Value.AproximateSizeInBytes / 1024.0));

        #endregion

        #region Getters.

        /// <summary>
        /// Returns true if the suppled key is found in the cache.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <returns></returns>
        public bool Contains(string key)
        {
            if (_configuration.IsCaseSensitive == false)
            {
                key = key.ToLower();
            }
            return _collection.Use((obj) => obj.ContainsKey(key));
        }

        /// <summary>
        /// Gets the cache item with the supplied key value, throws an exception if it is not found.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <returns></returns>
        public object Get(string key)
        {
            if (_configuration.IsCaseSensitive == false)
            {
                key = key.ToLower();
            }

            return _collection.Use((obj) =>
            {
                var result = obj[key];
                result.GetCount++;
                result.LastGetDate = DateTime.UtcNow;
                return result.Value;
            });
        }

        /// <summary>
        /// Gets the cache item with the supplied key value, throws an exception if it is not found.
        /// </summary>
        /// <typeparam name="T">The type of the object that is stored in cache.</typeparam>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <returns></returns>
        public T Get<T>(string key)
        {
            if (_configuration.IsCaseSensitive == false)
            {
                key = key.ToLower();
            }

            return (T)_collection.Use((obj) =>
            {
                var result = obj[key];
                result.GetCount++;
                result.LastGetDate = DateTime.UtcNow;
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
        /// <returns></returns>
        public bool TryGet<T>(string key, [NotNullWhen(true)] out T? cachedObject)
        {
            if (_configuration.IsCaseSensitive == false)
            {
                key = key.ToLower();
            }

            var cachedItem = _collection.Use((obj) =>
            {
                if (obj.ContainsKey(key))
                {
                    var result = obj[key];
                    result.GetCount++;
                    result.LastGetDate = DateTime.UtcNow;
                    return result;
                }

                return null;
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
        /// <returns></returns>
        public object? TryGet(string key)
        {
            if (_configuration.IsCaseSensitive == false)
            {
                key = key.ToLower();
            }

            return _collection.Use((obj) =>
            {
                if (obj.ContainsKey(key))
                {
                    var result = obj[key];
                    result.GetCount++;
                    result.LastGetDate = DateTime.UtcNow;
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
            if (_configuration.IsCaseSensitive == false)
            {
                key = key.ToLower();
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            approximateSizeInBytes ??= Estimations.ObjectSize(value);

            _collection.Use(obj =>
            {
                if (obj.ContainsKey(key))
                {
                    var cacheItem = obj[key];
                    cacheItem.Value = value;
                    cacheItem.SetCount++;
                    cacheItem.LastSetDate = DateTime.UtcNow;
                    cacheItem.AproximateSizeInBytes = (int)approximateSizeInBytes;
                }
                else
                {
                    obj.Add(key, new SingleMemoryCacheItem(value, (int)approximateSizeInBytes, (int)(timeToLive?.TotalMilliseconds ?? 0)));
                }
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
            if (_configuration.IsCaseSensitive == false)
            {
                key = key.ToLower();
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            approximateSizeInBytes ??= Estimations.ObjectSize(value);

            _collection.Use(obj =>
            {
                if (obj.ContainsKey(key))
                {
                    var cacheItem = obj[key];
                    cacheItem.Value = value;
                    cacheItem.SetCount++;
                    cacheItem.LastSetDate = DateTime.UtcNow;
                    cacheItem.AproximateSizeInBytes = (int)approximateSizeInBytes;
                }
                else
                {
                    obj.Add(key, new SingleMemoryCacheItem(value, (int)approximateSizeInBytes, (int)(timeToLive?.TotalMilliseconds ?? 0)));
                }
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
        public bool Remove(string key) => _collection.Use((obj) => obj.Remove(key));

        /// <summary>
        /// Removes all itemsfrom the cache that start with the given string, returns the count of items found and removed.
        /// </summary>
        /// <param name="prefix">The beginning of the cache key to look for when removing cache items.</param>
        /// <returns>The number of items that were removed from cache.</returns>
        public int RemoveItemsWithPrefix(string prefix)
        {
            int itemsRemoved = 0;

            if (_configuration.IsCaseSensitive == false)
            {
                prefix = prefix.ToLower();
            }

            _collection.Use(obj =>
            {
                var keysToRemove = CloneCacheKeys().Where(entry => entry.StartsWith(prefix)).ToList();

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
        public void Clear() => _collection.Use((obj) => obj.Clear());

        #endregion
    }
}
