using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Context for SIMD optimization operations.
    /// </summary>
    public sealed class SIMDOptimizationContext
    {
        public bool EnableVectorization { get; init; }
        public int VectorSize { get; init; }
        public bool EnableUnrolling { get; init; }
        public int UnrollFactor { get; init; }
        public int MinDataSize { get; init; }
        public bool IsHardwareAccelerated { get; init; }
        public string[] SupportedVectorTypes { get; init; } = Array.Empty<string>();
    }
}
