using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.RateLimiting.Implementations;

namespace Relay.Core.Tests.Performance;

/// <summary>
/// Benchmarks for rate limiting implementations
/// Compares InMemory, SlidingWindow, and Distributed strategies
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class RateLimitingBenchmarks
{
    private InMemoryRateLimiter _inMemoryLimiter = null!;
    private SlidingWindowRateLimiter _slidingWindowLimiter = null!;
    private const string TestKey = "benchmark-key";

    [GlobalSetup]
    public void Setup()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        _inMemoryLimiter = new InMemoryRateLimiter(cache, NullLogger<InMemoryRateLimiter>.Instance);
        _slidingWindowLimiter = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance,
            requestsPerWindow: 1000,
            windowSeconds: 60);
    }

    [Benchmark(Baseline = true)]
    public async ValueTask<bool> InMemoryRateLimiter_SingleRequest()
    {
        return await _inMemoryLimiter.IsAllowedAsync($"{TestKey}-1");
    }

    [Benchmark]
    public async ValueTask<bool> SlidingWindowRateLimiter_SingleRequest()
    {
        return await _slidingWindowLimiter.IsAllowedAsync($"{TestKey}-2");
    }

    [Benchmark]
    public async ValueTask InMemoryRateLimiter_MultipleKeys()
    {
        for (int i = 0; i < 100; i++)
        {
            await _inMemoryLimiter.IsAllowedAsync($"{TestKey}-multi-{i}");
        }
    }

    [Benchmark]
    public async ValueTask SlidingWindowRateLimiter_MultipleKeys()
    {
        for (int i = 0; i < 100; i++)
        {
            await _slidingWindowLimiter.IsAllowedAsync($"{TestKey}-multi-{i}");
        }
    }

    [Benchmark]
    public async ValueTask InMemoryRateLimiter_Concurrent()
    {
        var tasks = new ValueTask<bool>[10];
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = _inMemoryLimiter.IsAllowedAsync($"{TestKey}-concurrent-{i}");
        }

        for (int i = 0; i < 10; i++)
        {
            await tasks[i];
        }
    }

    [Benchmark]
    public async ValueTask SlidingWindowRateLimiter_Concurrent()
    {
        var tasks = new ValueTask<bool>[10];
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = _slidingWindowLimiter.IsAllowedAsync($"{TestKey}-concurrent-{i}");
        }

        for (int i = 0; i < 10; i++)
        {
            await tasks[i];
        }
    }
}

/// <summary>
/// Benchmarks for buffer management performance
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class BufferManagementBenchmarks
{
    private Relay.Core.Performance.BufferManagement.DefaultPooledBufferManager _defaultManager = null!;
    private Relay.Core.Performance.BufferManagement.OptimizedPooledBufferManager _optimizedManager = null!;

    [Params(256, 1024, 4096, 65536)]
    public int BufferSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _defaultManager = new Relay.Core.Performance.BufferManagement.DefaultPooledBufferManager();
        _optimizedManager = new Relay.Core.Performance.BufferManagement.OptimizedPooledBufferManager();
    }

    [Benchmark(Baseline = true)]
    public void DefaultBufferManager_RentReturn()
    {
        var buffer = _defaultManager.RentBuffer(BufferSize);
        _defaultManager.ReturnBuffer(buffer);
    }

    [Benchmark]
    public void OptimizedBufferManager_RentReturn()
    {
        var buffer = _optimizedManager.RentBuffer(BufferSize);
        _optimizedManager.ReturnBuffer(buffer);
    }

    [Benchmark]
    public void DefaultBufferManager_RentReturnWithClear()
    {
        var buffer = _defaultManager.RentBuffer(BufferSize);
        _defaultManager.ReturnBuffer(buffer, clearArray: true);
    }

    [Benchmark]
    public void OptimizedBufferManager_RentReturnWithClear()
    {
        var buffer = _optimizedManager.RentBuffer(BufferSize);
        _optimizedManager.ReturnBuffer(buffer, clearArray: true);
    }

    [Benchmark]
    public void DefaultBufferManager_MultipleRentReturn()
    {
        for (int i = 0; i < 10; i++)
        {
            var buffer = _defaultManager.RentBuffer(BufferSize);
            _defaultManager.ReturnBuffer(buffer);
        }
    }

    [Benchmark]
    public void OptimizedBufferManager_MultipleRentReturn()
    {
        for (int i = 0; i < 10; i++)
        {
            var buffer = _optimizedManager.RentBuffer(BufferSize);
            _optimizedManager.ReturnBuffer(buffer);
        }
    }
}

