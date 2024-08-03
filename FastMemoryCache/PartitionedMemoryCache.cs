using NTDLS.FastMemoryCache.Metrics;
using System.Diagnostics.CodeAnalysis;

namespace NTDLS.FastMemoryCache
{
    /// <summary>
    /// Defines an instance of a partitioned memory cache. This is basically an array of SingleMemoryCache 
    /// instances that are all managed independently and accesses are "striped" across the partitions.
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

        /// <summary>
        /// Computes the partition for a given key. This must be case insensitive.
        /// </summary>
        private int GetKeyPartition(string key)
            => Math.Abs(key.ToLowerInvariant().GetHashCode() % _configuration.PartitionCount);

        #region Ctor.

        /// <summary>
        /// Defines an instance of the memory cache with a default configuration.
        /// </summary>
        public PartitionedMemoryCache()
        {
            _configuration = new PartitionedCacheConfiguration();
            _partitions = new SingleMemoryCache[_configuration.PartitionCount];

            int maxMemoryPerPartition = (int)(_configuration.MaxMemoryBytes / (double)_configuration.PartitionCount);

            int minMemoryPerPartition = SingleMemoryCache.MinimumMemorySizePerPartition;
            if (maxMemoryPerPartition < minMemoryPerPartition)
            {
                maxMemoryPerPartition = minMemoryPerPartition;
            }

            var singleConfiguration = new SingleCacheConfiguration
            {
                MaxMemoryBytes = _configuration.MaxMemoryBytes == 0 ? 0 : maxMemoryPerPartition < 1 ? 1 : maxMemoryPerPartition,
                ScavengeIntervalSeconds = _configuration.ScavengeIntervalSeconds,
                IsCaseSensitive = _configuration.IsCaseSensitive,
                TrackObjectSize = _configuration.TrackObjectSize
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

            int maxMemoryPerPartition = (int)(_configuration.MaxMemoryBytes / (double)_configuration.PartitionCount);

            int minMemoryPerPartition = SingleMemoryCache.MinimumMemorySizePerPartition;
            if (maxMemoryPerPartition < minMemoryPerPartition)
            {
                maxMemoryPerPartition = minMemoryPerPartition;
            }

            var singleConfiguration = new SingleCacheConfiguration
            {
                MaxMemoryBytes = _configuration.MaxMemoryBytes == 0 ? 0 : maxMemoryPerPartition < 1 ? 1 : maxMemoryPerPartition,
                ScavengeIntervalSeconds = _configuration.ScavengeIntervalSeconds,
                IsCaseSensitive = _configuration.IsCaseSensitive,
                TrackObjectSize = _configuration.TrackObjectSize
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
        public long Count()
        {
            long totalValue = 0;

            for (int i = 0; i < _configuration.PartitionCount; i++)
            {
                lock (_partitions[i])
                {
                    totalValue += _partitions[i].Count();
                }
            }

            return totalValue;
        }

        /// <summary>
        /// The number of times that all items in the cache have been retrieved.
        /// </summary>
        public ulong TotalGetCount()
        {
            ulong totalValue = 0;

            for (int i = 0; i < _configuration.PartitionCount; i++)
            {
                lock (_partitions[i])
                {
                    totalValue += _partitions[i].TotalGetCount();
                }
            }

            return totalValue;
        }

        /// <summary>
        /// The number of times that all items have been updated in cache.
        /// </summary>
        public ulong TotalSetCount()
        {
            ulong totalValue = 0;

            for (int i = 0; i < _configuration.PartitionCount; i++)
            {
                lock (_partitions[i])
                {
                    totalValue += _partitions[i].TotalSetCount();
                }
            }

            return totalValue;
        }

        /// <summary>
        /// Returns the total size of all cache items across all cache partitions.
        /// </summary>
        public double ApproximateSizeInBytes()
        {
            double totalValue = 0;

            for (int i = 0; i < _configuration.PartitionCount; i++)
            {
                lock (_partitions[i])
                {
                    totalValue += _partitions[i].ApproximateSizeInBytes();
                }
            }

            return totalValue;
        }

        /// <summary>
        /// Returns high level statistics about the cache partitions.
        /// </summary>
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
                        SizeInBytes = _partitions[partitionIndex].ApproximateSizeInBytes(),
                        Reads = _partitions[partitionIndex].TotalGetCount(),
                        Writes = _partitions[partitionIndex].TotalSetCount(),
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Returns detailed level statistics about the cache partitions.
        /// </summary>
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
                            ApproximateSizeInBytes = item.Value.ApproximateSizeInBytes,
                            Reads = item.Value.Reads,
                            Writes = item.Value.Writes,
                            Created = item.Value.Created,
                            LastWrite = item.Value.LastWrite,
                            LastRead = item.Value.LastRead,
                        });
                    }
                }
            }

            return result;
        }

        #endregion

        #region Getters.

        /// <summary>
        /// Determines if any of the cache partitions contain a cache item with the supplied key value.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        public bool Contains(string key)
        {
            int partitionIndex = GetKeyPartition(key);

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
        /// <param name="key">The unique cache key used to identify the item.</param>
        public object Get(string key)
        {
            int partitionIndex = GetKeyPartition(key);

            lock (_partitions[partitionIndex])
            {
                return _partitions[partitionIndex].Get(key);
            }
        }

        /// <summary>
        /// Gets the cache item with the supplied key value, throws an exception if it is not found.
        /// </summary>
        /// <typeparam name="T">The type of the object that is stored in cache.</typeparam>
        /// <param name="key">The unique cache key used to identify the item.</param>
        public T Get<T>(string key)
        {
            int partitionIndex = GetKeyPartition(key);

            lock (_partitions[partitionIndex])
            {
                return _partitions[partitionIndex].Get<T>(key);
            }
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
            int partitionIndex = GetKeyPartition(key);

            lock (_partitions[partitionIndex])
            {
                return _partitions[partitionIndex].TryGet(key, out cachedObject);
            }
        }

        /// <summary>
        /// Attempts to get the cache item with the supplied key value, returns true of found otherwise false.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        public object? TryGet(string key)
        {
            int partitionIndex = GetKeyPartition(key);

            lock (_partitions[partitionIndex])
            {
                return _partitions[partitionIndex].TryGet(key);
            }
        }

        #endregion

        #region Upserters.

        /// <summary>
        /// Inserts an item into the memory cache. If it already exists, then it will be updated.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <param name="value">The value to store in the cache.</param>
        /// <param name="approximateSizeInBytes">The approximate size of the object in bytes. If NULL, the size will estimated.</param>
        /// <param name="timeToLive">The amount of time from insertion, update or last read that the item should live in cache. 0 = infinite.</param>
        public void Upsert(string key, object value, int? approximateSizeInBytes, TimeSpan? timeToLive)
        {
            int partitionIndex = GetKeyPartition(key);

            lock (_partitions[partitionIndex])
            {
                _partitions[partitionIndex].Upsert(key, value, approximateSizeInBytes, timeToLive);
            }
        }

        /// <summary>
        /// Inserts an item into the memory cache. If it already exists, then it will be updated.
        /// </summary>
        /// <typeparam name="T">The type of the object that is stored in cache.</typeparam>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <param name="value">The value to store in the cache.</param>
        /// <param name="approximateSizeInBytes">The approximate size of the object in bytes. If NULL, the size will estimated.</param>
        /// <param name="timeToLive">The amount of time from insertion, update or last read that the item should live in cache. 0 = infinite.</param>
        public void Upsert<T>(string key, T value, int? approximateSizeInBytes, TimeSpan? timeToLive)
        {
            int partitionIndex = GetKeyPartition(key);

            lock (_partitions[partitionIndex])
            {
                _partitions[partitionIndex].Upsert<T>(key, value, approximateSizeInBytes, timeToLive);
            }
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

        #region Removers and Clear.

        /// <summary>
        /// Removes an item from the cache if it is found, returns true if found and removed.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <returns>True of the item was removed from cache.</returns>
        public bool Remove(string key)
        {
            int partitionIndex = GetKeyPartition(key);

            lock (_partitions[partitionIndex])
            {
                return _partitions[partitionIndex].Remove(key);
            }
        }

        /// <summary>
        /// Removes all items from the cache that start with the given string, returns the count of items found and removed.
        /// </summary>
        /// <param name="prefix">The beginning of the cache key to look for when removing cache items.</param>
        /// <returns>The number of items that were removed from cache.</returns>
        public int RemoveItemsWithPrefix(string prefix)
        {
            int itemsRemoved = 0;

            for (int i = 0; i < _configuration.PartitionCount; i++)
            {
                lock (_partitions[i])
                {
                    itemsRemoved += _partitions[i].RemoveItemsWithPrefix(prefix);
                }
            }

            return itemsRemoved;
        }

        /// <summary>
        /// Removes all items from all cache partitions.
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
