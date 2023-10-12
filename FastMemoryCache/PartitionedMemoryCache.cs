using NTDLS.FastMemoryCache.Metrics;
using System.Diagnostics.CodeAnalysis;

namespace NTDLS.FastMemoryCache
{
    /// <summary>
    /// Defines an instance of a partitoned memory cache. This is basically an array of SingleMemoryCache 
    /// instances that are all managed independently and accesses are "striped" across the partitons.
    /// Partitioning reduces lock time as well as lookup time.
    /// </summary>
    public class PartitionedMemoryCache : IDisposable
    {
        private readonly SingleMemoryCache[] _partitions;
        private readonly PartitionedCacheConfiguration _configuration;

        /// <summary>
        /// Returns a cloned copy of the configuration.
        /// </summary>
        public PartitionedCacheConfiguration Configuration => _configuration.Clone();


        #region Ctor.

        /// <summary>
        /// Defines an instance of the memory cache with a default configuration.
        /// </summary>
        public PartitionedMemoryCache()
        {
            _configuration = new PartitionedCacheConfiguration();
            _partitions = new SingleMemoryCache[_configuration.PartitionCount];

            int maxMemoryPerPartition = (int)(_configuration.MaxMemoryMegabytes / (double)_configuration.PartitionCount);

            var singleConfiguration = new SingleCacheConfiguration
            {
                MaxMemoryMegabytes = maxMemoryPerPartition < 1 ? 1 : maxMemoryPerPartition,
                ScavengeIntervalSeconds = _configuration.ScavengeIntervalSeconds,
                IsCaseSensitive = _configuration.IsCaseSensitive
            };

            for (int i = 0; i < _configuration.PartitionCount; i++)
            {
                _partitions[i] = new SingleMemoryCache(singleConfiguration);
            }
        }

        /// <summary>
        /// Defines an instance of the memory cache with a user-defined configuration.
        /// </summary>
        /// <param name="configuration"></param>
        public PartitionedMemoryCache(PartitionedCacheConfiguration configuration)
        {
            _configuration = configuration.Clone();
            _partitions = new SingleMemoryCache[_configuration.PartitionCount];

            int maxMemoryPerPartition = (int)(_configuration.MaxMemoryMegabytes / (double)_configuration.PartitionCount);

            var singleConfiguration = new SingleCacheConfiguration
            {
                MaxMemoryMegabytes = maxMemoryPerPartition < 1 ? 1 : maxMemoryPerPartition,
                ScavengeIntervalSeconds = _configuration.ScavengeIntervalSeconds,
                IsCaseSensitive = _configuration.IsCaseSensitive
            };

            for (int i = 0; i < _configuration.PartitionCount; i++)
            {
                _partitions[i] = new SingleMemoryCache(singleConfiguration);
            }
        }

