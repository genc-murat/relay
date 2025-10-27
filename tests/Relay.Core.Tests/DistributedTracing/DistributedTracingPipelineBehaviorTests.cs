using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Configuration.Options.Core;
using Relay.Core.Configuration.Options.DistributedTracing;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.DistributedTracing;
using Xunit;

namespace Relay.Core.Tests.DistributedTracing;

[Trace]
public class TraceableRequest
{
    public string Id { get; set; } = "test-id";
    public int Value { get; set; } = 42;
}

public class NonTraceableRequest
{
    public string Name { get; set; } = "test-name";
}

[Trace(OperationName = "CustomOperation", TraceRequest = true, TraceResponse = true)]
public class CustomTraceableRequest
{
    public string Data { get; set; } = "test-data";
}

public class TestResponse
{
    public string Result { get; set; } = "success";
    public bool IsSuccess { get; set; } = true;
    public int StatusCode { get; set; } = 200;
}

public class ComplexRequest
{
    public string Name { get; set; } = "test";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid Id { get; set; } = Guid.NewGuid();
    public List<string> Items { get; set; } = new List<string> { "item1", "item2" };
}

public class ErrorTestResponse
{
    public string Error { get; set; } = "error occurred";
    public bool IsError { get; set; } = true;
}

public class DistributedTracingPipelineBehaviorTests
{
    #region Constructor and Setup Tests

