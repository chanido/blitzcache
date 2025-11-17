# BlitzCache Examples Index

> **Quick Reference**: Find the right example for your scenario. All examples use `BlitzGet` as the canonical get-or-add / GetOrAddAsync-style pattern for BlitzCache.

## 🔍 Find Examples by Problem

### Cache Stampede / Thundering Herd
- **[Example 11 (BasicUsageExamples.cs)](BlitzCache.Tests/Examples/BasicUsageExamples.cs#L282)** - Demonstrates preventing duplicate execution under concurrent load
- **[Example 2 (AdvancedUsageExamples.cs)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs)** - Thread-safe concurrent access patterns

### Database Query Optimization
- **[Example 10 (BasicUsageExamples.cs)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/BasicUsageExamples.cs)** - Caching expensive database calls
- **[Example 11 (BasicUsageExamples.cs)](BlitzCache.Tests/Examples/BasicUsageExamples.cs#L282)** - Preventing duplicate database queries

### API Rate Limiting Protection
- **[Example 3 (AdvancedUsageExamples.cs)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs)** - Circuit breaker pattern for external services
- **[Example 6 (AdvancedUsageExamples.cs)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs)** - Conditional caching with retry logic

### Getting Started
- **[Example 1 (BasicUsageExamples.cs)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/BasicUsageExamples.cs)** - Basic synchronous caching
- **[Example 2 (BasicUsageExamples.cs)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/BasicUsageExamples.cs)** - Basic asynchronous caching

## 📚 Examples by Category

### Basic Usage (Start Here)

| Example | Description | Keywords | File |
|---------|-------------|----------|------|
| Example 1 | Basic sync caching | simple, synchronous, getting-started | [BasicUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/BasicUsageExamples.cs) |
| Example 2 | Async operations | async, await, asynchronous | [BasicUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/BasicUsageExamples.cs) |
| Example 3 | Different cache keys | multi-tenant, user-specific, isolation | [BasicUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/BasicUsageExamples.cs) |
| Example 4 | Cache expiration | TTL, timeout, automatic-refresh | [BasicUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/BasicUsageExamples.cs) |
| Example 5 | Manual cache removal | invalidation, eviction, manual-control | [BasicUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/BasicUsageExamples.cs) |
| Example 6 | Pre-populate cache | cache-warming, startup, preload | [BasicUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/BasicUsageExamples.cs) |
| Example 7 | Any data type | objects, collections, generics | [BasicUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/BasicUsageExamples.cs) |
| Example 8 | Global cache | singleton, application-wide, shared | [BasicUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/BasicUsageExamples.cs) |
| Example 9 | Automatic keys | auto-generation, CallerMemberName, convenience | [BasicUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/BasicUsageExamples.cs) |
| Example 10 | Real-world scenario | database, production, performance | [BasicUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/BasicUsageExamples.cs) |
| Example 11 | Thundering herd prevention | cache-stampede, duplicate-execution, concurrent-load | [BasicUsageExamples.cs](BlitzCache.Tests/Examples/BasicUsageExamples.cs#L282) |

### Advanced Patterns

| Example | Description | Keywords | File |
|---------|-------------|----------|------|
| Example 1 | Dynamic timeout with Nuances | conditional-duration, smart-caching, result-based-duration | [AdvancedUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs) |
| Example 2 | Concurrent access | thread-safe, multi-threading, concurrency | [AdvancedUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs) |
| Example 3 | Circuit breaker | resilience, fault-tolerance, error-handling | [AdvancedUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs) |
| Example 4 | Multi-level caching | strategy, architecture, layered | [AdvancedUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs) |
| Example 5 | Cache warming | preload, startup-optimization, zero-latency | [AdvancedUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs) |
| Example 6 | Conditional caching | retry-logic, error-handling, smart-behavior | [AdvancedUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs) |
| Example 7 | Global vs independent | architecture, isolation, singleton | [AdvancedUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs) |
| Example 8 | Performance monitoring | metrics, diagnostics, semaphores | [AdvancedUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs) |
| Example 9 | Dependency injection | DI, ASP.NET-Core, enterprise | [AdvancedUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs) |
| Example 10 | Statistics monitoring | analytics, hit-ratio, observability | [AdvancedUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs) |
| Example 11 | Complete DI guide | production, patterns, best-practices | [AdvancedUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs) |
| Example 12 | Performance debugging | TopSlowestQueries, memory-tracking, troubleshooting | [AdvancedUsageExamples.cs](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs) |

## 🎯 Find Examples by Use Case

### I need to...

#### Prevent Duplicate Execution
→ **[Example 11 (Basic)](BlitzCache.Tests/Examples/BasicUsageExamples.cs#L282)** - Shows how BlitzCache prevents duplicate database queries  
→ **[Example 2 (Advanced)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs)** - Concurrent access patterns

#### Cache Database Queries
→ **[Example 10 (Basic)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/BasicUsageExamples.cs)** - Real-world database caching  
→ **[Example 4 (Advanced)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs)** - Multi-level caching strategies

#### Cache API Calls
→ **[Example 3 (Advanced)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs)** - Circuit breaker for APIs  
→ **[Example 6 (Advanced)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs)** - Retry logic for failed calls

#### Integrate with ASP.NET Core
→ **[Example 9 (Advanced)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs)** - Dependency injection patterns  
→ **[Example 11 (Advanced)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs)** - Complete DI guide

#### Monitor Cache Performance
→ **[Example 10 (Advanced)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs)** - Statistics monitoring  
→ **[Example 8 (Advanced)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs)** - Performance diagnostics

#### Debug Performance Issues
→ **[Example 12 (Advanced)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs)** - Performance monitoring with TopSlowestQueries and memory tracking

#### Cache Different Results for Different Durations
→ **[Example 1 (Advanced)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs)** - Dynamic timeout with Nuances (cache success longer than errors)

#### Handle Cache Expiration
→ **[Example 4 (Basic)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/BasicUsageExamples.cs)** - Automatic expiration  
→ **[Example 1 (Advanced)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs)** - Dynamic timeout with Nuances

#### Pre-load Cache Data
→ **[Example 6 (Basic)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/BasicUsageExamples.cs)** - BlitzUpdate for preloading  
→ **[Example 5 (Advanced)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs)** - Cache warming strategies

## 🔑 Search by Keyword

### Problem Keywords
- **cache-stampede** → Example 11 (Basic), Example 2 (Advanced)
- **thundering-herd** → Example 11 (Basic), Example 2 (Advanced)
- **dog-pile** → Example 2 (Advanced)
- **duplicate-execution** → Example 11 (Basic), Example 2 (Advanced)
- **race-condition** → Example 2 (Advanced)
- **connection-pool-exhausted** → Example 10 (Basic), Example 11 (Basic)

### Technology Keywords
- **database** → Example 10, 11 (Basic)
- **API** → Example 3, 6 (Advanced)
- **ASP.NET-Core** → Example 9, 11 (Advanced)
- **async-await** → Example 2 (Basic), Most Advanced examples
- **dependency-injection** → Example 9, 11 (Advanced)

### Feature Keywords
- **statistics** → Example 10 (Advanced)
- **monitoring** → Example 8, 10, 12 (Advanced)
- **performance-tracking** → Example 10, 12 (Advanced)
- **TopSlowestQueries** → Example 12 (Advanced)
- **memory-usage** → Example 12 (Advanced)
- **performance-debugging** → Example 12 (Advanced)
- **auto-keys** → Example 9 (Basic)
- **global-cache** → Example 8 (Basic), Example 7 (Advanced)
- **Nuances** → Example 1 (Advanced)
- **dynamic-cache-duration** → Example 1 (Advanced)
- **result-based-caching** → Example 1 (Advanced)
- **different-cache-times** → Example 1 (Advanced)
- **circuit-breaker** → Example 3 (Advanced)

## 📖 How to Run Examples

All examples are executable unit tests:

```bash
# Run all basic examples
dotnet test --filter "BasicUsageExamples"

# Run all advanced examples
dotnet test --filter "AdvancedUsageExamples"

# Run specific example
dotnet test --filter "Example1_BasicSyncCaching"

# Run thundering herd demo
dotnet test --filter "Example11_ThunderingHerdPrevention"
```

## 🚀 Quick Start Path

**New to BlitzCache?** Follow this learning path:

1. **[Example 1 (Basic)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/BasicUsageExamples.cs)** - Understand basic caching
2. **[Example 2 (Basic)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/BasicUsageExamples.cs)** - Learn async patterns
3. **[Example 10 (Basic)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/BasicUsageExamples.cs)** - See real-world usage
4. **[Example 11 (Basic)](BlitzCache.Tests/Examples/BasicUsageExamples.cs#L282)** - Understand thundering herd protection
5. **[Example 9 (Advanced)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs)** - Integrate with ASP.NET Core

## 💡 Example Code Patterns

### Simplest Possible Usage
```csharp
var result = cache.BlitzGet("key", () => ExpensiveOperation(), 300000);
```
See: **[Example 1 (Basic)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/BasicUsageExamples.cs)**

### Async Pattern
```csharp
var result = await cache.BlitzGet("key", async () => await ExpensiveOperationAsync(), 300000);
```
See: **[Example 2 (Basic)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/BasicUsageExamples.cs)**

### ASP.NET Core Service
```csharp
public class UserService
{
    private readonly IBlitzCache _cache;
    public UserService(IBlitzCache cache) => _cache = cache;
    
    public async Task<User> GetUser(int id) =>
        await _cache.BlitzGet($"user_{id}", 
            async () => await _db.Users.FindAsync(id), 
            300000);
}
```
See: **[Example 9 (Advanced)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs)**

### Dynamic Cache Duration (Nuances)
```csharp
var apiResponse = await cache.BlitzGet("api-key", async (nuances) => {
    var result = await CallExternalApi();
    
    // Set cache time based on result
    if (result.StatusCode == 200)
        nuances.CacheRetention = 600000;  // Success: 10 minutes
    else if (result.StatusCode == 404)
        nuances.CacheRetention = 60000;   // Not found: 1 minute
    else
        nuances.CacheRetention = 30000;   // Error: 30 seconds
    
    return result;
});
```
See: **[Example 1 (Advanced)](https://github.com/chanido/blitzcache/blob/develop/BlitzCache.Tests/Examples/AdvancedUsageExamples.cs)**

## 📖 Comprehensive Documentation Guides

Master BlitzCache with these in-depth guides:

- **[CONFIGURATION.md](CONFIGURATION.md)** - Complete configuration reference: all enums, appsettings.json patterns, decision trees
  - SizeComputationMode (Fast, Balanced, Accurate, Adaptive)
  - CapacityEvictionStrategy (SmallestFirst, LargestFirst)
  - Configuration decision trees and environment-specific recommendations

- **[ERROR_HANDLING.md](ERROR_HANDLING.md)** - 10 comprehensive error handling patterns
  - Pattern 1: Don't cache errors (CacheRetention = 0)
  - Pattern 2: Cache by HTTP status (200=10m, 404=1m, 500=5s)
  - Pattern 4: Circuit breaker pattern
  - Pattern 7: Differentiate exception types

- **[NUANCES_COOKBOOK.md](NUANCES_COOKBOOK.md)** - 10 recipe-style patterns for dynamic cache duration
  - Cache by HTTP status, data completeness, empty results
  - Cache by quality, size, age, user type, time of day
  - Multi-condition logic and validation patterns

- **[MIGRATION_GUIDE.md](MIGRATION_GUIDE.md)** - IMemoryCache to BlitzCache migration
  - Pattern 1: Basic IMemoryCache + SemaphoreSlim (15 lines → 2 lines)
  - Pattern 2: Custom GetOrAddAsync extension method removal
  - Pattern 3: Double-check locking pattern replacement
  - Pattern 4: Per-key semaphore dictionary elimination

- **[CACHE_KEY_DESIGN.md](CACHE_KEY_DESIGN.md)** - Best practices for cache key design
  - Good patterns: "user_{userId}", "tenant_{tenantId}_config_{configId}"
  - Bad patterns: object.ToString(), DateTime.Now.Ticks, Guid.NewGuid()
  - 4 composite key strategies (concatenation, structured, hash-based, JSON)
  - Multi-tenant and hierarchical patterns

- **[PERFORMANCE_TUNING.md](PERFORMANCE_TUNING.md)** - Performance modes and benchmarks
  - Zero-overhead mode: Don't call InitializeStatistics() (not maxTopSlowest: 0)
  - Performance comparison: With stats ~328K ops/sec, Without stats ~389K ops/sec
  - Configuration modes: zero-overhead, minimal tracking, production, memory-constrained

- **[TESTING_GUIDE.md](TESTING_GUIDE.md)** - Unit and integration testing patterns
  - Using NullBlitzCacheForTesting for business logic tests
  - Testing with statistics: hit/miss verification, eviction tracking
  - Mocking strategies: Mock<IBlitzCache>, TestFactory patterns
  - Common pitfalls: forgetting InitializeStatistics(), not disposing cache instances

- **[CAPACITY_EVICTION.md](CAPACITY_EVICTION.md)** - Deep-dive on capacity-based eviction
  - Proactive enforcement: CapacityEnforcer mechanism
  - Strategy comparison: SmallestFirst vs LargestFirst with benchmarks
  - Choosing size limits: formulas and environment-specific recommendations
  - Memory accounting: approximate vs exact tracking
  - Troubleshooting: common issues and solutions

## 📚 Additional Resources

- **[README.md](README.md)** - Complete documentation
- **[INSTRUCTIONS.md](INSTRUCTIONS.md)** - AI assistant guide
- **[GitHub Repository](https://github.com/chanido/blitzcache)** - Source code
- **[NuGet Package](https://www.nuget.org/packages/BlitzCache/)** - Download

---

**Can't find what you need?** Check the [full README](README.md) or open an [issue on GitHub](https://github.com/chanido/blitzcache/issues).
