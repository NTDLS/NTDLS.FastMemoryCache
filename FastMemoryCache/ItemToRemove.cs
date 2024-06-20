namespace NTDLS.FastMemoryCache
{
    internal class ItemToRemove
    {
        public string Key { get; set; }
        public int AproximateSizeInBytes { get; set; }
        public bool Expired { get; set; }

        public ItemToRemove(string key, int approximateSizeInBytes)
        {
            Key = key;
            AproximateSizeInBytes = approximateSizeInBytes;
        }

        public ItemToRemove(string key, int approximateSizeInBytes, bool expired)
        {
            Key = key;
            AproximateSizeInBytes = approximateSizeInBytes;
            Expired = expired;
        }
    }
}
