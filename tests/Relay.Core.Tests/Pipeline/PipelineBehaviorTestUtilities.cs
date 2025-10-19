using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;

namespace Relay.Core.Tests.Pipeline;

/// <summary>
/// Shared test utilities for PipelineBehavior tests
/// </summary>
public static class PipelineBehaviorTestUtilities
{
    // Test request types
    public class TestRequest : IRequest<string> { }
    public class TestStreamRequest : IStreamRequest<string> { }
}

// Test implementations for pipeline behaviors
public class TestPipelineBehavior : IPipelineBehavior<TestRequest, string>
{
    public List<string> ExecutionOrder { get; } = new();
    private readonly List<string>? _globalExecutionOrder;
    private readonly string _name;

    public TestPipelineBehavior(List<string>? globalExecutionOrder = null, string name = "Behavior")
    {
        _globalExecutionOrder = globalExecutionOrder;
        _name = name;
    }

    public async ValueTask<string> HandleAsync(TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        ExecutionOrder.Add("Before");
        _globalExecutionOrder?.Add($"{_name}_Before");
        var result = await next();
        ExecutionOrder.Add("After");
        _globalExecutionOrder?.Add($"{_name}_After");
        return result + "_Modified";
    }
}

public class TestStreamPipelineBehavior : IStreamPipelineBehavior<TestStreamRequest, string>
{
    public List<string> ExecutionOrder { get; } = new();

    public async IAsyncEnumerable<string> HandleAsync(TestStreamRequest request, StreamHandlerDelegate<string> next, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ExecutionOrder.Add("Before");
        await foreach (var item in next())
        {
            ExecutionOrder.Add($"Item: {item}");
            yield return item + "_Modified";
        }
        ExecutionOrder.Add("After");
    }
}

public class TestSystemModule : ISystemModule
{
    public int Order { get; }
    public List<string> ExecutionOrder { get; } = new();
    private readonly List<string>? _globalExecutionOrder;

    public TestSystemModule(int order = 0, List<string>? globalExecutionOrder = null)
    {
        Order = order;
        _globalExecutionOrder = globalExecutionOrder;
    }

    public async ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ExecutionOrder.Add($"SystemModule_{Order}_Before");
        _globalExecutionOrder?.Add($"SystemModule_{Order}_Before");
        var result = await next();
        ExecutionOrder.Add($"SystemModule_{Order}_After");
        _globalExecutionOrder?.Add($"SystemModule_{Order}_After");
        return result;
    }

    public async IAsyncEnumerable<TResponse> ExecuteStreamAsync<TRequest, TResponse>(TRequest request, StreamHandlerDelegate<TResponse> next, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ExecutionOrder.Add($"SystemModule_{Order}_Stream_Before");
        _globalExecutionOrder?.Add($"SystemModule_{Order}_Stream_Before");
        await foreach (var item in next())
        {
            ExecutionOrder.Add($"SystemModule_{Order}_Stream_Item");
            _globalExecutionOrder?.Add($"SystemModule_{Order}_Stream_Item");
            yield return item;
        }
        ExecutionOrder.Add($"SystemModule_{Order}_Stream_After");
        _globalExecutionOrder?.Add($"SystemModule_{Order}_Stream_After");
    }
}

public class TestSystemModuleWithGlobalOrder : ISystemModule
{
    public int Order { get; }
    private readonly List<string> _globalExecutionOrder;
    private readonly string _name;

    public TestSystemModuleWithGlobalOrder(int order, List<string> globalExecutionOrder, string name)
    {
        Order = order;
        _globalExecutionOrder = globalExecutionOrder;
        _name = name;
    }

    public async ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _globalExecutionOrder.Add($"{_name}_Before");
        var result = await next();
        _globalExecutionOrder.Add($"{_name}_After");
        return result;
    }

    public async IAsyncEnumerable<TResponse> ExecuteStreamAsync<TRequest, TResponse>(TRequest request, StreamHandlerDelegate<TResponse> next, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _globalExecutionOrder.Add($"{_name}_Stream_Before");
        await foreach (var item in next())
        {
            _globalExecutionOrder.Add($"{_name}_Stream_Item");
            yield return item;
        }
        _globalExecutionOrder.Add($"{_name}_Stream_After");
    }
}

public class TestPipelineBehaviorWithGlobalOrder : IPipelineBehavior<TestRequest, string>
{
    private readonly List<string> _globalExecutionOrder;
    private readonly string _name;

    public TestPipelineBehaviorWithGlobalOrder(List<string> globalExecutionOrder, string name)
    {
        _globalExecutionOrder = globalExecutionOrder;
        _name = name;
    }

    public async ValueTask<string> HandleAsync(TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        _globalExecutionOrder.Add($"{_name}_Before");
        var result = await next();
        _globalExecutionOrder.Add($"{_name}_After");
        return result + "_Modified";
    }
}

public class TestStreamPipelineBehaviorWithGlobalOrder : IStreamPipelineBehavior<TestStreamRequest, string>
{
    private readonly List<string> _globalExecutionOrder;
    private readonly string _name;

    public TestStreamPipelineBehaviorWithGlobalOrder(List<string> globalExecutionOrder, string name)
    {
        _globalExecutionOrder = globalExecutionOrder;
        _name = name;
    }

    public async IAsyncEnumerable<string> HandleAsync(TestStreamRequest request, StreamHandlerDelegate<string> next, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _globalExecutionOrder.Add($"{_name}_Before");
        await foreach (var item in next())
        {
            _globalExecutionOrder.Add($"{_name}_Item: {item}");
            yield return item + "_Modified";
        }
        _globalExecutionOrder.Add($"{_name}_After");
    }
}

public class TestPipelineBehaviorWithCancellationCheck : IPipelineBehavior<TestRequest, string>
{
    private readonly List<CancellationToken> _receivedTokens;

    public TestPipelineBehaviorWithCancellationCheck(List<CancellationToken> receivedTokens)
    {
        _receivedTokens = receivedTokens;
    }

    public async ValueTask<string> HandleAsync(TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        _receivedTokens.Add(cancellationToken);
        var result = await next();
        return result + "_Modified";
    }
}

public class TestPipelineBehaviorThatThrows : IPipelineBehavior<TestRequest, string>
{
    public async ValueTask<string> HandleAsync(TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Pipeline behavior exception");
    }
}

public class TestSystemModuleThatThrows : ISystemModule
{
    public int Order => 1;

    public async ValueTask<TResponse> ExecuteAsync<TRequest, TResponse>(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("System module exception");
    }

    public IAsyncEnumerable<TResponse> ExecuteStreamAsync<TRequest, TResponse>(TRequest request, StreamHandlerDelegate<TResponse> next, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("System module exception");
    }
}

public class TestPipelineBehaviorWithDelay : IPipelineBehavior<TestRequest, string>
{
    public async ValueTask<string> HandleAsync(TestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken); // Long delay to allow cancellation
        return await next();
    }
}