    [Fact]
    public void Constructor_WithNullTracingProvider_ShouldThrowArgumentNullException()
    {
        // Arrange
        var logger = new Mock<ILogger<DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>>>().Object;
        var options = new Mock<IOptions<RelayOptions>>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>(
            null!,
            logger,
            options));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var tracingProvider = new Mock<IDistributedTracingProvider>().Object;
        var options = new Mock<IOptions<RelayOptions>>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>(
            tracingProvider,
            null!,
            options));
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var tracingProvider = new Mock<IDistributedTracingProvider>().Object;
        var logger = new Mock<ILogger<DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>>>().Object;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>(
            tracingProvider,
            logger,
            null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
    {
        // Arrange
        var tracingProvider = new Mock<IDistributedTracingProvider>().Object;
        var logger = new Mock<ILogger<DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>>>().Object;
        var options = new Mock<IOptions<RelayOptions>>().Object;

        // Act
        var behavior = new DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>(
            tracingProvider,
            logger,
            options);

        // Assert
        Assert.NotNull(behavior);
    }

    #endregion

    #region HandleAsync Tests - Tracing Enabled

    [Fact]
    public async Task HandleAsync_WithTracingEnabled_ShouldAddTraceData()
    {
        // Arrange
        var tracingProviderMock = new Mock<IDistributedTracingProvider>();
        var loggerMock = new Mock<ILogger<DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>>>();
        var optionsMock = new Mock<IOptions<RelayOptions>>();
        var relayOptions = new RelayOptions
        {
            DefaultDistributedTracingOptions = new DistributedTracingOptions
            {
                EnableAutomaticDistributedTracing = true,
                TraceRequests = true,
                TraceResponses = true,
                RecordExceptions = true
            },
            DistributedTracingOverrides = new Dictionary<string, DistributedTracingOptions>()
        };
        optionsMock.Setup(o => o.Value).Returns(relayOptions);

        var behavior = new DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>(
            tracingProviderMock.Object,
            loggerMock.Object,
            optionsMock.Object);

        var request = new TraceableRequest();
        var response = new TestResponse();
        var activity = new Activity("TestOperation");
        activity.Start();
        
        tracingProviderMock.Setup(tp => tp.StartActivity(
                It.IsAny<string>(),
                It.IsAny<Type>(),
                It.IsAny<string?>(),
                It.IsAny<IDictionary<string, object?>>()))
            .Returns(activity);

        tracingProviderMock.Setup(tp => tp.SetActivityStatus(It.IsAny<ActivityStatusCode>(), It.IsAny<string?>()))
            .Verifiable();

        tracingProviderMock.Setup(tp => tp.AddActivityTags(It.IsAny<IDictionary<string, object?>>()))
            .Verifiable();

        RequestHandlerDelegate<TestResponse> next = () => ValueTask.FromResult(response);

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        tracingProviderMock.Verify(tp => tp.StartActivity(
            It.IsAny<string>(),
            It.IsAny<Type>(),
            It.IsAny<string?>(),
            It.IsAny<IDictionary<string, object?>>()), Times.Once);
        tracingProviderMock.Verify(tp => tp.SetActivityStatus(ActivityStatusCode.Ok, null), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithCustomTraceAttribute_ShouldUseCustomParameters()
    {
        // Arrange
        var tracingProviderMock = new Mock<IDistributedTracingProvider>();
        var loggerMock = new Mock<ILogger<DistributedTracingPipelineBehavior<CustomTraceableRequest, TestResponse>>>();
        var optionsMock = new Mock<IOptions<RelayOptions>>();
        var relayOptions = new RelayOptions
        {
            DefaultDistributedTracingOptions = new DistributedTracingOptions
            {
                EnableAutomaticDistributedTracing = true,
                TraceRequests = true,
                TraceResponses = true,
                RecordExceptions = true
            },
            DistributedTracingOverrides = new Dictionary<string, DistributedTracingOptions>()
        };
        optionsMock.Setup(o => o.Value).Returns(relayOptions);

        var behavior = new DistributedTracingPipelineBehavior<CustomTraceableRequest, TestResponse>(
            tracingProviderMock.Object,
            loggerMock.Object,
            optionsMock.Object);

        var request = new CustomTraceableRequest();
        var response = new TestResponse();
        var activity = new Activity("CustomOperation");
        activity.Start();
        
        tracingProviderMock.Setup(tp => tp.StartActivity(
                "CustomOperation",  // Expected custom operation name
                It.IsAny<Type>(),
                It.IsAny<string?>(),
                It.IsAny<IDictionary<string, object?>>()))
            .Returns(activity);

        tracingProviderMock.Setup(tp => tp.SetActivityStatus(It.IsAny<ActivityStatusCode>(), It.IsAny<string?>()))
            .Verifiable();

        tracingProviderMock.Setup(tp => tp.AddActivityTags(It.IsAny<IDictionary<string, object?>>()))
            .Verifiable();

        RequestHandlerDelegate<TestResponse> next = () => ValueTask.FromResult(response);

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        tracingProviderMock.Verify(tp => tp.StartActivity(
            "CustomOperation", // Should use custom operation name
            It.IsAny<Type>(),
            It.IsAny<string?>(),
            It.IsAny<IDictionary<string, object?>>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithNullActivity_ShouldProceedWithoutTracing()
    {
        // Arrange
        var tracingProviderMock = new Mock<IDistributedTracingProvider>();
        var loggerMock = new Mock<ILogger<DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>>>();
        var optionsMock = new Mock<IOptions<RelayOptions>>();
        var relayOptions = new RelayOptions
        {
            DefaultDistributedTracingOptions = new DistributedTracingOptions
            {
                EnableAutomaticDistributedTracing = true,
                TraceRequests = true,
                TraceResponses = true,
                RecordExceptions = true
            }
        };
        optionsMock.Setup(o => o.Value).Returns(relayOptions);

        var behavior = new DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>(
            tracingProviderMock.Object,
            loggerMock.Object,
            optionsMock.Object);

        var request = new TraceableRequest();
        var response = new TestResponse();
        
        tracingProviderMock.Setup(tp => tp.StartActivity(
                It.IsAny<string>(),
                It.IsAny<Type>(),
                It.IsAny<string?>(),
                It.IsAny<IDictionary<string, object?>>()))
            .Returns((Activity)null);

        RequestHandlerDelegate<TestResponse> next = () => ValueTask.FromResult(response);

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        tracingProviderMock.Verify(tp => tp.SetActivityStatus(It.IsAny<ActivityStatusCode>(), It.IsAny<string?>()), Times.Never);
    }

    #endregion

    #region HandleAsync Tests - Tracing Disabled Scenarios

    [Fact]
    public async Task HandleAsync_WithTracingGloballyDisabled_ShouldNotTrace()
    {
        // Arrange
        var tracingProviderMock = new Mock<IDistributedTracingProvider>();
        var loggerMock = new Mock<ILogger<DistributedTracingPipelineBehavior<NonTraceableRequest, TestResponse>>>();
        var optionsMock = new Mock<IOptions<RelayOptions>>();
        var options = new RelayOptions
        {
            DefaultDistributedTracingOptions = new DistributedTracingOptions
            {
                EnableAutomaticDistributedTracing = false, // Disabled
                TraceRequests = true,
                TraceResponses = true
            }
        };
        optionsMock.Setup(o => o.Value).Returns(options);

        var behavior = new DistributedTracingPipelineBehavior<NonTraceableRequest, TestResponse>(
            tracingProviderMock.Object,
            loggerMock.Object,
            optionsMock.Object);

        var request = new NonTraceableRequest();
        var response = new TestResponse();
        
        RequestHandlerDelegate<TestResponse> next = () => ValueTask.FromResult(response);

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        tracingProviderMock.Verify(tp => tp.StartActivity(It.IsAny<string>(), It.IsAny<Type>(), It.IsAny<string?>(), It.IsAny<IDictionary<string, object?>>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithHandlerOverride_ShouldUseOverrideOptions()
    {
        // Arrange
        var tracingProviderMock = new Mock<IDistributedTracingProvider>();
        var loggerMock = new Mock<ILogger<DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>>>();
        var optionsMock = new Mock<IOptions<RelayOptions>>();
        
        var overrideOptions = new DistributedTracingOptions
        {
            EnableAutomaticDistributedTracing = true,
            TraceRequests = false,
            TraceResponses = false
        };
        var relayOptions = new RelayOptions
        {
            DefaultDistributedTracingOptions = new DistributedTracingOptions
            {
                EnableAutomaticDistributedTracing = true,
                TraceRequests = true,
                TraceResponses = true
            },
            DistributedTracingOverrides = new Dictionary<string, DistributedTracingOptions>
            {
                { typeof(TraceableRequest).FullName ?? typeof(TraceableRequest).Name, overrideOptions }
            }
        };
        optionsMock.Setup(o => o.Value).Returns(relayOptions);
        
        var behavior = new DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>(
            tracingProviderMock.Object,
            loggerMock.Object,
            optionsMock.Object);

        var request = new TraceableRequest();
        var response = new TestResponse();
        var activity = new Activity("TestOperation");
        activity.Start();
        
        tracingProviderMock.Setup(tp => tp.StartActivity(
                It.IsAny<string>(),
                It.IsAny<Type>(),
                It.IsAny<string?>(),
                It.IsAny<IDictionary<string, object?>>()))
            .Returns(activity);

        tracingProviderMock.Setup(tp => tp.AddActivityTags(It.IsAny<IDictionary<string, object?>>()))
            .Verifiable();

        RequestHandlerDelegate<TestResponse> next = () => ValueTask.FromResult(response);

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        // Activity should still start, but request/response tracing should be controlled by override
        tracingProviderMock.Verify(tp => tp.StartActivity(It.IsAny<string>(), It.IsAny<Type>(), It.IsAny<string?>(), It.IsAny<IDictionary<string, object?>>()), Times.Once);
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task HandleAsync_WhenHandlerThrowsException_ShouldRecordException()
    {
        // Arrange
        var tracingProviderMock = new Mock<IDistributedTracingProvider>();
        var loggerMock = new Mock<ILogger<DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>>>();
        var optionsMock = new Mock<IOptions<RelayOptions>>();
        var relayOptions = new RelayOptions
        {
            DefaultDistributedTracingOptions = new DistributedTracingOptions
            {
                EnableAutomaticDistributedTracing = true,
                RecordExceptions = true
            }
        };
        optionsMock.Setup(o => o.Value).Returns(relayOptions);

        var behavior = new DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>(
            tracingProviderMock.Object,
            loggerMock.Object,
            optionsMock.Object);

        var request = new TraceableRequest();
        var exception = new InvalidOperationException("Test exception");
        var activity = new Activity("TestOperation");
        activity.Start();
        
        tracingProviderMock.Setup(tp => tp.StartActivity(
                It.IsAny<string>(),
                It.IsAny<Type>(),
                It.IsAny<string?>(),
                It.IsAny<IDictionary<string, object?>>()))
            .Returns(activity);

        tracingProviderMock.Setup(tp => tp.RecordException(It.IsAny<Exception>()))
            .Verifiable();

        tracingProviderMock.Setup(tp => tp.SetActivityStatus(ActivityStatusCode.Error, It.IsAny<string>()))
            .Verifiable();

        RequestHandlerDelegate<TestResponse> next = () => { throw exception; };

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await behavior.HandleAsync(request, next, CancellationToken.None));
        
        Assert.Equal(exception, actualException);
        tracingProviderMock.Verify(tp => tp.RecordException(exception), Times.Once);
        tracingProviderMock.Verify(tp => tp.SetActivityStatus(ActivityStatusCode.Error, exception.Message), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithExceptionRecordingDisabled_ShouldNotRecordException()
    {
        // Arrange
        var tracingProviderMock = new Mock<IDistributedTracingProvider>();
        var loggerMock = new Mock<ILogger<DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>>>();
        var optionsMock = new Mock<IOptions<RelayOptions>>();
        var options = new RelayOptions
        {
            DefaultDistributedTracingOptions = new DistributedTracingOptions
            {
                EnableAutomaticDistributedTracing = true,
                RecordExceptions = false // Disabled
            }
        };
        optionsMock.Setup(o => o.Value).Returns(options);

        var behavior = new DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>(
            tracingProviderMock.Object,
            loggerMock.Object,
            optionsMock.Object);

        var request = new TraceableRequest();
        var exception = new InvalidOperationException("Test exception");
        var activity = new Activity("TestOperation");
        activity.Start();
        
        tracingProviderMock.Setup(tp => tp.StartActivity(
                It.IsAny<string>(),
                It.IsAny<Type>(),
                It.IsAny<string?>(),
                It.IsAny<IDictionary<string, object?>>()))
            .Returns(activity);

        RequestHandlerDelegate<TestResponse> next = () => { throw exception; };

        // Act & Assert
        var actualException = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await behavior.HandleAsync(request, next, CancellationToken.None));
        
        Assert.Equal(exception, actualException);
        tracingProviderMock.Verify(tp => tp.RecordException(It.IsAny<Exception>()), Times.Never);
    }

    #endregion

    #region AddRequestInfoToTrace and AddResponseInfoToTrace Tests

    [Fact]
    public async Task HandleAsync_WithTraceRequestEnabled_ShouldAddRequestInfo()
    {
        // Arrange
        var tracingProviderMock = new Mock<IDistributedTracingProvider>();
        var loggerMock = new Mock<ILogger<DistributedTracingPipelineBehavior<ComplexRequest, TestResponse>>>();
        var optionsMock = new Mock<IOptions<RelayOptions>>();
        var options = new RelayOptions
        {
            DefaultDistributedTracingOptions = new DistributedTracingOptions
            {
                EnableAutomaticDistributedTracing = true,
                TraceRequests = true, // Enable request tracing
                TraceResponses = false
            }
        };
        optionsMock.Setup(o => o.Value).Returns(options);

        var behavior = new DistributedTracingPipelineBehavior<ComplexRequest, TestResponse>(
            tracingProviderMock.Object,
            loggerMock.Object,
            optionsMock.Object);

        var request = new ComplexRequest();
        var response = new TestResponse();
        var activity = new Activity("TestOperation");
        activity.Start();
        
        tracingProviderMock.Setup(tp => tp.StartActivity(
                It.IsAny<string>(),
                It.IsAny<Type>(),
                It.IsAny<string?>(),
                It.IsAny<IDictionary<string, object?>>()))
            .Returns(activity);

        tracingProviderMock.Setup(tp => tp.AddActivityTags(It.IsAny<IDictionary<string, object?>>()))
            .Verifiable();

        RequestHandlerDelegate<TestResponse> next = () => ValueTask.FromResult(response);

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        tracingProviderMock.Verify(tp => tp.AddActivityTags(It.IsAny<IDictionary<string, object?>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleAsync_WithTraceResponseEnabled_ShouldAddResponseInfo()
    {
        // Arrange
        var tracingProviderMock = new Mock<IDistributedTracingProvider>();
        var loggerMock = new Mock<ILogger<DistributedTracingPipelineBehavior<ComplexRequest, TestResponse>>>();
        var optionsMock = new Mock<IOptions<RelayOptions>>();
        var options = new RelayOptions
        {
            DefaultDistributedTracingOptions = new DistributedTracingOptions
            {
                EnableAutomaticDistributedTracing = true,
                TraceRequests = false,
                TraceResponses = true // Enable response tracing
            }
        };
        optionsMock.Setup(o => o.Value).Returns(options);

        var behavior = new DistributedTracingPipelineBehavior<ComplexRequest, TestResponse>(
            tracingProviderMock.Object,
            loggerMock.Object,
            optionsMock.Object);

        var request = new ComplexRequest();
        var response = new TestResponse();
        var activity = new Activity("TestOperation");
        activity.Start();
        
        tracingProviderMock.Setup(tp => tp.StartActivity(
                It.IsAny<string>(),
                It.IsAny<Type>(),
                It.IsAny<string?>(),
                It.IsAny<IDictionary<string, object?>>()))
            .Returns(activity);

        tracingProviderMock.Setup(tp => tp.AddActivityTags(It.IsAny<IDictionary<string, object?>>()))
            .Verifiable();

        RequestHandlerDelegate<TestResponse> next = () => ValueTask.FromResult(response);

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        tracingProviderMock.Verify(tp => tp.AddActivityTags(It.IsAny<IDictionary<string, object?>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleAsync_WithNullRequest_ShouldHandleGracefully()
    {
        // Arrange
        var tracingProviderMock = new Mock<IDistributedTracingProvider>();
        var loggerMock = new Mock<ILogger<DistributedTracingPipelineBehavior<object, TestResponse>>>();
        var optionsMock = new Mock<IOptions<RelayOptions>>();
        var relayOptions = new RelayOptions
        {
            DefaultDistributedTracingOptions = new DistributedTracingOptions
            {
                EnableAutomaticDistributedTracing = true,
                TraceRequests = true,
                TraceResponses = true
            }
        };
        optionsMock.Setup(o => o.Value).Returns(relayOptions);

        var behavior = new DistributedTracingPipelineBehavior<object, TestResponse>(
            tracingProviderMock.Object,
            loggerMock.Object,
            optionsMock.Object);

        object request = null;
        var response = new TestResponse();
        var activity = new Activity("TestOperation");
        activity.Start();
        
        tracingProviderMock.Setup(tp => tp.StartActivity(
                It.IsAny<string>(),
                It.IsAny<Type>(),
                It.IsAny<string?>(),
                It.IsAny<IDictionary<string, object?>>()))
            .Returns(activity);

        tracingProviderMock.Setup(tp => tp.AddActivityTags(It.IsAny<IDictionary<string, object?>>()))
            .Verifiable();

        RequestHandlerDelegate<TestResponse> next = () => ValueTask.FromResult(response);

        // Act & Assert - should not throw
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        tracingProviderMock.Verify(tp => tp.AddActivityTags(It.IsAny<IDictionary<string, object?>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleAsync_WithNullResponse_ShouldHandleGracefully()
    {
        // Arrange
        var tracingProviderMock = new Mock<IDistributedTracingProvider>();
        var loggerMock = new Mock<ILogger<DistributedTracingPipelineBehavior<TraceableRequest, object>>>();
        var optionsMock = new Mock<IOptions<RelayOptions>>();
        var relayOptions = new RelayOptions
        {
            DefaultDistributedTracingOptions = new DistributedTracingOptions
            {
                EnableAutomaticDistributedTracing = true,
                TraceRequests = true,
                TraceResponses = true
            }
        };
        optionsMock.Setup(o => o.Value).Returns(relayOptions);

        var behavior = new DistributedTracingPipelineBehavior<TraceableRequest, object>(
            tracingProviderMock.Object,
            loggerMock.Object,
            optionsMock.Object);

        var request = new TraceableRequest();
        object response = null;
        var activity = new Activity("TestOperation");
        activity.Start();
        
        tracingProviderMock.Setup(tp => tp.StartActivity(
                It.IsAny<string>(),
                It.IsAny<Type>(),
                It.IsAny<string?>(),
                It.IsAny<IDictionary<string, object?>>()))
            .Returns(activity);

        tracingProviderMock.Setup(tp => tp.AddActivityTags(It.IsAny<IDictionary<string, object?>>()))
            .Verifiable();

        RequestHandlerDelegate<object> next = () => ValueTask.FromResult(response);

        // Act & Assert - should not throw
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        tracingProviderMock.Verify(tp => tp.AddActivityTags(It.IsAny<IDictionary<string, object?>>()), Times.AtLeastOnce);
    }

    #endregion

    #region Private Methods Tests (using reflection)

    [Fact]
    public void GetDistributedTracingOptions_WithHandlerOverride_ShouldReturnOverrideOptions()
    {
        // Use reflection to test private method
        var behavior = CreateBehaviorWithReflection<TraceableRequest, TestResponse>(
            new RelayOptions
            {
                DefaultDistributedTracingOptions = new DistributedTracingOptions
                {
                    EnableAutomaticDistributedTracing = true,
                    TraceRequests = true,
                    TraceResponses = true
                },
                DistributedTracingOverrides = new Dictionary<string, DistributedTracingOptions>
                {
                    { typeof(TraceableRequest).FullName ?? typeof(TraceableRequest).Name, 
                      new DistributedTracingOptions { EnableAutomaticDistributedTracing = false } }
                }
            });

        var method = typeof(DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>)
            .GetMethod("GetDistributedTracingOptions", BindingFlags.NonPublic | BindingFlags.Instance);
        
        var result = (DistributedTracingOptions)method.Invoke(behavior, null);

        Assert.False(result.EnableAutomaticDistributedTracing); // Should come from override
    }

    [Fact]
    public void GetDistributedTracingOptions_WithoutHandlerOverride_ShouldReturnDefaultOptions()
    {
        // Use reflection to test private method
        var behavior = CreateBehaviorWithReflection<TraceableRequest, TestResponse>(
            new RelayOptions
            {
                DefaultDistributedTracingOptions = new DistributedTracingOptions
                {
                    EnableAutomaticDistributedTracing = true,
                    TraceRequests = true,
                    TraceResponses = true
                },
                DistributedTracingOverrides = new Dictionary<string, DistributedTracingOptions>()
            });

        var method = typeof(DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>)
            .GetMethod("GetDistributedTracingOptions", BindingFlags.NonPublic | BindingFlags.Instance);
        
        var result = (DistributedTracingOptions)method.Invoke(behavior, null);

        Assert.True(result.EnableAutomaticDistributedTracing); // Should come from default
    }

    #endregion

    #region Helper Methods

    private DistributedTracingPipelineBehavior<TRequest, TResponse> CreateBehaviorWithReflection<TRequest, TResponse>(RelayOptions options)
    {
        var tracingProviderMock = new Mock<IDistributedTracingProvider>().Object;
        var loggerMock = CreateLoggerMock<TRequest, TResponse>();
        var optionsMock = new Mock<IOptions<RelayOptions>>().Object;
        Mock.Get(optionsMock).Setup(o => o.Value).Returns(options);

        var constructor = typeof(DistributedTracingPipelineBehavior<TRequest, TResponse>)
            .GetConstructor(new[] {
                typeof(IDistributedTracingProvider),
                typeof(ILogger<DistributedTracingPipelineBehavior<TRequest, TResponse>>),
                typeof(IOptions<RelayOptions>)
            });

        return (DistributedTracingPipelineBehavior<TRequest, TResponse>)constructor.Invoke(
            new object[] { 
                tracingProviderMock, 
                loggerMock, 
                optionsMock 
            });
    }

    private ILogger<T> CreateLoggerMock<T>()
    {
        return new Mock<ILogger<T>>().Object;
    }

    private ILogger<DistributedTracingPipelineBehavior<TRequest, TResponse>> CreateLoggerMock<TRequest, TResponse>()
    {
        return new Mock<ILogger<DistributedTracingPipelineBehavior<TRequest, TResponse>>>().Object;
    }

    #endregion
}