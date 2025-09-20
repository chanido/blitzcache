using BenchmarkDotNet.Attributes;
using BlitzCacheCore;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;

namespace BlitzCache.Benchmarks;

/// <summary>
/// Benchmarks concurrent access to different cache keys to measure cache performance
/// when there's no deduplication benefit.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class ConcurrentDifferentKeysBenchmark
{
    private const int ConcurrentTasks = 50;
    private const int OperationDelayMs = 50; // Simulate expensive operation
    
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
    public async Task BlitzCache_ConcurrentDifferentKeys()
    {
        _executionCount = 0;
        var tasks = new Task<string>[ConcurrentTasks];
        
        for (int i = 0; i < ConcurrentTasks; i++)
        {
            int taskId = i;
            tasks[i] = _blitzCache.BlitzGet($"key_{taskId}", ExpensiveOperationAsync, 60000);
        }
        
        await Task.WhenAll(tasks);
        // Execution count should be equal to ConcurrentTasks since all keys are different
    }

    [Benchmark]
    public async Task MemoryCache_ConcurrentDifferentKeys()
    {
        _executionCount = 0;
        var tasks = new Task<string>[ConcurrentTasks];
        
        for (int i = 0; i < ConcurrentTasks; i++)
        {
            int taskId = i;
            tasks[i] = GetFromMemoryCacheAsync($"key_{taskId}");
        }
        
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async Task LazyCache_ConcurrentDifferentKeys()
    {
        _executionCount = 0;
        var tasks = new Task<string>[ConcurrentTasks];
        
        for (int i = 0; i < ConcurrentTasks; i++)
        {
            int taskId = i;
            tasks[i] = _lazyCache.GetOrAddAsync($"key_{taskId}", ExpensiveOperationAsync, TimeSpan.FromMinutes(1));
        }
        
        await Task.WhenAll(tasks);
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
        _memoryCache.Set(key, result, TimeSpan.FromMinutes(1));
        return result;
    }
}