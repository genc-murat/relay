using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Relay.Core;
using Relay.Core.Tests.Testing;

namespace Relay.Core.Tests.Integration;

/// <summary>
/// Full pipeline integration tests that validate end-to-end functionality
/// </summary>
[IntegrationTest(Category = "FullPipeline")]
public class FullPipelineIntegrationTests
{
    [Fact]
    public async Task FullPipeline_RequestWithHandler_ShouldExecuteSuccessfully()
    {
        // Arrange
        var handler = new IntegrationTestHandler();
        var harness = new RelayTestHarness()
            .AddHandler(handler);
        
        var relay = harness.Build();
        var request = new IntegrationTestRequest { Value = "test input" };

        // Act
        var result = await relay.SendAsync(request);

        // Assert
        result.Should().Be("Processed: test input");
        handler.WasCalled.Should().BeTrue();
        handler.LastRequest.Should().BeSameAs(request);
    }

    [Fact]
    public async Task FullPipeline_RequestWithPipeline_ShouldExecuteInOrder()
    {
        // Arrange
        var handler = new IntegrationTestHandler();
        var pipeline = new IntegrationTestPipeline();
        var harness = new RelayTestHarness()
            .AddHandler(handler)
            .AddPipeline<IntegrationTestPipeline>()
            .AddSingleton(pipeline);
        
        var relay = harness.Build();
        var request = new IntegrationTestRequest { Value = "test input" };

        // Act
        var result = await relay.SendAsync(request);

        // Assert
        result.Should().Be("Pipeline: Processed: test input");
        handler.WasCalled.Should().BeTrue();
        pipeline.WasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task FullPipeline_NotificationWithMultipleHandlers_ShouldExecuteAll()
    {
        // Arrange
        var handler1 = new IntegrationTestNotificationHandler("Handler1");
        var handler2 = new IntegrationTestNotificationHandler("Handler2");
        var harness = new RelayTestHarness()
            .AddHandler(handler1)
            .AddHandler(handler2);
        
        var relay = harness.Build();
        var notification = new IntegrationTestNotification { Message = "test message" };

        // Act
        await relay.PublishAsync(notification);

        // Assert
        handler1.WasCalled.Should().BeTrue();
        handler2.WasCalled.Should().BeTrue();
        handler1.LastNotification.Should().BeSameAs(notification);
        handler2.LastNotification.Should().BeSameAs(notification);
    }

    [Fact]
    public async Task FullPipeline_StreamingWithPipeline_ShouldTransformItems()
    {
        // Arrange
        var handler = new IntegrationTestStreamHandler();
        var pipeline = new IntegrationTestStreamPipeline();
        var harness = new RelayTestHarness()
            .AddHandler(handler)
            .AddPipeline<IntegrationTestStreamPipeline>()
            .AddSingleton(pipeline);
        
        var relay = harness.Build();
        var request = new IntegrationTestStreamRequest { ItemCount = 3 };

        // Act
        var results = new List<int>();
        await foreach (var item in relay.StreamAsync(request))
        {
            results.Add(item);
        }

        // Assert
        results.Should().BeEquivalentTo(new[] { 0, 2, 4 }); // Doubled by pipeline
        handler.WasCalled.Should().BeTrue();
        pipeline.WasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task FullPipeline_WithTelemetry_ShouldRecordMetrics()
    {
        // Arrange
        var handler = new IntegrationTestHandler();
        var harness = new RelayTestHarness()
            .AddHandler(handler); // Uses TestTelemetryProvider by default
        
        var relay = harness.Build();
        var telemetryProvider = harness.GetTestTelemetryProvider();
        var request = new IntegrationTestRequest { Value = "test input" };

        // Act
        var result = await relay.SendAsync(request);

        // Assert
        result.Should().Be("Processed: test input");
        
        // Verify telemetry was recorded
        telemetryProvider.Should().NotBeNull();
        TestUtilities.AssertTelemetryRecorded(telemetryProvider!, typeof(IntegrationTestRequest));
    }

    [Fact]
    public async Task FullPipeline_WithException_ShouldPropagateCorrectly()
    {
        // Arrange
        var handler = new IntegrationTestFailingHandler();
        var harness = new RelayTestHarness()
            .AddHandler(handler);
        
        var relay = harness.Build();
        var request = new IntegrationTestRequest { Value = "fail" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => 
            relay.SendAsync(request).AsTask());
        
        exception.Message.Should().Be("Handler failed");
        handler.WasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task FullPipeline_WithCancellation_ShouldCancelGracefully()
    {
        // Arrange
        var handler = new IntegrationTestDelayHandler();
        var harness = new RelayTestHarness()
            .AddHandler(handler);
        
        var relay = harness.Build();
        var request = new IntegrationTestDelayRequest { DelayMs = 1000 };
        
        using var cts = new CancellationTokenSource(100);

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => 
            relay.SendAsync(request, cts.Token).AsTask());
    }

    [Fact]
    public async Task FullPipeline_MultipleRequestTypes_ShouldRouteCorrectly()
    {
        // Arrange
        var stringHandler = new IntegrationTestHandler();
        var intHandler = new IntegrationTestIntHandler();
        var harness = new RelayTestHarness()
            .AddHandler(stringHandler)
            .AddHandler(intHandler);
        
        var relay = harness.Build();
        var stringRequest = new IntegrationTestRequest { Value = "test" };
        var intRequest = new IntegrationTestIntRequest { Value = 42 };

        // Act
        var stringResult = await relay.SendAsync(stringRequest);
        var intResult = await relay.SendAsync(intRequest);

        // Assert
        stringResult.Should().Be("Processed: test");
        intResult.Should().Be(84); // Doubled
        
        stringHandler.WasCalled.Should().BeTrue();
        intHandler.WasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task FullPipeline_ConcurrentRequests_ShouldHandleCorrectly()
    {
        // Arrange
        var handler = new IntegrationTestHandler();
        var harness = new RelayTestHarness()
            .AddHandler(handler);
        
        var relay = harness.Build();
        var requests = Enumerable.Range(0, 10)
            .Select(i => new IntegrationTestRequest { Value = $"request-{i}" })
            .ToList();

        // Act
        var tasks = requests.Select(r => relay.SendAsync(r).AsTask()).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10);
        results.Should().AllSatisfy(r => r.Should().StartWith("Processed: request-"));
        handler.CallCount.Should().Be(10);
    }

    [Fact]
    public async Task FullPipeline_StreamingWithCancellation_ShouldStopGracefully()
    {
        // Arrange
        var handler = new IntegrationTestStreamHandler();
        var harness = new RelayTestHarness()
            .AddHandler(handler);
        
        var relay = harness.Build();
        var request = new IntegrationTestStreamRequest { ItemCount = 100, DelayMs = 50 };
        
        using var cts = new CancellationTokenSource(200);

        // Act
        var results = new List<int>();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in relay.StreamAsync(request, cts.Token))
            {
                results.Add(item);
            }
        });

