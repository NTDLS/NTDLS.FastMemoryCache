namespace NTDLS.FastMemoryCache
{
    public class PartitionedCacheConfiguration
    {
        public int ParitionCount { get; set; } = Environment.ProcessorCount * 4;
        public int ScavengeIntervalSeconds { get; set; } = 30;
        public int MaxMemoryMegabytes { get; set; } = 4096;

        public PartitionedCacheConfiguration Clone()
        {
            return new PartitionedCacheConfiguration()
            {
                MaxMemoryMegabytes = MaxMemoryMegabytes,
                ParitionCount = ParitionCount,
                ScavengeIntervalSeconds = ScavengeIntervalSeconds
            };
        }
    }
}
