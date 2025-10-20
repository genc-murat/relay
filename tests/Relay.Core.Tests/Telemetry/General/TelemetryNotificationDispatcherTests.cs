using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

public class TelemetryNotificationDispatcherTests
{
    private readonly Mock<INotificationDispatcher> _innerDispatcherMock;
    private readonly TestTelemetryProvider _telemetryProvider;
    private readonly TelemetryNotificationDispatcher _dispatcher;

    public TelemetryNotificationDispatcherTests()
    {
        _innerDispatcherMock = new Mock<INotificationDispatcher>();
        _telemetryProvider = new TestTelemetryProvider();
        _dispatcher = new TelemetryNotificationDispatcher(_innerDispatcherMock.Object, _telemetryProvider);
    }

    [Fact]
    public void Constructor_WithNullInnerDispatcher_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TelemetryNotificationDispatcher(null!, _telemetryProvider));
    }

    [Fact]
    public void Constructor_WithNullTelemetryProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TelemetryNotificationDispatcher(_innerDispatcherMock.Object, null!));
    }

    [Fact]
    public async Task DispatchAsync_SuccessfulExecution_RecordsTelemetry()
    {
        // Arrange
        var notification = new TestNotification();
        var cancellationToken = CancellationToken.None;

        _innerDispatcherMock
            .Setup(x => x.DispatchAsync<TestNotification>(notification, cancellationToken))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _dispatcher.DispatchAsync(notification, cancellationToken);

        // Assert
        _innerDispatcherMock.Verify(x => x.DispatchAsync<TestNotification>(notification, cancellationToken), Times.Once);

        // Verify telemetry
        Assert.Single(_telemetryProvider.Activities);
        var activity = _telemetryProvider.Activities[0];
        Assert.Equal("Relay.Notification", activity.OperationName);
        Assert.Equal(typeof(TestNotification).FullName, activity.Tags["relay.request_type"]);

        Assert.Single(_telemetryProvider.NotificationPublishes);
        var publish = _telemetryProvider.NotificationPublishes[0];
        Assert.Equal(typeof(TestNotification), publish.NotificationType);
        Assert.Equal(0, publish.HandlerCount); // Placeholder value as noted in implementation
        Assert.True(publish.Success);
        Assert.True(publish.Duration > TimeSpan.Zero);
    }

    [Fact]
    public async Task DispatchAsync_WithException_RecordsFailureTelemetry()
    {
        // Arrange
        var notification = new TestNotification();
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Test exception");

        _innerDispatcherMock
            .Setup(x => x.DispatchAsync<TestNotification>(notification, cancellationToken))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _dispatcher.DispatchAsync(notification, cancellationToken).AsTask());

        Assert.Equal(expectedException, exception);
        _innerDispatcherMock.Verify(x => x.DispatchAsync<TestNotification>(notification, cancellationToken), Times.Once);

        // Verify telemetry
        Assert.Single(_telemetryProvider.Activities);
        Assert.Single(_telemetryProvider.NotificationPublishes);
        var publish = _telemetryProvider.NotificationPublishes[0];
        Assert.False(publish.Success);
        Assert.Equal(expectedException, publish.Exception);
        Assert.True(publish.Duration > TimeSpan.Zero);
    }

    [Fact]
    public async Task DispatchAsync_UsesCorrelationIdFromProvider()
    {
        // Arrange
        var notification = new TestNotification();
        var correlationId = "test-correlation-id";
        var cancellationToken = CancellationToken.None;

        _telemetryProvider.SetCorrelationId(correlationId);
        _innerDispatcherMock
            .Setup(x => x.DispatchAsync(notification, cancellationToken))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _dispatcher.DispatchAsync(notification, cancellationToken);

        // Assert
        Assert.Single(_telemetryProvider.Activities);
        var activity = _telemetryProvider.Activities[0];
        Assert.Equal(correlationId, activity.Tags["relay.correlation_id"]);
    }

    private class TestNotification : INotification
    {
    }
}