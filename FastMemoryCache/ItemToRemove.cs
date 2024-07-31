namespace NTDLS.FastMemoryCache
{
    internal class ItemToRemove
    {
        public string Key { get; set; }
        public int ApproximateSizeInBytes { get; set; }
        public bool Expired { get; set; }

        public ItemToRemove(string key, int approximateSizeInBytes)
        {
            Key = key;
            ApproximateSizeInBytes = approximateSizeInBytes;
        }

        public ItemToRemove(string key, int approximateSizeInBytes, bool expired)
        {
            Key = key;
            ApproximateSizeInBytes = approximateSizeInBytes;
            Expired = expired;
        }
    }
}
