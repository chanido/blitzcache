# BlitzCache Configuration Guide

> **Complete reference for configuring BlitzCache with all available options**

## Table of Contents
- [Quick Start](#quick-start)
- [Constructor-Based Configuration](#constructor-based-configuration)
- [Options Pattern Configuration](#options-pattern-configuration)
- [Configuration Properties](#configuration-properties)
- [SizeComputationMode Options](#sizecomputationmode-options)
- [CapacityEvictionStrategy Options](#capacityevictionstrategy-options)
- [Configuration Examples](#configuration-examples)
- [appsettings.json Examples](#appsettingsjson-examples)

---

## Quick Start

### Minimal Configuration (Defaults)
```csharp
// Uses all defaults: 60-second cache, statistics enabled, balanced sizing
var cache = new BlitzCacheInstance();
cache.InitializeStatistics();
```

**Tested in:** `CacheStatisticsTests.Statistics_IsNull_Then_NotNull_After_Initialize()`

### Production Configuration
```csharp
var cache = new BlitzCacheInstance(
    defaultMilliseconds: 300000,     // 5 minutes
    maxTopSlowest: 10,               // Track top 10 slow queries
    maxTopHeaviest: 10,              // Track top 10 heavy entries
    maxCacheSizeBytes: 200_000_000   // 200 MB limit
);
cache.InitializeStatistics();
```

**Tested in:** Multiple tests including `MemoryLimitEvictionTests`

---

## Constructor-Based Configuration

### Basic Constructor (All Parameters Optional)
```csharp
public BlitzCacheInstance(
    long? defaultMilliseconds = 60000,           // Default: 1 minute
    TimeSpan? cleanupInterval = null,            // Default: 10 seconds
    int? maxTopSlowest = null,                   // Default: 5
    IValueSizer? valueSizer = null,              // Default: ObjectGraphValueSizer
    int? maxTopHeaviest = 5,                     // Default: 5
    long? maxCacheSizeBytes = null,              // Default: null (unlimited)
    CapacityEvictionStrategy evictionStrategy = CapacityEvictionStrategy.SmallestFirst
)
```

### Recommended Constructor (Options Pattern)
```csharp
public BlitzCacheInstance(BlitzCacheOptions options)
```

**Why use the options pattern?**
- Forward-compatible (new properties won't break your code)
- Supports configuration binding from appsettings.json
- Easier to pass configuration through dependency injection
- Cleaner when using many parameters

---

## Options Pattern Configuration

### Using BlitzCacheOptions Directly
```csharp
var options = new BlitzCacheOptions
{
    DefaultMilliseconds = 300000,
    MaxTopSlowest = 10,
    MaxTopHeaviest = 5,
    MaxCacheSizeBytes = 200_000_000,
    SizeComputationMode = SizeComputationMode.Balanced,
    EvictionStrategy = CapacityEvictionStrategy.SmallestFirst
};

var cache = new BlitzCacheInstance(options);
cache.InitializeStatistics();
```

### Using DI with ASP.NET Core
```csharp
// In Program.cs or Startup.cs
services.Configure<BlitzCacheOptions>(Configuration.GetSection("BlitzCache"));
services.AddBlitzCache(); // Uses configured options

// In your service
public class MyService
{
    private readonly IBlitzCache _cache;
    
    public MyService(IBlitzCache cache)
    {
        _cache = cache;
    }
}
```

**Tested in:** `IServiceCollectionExtensionsTests`

---

## Configuration Properties

### DefaultMilliseconds
- **Type:** `long`
- **Default:** `60000` (1 minute)
- **Description:** Default cache duration in milliseconds when not specified in `BlitzGet()`
- **Range:** Must be positive (> 0)
- **Example:**
  ```csharp
  defaultMilliseconds: 300000  // 5 minutes
  defaultMilliseconds: 3600000 // 1 hour
  ```

### CleanupInterval
- **Type:** `TimeSpan?`
- **Default:** `TimeSpan.FromSeconds(10)`
- **Description:** Interval for automatic cleanup of unused semaphores
- **Note:** Shorter intervals = more frequent cleanup but slightly higher overhead
- **Example:**
  ```csharp
  cleanupInterval: TimeSpan.FromSeconds(30)  // Clean up every 30 seconds
  cleanupInterval: TimeSpan.FromMinutes(1)   // Clean up every minute
  ```

### MaxTopSlowest
- **Type:** `int`
- **Default:** `5`
- **Description:** Maximum number of slowest queries to track in statistics
- **Special Value:** `0` disables slowest query tracking (reduces overhead)
- **Example:**
  ```csharp
  maxTopSlowest: 10  // Track top 10 slowest
  maxTopSlowest: 0   // Disable tracking
  ```

**Tested in:** `CacheStatisticsTests.TopSlowestQueries_EmptyWhenDisabled()`

### MaxTopHeaviest
- **Type:** `int`
- **Default:** `5`
- **Description:** Maximum number of heaviest (largest memory) entries to track
- **Special Value:** `0` disables heaviest entry tracking
- **Note:** Only active when statistics are initialized
- **Example:**
  ```csharp
  maxTopHeaviest: 10  // Track top 10 largest entries
  maxTopHeaviest: 0   // Disable tracking
  ```

**Tested in:** `CacheStatisticsTests.TopHeaviestEntries_TracksLargest_ByApproximateSize()`

### MaxCacheSizeBytes
- **Type:** `long?`
- **Default:** `null` (unlimited)
- **Description:** Maximum total cache size in bytes. When exceeded, automatic eviction occurs.
- **Note:** Requires statistics to be initialized for tracking
- **Triggers:** Capacity-based eviction using the configured `EvictionStrategy`
- **Example:**
  ```csharp
  maxCacheSizeBytes: 100_000_000    // 100 MB
  maxCacheSizeBytes: 500_000_000    // 500 MB
  maxCacheSizeBytes: 1_000_000_000  // 1 GB
  maxCacheSizeBytes: null           // Unlimited (default)
  ```

**Tested in:** `MemoryLimitEvictionTests` and `CapacityEvictionStrategyTests`

### SizeComputationMode
- **Type:** `SizeComputationMode?`
- **Default:** `null` (uses Balanced mode)
- **Description:** Controls how object sizes are computed for memory tracking
- **See:** [SizeComputationMode Options](#sizecomputationmode-options) below

### EvictionStrategy
- **Type:** `CapacityEvictionStrategy`
- **Default:** `CapacityEvictionStrategy.SmallestFirst`
- **Description:** Strategy used when evicting entries due to capacity limits
- **See:** [CapacityEvictionStrategy Options](#capacityevictionstrategy-options) below

---

## SizeComputationMode Options

Determines how BlitzCache estimates object sizes for memory accounting.

### Fast
```csharp
SizeComputationMode = SizeComputationMode.Fast
```

**Characteristics:**
- Minimal overhead
- Uses precomputed shallow sizes from type metadata
- No deep object graph traversal
- Best for high-throughput scenarios where exact size isn't critical

**Use When:**
- Performance is paramount
- Objects have predictable sizes
- Memory tracking is for rough estimation only

**Example:**
```csharp
var options = new BlitzCacheOptions
{
    SizeComputationMode = SizeComputationMode.Fast,
    MaxCacheSizeBytes = 200_000_000
};
var cache = new BlitzCacheInstance(options);
cache.InitializeStatistics();
```

**Tested in:** `ObjectGraphValueSizerModesTests.FastMode_UsesMinimalTraversal()`

---

### Balanced (Default)
```csharp
SizeComputationMode = SizeComputationMode.Balanced  // or omit (default)
```

**Characteristics:**
- Good accuracy for common types
- Moderate object graph traversal (depth: 2 levels)
- Samples collections rather than counting all elements
- Best balance of accuracy vs performance for most applications

**Use When:**
- You need reasonable size estimates
- Typical .NET objects (POCOs, DTOs, collections)
- Production scenarios with memory limits

**Example:**
```csharp
// Balanced is the default, so these are equivalent:
var cache1 = new BlitzCacheInstance(maxCacheSizeBytes: 200_000_000);

var cache2 = new BlitzCacheInstance(new BlitzCacheOptions
{
    SizeComputationMode = SizeComputationMode.Balanced,
    MaxCacheSizeBytes = 200_000_000
});
```

**Tested in:** `ObjectGraphValueSizerCoverageTests` (default mode)

---

### Accurate
```csharp
SizeComputationMode = SizeComputationMode.Accurate
```

**Characteristics:**
- Deep object graph traversal
- More accurate size estimation
- Higher CPU overhead per operation
- Best when precise memory accounting is required

**Use When:**
- Memory limits are strict
- Objects have complex nested structures
- Accurate capacity enforcement is critical
- Performance overhead is acceptable

**Example:**
```csharp
var options = new BlitzCacheOptions
{
    SizeComputationMode = SizeComputationMode.Accurate,
    MaxCacheSizeBytes = 100_000_000  // Strict 100 MB limit
};
var cache = new BlitzCacheInstance(options);
cache.InitializeStatistics();
```

**Tested in:** `ObjectGraphValueSizerModesTests.AllModes_ProduceReasonableEstimates()`

---

### Adaptive
```csharp
SizeComputationMode = SizeComputationMode.Adaptive
```

**Characteristics:**
- Adjusts traversal depth based on object complexity
- Simple objects: shallow traversal (fast)
- Complex objects: deeper traversal (more accurate)
- Best for mixed workloads with varying object complexity

**Use When:**
- Caching diverse object types (small DTOs + large complex objects)
- Need performance for simple objects, accuracy for complex ones
- Workload characteristics vary over time

**Example:**
```csharp
var options = new BlitzCacheOptions
{
    SizeComputationMode = SizeComputationMode.Adaptive,
    MaxCacheSizeBytes = 200_000_000
};
var cache = new BlitzCacheInstance(options);
cache.InitializeStatistics();
```

**Tested in:** `ObjectGraphValueSizerModesTests.AdaptiveMode_AdjustsBehaviorBasedOnComplexity()`

---

### Mode Comparison Table

| Mode | Overhead | Accuracy | Best For | Tested In |
|------|----------|----------|----------|-----------|
| **Fast** | Lowest (~0.001ms) | Rough | High-throughput, simple objects | `ObjectGraphValueSizerModesTests` |
| **Balanced** | Low (~0.01ms) | Good | General purpose (default) | `ObjectGraphValueSizerCoverageTests` |
| **Accurate** | Medium (~0.05ms) | High | Strict memory limits | `ObjectGraphValueSizerModesTests` |
| **Adaptive** | Variable | Variable | Mixed workloads | `ObjectGraphValueSizerModesTests` |

---

## CapacityEvictionStrategy Options

Determines which entries are evicted first when `maxCacheSizeBytes` is exceeded.

### SmallestFirst (Default)
```csharp
EvictionStrategy = CapacityEvictionStrategy.SmallestFirst
```

**Behavior:** Evicts smallest entries first

**Advantages:**
- Preserves large, expensive-to-compute entries
- Minimizes cost of re-computation
- Good for scenarios where large entries take longer to rebuild

**Disadvantages:**
- May need to evict many small entries to free enough space
- Can lead to "death by a thousand cuts" eviction pattern

**Use When:**
- Large entries are expensive to recompute (database queries, API calls)
- Small entries are cheap to regenerate
- Maximizing cache value (large entries often = more work saved)

**Example:**
```csharp
var cache = new BlitzCacheInstance(
    maxCacheSizeBytes: 200_000_000,
    evictionStrategy: CapacityEvictionStrategy.SmallestFirst
);
cache.InitializeStatistics();
```

**Tested in:** `CapacityEvictionStrategyTests.SmallestFirst_EvictsSmallEntriesBeforeLarge()`

---

### LargestFirst
```csharp
EvictionStrategy = CapacityEvictionStrategy.LargestFirst
```

**Behavior:** Evicts largest entries first

**Advantages:**
- Quickly frees memory with few evictions
- Efficient when memory pressure is high
- Prevents memory exhaustion from large entries

**Disadvantages:**
- May evict expensive-to-rebuild large entries
- Large entries might thrash (evicted/rebuilt repeatedly)

**Use When:**
- Memory pressure is critical
- Large entries can be recomputed quickly
- Need to free space efficiently
- Preventing out-of-memory conditions

**Example:**
```csharp
var cache = new BlitzCacheInstance(
    maxCacheSizeBytes: 100_000_000,
    evictionStrategy: CapacityEvictionStrategy.LargestFirst
);
cache.InitializeStatistics();
```

**Tested in:** `CapacityEvictionStrategyTests.LargestFirst_EvictsLargeEntriesBeforeSmall()`

---

### Strategy Comparison Table

| Strategy | Evicts | Eviction Count | Memory Freed | Best For | Tested In |
|----------|--------|----------------|--------------|----------|-----------|
| **SmallestFirst** | Small entries | Many | Gradual | Expensive large entries | `CapacityEvictionStrategyTests` |
| **LargestFirst** | Large entries | Few | Quick | Memory pressure scenarios | `CapacityEvictionStrategyTests` |

---

## Configuration Examples

### Example 1: Zero-Overhead Mode (Maximum Performance)
```csharp
// Don't initialize statistics - zero tracking overhead
var cache = new BlitzCacheInstance(defaultMilliseconds: 300000);
// Do NOT call cache.InitializeStatistics()

// Result: No statistics, no tracking, minimal overhead
```

**Tested in:** `CacheStatisticsTests.Statistics_WhenDisabled_ReturnsNull()`

---

### Example 2: Basic Statistics Only (Minimal Tracking)
```csharp
var cache = new BlitzCacheInstance(
    defaultMilliseconds: 300000,
    maxTopSlowest: 0,      // Disable slow query tracking
    maxTopHeaviest: 0,     // Disable heavy entry tracking
    maxCacheSizeBytes: null // No capacity limits
);
cache.InitializeStatistics();

// Result: Basic hit/miss/entry counts only, minimal overhead
```

**Tested in:** `CacheStatisticsTests.TopSlowestQueries_EmptyWhenDisabled()`

---

### Example 3: Development Mode (Full Monitoring)
```csharp
var cache = new BlitzCacheInstance(
    defaultMilliseconds: 60000,  // 1 minute for quick cache turnover
    maxTopSlowest: 20,           // Track more for debugging
    maxTopHeaviest: 10,
    maxCacheSizeBytes: 50_000_000 // Small limit to test eviction
);
cache.InitializeStatistics();
```

---

### Example 4: Production Mode (Balanced)
```csharp
var cache = new BlitzCacheInstance(
    defaultMilliseconds: 300000,     // 5 minutes
    maxTopSlowest: 10,               // Monitor slow queries
    maxTopHeaviest: 5,               // Track memory hogs
    maxCacheSizeBytes: 500_000_000   // 500 MB limit
);
cache.InitializeStatistics();
```

**Tested in:** `MemoryLimitEvictionTests`

---

### Example 5: High-Memory Environment (Large Cache)
```csharp
var options = new BlitzCacheOptions
{
    DefaultMilliseconds = 600000,         // 10 minutes
    MaxTopSlowest = 10,
    MaxTopHeaviest = 10,
    MaxCacheSizeBytes = 2_000_000_000,    // 2 GB
    SizeComputationMode = SizeComputationMode.Balanced,
    EvictionStrategy = CapacityEvictionStrategy.SmallestFirst
};

var cache = new BlitzCacheInstance(options);
cache.InitializeStatistics();
```

---

### Example 6: Memory-Constrained Environment
```csharp
var options = new BlitzCacheOptions
{
    DefaultMilliseconds = 180000,         // 3 minutes (shorter cache)
    MaxTopSlowest = 5,
    MaxTopHeaviest = 5,
    MaxCacheSizeBytes = 50_000_000,       // 50 MB strict limit
    SizeComputationMode = SizeComputationMode.Accurate, // Precise sizing
    EvictionStrategy = CapacityEvictionStrategy.LargestFirst // Free memory fast
};

var cache = new BlitzCacheInstance(options);
cache.InitializeStatistics();
```

---

### Example 7: API Response Caching
```csharp
var cache = new BlitzCacheInstance(
    defaultMilliseconds: 300000,          // 5 minutes default
    maxTopSlowest: 10,                    // Track slow API calls
    maxTopHeaviest: 5,
    maxCacheSizeBytes: 200_000_000        // 200 MB for API responses
);
cache.InitializeStatistics();

// Use with Nuances for conditional caching by status code
var result = await cache.BlitzGet("api-call", async (nuances) => {
    var response = await CallExternalApi();
    nuances.CacheRetention = response.StatusCode == 200 ? 600000 : 30000;
    return response;
});
```

**See:** [NUANCES_COOKBOOK.md](NUANCES_COOKBOOK.md) for more patterns

---

## appsettings.json Examples

### Example 1: Basic Configuration
```json
{
  "BlitzCache": {
    "DefaultMilliseconds": 300000,
    "MaxTopSlowest": 10,
    "MaxTopHeaviest": 5,
    "MaxCacheSizeBytes": 200000000
  }
}
```

```csharp
// In Program.cs
services.Configure<BlitzCacheOptions>(Configuration.GetSection("BlitzCache"));
services.AddBlitzCache();
```

**Tested in:** `IServiceCollectionExtensionsTests`

---

### Example 2: Environment-Specific Configuration

**appsettings.Development.json:**
```json
{
  "BlitzCache": {
    "DefaultMilliseconds": 60000,
    "MaxTopSlowest": 20,
    "MaxTopHeaviest": 10,
    "MaxCacheSizeBytes": 50000000,
    "SizeComputationMode": "Balanced",
    "EvictionStrategy": "SmallestFirst"
  }
}
```

**appsettings.Production.json:**
```json
{
  "BlitzCache": {
    "DefaultMilliseconds": 300000,
    "MaxTopSlowest": 10,
    "MaxTopHeaviest": 5,
    "MaxCacheSizeBytes": 500000000,
    "SizeComputationMode": "Balanced",
    "EvictionStrategy": "SmallestFirst"
  }
}
```

---

### Example 3: Multiple Cache Instances

```json
{
  "ApiCache": {
    "DefaultMilliseconds": 300000,
    "MaxTopSlowest": 10,
    "MaxCacheSizeBytes": 200000000,
    "EvictionStrategy": "SmallestFirst"
  },
  "DatabaseCache": {
    "DefaultMilliseconds": 600000,
    "MaxTopSlowest": 5,
    "MaxCacheSizeBytes": 500000000,
    "EvictionStrategy": "SmallestFirst"
  }
}
```

```csharp
// In Program.cs
services.Configure<BlitzCacheOptions>("ApiCache", 
    Configuration.GetSection("ApiCache"));
services.Configure<BlitzCacheOptions>("DatabaseCache", 
    Configuration.GetSection("DatabaseCache"));

// Use named options
services.AddSingleton(sp =>
{
    var apiOptions = sp.GetRequiredService<IOptionsSnapshot<BlitzCacheOptions>>()
        .Get("ApiCache");
    return new BlitzCacheInstance(apiOptions);
});
```

---

## Decision Tree: Which Configuration to Use?

```
Do you need statistics at all?
├─ No → Don't call InitializeStatistics() (zero overhead)
└─ Yes
   └─ Do you need capacity limits?
      ├─ No → maxCacheSizeBytes: null
      └─ Yes → Set maxCacheSizeBytes
         └─ How important is size accuracy?
            ├─ Rough estimate OK → SizeComputationMode.Fast
            ├─ Good enough → SizeComputationMode.Balanced (default)
            ├─ Very precise → SizeComputationMode.Accurate
            └─ Mixed objects → SizeComputationMode.Adaptive

Do you need to track slow queries?
├─ Yes → Set maxTopSlowest: 10 (or higher)
└─ No → Set maxTopSlowest: 0

Do you need to track memory usage?
├─ Yes → Set maxTopHeaviest: 5 (or higher)
└─ No → Set maxTopHeaviest: 0

When capacity limit reached, prefer:
├─ Keep expensive large entries → EvictionStrategy.SmallestFirst
└─ Free memory quickly → EvictionStrategy.LargestFirst
```

---

## Related Documentation

- **[ERROR_HANDLING.md](ERROR_HANDLING.md)** - Error handling patterns with Nuances
- **[NUANCES_COOKBOOK.md](NUANCES_COOKBOOK.md)** - Dynamic cache duration recipes
- **[PERFORMANCE_TUNING.md](PERFORMANCE_TUNING.md)** - Performance optimization guide
- **[CAPACITY_EVICTION.md](CAPACITY_EVICTION.md)** - Deep dive on capacity-based eviction
- **[TESTING_GUIDE.md](TESTING_GUIDE.md)** - Testing patterns and examples

---

## Validation and Defaults

BlitzCache validates configuration on construction:

```csharp
// These will throw ArgumentOutOfRangeException:
new BlitzCacheInstance(defaultMilliseconds: 0);      // Must be > 0
new BlitzCacheInstance(defaultMilliseconds: -1000);  // Must be positive
new BlitzCacheOptions { MaxTopSlowest = -1 };        // Must be >= 0
new BlitzCacheOptions { MaxTopHeaviest = -1 };       // Must be >= 0
new BlitzCacheOptions { MaxCacheSizeBytes = -1 };    // Must be >= 0 or null
```

**Tested in:** `BlitzCacheOptions.Validate()` method

---

## Summary

- **Zero overhead:** Don't call `InitializeStatistics()`
- **Minimal tracking:** `maxTopSlowest: 0`, `maxTopHeaviest: 0`
- **Production:** Use defaults or Options pattern with environment-specific config
- **Memory limits:** Set `maxCacheSizeBytes` and choose appropriate `SizeComputationMode`
- **Eviction:** Choose strategy based on entry rebuild cost and memory pressure

All configuration examples are validated against real unit tests to ensure accuracy.
