using BenchmarkDotNet.Attributes;
using BlitzCacheCore;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;

namespace BlitzCache.Benchmarks;

/// <summary>
/// Benchmarks cache behavior with expiration scenarios to measure performance
/// when cache entries expire and need to be refreshed.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class CacheExpirationBenchmark
{
    private const int Operations = 100;
    private const int OperationDelayMs = 10;
    private const int CacheExpirationMs = 200; // Short expiration to force refreshes
    
    private IBlitzCacheInstance _blitzCache = null!;
    private IMemoryCache _memoryCache = null!;
    private IAppCache _lazyCache = null!;
    private static int _executionCount;

    [GlobalSetup]
    public void Setup()
    {
        _blitzCache = new BlitzCacheInstance();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _lazyCache = new CachingService();
        _executionCount = 0;
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _blitzCache?.Dispose();
        _memoryCache?.Dispose();
        // LazyCache's IAppCache doesn't implement IDisposable directly
        (_lazyCache as IDisposable)?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public async Task BlitzCache_WithExpiration()
    {
        _executionCount = 0;
        const string cacheKey = "expiring_key";
        
        for (int i = 0; i < Operations; i++)
        {
            await _blitzCache.BlitzGet(cacheKey, ExpensiveOperationAsync, CacheExpirationMs);
            
            // Add small delay to allow some cache entries to expire
            if (i % 10 == 0)
            {
                await Task.Delay(CacheExpirationMs + 50);
            }
        }
    }

    [Benchmark]
    public async Task MemoryCache_WithExpiration()
    {
        _executionCount = 0;
        const string cacheKey = "expiring_key";
        
        for (int i = 0; i < Operations; i++)
        {
            await GetFromMemoryCacheAsync(cacheKey);
            
            // Add small delay to allow some cache entries to expire
            if (i % 10 == 0)
            {
                await Task.Delay(CacheExpirationMs + 50);
            }
        }
    }

    [Benchmark]
    public async Task LazyCache_WithExpiration()
    {
        _executionCount = 0;
        const string cacheKey = "expiring_key";
        
        for (int i = 0; i < Operations; i++)
        {
            await _lazyCache.GetOrAddAsync(cacheKey, ExpensiveOperationAsync, TimeSpan.FromMilliseconds(CacheExpirationMs));
            
            // Add small delay to allow some cache entries to expire
            if (i % 10 == 0)
            {
                await Task.Delay(CacheExpirationMs + 50);
            }
        }
    }

    private async Task<string> ExpensiveOperationAsync()
    {
        Interlocked.Increment(ref _executionCount);
        await Task.Delay(OperationDelayMs);
        return $"Result_{DateTime.Now.Ticks}";
    }

    private async Task<string> GetFromMemoryCacheAsync(string key)
    {
        if (_memoryCache.TryGetValue(key, out string? cachedResult) && cachedResult != null)
        {
            return cachedResult;
        }

        var result = await ExpensiveOperationAsync();
        _memoryCache.Set(key, result, TimeSpan.FromMilliseconds(CacheExpirationMs));
        return result;
    }
}