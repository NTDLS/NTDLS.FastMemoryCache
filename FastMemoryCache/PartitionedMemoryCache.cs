using NTDLS.FastMemoryCache.Metrics;
using System.Diagnostics.CodeAnalysis;

namespace NTDLS.FastMemoryCache
{
    public class PartitionedMemoryCache : IDisposable
    {
        private readonly SingleMemoryCache[] _partitions;
        private readonly PartitionedCacheConfiguration _configuration;

        #region Ctor.

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

        public double MaxSizeInMegabytes()
        {
            double totalVlaue = 0;

            for (int i = 0; i < _configuration.PartitionCount; i++)
            {
                lock (_partitions[i])
                {
                    totalVlaue += _partitions[i].MaxSizeInMegabytes();
                }
            }

            return totalVlaue;
        }

        public double MaxSizeInKilobytes()
        {
            double totalVlaue = 0;

            for (int i = 0; i < _configuration.PartitionCount; i++)
            {
                lock (_partitions[i])
                {
                    totalVlaue += _partitions[i].MaxSizeInKilobytes();
                }
            }

            return totalVlaue;
        }

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

        public CachePartitionAllocationStats GetPartitionAllocationStatistics()
        {
            var result = new CachePartitionAllocationStats
            {
                PartitionCount = _configuration.PartitionCount,
            };

            for (int partitionIndex = 0; partitionIndex < _configuration.PartitionCount; partitionIndex++)
            {
                lock (_partitions[partitionIndex])
                {
                    result.Partitions.Add(new CachePartitionAllocationStats.CachePartitionAllocationStat
                    {
                        Partition = partitionIndex,
                        Allocations = _partitions[partitionIndex].Count(),
                        SizeInKilobytes = _partitions[partitionIndex].SizeInKilobytes(),
                        MaxSizeInKilobytes = _partitions[partitionIndex].MaxSizeInKilobytes()
                    });
                }
            }

            return result;
        }

        public CachePartitionAllocationDetails GetPartitionAllocationDetails()
        {
            var result = new CachePartitionAllocationDetails
            {
                PartitionCount = _configuration.PartitionCount
            };

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

        public int Remove(string key)
        {
            if (_configuration.IsCaseSensitive == false)
            {
                key = key.ToLower();
            }

            int partitionIndex = Math.Abs(key.GetHashCode() % _configuration.PartitionCount);

            int itemsEjected = 0;

            lock (_partitions[partitionIndex])
            {
                if (_partitions[partitionIndex].Remove(key))
                {
                    itemsEjected++;
                }
            }

            return itemsEjected;
        }

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
