# ⚡ BlitzCache

<div align="center">
  <img src="BlitzCache.png" alt="BlitzCache" width="300"/>
</div>

[![NuGet](https://img.shields.io/nuget/v/BlitzCache.svg)](https://www.nuget.org/packages/BlitzCache/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/BlitzCache.svg)](https://www.nuget.org/packages/BlitzCache/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Standard](https://img.shields.io/badge/.NET%20Standard-2.1-blue.svg)](https://docs.microsoft.com/en-us/dotnet/standard/net-standard)
[![Build Status](https://img.shields.io/badge/tests-44%20passing-brightgreen.svg)](https://github.com/chanido/blitzcache)

> **🚀 Enterprise-grade caching that's ridiculously simple to use**

**One line of code prevents duplicate execution of expensive operations.** BlitzCache is production-ready, ultra-fast (0.03ms operations), and completely thread-safe. No configuration required.

## ✨ Why BlitzCache?

**The Problem:** Multiple concurrent calls = Multiple expensive operations
```csharp
// Without BlitzCache: Expensive operations run multiple times
Task.Run(() => ExpensiveApiCall()); // Executes
Task.Run(() => ExpensiveApiCall()); // Executes again! 💸
Task.Run(() => ExpensiveApiCall()); // And again! 💸💸
```

**The Solution:** One line of code changes everything
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

**Perfect for protecting:**
- 🗄️ **SQL Server** - Prevents slow query pile-ups that can crash databases
- 🌐 **External APIs** - Avoids rate limiting and reduces costs
- 📁 **File System** - Prevents I/O bottlenecks from concurrent reads
- 🧮 **Heavy Calculations** - CPU-intensive operations run once, benefit everyone

## 🏆 Enterprise Features, Simple API

✅ **Zero duplicate execution** - Guaranteed single execution per cache period  
✅ **Ultra-fast performance** - 0.03ms per operation with intelligent memory management  
✅ **Thread-safe by design** - Handles any concurrency scenario automatically  
✅ **Memory leak prevention** - Advanced cleanup prevents memory bloat  
✅ **Production tested** - 44 comprehensive tests ensure reliability  
✅ **Works with everything** - Sync, async, any data type, any .NET app

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
- [API Reference](#-api-reference)
- [Comparison](#-comparison-with-alternatives)
- [Contributing](#-contributing)

## 🎯 Why BlitzCache?

### The Problem
```csharp
// Without BlitzCache: Multiple concurrent calls = Multiple executions
Task.Run(() => ExpensiveApiCall()); // Executes
Task.Run(() => ExpensiveApiCall()); // Executes again! 💸
Task.Run(() => ExpensiveApiCall()); // And again! 💸💸
```

### The Solution
```csharp
// With BlitzCache: Multiple concurrent calls = Single execution
Task.Run(() => cache.BlitzGet("api-call", ExpensiveApiCall)); // Executes once
Task.Run(() => cache.BlitzGet("api-call", ExpensiveApiCall)); // Waits for first to complete ⏳
Task.Run(() => cache.BlitzGet("api-call", ExpensiveApiCall)); // Waits for first to complete ⏳
// Result: ALL callers get the same result from the single execution!
```

**🔒 Intelligent Execution Control:** While the first call executes, subsequent calls **wait** for the result instead of executing again. This prevents database overload, API rate limiting, and resource exhaustion.

## 📊 Real Impact

| Scenario | Without BlitzCache | With BlitzCache | Impact |
|----------|-------------------|-----------------|--------|
| 1000 concurrent API calls | 1000 executions | 1 execution | **99.9% faster** |
| Database query bursts | Multiple DB hits | Single DB hit | **Massive savings** |
| SQL server under load | **Server crashes** 💥 | **Server protected** 🛡️ | **System stability** |
| Operation speed | Varies | **0.03ms** | **Lightning fast** |

[Detailed benchmarks and analysis →](http://www.codegrimoire.com/2020/05/synchronous-and-asychronous-threadsafe.html)

## 📦 Get Started in 30 Seconds

```bash
dotnet add package BlitzCache
```

**Basic Usage**
```csharp
var cache = new BlitzCache();

// Any expensive operation becomes cached instantly
var data = await cache.BlitzGet("key", ExpensiveOperation, timeoutMs);
```

**ASP.NET Core Integration**  
```csharp
// Setup (one line in Program.cs)
services.AddBlitzCache();

// Usage anywhere
public WeatherService(IBlitzCache cache) => _cache = cache;

public async Task<Weather> GetWeather(string city) =>
    await _cache.BlitzGet($"weather_{city}", 
        () => CallWeatherApi(city), 
        TimeSpan.FromMinutes(5).TotalMilliseconds);
```

**Compatibility:** .NET Standard 2.1+ | .NET Core 3.1+ | .NET 5-8+
## 📚 Learning BlitzCache - Examples & Tutorials

### **Comprehensive Example Files**
BlitzCache includes comprehensive example test files that serve as **interactive tutorials** and **real-world usage guides**:

#### 🌱 **[BasicUsageExamples.cs](BlitzCache.Tests/Examples/BasicUsageExamples.cs)**
Perfect for **getting started** - covers essential patterns:
- ✅ **Basic synchronous caching** - Simple function caching
- ✅ **Asynchronous operations** - Async/await patterns  
- ✅ **Cache key management** - Working with different keys
- ✅ **Cache expiration** - Understanding timeout behavior
- ✅ **Manual cache removal** - Cache invalidation strategies
- ✅ **BlitzUpdate usage** - Pre-populating cache
- ✅ **Different data types** - Caching various objects
- ✅ **Cache statistics monitoring** - Performance analytics and hit/miss tracking
- ✅ **Dependency injection** - ASP.NET Core integration

#### 🚀 **[AdvancedUsageExamples.cs](BlitzCache.Tests/Examples/AdvancedUsageExamples.cs)**
For **experienced users** - sophisticated scenarios:
- ✅ **Dynamic cache timeout with Nuances** - Result-based cache duration
- ✅ **Thread-safe concurrent access** - Multi-threading patterns
- ✅ **Circuit breaker pattern** - Resilient external service calls
- ✅ **Multi-level caching strategy** - Complex caching hierarchies
- ✅ **Cache warming techniques** - Pre-loading strategies
- ✅ **Conditional caching** - Success/failure caching logic
- ✅ **Global vs Independent caches** - Instance management
- ✅ **Performance monitoring** - Metrics and diagnostics

### **Running the Examples**
```bash
# Run basic examples
dotnet test --filter "BasicUsageExamples"

# Run advanced examples  
dotnet test --filter "AdvancedUsageExamples"

# Run specific example
dotnet test --filter "Example1_BasicSyncCaching"
```

These example files are **executable tests** that demonstrate real-world usage patterns and serve as **living documentation** that stays up-to-date with the codebase.

## 🌟 Real-World Examples

### Database Operations
```csharp
public class UserRepository
{
    private readonly IBlitzCache _cache;
    
    public async Task<User> GetUserAsync(int userId)
    {
        return await _cache.BlitzGet($"user_{userId}", 
            async () => await database.Users.FindAsync(userId), 
            120000); // Cache for 2 minutes
    }
    
    // Multiple concurrent calls to GetUserAsync(123) will result in only ONE database query
}
```

### HTTP API Calls
```csharp
public class ExchangeRateService
{
    public async Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency)
    {
        return await _cache.BlitzGet($"rate_{fromCurrency}_{toCurrency}",
            async () => {
                var response = await httpClient.GetAsync($"api/rates/{fromCurrency}/{toCurrency}");
                return await response.Content.ReadFromJsonAsync<decimal>();
            }, 
            600000); // Cache for 10 minutes
    }
}
```

### File System Operations
```csharp
public class ConfigurationService
{
    public async Task<AppConfig> LoadConfigAsync()
    {
        return await _cache.BlitzGet("app-config",
            async () => {
                var json = await File.ReadAllTextAsync("appsettings.json");
                return JsonSerializer.Deserialize<AppConfig>(json);
            },
            1800000); // Cache for 30 minutes
    }
}
```

### Complex Calculations
```csharp
public class ReportService
{
    public async Task<SalesReport> GenerateMonthlyReportAsync(int year, int month)
    {
        return await _cache.BlitzGet($"sales_report_{year}_{month}",
            async () => {
                // This expensive calculation will only run once
                var salesData = await CalculateComplexSalesMetricsAsync(year, month);
                var report = await GenerateChartsAndGraphsAsync(salesData);
                return report;
            },
            3600000); // Cache for 1 hour
    }
}
```

## 🔧 Advanced Usage

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
BlitzCache provides built-in performance statistics to help you monitor cache effectiveness and optimize your application:

```csharp
// Access cache statistics
var stats = cache.Statistics;

Console.WriteLine($"Cache Hit Ratio: {stats.HitRatio:P1}"); // e.g., "75.5%"
Console.WriteLine($"Total Operations: {stats.TotalOperations}");
Console.WriteLine($"Cache Hits: {stats.HitCount}");
Console.WriteLine($"Cache Misses: {stats.MissCount}");
Console.WriteLine($"Current Entries: {stats.CurrentEntryCount}");
Console.WriteLine($"Evictions: {stats.EvictionCount}");
Console.WriteLine($"Active Semaphores: {stats.ActiveSemaphoreCount}");
```

#### Real-World Monitoring Example
```csharp
public class UserService
{
    private readonly IBlitzCache _cache;
    
    public UserService(IBlitzCache cache)
    {
        _cache = cache;
    }
    
    public async Task<UserProfile> GetUserProfileAsync(int userId)
    {
        // Cache the expensive database operation
        var profile = await _cache.BlitzGet($"user_profile_{userId}", 
            async () => await database.GetUserProfileAsync(userId), 
            300000); // 5 minutes
            
        // Log cache performance periodically
        var stats = _cache.Statistics;
        if (stats.TotalOperations % 100 == 0) // Every 100 operations
        {
            _logger.LogInformation("Cache performance: {HitRatio:P1} hit ratio, {CurrentEntries} entries", 
                stats.HitRatio, stats.CurrentEntryCount);
        }
        
        return profile;
    }
}
```

#### Statistics Reset for Time-Windowed Monitoring
```csharp
// Reset statistics to monitor performance over specific periods
cache.Statistics.Reset();

// Perform operations...
DoSomeWork();

// Check performance for this period only
var periodStats = cache.Statistics;
Console.WriteLine($"Period hit ratio: {periodStats.HitRatio:P1}");
```

**Available Statistics:**
- **`HitCount`**: Total cache hits since instance creation
- **`MissCount`**: Total cache misses since instance creation  
- **`HitRatio`**: Hit percentage (0.0 to 1.0)
- **`TotalOperations`**: Sum of hits and misses
- **`CurrentEntryCount`**: Current number of cached entries
- **`EvictionCount`**: Number of manual removals and expirations
- **`ActiveSemaphoreCount`**: Current concurrency control structures
- **`Reset()`**: Method to reset all counters to zero

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

### Why Choose BlitzCache?

1. **Prevents Thundering Herd**: Unlike basic caches, BlitzCache prevents multiple concurrent executions
2. **Zero Configuration**: Works out of the box with sensible defaults
3. **Performance Focused**: Designed specifically for high-concurrency scenarios
4. **Developer Friendly**: Simple, intuitive API that "just works"
5. **� Enterprise Grade**: Advanced memory leak prevention with comprehensive testing
6. **⚡ Ultra-Fast**: 0.03ms per operation with optimal memory management
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

- 📚 [Detailed Blog Post](http://www.codegrimoire.com/2020/05/synchronous-and-asychronous-threadsafe.html)
- 🐛 [Report Issues](https://github.com/chanido/blitzcache/issues)
- 💬 [Discussions](https://github.com/chanido/blitzcache/discussions)
- 📊 [Performance Details & Test Results](IMPROVEMENTS.md)

## 🎯 **Production-Ready Caching Solution**

BlitzCache delivers enterprise-grade performance and reliability:
- ✅ **Zero memory leaks** - Advanced usage-based cleanup
- ✅ **0.03ms per operation** - Ultra-high performance 
- ✅ **44 tests passing** - Comprehensive reliability
- ✅ **Advanced architecture** - Intelligent memory management
- ✅ **Thread-safe** - Concurrent operation guarantees

Perfect for demanding production workloads! 🚀

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
- Inspired by the need for simple, high-performance caching solutions

---

<div align="center">

**⭐ If BlitzCache helped you, please consider giving it a star! ⭐**

[![GitHub stars](https://img.shields.io/github/stars/chanido/blitzcache.svg?style=social&label=Star)](https://github.com/chanido/blitzcache)

Made with ⚡ by the BlitzCache team

</div>
