using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Configuration.Options.Core;
using Relay.Core.Configuration.Options.DistributedTracing;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.DistributedTracing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type
#pragma warning disable CS8602 // Dereference of a possibly null reference

namespace Relay.Core.Tests.DistributedTracing;

public class DistributedTracingPipelineBehaviorTests
{
    private readonly Mock<IDistributedTracingProvider> _mockTracingProvider;
    private readonly Mock<ILogger<DistributedTracingPipelineBehavior<TestRequest, TestResponse>>> _mockLogger;
    private readonly Mock<IOptions<RelayOptions>> _mockOptions;

    public DistributedTracingPipelineBehaviorTests()
    {
        _mockTracingProvider = new Mock<IDistributedTracingProvider>();
        _mockLogger = new Mock<ILogger<DistributedTracingPipelineBehavior<TestRequest, TestResponse>>>();
        _mockOptions = new Mock<IOptions<RelayOptions>>();

        var relayOptions = new RelayOptions
        {
            DefaultDistributedTracingOptions = new DistributedTracingOptions
            {
                EnableAutomaticDistributedTracing = true,
                TraceRequests = true,
                TraceResponses = true
            }
        };
        _mockOptions.Setup(x => x.Value).Returns(relayOptions);
    }

    [Fact]
    public async Task HandleAsync_ShouldStartActivity_WhenTracingEnabled()
    {
        // Arrange
        var activity = new Activity("test-operation");
        activity.Start();

        _mockTracingProvider.Setup(x => x.StartActivity(
            It.IsAny<string>(),
            It.IsAny<Type>(),
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, object?>>()))
            .Returns(activity);

        var behavior = new DistributedTracingPipelineBehavior<TestRequest, TestResponse>(
            _mockTracingProvider.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        var request = new TestRequest { Value = "test" };
        var expectedResponse = new TestResponse { Result = "result" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(expectedResponse));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResponse, result);
        _mockTracingProvider.Verify(x => x.StartActivity(
            It.IsAny<string>(),
            It.IsAny<Type>(),
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, object?>>()), Times.Once());

        activity.Stop();
    }

    [Fact]
    public async Task HandleAsync_ShouldNotTrace_WhenTracingDisabled()
    {
        // Arrange
        var relayOptions = new RelayOptions
        {
            DefaultDistributedTracingOptions = new DistributedTracingOptions
            {
                EnableAutomaticDistributedTracing = false
            }
        };
        _mockOptions.Setup(x => x.Value).Returns(relayOptions);

        var behavior = new DistributedTracingPipelineBehavior<TestRequest, TestResponse>(
            _mockTracingProvider.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        var request = new TestRequest { Value = "test" };
        var expectedResponse = new TestResponse { Result = "result" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(expectedResponse));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResponse, result);
        _mockTracingProvider.Verify(x => x.StartActivity(
            It.IsAny<string>(),
            It.IsAny<Type>(),
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, object?>>()), Times.Never());
    }

