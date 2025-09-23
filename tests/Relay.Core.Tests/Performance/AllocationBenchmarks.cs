using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Performance;
using Relay.Core.Telemetry;

namespace Relay.Core.Tests.Performance;

/// <summary>
/// Benchmarks to measure allocation reduction from object pooling
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[Config(typeof(Config))]
public class AllocationBenchmarks
{
    private ITelemetryContextPool _pool = null!;
    private PooledTelemetryProvider _pooledProvider = null!;
    private DefaultTelemetryProvider _defaultProvider = null!;
    private DefaultPooledBufferManager _bufferManager = null!;

    private class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance));
        }
    }

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddRelayPerformanceOptimizations();
        var provider = services.BuildServiceProvider();

        _pool = provider.GetRequiredService<ITelemetryContextPool>();
        _pooledProvider = new PooledTelemetryProvider(_pool);
        _defaultProvider = new DefaultTelemetryProvider();
        _bufferManager = new DefaultPooledBufferManager();
    }

    [Benchmark(Baseline = true)]
    public TelemetryContext CreateTelemetryContext_New()
    {
        return TelemetryContext.Create(typeof(string), typeof(int), "TestHandler", "correlation-123");
    }

    [Benchmark]
    public TelemetryContext CreateTelemetryContext_Pooled()
    {
        var context = TelemetryContextPool.Create(typeof(string), typeof(int), "TestHandler", "correlation-123");
        TelemetryContextPool.Return(context);
        return context;
    }

    [Benchmark]
    public void RecordHandlerExecution_Default()
    {
        _defaultProvider.RecordHandlerExecution(
            typeof(string),
            typeof(int),
            "TestHandler",
            TimeSpan.FromMilliseconds(50),
            true);
    }

    [Benchmark]
    public void RecordHandlerExecution_Pooled()
    {
        _pooledProvider.RecordHandlerExecution(
            typeof(string),
            typeof(int),
            "TestHandler",
            TimeSpan.FromMilliseconds(50),
            true);
    }

    [Benchmark]
    public byte[] RentBuffer_ArrayPool()
    {
        var buffer = _bufferManager.RentBuffer(1024);
        _bufferManager.ReturnBuffer(buffer);
        return buffer;
    }

    [Benchmark]
    public byte[] CreateBuffer_New()
    {
        return new byte[1024];
    }

    [Benchmark]
    public void TelemetryContextPool_GetReturn()
    {
        var context = _pool.Get();
        _pool.Return(context);
    }

    [Benchmark]
    public void TelemetryContext_CreateNew()
    {
        var context = new TelemetryContext
        {
            RequestType = typeof(string),
            ResponseType = typeof(int),
            HandlerName = "TestHandler",
            CorrelationId = "correlation-123"
        };
        // Simulate some usage
        context.Properties["key"] = "value";
    }

    [Benchmark]
    public Span<byte> RentSpan_Pooled()
    {
        return _bufferManager.RentSpan(512);
    }

    [Benchmark]
    public Span<byte> CreateSpan_New()
    {
        return new byte[512].AsSpan();
    }
}