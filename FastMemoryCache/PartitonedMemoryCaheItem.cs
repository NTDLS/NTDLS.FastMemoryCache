namespace NTDLS.FastMemoryCache
{
    public class PartitonedMemoryCaheItem
    {
        public object Value { get; set; }
        public int AproximateSizeInBytes { get; set; }
        public ulong GetCount { get; set; } = 0;
        public ulong SetCount { get; set; } = 0;
        public DateTime? Created { get; set; }
        public DateTime? LastSetDate { get; set; }
        public DateTime? LastGetDate { get; set; }

        public PartitonedMemoryCaheItem(object value)
        {
            Value = value;
            Created = DateTime.UtcNow;
            LastSetDate = Created;
            LastGetDate = Created;
            SetCount = 1;
        }

        public PartitonedMemoryCaheItem(object value, int aproximateSizeInBytes)
        {
            Value = value;
            Created = DateTime.UtcNow;
            LastSetDate = Created;
            LastGetDate = Created;
            SetCount = 1;
            AproximateSizeInBytes = aproximateSizeInBytes;
        }

        public PartitonedMemoryCaheItem Clone()
        {
            return new PartitonedMemoryCaheItem(Value, AproximateSizeInBytes)
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
