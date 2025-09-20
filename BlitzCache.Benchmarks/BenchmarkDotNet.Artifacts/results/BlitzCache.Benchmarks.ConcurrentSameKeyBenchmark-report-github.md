```

BenchmarkDotNet v0.14.0, Ubuntu 24.04.3 LTS (Noble Numbat)
AMD EPYC 7763, 1 CPU, 4 logical and 2 physical cores
.NET SDK 8.0.119
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX2


```
| Method                        | Mean      | Error     | StdDev    | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------:|----------:|----------:|------:|-------:|-------:|----------:|------------:|
| BlitzCache_ConcurrentSameKey  | 12.160 μs | 0.0652 μs | 0.0610 μs |  1.00 | 1.4801 | 0.0305 |  24.38 KB |        1.00 |
| MemoryCache_ConcurrentSameKey |  7.024 μs | 0.0307 μs | 0.0272 μs |  0.58 | 0.5798 | 0.0153 |   9.53 KB |        0.39 |
| LazyCache_ConcurrentSameKey   | 21.557 μs | 0.1140 μs | 0.0890 μs |  1.77 | 3.1128 | 0.0916 |  50.94 KB |        2.09 |
