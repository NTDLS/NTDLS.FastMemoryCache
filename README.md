# NTDLS.FastMemoryCache

A fast, thread-safe, partitioned in-memory cache for C#. It splits the cache into multiple independent partitions to reduce lock contention under heavy concurrent load, and lets you manage the maximum size of the cache (with automatic compaction when the limit is exceeded) while tracking hit/miss performance metrics.

📦 Be sure to check out the NuGet package: https://www.nuget.org/packages/NTDLS.FastMemoryCache

## When should you use it?

This cache was developed to deal with caches containing large numbers of items. When working on a database server written in C#, we found that we had significant contention around the cache. This project was the solution — it also allowed us to better manage the size of the cache and enforce max memory constraints.

You will want to use this package when:

- You are caching **large numbers of items** and see lock contention with `IMemoryCache` or similar single-lock designs.
- You need to **enforce a maximum memory size** on your cache and have it compact itself automatically when the limit is exceeded.
- You want **performance metrics** (hit count, miss count, item count, estimated current size) built in.
- You want per-item **time-to-live** expiration or prefix-based bulk removal.

That said, if you are only caching a few dozen items — stick with `IMemoryCache`.

## Usage

### Basic file cache

Using the memory cache is easy — just initialize it and upsert some values:

```csharp
using NTDLS.FastMemoryCache;

// Create a cache instance (defaults: no size limit, keys case sensitive).
PartitionedMemoryCache _cache = new();

public string ReadFileFromDisk(string path)
{
    // Try to read from cache first.
    if (_cache.TryGet<string>(path, out var cachedObject))
    {
        return cachedObject;
    }

    // Cache miss — read from disk and store the result.
    // The third argument is the approximate size in bytes, so the cache
    // does not have to estimate it (useful when EstimateObjectSize is off).
    string fileContents = File.ReadAllText(path);
    _cache.Upsert(path, fileContents, fileContents.Length * sizeof(char));
    return fileContents;
}
```

### Custom configuration

Pass a `PartitionedCacheConfiguration` to control partition count, maximum size, key casing, size estimation and expiration scanning:

```csharp
using NTDLS.FastMemoryCache;

var configuration = new PartitionedCacheConfiguration
{
    PartitionCount = 16,                       // number of cache partitions
    SizeLimitBytes = 256L * 1024 * 1024,       // max 256 MB across all partitions (0 = no limit)
    IsCaseSensitive = false,                   // treat keys as case insensitive
    EstimateObjectSize = true,                 // estimate object sizes when not supplied
    CompactionPercentage = 0.10,               // compact by 10% when the size limit is exceeded
    ExpirationScanFrequency = TimeSpan.FromMinutes(5)
};

using PartitionedMemoryCache cache = new(configuration);
```

### Storing values with a time-to-live

The full `Upsert` overload accepts an optional time-to-live (the item expires after that amount of time from insertion, update or last read):

```csharp
// Store a value that lives in the cache for 30 minutes.
cache.Upsert("user:42", user, null, TimeSpan.FromMinutes(30));

// Retrieve it later (throws if not found).
User? user = cache.Get<User>("user:42");
```

### Removing items and checking metrics

```csharp
// Remove a single item by key.
cache.Remove("user:42");

// Remove every item whose key starts with "user:" — returns the number removed.
int removed = cache.RemoveItemsWithPrefix("user:");

// Built-in performance metrics.
long hits   = cache.TotalHits();
long misses = cache.TotalMisses();
long items  = cache.Count();
```

When the cache is no longer needed, dispose it (it implements `IDisposable`), which disposes all of its partitions.

## License

[MIT](https://choosealicense.com/licenses/mit/)
