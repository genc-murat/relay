using System;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
    /// <summary>
    /// Helper class providing a scope for SIMD optimization operations.
    /// </summary>
    public sealed class SIMDOptimizationScope : IDisposable
    {
        private readonly ILogger? _logger;
        private bool _disposed = false;
        private int _vectorOperations = 0;
        private int _scalarOperations = 0;
        private int _totalElementsProcessed = 0;

        public SIMDOptimizationStatistics Statistics { get; } = new();

        private SIMDOptimizationScope(SIMDOptimizationContext context, ILogger? logger)
        {
            _logger = logger;
        }

        public static SIMDOptimizationScope Create(ILogger? logger)
        {
            return new SIMDOptimizationScope(new SIMDOptimizationContext(), logger);
        }

        public static SIMDOptimizationScope Create(SIMDOptimizationContext context, ILogger? logger)
        {
            return new SIMDOptimizationScope(context, logger);
        }

        /// <summary>
        /// Processes data using SIMD when possible, falling back to scalar operations.
        /// </summary>
        public void ProcessData<T>(ReadOnlySpan<T> data, Action<System.Numerics.Vector<T>> vectorAction, Action<T> scalarAction)
            where T : struct
        {
            var vectorSize = System.Numerics.Vector<T>.Count;
            var vectorCount = data.Length / vectorSize;

            // Process vector-aligned data
            for (int i = 0; i < vectorCount; i++)
            {
                var vector = new System.Numerics.Vector<T>(data.Slice(i * vectorSize, vectorSize));
                vectorAction(vector);
                RecordVectorOperation(vectorSize);
            }

            // Process remaining scalar elements
            for (int i = vectorCount * vectorSize; i < data.Length; i++)
            {
                scalarAction(data[i]);
                RecordScalarOperation(1);
            }
        }

        public void RecordVectorOperation(int elementsProcessed)
        {
            System.Threading.Interlocked.Increment(ref _vectorOperations);
            System.Threading.Interlocked.Add(ref _totalElementsProcessed, elementsProcessed);
        }

        public void RecordScalarOperation(int elementsProcessed)
        {
            System.Threading.Interlocked.Increment(ref _scalarOperations);
            System.Threading.Interlocked.Add(ref _totalElementsProcessed, elementsProcessed);
        }

        /// <summary>
        /// Gets current statistics.
        /// </summary>
        public SIMDOptimizationStatistics GetStatistics()
        {
            Statistics.VectorOperations = _vectorOperations;
            Statistics.ScalarOperations = _scalarOperations;
            Statistics.TotalElementsProcessed = _totalElementsProcessed;

            var totalOps = _vectorOperations + _scalarOperations;
            Statistics.VectorizationRatio = totalOps > 0 ? (double)_vectorOperations / totalOps : 0.0;

            // Estimate speedup based on vectorization ratio
            var vectorSize = System.Numerics.Vector<int>.Count;
            Statistics.EstimatedSpeedup = _totalElementsProcessed > 0
                ? (_vectorOperations * vectorSize + _scalarOperations) / (double)_totalElementsProcessed
                : 1.0;

            return Statistics;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Statistics.VectorOperations = _vectorOperations;
                Statistics.ScalarOperations = _scalarOperations;
                Statistics.TotalElementsProcessed = _totalElementsProcessed;

                var totalOps = _vectorOperations + _scalarOperations;
                Statistics.VectorizationRatio = totalOps > 0 ? (double)_vectorOperations / totalOps : 0.0;

                // Estimate speedup based on vectorization ratio
                var vectorSize = System.Numerics.Vector<int>.Count; // Assume typical vector size
                Statistics.EstimatedSpeedup = _totalElementsProcessed > 0
                    ? (_vectorOperations * vectorSize + _scalarOperations) / (double)_totalElementsProcessed
                    : 0.0;

                _logger?.LogDebug(
                    "SIMD optimization scope disposed: Vector Ops={VectorOps}, Scalar Ops={ScalarOps}, Vectorization={Vectorization:P2}, Speedup={Speedup:F2}x",
                    _vectorOperations, _scalarOperations, Statistics.VectorizationRatio, Statistics.EstimatedSpeedup);

                _disposed = true;
            }
        }
    }
}
