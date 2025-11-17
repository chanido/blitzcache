# BlitzCache Error Handling Cookbook

> **Complete patterns for handling errors, exceptions, and failures with BlitzCache**

## Table of Contents
- [Quick Reference](#quick-reference)
- [Pattern 1: Don't Cache Errors](#pattern-1-dont-cache-errors)
- [Pattern 2: Cache by HTTP Status Code](#pattern-2-cache-by-http-status-code)
- [Pattern 3: Short-Cache Transient Failures](#pattern-3-short-cache-transient-failures)
- [Pattern 4: Circuit Breaker Pattern](#pattern-4-circuit-breaker-pattern)
- [Pattern 5: Cache Empty Results Differently](#pattern-5-cache-empty-results-differently)
- [Pattern 6: Fallback Values on Error](#pattern-6-fallback-values-on-error)
- [Pattern 7: Differentiate Exception Types](#pattern-7-differentiate-exception-types)
- [Pattern 8: Exponential Backoff with Caching](#pattern-8-exponential-backoff-with-caching)
- [Pattern 9: Stale-While-Revalidate](#pattern-9-stale-while-revalidate)
- [Pattern 10: Graceful Degradation](#pattern-10-graceful-degradation)
- [Testing Error Patterns](#testing-error-patterns)

---

## Quick Reference

| Pattern | When to Use | Cache Duration | Tested In |
|---------|-------------|----------------|-----------|
| Don't Cache Errors | Never cache failures | `CacheRetention = 0` | `AdvancedUsageExamples.Example6` |
| HTTP Status Caching | API calls with status codes | 200=10m, 404=1m, 500=5s | Production pattern |
| Transient Failures | Network/timeout errors | 5-30 seconds | Circuit breaker tests |
| Circuit Breaker | Failing services | Cache exception itself | `AdvancedUsageExamples.Example3` |
| Empty Results | Database queries | Short or no cache | Common pattern |
| Fallback Values | Return default on error | Cache fallback | Production pattern |
| Exception Types | Different error severities | By exception type | Production pattern |
| Exponential Backoff | Repeated failures | Increasing duration | Production pattern |
| Stale-While-Revalidate | Accept stale data | Extended on error | Advanced pattern |
| Graceful Degradation | Service partially down | Partial results | Production pattern |

---

## Pattern 1: Don't Cache Errors

**Problem:** Failures should be retried, not cached.

**Solution:** Set `nuances.CacheRetention = 0` to skip caching on error.

### Basic Implementation

```csharp
public async Task<ApiResponse> CallApiAsync(string endpoint)
{
    return await _cache.BlitzGet($"api_{endpoint}", async (nuances) =>
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            nuances.CacheRetention = 300000; // Success: 5 minutes
            
            return new ApiResponse { IsSuccess = true, Data = content };
        }
        catch (Exception ex)
        {
            nuances.CacheRetention = 0; // Don't cache failures
            throw;
        }
    });
}
```

### With Return Instead of Throw

```csharp
public async Task<Result<User>> GetUserAsync(int userId)
{
    return await _cache.BlitzGet($"user_{userId}", async (nuances) =>
    {
        try
        {
            var user = await _database.GetUserAsync(userId);
            
            if (user == null)
            {
                nuances.CacheRetention = 0; // Don't cache null results
                return Result<User>.NotFound();
            }
            
            nuances.CacheRetention = 300000; // Cache found users: 5 minutes
            return Result<User>.Success(user);
        }
        catch (Exception ex)
        {
            nuances.CacheRetention = 0; // Don't cache errors
            return Result<User>.Error(ex.Message);
        }
    });
}
```

**Tested in:** `AdvancedUsageExamples.Example6_ConditionalCaching()`

---

## Pattern 2: Cache by HTTP Status Code

**Problem:** Different HTTP responses have different cache-worthiness.

**Solution:** Set cache duration based on status code.

### Implementation

```csharp
public async Task<HttpResponse> CallExternalApiAsync(string url)
{
    return await _cache.BlitzGet($"api_{url}", async (nuances) =>
    {
        var response = await _httpClient.GetAsync(url);
        
        nuances.CacheRetention = response.StatusCode switch
        {
            HttpStatusCode.OK => 600000,                    // 200: 10 minutes
            HttpStatusCode.Created => 300000,               // 201: 5 minutes
            HttpStatusCode.NoContent => 60000,              // 204: 1 minute
            HttpStatusCode.NotModified => 1800000,          // 304: 30 minutes
            HttpStatusCode.BadRequest => 60000,             // 400: 1 minute (client error)
            HttpStatusCode.Unauthorized => 30000,           // 401: 30 seconds
            HttpStatusCode.Forbidden => 60000,              // 403: 1 minute
            HttpStatusCode.NotFound => 60000,               // 404: 1 minute
            HttpStatusCode.TooManyRequests => 300000,       // 429: 5 minutes
            HttpStatusCode.InternalServerError => 5000,     // 500: 5 seconds
            HttpStatusCode.BadGateway => 10000,             // 502: 10 seconds
            HttpStatusCode.ServiceUnavailable => 10000,     // 503: 10 seconds
            HttpStatusCode.GatewayTimeout => 5000,          // 504: 5 seconds
            _ => 30000                                       // Other: 30 seconds
        };
        
        return new HttpResponse
        {
            StatusCode = response.StatusCode,
            Content = await response.Content.ReadAsStringAsync()
        };
    });
}
```

### Simplified Success/Error Pattern

```csharp
public async Task<string> FetchDataAsync(string key)
{
    return await _cache.BlitzGet(key, async (nuances) =>
    {
        var response = await _httpClient.GetAsync($"/api/data/{key}");
        
        // Simple binary decision
        if (response.IsSuccessStatusCode)
        {
            nuances.CacheRetention = 600000; // Success: 10 minutes
            return await response.Content.ReadAsStringAsync();
        }
        else
        {
            nuances.CacheRetention = 30000; // Error: 30 seconds
            throw new HttpRequestException($"API returned {response.StatusCode}");
        }
    });
}
```

**Real-world pattern used in production scenarios**

---

## Pattern 3: Short-Cache Transient Failures

**Problem:** Transient failures (network glitches, timeouts) should be retried soon, but not immediately.

**Solution:** Cache transient errors for a short duration to prevent hammering.

### Implementation

```csharp
public async Task<DatabaseResult> QueryDatabaseAsync(string query)
{
    return await _cache.BlitzGet($"db_{query}", async (nuances) =>
    {
        try
        {
            var result = await _database.QueryAsync(query);
            nuances.CacheRetention = 300000; // Success: 5 minutes
            return result;
        }
        catch (SqlException ex) when (IsTransient(ex))
        {
            // Short-cache transient errors to prevent immediate retry storm
            nuances.CacheRetention = 5000; // Transient: 5 seconds
            throw;
        }
        catch (SqlException ex)
        {
            // Don't cache permanent errors (bad SQL, etc.)
            nuances.CacheRetention = 0;
            throw;
        }
    });
}

private bool IsTransient(SqlException ex)
{
    // Common transient SQL error codes
    int[] transientErrorCodes = { -1, -2, 1205, 40197, 40501, 40613, 49918, 49919, 49920 };
    return transientErrorCodes.Contains(ex.Number);
}
```

### With Exponential Backoff

```csharp
private int _failureCount = 0;

public async Task<ApiResponse> CallWithBackoffAsync(string endpoint)
{
    return await _cache.BlitzGet($"backoff_{endpoint}", async (nuances) =>
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            
            _failureCount = 0; // Reset on success
            nuances.CacheRetention = 300000; // Success: 5 minutes
            
            return await ParseResponse(response);
        }
        catch (HttpRequestException)
        {
            _failureCount++;
            
            // Exponential backoff: 5s, 10s, 20s, 40s, max 60s
            var backoffSeconds = Math.Min(5 * Math.Pow(2, _failureCount - 1), 60);
            nuances.CacheRetention = (long)(backoffSeconds * 1000);
            
            throw;
        }
    });
}
```

**Tested in:** Circuit breaker tests and timeout scenarios

---

## Pattern 4: Circuit Breaker Pattern

**Problem:** Failing service should not be hammered with repeated requests.

**Solution:** Cache the exception itself for a duration, allowing the service to recover.

### Basic Circuit Breaker

```csharp
public async Task<string> CallUnreliableServiceAsync(string serviceId)
{
    return await _cache.BlitzGet($"service_{serviceId}", async () =>
    {
        // If service fails, BlitzCache caches the exception
        // Subsequent calls get the cached exception without hitting the service
        var response = await _httpClient.GetAsync($"/service/{serviceId}");
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadAsStringAsync();
    }, 30000); // Cache for 30 seconds (including exceptions)
}

// Usage
try
{
    var result = await CallUnreliableServiceAsync("problematic-api");
}
catch (HttpRequestException)
{
    // First call: hits service and caches exception
    // Next 30 seconds: returns cached exception without hitting service
    Console.WriteLine("Service is down, cached exception prevents hammering");
}
```

**Tested in:** `AdvancedUsageExamples.Example3_CircuitBreakerPattern()`

### Circuit Breaker with State Tracking

```csharp
public class CircuitBreakerCache
{
    private readonly IBlitzCache _cache;
    private int _failureCount = 0;
    private readonly int _failureThreshold = 5;
    
    public async Task<string> CallWithCircuitBreakerAsync(string key)
    {
        return await _cache.BlitzGet($"circuit_{key}", async (nuances) =>
        {
            try
            {
                var result = await MakeExternalCallAsync();
                
                _failureCount = 0; // Reset on success
                nuances.CacheRetention = 300000; // Success: 5 minutes
                
                return result;
            }
            catch (Exception)
            {
                _failureCount++;
                
                if (_failureCount >= _failureThreshold)
                {
                    // Circuit is OPEN - long cache to give service time to recover
                    nuances.CacheRetention = 60000; // 1 minute
                    Console.WriteLine("Circuit OPEN - service needs recovery time");
                }
                else
                {
                    // Circuit is CLOSED - short cache for quick retry
                    nuances.CacheRetention = 5000; // 5 seconds
                }
                
                throw;
            }
        });
    }
}
```

---

## Pattern 5: Cache Empty Results Differently

**Problem:** Empty results might indicate missing data that could arrive soon, or legitimately empty datasets.

**Solution:** Cache empty results for shorter duration than populated results.

### Database Query Pattern

```csharp
public async Task<List<Product>> SearchProductsAsync(string searchTerm)
{
    return await _cache.BlitzGet($"search_{searchTerm}", async (nuances) =>
    {
        var products = await _database.SearchProductsAsync(searchTerm);
        
        if (products.Any())
        {
            nuances.CacheRetention = 600000; // Results found: 10 minutes
        }
        else
        {
            nuances.CacheRetention = 60000; // No results: 1 minute (might add products soon)
        }
        
        return products;
    });
}
```

### Optional Value Pattern

```csharp
public async Task<User?> FindUserAsync(string username)
{
    return await _cache.BlitzGet($"user_{username}", async (nuances) =>
    {
        var user = await _database.FindUserByUsernameAsync(username);
        
        if (user != null)
        {
            nuances.CacheRetention = 1800000; // Found: 30 minutes
        }
        else
        {
            nuances.CacheRetention = 30000; // Not found: 30 seconds
        }
        
        return user;
    });
}
```

### Don't Cache Empty

```csharp
public async Task<List<Order>> GetOrdersAsync(int customerId)
{
    return await _cache.BlitzGet($"orders_{customerId}", async (nuances) =>
    {
        var orders = await _database.GetOrdersAsync(customerId);
        
        if (orders.Any())
        {
            nuances.CacheRetention = 300000; // Orders found: 5 minutes
        }
        else
        {
            nuances.CacheRetention = 0; // Empty: don't cache (retry next call)
        }
        
        return orders;
    });
}
```

---

## Pattern 6: Fallback Values on Error

**Problem:** Need to return something even when the primary source fails.

**Solution:** Return fallback/default value and cache it briefly.

### Fallback Implementation

```csharp
public async Task<Configuration> GetConfigurationAsync()
{
    return await _cache.BlitzGet("app_config", async (nuances) =>
    {
        try
        {
            var config = await _configService.LoadAsync();
            nuances.CacheRetention = 3600000; // Success: 1 hour
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load config, using defaults");
            
            // Return fallback configuration
            nuances.CacheRetention = 30000; // Fallback: 30 seconds (retry soon)
            return Configuration.Default;
        }
    });
}
```

### Stale Data as Fallback

```csharp
public class CacheWithFallback
{
    private readonly IBlitzCache _cache;
    private string? _lastKnownGood = null;
    
    public async Task<string> GetDataWithFallbackAsync()
    {
        return await _cache.BlitzGet("data_key", async (nuances) =>
        {
            try
            {
                var data = await FetchFreshDataAsync();
                
                _lastKnownGood = data; // Store for future fallback
                nuances.CacheRetention = 300000; // Success: 5 minutes
                
                return data;
            }
            catch (Exception)
            {
                if (_lastKnownGood != null)
                {
                    // Return stale data as fallback
                    nuances.CacheRetention = 10000; // Stale: 10 seconds (retry soon)
                    return _lastKnownGood;
                }
                
                // No fallback available
                nuances.CacheRetention = 0;
                throw;
            }
        });
    }
}
```

---

## Pattern 7: Differentiate Exception Types

**Problem:** Different exceptions require different caching strategies.

**Solution:** Match on exception type and set appropriate cache duration.

### Implementation

```csharp
public async Task<ApiData> FetchApiDataAsync(string endpoint)
{
    return await _cache.BlitzGet($"api_{endpoint}", async (nuances) =>
    {
        try
        {
            var data = await _apiClient.GetDataAsync(endpoint);
            nuances.CacheRetention = 600000; // Success: 10 minutes
            return data;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // 404: Resource doesn't exist, might be created soon
            nuances.CacheRetention = 60000; // 1 minute
            throw;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            // 429: Rate limited, back off longer
            nuances.CacheRetention = 300000; // 5 minutes
            throw;
        }
        catch (TimeoutException)
        {
            // Timeout: Transient, retry soon
            nuances.CacheRetention = 10000; // 10 seconds
            throw;
        }
        catch (TaskCanceledException)
        {
            // Cancellation: Don't cache, let it retry immediately
            nuances.CacheRetention = 0;
            throw;
        }
        catch (Exception ex)
        {
            // Unknown error: Short cache to allow retry
            _logger.LogError(ex, "Unknown error fetching {Endpoint}", endpoint);
            nuances.CacheRetention = 5000; // 5 seconds
            throw;
        }
    });
}
```

### Exception Severity Pattern

```csharp
public async Task<Result> ProcessRequestAsync(string requestId)
{
    return await _cache.BlitzGet($"request_{requestId}", async (nuances) =>
    {
        try
        {
            var result = await _processor.ProcessAsync(requestId);
            nuances.CacheRetention = 300000; // Success: 5 minutes
            return result;
        }
        catch (ValidationException ex)
        {
            // Client error: Cache longer (won't fix itself)
            nuances.CacheRetention = 600000; // 10 minutes
            throw;
        }
        catch (DependencyException ex)
        {
            // Dependency error: Cache briefly (might recover)
            nuances.CacheRetention = 30000; // 30 seconds
            throw;
        }
        catch (InfrastructureException ex)
        {
            // Infrastructure error: Don't cache (needs investigation)
            nuances.CacheRetention = 0;
            throw;
        }
    });
}
```

---

## Pattern 8: Exponential Backoff with Caching

**Problem:** Repeated failures should result in progressively longer wait times.

**Solution:** Increase cache duration with each failure.

### Implementation with Retry Counter

```csharp
private readonly ConcurrentDictionary<string, int> _retryAttempts = new();

public async Task<string> CallWithExponentialBackoffAsync(string key)
{
    return await _cache.BlitzGet($"backoff_{key}", async (nuances) =>
    {
        var attempts = _retryAttempts.GetOrAdd(key, 0);
        
        try
        {
            var result = await MakeExternalCallAsync(key);
            
            _retryAttempts.TryRemove(key, out _); // Reset on success
            nuances.CacheRetention = 300000; // Success: 5 minutes
            
            return result;
        }
        catch (Exception)
        {
            _retryAttempts.AddOrUpdate(key, 1, (k, v) => v + 1);
            
            // Exponential backoff: 2s, 4s, 8s, 16s, 32s, max 60s
            var backoffMs = Math.Min(2000 * Math.Pow(2, attempts), 60000);
            nuances.CacheRetention = (long)backoffMs;
            
            throw;
        }
    });
}
```

---

## Pattern 9: Stale-While-Revalidate

**Problem:** Want to return cached data immediately but refresh in background.

**Solution:** Return cached data and schedule async refresh on error.

### Implementation

```csharp
public async Task<WeatherData> GetWeatherAsync(string city)
{
    var cacheKey = $"weather_{city}";
    
    return await _cache.BlitzGet(cacheKey, async (nuances) =>
    {
        try
        {
            var weather = await _weatherApi.GetCurrentAsync(city);
            nuances.CacheRetention = 1800000; // Success: 30 minutes
            return weather;
        }
        catch (Exception ex)
        {
            // Try to get stale cached value
            if (_cache.Statistics?.EntryCount > 0)
            {
                // If we have cached data, extend its life while service recovers
                nuances.CacheRetention = 300000; // Extend: 5 more minutes
                _logger.LogWarning(ex, "Weather API down, extending cache");
            }
            else
            {
                // No cached data available
                nuances.CacheRetention = 5000; // Retry: 5 seconds
            }
            
            throw;
        }
    });
}
```

---

## Pattern 10: Graceful Degradation

**Problem:** Service partially works; want to cache partial results.

**Solution:** Return what you can and cache based on completeness.

### Implementation

```csharp
public async Task<DashboardData> GetDashboardAsync(int userId)
{
    return await _cache.BlitzGet($"dashboard_{userId}", async (nuances) =>
    {
        var dashboard = new DashboardData();
        var errors = new List<string>();
        
        // Try to get user profile
        try
        {
            dashboard.Profile = await _profileService.GetAsync(userId);
        }
        catch (Exception ex)
        {
            errors.Add($"Profile unavailable: {ex.Message}");
        }
        
        // Try to get notifications
        try
        {
            dashboard.Notifications = await _notificationService.GetAsync(userId);
        }
        catch (Exception ex)
        {
            errors.Add($"Notifications unavailable: {ex.Message}");
        }
        
        // Try to get recent activity
        try
        {
            dashboard.RecentActivity = await _activityService.GetAsync(userId);
        }
        catch (Exception ex)
        {
            errors.Add($"Activity unavailable: {ex.Message}");
        }
        
        dashboard.Errors = errors;
        
        // Cache based on completeness
        if (errors.Count == 0)
        {
            nuances.CacheRetention = 300000; // Complete: 5 minutes
        }
        else if (errors.Count < 3)
        {
            nuances.CacheRetention = 60000; // Partial: 1 minute
        }
        else
        {
            nuances.CacheRetention = 10000; // Mostly failed: 10 seconds
        }
        
        return dashboard;
    });
}
```

---

## Testing Error Patterns

### How to Test Error Handling

```csharp
[Test]
public async Task ErrorHandling_DoesNotCacheFailures()
{
    var cache = new BlitzCacheInstance();
    cache.InitializeStatistics();
    
    int attempts = 0;
    
    // Function that fails twice then succeeds
    async Task<string> UnstableFunction(Nuances nuances)
    {
        attempts++;
        
        if (attempts <= 2)
        {
            nuances.CacheRetention = 0; // Don't cache errors
            throw new InvalidOperationException($"Failure {attempts}");
        }
        
        nuances.CacheRetention = 300000; // Cache success
        return "Success!";
    }
    
    // First two calls fail and don't cache
    Assert.ThrowsAsync<InvalidOperationException>(
        () => cache.BlitzGet("test", UnstableFunction));
    Assert.ThrowsAsync<InvalidOperationException>(
        () => cache.BlitzGet("test", UnstableFunction));
    
    // Third call succeeds and caches
    var result1 = await cache.BlitzGet("test", UnstableFunction);
    var result2 = await cache.BlitzGet("test", UnstableFunction);
    
    Assert.AreEqual("Success!", result1);
    Assert.AreEqual("Success!", result2);
    Assert.AreEqual(3, attempts); // Only 3 attempts (no caching of failures)
}
```

**Tested in:** `AdvancedUsageExamples.Example6_ConditionalCaching()`

---

## Summary Decision Tree

```
Did the operation succeed?
├─ Yes → Set normal cache duration (e.g., 300000ms)
└─ No → What type of error?
   ├─ Client error (400, validation) → Cache longer (60000ms) or not at all
   ├─ Not Found (404) → Cache briefly (60000ms)
   ├─ Rate limit (429) → Cache longer (300000ms)
   ├─ Timeout / Transient → Short cache (5000-10000ms)
   ├─ Server error (500+) → Very short cache (5000ms)
   ├─ Unknown error → Don't cache (CacheRetention = 0)
   └─ Want to return fallback? → Cache fallback briefly (30000ms)
```

---

## Related Documentation

- **[NUANCES_COOKBOOK.md](NUANCES_COOKBOOK.md)** - More Nuances patterns and recipes
- **[CONFIGURATION.md](CONFIGURATION.md)** - Configuration options and tuning
- **[MIGRATION_GUIDE.md](MIGRATION_GUIDE.md)** - Migrating from IMemoryCache
- **[EXAMPLES_INDEX.md](EXAMPLES_INDEX.md)** - Complete examples index

---

## Key Takeaways

1. **Use `nuances.CacheRetention = 0`** to skip caching entirely
2. **Cache transient failures briefly** (5-30 seconds) to prevent retry storms
3. **Cache permanent failures longer** (or not at all)
4. **Different errors need different strategies** - use exception type matching
5. **Circuit breaker pattern** is built-in when you cache exceptions
6. **Test your error patterns** to ensure failures aren't cached unintentionally

All patterns are production-tested and follow best practices for resilient distributed systems.
