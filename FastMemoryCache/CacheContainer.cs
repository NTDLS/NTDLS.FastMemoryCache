using System.Runtime.Caching;

namespace NTDLS.FastMemoryCache
{
    internal class CacheContainer
    {
        private readonly CacheItemPolicy _infinitePolicy = new()
        {
            AbsoluteExpiration = ObjectCache.InfiniteAbsoluteExpiration
        };

        private SingleCacheConfiguration _configuration = new();

        public MemoryCache MemMache { get; set; } = new("<SingleMemoryCache>");

        public void Configure(SingleCacheConfiguration configuration)
        {
            _configuration = configuration;
        }

        public SingleMemoryCacheItem? Get(string key)
        {
            if (!_configuration.IsCaseSensitive)
            {
                key = key.ToLowerInvariant();
            }

            return (SingleMemoryCacheItem?)MemMache.Get(key);
        }

        public bool Remove(string key)
        {
            if (!_configuration.IsCaseSensitive)
            {
                key = key.ToLowerInvariant();
            }
            return MemMache.Remove(key) != null;
        }

        public void Clear()
        {
            var keys = MemMache.Select(o => o.Key).ToList();
            foreach (var key in keys)
            {
                MemMache.Remove(key);
            }
        }

        public void Upsert(string key, object value, int? approximateSizeInBytes, TimeSpan? timeToLive)
        {
            if (!_configuration.IsCaseSensitive)
            {
                key = key.ToLowerInvariant();
            }

            var result = Get(key);
            if (result != null)
            {
                result.Value = value;
                result.Writes++;
                result.LastWrite = DateTime.UtcNow;
                result.ApproximateSizeInBytes = (approximateSizeInBytes ?? 0);
            }
            else
            {
                MemMache.Add(key, new SingleMemoryCacheItem(value, timeToLive ?? TimeSpan.Zero, (approximateSizeInBytes ?? 0)), _infinitePolicy);
            }
        }
    }
}
