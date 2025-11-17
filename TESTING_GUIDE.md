# BlitzCache Testing Guide

Comprehensive guide for testing applications that use BlitzCache, including unit testing patterns, integration testing strategies, and test-specific utilities.

## Table of Contents

- [Unit Testing with BlitzCache](#unit-testing-with-blitzcache)
- [Using NullBlitzCacheForTesting](#using-nullblitzcachefortesting)
- [Testing with Statistics](#testing-with-statistics)
- [Integration Testing Patterns](#integration-testing-patterns)
- [Mocking Strategies](#mocking-strategies)
- [Test Verification Patterns](#test-verification-patterns)
- [Common Testing Pitfalls](#common-testing-pitfalls)
- [Test Factory Patterns](#test-factory-patterns)

---

## Unit Testing with BlitzCache

### Pattern 1: Testing Business Logic Without Real Cache

When testing business logic, you often want to avoid cache interference. Use `NullBlitzCacheForTesting` to ensure every call executes the function.

```csharp
[Test]
public void ProcessOrder_ShouldCalculateCorrectTotal()
{
    // Arrange
    var cache = new NullBlitzCacheForTesting();
    var orderService = new OrderService(cache);
    var order = new Order { Items = new[] { 10.0, 20.0, 30.0 } };

    // Act
    var total = orderService.CalculateTotal(order); // Cache bypassed

    // Assert
    Assert.AreEqual(60.0, total);
}
```

**Why NullBlitzCacheForTesting?**
- Always executes the function (no caching behavior)
- Zero overhead for tests that don't need caching
- Validates business logic without cache side effects
- See: `BlitzCache/NullBlitzCacheForTesting.cs`
- **Validated by:** `BlitzCache.Tests/NullBlitzCacheForTestingTests.cs` (12 comprehensive tests)

---

### Pattern 2: Testing with Real Cache Instance

When you need to test actual caching behavior:

```csharp
[TestFixture]
public class CacheIntegrationTests
{
    private IBlitzCacheInstance cache;

    [SetUp]
    public void Setup()
    {
        cache = new BlitzCacheInstance(
            defaultMilliseconds: 60000,
            cleanupInterval: TimeSpan.FromMilliseconds(100)
        );
    }

    [TearDown]
    public void TearDown() => cache?.Dispose();

    [Test]
    public void ExpensiveOperation_ShouldBeCached()
    {
        // Arrange
        var callCount = 0;
        string ExpensiveFunction()
        {
            callCount++;
            return "expensive result";
        }

        // Act
        var result1 = cache.BlitzGet("key1", ExpensiveFunction, 1000);
        var result2 = cache.BlitzGet("key1", ExpensiveFunction, 1000);

        // Assert
        Assert.AreEqual(1, callCount, "Function should only execute once");
        Assert.AreEqual(result1, result2);
    }
}
```

**Reference:** See `BlitzCache.Tests/IntegrationTests.cs` for comprehensive examples.

---

### Pattern 3: Testing Error Handling

Test how your code handles cache failures without crashing:

```csharp
[Test]
public async Task ApiCall_WithCacheFailure_ShouldStillWork()
{
    // Arrange
    var faultyCache = new FaultyCacheForTesting(); // Throws exceptions
    var apiService = new ApiService(faultyCache);

    // Act & Assert - Should not throw
    var result = await apiService.GetUserAsync(userId: 123);
    Assert.IsNotNull(result);
}
```

**Reference:** `FaultyCacheForTesting` extends `NullBlitzCacheForTesting` to simulate failures. See `BlitzCache.Tests/Helpers/FaultyCacheForTesting.cs` and `BlitzCacheLoggingServiceTests.cs`.

---

## Using NullBlitzCacheForTesting

### What is NullBlitzCacheForTesting?

A test implementation of `IBlitzCache` that:
- **Never caches**: Always executes the provided function
- **Zero statistics overhead**: Returns `NullCacheStatistics` with all zeros
- **Lightweight**: No memory overhead, no cleanup threads
- **Transparent**: Same interface as real cache

### When to Use NullBlitzCacheForTesting

✅ **USE when:**
- Testing business logic without cache interference
- Testing components that inject `IBlitzCache` via DI
- Writing fast unit tests that don't need cache behavior
- Testing error handling without cache complexity

❌ **DON'T USE when:**
- Testing actual cache behavior (hits, misses, eviction)
- Testing cache-dependent timing or performance
- Testing cache key generation logic
- Testing statistics or monitoring functionality

### Validated Behavior

The following behaviors are validated by comprehensive tests in `NullBlitzCacheForTestingTests.cs`:

✅ **Always executes function** - Never caches, every call executes the function  
✅ **Works with sync and async** - Both `BlitzGet(Func<T>)` and `BlitzGet(Func<Task<T>>)` execute every time  
✅ **Returns null statistics by default** - `cache.Statistics` is `null` until `InitializeStatistics()` is called  
✅ **Returns zero statistics after initialization** - `NullCacheStatistics` always returns 0 for all metrics  
✅ **BlitzUpdate does nothing** - Update operations are no-ops (don't execute the function)  
✅ **Remove does nothing** - Safe to call, but has no effect  
✅ **GetSemaphoreCount returns 0** - No semaphores tracked  
✅ **Dispose is safe** - Can be disposed without throwing  

**Reference:** Run `dotnet test --filter "NullBlitzCacheForTestingTests"` to see all 12 validation tests.

### Example: Dependency Injection Testing

```csharp
public class UserService
{
    private readonly IBlitzCache cache;
    private readonly IUserRepository repository;

    public UserService(IBlitzCache cache, IUserRepository repository)
    {
        this.cache = cache;
        this.repository = repository;
    }

    public User GetUser(int userId)
    {
        return cache.BlitzGet($"user_{userId}", () => repository.GetById(userId), 60000);
    }
}

[TestFixture]
public class UserServiceTests
{
    [Test]
    public void GetUser_ShouldCallRepositoryEveryTime_WhenCacheDisabled()
    {
        // Arrange
        var nullCache = new NullBlitzCacheForTesting();
        var mockRepository = new Mock<IUserRepository>();
        mockRepository.Setup(r => r.GetById(It.IsAny<int>())).Returns(new User { Id = 1 });
        var service = new UserService(nullCache, mockRepository.Object);

        // Act
        service.GetUser(1);
        service.GetUser(1);
        service.GetUser(1);

        // Assert
        mockRepository.Verify(r => r.GetById(1), Times.Exactly(3));
    }

    [Test]
    public void GetUser_ShouldCallRepositoryOnce_WhenCacheEnabled()
    {
        // Arrange
        var realCache = new BlitzCacheInstance(60000);
        var mockRepository = new Mock<IUserRepository>();
        mockRepository.Setup(r => r.GetById(It.IsAny<int>())).Returns(new User { Id = 1 });
        var service = new UserService(realCache, mockRepository.Object);

        // Act
        service.GetUser(1);
        service.GetUser(1);
        service.GetUser(1);

        // Assert
        mockRepository.Verify(r => r.GetById(1), Times.Once);
        realCache.Dispose();
    }
}
```

---

## Testing with Statistics

### Pattern 1: Verify Cache Hit/Miss Behavior

```csharp
[TestFixture]
public class CacheStatisticsTests
{
    private IBlitzCacheInstance cache;

    [SetUp]
    public void Setup()
    {
        cache = new BlitzCacheInstance(60000);
        cache.InitializeStatistics(); // CRITICAL: Must call before using Statistics
    }

    [TearDown]
    public void TearDown() => cache?.Dispose();

    [Test]
    public void Statistics_CacheMiss_IncrementsCounters()
    {
        // Arrange
        var callCount = 0;
        string TestFunction()
        {
            callCount++;
            return "test result";
        }

        // Capture initial state
        var hitCountBefore = cache.Statistics.HitCount;
        var missCountBefore = cache.Statistics.MissCount;
        var entryCountBefore = cache.Statistics.EntryCount;

        // Act
        var result = cache.BlitzGet("test_key", TestFunction, 1000);

        // Assert
        var stats = cache.Statistics;
        Assert.AreEqual(hitCountBefore, stats.HitCount, "Should have no hits");
        Assert.AreEqual(missCountBefore + 1, stats.MissCount, "Should have one miss");
        Assert.AreEqual(entryCountBefore + 1, stats.EntryCount, "Should have one entry");
        Assert.AreEqual(1, callCount, "Function called once");
    }

    [Test]
    public void Statistics_CacheHit_IncrementsHitCounter()
    {
        // Arrange
        var callCount = 0;
        string TestFunction()
        {
            callCount++;
            return "test result";
        }

        // First call (miss)
        cache.BlitzGet("test_key", TestFunction, 1000);

        var hitCountBefore = cache.Statistics.HitCount;
        var missCountBefore = cache.Statistics.MissCount;

        // Act - Second call (hit)
        var result = cache.BlitzGet("test_key", TestFunction, 1000);

        // Assert
        var stats = cache.Statistics;
        Assert.AreEqual(hitCountBefore + 1, stats.HitCount, "Should have one hit");
        Assert.AreEqual(missCountBefore, stats.MissCount, "Miss count unchanged");
        Assert.AreEqual(0.5, stats.HitRatio, 0.001, "Hit ratio should be 50%");
        Assert.AreEqual(1, callCount, "Function only called on first miss");
    }
}
```

**Reference:** See `BlitzCache.Tests/Statistics/CacheStatisticsTests.cs` for comprehensive statistics testing patterns.

---

### Pattern 2: Verify Eviction Tracking

```csharp
[Test]
public void Statistics_RemoveOperation_IncrementsEvictionCount()
{
    // Arrange
    cache.BlitzGet("test_key", () => "test value", 1000);
    var evictionCountBefore = cache.Statistics.EvictionCount;

    // Act
    cache.Remove("test_key");

    // Assert
    Assert.AreEqual(evictionCountBefore + 1, cache.Statistics.EvictionCount);
    Assert.AreEqual(0, cache.Statistics.EntryCount, "Entry count zero after removal");
}

[Test]
public void Statistics_RemoveNonExistentKey_DoesNotIncrementEvictionCount()
{
    // Arrange
    var evictionCountBefore = cache.Statistics.EvictionCount;

    // Act
    cache.Remove("non_existent_key");

    // Assert
    Assert.AreEqual(evictionCountBefore, cache.Statistics.EvictionCount);
}
```

**Reference:** See `CacheStatisticsTests.Statistics_RemoveOperation_IncrementsEvictionCount()` in `BlitzCache.Tests/Statistics/CacheStatisticsTests.cs`.

---

### Pattern 3: Test Statistics Disabled State

```csharp
[Test]
public void Statistics_WhenDisabled_ReturnsNull()
{
    // Arrange
    var cache = new BlitzCacheInstance(60000);
    // NOTE: InitializeStatistics() NOT called

    // Act & Assert
    Assert.IsNull(cache.Statistics, "Statistics should be null when not initialized");
}

[Test]
public void TopSlowestQueries_EmptyWhenDisabled()
{
    var cache = new BlitzCacheInstance(60000);
    // No InitializeStatistics()

    // Act
    cache.BlitzGet("key1", () => "value", 1000);

    // Assert
    Assert.IsNull(cache.Statistics, "Statistics remain null");
}
```

**Critical:** If `InitializeStatistics()` is not called, `cache.Statistics` returns `null`. This is the **true zero-overhead mode**.

**Reference:** See `CacheStatisticsTests.Statistics_WhenDisabled_ReturnsNull()` in `BlitzCache.Tests/Statistics/CacheStatisticsTests.cs`.

---

## Integration Testing Patterns

### Pattern 1: Testing Cache with Real Dependencies

```csharp
[TestFixture]
public class OrderServiceIntegrationTests
{
    private IBlitzCacheInstance cache;
    private IDbConnection dbConnection;
    private OrderService orderService;

    [SetUp]
    public void Setup()
    {
        cache = new BlitzCacheInstance(60000);
        cache.InitializeStatistics();
        dbConnection = CreateTestDatabase();
        orderService = new OrderService(cache, dbConnection);
    }

    [TearDown]
    public void TearDown()
    {
        cache?.Dispose();
        dbConnection?.Dispose();
    }

    [Test]
    public void GetOrder_ShouldCacheDatabaseResults()
    {
        // Arrange
        SeedTestData();
        var orderId = 123;

        // Act - First call (miss)
        var result1 = orderService.GetOrder(orderId);
        var missCount1 = cache.Statistics.MissCount;
        var hitCount1 = cache.Statistics.HitCount;

        // Act - Second call (hit)
        var result2 = orderService.GetOrder(orderId);
        var missCount2 = cache.Statistics.MissCount;
        var hitCount2 = cache.Statistics.HitCount;

        // Assert
        Assert.AreEqual(result1.OrderId, result2.OrderId);
        Assert.AreEqual(1, missCount1, "First call is a miss");
        Assert.AreEqual(0, hitCount1, "No hits yet");
        Assert.AreEqual(1, missCount2, "Miss count unchanged");
        Assert.AreEqual(1, hitCount2, "Second call is a hit");
    }
}
```

---

### Pattern 2: Testing Async Operations with Cache

```csharp
[Test]
public async Task GetUserAsync_ShouldCacheAsyncResults()
{
    // Arrange
    var callCount = 0;
    async Task<User> GetUserFromApiAsync(int userId)
    {
        callCount++;
        await Task.Delay(100); // Simulate API call
        return new User { Id = userId, Name = $"User {userId}" };
    }

    // Act
    var user1 = await cache.BlitzGet("user_123", () => GetUserFromApiAsync(123), 60000);
    var user2 = await cache.BlitzGet("user_123", () => GetUserFromApiAsync(123), 60000);

    // Assert
    Assert.AreEqual(1, callCount, "Async function called once");
    Assert.AreEqual(user1.Id, user2.Id);
}
```

**Reference:** See `BlitzCache.Tests/IntegrationTests.cs` for async integration patterns.

---

### Pattern 3: Testing Cache Expiration

```csharp
[Test]
public async Task CachedValue_ShouldExpire_AfterTimeout()
{
    // Arrange
    var callCount = 0;
    string ExpirableFunction()
    {
        callCount++;
        return $"result_{callCount}";
    }

    // Act
    var result1 = cache.BlitzGet("expiring_key", ExpirableFunction, milliseconds: 50);
    await Task.Delay(100); // Wait for expiration
    var result2 = cache.BlitzGet("expiring_key", ExpirableFunction, milliseconds: 50);

    // Assert
    Assert.AreEqual(2, callCount, "Function called twice after expiration");
    Assert.AreNotEqual(result1, result2, "Results differ after expiration");
}
```

**Reference:** See `BlitzCache.Tests/Statistics/CacheExpirationStatisticsTests.cs` for expiration testing patterns.

---

## Mocking Strategies

### Pattern 1: Using Moq for IBlitzCache

```csharp
[Test]
public void ProductService_ShouldUseCache_WhenEnabled()
{
    // Arrange
    var mockCache = new Mock<IBlitzCache>();
    mockCache
        .Setup(c => c.BlitzGet(It.IsAny<string>(), It.IsAny<Func<Product>>(), It.IsAny<long?>()))
        .Returns<string, Func<Product>, long?>((key, func, ms) => func());

    var productService = new ProductService(mockCache.Object);

    // Act
    var product = productService.GetProduct(productId: 456);

    // Assert
    mockCache.Verify(
        c => c.BlitzGet(
            "product_456",
            It.IsAny<Func<Product>>(),
            It.IsAny<long?>()
        ),
        Times.Once
    );
}
```

---

### Pattern 2: Using NullBlitzCacheForTesting vs Real Cache

```csharp
[TestCase(true, 1, Description = "With cache: function called once")]
[TestCase(false, 3, Description = "Without cache: function called every time")]
public void ConfigurableCache_ShouldBehaveCorrectly(bool useCaching, int expectedCallCount)
{
    // Arrange
    IBlitzCache cache = useCaching
        ? new BlitzCacheInstance(60000)
        : new NullBlitzCacheForTesting();

    var callCount = 0;
    string TestFunction()
    {
        callCount++;
        return "result";
    }

    // Act
    cache.BlitzGet("key", TestFunction, 1000);
    cache.BlitzGet("key", TestFunction, 1000);
    cache.BlitzGet("key", TestFunction, 1000);

    // Assert
    Assert.AreEqual(expectedCallCount, callCount);

    if (cache is IDisposable disposable)
        disposable.Dispose();
}
```

---

### Pattern 3: Testing Statistics Availability

```csharp
[Test]
public void Service_ShouldHandleNullStatistics_Gracefully()
{
    // Arrange - Cache without statistics
    var cache = new BlitzCacheInstance(60000);
    var monitoringService = new CacheMonitoringService(cache);

    // Act & Assert - Should not throw NullReferenceException
    Assert.DoesNotThrow(() => monitoringService.LogCacheMetrics());
}

public class CacheMonitoringService
{
    private readonly IBlitzCacheInstance cache;

    public CacheMonitoringService(IBlitzCacheInstance cache)
    {
        this.cache = cache;
    }

    public void LogCacheMetrics()
    {
        if (cache.Statistics == null)
        {
            Console.WriteLine("Statistics not enabled");
            return;
        }

        Console.WriteLine($"Hit Ratio: {cache.Statistics.HitRatio:P}");
        Console.WriteLine($"Total Ops: {cache.Statistics.TotalOperations}");
    }
}
```

---

## Test Verification Patterns

### Pattern 1: Verify Cache Key Generation

```csharp
[TestCase(123, "user_123")]
[TestCase(456, "user_456")]
public void GetUser_ShouldUsePredictableCacheKey(int userId, string expectedKey)
{
    // Arrange
    var mockCache = new Mock<IBlitzCache>();
    var capturedKey = string.Empty;

    mockCache
        .Setup(c => c.BlitzGet(It.IsAny<string>(), It.IsAny<Func<User>>(), It.IsAny<long?>()))
        .Callback<string, Func<User>, long?>((key, func, ms) => capturedKey = key)
        .Returns<string, Func<User>, long?>((key, func, ms) => func());

    var userService = new UserService(mockCache.Object, Mock.Of<IUserRepository>());

    // Act
    userService.GetUser(userId);

    // Assert
    Assert.AreEqual(expectedKey, capturedKey);
}
```

---

### Pattern 2: Verify Cache Duration

```csharp
[Test]
public void GetProducts_ShouldCacheForFiveMinutes()
{
    // Arrange
    var mockCache = new Mock<IBlitzCache>();
    long? capturedDuration = null;

    mockCache
        .Setup(c => c.BlitzGet(It.IsAny<string>(), It.IsAny<Func<List<Product>>>(), It.IsAny<long?>()))
        .Callback<string, Func<List<Product>>, long?>((key, func, ms) => capturedDuration = ms)
        .Returns<string, Func<List<Product>>, long?>((key, func, ms) => func());

    var productService = new ProductService(mockCache.Object);

    // Act
    productService.GetProducts();

    // Assert
    Assert.AreEqual(300000, capturedDuration, "Should cache for 5 minutes (300,000ms)");
}
```

---

### Pattern 3: Verify No Duplicate Cache Calls

```csharp
[Test]
public void ConcurrentRequests_ShouldOnlyExecuteFunctionOnce()
{
    // Arrange
    var cache = new BlitzCacheInstance(60000);
    var callCount = 0;
    var lockObject = new object();

    string SlowFunction()
    {
        lock (lockObject)
        {
            callCount++;
        }
        Thread.Sleep(50); // Simulate slow operation
        return "result";
    }

    // Act - Fire 10 concurrent requests
    var tasks = Enumerable.Range(0, 10)
        .Select(_ => Task.Run(() => cache.BlitzGet("concurrent_key", SlowFunction, 60000)))
        .ToArray();

    Task.WaitAll(tasks);

    // Assert
    Assert.AreEqual(1, callCount, "Function should execute only once despite concurrency");
    cache.Dispose();
}
```

**Reference:** See `BlitzCache.Tests/ConcurrencyTests.cs` for comprehensive concurrency testing patterns.

---

## Common Testing Pitfalls

### Pitfall 1: Forgetting to Initialize Statistics

❌ **WRONG:**
```csharp
[Test]
public void TestStatistics()
{
    var cache = new BlitzCacheInstance(60000);
    // Missing: cache.InitializeStatistics();

    cache.BlitzGet("key", () => "value", 1000);
    
    Assert.IsNotNull(cache.Statistics); // FAILS - Statistics is null!
}
```

✅ **CORRECT:**
```csharp
[Test]
public void TestStatistics()
{
    var cache = new BlitzCacheInstance(60000);
    cache.InitializeStatistics(); // CRITICAL

    cache.BlitzGet("key", () => "value", 1000);
    
    Assert.IsNotNull(cache.Statistics); // PASSES
}
```

---

### Pitfall 2: Not Disposing Cache Instances

❌ **WRONG:**
```csharp
[Test]
public void TestCache()
{
    var cache = new BlitzCacheInstance(60000); // Not disposed
    cache.BlitzGet("key", () => "value", 1000);
    // Memory leak: cleanup thread still running
}
```

✅ **CORRECT:**
```csharp
[TestFixture]
public class MyTests
{
    private IBlitzCacheInstance cache;

    [SetUp]
    public void Setup() => cache = new BlitzCacheInstance(60000);

    [TearDown]
    public void TearDown() => cache?.Dispose(); // CRITICAL

    [Test]
    public void TestCache() => cache.BlitzGet("key", () => "value", 1000);
}
```

---

### Pitfall 3: Shared Cache State Between Tests

❌ **WRONG:**
```csharp
[TestFixture]
public class MyTests
{
    private static readonly IBlitzCacheInstance sharedCache = new BlitzCacheInstance(60000);

    [Test]
    public void Test1()
    {
        sharedCache.BlitzGet("key", () => "value1", 1000);
        // Test2 might see cached "value1"
    }

    [Test]
    public void Test2()
    {
        var result = sharedCache.BlitzGet("key", () => "value2", 1000);
        Assert.AreEqual("value2", result); // MIGHT FAIL due to Test1's cache
    }
}
```

✅ **CORRECT:**
```csharp
[TestFixture]
public class MyTests
{
    private IBlitzCacheInstance cache;

    [SetUp]
    public void Setup() => cache = new BlitzCacheInstance(60000); // Fresh cache per test

    [TearDown]
    public void TearDown() => cache?.Dispose();

    [Test]
    public void Test1() => cache.BlitzGet("key", () => "value1", 1000);

    [Test]
    public void Test2()
    {
        var result = cache.BlitzGet("key", () => "value2", 1000);
        Assert.AreEqual("value2", result); // PASSES - isolated cache
    }
}
```

---

### Pitfall 4: Using NullBlitzCacheForTesting When Testing Cache Behavior

❌ **WRONG:**
```csharp
[Test]
public void VerifyCacheHit()
{
    var cache = new NullBlitzCacheForTesting(); // Never caches!
    var callCount = 0;

    cache.BlitzGet("key", () => { callCount++; return "value"; }, 1000);
    cache.BlitzGet("key", () => { callCount++; return "value"; }, 1000);

    Assert.AreEqual(1, callCount); // FAILS - NullCache never caches
}
```

✅ **CORRECT:**
```csharp
[Test]
public void VerifyCacheHit()
{
    var cache = new BlitzCacheInstance(60000); // Real cache
    var callCount = 0;

    cache.BlitzGet("key", () => { callCount++; return "value"; }, 1000);
    cache.BlitzGet("key", () => { callCount++; return "value"; }, 1000);

    Assert.AreEqual(1, callCount); // PASSES
    cache.Dispose();
}
```

---

### Pitfall 5: Not Waiting for Async Operations

❌ **WRONG:**
```csharp
[Test]
public void TestAsyncCache()
{
    cache.BlitzGet("key", async () => await GetDataAsync(), 1000); // Not awaited!
    // Test completes before async operation finishes
}
```

✅ **CORRECT:**
```csharp
[Test]
public async Task TestAsyncCache()
{
    var result = await cache.BlitzGet("key", async () => await GetDataAsync(), 1000);
    Assert.IsNotNull(result);
}
```

---

## Test Factory Patterns

### Using TestFactory for Consistent Test Setup

```csharp
public static class TestFactory
{
    public static IBlitzCache CreateBlitzCacheGlobal() =>
        new BlitzCache(defaultMilliseconds: 60000);

    public static IBlitzCacheInstance CreateBlitzCacheInstance() =>
        new BlitzCacheInstance(
            defaultMilliseconds: 60000,
            cleanupInterval: TimeSpan.FromMilliseconds(100)
        );

    public static IBlitzCacheInstance CreateStatisticsEnabledCache()
    {
        var cache = CreateBlitzCacheInstance();
        cache.InitializeStatistics();
        return cache;
    }

    public static IBlitzCache CreateNullCache() =>
        new NullBlitzCacheForTesting();
}
```

**Usage in tests:**

```csharp
[TestFixture]
public class MyTests
{
    [Test]
    public void TestWithRealCache()
    {
        using var cache = TestFactory.CreateBlitzCacheInstance();
        cache.BlitzGet("key", () => "value", 1000);
    }

    [Test]
    public void TestWithoutCache()
    {
        var cache = TestFactory.CreateNullCache();
        cache.BlitzGet("key", () => "value", 1000);
    }

    [Test]
    public void TestStatistics()
    {
        using var cache = TestFactory.CreateStatisticsEnabledCache();
        Assert.IsNotNull(cache.Statistics);
    }
}
```

**Reference:** See `BlitzCache.Tests/Helpers/TestFactory.cs` for the actual implementation.

---

## Summary

### Quick Reference: Choose Your Testing Approach

| Scenario | Use | Initialize Statistics? | Dispose? |
|----------|-----|----------------------|----------|
| Test business logic only | `NullBlitzCacheForTesting` | No | No |
| Test cache behavior | `BlitzCacheInstance` | Optional | **Yes** |
| Test statistics | `BlitzCacheInstance` | **Yes** | **Yes** |
| Test error handling | `FaultyCacheForTesting` | No | No |
| Integration tests | `BlitzCacheInstance` | Optional | **Yes** |
| Mock with Moq | `Mock<IBlitzCache>` | N/A | No |

### Key Testing Principles

1. **Use NullBlitzCacheForTesting** for business logic tests that don't need caching
2. **Always call InitializeStatistics()** before accessing `cache.Statistics`
3. **Always dispose** `BlitzCacheInstance` in `[TearDown]` or `using` statements
4. **Use fresh cache instances** per test to avoid state pollution
5. **Use TestFactory** for consistent, maintainable test setup
6. **Test both hit and miss scenarios** when verifying cache behavior
7. **Verify cache keys and durations** using mocks when testing services

### Related Documentation

- [ERROR_HANDLING.md](ERROR_HANDLING.md) - Error handling patterns including caching exceptions
- [CONFIGURATION.md](CONFIGURATION.md) - Configuration options for test environments
- [PERFORMANCE_TUNING.md](PERFORMANCE_TUNING.md) - Zero-overhead mode for performance testing
- [EXAMPLES_INDEX.md](EXAMPLES_INDEX.md) - Additional code examples

### Test File References

All patterns in this guide are validated against actual tests:
- `BlitzCache.Tests/NullBlitzCacheForTestingTests.cs` - **NEW: 12 tests validating NullBlitzCacheForTesting behavior**
- `BlitzCache.Tests/Statistics/CacheStatisticsTests.cs` - Statistics verification patterns
- `BlitzCache.Tests/IntegrationTests.cs` - Integration testing patterns
- `BlitzCache.Tests/ConcurrencyTests.cs` - Concurrency and thread-safety tests
- `BlitzCache.Tests/Helpers/TestFactory.cs` - Centralized test setup utilities
- `BlitzCache/NullBlitzCacheForTesting.cs` - No-cache test implementation
- `BlitzCache.Tests/Helpers/FaultyCacheForTesting.cs` - Error simulation utility
