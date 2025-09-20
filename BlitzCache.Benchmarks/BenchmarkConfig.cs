using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace BlitzCache.Benchmarks;

/// <summary>
/// Custom benchmark configuration for development and testing.
/// </summary>
public class DebugBenchmarkConfig : ManualConfig
{
    public DebugBenchmarkConfig()
    {
        AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance));
        WithOptions(ConfigOptions.DisableOptimizationsValidator);
    }
}