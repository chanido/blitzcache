using BlitzCacheCore;
using System;

// Simple test to verify new default behavior
var cache = new BlitzCache();

Console.WriteLine($"Statistics enabled by default: {cache.Statistics != null}");
Console.WriteLine($"Statistics value: {cache.Statistics}");

// Test basic functionality still works
var result1 = cache.BlitzGet("test", () => "Hello World!", 5000);
var result2 = cache.BlitzGet("test", () => "Should be cached", 5000);

Console.WriteLine($"First call result: {result1}");
Console.WriteLine($"Second call result: {result2}");
Console.WriteLine($"Results match (cached): {result1 == result2}");

cache.Dispose();

Console.WriteLine("✓ Default behavior test completed successfully!");
