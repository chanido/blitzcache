using BenchmarkDotNet.Attributes;
using BlitzCacheCore;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace BlitzCache.Benchmarks;

/// <summary>
/// Benchmarks concurrent access to the same cache key to demonstrate BlitzCache's 
/// prevention of the "thundering herd" problem.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class ConcurrentSameKeyBenchmark
{
    private const int ConcurrentTasks = 100;
    private const string CacheKey = "expensive_operation";
    private const int OperationDelayMs = 100; // Simulate expensive operation
    
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
    public async Task BlitzCache_ConcurrentSameKey()
    {
        _executionCount = 0;
        var tasks = new Task<string>[ConcurrentTasks];
        
        for (int i = 0; i < ConcurrentTasks; i++)
        {
            tasks[i] = _blitzCache.BlitzGet(CacheKey, ExpensiveOperationAsync, 60000);
        }
        
        await Task.WhenAll(tasks);
        // With BlitzCache, execution count should be 1 due to automatic deduplication
    }

    [Benchmark]
    public async Task MemoryCache_ConcurrentSameKey()
    {
        _executionCount = 0;
        var tasks = new Task<string>[ConcurrentTasks];
        
        for (int i = 0; i < ConcurrentTasks; i++)
        {
            tasks[i] = GetFromMemoryCacheAsync();
        }
        
        await Task.WhenAll(tasks);
        // With MemoryCache, execution count will likely be close to ConcurrentTasks due to race conditions
    }

    [Benchmark]
    public async Task LazyCache_ConcurrentSameKey()
    {
        _executionCount = 0;
        var tasks = new Task<string>[ConcurrentTasks];
        
        for (int i = 0; i < ConcurrentTasks; i++)
        {
            tasks[i] = _lazyCache.GetOrAddAsync(CacheKey, ExpensiveOperationAsync, TimeSpan.FromMinutes(1));
        }
        
        await Task.WhenAll(tasks);
        // LazyCache should handle this well, but let's see how it compares
    }

    private async Task<string> ExpensiveOperationAsync()
    {
        Interlocked.Increment(ref _executionCount);
        await Task.Delay(OperationDelayMs);
        return $"Result_{DateTime.Now.Ticks}";
    }

    private async Task<string> GetFromMemoryCacheAsync()
    {
        if (_memoryCache.TryGetValue(CacheKey, out string? cachedResult) && cachedResult != null)
        {
            return cachedResult;
        }

        var result = await ExpensiveOperationAsync();
        _memoryCache.Set(CacheKey, result, TimeSpan.FromMinutes(1));
        return result;
    }
}