    [Fact]
    public async Task HandleAsync_ShouldProceed_WhenActivityIsNull()
    {
        // Arrange
        _mockTracingProvider.Setup(x => x.StartActivity(
            "CustomOperation",
            It.IsAny<Type>(),
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, object?>>()))
            .Returns((Activity)null);

        var logger = new Mock<ILogger<DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>>>();
        var behavior = new DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>(
            _mockTracingProvider.Object,
            logger.Object,
            _mockOptions.Object);

        var request = new TraceableRequest();
        var expectedResponse = new TestResponse { Result = "result" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(expectedResponse));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResponse, result);
    }

    [Fact]
    public async Task HandleAsync_ShouldHandleExceptions_AndStopActivity()
    {
        // Arrange
        var activity = new Activity("test-operation");
        activity.Start();

        _mockTracingProvider.Setup(x => x.StartActivity(
            It.IsAny<string>(),
            It.IsAny<Type>(),
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, object?>>()))
            .Returns(activity);

        var behavior = new DistributedTracingPipelineBehavior<TestRequest, TestResponse>(
            _mockTracingProvider.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        var request = new TestRequest { Value = "test" };
        var next = new RequestHandlerDelegate<TestResponse>(() => throw new InvalidOperationException("Test exception"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await behavior.HandleAsync(request, next, CancellationToken.None));

        activity.Stop();
    }

    [Fact]
    public async Task HandleAsync_ShouldHandleCancelledToken_WhenAlreadyCancelled()
    {
        // Arrange
        var activity = new Activity("test-operation");
        activity.Start();

        _mockTracingProvider.Setup(x => x.StartActivity(
            It.IsAny<string>(),
            It.IsAny<Type>(),
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, object?>>()))
            .Returns(activity);

        var behavior = new DistributedTracingPipelineBehavior<TestRequest, TestResponse>(
            _mockTracingProvider.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        var request = new TestRequest { Value = "test" };
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel before calling
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(new TestResponse { Result = "cancelled" }));

        // Act & Assert - Current implementation doesn't check cancellation token, so it should proceed
        var result = await behavior.HandleAsync(request, next, cts.Token);
        Assert.Equal("cancelled", result.Result);

        activity.Stop();
    }

    [Fact]
    public void TraceAttribute_ShouldInitializeWithDefaults()
    {
        // Act
        var attribute = new TraceAttribute();

        // Assert
        Assert.NotNull(attribute);
    }

    [Fact]
    public void TraceAttribute_ShouldAllowCustomization()
    {
        // Act
        var attribute = new TraceAttribute
        {
            OperationName = "CustomOperation",
            TraceRequest = false,
            TraceResponse = false
        };

        // Assert
        Assert.Equal("CustomOperation", attribute.OperationName);
        Assert.False(attribute.TraceRequest);
        Assert.False(attribute.TraceResponse);
    }

    [Fact]
    public void TraceAttribute_ConstructorWithParameters_ShouldSetValues()
    {
        // Act
        var attribute = new TraceAttribute(true, false);

        // Assert
        Assert.True(attribute.TraceRequest);
        Assert.False(attribute.TraceResponse);
        Assert.Null(attribute.OperationName); // Default value
    }

    [Fact]
    public async Task HandleAsync_ShouldUseTraceAttribute_WhenPresent()
    {
        // Arrange
        var activity = new Activity("custom-operation");
        activity.Start();

        _mockTracingProvider.Setup(x => x.StartActivity(
            "CustomOperation",
            It.IsAny<Type>(),
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, object?>>()))
            .Returns(activity);

        var logger = new Mock<ILogger<DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>>>();
        var behavior = new DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>(
            _mockTracingProvider.Object,
            logger.Object,
            _mockOptions.Object);

        var request = new TraceableRequest();
        var expectedResponse = new TestResponse { Result = "result" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(expectedResponse));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResponse, result);
        _mockTracingProvider.Verify(x => x.StartActivity(
            "CustomOperation",
            typeof(TraceableRequest),
            null,
            It.IsAny<Dictionary<string, object?>>()), Times.Once());

        activity.Stop();
    }

    [Fact]
    public async Task HandleAsync_ShouldUseHandlerSpecificOptions_WhenAvailable()
    {
        // Arrange
        var handlerKey = typeof(TestRequest).FullName!;
        var handlerSpecificOptions = new DistributedTracingOptions
        {
            EnableAutomaticDistributedTracing = true,
            TraceRequests = false,
            TraceResponses = false
        };

        var relayOptions = new RelayOptions
        {
            DefaultDistributedTracingOptions = new DistributedTracingOptions
            {
                EnableAutomaticDistributedTracing = false
            }
        };
        relayOptions.DistributedTracingOverrides[handlerKey] = handlerSpecificOptions;
        _mockOptions.Setup(x => x.Value).Returns(relayOptions);

        var activity = new Activity("test-operation");
        activity.Start();

        _mockTracingProvider.Setup(x => x.StartActivity(
            It.IsAny<string>(),
            It.IsAny<Type>(),
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, object?>>()))
            .Returns(activity);

        var behavior = new DistributedTracingPipelineBehavior<TestRequest, TestResponse>(
            _mockTracingProvider.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        var request = new TestRequest { Value = "test" };
        var expectedResponse = new TestResponse { Result = "result" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(expectedResponse));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResponse, result);
        _mockTracingProvider.Verify(x => x.StartActivity(
            It.IsAny<string>(),
            It.IsAny<Type>(),
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, object?>>()), Times.Once());

        activity.Stop();
    }

    [Fact]
    public async Task HandleAsync_ShouldUseDefaultOptions_WhenNoHandlerOverride()
    {
        // Arrange
        var relayOptions = new RelayOptions
        {
            DefaultDistributedTracingOptions = new DistributedTracingOptions
            {
                EnableAutomaticDistributedTracing = true,
                TraceRequests = true,
                TraceResponses = true
            }
        };
        _mockOptions.Setup(x => x.Value).Returns(relayOptions);

        var activity = new Activity("test-operation");
        activity.Start();

        _mockTracingProvider.Setup(x => x.StartActivity(
            It.IsAny<string>(),
            It.IsAny<Type>(),
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, object?>>()))
            .Returns(activity);

        var behavior = new DistributedTracingPipelineBehavior<TestRequest, TestResponse>(
            _mockTracingProvider.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        var request = new TestRequest { Value = "test" };
        var expectedResponse = new TestResponse { Result = "result" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(expectedResponse));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResponse, result);
        _mockTracingProvider.Verify(x => x.StartActivity(
            It.IsAny<string>(),
            It.IsAny<Type>(),
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, object?>>()), Times.Once());
        _mockTracingProvider.Verify(x => x.AddActivityTags(It.IsAny<Dictionary<string, object?>>()), Times.Exactly(2)); // Request and response

        activity.Stop();
    }

    [Fact]
    public async Task HandleAsync_ShouldRecordException_WhenRecordExceptionsEnabled()
    {
        // Arrange
        var relayOptions = new RelayOptions
        {
            DefaultDistributedTracingOptions = new DistributedTracingOptions
            {
                EnableAutomaticDistributedTracing = true,
                RecordExceptions = true
            }
        };
        _mockOptions.Setup(x => x.Value).Returns(relayOptions);

        var activity = new Activity("test-operation");
        activity.Start();

        _mockTracingProvider.Setup(x => x.StartActivity(
            It.IsAny<string>(),
            It.IsAny<Type>(),
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, object?>>()))
            .Returns(activity);

        var behavior = new DistributedTracingPipelineBehavior<TestRequest, TestResponse>(
            _mockTracingProvider.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        var request = new TestRequest { Value = "test" };
        var expectedException = new InvalidOperationException("Test exception");
        var next = new RequestHandlerDelegate<TestResponse>(() => throw expectedException);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await behavior.HandleAsync(request, next, CancellationToken.None));

        _mockTracingProvider.Verify(x => x.RecordException(expectedException), Times.Once());
        _mockTracingProvider.Verify(x => x.SetActivityStatus(ActivityStatusCode.Error, expectedException.Message), Times.Once());

        activity.Stop();
    }

    [Fact]
    public async Task HandleAsync_ShouldNotRecordException_WhenRecordExceptionsDisabled()
    {
        // Arrange
        var relayOptions = new RelayOptions
        {
            DefaultDistributedTracingOptions = new DistributedTracingOptions
            {
                EnableAutomaticDistributedTracing = true,
                RecordExceptions = false
            }
        };
        _mockOptions.Setup(x => x.Value).Returns(relayOptions);

        var activity = new Activity("test-operation");
        activity.Start();

        _mockTracingProvider.Setup(x => x.StartActivity(
            It.IsAny<string>(),
            It.IsAny<Type>(),
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, object?>>()))
            .Returns(activity);

        var behavior = new DistributedTracingPipelineBehavior<TestRequest, TestResponse>(
            _mockTracingProvider.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        var request = new TestRequest { Value = "test" };
        var expectedException = new InvalidOperationException("Test exception");
        var next = new RequestHandlerDelegate<TestResponse>(() => throw expectedException);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await behavior.HandleAsync(request, next, CancellationToken.None));

        _mockTracingProvider.Verify(x => x.RecordException(It.IsAny<Exception>()), Times.Never());
        _mockTracingProvider.Verify(x => x.SetActivityStatus(ActivityStatusCode.Error, It.IsAny<string>()), Times.Never());

        activity.Stop();
    }

    [Fact]
    public async Task HandleAsync_ShouldAddRequestAndResponseInfoToTrace_WhenEnabled()
    {
        // Arrange
        var activity = new Activity("test-operation");
        activity.Start();

        _mockTracingProvider.Setup(x => x.StartActivity(
            It.IsAny<string>(),
            It.IsAny<Type>(),
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, object?>>()))
            .Returns(activity);

        var behavior = new DistributedTracingPipelineBehavior<TestRequest, TestResponse>(
            _mockTracingProvider.Object,
            _mockLogger.Object,
            _mockOptions.Object);

        var request = new TestRequest { Value = "test request" };
        var response = new TestResponse { Result = "test response" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        _mockTracingProvider.Verify(x => x.AddActivityTags(It.Is<Dictionary<string, object?>>(tags =>
            tags.ContainsKey("request.info") && tags["request.info"].ToString() == request.ToString())), Times.Once());
        _mockTracingProvider.Verify(x => x.AddActivityTags(It.Is<Dictionary<string, object?>>(tags =>
            tags.ContainsKey("response.info") && tags["response.info"].ToString() == response.ToString())), Times.Once());
    }

    [Fact]
    public async Task HandleAsync_ShouldHandleTraceableRequest_WhenEnabled()
    {
        // Arrange
        var relayOptions = new RelayOptions
        {
            DefaultDistributedTracingOptions = new DistributedTracingOptions
            {
                EnableAutomaticDistributedTracing = false
            }
        };
        _mockOptions.Setup(x => x.Value).Returns(relayOptions);

        var activity = new Activity("custom-operation");
        activity.Start();

        _mockTracingProvider.Setup(x => x.StartActivity(
            "CustomOperation",
            It.IsAny<Type>(),
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, object?>>()))
            .Returns(activity);

        var logger = new Mock<ILogger<DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>>>();
        var behavior = new DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>(
            _mockTracingProvider.Object,
            logger.Object,
            _mockOptions.Object);

        var request = new TraceableRequest();
        var response = new TestResponse { Result = "test response" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        _mockTracingProvider.Verify(x => x.AddActivityTags(It.Is<Dictionary<string, object?>>(tags =>
            tags.ContainsKey("request.info") && tags["request.info"].ToString() == request.ToString())), Times.Once());
        _mockTracingProvider.Verify(x => x.AddActivityTags(It.Is<Dictionary<string, object?>>(tags =>
            tags.ContainsKey("response.info") && tags["response.info"].ToString() == response.ToString())), Times.Once());
        _mockTracingProvider.Verify(x => x.SetActivityStatus(ActivityStatusCode.Ok), Times.Once());

        activity.Stop();
    }

    [Fact]
    public async Task HandleAsync_ShouldEnableTracing_WhenTraceAttributePresentAndGloballyDisabled()
    {
        // Arrange
        var relayOptions = new RelayOptions
        {
            DefaultDistributedTracingOptions = new DistributedTracingOptions
            {
                EnableAutomaticDistributedTracing = false
            }
        };
        _mockOptions.Setup(x => x.Value).Returns(relayOptions);

        var activity = new Activity("custom-operation");
        activity.Start();

        _mockTracingProvider.Setup(x => x.StartActivity(
            "CustomOperation",
            It.IsAny<Type>(),
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, object?>>()))
            .Returns(activity);

        var logger = new Mock<ILogger<DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>>>();
        var behavior = new DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>(
            _mockTracingProvider.Object,
            logger.Object,
            _mockOptions.Object);

        var request = new TraceableRequest();
        var expectedResponse = new TestResponse { Result = "result" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(expectedResponse));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResponse, result);
        _mockTracingProvider.Verify(x => x.StartActivity(
            It.IsAny<string>(),
            It.IsAny<Type>(),
            It.IsAny<string?>(),
            It.IsAny<Dictionary<string, object?>>()), Times.Once());

        activity.Stop();
    }

    public class TestRequest : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    [Trace(OperationName = "CustomOperation")]
    public class TraceableRequest : IRequest<TestResponse>
    {
    }

    public class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }
}