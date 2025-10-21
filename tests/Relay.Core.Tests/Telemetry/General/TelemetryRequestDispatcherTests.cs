using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

public class TelemetryRequestDispatcherTests
{
    private readonly Mock<IRequestDispatcher> _innerDispatcherMock;
    private readonly TestTelemetryProvider _telemetryProvider;
    private readonly TelemetryRequestDispatcher _dispatcher;

    public TelemetryRequestDispatcherTests()
    {
        _innerDispatcherMock = new Mock<IRequestDispatcher>();
        _telemetryProvider = new TestTelemetryProvider();
        _dispatcher = new TelemetryRequestDispatcher(_innerDispatcherMock.Object, _telemetryProvider);
    }

    [Fact]
    public void Constructor_WithNullInnerDispatcher_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TelemetryRequestDispatcher(null!, _telemetryProvider));
    }

    [Fact]
    public void Constructor_WithNullTelemetryProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new TelemetryRequestDispatcher(_innerDispatcherMock.Object, null!));
    }

    [Fact]
    public async Task DispatchAsync_Generic_SuccessfulExecution_RecordsTelemetry()
    {
        // Arrange
        var request = new TestRequest();
        var expectedResponse = "response";
        var cancellationToken = CancellationToken.None;

        _innerDispatcherMock
            .Setup(x => x.DispatchAsync<string>(request, cancellationToken))
            .Returns(ValueTask.FromResult(expectedResponse));

        // Act
        var result = await _dispatcher.DispatchAsync<string>(request, cancellationToken);

        // Assert
        Assert.Equal(expectedResponse, result);
        _innerDispatcherMock.Verify(x => x.DispatchAsync<string>(request, cancellationToken), Times.Once);

        // Verify telemetry
        Assert.Single(_telemetryProvider.Activities);
        var activity = _telemetryProvider.Activities[0];
        Assert.Equal("Relay.Request", activity.OperationName);
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
    public async Task DispatchAsync_Generic_WithException_RecordsFailureTelemetry()
    {
        // Arrange
        var request = new TestRequest();
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Test exception");

        _innerDispatcherMock
            .Setup(x => x.DispatchAsync<string>(request, cancellationToken))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _dispatcher.DispatchAsync<string>(request, cancellationToken).AsTask());

        Assert.Equal(expectedException, exception);
        _innerDispatcherMock.Verify(x => x.DispatchAsync<string>(request, cancellationToken), Times.Once);

        // Verify telemetry
        Assert.Single(_telemetryProvider.Activities);
        Assert.Single(_telemetryProvider.HandlerExecutions);
        var execution = _telemetryProvider.HandlerExecutions[0];
        Assert.False(execution.Success);
        Assert.Equal(expectedException, execution.Exception);
        Assert.True(execution.Duration > TimeSpan.Zero);
    }

    [Fact]
    public async Task DispatchAsync_NonGeneric_SuccessfulExecution_RecordsTelemetry()
    {
        // Arrange
        var request = new TestRequest();
        var cancellationToken = CancellationToken.None;

        _innerDispatcherMock
            .Setup(x => x.DispatchAsync(request, cancellationToken))
            .Returns(ValueTask.FromResult("test"));

        // Act
        await _dispatcher.DispatchAsync(request, cancellationToken);

        // Assert
        _innerDispatcherMock.Verify(x => x.DispatchAsync(request, cancellationToken), Times.Once);

        // Verify telemetry
        Assert.Single(_telemetryProvider.Activities);
        var activity = _telemetryProvider.Activities[0];
        Assert.Equal("Relay.Request", activity.OperationName);

        Assert.Single(_telemetryProvider.HandlerExecutions);
        var execution = _telemetryProvider.HandlerExecutions[0];
        Assert.Equal(typeof(TestRequest), execution.RequestType);
        Assert.Equal(typeof(string), execution.ResponseType);
        Assert.Null(execution.HandlerName);
        Assert.True(execution.Success);
    }

    [Fact]
    public async Task DispatchAsync_NonGeneric_WithException_RecordsFailureTelemetry()
    {
        // Arrange
        var request = new TestRequest();
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Test exception");

        _innerDispatcherMock
            .Setup(x => x.DispatchAsync(request, cancellationToken))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _dispatcher.DispatchAsync(request, cancellationToken).AsTask());

        Assert.Equal(expectedException, exception);

        // Verify telemetry
        Assert.Single(_telemetryProvider.HandlerExecutions);
        var execution = _telemetryProvider.HandlerExecutions[0];
        Assert.False(execution.Success);
        Assert.Equal(expectedException, execution.Exception);
    }

    [Fact]
    public async Task DispatchAsync_FireAndForget_SuccessfulExecution_RecordsTelemetry()
    {
        // Arrange
        var request = new TestFireAndForgetRequest();
        var cancellationToken = CancellationToken.None;

        _innerDispatcherMock
            .Setup(x => x.DispatchAsync(request, cancellationToken))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _dispatcher.DispatchAsync(request, cancellationToken);

        // Assert
        _innerDispatcherMock.Verify(x => x.DispatchAsync(request, cancellationToken), Times.Once);

        // Verify telemetry
        Assert.Single(_telemetryProvider.Activities);
        var activity = _telemetryProvider.Activities[0];
        Assert.Equal("Relay.Request", activity.OperationName);
        Assert.Equal(typeof(TestFireAndForgetRequest).FullName, activity.Tags["relay.request_type"]);

        Assert.Single(_telemetryProvider.HandlerExecutions);
        var execution = _telemetryProvider.HandlerExecutions[0];
        Assert.Equal(typeof(TestFireAndForgetRequest), execution.RequestType);
        Assert.Null(execution.ResponseType);
        Assert.Null(execution.HandlerName);
        Assert.True(execution.Success);
        Assert.True(execution.Duration > TimeSpan.Zero);
    }

    [Fact]
    public async Task DispatchAsync_FireAndForget_WithException_RecordsFailureTelemetry()
    {
        // Arrange
        var request = new TestFireAndForgetRequest();
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Test exception");

        _innerDispatcherMock
            .Setup(x => x.DispatchAsync(request, cancellationToken))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _dispatcher.DispatchAsync(request, cancellationToken).AsTask());

        Assert.Equal(expectedException, exception);
        _innerDispatcherMock.Verify(x => x.DispatchAsync(request, cancellationToken), Times.Once);

        // Verify telemetry
        Assert.Single(_telemetryProvider.Activities);
        Assert.Single(_telemetryProvider.HandlerExecutions);
        var execution = _telemetryProvider.HandlerExecutions[0];
        Assert.False(execution.Success);
        Assert.Equal(expectedException, execution.Exception);
        Assert.True(execution.Duration > TimeSpan.Zero);
    }

    [Fact]
    public async Task DispatchAsync_FireAndForget_WithHandlerName_SuccessfulExecution_RecordsTelemetryWithHandlerName()
    {
        // Arrange
        var request = new TestFireAndForgetRequest();
        var handlerName = "TestHandler";
        var cancellationToken = CancellationToken.None;

        _innerDispatcherMock
            .Setup(x => x.DispatchAsync(request, handlerName, cancellationToken))
            .Returns(ValueTask.CompletedTask);

        // Act
        await _dispatcher.DispatchAsync(request, handlerName, cancellationToken);

        // Assert
        _innerDispatcherMock.Verify(x => x.DispatchAsync(request, handlerName, cancellationToken), Times.Once);

        // Verify telemetry
        Assert.Single(_telemetryProvider.Activities);
        var activity = _telemetryProvider.Activities[0];
        Assert.Equal("Relay.NamedRequest", activity.OperationName);

        Assert.Single(_telemetryProvider.HandlerExecutions);
        var execution = _telemetryProvider.HandlerExecutions[0];
        Assert.Equal(handlerName, execution.HandlerName);
        Assert.True(execution.Success);
    }

    [Fact]
    public async Task DispatchAsync_FireAndForget_WithHandlerName_WithException_RecordsFailureTelemetry()
    {
        // Arrange
        var request = new TestFireAndForgetRequest();
        var handlerName = "TestHandler";
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Test exception");

        _innerDispatcherMock
            .Setup(x => x.DispatchAsync(request, handlerName, cancellationToken))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _dispatcher.DispatchAsync(request, handlerName, cancellationToken).AsTask());

        Assert.Equal(expectedException, exception);

        // Verify telemetry
        Assert.Single(_telemetryProvider.HandlerExecutions);
        var execution = _telemetryProvider.HandlerExecutions[0];
        Assert.Equal(handlerName, execution.HandlerName);
        Assert.False(execution.Success);
        Assert.Equal(expectedException, execution.Exception);
    }

    [Fact]
    public async Task DispatchAsync_Generic_WithHandlerName_SuccessfulExecution_RecordsTelemetryWithHandlerName()
    {
        // Arrange
        var request = new TestRequest();
        var handlerName = "TestHandler";
        var expectedResponse = "response";
        var cancellationToken = CancellationToken.None;

        _innerDispatcherMock
            .Setup(x => x.DispatchAsync<string>(request, handlerName, cancellationToken))
            .Returns(ValueTask.FromResult(expectedResponse));

        // Act
        var result = await _dispatcher.DispatchAsync<string>(request, handlerName, cancellationToken);

        // Assert
        Assert.Equal(expectedResponse, result);
        _innerDispatcherMock.Verify(x => x.DispatchAsync<string>(request, handlerName, cancellationToken), Times.Once);

        // Verify telemetry
        Assert.Single(_telemetryProvider.Activities);
        var activity = _telemetryProvider.Activities[0];
        Assert.Equal("Relay.NamedRequest", activity.OperationName);

        Assert.Single(_telemetryProvider.HandlerExecutions);
        var execution = _telemetryProvider.HandlerExecutions[0];
        Assert.Equal(handlerName, execution.HandlerName);
        Assert.True(execution.Success);
    }

    [Fact]
    public async Task DispatchAsync_Generic_WithHandlerName_WithException_RecordsFailureTelemetry()
    {
        // Arrange
        var request = new TestRequest();
        var handlerName = "TestHandler";
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Test exception");

        _innerDispatcherMock
            .Setup(x => x.DispatchAsync<string>(request, handlerName, cancellationToken))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _dispatcher.DispatchAsync<string>(request, handlerName, cancellationToken).AsTask());

        Assert.Equal(expectedException, exception);

        // Verify telemetry
        Assert.Single(_telemetryProvider.HandlerExecutions);
        var execution = _telemetryProvider.HandlerExecutions[0];
        Assert.Equal(handlerName, execution.HandlerName);
        Assert.False(execution.Success);
        Assert.Equal(expectedException, execution.Exception);
    }

    [Fact]
    public async Task DispatchAsync_NonGeneric_WithHandlerName_SuccessfulExecution_RecordsTelemetryWithHandlerName()
    {
        // Arrange
        var request = new TestRequest();
        var handlerName = "TestHandler";
        var cancellationToken = CancellationToken.None;

        _innerDispatcherMock
            .Setup(x => x.DispatchAsync<string>(request, handlerName, cancellationToken))
            .Returns(ValueTask.FromResult("test"));

        // Act
        await _dispatcher.DispatchAsync(request, handlerName, cancellationToken);

        // Assert
        _innerDispatcherMock.Verify(x => x.DispatchAsync(request, handlerName, cancellationToken), Times.Once);

        // Verify telemetry
        Assert.Single(_telemetryProvider.Activities);
        var activity = _telemetryProvider.Activities[0];
        Assert.Equal("Relay.NamedRequest", activity.OperationName);

        Assert.Single(_telemetryProvider.HandlerExecutions);
        var execution = _telemetryProvider.HandlerExecutions[0];
        Assert.Equal(handlerName, execution.HandlerName);
        Assert.True(execution.Success);
    }

    [Fact]
    public async Task DispatchAsync_NonGeneric_WithHandlerName_WithException_RecordsFailureTelemetry()
    {
        // Arrange
        var request = new TestRequest();
        var handlerName = "TestHandler";
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Test exception");

        _innerDispatcherMock
            .Setup(x => x.DispatchAsync(request, handlerName, cancellationToken))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _dispatcher.DispatchAsync(request, handlerName, cancellationToken).AsTask());

        Assert.Equal(expectedException, exception);

        // Verify telemetry
        Assert.Single(_telemetryProvider.HandlerExecutions);
        var execution = _telemetryProvider.HandlerExecutions[0];
        Assert.Equal(handlerName, execution.HandlerName);
        Assert.False(execution.Success);
        Assert.Equal(expectedException, execution.Exception);
    }

    [Fact]
    public async Task DispatchAsync_UsesCorrelationIdFromProvider()
    {
        // Arrange
        var request = new TestRequest();
        var expectedResponse = "response";
        var correlationId = "test-correlation-id";
        var cancellationToken = CancellationToken.None;

        _telemetryProvider.SetCorrelationId(correlationId);
        _innerDispatcherMock
            .Setup(x => x.DispatchAsync<string>(request, cancellationToken))
            .Returns(ValueTask.FromResult(expectedResponse));

        // Act
        await _dispatcher.DispatchAsync<string>(request, cancellationToken);

        // Assert
        Assert.Single(_telemetryProvider.Activities);
        var activity = _telemetryProvider.Activities[0];
        Assert.Equal(correlationId, activity.Tags["relay.correlation_id"]);
    }

    private class TestRequest : IRequest<string>
    {
    }

    private class TestFireAndForgetRequest : IRequest
    {
    }
}