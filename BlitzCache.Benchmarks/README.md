# BlitzCache Benchmarks

This project contains comprehensive performance benchmarks comparing BlitzCache against other popular caching solutions like `MemoryCache` and `LazyCache`.

## 🎯 Purpose

The benchmarks demonstrate BlitzCache's unique value proposition:
- **Thundering Herd Prevention**: Multiple concurrent requests for the same expensive operation execute only once
- **Thread Safety**: Automatic synchronization without manual locking
- **Performance**: Competitive performance across different scenarios

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK or later
- BlitzCache solution built

### Running Benchmarks

```bash
# Run all benchmarks (recommended for accurate results)
dotnet run -c Release

# Run specific benchmark category
dotnet run -c Release --filter "*ConcurrentSameKey*"
dotnet run -c Release --filter "*ConcurrentDifferentKeys*"
dotnet run -c Release --filter "*Expiration*"

# Quick test run for development
dotnet run -c Release --job dry
```

## 📊 Benchmark Categories

### 1. Concurrent Same Key (`ConcurrentSameKeyBenchmark`)
Tests the thundering herd scenario where 100 concurrent tasks request the same cache key.

**Expected Results:**
- **BlitzCache**: Single execution, all threads wait for result
- **MemoryCache**: Multiple executions due to race conditions  
- **LazyCache**: Single execution (similar to BlitzCache)

### 2. Concurrent Different Keys (`ConcurrentDifferentKeysBenchmark`)
Tests performance when 50 concurrent tasks request different cache keys.

**Expected Results:**
- All libraries should perform similarly since no deduplication is possible
- Tests baseline caching performance

### 3. Cache Expiration (`CacheExpirationBenchmark`)
Tests behavior when cache entries expire and need to be refreshed.

**Expected Results:**
- Measures overhead of expiration logic
- Tests cache refresh patterns under load

## 🔧 Customization

### Benchmark Parameters

Edit the benchmark classes to adjust:
- `ConcurrentTasks`: Number of concurrent operations
- `OperationDelayMs`: Simulated expensive operation delay
- `CacheExpirationMs`: Cache entry lifetime

### Adding New Benchmarks

1. Create a new class inheriting from base benchmark patterns
2. Add `[Benchmark]` methods for each caching library
3. Include the class in `Program.cs`

### Example Custom Benchmark

```csharp
[MemoryDiagnoser]
[SimpleJob]
public class CustomBenchmark
{
    [Benchmark(Baseline = true)]
    public async Task BlitzCache_CustomScenario()
    {
        // Your BlitzCache test here
    }

    [Benchmark]
    public async Task MemoryCache_CustomScenario()
    {
        // Your MemoryCache test here
    }
}
```

## 📈 Understanding Results

### Key Metrics
- **Mean**: Average execution time
- **StdDev**: Performance consistency (lower is better)
- **Allocated**: Memory usage per operation
- **Gen 0/1/2**: Garbage collection pressure

### Performance Tips
- Run benchmarks on dedicated hardware for consistent results
- Use Release configuration for accurate performance measurements
- Allow benchmarks to complete warm-up phases
- Consider multiple runs for statistical significance

## 🤝 Contributing

When adding new benchmarks:
1. Follow existing naming conventions
2. Include all three caching libraries for comparison
3. Document expected behavior and results
4. Test thoroughly before submitting

## 📚 References

- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/)
- [BlitzCache Repository](https://github.com/chanido/blitzcache)
- [Microsoft.Extensions.Caching.Memory](https://docs.microsoft.com/en-us/aspnet/core/performance/caching/memory)
- [LazyCache](https://github.com/alastairtree/LazyCache)