        // Assert
        results.Should().NotBeEmpty();
        results.Should().HaveCountLessThan(100); // Should be cancelled before completion
    }

    // Test classes
    private class IntegrationTestRequest : IRequest<string>
    {
        public string Value { get; set; } = string.Empty;
    }

    private class IntegrationTestIntRequest : IRequest<int>
    {
        public int Value { get; set; }
    }

    private class IntegrationTestDelayRequest : IRequest<string>
    {
        public int DelayMs { get; set; }
    }

    private class IntegrationTestStreamRequest : IStreamRequest<int>
    {
        public int ItemCount { get; set; }
        public int DelayMs { get; set; } = 10;
    }

    private class IntegrationTestNotification : INotification
    {
        public string Message { get; set; } = string.Empty;
    }

    private class IntegrationTestHandler : IRequestHandler<IntegrationTestRequest, string>
    {
        public bool WasCalled { get; private set; }
        public IntegrationTestRequest? LastRequest { get; private set; }
        public int CallCount { get; private set; }

        public ValueTask<string> HandleAsync(IntegrationTestRequest request, CancellationToken cancellationToken)
        {
            WasCalled = true;
            LastRequest = request;
            CallCount++;
            return ValueTask.FromResult($"Processed: {request.Value}");
        }
    }

    private class IntegrationTestIntHandler : IRequestHandler<IntegrationTestIntRequest, int>
    {
        public bool WasCalled { get; private set; }

        public ValueTask<int> HandleAsync(IntegrationTestIntRequest request, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return ValueTask.FromResult(request.Value * 2);
        }
    }

    private class IntegrationTestFailingHandler : IRequestHandler<IntegrationTestRequest, string>
    {
        public bool WasCalled { get; private set; }

        public ValueTask<string> HandleAsync(IntegrationTestRequest request, CancellationToken cancellationToken)
        {
            WasCalled = true;
            throw new InvalidOperationException("Handler failed");
        }
    }

    private class IntegrationTestDelayHandler : IRequestHandler<IntegrationTestDelayRequest, string>
    {
        public async ValueTask<string> HandleAsync(IntegrationTestDelayRequest request, CancellationToken cancellationToken)
        {
            await Task.Delay(request.DelayMs, cancellationToken);
            return "completed";
        }
    }

    private class IntegrationTestStreamHandler : IStreamHandler<IntegrationTestStreamRequest, int>
    {
        public bool WasCalled { get; private set; }

        public async IAsyncEnumerable<int> HandleAsync(IntegrationTestStreamRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            WasCalled = true;
            for (int i = 0; i < request.ItemCount; i++)
            {
                if (request.DelayMs > 0)
                {
                    await Task.Delay(request.DelayMs, cancellationToken);
                }
                yield return i;
            }
        }
    }

    private class IntegrationTestNotificationHandler : INotificationHandler<IntegrationTestNotification>
    {
        private readonly string _name;

        public IntegrationTestNotificationHandler(string name)
        {
            _name = name;
        }

        public bool WasCalled { get; private set; }
        public IntegrationTestNotification? LastNotification { get; private set; }

        public ValueTask HandleAsync(IntegrationTestNotification notification, CancellationToken cancellationToken)
        {
            WasCalled = true;
            LastNotification = notification;
            return ValueTask.CompletedTask;
        }
    }

    private class IntegrationTestPipeline : IPipelineBehavior<IntegrationTestRequest, string>
    {
        public bool WasCalled { get; private set; }

        public async ValueTask<string> HandleAsync(IntegrationTestRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            WasCalled = true;
            var result = await next();
            return $"Pipeline: {result}";
        }
    }

    private class IntegrationTestStreamPipeline : IStreamPipelineBehavior<IntegrationTestStreamRequest, int>
    {
        public bool WasCalled { get; private set; }

        public async IAsyncEnumerable<int> HandleAsync(IntegrationTestStreamRequest request, StreamHandlerDelegate<int> next, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            WasCalled = true;
            await foreach (var item in next().WithCancellation(cancellationToken))
            {
                yield return item * 2; // Double each item
            }
        }
    }
}