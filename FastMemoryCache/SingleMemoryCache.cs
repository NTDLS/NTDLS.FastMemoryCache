using NTDLS.Semaphore;
using System.Diagnostics.CodeAnalysis;

namespace NTDLS.FastMemoryCache
{
    /// <summary>
    /// Defines a single memory cache instance.
    /// </summary>
    public class SingleMemoryCache : IDisposable
    {
        private readonly PessimisticSemaphore<Dictionary<string, SingleMemoryCacheItem>> _collection = new();
        private readonly Timer? _timer;
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
            if (_configuration.MaxMemoryMegabytes <= 0)
            {
                return;
            }

            var sizeInMegabytes = SizeInMegabytes();
            if (sizeInMegabytes > _configuration.MaxMemoryMegabytes)
            {
                _collection.TryUse(50, (obj) =>
                {
                    //When we reach our set memory pressure, we will remove the least recently hit items from cache.
                    //TODO: since we have the hit count, update count, etc. maybe we can make this more intelligent?

                    var oldestGottenItems = obj.OrderBy(o => o.Value.LastGetDate)
                        .Select(o => new
                        {
                            o.Key,
                            o.Value.AproximateSizeInBytes
                        }
                        ).ToList();

                    double objectSizeSummation = 0;
                    double spaceNeededToClear = (sizeInMegabytes - _configuration.MaxMemoryMegabytes) * 1024.0 * 1024.0;

                    foreach (var item in oldestGottenItems)
                    {
                        Remove(item.Key);
                        objectSizeSummation += item.AproximateSizeInBytes;
                        if (objectSizeSummation >= spaceNeededToClear)
                        {
                            break;
                        }
                    }
                });
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
        /// <param name="key"></param>
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
        /// <param name="key"></param>
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
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
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
        /// Attempts to get the cache item with the supplied key value, returns true of found otherwise fale.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
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
        /// Attempts to get the cache item with the supplied key value, returns true of found otherwise fale.
        /// </summary>
        /// <param name="key"></param>
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
        /// Inserts an item into the memory cache. If it alreay exists, then it will be updated. The size of the object will be estimated.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Upsert<T>(string key, T value)
        {
            if (_configuration.IsCaseSensitive == false)
            {
                key = key.ToLower();
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var aproximateSizeInBytes = Estimations.ObjectSize(value);

            _collection.Use(obj =>
            {
                if (obj.ContainsKey(key))
                {
                    var cacheItem = obj[key];
                    cacheItem.Value = value;
                    cacheItem.SetCount++;
                    cacheItem.LastSetDate = DateTime.UtcNow;
                    cacheItem.AproximateSizeInBytes = aproximateSizeInBytes;
                }
                else
                {
                    obj.Add(key, new SingleMemoryCacheItem(value, aproximateSizeInBytes));
                }
            });
        }

        /// <summary>
        /// Inserts an item into the memory cache. If it alreay exists, then it will be updated. The size of the object will be estimated.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Upsert(string key, object value)
        {
            if (_configuration.IsCaseSensitive == false)
            {
                key = key.ToLower();
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var aproximateSizeInBytes = Estimations.ObjectSize(value);

            _collection.Use(obj =>
            {
                if (obj.ContainsKey(key))
                {
                    var cacheItem = obj[key];
                    cacheItem.Value = value;
                    cacheItem.SetCount++;
                    cacheItem.LastSetDate = DateTime.UtcNow;
                    cacheItem.AproximateSizeInBytes = aproximateSizeInBytes;
                }
                else
                {
                    obj.Add(key, new SingleMemoryCacheItem(value, aproximateSizeInBytes));
                }
            });
        }

        /// <summary>
        /// Inserts an item into the memory cache. If it alreay exists, then it will be updated. The size of the object will be estimated.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="aproximateSizeInBytes"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Upsert(string key, object value, int aproximateSizeInBytes = 0)
        {
            if (_configuration.IsCaseSensitive == false)
            {
                key = key.ToLower();
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _collection.Use(obj =>
            {
                if (obj.ContainsKey(key))
                {
                    var cacheItem = obj[key];
                    cacheItem.Value = value;
                    cacheItem.SetCount++;
                    cacheItem.LastSetDate = DateTime.UtcNow;
                    cacheItem.AproximateSizeInBytes = aproximateSizeInBytes;
                }
                else
                {
                    obj.Add(key, new SingleMemoryCacheItem(value, aproximateSizeInBytes));
                }
            });
        }


        /// <summary>
        /// Inserts an item into the memory cache. If it alreay exists, then it will be updated. The size of the object will be estimated.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="aproximateSizeInBytes"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void Upsert<T>(string key, T value, int aproximateSizeInBytes = 0)
        {
            if (_configuration.IsCaseSensitive == false)
            {
                key = key.ToLower();
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _collection.Use(obj =>
            {
                if (obj.ContainsKey(key))
                {
                    var cacheItem = obj[key];
                    cacheItem.Value = value;
                    cacheItem.SetCount++;
                    cacheItem.LastSetDate = DateTime.UtcNow;
                    cacheItem.AproximateSizeInBytes = aproximateSizeInBytes;
                }
                else
                {
                    obj.Add(key, new SingleMemoryCacheItem(value, aproximateSizeInBytes));
                }
            });
        }

        #endregion

        #region Removers / Clear.

        /// <summary>
        /// Removes an item from the cache if it is found, returns true if found and removed.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(string key) => _collection.Use((obj) => obj.Remove(key));

        /// <summary>
        /// Removes all itemsfrom the cache that start with the given string, returns the count of items found and removed.
        /// </summary>
        /// <param name="prefix"></param>
        public void RemoveItemsWithPrefix(string prefix)
        {
            if (_configuration.IsCaseSensitive == false)
            {
                prefix = prefix.ToLower();
            }

            _collection.Use(obj =>
            {
                var keysToRemove = CloneCacheKeys().Where(entry => entry.StartsWith(prefix)).ToList();

                foreach (var key in keysToRemove)
                {
                    obj.Remove(key);
                }

            });
        }

        /// <summary>
        /// Removes all items from the cache.
        /// </summary>
        public void Clear() => _collection.Use((obj) => obj.Clear());

        #endregion
    }
}
