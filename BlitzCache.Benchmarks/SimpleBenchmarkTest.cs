using BlitzCacheCore;
using LazyCache;
using Microsoft.Extensions.Caching.Memory;

namespace BlitzCache.Benchmarks;

/// <summary>
/// Simple test to verify that all caching libraries work correctly before running benchmarks.
/// </summary>
public class SimpleBenchmarkTest
{
    public static async Task RunAsync()
    {
        Console.WriteLine("Testing BlitzCache, MemoryCache, and LazyCache basic functionality...");
        
        // Test BlitzCache
        using var blitzCache = new BlitzCacheInstance();
        var blitzResult = await blitzCache.BlitzGet("test", async () =>
        {
            await Task.Delay(10);
            return "BlitzCache works!";
        }, 60000);
        Console.WriteLine($"BlitzCache: {blitzResult}");
        
        // Test MemoryCache
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        if (!memoryCache.TryGetValue("test", out string? memoryCacheResult))
        {
            memoryCacheResult = "MemoryCache works!";
            memoryCache.Set("test", memoryCacheResult, TimeSpan.FromMinutes(1));
        }
        Console.WriteLine($"MemoryCache: {memoryCacheResult}");
        
        // Test LazyCache
        var lazyCache = new CachingService();
        var lazyCacheResult = await lazyCache.GetOrAddAsync("test", async () =>
        {
            await Task.Delay(10);
            return "LazyCache works!";
        }, TimeSpan.FromMinutes(1));
        Console.WriteLine($"LazyCache: {lazyCacheResult}");
        
        Console.WriteLine("All cache libraries are working correctly!");
    }
}