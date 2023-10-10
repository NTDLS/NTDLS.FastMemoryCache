﻿namespace NTDLS.FastMemoryCache
{
    public class PartitionedCacheConfiguration
    {
        public int PartitionCount { get; set; } = Environment.ProcessorCount * 4;
        public int ScavengeIntervalSeconds { get; set; } = 30;
        public int MaxMemoryMegabytes { get; set; } = 4096;
        public bool IsCaseSensitive { get; set; } = true;

        public PartitionedCacheConfiguration Clone()
        {
            return new PartitionedCacheConfiguration()
            {
                MaxMemoryMegabytes = MaxMemoryMegabytes,
                PartitionCount = PartitionCount,
                ScavengeIntervalSeconds = ScavengeIntervalSeconds,
                IsCaseSensitive = IsCaseSensitive
            };
        }
    }
}
