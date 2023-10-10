namespace NTDLS.FastMemoryCache
{
    public class SingleCacheConfiguration
    {
        public int ScavengeIntervalSeconds { get; set; } = 30;
        public int MaxMemoryMegabytes { get; set; } = 4096;
        public bool IsCaseSensitive { get; set; } = true;

        public SingleCacheConfiguration Clone()
        {
            return new SingleCacheConfiguration()
            {
                MaxMemoryMegabytes = MaxMemoryMegabytes,
                ScavengeIntervalSeconds = ScavengeIntervalSeconds,
                IsCaseSensitive = IsCaseSensitive
            };
        }
    }
}
