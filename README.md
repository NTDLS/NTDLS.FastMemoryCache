# NTDLS.FastMemoryCache

📦 Be sure to check out the NuGet package: https://www.nuget.org/packages/NTDLS.FastMemoryCache

Provides fast and easy to use thread-safe partitioned memory cache for C# that helps manage the maximum size and track performance.

👀 This memory cache was developed to deal with caches containing large numbers of items. When working on a database server written in C# we found that we had significant contention around the cache. This project was the solution. It also allowed us to better manage the size of the cache and enforce max memory constraints. That said, if you are only caching a few dozen items - stick with IMemoryCache.


>**Quick and easy example of a file cache:**
>
>Using the memory cache is easy, just initialize and upsert some values.
> You can also pass a configuration parameter to set max memory size, cache scavenge rate and partition count.
```csharp
PartitionedMemoryCache _cache = new();

public string ReadFileFromDisk(string path)
{
    if (_cache.TryGet<string>(cacheKey, out var cachedObject))
    {
        return cachedObject;
    }

    string fileContents = File.ReadAllText(path);
    _cache.Upsert(cacheKey, fileContents, fileContents.Length * sizeof(char));
    return fileContents;
}
```

## License
[MIT](https://choosealicense.com/licenses/mit/)
