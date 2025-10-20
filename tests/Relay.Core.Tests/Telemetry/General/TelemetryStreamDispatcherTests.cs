using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

public class TelemetryStreamDispatcherTests
{
    private readonly Mock<IStreamDispatcher> _innerDispatcherMock;
    private readonly TestTelemetryProvider _telemetryProvider;
    private readonly TelemetryStreamDispatcher _dispatcher;

    public TelemetryStreamDispatcherTests()
    {
        _innerDispatcherMock = new Mock<IStreamDispatcher>();
        _telemetryProvider = new TestTelemetryProvider();
        _dispatcher = new TelemetryStreamDispatcher(_innerDispatcherMock.Object, _telemetryProvider);
    }

    [Fact]
    public void Constructor_WithNullInnerDispatcher_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TelemetryStreamDispatcher(null!, _telemetryProvider));
    }

    [Fact]
    public void Constructor_WithNullTelemetryProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TelemetryStreamDispatcher(_innerDispatcherMock.Object, null!));
    }

    [Fact]
    public async Task DispatchAsync_WithoutHandlerName_SuccessfulStreaming_RecordsTelemetry()
    {
        // Arrange
        var request = new TestStreamRequest();
        var expectedItems = new[] { "item1", "item2", "item3" };
        var cancellationToken = CancellationToken.None;

        _innerDispatcherMock
            .Setup(x => x.DispatchAsync<string>(request, cancellationToken))
            .Returns(CreateAsyncEnumerable(expectedItems));

        // Act
        var results = new List<string>();
        await foreach (var item in _dispatcher.DispatchAsync<string>(request, cancellationToken))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(expectedItems, results);
        _innerDispatcherMock.Verify(x => x.DispatchAsync<string>(request, cancellationToken), Times.Once);

        // Verify telemetry
        Assert.Single(_telemetryProvider.Activities);
        var activity = _telemetryProvider.Activities[0];
        Assert.Equal("Relay.Stream", activity.OperationName);
        Assert.Equal(typeof(TestStreamRequest).FullName, activity.Tags["relay.request_type"]);

        Assert.Single(_telemetryProvider.StreamingOperations);
        var operation = _telemetryProvider.StreamingOperations[0];
        Assert.Equal(typeof(TestStreamRequest), operation.RequestType);
        Assert.Equal(typeof(string), operation.ResponseType);
        Assert.Null(operation.HandlerName);
        Assert.Equal(3L, operation.ItemCount);
        Assert.True(operation.Success);
        Assert.True(operation.Duration > TimeSpan.Zero);
    }

    [Fact]
    public async Task DispatchAsync_WithHandlerName_SuccessfulStreaming_RecordsTelemetryWithHandlerName()
    {
        // Arrange
        var request = new TestStreamRequest();
        var handlerName = "TestHandler";
        var expectedItems = new[] { "item1", "item2" };
        var cancellationToken = CancellationToken.None;

        _innerDispatcherMock
            .Setup(x => x.DispatchAsync<string>(request, handlerName, cancellationToken))
            .Returns(CreateAsyncEnumerable(expectedItems));

        // Act
        var results = new List<string>();
        await foreach (var item in _dispatcher.DispatchAsync<string>(request, handlerName, cancellationToken))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(expectedItems, results);
        _innerDispatcherMock.Verify(x => x.DispatchAsync<string>(request, handlerName, cancellationToken), Times.Once);

        // Verify telemetry
        Assert.Single(_telemetryProvider.Activities);
        var activity = _telemetryProvider.Activities[0];
        Assert.Equal("Relay.NamedStream", activity.OperationName);

        Assert.Single(_telemetryProvider.StreamingOperations);
        var operation = _telemetryProvider.StreamingOperations[0];
        Assert.Equal(handlerName, operation.HandlerName);
        Assert.Equal(2L, operation.ItemCount);
        Assert.True(operation.Success);
    }

    [Fact]
    public async Task DispatchAsync_WithoutHandlerName_WithException_RecordsFailureTelemetry()
    {
        // Arrange
        var request = new TestStreamRequest();
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Test exception");

        _innerDispatcherMock
            .Setup(x => x.DispatchAsync<string>(request, cancellationToken))
            .Returns(CreateFaultyAsyncEnumerable<string>(expectedException));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var item in _dispatcher.DispatchAsync<string>(request, cancellationToken))
            {
                // Consume the stream
            }
        });

        Assert.Equal(expectedException, exception);
        _innerDispatcherMock.Verify(x => x.DispatchAsync<string>(request, cancellationToken), Times.Once);

        // Verify telemetry
        Assert.Single(_telemetryProvider.Activities);
        Assert.Single(_telemetryProvider.StreamingOperations);
        var operation = _telemetryProvider.StreamingOperations[0];
        Assert.False(operation.Success);
        Assert.Equal(expectedException, operation.Exception);
        Assert.True(operation.Duration > TimeSpan.Zero);
    }

    [Fact]
    public async Task DispatchAsync_WithHandlerName_WithException_RecordsFailureTelemetry()
    {
        // Arrange
        var request = new TestStreamRequest();
        var handlerName = "TestHandler";
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Test exception");

        _innerDispatcherMock
            .Setup(x => x.DispatchAsync<string>(request, handlerName, cancellationToken))
            .Returns(CreateFaultyAsyncEnumerable<string>(expectedException));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var item in _dispatcher.DispatchAsync<string>(request, handlerName, cancellationToken))
            {
                // Consume the stream
            }
        });

        Assert.Equal(expectedException, exception);

        // Verify telemetry
        Assert.Single(_telemetryProvider.StreamingOperations);
        var operation = _telemetryProvider.StreamingOperations[0];
        Assert.Equal(handlerName, operation.HandlerName);
        Assert.False(operation.Success);
        Assert.Equal(expectedException, operation.Exception);
    }

    [Fact]
    public async Task DispatchAsync_UsesCorrelationIdFromProvider()
    {
        // Arrange
        var request = new TestStreamRequest();
        var correlationId = "test-correlation-id";
        var expectedItems = new[] { "item1" };
        var cancellationToken = CancellationToken.None;

        _telemetryProvider.SetCorrelationId(correlationId);
        _innerDispatcherMock
            .Setup(x => x.DispatchAsync<string>(request, cancellationToken))
            .Returns(CreateAsyncEnumerable(expectedItems));

        // Act
        await foreach (var item in _dispatcher.DispatchAsync<string>(request, cancellationToken))
        {
            // Consume the stream
        }

        // Assert
        Assert.Single(_telemetryProvider.Activities);
        var activity = _telemetryProvider.Activities[0];
        Assert.Equal(correlationId, activity.Tags["relay.correlation_id"]);
    }

    private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(IEnumerable<T> items, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            yield return item;
        }
    }

    private static async IAsyncEnumerable<string> CreateSuccessfulAsyncEnumerable()
    {
        yield return "item1";
        yield return "item2";
    }

    private static async IAsyncEnumerable<T> CreateFaultyAsyncEnumerable<T>(Exception exception, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield(); // Ensure async execution
        throw exception;
        yield break;
    }

    private class TestStreamRequest : IStreamRequest<string>
    {
    }
}