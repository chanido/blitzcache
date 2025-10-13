namespace BlitzCacheCore.Statistics.Memory
{
    /// <summary>
    /// Sizing strategy modes controlling traversal cost vs accuracy.
    /// </summary>
    public enum SizeComputationMode
    {
        Fast = 0,
        Balanced = 1,
        Accurate = 2,
        Adaptive = 3
    }
}
