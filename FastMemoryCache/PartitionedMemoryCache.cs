using System.Diagnostics.CodeAnalysis;

namespace NTDLS.FastMemoryCache
{
    internal class PartitionedMemoryCache
    {

        private readonly SingleMemoryCache[] _partitions;

        public int PartitionCount { get; private set; }

        private readonly PartitionedCacheConfiguration _configuration;


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

            for (int i = 0; i < PartitionCount; i++)
            {
                _partitions[i] = new SingleMemoryCache(singleConfiguration);
            }
        }

        public void Close()
        {
            for (int partitionIndex = 0; partitionIndex < PartitionCount; partitionIndex++)
            {
                _partitions[partitionIndex].Dispose();
            }
        }

        public void Clear()
        {
            for (int partitionIndex = 0; partitionIndex < PartitionCount; partitionIndex++)
            {
                _partitions[partitionIndex].Clear();
            }
        }

        public CachePartitionAllocationStats GetPartitionAllocationStatistics()
        {
            var result = new CachePartitionAllocationStats
            {
                PartitionCount = PartitionCount
            };

            for (int partitionIndex = 0; partitionIndex < PartitionCount; partitionIndex++)
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
                PartitionCount = PartitionCount
            };

            for (int partitionIndex = 0; partitionIndex < PartitionCount; partitionIndex++)
            {
                lock (_partitions[partitionIndex])
                {
                    foreach (var item in _partitions[partitionIndex].CloneCacheItems())
                    {
                        result.Partitions.Add(new CachePartitionAllocationDetails.CachePartitionAllocationDetail(item.Key)
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

        public object Get(string key)
        {
            if (_configuration.IsCaseSensitive == false)
            {
                key = key.ToLower();
            }

            int partitionIndex = Math.Abs(key.GetHashCode() % PartitionCount);

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

            int partitionIndex = Math.Abs(key.GetHashCode() % PartitionCount);

            lock (_partitions[partitionIndex])
            {
                return _partitions[partitionIndex].Get<T>(key);
            }
        }

        public bool TryGet<T>(string key, [NotNullWhen(true)] out T? cachedObject)
        {
            if (_configuration.IsCaseSensitive == false)
            {
                key = key.ToLower();
            }

            int partitionIndex = Math.Abs(key.GetHashCode() % PartitionCount);

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

            int partitionIndex = Math.Abs(key.GetHashCode() % PartitionCount);

            lock (_partitions[partitionIndex])
            {
                return _partitions[partitionIndex].TryGet(key);
            }
        }

        public void Upsert<T>(string key, T value)
        {
            if (_configuration.IsCaseSensitive == false)
            {
                key = key.ToLower();
            }

            int partitionIndex = Math.Abs(key.GetHashCode() % PartitionCount);

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

            int partitionIndex = Math.Abs(key.GetHashCode() % PartitionCount);

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

            int partitionIndex = Math.Abs(key.GetHashCode() % PartitionCount);

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

            int partitionIndex = Math.Abs(key.GetHashCode() % PartitionCount);

            lock (_partitions[partitionIndex])
            {
                _partitions[partitionIndex].Upsert<T>(key, value, aproximateSizeInBytes);
            }
        }


        public int Remove(string key)
        {
            if (_configuration.IsCaseSensitive == false)
            {
                key = key.ToLower();
            }

            int partitionIndex = Math.Abs(key.GetHashCode() % PartitionCount);

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

            for (int i = 0; i < PartitionCount; i++)
            {
                lock (_partitions[i])
                {
                    _partitions[i].RemoveItemsWithPrefix(prefix);
                }
            }
        }
    }
}
