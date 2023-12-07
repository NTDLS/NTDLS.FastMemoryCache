namespace NTDLS.FastMemoryCache
{
    internal class ItemToRemove
    {
        public string Key { get; set; }
        public int AproximateSizeInBytes { get; set; }
        public bool Expired { get; set; }

        public ItemToRemove(string key, int aproximateSizeInBytes)
        {
            Key = key;
            AproximateSizeInBytes = aproximateSizeInBytes;
        }

        public ItemToRemove(string key, int aproximateSizeInBytes, bool expired)
        {
            Key = key;
            AproximateSizeInBytes = aproximateSizeInBytes;
            Expired = expired;
        }
    }
}
