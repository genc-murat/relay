using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.Performance;

namespace Relay.Core.Tests.Performance;

/// <summary>
/// Benchmarks comparing optimized dispatch vs reflection-based dispatch
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[Config(typeof(Config))]
public class DispatcherBenchmarks
{
    private IServiceProvider _serviceProvider = null!;
    private TestRequest _request = null!;
    private TestHandler _handler = null!;
    private MethodInfo _handlerMethod = null!;

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
        services.AddSingleton<TestHandler>();
        services.AddRelayPerformanceOptimizations();
        _serviceProvider = services.BuildServiceProvider();

        _request = new TestRequest { Value = "test" };
        _handler = new TestHandler();
        _handlerMethod = typeof(TestHandler).GetMethod(nameof(TestHandler.HandleAsync))!;
    }

    [Benchmark(Baseline = true)]
    public async Task<TestResponse> DirectCall()
    {
        return await _handler.HandleAsync(_request, CancellationToken.None);
    }

    [Benchmark]
    public async Task<TestResponse> ReflectionCall()
    {
        var result = await (ValueTask<TestResponse>)_handlerMethod.Invoke(_handler, new object[] { _request, CancellationToken.None })!;
        return result;
    }

    [Benchmark]
    public async Task<TestResponse> OptimizedDispatch()
    {
        // This would use the generated optimized dispatcher
        // For now, we'll simulate the optimized path
        return await SimulateOptimizedDispatch(_request);
    }

    [Benchmark]
    public async Task<TestResponse> ServiceProviderResolve()
    {
        var handler = _serviceProvider.GetRequiredService<TestHandler>();
        return await handler.HandleAsync(_request, CancellationToken.None);
    }

    [Benchmark]
    public TestResponse SynchronousCall()
    {
        // Simulate synchronous path optimization
        return new TestResponse { Result = _request.Value + "_processed" };
    }

    [Benchmark]
    public async ValueTask<TestResponse> ValueTaskOptimization()
    {
        // Test ValueTask vs Task performance
        return await new ValueTask<TestResponse>(new TestResponse { Result = _request.Value + "_processed" });
    }

    private async ValueTask<TestResponse> SimulateOptimizedDispatch(TestRequest request)
    {
        // Simulate the generated optimized dispatcher logic
        // In reality, this would be generated code with direct method calls
        var handler = _serviceProvider.GetRequiredService<TestHandler>();
        return await handler.HandleAsync(request, CancellationToken.None);
    }

    // Test classes
    public class TestRequest : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }

    public class TestHandler
    {
        public async ValueTask<TestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            // Simulate some async work
            await Task.Delay(1, cancellationToken);
            return new TestResponse { Result = request.Value + "_processed" };
        }
    }
}

/// <summary>
/// Benchmarks for branch prediction optimization
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class BranchPredictionBenchmarks
{
    private readonly Random _random = new(42); // Fixed seed for consistent results
    private string[] _handlerNames = null!;
    private TestRequest _request = null!;

    [GlobalSetup]
    public void Setup()
    {
        _handlerNames = new[] { "default", "handler1", "handler2", "handler3", "handler4" };
        _request = new TestRequest { Value = "test" };
    }

    [Benchmark(Baseline = true)]
    public string OptimizedHandlerSelection()
    {
        // Simulate optimized handler selection with most common case first
        var handlerName = _handlerNames[_random.Next(_handlerNames.Length)];

        // Most common case first (better branch prediction)
        if (handlerName == "default")
        {
            return ProcessWithDefaultHandler(_request);
        }
        else if (handlerName == "handler1")
        {
            return ProcessWithHandler1(_request);
        }
        else if (handlerName == "handler2")
        {
            return ProcessWithHandler2(_request);
        }
        else if (handlerName == "handler3")
        {
            return ProcessWithHandler3(_request);
        }
        else if (handlerName == "handler4")
        {
            return ProcessWithHandler4(_request);
        }
        else
        {
            throw new InvalidOperationException($"Unknown handler: {handlerName}");
        }
    }

    [Benchmark]
    public string UnoptimizedHandlerSelection()
    {
        // Simulate unoptimized handler selection with dictionary lookup
        var handlerName = _handlerNames[_random.Next(_handlerNames.Length)];

        var handlers = new Dictionary<string, Func<TestRequest, string>>
        {
            ["default"] = ProcessWithDefaultHandler,
            ["handler1"] = ProcessWithHandler1,
            ["handler2"] = ProcessWithHandler2,
            ["handler3"] = ProcessWithHandler3,
            ["handler4"] = ProcessWithHandler4
        };

        return handlers[handlerName](_request);
    }

    private string ProcessWithDefaultHandler(TestRequest request) => request.Value + "_default";
    private string ProcessWithHandler1(TestRequest request) => request.Value + "_handler1";
    private string ProcessWithHandler2(TestRequest request) => request.Value + "_handler2";
    private string ProcessWithHandler3(TestRequest request) => request.Value + "_handler3";
    private string ProcessWithHandler4(TestRequest request) => request.Value + "_handler4";

    public class TestRequest
    {
        public string Value { get; set; } = string.Empty;
    }
}