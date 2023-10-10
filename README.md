# NTDLS.FastMemoryCache
Provides fast and easy to use partitioned memory cache for C# that helps manage the maximum size and track performance.

>**Quick and easy eample of a file cache:**
>
>Using the memory cache is easy, just initialize and upsert some values.
> You can also pass a configuration parameter to set max memory size, cache scavange rate and partition count.
```csharp
readonly PartitionedCache _cache = new PartitionedCache();

public string ReadFileFromDisk(string path)
{
    string cacheKey = path.ToLower();

    if (_cache.TryGet<string>(cacheKey, out var cachedObject))
    {
        return cachedObject;
    }

    string fileContents = File.ReadAllText(path);
    _cache.Upsert(cacheKey, fileContents, fileContents.Length * sizeof(char));
    return fileContents;
}
```
