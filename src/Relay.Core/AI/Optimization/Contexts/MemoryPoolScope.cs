using System;
using Microsoft.Extensions.Logging;

namespace Relay.Core.AI
{
    /// <summary>
    /// Helper class providing a scope for memory pooling operations.
    /// </summary>
    public sealed class MemoryPoolScope : IDisposable
    {
        private readonly int _bufferSize;
        private readonly ILogger? _logger;
        private bool _disposed = false;

        public MemoryPoolStatistics Statistics { get; } = new();

        private MemoryPoolScope(MemoryPoolingContext context, ILogger? logger)
        {
            _bufferSize = context.EstimatedBufferSize;
            _logger = logger;
        }

        public static MemoryPoolScope Create(int bufferSize, ILogger? logger)
        {
            return new MemoryPoolScope(
                new MemoryPoolingContext { EstimatedBufferSize = bufferSize },
                logger);
        }

        public static MemoryPoolScope Create(MemoryPoolingContext context, ILogger? logger)
        {
            return new MemoryPoolScope(context, logger);
        }

        /// <summary>
        /// Rents a buffer from the array pool.
        /// </summary>
        public byte[] RentBuffer(int minimumSize)
        {
            var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(minimumSize);
            Statistics.BuffersRented++;
            Statistics.TotalBytesAllocated += buffer.Length;
            return buffer;
        }

        /// <summary>
        /// Returns a buffer to the array pool.
        /// </summary>
        public void ReturnBuffer(byte[] buffer, bool clearArray = false)
        {
            if (buffer == null) return;

            System.Buffers.ArrayPool<byte>.Shared.Return(buffer, clearArray);
            Statistics.BuffersReturned++;
        }

        /// <summary>
        /// Gets current statistics.
        /// </summary>
        public MemoryPoolStatistics GetStatistics()
        {
            return Statistics;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                var efficiency = Statistics.PoolEfficiency;
                _logger?.LogDebug(
                    "Memory pool scope disposed: Rented={Rented}, Returned={Returned}, Efficiency={Efficiency:P2}, Bytes={TotalBytes}",
                    Statistics.BuffersRented, Statistics.BuffersReturned, efficiency, Statistics.TotalBytesAllocated);

                _disposed = true;
            }
        }
    }
}
