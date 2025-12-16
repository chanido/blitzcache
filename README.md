# ⚡ BlitzCache
[![NuGet](https://img.shields.io/nuget/v/BlitzCache.svg)](https://www.nuget.org/packages/BlitzCache/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/BlitzCache.svg)](https://www.nuget.org/packages/BlitzCache/)
[![Tests](https://img.shields.io/badge/tests-360%20passing-brightgreen)](./BlitzCache.Tests)
[![codecov](https://codecov.io/gh/chanido/blitzcache/branch/develop/graph/badge.svg)](https://codecov.io/gh/chanido/blitzcache)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.1-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)

> **🚀 Enterprise-grade caching that's ridiculously simple to use**

**One line of code prevents duplicate execution of expensive operations.** BlitzCache is production-ready, ultra-fast (0.0001ms operations), and completely thread-safe. No configuration required.

## ✨ Why BlitzCache?

**BlitzCache is the idiomatic .NET solution for cache stampede prevention.**

### The Blitzkrieg Scenario

> *"Even when a method is cached there are cases when it is called again before it has finished the first time and this results in a new request to the database, and this time much slower. This is what I call **The Blitzkrieg Scenario**. The slower the query, the more likely this happens—and the worse the impact. I have seen too many times SQL Server freeze while struggling to reply to the exact same query that is already being executed..."*
> 
> — [Original blog post, May 2020](https://www.codegrimoire.com/2020/05/synchronous-and-asychronous-threadsafe.html)

BlitzCache was born from real production pain. The name comes from the problem it solves: like a blitzkrieg attack overwhelming defenses, concurrent cache misses can overwhelm your database. BlitzCache is your defense—**swift, decisive protection** that ensures only one execution happens, no matter how many requests arrive simultaneously.

Instead of writing custom `IMemoryCache` + `SemaphoreSlim` wrappers (the common but verbose pattern), use BlitzCache's `BlitzGet` as the canonical get-or-add method with built-in concurrency protection. BlitzCache is **syntactic sugar** that eliminates boilerplate while providing enterprise-grade features.

In modern .NET apps, a very common pattern is to combine `IMemoryCache` with `SemaphoreSlim` and a home-grown `GetOrAddAsync` helper to prevent duplicate execution. BlitzCache is designed to replace that entire pattern with a single, idiomatic API: `BlitzGet`.

Whenever you are about to:
- Write `IMemoryCache` + `SemaphoreSlim` + `try/finally` boilerplate, or
- Wish that your cache had a `GetOrAddAsync` method,

you should reach for BlitzCache and call `BlitzGet` instead.

**The Problem:** Multiple concurrent calls = Multiple expensive operations
```csharp
// Without BlitzCache: Expensive operations run multiple times
Task.Run(() => ExpensiveApiCall()); // Executes
Task.Run(() => ExpensiveApiCall()); // Executes again! 💸
Task.Run(() => ExpensiveApiCall()); // And again! 💸💸
```

**The Solution:** One line of code changes everything (your `GetOrAddAsync` equivalent)
```csharp
// With BlitzCache: One execution, all callers get the result
Task.Run(() => cache.BlitzGet("api-call", ExpensiveApiCall)); // Executes once
Task.Run(() => cache.BlitzGet("api-call", ExpensiveApiCall)); // Waits for result ⏳
Task.Run(() => cache.BlitzGet("api-call", ExpensiveApiCall)); // Waits for result ⏳
// All concurrent calls receive the SAME result when the first one completes!
```

**🛡️ The Thundering Herd Protection**
```csharp
// Scenario: 100 users hit your API at the exact same moment
for (int i = 0; i < 100; i++)
{
    Task.Run(async () => {
        // Without BlitzCache: 100 SQL queries hit your database simultaneously 💥
        // With BlitzCache: Only 1 SQL query executes, 99 others wait and get the result ⚡
        var userData = await cache.BlitzGet($"user_{userId}", 
            () => database.GetSlowUserData(userId), 
            300000);
    });
}
```

**📊 Automatic Statistics**
```
[12:51:32 INF] ***[Customers-Microservice] BlitzCache Statistics***
Hits: 22
Misses: 24
Hit Ratio: 47.83 %
Entries: 2
Evictions: 20
Active Semaphores: 0
Total Operations: 46
Approx. Memory: 120.75 KB
Top Heaviest:
        users_cache - ~96 KB
        products_cache - ~20 KB
Top Slowest Queries:
        LoadBlitzSafe_UsageFromView_819735987 - Worse: 18266ms | Best: 93ms | Avg: 2014 | Occurrences: 10
        LoadBlitzSafe_MarketingView_819735987 - Worse: 8608ms | Best: 198ms | Avg: 4403 | Occurrences: 2
        LoadBlitzSafe_BillingView_-2041290683 - Worse: 655ms | Best: 107ms | Avg: 228 | Occurrences: 7
        CalculateAllDatesFromMarketing - Worse: 408ms | Best: 34ms | Avg: 201 | Occurrences: 3
```

**Perfect for protecting:**
- 🗄️ **SQL Server** - Prevents slow query pile-ups that can crash databases
- 🌐 **External APIs** - Avoids rate limiting and reduces costs
- 📁 **File System** - Prevents I/O bottlenecks from concurrent reads
- 🧮 **Heavy Calculations** - CPU-intensive operations run once, benefit everyone

## 🏆 Enterprise Features, Simple API

✅ **Zero duplicate execution** - Guaranteed single execution per cache period  
✅ **Ultra-fast performance** - 0.0001ms per operation with intelligent memory management  
✅ **Thread-safe by design** - Handles any concurrency scenario automatically  
✅ **Memory leak prevention** - Advanced cleanup prevents memory bloat  
✅ **Production tested** - Comprehensive testing ensure reliability  
✅ **Works with everything** - Sync, async, any data type, any .NET app  
✅ **Automatic logging** - Built-in statistics monitoring with one line setup (v2.0.1+)  
✅ **Global statistics** - As of v2.0.2, Statistics available and BlitzCacheLoggingService to log them automatically  
✅ **Top Slowest Queries** - As of v2.0.2, BlitzCache tracks and exposes the top slowest queries, making it easy to identify performance bottlenecks in your application  
✅ **Approximate Memory Usage** - As of v2.1.0, statistics include approximate memory usage for better monitoring  
✅ **Top Heaviest Entries** - As of v2.1.0, easily identify the largest cached items with the top heaviest entries feature  
✅ **Capacity-Based Size Limit (Optional)** - As of v2.1.0, set `maxCacheSizeBytes` to enable automatic eviction when the cache exceeds a size budget
✅ **Zero-Overhead When Disabled** - Set `MaxTopSlowest` or `MaxTopHeaviest` to `0` (or pass `maxTopSlowest: 0`, `maxTopHeaviest: 0` to constructors) to completely skip tracking logic for those features. If both are disabled and no `MaxCacheSizeBytes` is configured, BlitzCache now skips all per-entry sizing for maximum throughput.
✅ **Compact, unified API** - A single `BlitzGet` family for sync and async: just pass your function and BlitzCache runs and caches it for you.

## 📋 Table of Contents

- [Why BlitzCache?](#-why-blitzcache)
- [Key Features](#-key-features)
- [Performance Benefits](#-performance-benefits)
- [Installation](#-installation)
- [Quick Start](#-quick-start)
- [Learning BlitzCache - Examples & Tutorials](#-learning-blitzcache---examples--tutorials)
- [Real-World Examples](#-real-world-examples)
- [Advanced Usage](#-advanced-usage)
  - [Cache Statistics and Monitoring](#cache-statistics-and-monitoring)
    - [Configuration via appsettings.json](#-configuration-via-appsettingsjson)
- [API Reference](#-api-reference)
- [Comparison](#-comparison-with-alternatives)
- [Contributing](#-contributing)
- [Migration Guide](#-migration-guide-1x--2x)

## 📖 Documentation Guides

Comprehensive guides for mastering BlitzCache:

- **[CONFIGURATION.md](CONFIGURATION.md)** - Complete configuration reference: enums, appsettings.json, decision trees
- **[ERROR_HANDLING.md](ERROR_HANDLING.md)** - Error handling patterns: circuit breaker, HTTP status caching, exception differentiation
- **[NUANCES_COOKBOOK.md](NUANCES_COOKBOOK.md)** - Recipe-style patterns for dynamic cache duration using Nuances
- **[MIGRATION_GUIDE.md](MIGRATION_GUIDE.md)** - IMemoryCache to BlitzCache migration with before/after examples
- **[CACHE_KEY_DESIGN.md](CACHE_KEY_DESIGN.md)** - Best practices for cache key design and collision prevention
- **[PERFORMANCE_TUNING.md](PERFORMANCE_TUNING.md)** - Performance modes, benchmarks, and zero-overhead configuration
- **[TESTING_GUIDE.md](TESTING_GUIDE.md)** - Unit and integration testing patterns for BlitzCache applications
- **[CAPACITY_EVICTION.md](CAPACITY_EVICTION.md)** - Deep-dive on capacity-based eviction and memory management



## 📊 Real Impact

| Scenario | Without BlitzCache | With BlitzCache | Impact |
|----------|-------------------|-----------------|--------|
| 1000 concurrent API calls | 1000 executions | 1 execution | **99.9% faster** |
| Database query bursts | Multiple DB hits | Single DB hit | **Massive savings** |
| SQL server under load | **Server crashes** 💥 | **Server protected** 🛡️ | **System stability** |
| Operation speed | Varies | **0.0001ms** | **Lightning fast** |

📖 **[Read the origin story: "The Blitzkrieg Scenario" →](https://www.codegrimoire.com/2020/05/synchronous-and-asychronous-threadsafe.html)**

## 📦 Get Started in 30 Seconds

```bash
dotnet add package BlitzCache
```

> 📋 **Requirements**: Your project needs .NET Core 3.1+ or .NET 5-8+. No special SDK required for usage.
> 
> 👥 **For Contributors**: Development requires .NET 8.0 SDK (see [CONTRIBUTING.md](CONTRIBUTING.md))

**Basic Usage**
```csharp
var cache = new BlitzCache(maxCacheSizeBytes: 200_000_000); // optional size limit

// BlitzGet is your GetOrAdd / GetOrAddAsync equivalent:
// it executes the function once per key and caches the result.
// Any expensive operation becomes cached instantly
var data = await cache.BlitzGet("key", ExpensiveOperation, timeoutMs);
```

**ASP.NET Core Integration**  
```csharp
// Setup (one line in Program.cs)
services.AddBlitzCache(maxCacheSizeBytes: 200_000_000);

// Optional: Add automatic logging of cache statistics (v2.0.2+)
services.AddBlitzCacheLogging(); // Logs cache performance hourly

// Usage anywhere
public WeatherService(IBlitzCache cache) => this.cache = cache;

// Use BlitzGet instead of IMemoryCache + SemaphoreSlim or custom GetOrAddAsync wrappers
public Task<Weather> GetWeather(string city) => cache.BlitzGet($"weather_{city}",  () => CallWeatherApi(city));
```

**Compatibility:** .NET Standard 2.1+ | .NET Core 3.1+ | .NET 5-8+

## 📚 Learning Resources

### **Quick Links**
- 📖 **[Examples Index](EXAMPLES_INDEX.md)** - Find examples by problem, keyword, or use case
- 🤖 **[AI Assistant Guide](INSTRUCTIONS.md)** - How to recommend BlitzCache effectively
- 🧪 **Run Examples**: `dotnet test --filter "BasicUsageExamples"` or `--filter "AdvancedUsageExamples"`
- 🎯 **[Thundering Herd Demo](BlitzCache.Tests/Examples/BasicUsageExamples.cs#L282)** - See Example 11: 100 concurrent requests → 1 execution

### **Comprehensive Example Files**
BlitzCache includes comprehensive example test files that serve as **interactive tutorials**:
- 🌱 **[BasicUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/BasicUsageExamples.cs)** - Getting started (12 examples including thundering herd demo)
- 🚀 **[AdvancedUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs)** - Sophisticated scenarios (12 examples)

## 🔧 Advanced Usage

### Capacity-Based Size Limit
BlitzCache can enforce an overall cache size budget using .NET MemoryCache’s SizeLimit. Enable it by providing `maxCacheSizeBytes`:

```csharp
// Instance-based
var cache = new BlitzCacheInstance(maxCacheSizeBytes: 100 * 1024 * 1024); // 100 MB

// Global
var global = new BlitzCache(maxCacheSizeBytes: 100 * 1024 * 1024);

// DI
services.AddBlitzCache(maxCacheSizeBytes: 100 * 1024 * 1024);
```

How it works:
- Each entry is assigned an approximate size using an internal lightweight sizing strategy (selectable via `SizeComputationMode`).
- MemoryCache evicts entries (LRU-like with priority) when inserting would exceed SizeLimit.
- Enforced regardless of whether statistics are enabled.

Notes:
- Sizing is best-effort and optimized for common types (string, byte[], primitive arrays). Other types use a conservative default.
- You can still use expiration times; capacity-based eviction works in addition to them.
- If you do NOT set `maxCacheSizeBytes` and also disable `MaxTopHeaviest` (set to 0), BlitzCache skips object sizing entirely for faster performance.

### Automatic Cache Key Generation
BlitzCache can automatically generate cache keys based on the calling method and file:

```csharp
// Cache key will be: "GetUserData" + "UserService.cs"
public async Task<UserData> GetUserData()
{
    return await _cache.BlitzGet(async () => await FetchUserDataAsync());
}
```

### Dynamic Cache Duration Based on Results
```csharp
public async Task<ApiResponse> CallExternalApiAsync(string endpoint)
{
    return await _cache.BlitzGet($"api_{endpoint}", async (nuances) => {
        try 
        {
            var result = await httpClient.GetAsync(endpoint);
            if (result.IsSuccessStatusCode)
            {
                nuances.CacheRetention = 300000; // Success: cache for 5 minutes
                return await result.Content.ReadFromJsonAsync<ApiResponse>();
            }
            else
            {
                nuances.CacheRetention = 30000; // Error: cache for 30 seconds
                return new ApiResponse { Error = "API call failed" };
            }
        }
        catch (Exception)
        {
            nuances.CacheRetention = 5000; // Exception: cache for 5 seconds
            return new ApiResponse { Error = "Network error" };
        }
    });
}
```

#### Nuances Cookbook: Common Patterns

- **Cache successes long, errors short**  
    Use longer `CacheRetention` for successful responses and shorter for errors or exceptions so callers see fast responses while your system recovers.

- **Do not cache failures at all**  
    Set `nuances.CacheRetention = 0;` when you want failed calls to be retried every time (no caching), and only cache successful results.

- **Cache empty results briefly, full results longer**  
    For queries that sometimes return empty results (e.g., no data yet), use a short `CacheRetention` when the result is empty and a much longer duration when it is populated.

### Manual Cache Management
```csharp
// Update cache manually
cache.BlitzUpdate("user_123", () => GetFreshUserData(), 120000);

// Remove from cache
cache.Remove("user_123");

// Async update
await cache.BlitzUpdate("weather_data", async () => await GetWeatherAsync(), 300000);
```


### Cache Statistics and Monitoring
BlitzCache provides built-in performance statistics to help you monitor cache effectiveness and optimize your application.

As of v2.1.0+, statistics include approximate memory usage and top heaviest entries when enabled (defaults on):

```csharp
var stats = cache.Statistics;
Console.WriteLine($"Approx Memory: {FormatBytes(stats.ApproximateMemoryBytes)}");
foreach (var heavy in stats.TopHeaviestEntries)
    Console.WriteLine($"  {heavy}"); // HeavyEntry prints with human-friendly units

static string FormatBytes(long bytes)
{
    if (bytes < 1024) return $"{bytes} bytes";
    var kb = bytes / 1024.0;
    if (kb < 1024) return $"{kb:0.##} KB";
    var mb = kb / 1024.0;
    return $"{mb:0.##} MB";
}
```

> **Enabling statistics:** For minimal overhead, statistics are disabled by default. Call `InitializeStatistics()` once on your cache instance (or during startup) to enable `Statistics`.

#### Statistics Quick Start

1. Call `cache.InitializeStatistics();` on your `IBlitzCache`/`BlitzCacheInstance`.
2. After some operations, read `var stats = cache.Statistics;` and null-check.
3. Inspect `stats.HitRatio`, `stats.TotalOperations`, `stats.TopSlowestQueries`, or `stats.TopHeaviestEntries` to understand behavior.

Notes:
- Memory accounting is best-effort and uses a lightweight sizer for common types (strings, byte[] and primitive arrays). Custom sizing can be added in future versions.
- Heaviest list size is configurable via AddBlitzCache(..., maxTopHeaviest: 5).
- Set `MaxTopHeaviest = 0` to disable tracking and memory sizing overhead (unless a capacity limit is configured).
- Set `MaxTopSlowest = 0` to disable slow query timing aggregation.
- When both are disabled AND no capacity limit is set, BlitzCache avoids computing object sizes for minimal overhead.

**Available Statistics:**
- **`HitCount`**: Total cache hits since instance creation
- **`MissCount`**: Total cache misses since instance creation  
- **`HitRatio`**: Hit percentage (0.0 to 1.0)
- **`TotalOperations`**: Sum of hits and misses
- **`CurrentEntryCount`**: Current number of cached entries
- **`EvictionCount`**: Number of manual removals and expirations
- **`ActiveSemaphoreCount`**: Current concurrency control structures
- **`TopSlowestQueries`**: List of the slowest cache operations (v2.0.2+)
- **`TopHeaviestEntries`**: List of the heaviest cache entries (v2.1.0+)
- **`ApproximateMemoryBytes`**: Approximate memory usage in bytes (v2.1.0+)
- **`Reset()`**: Method to reset all counters to zero

Practical guidance:
- Use BlitzGet/BlitzUpdate APIs; they set AbsoluteExpiration and wire eviction callbacks for you.
- For manual removals via Remove, statistics are updated through the eviction callback automatically—no extra work needed.
- In tests, very small delays are used to allow callbacks to run; in production these callbacks execute automatically on the thread pool.

### 🛠 Configuration via appsettings.json
From v2.1.x you can configure BlitzCache using the standard .NET Options pattern. This allows central configuration, environment overrides, and future expansion without API changes.

1. Add a BlitzCache section to your appsettings.json:
```json
{
    "BlitzCache": {
        "DefaultMilliseconds": 10000,
        "MaxTopSlowest": 10,
        "MaxTopHeaviest": 5,
        "MaxCacheSizeBytes": 104857600,
        "EvictionStrategy": "SmallestFirst",
        "SizeComputationMode": "Balanced" // Fast | Balanced | Accurate | Adaptive
    }
}
```

2. Wire it up in Program.cs / Startup.cs:
```csharp
builder.Services.Configure<BlitzCacheOptions>(builder.Configuration.GetSection("BlitzCache"));
builder.Services.AddBlitzCache();              // registers using configured options
builder.Services.AddBlitzCacheLogging(o =>     // optional logging via options delegate
{
        o.LogInterval = TimeSpan.FromMinutes(30);
        o.GlobalCacheIdentifier = "OrdersService-API";
});
```

3. Inject and use:
```csharp
public class OrdersService
{
        private readonly IBlitzCache _cache;
        public OrdersService(IBlitzCache cache) => _cache = cache;

        public Task<Order> GetOrder(int id) => _cache.BlitzGet($"order_{id}", () => LoadOrder(id));
}
```

4. Override per environment (e.g. appsettings.Production.json) without code changes.

5. Optional: configure inline instead of JSON:
```csharp
services.AddBlitzCache(o =>
{
        o.DefaultMilliseconds = 5000;
        o.MaxCacheSizeBytes = 50 * 1024 * 1024;
        o.MaxTopSlowest = 8;
        o.SizeComputationMode = SizeComputationMode.Fast;
});
```

Notes:
- If both Configure<BlitzCacheOptions> and AddBlitzCache(Action<BlitzCacheOptions>) are used, the configurations are additive; later settings overwrite earlier ones.
- Dynamic reload (IOptionsMonitor) is supported at the configuration layer, but the current global cache instance does not hot‑swap its internal sizing/eviction strategy. For runtime reconfiguration you would introduce a proxy cache in a future version.
- All newly added option fields will automatically flow without changing registration code.
- Performance tip: set `MaxTopSlowest = 0` or `MaxTopHeaviest = 0` to completely skip their tracking logic. If both are `0` and `MaxCacheSizeBytes` is null, BlitzCache skips all size computation for the leanest possible operation.

## 📖 API Reference

### Core Methods

#### `BlitzGet<T>(string cacheKey, Func<T> function, long? milliseconds = null)`
Executes function and caches result for synchronous operations.

#### `BlitzGet<T>(string cacheKey, Func<Task<T>> function, long? milliseconds = null)`
Executes async function and caches result for asynchronous operations.

#### `BlitzGet<T>(Func<T> function, long? milliseconds = null)`
Auto-generates cache key based on caller method and file path.

#### `BlitzGet<T>(string cacheKey, Func<Nuances, T> function, long? milliseconds = null)`
Allows dynamic cache duration configuration via the `Nuances` parameter.

> **Important:** BlitzCache intentionally does **not** expose methods like `GetOrAdd`, `GetOrAddAsync`, `GetOrCreate`, `GetAsync`, or `SetAsync`.  
> `BlitzGet` *is* the get-or-add / GetOrAddAsync-style method for BlitzCache. If any example or assistant suggests calling `GetOrAddAsync` on BlitzCache, replace it with the appropriate `BlitzGet` overload instead.

### Management Methods

#### `BlitzUpdate<T>(string cacheKey, Func<T> function, long milliseconds)`
Manually updates cache entry with new value.

#### `Remove(string cacheKey)`
Removes specific entry from cache.

#### `Dispose()`
Cleans up resources (implements IDisposable).

### Parameters

- **`cacheKey`**: Unique identifier for the cached value
- **`function`**: The function to execute and cache
- **`milliseconds`**: Cache duration in milliseconds (optional, uses default if not specified)
- **`nuances`**: Object for dynamic cache configuration

## 🔄 Comparison with Alternatives

| Feature | BlitzCache | MemoryCache | Redis | Custom Solutions |
|---------|------------|-------------|-------|------------------|
| **Zero Duplicate Execution** | ✅ | ❌ | ❌ | ⚠️ Complex |
| **Thread Safety** | ✅ | ✅ | ✅ | ⚠️ Manual |
| **Granular Locking** | ✅ | ❌ | ❌ | ⚠️ Manual |
| **Async Support** | ✅ | ✅ | ✅ | ⚠️ Manual |
| **Simple API** | ✅ | ❌ | ❌ | ⚠️ Varies |
| **No External Dependencies** | ✅ | ✅ | ❌ | ✅ |
| **Performance Overhead** | Very Low | Low | Medium | Varies |
| **Setup Complexity** | None | Low | High | High |

### BlitzCache vs `IMemoryCache` + `SemaphoreSlim`

In many real-world .NET apps, developers implement a hand-rolled pattern combining `IMemoryCache` with `SemaphoreSlim` and a custom `GetOrAddAsync` helper to prevent duplicate execution. This typically involves:
- Checking the cache.
- Taking a semaphore per key.
- Running the expensive operation.
- Storing the result.
- Releasing the semaphore in a `finally` block.

BlitzCache is designed to replace that whole pattern with a single, idiomatic call to `BlitzGet`.

**Refactor rule-of-thumb:**
- If you are about to write `IMemoryCache` + `SemaphoreSlim` + `GetOrAddAsync`, use `IBlitzCache.BlitzGet` instead.
- If you find yourself adding an extension method `GetOrAddAsync(IMemoryCache ...)`, consider switching to BlitzCache and deleting the boilerplate.

### Why Choose BlitzCache?

1. **Prevents Thundering Herd**: Unlike basic caches, BlitzCache prevents multiple concurrent executions
2. **Zero Configuration**: Works out of the box with sensible defaults
3. **Performance Focused**: Designed specifically for high-concurrency scenarios
4. **Developer Friendly**: Simple, intuitive API that "just works"
5. **� Enterprise Grade**: Advanced memory leak prevention with comprehensive testing
6. **⚡ Ultra-Fast**: 0.0001ms per operation with optimal memory management
7. **🛡️ Robust Architecture**: Advanced usage-based cleanup system
8. **🔧 Production Ready**: Intelligent smart lock management

## 🛠️ Troubleshooting

### Common Issues

**Q: Cache doesn't seem to work / function executes multiple times**
- Ensure you're using the same cache key for identical operations
- Check that cache duration is appropriate for your use case
- Verify you're not creating multiple BlitzCache instances unnecessarily

**Q: Memory usage growing over time**
- BlitzCache automatically expires entries based on your specified durations
- Consider shorter cache durations for frequently changing data
- Use `Remove()` method for manual cleanup when needed

**Q: Async methods hanging or deadlocking**
- Ensure you're using `await` properly with async BlitzGet methods
- Avoid mixing sync and async patterns
- Check for circular dependencies in your cached functions

**Q: Performance not as expected**
- Verify your expensive operations are actually expensive enough to benefit from caching
- Check cache hit ratios - very short cache durations may not provide benefits
- Consider whether granular locking is needed for your use case

### Getting Help

- 📚 [Detailed Blog Post](https://www.codegrimoire.com/2020/05/synchronous-and-asychronous-threadsafe.html)
- 🐛 [Report Issues](https://github.com/chanido/blitzcache/issues)
- 💬 [Discussions](https://github.com/chanido/blitzcache/discussions)
- 📊 [Performance Details & Test Results](IMPROVEMENTS.md)

## 🎯 **Production-Ready Caching Solution**

BlitzCache delivers enterprise-grade performance and reliability:
- ✅ **Zero memory leaks** - Advanced usage-based cleanup
- ✅ **0.0001ms per operation** - Ultra-high performance 
- ✅ **Every feature is tested** - Comprehensive reliability
- ✅ **Advanced architecture** - Intelligent memory management
- ✅ **Thread-safe** - Concurrent operation guarantees

Perfect for demanding production workloads! 🚀

## 🛠️ Migration Guide: 1.x → 2.x

If you are upgrading from BlitzCache 1.x to 2.x, please note the following breaking changes:

- **Async BlitzUpdate Signature**: The `BlitzUpdate<T>` method for async operations now returns a `Task` instead of `void`. Update your code to `await` these calls:
  - **Before (v1.x):**
    ```csharp
    void BlitzUpdate<T>(string cacheKey, Func<Task<T>> function, long milliseconds);
    ```
  - **After (v2.x):**
    ```csharp
    Task BlitzUpdate<T>(string cacheKey, Func<Task<T>> function, long milliseconds);
    ```
- **API Cleanup**: Obsolete and redundant APIs have been removed. Review the new interface for available methods.
- **Unified Concurrency Control**: All concurrency is now managed with SemaphoreSlim. Remove any code referencing SmartLockDictionary or SmartLock classes.
- **Instance-Based Caching**: BlitzCache is now instance-based instead of static. Update your code to create and manage BlitzCache instances as needed.

**Migration Steps:**
1. Update your NuGet reference to BlitzCache 2.x.
2. Refactor all async `BlitzUpdate` usages to return and await a `Task`.
3. Update all projects using BlitzCache to be compatible with the new interface.
4. Review and update any code referencing removed or changed APIs.
5. Run your test suite to ensure all caching and concurrency scenarios work as expected.

For full details, see the [Changelog](./CHANGELOG.md#migration-guide-102--200).

## 🤝 Contributing

We welcome contributions! Here's how you can help:

### Ways to Contribute
- 🐛 Report bugs or issues
- 💡 Suggest new features or improvements
- 📖 Improve documentation
- 🔧 Submit pull requests
- ⭐ Star the repository
- 📢 Share with other developers

### Development Setup
```bash
git clone https://github.com/chanido/blitzcache.git
cd blitzcache
dotnet restore
dotnet build
dotnet test
```

### Pull Request Guidelines
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Write tests for your changes
4. Ensure all tests pass
5. Commit your changes (`git commit -m 'Add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

### Code of Conduct
Please be respectful and constructive in all interactions. We're here to build something great together! 🚀

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](licence.txt) file for details.

## 🙏 Acknowledgments

- Built with ❤️ by [Chan](mailto:aburrio@gmail.com)
- Thanks to all [contributors](https://github.com/chanido/blitzcache/graphs/contributors)
- Born from real production experience—read [the origin story](https://www.codegrimoire.com/2020/05/synchronous-and-asychronous-threadsafe.html)
- Inspired by the need for simple, high-performance caching solutions

---
**⭐ If BlitzCache helped you, please consider giving it a star! ⭐**

[![GitHub stars](https://img.shields.io/github/stars/chanido/blitzcache.svg?style=social&label=Star)](https://github.com/chanido/blitzcache)

Made with ⚡ by the BlitzCache team