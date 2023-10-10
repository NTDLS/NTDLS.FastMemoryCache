using NTDLS.Semaphore;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace NTDLS.FastMemoryCache
{
    public class PartitionedCache : IDisposable
    {
        private readonly CriticalResource<Dictionary<string, PartitonedMemoryCaheItem>> _collection = new();
        private readonly Timer _timer;
        private readonly PartitionedCacheConfiguration _configuration;

        #region IDisposable

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _collection.Use((obj) => obj.Clear());
                    _timer.Dispose();
                }
                _disposed = true;
            }
        }

        #endregion


        public List<string> CloneCacheKeys() => _collection.Use((obj) => obj.Select(o => o.Key).ToList());

        public Dictionary<string, PartitonedMemoryCaheItem> CloneCacheItems() =>
            _collection.Use((obj) => obj.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Clone()
            ));

        /// <summary>
        /// Initializes a new memory cache with the default configuration.
        /// </summary>
        public PartitionedCache()
        {
            _configuration = new PartitionedCacheConfiguration();
            _timer = new Timer(TimerTickCallback, this, TimeSpan.FromSeconds(_configuration.ScavengeIntervalSeconds), TimeSpan.FromSeconds(_configuration.ScavengeIntervalSeconds));
        }

        /// <summary>
        /// Initializes a new memory cache with the default configuration.
        /// </summary>
        public PartitionedCache(PartitionedCacheConfiguration configuration)
        {
            _configuration = configuration.Clone();
            _timer = new Timer(TimerTickCallback, this, TimeSpan.FromSeconds(_configuration.ScavengeIntervalSeconds), TimeSpan.FromSeconds(_configuration.ScavengeIntervalSeconds));
        }

        private void TimerTickCallback(object? state)
        {
            var maxMemoryMB = MaxSizeInMegabytes();

            var sizeInMegabytes = SizeInMegabytes();
            if (sizeInMegabytes > maxMemoryMB)
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
                    double spaceNeededToClear = (sizeInMegabytes - maxMemoryMB) * 1024.0 * 1024.0;

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

        public double SizeInMegabytes() => _collection.Use((obj) => obj.Sum(o => o.Value.AproximateSizeInBytes / 1024.0 / 1024.0));
        public double MaxSizeInMegabytes() => (_configuration.MaxMemoryMegabytes / _configuration.ParitionCount);
        public double MaxSizeInKilobytes() => (_configuration.MaxMemoryMegabytes / _configuration.ParitionCount) * 1024.0;
        public double SizeInKilobytes() => _collection.Use((obj) => obj.Sum(o => o.Value.AproximateSizeInBytes / 1024.0));
        public int Count() => _collection.Use((obj) => obj.Count);
        public bool Contains(string key) => _collection.Use((obj) => obj.ContainsKey(key));
        public bool Remove(string key) => _collection.Use((obj) => obj.Remove(key));
        public void Clear() => _collection.Use((obj) => obj.Clear());

        public object Get(string key)
        {
            return _collection.Use((obj) =>
            {
                var result = obj[key];
                result.GetCount++;
                result.LastGetDate = DateTime.UtcNow;
                return result.Value;
            });
        }

        public T Get<T>(string key)
        {
            return (T)_collection.Use((obj) =>
            {
                var result = obj[key];
                result.GetCount++;
                result.LastGetDate = DateTime.UtcNow;
                return result.Value;
            });
        }

        public bool TryGet<T>(string key, [NotNullWhen(true)] out T? cachedObject)
        {
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

        public object? TryGet(string key)
        {
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

        public void Upsert<T>(string key, T value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            int aproximateSizeInBytes = JsonSerializer.SerializeToUtf8Bytes(value).Length;

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
                    obj.Add(key, new PartitonedMemoryCaheItem(value, aproximateSizeInBytes));
                }
            });
        }

        public void Upsert(string key, object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            int aproximateSizeInBytes = JsonSerializer.SerializeToUtf8Bytes(value).Length;

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
                    obj.Add(key, new PartitonedMemoryCaheItem(value, aproximateSizeInBytes));
                }
            });
        }

        public void Upsert(string key, object value, int aproximateSizeInBytes = 0)
        {
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
                    obj.Add(key, new PartitonedMemoryCaheItem(value, aproximateSizeInBytes));
                }
            });
        }

        public void Upsert<T>(string key, T value, int aproximateSizeInBytes = 0)
        {
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
                    obj.Add(key, new PartitonedMemoryCaheItem(value, aproximateSizeInBytes));
                }
            });
        }
    }
}
