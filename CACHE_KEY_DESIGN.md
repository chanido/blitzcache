# BlitzCache Key Design Guide

> **Best practices for designing effective, collision-free cache keys**

## Table of Contents
- [Why Cache Key Design Matters](#why-cache-key-design-matters)
- [Quick Reference](#quick-reference)
- [Good Key Patterns](#good-key-patterns)
- [Bad Key Patterns to Avoid](#bad-key-patterns-to-avoid)
- [Composite Key Strategies](#composite-key-strategies)
- [Key Stability and Consistency](#key-stability-and-consistency)
- [Multi-Tenant Key Design](#multi-tenant-key-design)
- [Hierarchical Key Patterns](#hierarchical-key-patterns)
- [Key Collision Prevention](#key-collision-prevention)
- [Performance Considerations](#performance-considerations)
- [Common Pitfalls](#common-pitfalls)

---

## Why Cache Key Design Matters

Cache keys are the foundation of effective caching. Poor key design leads to:
- **Cache collisions** - Different data sharing the same key
- **Cache misses** - Identical requests using different keys
- **Poor concurrency** - Broad keys causing unnecessary locking
- **Debugging nightmares** - Unclear what's cached under each key

Good key design ensures:
- ✅ **Unique identification** of cached data
- ✅ **Predictable** cache behavior
- ✅ **Optimal concurrency** with granular keys
- ✅ **Easy debugging** and monitoring

---

## Quick Reference

### ✅ Good Patterns
```csharp
"user_{userId}"                           // Simple entity
"product_{productId}_v{version}"          // Versioned data
"tenant_{tenantId}_config_{configId}"     // Multi-tenant
"search_{keyword}_{page}_{pageSize}"      // Query parameters
"api_{endpoint}_{method}_{userId}"        // API calls
```

### ❌ Bad Patterns
```csharp
userObject.ToString()                     // Unstable (object reference)
$"data_{DateTime.Now.Ticks}"             // Never caches (always unique)
$"key_{Guid.NewGuid()}"                  // Random (defeats caching)
"global_data"                             // Too broad (contention)
complexObject.GetHashCode().ToString()   // Collision-prone
```

---

## Good Key Patterns

### Pattern 1: Simple Entity Keys
```csharp
// User by ID
public async Task<User> GetUserAsync(int userId) =>
    await _cache.BlitzGet($"user_{userId}", 
        async () => await _database.GetUserAsync(userId), 
        300000);

// Product by ID
public async Task<Product> GetProductAsync(int productId) =>
    await _cache.BlitzGet($"product_{productId}", 
        async () => await _database.GetProductAsync(productId), 
        600000);

// Order by order number
public async Task<Order> GetOrderAsync(string orderNumber) =>
    await _cache.BlitzGet($"order_{orderNumber}", 
        async () => await _database.GetOrderAsync(orderNumber), 
        300000);
```

**Why good:**
- Unique per entity
- Easy to understand
- Predictable behavior
- Good concurrency (different IDs = different locks)

---

### Pattern 2: Versioned Data Keys
```csharp
// Document with version
public async Task<Document> GetDocumentAsync(int docId, int version) =>
    await _cache.BlitzGet($"doc_{docId}_v{version}", 
        async () => await _storage.GetDocumentVersionAsync(docId, version), 
        3600000);

// Configuration with version
public async Task<AppConfig> GetConfigAsync(string configKey, string version) =>
    await _cache.BlitzGet($"config_{configKey}_v{version}", 
        async () => await _configService.LoadAsync(configKey, version), 
        1800000);
```

**Why good:**
- Supports multiple versions simultaneously
- Cache invalidation via version change
- Clear what version is cached

---

### Pattern 3: Composite Keys (Multiple Parameters)
```csharp
// Search with parameters
public async Task<SearchResults> SearchAsync(string query, int page, int pageSize) =>
    await _cache.BlitzGet($"search_{query}_{page}_{pageSize}", 
        async () => await _searchService.SearchAsync(query, page, pageSize), 
        300000);

// Report with filters
public async Task<Report> GetReportAsync(int userId, string reportType, DateTime date) =>
    await _cache.BlitzGet($"report_{userId}_{reportType}_{date:yyyyMMdd}", 
        async () => await _reportService.GenerateAsync(userId, reportType, date), 
        600000);

// API call with method and parameters
public async Task<ApiResponse> CallApiAsync(string endpoint, string method, int customerId) =>
    await _cache.BlitzGet($"api_{endpoint}_{method}_{customerId}", 
        async () => await _httpClient.SendAsync(endpoint, method, customerId), 
        300000);
```

**Why good:**
- All relevant parameters included
- No accidental collisions
- Different parameter combinations cached separately

---

### Pattern 4: Hierarchical Keys
```csharp
// Tenant-scoped data
public async Task<TenantData> GetTenantDataAsync(int tenantId, string dataType) =>
    await _cache.BlitzGet($"tenant:{tenantId}:data:{dataType}", 
        async () => await _database.GetTenantDataAsync(tenantId, dataType), 
        600000);

// Category-based products
public async Task<List<Product>> GetProductsByCategoryAsync(string category, int page) =>
    await _cache.BlitzGet($"products:category:{category}:page:{page}", 
        async () => await _database.GetProductsAsync(category, page), 
        300000);
```

**Why good:**
- Clear hierarchy
- Easy to invalidate entire subtree
- Self-documenting structure

---

## Bad Key Patterns to Avoid

### ❌ Anti-Pattern 1: Using Object References

**Bad:**
```csharp
// Object.ToString() uses object reference - unstable!
var key = userObject.ToString(); // "MyApp.Models.User" - same for ALL users!
var result = await _cache.BlitzGet(key, () => ProcessUser(userObject), 300000);
```

**Good:**
```csharp
// Use stable properties
var key = $"user_{userObject.Id}";
var result = await _cache.BlitzGet(key, () => ProcessUser(userObject), 300000);
```

---

### ❌ Anti-Pattern 2: Including Timestamps

**Bad:**
```csharp
// DateTime.Now creates unique key every millisecond - never caches!
var key = $"data_{DateTime.Now.Ticks}";
var result = await _cache.BlitzGet(key, () => GetData(), 300000);
```

**Good:**
```csharp
// Use stable identifier
var key = $"data_{dataId}";
var result = await _cache.BlitzGet(key, () => GetData(), 300000);

// If time-based caching needed, use date (not time)
var key = $"daily_report_{DateTime.UtcNow:yyyyMMdd}";
var result = await _cache.BlitzGet(key, () => GetDailyReport(), 3600000);
```

---

### ❌ Anti-Pattern 3: Using Random Values

**Bad:**
```csharp
// Guid.NewGuid() defeats caching - every call is unique!
var key = $"request_{Guid.NewGuid()}";
var result = await _cache.BlitzGet(key, () => ProcessRequest(), 300000);
```

**Good:**
```csharp
// Use request-specific stable identifier
var key = $"request_{requestId}";
var result = await _cache.BlitzGet(key, () => ProcessRequest(requestId), 300000);
```

---

### ❌ Anti-Pattern 4: Using GetHashCode()

**Bad:**
```csharp
// GetHashCode() has collisions and can be negative!
var key = complexObject.GetHashCode().ToString();
var result = await _cache.BlitzGet(key, () => Process(complexObject), 300000);
```

**Good:**
```csharp
// Use stable, meaningful properties
var key = $"process_{complexObject.Id}_{complexObject.Type}";
var result = await _cache.BlitzGet(key, () => Process(complexObject), 300000);

// Or serialize to JSON for complex keys
var key = $"complex_{JsonSerializer.Serialize(new { 
    complexObject.Id, 
    complexObject.Type, 
    complexObject.Category 
})}";
```

---

### ❌ Anti-Pattern 5: Too Broad Keys

**Bad:**
```csharp
// Single key for all users - poor concurrency!
var key = "all_users_data";
var result = await _cache.BlitzGet(key, () => GetAllUsersData(), 300000);
// EVERY user request waits on the same lock!
```

**Good:**
```csharp
// Per-user keys - excellent concurrency
var key = $"user_{userId}_data";
var result = await _cache.BlitzGet(key, () => GetUserData(userId), 300000);
// Each user has independent lock - concurrent execution!
```

---

## Composite Key Strategies

### Strategy 1: Parameter Concatenation
```csharp
// Simple concatenation with delimiter
public async Task<Result> GetDataAsync(string type, int id, string region) =>
    await _cache.BlitzGet($"{type}_{id}_{region}", 
        async () => await FetchDataAsync(type, id, region), 
        300000);
```

### Strategy 2: Structured Keys
```csharp
// Colon-separated hierarchy
public async Task<Data> GetDataAsync(string category, string subcategory, int itemId) =>
    await _cache.BlitzGet($"{category}:{subcategory}:{itemId}", 
        async () => await FetchAsync(category, subcategory, itemId), 
        300000);
```

### Strategy 3: Hash-Based Keys (for complex parameters)
```csharp
public async Task<Result> QueryAsync(ComplexQuery query)
{
    // Create stable hash from query parameters
    var keyData = $"{query.Type}|{query.StartDate}|{query.EndDate}|{string.Join(",", query.Filters)}";
    var keyHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(keyData)));
    var key = $"query_{keyHash}";
    
    return await _cache.BlitzGet(key, 
        async () => await ExecuteQueryAsync(query), 
        600000);
}
```

### Strategy 4: JSON Serialization (for objects)
```csharp
public async Task<Report> GetReportAsync(ReportParameters parameters)
{
    // Serialize parameters to stable JSON
    var keyData = JsonSerializer.Serialize(new
    {
        parameters.ReportType,
        parameters.StartDate,
        parameters.EndDate,
        parameters.Filters
    });
    var key = $"report_{Convert.ToBase64String(Encoding.UTF8.GetBytes(keyData))}";
    
    return await _cache.BlitzGet(key, 
        async () => await GenerateReportAsync(parameters), 
        1800000);
}
```

---

## Key Stability and Consistency

### Principle 1: Deterministic Keys

Same inputs MUST produce same key:

```csharp
// ✅ Deterministic - always produces same key for same inputs
public string CreateKey(int userId, string action)
{
    return $"user_{userId}_action_{action}";
}

// ❌ Non-deterministic - produces different key each time
public string CreateKey(int userId, string action)
{
    return $"user_{userId}_action_{action}_{Guid.NewGuid()}"; // BAD!
}
```

### Principle 2: Case Sensitivity

Be consistent with casing:

```csharp
// ✅ Consistent casing (lowercase)
var key1 = $"product_{productId.ToString().ToLowerInvariant()}";

// ❌ Inconsistent - "Product_123" vs "product_123" are different keys
var key1 = $"Product_{productId}"; // Uppercase P
var key2 = $"product_{productId}"; // Lowercase p
// These are DIFFERENT keys!
```

### Principle 3: Null Handling

Handle null values consistently:

```csharp
// ✅ Consistent null handling
public string CreateKey(int userId, string? optionalTag)
{
    var tag = optionalTag ?? "none";
    return $"user_{userId}_tag_{tag}";
}

// ❌ Inconsistent - null vs "null" string
var key = $"user_{userId}_tag_{optionalTag}"; // Could be "null" or empty
```

---

## Multi-Tenant Key Design

### Pattern: Tenant Prefix

```csharp
public async Task<TenantConfig> GetConfigAsync(int tenantId, string configKey) =>
    await _cache.BlitzGet($"tenant_{tenantId}_config_{configKey}", 
        async () => await _configService.LoadAsync(tenantId, configKey), 
        1800000);

public async Task<List<Order>> GetOrdersAsync(int tenantId, int customerId) =>
    await _cache.BlitzGet($"tenant_{tenantId}_orders_{customerId}", 
        async () => await _database.GetOrdersAsync(tenantId, customerId), 
        300000);
```

**Why important:**
- Prevents cross-tenant data leakage
- Clear tenant isolation
- Easy to invalidate all tenant data

---

## Hierarchical Key Patterns

### Pattern: Domain-Driven Keys

```csharp
// Hierarchy: {domain}:{entity}:{id}:{attribute}

public async Task<UserProfile> GetProfileAsync(int userId) =>
    await _cache.BlitzGet($"users:profile:{userId}", 
        async () => await _database.GetProfileAsync(userId), 
        300000);

public async Task<UserSettings> GetSettingsAsync(int userId) =>
    await _cache.BlitzGet($"users:settings:{userId}", 
        async () => await _database.GetSettingsAsync(userId), 
        600000);

public async Task<UserOrders> GetOrdersAsync(int userId) =>
    await _cache.BlitzGet($"users:orders:{userId}", 
        async () => await _database.GetOrdersAsync(userId), 
        300000);
```

**Benefits:**
- Logical grouping
- Easy to invalidate related data
- Self-documenting structure

---

## Key Collision Prevention

### Technique 1: Use Type Prefixes

```csharp
// ✅ Type prefix prevents collisions
var userKey = $"user_{id}";        // User with ID 123
var productKey = $"product_{id}";  // Product with ID 123
// Different keys even with same ID!

// ❌ No prefix - collision risk
var key1 = $"{userId}";           // "123"
var key2 = $"{productId}";        // "123" - COLLISION!
```

### Technique 2: Include All Discriminators

```csharp
// ✅ All parameters included
public string CreateKey(string region, string category, int id)
{
    return $"{region}_{category}_{id}";
}

// ❌ Missing parameter - possible collision
public string CreateKey(string region, string category, int id)
{
    return $"{category}_{id}"; // Different regions collide!
}
```

### Technique 3: Use Delimiters

```csharp
// ✅ Delimiters prevent ambiguity
var key1 = "product_12_3";   // product 12, variant 3
var key2 = "product_1_23";   // product 1, variant 23
// Clear distinction

// ❌ No delimiters - ambiguous
var key1 = "product123";     // product 12, variant 3?
var key2 = "product123";     // product 1, variant 23?
// COLLISION!
```

---

## Performance Considerations

### Tip 1: Key Length

- **Short keys** = less memory, faster string operations
- **Long keys** = more descriptive but slower

```csharp
// ✅ Balance: descriptive but not excessive
$"user_{userId}_orders_{year}"

// ❌ Too long
$"application_user_management_orders_list_for_user_{userId}_in_year_{year}_with_filters"

// ❌ Too cryptic
$"u{userId}o{year}"  // Hard to debug
```

### Tip 2: Key Format

Use culture-invariant formatting:

```csharp
// ✅ Culture-invariant
var key = $"report_{date:yyyyMMdd}_{amount:F2}";

// ❌ Culture-dependent - different keys in different locales!
var key = $"report_{date}_{amount}";
```

### Tip 3: Avoid Heavy Computation

```csharp
// ❌ Expensive key generation
var key = $"data_{ExpensiveHash(complexObject)}";

// ✅ Pre-compute or use simple properties
var key = $"data_{complexObject.Id}_{complexObject.Version}";
```

---

## Common Pitfalls

### Pitfall 1: Whitespace Handling

```csharp
// ❌ User input with spaces
var searchKey = $"search_{userQuery}"; // "search_hello world" vs "search_hello  world"

// ✅ Normalize whitespace
var normalizedQuery = string.Join("_", userQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries));
var searchKey = $"search_{normalizedQuery}";
```

### Pitfall 2: Special Characters

```csharp
// ❌ User input with special chars
var key = $"file_{fileName}"; // Could contain /\:*?"<>|

// ✅ Sanitize or encode
var safeFileName = Convert.ToBase64String(Encoding.UTF8.GetBytes(fileName));
var key = $"file_{safeFileName}";
```

### Pitfall 3: Collection Order

```csharp
// ❌ Unstable order
var key = $"tags_{string.Join("_", tags)}"; // ["a", "b"] vs ["b", "a"]

// ✅ Sort for stability
var sortedTags = string.Join("_", tags.OrderBy(t => t));
var key = $"tags_{sortedTags}";
```

---

## Summary: Key Design Checklist

✅ **Is the key unique** for different data?  
✅ **Is the key stable** (same inputs = same key)?  
✅ **Is the key readable** (can you debug it)?  
✅ **Does the key include all parameters** that affect the result?  
✅ **Are special characters handled** properly?  
✅ **Is the key culture-invariant** (dates, numbers formatted consistently)?  
✅ **Is the key length reasonable** (not too long, not cryptic)?  
✅ **Does the key support** multi-tenancy if needed?  
✅ **Can you invalidate related keys** easily?  
✅ **Does the key prevent collisions** with type prefixes?

---

## Related Documentation

- **[CONFIGURATION.md](CONFIGURATION.md)** - Configuration options
- **[NUANCES_COOKBOOK.md](NUANCES_COOKBOOK.md)** - Dynamic caching patterns
- **[ERROR_HANDLING.md](ERROR_HANDLING.md)** - Error handling with caching
- **[MIGRATION_GUIDE.md](MIGRATION_GUIDE.md)** - Migrating from IMemoryCache
- **[EXAMPLES_INDEX.md](EXAMPLES_INDEX.md)** - Complete examples

---

## Key Takeaways

1. **Use stable, predictable key patterns** based on entity properties
2. **Include type prefixes** to prevent collisions
3. **Avoid timestamps, GUIDs, random values** in keys
4. **Be consistent** with casing, null handling, formatting
5. **Make keys debuggable** - include enough context
6. **Consider multi-tenancy** - always include tenant ID when relevant
7. **Test your keys** - ensure same inputs produce same keys

Good key design is the foundation of effective caching. Take time to design keys properly from the start!
