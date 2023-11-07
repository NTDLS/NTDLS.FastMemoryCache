# NTDLS.FastMemoryCache

📦 Be sure to check out the NuGet pacakge: https://www.nuget.org/packages/NTDLS.FastMemoryCache

Provides fast and easy to use thread-safe partitioned memory cache for C# that helps manage the maximum size and track performance.

>**Quick and easy eample of a file cache:**
>
>Using the memory cache is easy, just initialize and upsert some values.
> You can also pass a configuration parameter to set max memory size, cache scavange rate and partition count.
```csharp
readonly PartitionedMemoryCache _cache = new();

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

## License
[Apache-2.0](https://choosealicense.com/licenses/apache-2.0/)
