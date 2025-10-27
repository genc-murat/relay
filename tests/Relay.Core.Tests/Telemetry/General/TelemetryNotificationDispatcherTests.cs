using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Configuration;
using Relay.Core.Implementation.Dispatchers;
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
        Assert.Equal(0, publish.HandlerCount); // 0 because inner dispatcher is mocked, not NotificationDispatcher
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

    [Fact]
    public async Task DispatchAsync_WithNotificationDispatcher_RecordsActualHandlerCount()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var notificationDispatcher = new NotificationDispatcher(serviceProvider);
        
        // Register 3 handlers
        for (int i = 0; i < 3; i++)
        {
            var registration = new NotificationHandlerRegistration
            {
                NotificationType = typeof(TestNotification),
                HandlerType = typeof(TestNotificationHandler),
                DispatchMode = NotificationDispatchMode.Parallel,
                Priority = 0,
                HandlerFactory = sp => new TestNotificationHandler(),
                ExecuteHandler = (handler, notification, ct) =>
                    ((TestNotificationHandler)handler).HandleAsync((TestNotification)notification, ct)
            };
            notificationDispatcher.RegisterHandler(registration);
        }

        var telemetryDispatcher = new TelemetryNotificationDispatcher(notificationDispatcher, _telemetryProvider);
        var notification = new TestNotification();

        // Act
        await telemetryDispatcher.DispatchAsync(notification, CancellationToken.None);

        // Assert
        Assert.Single(_telemetryProvider.NotificationPublishes);
        var publish = _telemetryProvider.NotificationPublishes[0];
        Assert.Equal(3, publish.HandlerCount);
        Assert.True(publish.Success);
    }

    [Fact]
    public async Task DispatchAsync_WithNonNotificationDispatcher_RecordsZeroHandlerCount()
    {
        // Arrange
        var notification = new TestNotification();
        _innerDispatcherMock
            .Setup(x => x.DispatchAsync(notification, CancellationToken.None))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _dispatcher.DispatchAsync(notification, CancellationToken.None);

        // Assert
        Assert.Single(_telemetryProvider.NotificationPublishes);
        var publish = _telemetryProvider.NotificationPublishes[0];
        Assert.Equal(0, publish.HandlerCount);
        Assert.True(publish.Success);
    }

    [Fact]
    public async Task DispatchAsync_WithNoHandlers_RecordsZeroHandlerCount()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var notificationDispatcher = new NotificationDispatcher(serviceProvider);
        var telemetryDispatcher = new TelemetryNotificationDispatcher(notificationDispatcher, _telemetryProvider);
        var notification = new TestNotification();

        // Act
        await telemetryDispatcher.DispatchAsync(notification, CancellationToken.None);

        // Assert
        Assert.Single(_telemetryProvider.NotificationPublishes);
        var publish = _telemetryProvider.NotificationPublishes[0];
        Assert.Equal(0, publish.HandlerCount);
        Assert.True(publish.Success);
    }

    [Fact]
    public async Task DispatchAsync_WithMultipleHandlers_RecordsCorrectCount()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var notificationDispatcher = new NotificationDispatcher(serviceProvider);
        
        // Register 5 handlers with different priorities
        for (int i = 0; i < 5; i++)
        {
            var registration = new NotificationHandlerRegistration
            {
                NotificationType = typeof(TestNotification),
                HandlerType = typeof(TestNotificationHandler),
                DispatchMode = NotificationDispatchMode.Sequential,
                Priority = i,
                HandlerFactory = sp => new TestNotificationHandler(),
                ExecuteHandler = (handler, notification, ct) =>
                    ((TestNotificationHandler)handler).HandleAsync((TestNotification)notification, ct)
            };
            notificationDispatcher.RegisterHandler(registration);
        }

        var telemetryDispatcher = new TelemetryNotificationDispatcher(notificationDispatcher, _telemetryProvider);
        var notification = new TestNotification();

        // Act
        await telemetryDispatcher.DispatchAsync(notification, CancellationToken.None);

        // Assert
        Assert.Single(_telemetryProvider.NotificationPublishes);
        var publish = _telemetryProvider.NotificationPublishes[0];
        Assert.Equal(5, publish.HandlerCount);
        Assert.True(publish.Success);
    }

    [Fact]
    public async Task DispatchAsync_WithExceptionAndHandlerCount_RecordsCountInFailure()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var options = new NotificationDispatchOptions { ContinueOnException = false };
        var notificationDispatcher = new NotificationDispatcher(serviceProvider, options);
        
        var executionCount = 0;
        var registration = new NotificationHandlerRegistration
        {
            NotificationType = typeof(TestNotification),
            HandlerType = typeof(TestNotificationHandler),
            DispatchMode = NotificationDispatchMode.Parallel,
            Priority = 0,
            HandlerFactory = sp => new TestNotificationHandler(() =>
            {
                executionCount++;
                throw new InvalidOperationException("Handler failed");
            }),
            ExecuteHandler = (handler, notification, ct) =>
                ((TestNotificationHandler)handler).HandleAsync((TestNotification)notification, ct)
        };
        notificationDispatcher.RegisterHandler(registration);

        var telemetryDispatcher = new TelemetryNotificationDispatcher(notificationDispatcher, _telemetryProvider);
        var notification = new TestNotification();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            telemetryDispatcher.DispatchAsync(notification, CancellationToken.None).AsTask());

        // Assert
        Assert.Single(_telemetryProvider.NotificationPublishes);
        var publish = _telemetryProvider.NotificationPublishes[0];
        Assert.Equal(1, publish.HandlerCount);
        Assert.False(publish.Success);
        Assert.NotNull(publish.Exception);
        Assert.IsType<InvalidOperationException>(publish.Exception);
    }

    [Fact]
    public async Task DispatchAsync_WithDifferentNotificationTypes_RecordsCorrectHandlerCounts()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var notificationDispatcher = new NotificationDispatcher(serviceProvider);
        
        // Register 2 handlers for TestNotification
        for (int i = 0; i < 2; i++)
        {
            var registration = new NotificationHandlerRegistration
            {
                NotificationType = typeof(TestNotification),
                HandlerType = typeof(TestNotificationHandler),
                DispatchMode = NotificationDispatchMode.Parallel,
                Priority = 0,
                HandlerFactory = sp => new TestNotificationHandler(),
                ExecuteHandler = (handler, notification, ct) =>
                    ((TestNotificationHandler)handler).HandleAsync((TestNotification)notification, ct)
            };
            notificationDispatcher.RegisterHandler(registration);
        }

        // Register 3 handlers for AnotherTestNotification
        for (int i = 0; i < 3; i++)
        {
            var registration = new NotificationHandlerRegistration
            {
                NotificationType = typeof(AnotherTestNotification),
                HandlerType = typeof(AnotherTestNotificationHandler),
                DispatchMode = NotificationDispatchMode.Parallel,
                Priority = 0,
                HandlerFactory = sp => new AnotherTestNotificationHandler(),
                ExecuteHandler = (handler, notification, ct) =>
                    ((AnotherTestNotificationHandler)handler).HandleAsync((AnotherTestNotification)notification, ct)
            };
            notificationDispatcher.RegisterHandler(registration);
        }

        var telemetryDispatcher = new TelemetryNotificationDispatcher(notificationDispatcher, _telemetryProvider);

        // Act
        await telemetryDispatcher.DispatchAsync(new TestNotification(), CancellationToken.None);
        await telemetryDispatcher.DispatchAsync(new AnotherTestNotification(), CancellationToken.None);

        // Assert
        Assert.Equal(2, _telemetryProvider.NotificationPublishes.Count);
        Assert.Equal(2, _telemetryProvider.NotificationPublishes[0].HandlerCount);
        Assert.Equal(3, _telemetryProvider.NotificationPublishes[1].HandlerCount);
    }

    private class TestNotification : INotification
    {
    }

    private class AnotherTestNotification : INotification
    {
    }

    private class TestNotificationHandler : INotificationHandler<TestNotification>
    {
        private readonly Action? _onHandle;

        public TestNotificationHandler(Action? onHandle = null)
        {
            _onHandle = onHandle;
        }

        public ValueTask HandleAsync(TestNotification notification, CancellationToken cancellationToken)
        {
            _onHandle?.Invoke();
            return ValueTask.CompletedTask;
        }
    }

    private class AnotherTestNotificationHandler : INotificationHandler<AnotherTestNotification>
    {
        public ValueTask HandleAsync(AnotherTestNotification notification, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }
}