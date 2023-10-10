namespace NTDLS.FastMemoryCache
{
    public class SingleMemoryCacheItem
    {
        public object Value { get; set; }
        public int AproximateSizeInBytes { get; set; }
        public ulong GetCount { get; set; } = 0;
        public ulong SetCount { get; set; } = 0;
        public DateTime? Created { get; set; }
        public DateTime? LastSetDate { get; set; }
        public DateTime? LastGetDate { get; set; }

        public SingleMemoryCacheItem(object value)
        {
            Value = value;
            Created = DateTime.UtcNow;
            LastSetDate = Created;
            LastGetDate = Created;
            SetCount = 1;
        }

        public SingleMemoryCacheItem(object value, int aproximateSizeInBytes)
        {
            Value = value;
            Created = DateTime.UtcNow;
            LastSetDate = Created;
            LastGetDate = Created;
            SetCount = 1;
            AproximateSizeInBytes = aproximateSizeInBytes;
        }

        public SingleMemoryCacheItem Clone()
        {
            return new SingleMemoryCacheItem(Value, AproximateSizeInBytes)
            {
                GetCount = GetCount,
                SetCount = SetCount,
                Created = Created,
                LastSetDate = LastSetDate,
                LastGetDate = LastGetDate,
            };
        }
    }
}