        #endregion

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
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    for (int partitionIndex = 0; partitionIndex < _configuration.PartitionCount; partitionIndex++)
                    {
                        _partitions[partitionIndex].Dispose();
                    }
                }
                _disposed = true;
            }
        }

        #endregion

        #region Metrics.

        /// <summary>
        /// Returns the count of items across all cache partitions.
        /// </summary>
        /// <returns></returns>
        public int Count()
        {
            int totalVlaue = 0;

            for (int i = 0; i < _configuration.PartitionCount; i++)
            {
                lock (_partitions[i])
                {
                    totalVlaue += _partitions[i].Count();
                }
            }

            return totalVlaue;
        }

        /// <summary>
        /// The number of times that all items in the cache have been retrieved.
        /// </summary>
        /// <returns></returns>
        public ulong TotalGetCount()
        {
            ulong totalVlaue = 0;

            for (int i = 0; i < _configuration.PartitionCount; i++)
            {
                lock (_partitions[i])
                {
                    totalVlaue += _partitions[i].TotalGetCount();
                }
            }

            return totalVlaue;
        }

        /// <summary>
        /// The number of times that all items have been updated in cache.
        /// </summary>
        /// <returns></returns>
        public ulong TotalSetCount()
        {
            ulong totalVlaue = 0;

            for (int i = 0; i < _configuration.PartitionCount; i++)
            {
                lock (_partitions[i])
                {
                    totalVlaue += _partitions[i].TotalSetCount();
                }
            }

            return totalVlaue;
        }

        /// <summary>
        /// Returns the total size of all cache items across all cache partitions.
        /// </summary>
        /// <returns></returns>
        public double SizeInMegabytes()
        {
            double totalVlaue = 0;

            for (int i = 0; i < _configuration.PartitionCount; i++)
            {
                lock (_partitions[i])
                {
                    totalVlaue += _partitions[i].SizeInMegabytes();
                }
            }

            return totalVlaue;
        }

        /// <summary>
        /// Returns the total size of all cache items across all cache partitions.
        /// </summary>
        /// <returns></returns>
        public double SizeInKilobytes()
        {
            double totalVlaue = 0;

            for (int i = 0; i < _configuration.PartitionCount; i++)
            {
                lock (_partitions[i])
                {
                    totalVlaue += _partitions[i].SizeInKilobytes();
                }
            }

            return totalVlaue;
        }

        /// <summary>
        /// Returns high level statistics about the cache partitons.
        /// </summary>
        /// <returns></returns>
        public CachePartitionAllocationStats GetPartitionAllocationStatistics()
        {
            var result = new CachePartitionAllocationStats(_configuration);

            for (int partitionIndex = 0; partitionIndex < _configuration.PartitionCount; partitionIndex++)
            {
                lock (_partitions[partitionIndex])
                {
                    result.Partitions.Add(new CachePartitionAllocationStats.CachePartitionAllocationStat(_partitions[partitionIndex].Configuration)
                    {
                        Partition = partitionIndex,
                        Count = _partitions[partitionIndex].Count(),
                        SizeInKilobytes = _partitions[partitionIndex].SizeInKilobytes(),
                        GetCount = _partitions[partitionIndex].TotalGetCount(),
                        SetCount = _partitions[partitionIndex].TotalSetCount(),
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Returns detailed level statistics about the cache partitons.
        /// </summary>
        /// <returns></returns>
        public CachePartitionAllocationDetails GetPartitionAllocationDetails()
        {
            var result = new CachePartitionAllocationDetails(_configuration);

            for (int partitionIndex = 0; partitionIndex < _configuration.PartitionCount; partitionIndex++)
            {
                lock (_partitions[partitionIndex])
                {
                    foreach (var item in _partitions[partitionIndex].CloneCacheItems())
                    {
                        result.Items.Add(new CachePartitionAllocationDetails.CachePartitionAllocationDetailItem(item.Key)
                        {
                            Partition = partitionIndex,
                            AproximateSizeInBytes = item.Value.AproximateSizeInBytes,
                            GetCount = item.Value.GetCount,
                            SetCount = item.Value.SetCount,
                            Created = item.Value.Created,
                            LastSetDate = item.Value.LastSetDate,
                            LastGetDate = item.Value.LastGetDate,
                        });
                    }
                }
            }

            return result;
        }

        #endregion

        #region Getters.

        /// <summary>
        /// Determines if any of the cache partitons contain a cache item with the supplied key value.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Contains(string key)
        {
            if (_configuration.IsCaseSensitive == false)
            {
                key = key.ToLower();
            }

            int partitionIndex = Math.Abs(key.GetHashCode() % _configuration.PartitionCount);

            lock (_partitions[partitionIndex])
            {
                if (_partitions[partitionIndex].Contains(key))
                {
                    return true;
                }
            }

            return false;
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

            int partitionIndex = Math.Abs(key.GetHashCode() % _configuration.PartitionCount);

            lock (_partitions[partitionIndex])
            {
                return _partitions[partitionIndex].Get(key);
            }
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

            int partitionIndex = Math.Abs(key.GetHashCode() % _configuration.PartitionCount);

            lock (_partitions[partitionIndex])
            {
                return _partitions[partitionIndex].Get<T>(key);
            }
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

            int partitionIndex = Math.Abs(key.GetHashCode() % _configuration.PartitionCount);

            lock (_partitions[partitionIndex])
            {
                return _partitions[partitionIndex].TryGet(key, out cachedObject);
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

            int partitionIndex = Math.Abs(key.GetHashCode() % _configuration.PartitionCount);

            lock (_partitions[partitionIndex])
            {
                return _partitions[partitionIndex].TryGet(key);
            }
        }

        #endregion

        #region Upserters.

        /// <summary>
        /// Inserts an item into the memory cache. If it alreay exists, then it will be updated. The size of the object will be estimated.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Upsert<T>(string key, T value)
        {
            if (_configuration.IsCaseSensitive == false)
            {
                key = key.ToLower();
            }

            int partitionIndex = Math.Abs(key.GetHashCode() % _configuration.PartitionCount);

            lock (_partitions[partitionIndex])
            {
                _partitions[partitionIndex].Upsert<T>(key, value);
            }
        }

        /// <summary>
        /// Inserts an item into the memory cache. If it alreay exists, then it will be updated. The size of the object will be estimated.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Upsert(string key, object value)
        {
            if (_configuration.IsCaseSensitive == false)
            {
                key = key.ToLower();
            }

            int partitionIndex = Math.Abs(key.GetHashCode() % _configuration.PartitionCount);

            lock (_partitions[partitionIndex])
            {
                _partitions[partitionIndex].Upsert(key, value);
            }
        }

        /// <summary>
        /// Inserts an item into the memory cache. If it alreay exists, then it will be updated.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="aproximateSizeInBytes"></param>
        public void Upsert(string key, object value, int aproximateSizeInBytes = 0)
        {
            if (_configuration.IsCaseSensitive == false)
            {
                key = key.ToLower();
            }

            int partitionIndex = Math.Abs(key.GetHashCode() % _configuration.PartitionCount);

            lock (_partitions[partitionIndex])
            {
                _partitions[partitionIndex].Upsert(key, value, aproximateSizeInBytes);
            }
        }

        /// <summary>
        /// Inserts an item into the memory cache. If it alreay exists, then it will be updated.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="aproximateSizeInBytes"></param>
        public void Upsert<T>(string key, T value, int aproximateSizeInBytes = 0)
        {
            if (_configuration.IsCaseSensitive == false)
            {
                key = key.ToLower();
            }

            int partitionIndex = Math.Abs(key.GetHashCode() % _configuration.PartitionCount);

            lock (_partitions[partitionIndex])
            {
                _partitions[partitionIndex].Upsert<T>(key, value, aproximateSizeInBytes);
            }
        }

        #endregion

        #region Removers and Clear.

        /// <summary>
        /// Removes an item from the cache if it is found, returns true if found and removed.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(string key)
        {
            if (_configuration.IsCaseSensitive == false)
            {
                key = key.ToLower();
            }

            int partitionIndex = Math.Abs(key.GetHashCode() % _configuration.PartitionCount);

            lock (_partitions[partitionIndex])
            {
                return _partitions[partitionIndex].Remove(key);
            }
        }

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

            for (int i = 0; i < _configuration.PartitionCount; i++)
            {
                lock (_partitions[i])
                {
                    _partitions[i].RemoveItemsWithPrefix(prefix);
                }
            }
        }

        /// <summary>
        /// Removes all items from all cache partitons.
        /// </summary>
        public void Clear()
        {
            for (int partitionIndex = 0; partitionIndex < _configuration.PartitionCount; partitionIndex++)
            {
                _partitions[partitionIndex].Clear();
            }
        }

        #endregion
    }
}
