using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core;
using Relay.Core.Tests.Testing;

namespace Relay.Core.Tests.Performance;

/// <summary>
/// Performance benchmarks for Relay framework components
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net60)]
[PerformanceTest(MaxExecutionTimeMs = 5000, ReleaseModeOnly = true)]
public class RelayPerformanceBenchmarks
{
    private IRelay _relay = null!;
    private BenchmarkRequest _request = null!;
    private BenchmarkNotification _notification = null!;
    private BenchmarkStreamRequest _streamRequest = null!;

    [GlobalSetup]
    public void Setup()
    {
        var handler = new BenchmarkHandler();
        var streamHandler = new BenchmarkStreamHandler();
        var notificationHandler = new BenchmarkNotificationHandler();

        var harness = new RelayTestHarness()
            .AddHandler(handler)
            .AddHandler(streamHandler)
            .AddHandler(notificationHandler)
            .WithoutTelemetry(); // Disable telemetry for pure performance testing

        _relay = harness.Build();
        _request = new BenchmarkRequest { Value = "benchmark" };
        _notification = new BenchmarkNotification { Message = "benchmark" };
        _streamRequest = new BenchmarkStreamRequest { ItemCount = 100 };
    }

    [Benchmark(Baseline = true)]
    public async ValueTask<string> SendRequest()
    {
        return await _relay.SendAsync(_request);
    }

    [Benchmark]
    public async ValueTask SendVoidRequest()
    {
        await _relay.SendAsync(new BenchmarkVoidRequest());
    }

    [Benchmark]
    public async ValueTask PublishNotification()
    {
        await _relay.PublishAsync(_notification);
    }

    [Benchmark]
    public async ValueTask<int> StreamItems()
    {
        var count = 0;
        await foreach (var item in _relay.StreamAsync(_streamRequest))
        {
            count++;
        }
        return count;
    }

    [Benchmark]
    public async ValueTask<string[]> SendMultipleRequests()
    {
        var tasks = new ValueTask<string>[10];
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = _relay.SendAsync(new BenchmarkRequest { Value = $"request-{i}" });
        }

        var results = new string[10];
        for (int i = 0; i < 10; i++)
        {
            results[i] = await tasks[i];
        }
        return results;
    }

    [Benchmark]
    public async ValueTask SendConcurrentRequests()
    {
        var tasks = new Task[100];
        for (int i = 0; i < 100; i++)
        {
            var request = new BenchmarkRequest { Value = $"concurrent-{i}" };
            tasks[i] = _relay.SendAsync(request).AsTask();
        }
        await Task.WhenAll(tasks);
    }

    [Benchmark]
    public async ValueTask PublishMultipleNotifications()
    {
        var tasks = new ValueTask[10];
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = _relay.PublishAsync(new BenchmarkNotification { Message = $"notification-{i}" });
        }

        for (int i = 0; i < 10; i++)
        {
            await tasks[i];
        }
    }

    // Benchmark classes
    private class BenchmarkRequest : IRequest<string>
    {
        public string Value { get; set; } = string.Empty;
    }

    private class BenchmarkVoidRequest : IRequest<Unit>
    {
        public string Value { get; set; } = string.Empty;
    }

    private class BenchmarkStreamRequest : IStreamRequest<int>
    {
        public int ItemCount { get; set; }
    }

    private class BenchmarkNotification : INotification
    {
        public string Message { get; set; } = string.Empty;
    }

    private class BenchmarkHandler : IRequestHandler<BenchmarkRequest, string>
    {
        public ValueTask<string> HandleAsync(BenchmarkRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult($"Processed: {request.Value}");
        }
    }

    private class BenchmarkVoidHandler : IRequestHandler<BenchmarkVoidRequest, Unit>
    {
        public ValueTask<Unit> HandleAsync(BenchmarkVoidRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(Unit.Value);
        }
    }

    private class BenchmarkStreamHandler : IStreamHandler<BenchmarkStreamRequest, int>
    {
        public IAsyncEnumerable<int> HandleAsync(BenchmarkStreamRequest request, CancellationToken cancellationToken)
        {
            return GenerateNumbers(request.ItemCount, cancellationToken);
        }

        private static async IAsyncEnumerable<int> GenerateNumbers(int count, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (int i = 0; i < count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return i;
            }
        }
    }

    private class BenchmarkNotificationHandler : INotificationHandler<BenchmarkNotification>
    {
        public ValueTask HandleAsync(BenchmarkNotification notification, CancellationToken cancellationToken)
        {
            // Minimal processing for benchmark
            return ValueTask.CompletedTask;
        }
    }
}

