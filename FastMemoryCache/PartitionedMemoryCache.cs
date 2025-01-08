using NTDLS.FastMemoryCache.Metrics;

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
        private int ComputePartition(string key)
            => Math.Abs(key.ToLowerInvariant().GetHashCode() % _configuration.PartitionCount);

        #region Ctor.

        /// <summary>
        /// Defines an instance of the memory cache with a default configuration.
        /// </summary>
        public PartitionedMemoryCache()
        {
            _configuration = new PartitionedCacheConfiguration();
            _partitions = new SingleMemoryCache[_configuration.PartitionCount];

            var sizeLimitBytesPerPartition = (long)(_configuration.SizeLimitBytes / (double)_configuration.PartitionCount);
            if (sizeLimitBytesPerPartition < Defaults.MinimumMemoryBytesPerPartition)
            {
                sizeLimitBytesPerPartition = Defaults.MinimumMemoryBytesPerPartition;
            }

            var singleConfiguration = new SingleCacheConfiguration
            {
                CompactionPercentage = _configuration.CompactionPercentage,
                EstimateObjectSize = _configuration.EstimateObjectSize,
                ExpirationScanFrequency = _configuration.ExpirationScanFrequency,
                IsCaseSensitive = _configuration.IsCaseSensitive,
                SizeLimitBytes = _configuration.SizeLimitBytes == 0 ? 0 : sizeLimitBytesPerPartition,
                TrackLinkedCacheEntries = _configuration.TrackLinkedCacheEntries
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

            var sizeLimitBytesPerPartition = (long)(_configuration.SizeLimitBytes / (double)_configuration.PartitionCount);
            if (sizeLimitBytesPerPartition < Defaults.MinimumMemoryBytesPerPartition)
            {
                sizeLimitBytesPerPartition = Defaults.MinimumMemoryBytesPerPartition;
            }

            var singleConfiguration = new SingleCacheConfiguration
            {
                CompactionPercentage = _configuration.CompactionPercentage,
                EstimateObjectSize = _configuration.EstimateObjectSize,
                ExpirationScanFrequency = _configuration.ExpirationScanFrequency,
                IsCaseSensitive = _configuration.IsCaseSensitive,
                SizeLimitBytes = _configuration.SizeLimitBytes == 0 ? 0 : sizeLimitBytesPerPartition,
                TrackLinkedCacheEntries = _configuration.TrackLinkedCacheEntries
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
        /// Returns the count of items stored across all cache partitions.
        /// </summary>
        public long Count()
            => _partitions.Sum(o => o.Count());

        /// <summary>
        /// Gets the total number of cache hits across all cache partitions.
        /// </summary>
        public long TotalHits()
            => _partitions.Sum(o => o.TotalHits());

        /// <summary>
        /// Gets the total number of cache misses across all cache partitions.
        /// </summary>
        public long TotalMisses()
            => _partitions.Sum(o => o.TotalMisses());

        /// <summary>
        /// Returns the total size of all cache items across all cache partitions.
        /// </summary>
        public long CurrentEstimatedSize()
            => _partitions.Sum(o => o.CurrentEstimatedSize());

        /// <summary>
        /// Returns high level statistics about the cache partitions.
        /// </summary>
        public CachePartitionAllocationStatistics GetPartitionAllocationStatistics()
        {
            var result = new CachePartitionAllocationStatistics(_configuration);

            int partitionIndex = 0;

            foreach (var partition in _partitions)
            {
                result.Partitions.Add(new CachePartitionAllocationStatistic(partition.Configuration)
                {
                    Partition = partitionIndex++,
                    Count = partition.Count(),
                    SizeInBytes = partition.CurrentEstimatedSize(),
                    Hits = partition.TotalHits(),
                    Misses = partition.TotalMisses()
                });
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
            => _partitions[ComputePartition(key)].Contains(key);

        /// <summary>
        /// Gets the cache item with the supplied key value, throws an exception if it is not found.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        public object? Get(string key)
            => _partitions[ComputePartition(key)].Get(key);

        /// <summary>
        /// Gets the cache item with the supplied key value, throws an exception if it is not found.
        /// </summary>
        /// <typeparam name="T">The type of the object that is stored in cache.</typeparam>
        /// <param name="key">The unique cache key used to identify the item.</param>
        public T? Get<T>(string key)
            => _partitions[ComputePartition(key)].Get<T>(key);

        #endregion

        #region TryGetters.

        /// <summary>
        /// Attempts to get the cache item with the supplied key value, returns true of found otherwise false.
        /// </summary>
        /// <typeparam name="T">The type of the object that is stored in cache.</typeparam>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <param name="result">The value associated with the given key.</param>
        public bool TryGet<T>(string key, out T? result)
            => _partitions[ComputePartition(key)].TryGet(key, out result);

        /// <summary>
        /// Attempts to get the cache item with the supplied key value, returns true of found otherwise false.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        /// <param name="result">The value associated with the given key.</param>
        public bool TryGet(string key, out object? result)
            => _partitions[ComputePartition(key)].TryGet(key, out result);

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
            => _partitions[ComputePartition(key)].Upsert(key, value, approximateSizeInBytes, timeToLive);

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

        #region Removers and Clear.

        /// <summary>
        /// Removes an item from the cache if it is found, returns true if found and removed.
        /// </summary>
        /// <param name="key">The unique cache key used to identify the item.</param>
        public void Remove(string key)
            => _partitions[ComputePartition(key)].Remove(key);

        /// <summary>
        /// Removes all items from the cache that start with the given string, returns the count of items found and removed.
        /// </summary>
        /// <param name="prefix">The beginning of the cache key to look for when removing cache items.</param>
        /// <returns>The number of items that were removed from cache.</returns>
        public int RemoveItemsWithPrefix(string prefix)
        {
            int itemsRemoved = 0;

            foreach (var partition in _partitions)
            {
                itemsRemoved += partition.RemoveItemsWithPrefix(prefix);
            }

            return itemsRemoved;
        }

        /// <summary>
        /// Removes all items from all cache partitions.
        /// </summary>
        public void Clear()
        {
            foreach (var partition in _partitions)
            {
                partition.Clear();
            }
        }

        #endregion
    }
}
