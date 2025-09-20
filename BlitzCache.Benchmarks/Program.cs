using BenchmarkDotNet.Running;
using BlitzCache.Benchmarks;

// First, run a simple test to verify all libraries work
Console.WriteLine("=== BlitzCache Benchmarks ===");
await SimpleBenchmarkTest.RunAsync();

Console.WriteLine("\nStarting benchmarks...");
BenchmarkRunner.Run<ConcurrentSameKeyBenchmark>();
BenchmarkRunner.Run<ConcurrentDifferentKeysBenchmark>();
BenchmarkRunner.Run<CacheExpirationBenchmark>();