/// <summary>
/// Memory allocation benchmarks to validate low-allocation pathways
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[PerformanceTest(MaxAllocationBytes = 1024)] // Very strict allocation limit
public class RelayAllocationBenchmarks
{
    private IRelay _relay = null!;
    private SimpleRequest _request = null!;

    [GlobalSetup]
    public void Setup()
    {
        var handler = new SimpleHandler();
        var harness = new RelayTestHarness()
            .AddHandler(handler)
            .WithoutTelemetry();

        _relay = harness.Build();
        _request = new SimpleRequest();
    }

    [Benchmark]
    public async ValueTask<int> MinimalAllocationRequest()
    {
        return await _relay.SendAsync(_request);
    }

    [Benchmark]
    public async ValueTask MinimalAllocationVoidRequest()
    {
        await _relay.SendAsync(new SimpleVoidRequest());
    }

    [Benchmark]
    public async ValueTask MinimalAllocationNotification()
    {
        await _relay.PublishAsync(new SimpleNotification());
    }

    // Simple classes to minimize allocations
    private class SimpleRequest : IRequest<int>
    {
    }

    private class SimpleVoidRequest : IRequest<Unit>
    {
    }

    private class SimpleNotification : INotification
    {
    }

    private class SimpleHandler : IRequestHandler<SimpleRequest, int>
    {
        public ValueTask<int> HandleAsync(SimpleRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(42);
        }
    }

    private class SimpleVoidHandler : IRequestHandler<SimpleVoidRequest, Unit>
    {
        public ValueTask<Unit> HandleAsync(SimpleVoidRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(Unit.Value);
        }
    }

    private class SimpleNotificationHandler : INotificationHandler<SimpleNotification>
    {
        public ValueTask HandleAsync(SimpleNotification notification, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }
}

/// <summary>
/// Throughput benchmarks to measure requests per second
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[PerformanceTest]
public class RelayThroughputBenchmarks
{
    private IRelay _relay = null!;
    private ThroughputRequest[] _requests = null!;

    [Params(100, 1000, 10000)]
    public int RequestCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var handler = new ThroughputHandler();
        var harness = new RelayTestHarness()
            .AddHandler(handler)
            .WithoutTelemetry();

        _relay = harness.Build();
        _requests = new ThroughputRequest[RequestCount];
        for (int i = 0; i < RequestCount; i++)
        {
            _requests[i] = new ThroughputRequest { Id = i };
        }
    }

    [Benchmark]
    public async ValueTask ProcessSequentialRequests()
    {
        for (int i = 0; i < RequestCount; i++)
        {
            await _relay.SendAsync(_requests[i]);
        }
    }

    [Benchmark]
    public async ValueTask ProcessParallelRequests()
    {
        var tasks = new ValueTask<int>[RequestCount];
        for (int i = 0; i < RequestCount; i++)
        {
            tasks[i] = _relay.SendAsync(_requests[i]);
        }

        for (int i = 0; i < RequestCount; i++)
        {
            await tasks[i];
        }
    }

    [Benchmark]
    public async ValueTask ProcessConcurrentRequests()
    {
        var tasks = new Task[RequestCount];
        for (int i = 0; i < RequestCount; i++)
        {
            tasks[i] = _relay.SendAsync(_requests[i]).AsTask();
        }
        await Task.WhenAll(tasks);
    }

    private class ThroughputRequest : IRequest<int>
    {
        public int Id { get; set; }
    }

    private class ThroughputHandler : IRequestHandler<ThroughputRequest, int>
    {
        public ValueTask<int> HandleAsync(ThroughputRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(request.Id * 2);
        }
    }
}