using System;

namespace BlitzCacheCore.Statistics.Memory
{
    /// <summary>
    /// Configuration options for ObjectGraphValueSizer.
    /// </summary>
    public sealed class ObjectGraphSizerOptions
    {
        public int MaxDepth { get; set; } = 2;
        public int MaxObjects { get; set; } = 512;
        public int MaxSampledElementsPerLevel { get; set; } = 32;
        public int FallbackStructSize { get; set; } = 32;
        public int ObjectHeaderSize { get; set; } = 16;
        public int ReferenceSize { get; set; } = IntPtr.Size;
        public int ArrayHeaderSize { get; set; } = 24;
        public int StringOverhead { get; set; } = 24;
        public bool ReflectIntoStructs { get; set; } = true;
        public SizeComputationMode Mode { get; set; } = SizeComputationMode.Balanced;
    }
}
