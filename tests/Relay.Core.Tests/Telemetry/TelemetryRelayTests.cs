using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

public class TelemetryRelayTests
{
    private readonly Mock<IRelay> _innerRelayMock;
    private readonly TestTelemetryProvider _telemetryProvider;
    private readonly TelemetryRelay _relay;

    public TelemetryRelayTests()
    {
        _innerRelayMock = new Mock<IRelay>();
        _telemetryProvider = new TestTelemetryProvider();
        _relay = new TelemetryRelay(_innerRelayMock.Object, _telemetryProvider);
    }

    [Fact]
    public void Constructor_WithNullInnerRelay_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TelemetryRelay(null!, _telemetryProvider));
    }

    [Fact]
    public void Constructor_WithNullTelemetryProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TelemetryRelay(_innerRelayMock.Object, null!));
    }

    [Fact]
    public async Task SendAsync_Generic_SuccessfulExecution_RecordsTelemetry()
    {
        // Arrange
        var request = new TestRequest();
        var expectedResponse = "response";
        var cancellationToken = CancellationToken.None;

        _innerRelayMock
            .Setup(x => x.SendAsync<string>(request, cancellationToken))
            .Returns(ValueTask.FromResult(expectedResponse));

        // Act
        var result = await _relay.SendAsync<string>(request, cancellationToken);

        // Assert
        Assert.Equal(expectedResponse, result);
        _innerRelayMock.Verify(x => x.SendAsync<string>(request, cancellationToken), Times.Once);

        // Verify telemetry
        Assert.Single(_telemetryProvider.Activities);
        var activity = _telemetryProvider.Activities[0];
        Assert.Equal("Relay.Send", activity.OperationName);
        Assert.Equal(typeof(TestRequest).FullName, activity.Tags["relay.request_type"]);

        Assert.Single(_telemetryProvider.HandlerExecutions);
        var execution = _telemetryProvider.HandlerExecutions[0];
        Assert.Equal(typeof(TestRequest), execution.RequestType);
        Assert.Equal(typeof(string), execution.ResponseType);
        Assert.Null(execution.HandlerName);
        Assert.True(execution.Success);
        Assert.True(execution.Duration > TimeSpan.Zero);
    }

    [Fact]
    public async Task SendAsync_Generic_WithException_RecordsFailureTelemetry()
    {
        // Arrange
        var request = new TestRequest();
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Test exception");

        _innerRelayMock
            .Setup(x => x.SendAsync<string>(request, cancellationToken))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _relay.SendAsync<string>(request, cancellationToken).AsTask());

        Assert.Equal(expectedException, exception);

        // Verify telemetry
        Assert.Single(_telemetryProvider.HandlerExecutions);
        var execution = _telemetryProvider.HandlerExecutions[0];
        Assert.False(execution.Success);
        Assert.Equal(expectedException, execution.Exception);
        Assert.True(execution.Duration > TimeSpan.Zero);
    }

    [Fact]
    public async Task SendAsync_NonGeneric_SuccessfulExecution_RecordsTelemetry()
    {
        // Arrange
        var request = new TestRequest();
        var cancellationToken = CancellationToken.None;

        _innerRelayMock
            .Setup(x => x.SendAsync(request, cancellationToken))
            .Returns(ValueTask.FromResult("test"));

        // Act
        await _relay.SendAsync(request, cancellationToken);

        // Assert
        _innerRelayMock.Verify(x => x.SendAsync(request, cancellationToken), Times.Once);

        // Verify telemetry
        Assert.Single(_telemetryProvider.Activities);
        var activity = _telemetryProvider.Activities[0];
        Assert.Equal("Relay.Send", activity.OperationName);

        Assert.Single(_telemetryProvider.HandlerExecutions);
        var execution = _telemetryProvider.HandlerExecutions[0];
        Assert.Equal(typeof(TestRequest), execution.RequestType);
        Assert.Equal(typeof(string), execution.ResponseType);
        Assert.Null(execution.HandlerName);
        Assert.True(execution.Success);
    }

    [Fact]
    public async Task StreamAsync_SuccessfulStreaming_RecordsTelemetry()
    {
        // Arrange
        var request = new TestStreamRequest();
        var expectedItems = new[] { "item1", "item2" };
        var cancellationToken = CancellationToken.None;

        _innerRelayMock
            .Setup(x => x.StreamAsync<string>(request, cancellationToken))
            .Returns(CreateAsyncEnumerable(expectedItems));

        // Act
        var results = new List<string>();
        await foreach (var item in _relay.StreamAsync<string>(request, cancellationToken))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(expectedItems, results);
        _innerRelayMock.Verify(x => x.StreamAsync<string>(request, cancellationToken), Times.Once);

        // Verify telemetry
        Assert.Single(_telemetryProvider.Activities);
        var activity = _telemetryProvider.Activities[0];
        Assert.Equal("Relay.Stream", activity.OperationName);

        Assert.Single(_telemetryProvider.StreamingOperations);
        var operation = _telemetryProvider.StreamingOperations[0];
        Assert.Equal(typeof(TestStreamRequest), operation.RequestType);
        Assert.Equal(typeof(string), operation.ResponseType);
        Assert.Null(operation.HandlerName);
        Assert.Equal(2L, operation.ItemCount);
        Assert.True(operation.Success);
        Assert.True(operation.Duration > TimeSpan.Zero);
    }

    [Fact]
    public async Task PublishAsync_SuccessfulExecution_RecordsTelemetry()
    {
        // Arrange
        var notification = new TestNotification();
        var cancellationToken = CancellationToken.None;

        _innerRelayMock
            .Setup(x => x.PublishAsync(notification, cancellationToken))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _relay.PublishAsync(notification, cancellationToken);

        // Assert
        _innerRelayMock.Verify(x => x.PublishAsync(notification, cancellationToken), Times.Once);

        // Verify telemetry
        Assert.Single(_telemetryProvider.Activities);
        var activity = _telemetryProvider.Activities[0];
        Assert.Equal("Relay.Publish", activity.OperationName);
        Assert.Equal(typeof(TestNotification).FullName, activity.Tags["relay.request_type"]);

        Assert.Single(_telemetryProvider.NotificationPublishes);
        var publish = _telemetryProvider.NotificationPublishes[0];
        Assert.Equal(typeof(TestNotification), publish.NotificationType);
        Assert.Equal(0, publish.HandlerCount); // Placeholder value
        Assert.True(publish.Success);
        Assert.True(publish.Duration > TimeSpan.Zero);
    }

    [Fact]
    public async Task PublishAsync_WithException_RecordsFailureTelemetry()
    {
        // Arrange
        var notification = new TestNotification();
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Test exception");

        _innerRelayMock
            .Setup(x => x.PublishAsync(notification, cancellationToken))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _relay.PublishAsync(notification, cancellationToken).AsTask());

        Assert.Equal(expectedException, exception);

        // Verify telemetry
        Assert.Single(_telemetryProvider.NotificationPublishes);
        var publish = _telemetryProvider.NotificationPublishes[0];
        Assert.False(publish.Success);
        Assert.Equal(expectedException, publish.Exception);
        Assert.True(publish.Duration > TimeSpan.Zero);
    }

    [Fact]
    public async Task Operations_UseCorrelationIdFromProvider()
    {
        // Arrange
        var correlationId = "test-correlation-id";
        _telemetryProvider.SetCorrelationId(correlationId);

        var request = new TestRequest();
        var cancellationToken = CancellationToken.None;

        _innerRelayMock
            .Setup(x => x.SendAsync<string>(request, cancellationToken))
            .Returns(ValueTask.FromResult("response"));

        // Act
        await _relay.SendAsync<string>(request, cancellationToken);

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

    private class TestRequest : IRequest<string>
    {
    }

    private class TestStreamRequest : IStreamRequest<string>
    {
    }

    private class TestNotification : INotification
    {
    }
}