/// <summary>
/// Stress test benchmarks for high-throughput scenarios
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 3, iterationCount: 5)]
public class HighThroughputBenchmarks
{
    private SlidingWindowRateLimiter _rateLimiter = null!;
    private Relay.Core.Performance.BufferManagement.OptimizedPooledBufferManager _bufferManager = null!;

    [Params(1000, 10000)]
    public int OperationCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rateLimiter = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance,
            requestsPerWindow: 1000000, // Very high limit for throughput testing
            windowSeconds: 60);

        _bufferManager = new Relay.Core.Performance.BufferManagement.OptimizedPooledBufferManager();
    }

    [Benchmark]
    public async ValueTask RateLimiter_HighThroughput()
    {
        var tasks = new ValueTask<bool>[OperationCount];
        for (int i = 0; i < OperationCount; i++)
        {
            tasks[i] = _rateLimiter.IsAllowedAsync($"key-{i % 100}"); // 100 unique keys
        }

        for (int i = 0; i < OperationCount; i++)
        {
            await tasks[i];
        }
    }

    [Benchmark]
    public void BufferManager_HighThroughput()
    {
        var buffers = new byte[100][];

        for (int i = 0; i < OperationCount; i++)
        {
            var index = i % 100;
            buffers[index] = _bufferManager.RentBuffer(1024);
        }

        for (int i = 0; i < 100; i++)
        {
            if (buffers[i] != null)
                _bufferManager.ReturnBuffer(buffers[i]);
        }
    }

    [Benchmark]
    public async ValueTask Combined_RateLimitAndBuffer()
    {
        for (int i = 0; i < OperationCount / 10; i++)
        {
            var allowed = await _rateLimiter.IsAllowedAsync($"combined-{i % 50}");
            if (allowed)
            {
                var buffer = _bufferManager.RentBuffer(512);
                // Simulate work
                buffer[0] = (byte)i;
                _bufferManager.ReturnBuffer(buffer);
            }
        }
    }
}

/// <summary>
/// Memory efficiency benchmarks
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class MemoryEfficiencyBenchmarks
{
    private SlidingWindowRateLimiter _slidingWindow = null!;
    private Relay.Core.Performance.BufferManagement.OptimizedPooledBufferManager _bufferManager = null!;

    [GlobalSetup]
    public void Setup()
    {
        _slidingWindow = new SlidingWindowRateLimiter(
            NullLogger<SlidingWindowRateLimiter>.Instance);
        _bufferManager = new Relay.Core.Performance.BufferManagement.OptimizedPooledBufferManager();
    }

    [Benchmark]
    public async ValueTask RateLimiter_MemoryFootprint()
    {
        // Test memory footprint of rate limiter with multiple keys
        for (int i = 0; i < 100; i++)
        {
            await _slidingWindow.IsAllowedAsync($"memory-test-{i}");
        }
    }

    [Benchmark]
    public void BufferManager_MemoryFootprint()
    {
        // Test memory footprint of buffer pooling
        var buffers = new byte[50][];
        for (int i = 0; i < 50; i++)
        {
            buffers[i] = _bufferManager.RentBuffer(1024);
        }

        for (int i = 0; i < 50; i++)
        {
            _bufferManager.ReturnBuffer(buffers[i]);
        }
    }

    [Benchmark]
    public async ValueTask SlidingWindow_MultipleWindows()
    {
        // Test behavior with many concurrent windows
        for (int i = 0; i < 50; i++)
        {
            await _slidingWindow.IsAllowedAsync($"window-{i}");
            await _slidingWindow.IsAllowedAsync($"window-{i}");
            await _slidingWindow.IsAllowedAsync($"window-{i}");
        }
    }
}
