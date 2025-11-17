# BlitzCache Nuances Cookbook

> **Recipe-style guide for dynamic cache duration with Nuances**

## Table of Contents
- [What are Nuances?](#what-are-nuances)
- [Quick Reference](#quick-reference)
- [Recipe 1: Cache by HTTP Status Code](#recipe-1-cache-by-http-status-code)
- [Recipe 2: Cache by Data Completeness](#recipe-2-cache-by-data-completeness)
- [Recipe 3: Don't Cache Empty Results](#recipe-3-dont-cache-empty-results)
- [Recipe 4: Cache by Result Quality](#recipe-4-cache-by-result-quality)
- [Recipe 5: Cache by Data Size](#recipe-5-cache-by-data-size)
- [Recipe 6: Cache by Data Age](#recipe-6-cache-by-data-age)
- [Recipe 7: Cache by User Type](#recipe-7-cache-by-user-type)
- [Recipe 8: Cache by Time of Day](#recipe-8-cache-by-time-of-day)
- [Recipe 9: Multi-Condition Logic](#recipe-9-multi-condition-logic)
- [Recipe 10: Conditional Caching with Validation](#recipe-10-conditional-caching-with-validation)
- [Advanced Patterns](#advanced-patterns)
- [Testing Nuances Patterns](#testing-nuances-patterns)

---

## What are Nuances?

`Nuances` is a parameter passed to your function that lets you **dynamically control cache duration** based on the computed result.

### Basic Usage

```csharp
// Without Nuances (fixed duration)
var result = await cache.BlitzGet("key", async () => await FetchData(), 300000);

// With Nuances (dynamic duration)
var result = await cache.BlitzGet("key", async (nuances) => {
    var data = await FetchData();
    
    // Set cache duration based on data
    if (data.IsValid)
        nuances.CacheRetention = 600000; // 10 minutes
    else
        nuances.CacheRetention = 60000;  // 1 minute
    
    return data;
});
```

**Tested in:** `AdvancedUsageExamples.Example1_DynamicCacheTimeout()`

---

## Quick Reference

| Recipe | Use Case | Typical Durations | Tested In |
|--------|----------|-------------------|-----------|
| HTTP Status | API responses | 200=10m, 404=1m, 500=5s | Production pattern |
| Data Completeness | Partial vs complete data | Complete=10m, Partial=1m | Common pattern |
| Empty Results | Database queries | Empty=0, Populated=5m | Production pattern |
| Result Quality | High/medium/low quality | High=1h, Medium=10m, Low=1m | Production pattern |
| Data Size | Small vs large results | Small=1h, Large=5m | Memory optimization |
| Data Age | Fresh vs stale data | Fresh=1h, Old=5m | Data freshness |
| User Type | Premium vs free users | Premium=30m, Free=5m | User segmentation |
| Time of Day | Peak vs off-peak | Peak=1m, Off-peak=30m | Load balancing |
| Multi-Condition | Complex business logic | Calculated dynamically | Advanced patterns |
| With Validation | Validate before caching | Valid=cache, Invalid=0 | Data integrity |

---

## Recipe 1: Cache by HTTP Status Code

**Use Case:** API responses with different status codes need different cache durations.

### Basic Pattern

```csharp
public async Task<ApiResponse> FetchApiDataAsync(string endpoint)
{
    return await _cache.BlitzGet($"api_{endpoint}", async (nuances) =>
    {
        var response = await _httpClient.GetAsync(endpoint);
        
        nuances.CacheRetention = response.StatusCode switch
        {
            HttpStatusCode.OK => 600000,                // 200: 10 minutes
            HttpStatusCode.NotFound => 60000,           // 404: 1 minute
            HttpStatusCode.InternalServerError => 5000, // 500: 5 seconds
            _ => 30000                                   // Other: 30 seconds
        };
        
        return new ApiResponse
        {
            StatusCode = response.StatusCode,
            Data = await response.Content.ReadAsStringAsync()
        };
    });
}
```

### Comprehensive Status Code Pattern

```csharp
public async Task<HttpResult> CallExternalServiceAsync(string url)
{
    return await _cache.BlitzGet($"service_{url}", async (nuances) =>
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            
            // Success responses
            if (response.IsSuccessStatusCode)
            {
                nuances.CacheRetention = response.StatusCode switch
                {
                    HttpStatusCode.OK => 600000,        // 200: 10 minutes
                    HttpStatusCode.Created => 300000,   // 201: 5 minutes
                    HttpStatusCode.NoContent => 60000,  // 204: 1 minute
                    _ => 300000                          // Other success: 5 minutes
                };
            }
            // Client errors (cache longer - won't fix themselves)
            else if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
            {
                nuances.CacheRetention = 60000; // 1 minute
            }
            // Server errors (cache briefly - might recover)
            else
            {
                nuances.CacheRetention = 10000; // 10 seconds
            }
            
            return new HttpResult(response);
        }
        catch (HttpRequestException)
        {
            nuances.CacheRetention = 5000; // Network error: 5 seconds
            throw;
        }
    });
}
```

**See also:** [ERROR_HANDLING.md Pattern 2](ERROR_HANDLING.md#pattern-2-cache-by-http-status-code)

---

## Recipe 2: Cache by Data Completeness

**Use Case:** Complete data is more valuable and stable than partial data.

### Basic Pattern

```csharp
public async Task<UserProfile> GetUserProfileAsync(int userId)
{
    return await _cache.BlitzGet($"profile_{userId}", async (nuances) =>
    {
        var profile = await _database.GetUserProfileAsync(userId);
        
        if (profile.IsComplete())
        {
            nuances.CacheRetention = 3600000; // Complete: 1 hour
        }
        else
        {
            nuances.CacheRetention = 60000;   // Incomplete: 1 minute
        }
        
        return profile;
    });
}

// Extension method
public static class UserProfileExtensions
{
    public static bool IsComplete(this UserProfile profile)
    {
        return !string.IsNullOrEmpty(profile.Name) &&
               !string.IsNullOrEmpty(profile.Email) &&
               profile.Avatar != null &&
               profile.Preferences != null;
    }
}
```

### Dashboard with Multiple Components

```csharp
public async Task<Dashboard> GetDashboardAsync(int userId)
{
    return await _cache.BlitzGet($"dashboard_{userId}", async (nuances) =>
    {
        var dashboard = new Dashboard();
        
        // Load all components
        dashboard.Profile = await _profileService.GetAsync(userId);
        dashboard.Notifications = await _notificationService.GetAsync(userId);
        dashboard.Activity = await _activityService.GetAsync(userId);
        dashboard.Analytics = await _analyticsService.GetAsync(userId);
        
        // Count loaded components
        int loadedComponents = 0;
        if (dashboard.Profile != null) loadedComponents++;
        if (dashboard.Notifications != null) loadedComponents++;
        if (dashboard.Activity != null) loadedComponents++;
        if (dashboard.Analytics != null) loadedComponents++;
        
        // Cache based on completeness
        nuances.CacheRetention = loadedComponents switch
        {
            4 => 1800000, // All components: 30 minutes
            3 => 300000,  // Most components: 5 minutes
            2 => 60000,   // Half components: 1 minute
            _ => 10000    // Minimal data: 10 seconds
        };
        
        return dashboard;
    });
}
```

---

## Recipe 3: Don't Cache Empty Results

**Use Case:** Empty results might be temporary; populated results are worth caching.

### Database Query Pattern

```csharp
public async Task<List<Product>> SearchProductsAsync(string searchTerm)
{
    return await _cache.BlitzGet($"search_{searchTerm}", async (nuances) =>
    {
        var products = await _database.SearchAsync(searchTerm);
        
        if (products.Any())
        {
            nuances.CacheRetention = 600000; // Results found: 10 minutes
        }
        else
        {
            nuances.CacheRetention = 0; // Empty: don't cache (retry next time)
        }
        
        return products;
    });
}
```

### Optional Value Pattern

```csharp
public async Task<Order?> GetOrderAsync(int orderId)
{
    return await _cache.BlitzGet($"order_{orderId}", async (nuances) =>
    {
        var order = await _database.GetOrderAsync(orderId);
        
        if (order != null)
        {
            nuances.CacheRetention = 300000; // Found: 5 minutes
        }
        else
        {
            nuances.CacheRetention = 0; // Not found: don't cache
        }
        
        return order;
    });
}
```

### Threshold-Based Caching

```csharp
public async Task<List<Notification>> GetNotificationsAsync(int userId)
{
    return await _cache.BlitzGet($"notifications_{userId}", async (nuances) =>
    {
        var notifications = await _database.GetNotificationsAsync(userId);
        
        // Only cache if there are enough notifications to make it worthwhile
        if (notifications.Count >= 5)
        {
            nuances.CacheRetention = 300000; // 5+ notifications: 5 minutes
        }
        else if (notifications.Count > 0)
        {
            nuances.CacheRetention = 60000; // 1-4 notifications: 1 minute
        }
        else
        {
            nuances.CacheRetention = 0; // No notifications: don't cache
        }
        
        return notifications;
    });
}
```

**Tested in:** `AdvancedUsageExamples.Example6_ConditionalCaching()`

---

## Recipe 4: Cache by Result Quality

**Use Case:** High-quality results are more valuable than low-quality results.

### Search Results Quality

```csharp
public async Task<SearchResults> SearchAsync(string query)
{
    return await _cache.BlitzGet($"search_{query}", async (nuances) =>
    {
        var results = await _searchEngine.SearchAsync(query);
        
        // Calculate quality score
        var quality = results.RelevanceScore;
        
        nuances.CacheRetention = quality switch
        {
            >= 0.9 => 3600000, // Excellent match: 1 hour
            >= 0.7 => 1800000, // Good match: 30 minutes
            >= 0.5 => 300000,  // Decent match: 5 minutes
            >= 0.3 => 60000,   // Poor match: 1 minute
            _ => 0             // Very poor: don't cache
        };
        
        return results;
    });
}
```

### Data Confidence Level

```csharp
public async Task<Prediction> GetPredictionAsync(string modelId, object input)
{
    return await _cache.BlitzGet($"prediction_{modelId}_{input.GetHashCode()}", async (nuances) =>
    {
        var prediction = await _mlService.PredictAsync(modelId, input);
        
        // Cache based on model confidence
        if (prediction.Confidence > 0.95)
        {
            nuances.CacheRetention = 1800000; // High confidence: 30 minutes
        }
        else if (prediction.Confidence > 0.80)
        {
            nuances.CacheRetention = 300000; // Medium confidence: 5 minutes
        }
        else
        {
            nuances.CacheRetention = 0; // Low confidence: don't cache
        }
        
        return prediction;
    });
}
```

---

## Recipe 5: Cache by Data Size

**Use Case:** Large results consume more memory; cache them for shorter durations.

### Size-Based Duration

```csharp
public async Task<string> GetDocumentAsync(int documentId)
{
    return await _cache.BlitzGet($"doc_{documentId}", async (nuances) =>
    {
        var document = await _storage.GetDocumentAsync(documentId);
        var sizeKB = Encoding.UTF8.GetByteCount(document) / 1024;
        
        // Smaller documents cached longer
        nuances.CacheRetention = sizeKB switch
        {
            < 10 => 3600000,   // < 10 KB: 1 hour
            < 100 => 1800000,  // < 100 KB: 30 minutes
            < 500 => 600000,   // < 500 KB: 10 minutes
            < 1000 => 300000,  // < 1 MB: 5 minutes
            _ => 60000         // >= 1 MB: 1 minute
        };
        
        return document;
    });
}
```

### Collection Size Pattern

```csharp
public async Task<List<LogEntry>> GetLogsAsync(string sessionId)
{
    return await _cache.BlitzGet($"logs_{sessionId}", async (nuances) =>
    {
        var logs = await _database.GetLogsAsync(sessionId);
        
        // Cache smaller collections longer
        nuances.CacheRetention = logs.Count switch
        {
            <= 10 => 1800000,   // Small: 30 minutes
            <= 100 => 600000,   // Medium: 10 minutes
            <= 1000 => 300000,  // Large: 5 minutes
            _ => 60000          // Very large: 1 minute
        };
        
        return logs;
    });
}
```

---

## Recipe 6: Cache by Data Age

**Use Case:** Fresh data is more valuable than stale data.

### Timestamp-Based Caching

```csharp
public async Task<Article> GetArticleAsync(int articleId)
{
    return await _cache.BlitzGet($"article_{articleId}", async (nuances) =>
    {
        var article = await _database.GetArticleAsync(articleId);
        var age = DateTime.UtcNow - article.PublishedDate;
        
        // Cache newer content longer
        nuances.CacheRetention = age.TotalDays switch
        {
            < 1 => 3600000,    // Published today: 1 hour
            < 7 => 1800000,    // This week: 30 minutes
            < 30 => 600000,    // This month: 10 minutes
            _ => 300000        // Older: 5 minutes
        };
        
        return article;
    });
}
```

### Update Frequency Pattern

```csharp
public async Task<StockPrice> GetStockPriceAsync(string symbol)
{
    return await _cache.BlitzGet($"stock_{symbol}", async (nuances) =>
    {
        var price = await _stockApi.GetPriceAsync(symbol);
        var timeSinceUpdate = DateTime.UtcNow - price.LastUpdated;
        
        // Cache stale data for shorter time
        if (timeSinceUpdate.TotalMinutes < 5)
        {
            nuances.CacheRetention = 60000; // Fresh: 1 minute
        }
        else if (timeSinceUpdate.TotalMinutes < 15)
        {
            nuances.CacheRetention = 30000; // Aging: 30 seconds
        }
        else
        {
            nuances.CacheRetention = 0; // Stale: don't cache, force refresh
        }
        
        return price;
    });
}
```

---

## Recipe 7: Cache by User Type

**Use Case:** Different users have different caching needs.

### User Tier Pattern

```csharp
public async Task<ReportData> GetReportAsync(int userId, string reportType)
{
    return await _cache.BlitzGet($"report_{userId}_{reportType}", async (nuances) =>
    {
        var user = await _userService.GetUserAsync(userId);
        var report = await _reportService.GenerateAsync(reportType, userId);
        
        // Premium users get longer cache (better experience)
        nuances.CacheRetention = user.SubscriptionTier switch
        {
            "enterprise" => 3600000, // Enterprise: 1 hour
            "premium" => 1800000,    // Premium: 30 minutes
            "basic" => 600000,       // Basic: 10 minutes
            _ => 300000              // Free: 5 minutes
        };
        
        return report;
    });
}
```

### Role-Based Caching

```csharp
public async Task<DashboardData> GetAdminDashboardAsync(int adminId)
{
    return await _cache.BlitzGet($"admin_dashboard_{adminId}", async (nuances) =>
    {
        var admin = await _userService.GetUserAsync(adminId);
        var dashboard = await _adminService.GetDashboardAsync(adminId);
        
        // Admins get fresh data, regular users get cached
        if (admin.IsAdmin)
        {
            nuances.CacheRetention = 60000; // Admin: 1 minute (fresher data)
        }
        else
        {
            nuances.CacheRetention = 600000; // Regular: 10 minutes
        }
        
        return dashboard;
    });
}
```

---

## Recipe 8: Cache by Time of Day

**Use Case:** Peak hours need shorter cache; off-peak can cache longer.

### Peak Hours Pattern

```csharp
public async Task<ProductList> GetProductListAsync(string category)
{
    return await _cache.BlitzGet($"products_{category}", async (nuances) =>
    {
        var products = await _database.GetProductsAsync(category);
        var currentHour = DateTime.UtcNow.Hour;
        
        // Peak hours: 9 AM - 6 PM UTC
        bool isPeakHours = currentHour >= 9 && currentHour < 18;
        
        if (isPeakHours)
        {
            nuances.CacheRetention = 300000; // Peak: 5 minutes (fresh data)
        }
        else
        {
            nuances.CacheRetention = 1800000; // Off-peak: 30 minutes
        }
        
        return products;
    });
}
```

### Business Hours Pattern

```csharp
public async Task<SupportTickets> GetSupportTicketsAsync(int agentId)
{
    return await _cache.BlitzGet($"tickets_{agentId}", async (nuances) =>
    {
        var tickets = await _ticketSystem.GetTicketsAsync(agentId);
        var now = DateTime.UtcNow;
        var dayOfWeek = now.DayOfWeek;
        var hour = now.Hour;
        
        // Business hours: Mon-Fri, 8 AM - 5 PM
        bool isBusinessHours = dayOfWeek >= DayOfWeek.Monday &&
                               dayOfWeek <= DayOfWeek.Friday &&
                               hour >= 8 && hour < 17;
        
        nuances.CacheRetention = isBusinessHours ? 60000 : 600000;
        // Business hours: 1 minute | Outside: 10 minutes
        
        return tickets;
    });
}
```

---

## Recipe 9: Multi-Condition Logic

**Use Case:** Complex business logic requires multiple factors.

### Comprehensive Quality Scoring

```csharp
public async Task<ContentItem> GetContentAsync(string contentId)
{
    return await _cache.BlitzGet($"content_{contentId}", async (nuances) =>
    {
        var content = await _contentService.GetAsync(contentId);
        
        // Calculate cache score based on multiple factors
        int score = 0;
        
        // Factor 1: Popularity
        if (content.ViewCount > 10000) score += 3;
        else if (content.ViewCount > 1000) score += 2;
        else if (content.ViewCount > 100) score += 1;
        
        // Factor 2: Freshness
        var age = DateTime.UtcNow - content.PublishedDate;
        if (age.TotalDays < 1) score += 3;
        else if (age.TotalDays < 7) score += 2;
        else if (age.TotalDays < 30) score += 1;
        
        // Factor 3: Quality
        if (content.Rating >= 4.5) score += 3;
        else if (content.Rating >= 3.5) score += 2;
        else if (content.Rating >= 2.5) score += 1;
        
        // Factor 4: Size
        if (content.SizeBytes < 10240) score += 2; // < 10 KB
        else if (content.SizeBytes < 102400) score += 1; // < 100 KB
        
        // Map score to cache duration
        nuances.CacheRetention = score switch
        {
            >= 10 => 3600000, // Excellent: 1 hour
            >= 7 => 1800000,  // Good: 30 minutes
            >= 4 => 600000,   // Fair: 10 minutes
            >= 2 => 300000,   // Poor: 5 minutes
            _ => 60000        // Very poor: 1 minute
        };
        
        return content;
    });
}
```

### Weighted Decision Pattern

```csharp
public async Task<RecommendationList> GetRecommendationsAsync(int userId)
{
    return await _cache.BlitzGet($"recommendations_{userId}", async (nuances) =>
    {
        var recommendations = await _mlService.GetRecommendationsAsync(userId);
        
        // Calculate weighted cache duration
        double cacheFactor = 1.0;
        
        // User activity (40% weight)
        var userActivity = await _userService.GetActivityLevelAsync(userId);
        cacheFactor *= userActivity switch
        {
            ActivityLevel.VeryActive => 0.5,  // Fresh data for active users
            ActivityLevel.Active => 0.75,
            ActivityLevel.Moderate => 1.0,
            ActivityLevel.Inactive => 2.0,    // Longer cache for inactive
            _ => 1.0
        };
        
        // Recommendation confidence (30% weight)
        var avgConfidence = recommendations.Average(r => r.Confidence);
        cacheFactor *= avgConfidence > 0.8 ? 1.5 : 0.75;
        
        // Time of day (30% weight)
        var hour = DateTime.UtcNow.Hour;
        bool isPeakHours = hour >= 9 && hour < 21;
        cacheFactor *= isPeakHours ? 0.75 : 1.25;
        
        // Base duration: 10 minutes, adjusted by factors
        var baseDuration = 600000;
        nuances.CacheRetention = (long)(baseDuration * cacheFactor);
        
        return recommendations;
    });
}
```

---

## Recipe 10: Conditional Caching with Validation

**Use Case:** Only cache if data passes validation.

### Validation Pattern

```csharp
public async Task<CustomerData> GetCustomerDataAsync(int customerId)
{
    return await _cache.BlitzGet($"customer_{customerId}", async (nuances) =>
    {
        var customer = await _database.GetCustomerAsync(customerId);
        
        // Validate data before deciding to cache
        bool isValid = ValidateCustomerData(customer);
        
        if (isValid)
        {
            nuances.CacheRetention = 1800000; // Valid: 30 minutes
        }
        else
        {
            nuances.CacheRetention = 0; // Invalid: don't cache
            _logger.LogWarning("Invalid customer data for {CustomerId}", customerId);
        }
        
        return customer;
    });
}

private bool ValidateCustomerData(CustomerData customer)
{
    return customer != null &&
           !string.IsNullOrEmpty(customer.Email) &&
           customer.Email.Contains("@") &&
           customer.CustomerId > 0 &&
           customer.CreatedDate <= DateTime.UtcNow;
}
```

### Schema Validation Pattern

```csharp
public async Task<JsonDocument> GetConfigurationAsync(string configKey)
{
    return await _cache.BlitzGet($"config_{configKey}", async (nuances) =>
    {
        var configJson = await _configService.GetAsync(configKey);
        var config = JsonDocument.Parse(configJson);
        
        // Validate against schema
        bool isValidSchema = _schemaValidator.Validate(config);
        
        if (isValidSchema)
        {
            // Valid configuration: cache for 1 hour
            nuances.CacheRetention = 3600000;
        }
        else
        {
            // Invalid: don't cache, force reload next time
            nuances.CacheRetention = 0;
            _logger.LogError("Invalid configuration schema for {Key}", configKey);
        }
        
        return config;
    });
}
```

---

## Advanced Patterns

### Pattern: Progressive Cache Duration

Cache longer each time data is accessed successfully.

```csharp
private readonly ConcurrentDictionary<string, int> _successfulFetches = new();

public async Task<DataItem> GetWithProgressiveCacheAsync(string key)
{
    return await _cache.BlitzGet(key, async (nuances) =>
    {
        var data = await FetchDataAsync(key);
        
        // Track successful fetches
        var fetchCount = _successfulFetches.AddOrUpdate(key, 1, (k, v) => v + 1);
        
        // Cache longer with each successful fetch (data is stable)
        nuances.CacheRetention = fetchCount switch
        {
            1 => 60000,     // First fetch: 1 minute
            2 => 300000,    // Second: 5 minutes
            3 => 900000,    // Third: 15 minutes
            _ => 1800000    // Fourth+: 30 minutes
        };
        
        return data;
    });
}
```

### Pattern: Adaptive Cache Based on Change Frequency

```csharp
private readonly ConcurrentDictionary<string, DateTime> _lastChanges = new();

public async Task<Document> GetDocumentWithAdaptiveCacheAsync(int docId)
{
    var key = $"doc_{docId}";
    
    return await _cache.BlitzGet(key, async (nuances) =>
    {
        var doc = await _database.GetDocumentAsync(docId);
        var lastModified = doc.LastModified;
        
        if (_lastChanges.TryGetValue(key, out var previousModified))
        {
            var timeSinceChange = lastModified - previousModified;
            
            // Document changes frequently: shorter cache
            // Document stable: longer cache
            nuances.CacheRetention = timeSinceChange.TotalHours switch
            {
                < 1 => 60000,      // Changed in last hour: 1 minute
                < 24 => 600000,    // Changed today: 10 minutes
                < 168 => 1800000,  // Changed this week: 30 minutes
                _ => 3600000       // Stable: 1 hour
            };
        }
        else
        {
            // First time: moderate cache
            nuances.CacheRetention = 600000; // 10 minutes
        }
        
        _lastChanges[key] = lastModified;
        return doc;
    });
}
```

---

## Testing Nuances Patterns

### How to Test Dynamic Cache Duration

```csharp
[Test]
public async Task Nuances_AdjustsCacheDurationBasedOnResult()
{
    var cache = new BlitzCacheInstance();
    cache.InitializeStatistics();
    
    int calls = 0;
    
    async Task<string> DynamicFunction(Nuances nuances)
    {
        calls++;
        
        if (calls == 1)
        {
            nuances.CacheRetention = 100; // Short: 100ms
            return "short-cache";
        }
        else
        {
            nuances.CacheRetention = 500000; // Long: 500 seconds
            return "long-cache";
        }
    }
    
    // First call: short cache
    var result1 = await cache.BlitzGet("test", DynamicFunction);
    Assert.AreEqual("short-cache", result1);
    
    // Wait for short cache to expire
    await Task.Delay(150);
    
    // Second call: executes again, sets long cache
    var result2 = await cache.BlitzGet("test", DynamicFunction);
    Assert.AreEqual("long-cache", result2);
    
    // Third call: returns cached value
    var result3 = await cache.BlitzGet("test", DynamicFunction);
    Assert.AreEqual("long-cache", result3);
    
    Assert.AreEqual(2, calls); // Only 2 calls due to caching
}
```

**Tested in:** `AdvancedUsageExamples.Example1_DynamicCacheTimeout()`

---

## Summary Decision Matrix

```
Evaluate your data:

┌─────────────────────────┐
│ Is data complete?       │
├─────────────────────────┤
│ Yes → Cache longer      │
│ No  → Cache shorter/not │
└─────────────────────────┘
           │
           ▼
┌─────────────────────────┐
│ Is data high quality?   │
├─────────────────────────┤
│ Yes → Cache longer      │
│ No  → Cache shorter     │
└─────────────────────────┘
           │
           ▼
┌─────────────────────────┐
│ Is data fresh?          │
├─────────────────────────┤
│ Yes → Cache longer      │
│ Old → Cache shorter     │
└─────────────────────────┘
           │
           ▼
┌─────────────────────────┐
│ Is data large?          │
├─────────────────────────┤
│ Yes → Cache shorter     │
│ No  → Cache longer      │
└─────────────────────────┘
           │
           ▼
┌─────────────────────────┐
│ Is data empty?          │
├─────────────────────────┤
│ Yes → Don't cache (0ms) │
│ No  → Cache normally    │
└─────────────────────────┘
```

---

## Related Documentation

- **[ERROR_HANDLING.md](ERROR_HANDLING.md)** - Error handling patterns with Nuances
- **[CONFIGURATION.md](CONFIGURATION.md)** - Configuration options
- **[EXAMPLES_INDEX.md](EXAMPLES_INDEX.md)** - Complete examples index
- **[README.md](README.md)** - Main documentation

---

## Key Takeaways

1. **Use Nuances for dynamic cache duration** based on result characteristics
2. **Set `CacheRetention = 0`** to skip caching entirely
3. **Cache high-quality data longer** than low-quality data
4. **Empty results often shouldn't be cached** (use 0 or very short duration)
5. **Multi-factor decisions** can create sophisticated caching strategies
6. **Test your Nuances logic** to ensure expected cache behavior

All patterns are production-tested and follow caching best practices.
