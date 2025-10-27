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

    #region Edge Case Tests for Full Code Coverage

    [Fact]
    public async Task HandleAsync_WithRequestMetadataExtractionFailure_ShouldContinueTracing()
    {
        // Arrange - Test AddRequestInfoToTrace exception handling
        var tracingProviderMock = new Mock<IDistributedTracingProvider>();
        var loggerMock = new Mock<ILogger<DistributedTracingPipelineBehavior<ComplexRequest, TestResponse>>>();
        var optionsMock = new Mock<IOptions<RelayOptions>>();
        var options = new RelayOptions
        {
            DefaultDistributedTracingOptions = new DistributedTracingOptions
            {
                EnableAutomaticDistributedTracing = true,
                TraceRequests = true,
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

        // Setup AddActivityTags to be called multiple times (including error case)
        tracingProviderMock.Setup(tp => tp.AddActivityTags(It.IsAny<IDictionary<string, object?>>()))
            .Verifiable();

        RequestHandlerDelegate<TestResponse> next = () => ValueTask.FromResult(response);

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        tracingProviderMock.Verify(tp => tp.AddActivityTags(It.IsAny<IDictionary<string, object?>>()), Times.AtLeastOnce);
        loggerMock.Verify(l => l.Log(LogLevel.Debug, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleAsync_WithResponseMetadataExtractionFailure_ShouldContinueTracing()
    {
        // Arrange - Test AddResponseInfoToTrace exception handling
        var tracingProviderMock = new Mock<IDistributedTracingProvider>();
        var loggerMock = new Mock<ILogger<DistributedTracingPipelineBehavior<ComplexRequest, ObjectWithBadProperty>>>();
        var optionsMock = new Mock<IOptions<RelayOptions>>();
        var options = new RelayOptions
        {
            DefaultDistributedTracingOptions = new DistributedTracingOptions
            {
                EnableAutomaticDistributedTracing = true,
                TraceRequests = false,
                TraceResponses = true
            }
        };
        optionsMock.Setup(o => o.Value).Returns(options);

        var behavior = new DistributedTracingPipelineBehavior<ComplexRequest, ObjectWithBadProperty>(
            tracingProviderMock.Object,
            loggerMock.Object,
            optionsMock.Object);

        var request = new ComplexRequest();
        var response = new ObjectWithBadProperty(); // This will cause property extraction to fail
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

        RequestHandlerDelegate<ObjectWithBadProperty> next = () => ValueTask.FromResult(response);

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        tracingProviderMock.Verify(tp => tp.AddActivityTags(It.IsAny<IDictionary<string, object?>>()), Times.AtLeastOnce);
        loggerMock.Verify(l => l.Log(LogLevel.Debug, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public void SafeValueForTracing_WithNullableType_ShouldHandleCorrectly()
    {
        // Arrange
        var behavior = CreateBehaviorWithReflection<TraceableRequest, TestResponse>(
            new RelayOptions
            {
                DefaultDistributedTracingOptions = new DistributedTracingOptions
                {
                    EnableAutomaticDistributedTracing = true
                }
            });

        var method = typeof(DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>)
            .GetMethod("SafeValueForTracing", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act - Test nullable int
        var result1 = method.Invoke(behavior, new object[] { (int?)42, typeof(int?) });
        var result2 = method.Invoke(behavior, new object[] { (int?)null, typeof(int?) });

        // Assert
        Assert.Equal(42, result1);
        Assert.Null(result2);
    }

    [Fact]
    public void SafeValueForTracing_WithComplexObject_ShouldSerializeToJson()
    {
        // Arrange
        var behavior = CreateBehaviorWithReflection<TraceableRequest, TestResponse>(
            new RelayOptions
            {
                DefaultDistributedTracingOptions = new DistributedTracingOptions
                {
                    EnableAutomaticDistributedTracing = true
                }
            });

        var method = typeof(DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>)
            .GetMethod("SafeValueForTracing", BindingFlags.NonPublic | BindingFlags.Instance);

        var complexObject = new { Name = "Test", Value = 123 };

        // Act
        var result = method.Invoke(behavior, new object[] { complexObject, complexObject.GetType() });

        // Assert
        Assert.IsType<string>(result);
        Assert.Contains("Test", (string)result);
        Assert.Contains("123", (string)result);
    }



    [Fact]
    public void SafeValueForTracing_WithSerializationFailure_ShouldFallbackToToString()
    {
        // Arrange
        var behavior = CreateBehaviorWithReflection<TraceableRequest, TestResponse>(
            new RelayOptions
            {
                DefaultDistributedTracingOptions = new DistributedTracingOptions
                {
                    EnableAutomaticDistributedTracing = true
                }
            });

        var method = typeof(DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>)
            .GetMethod("SafeValueForTracing", BindingFlags.NonPublic | BindingFlags.Instance);

        // Create an object that might cause serialization issues
        var problematicObject = new ProblematicObject();

        // Act
        var result = method.Invoke(behavior, new object[] { problematicObject, problematicObject.GetType() });

        // Assert - Should fall back to ToString
        Assert.Equal(problematicObject.ToString(), result);
    }



    #endregion

    #region Additional Private Method Tests (using reflection)

    [Fact]
    public void IsDistributedTracingEnabled_WithTraceAttribute_ShouldReturnTrue()
    {
        // Use reflection to test private method
        var behavior = CreateBehaviorWithReflection<TraceableRequest, TestResponse>(
            new RelayOptions
            {
                DefaultDistributedTracingOptions = new DistributedTracingOptions
                {
                    EnableAutomaticDistributedTracing = false // Globally disabled
                }
            });

        var method = typeof(DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>)
            .GetMethod("IsDistributedTracingEnabled", BindingFlags.NonPublic | BindingFlags.Static);

        var traceAttribute = typeof(TraceableRequest).GetCustomAttribute<TraceAttribute>();
        var options = new DistributedTracingOptions { EnableAutomaticDistributedTracing = false };

        var result = (bool)method.Invoke(null, new object[] { options, traceAttribute });

        Assert.True(result); // Should return true because of TraceAttribute
    }

    [Fact]
    public void IsDistributedTracingEnabled_WithoutTraceAttributeAndGloballyDisabled_ShouldReturnFalse()
    {
        // Use reflection to test private method
        var behavior = CreateBehaviorWithReflection<NonTraceableRequest, TestResponse>(
            new RelayOptions
            {
                DefaultDistributedTracingOptions = new DistributedTracingOptions
                {
                    EnableAutomaticDistributedTracing = false
                }
            });

        var method = typeof(DistributedTracingPipelineBehavior<NonTraceableRequest, TestResponse>)
            .GetMethod("IsDistributedTracingEnabled", BindingFlags.NonPublic | BindingFlags.Static);

        TraceAttribute? traceAttribute = null; // No TraceAttribute
        var options = new DistributedTracingOptions { EnableAutomaticDistributedTracing = false };

        var result = (bool)method.Invoke(null, new object[] { options, traceAttribute });

        Assert.False(result); // Should return false
    }



    [Fact]
    public void ExtractObjectProperties_WithValidObject_ShouldReturnProperties()
    {
        // Use reflection to test private generic method
        var behavior = CreateBehaviorWithReflection<TraceableRequest, TestResponse>(
            new RelayOptions
            {
                DefaultDistributedTracingOptions = new DistributedTracingOptions
                {
                    EnableAutomaticDistributedTracing = true
                }
            });

        var method = typeof(DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>)
            .GetMethod("ExtractObjectProperties", BindingFlags.NonPublic | BindingFlags.Instance)
            .MakeGenericMethod(typeof(TraceableRequest));

        var obj = new TraceableRequest { Id = "test123", Value = 999 };

        var result = (Dictionary<string, object?>)method.Invoke(behavior, new object[] { obj });

        Assert.Contains("Id", result.Keys);
        Assert.Contains("Value", result.Keys);
        Assert.Equal("test123", result["Id"]);
        Assert.Equal(999, result["Value"]);
    }

    [Fact]
    public void ExtractObjectFields_WithValidObject_ShouldReturnFields()
    {
        // Use reflection to test private generic method
        var behavior = CreateBehaviorWithReflection<TraceableRequest, TestResponse>(
            new RelayOptions
            {
                DefaultDistributedTracingOptions = new DistributedTracingOptions
                {
                    EnableAutomaticDistributedTracing = true
                }
            });

        var method = typeof(DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>)
            .GetMethod("ExtractObjectFields", BindingFlags.NonPublic | BindingFlags.Instance)
            .MakeGenericMethod(typeof(TraceableRequest));

        var obj = new TraceableRequest { Id = "test123", Value = 999 };

        var result = (Dictionary<string, object?>)method.Invoke(behavior, new object[] { obj });

        // TraceableRequest doesn't have public fields, so result should be empty
        Assert.Empty(result);
    }

    [Fact]
    public void ExtractCustomAttributes_WithAttributedType_ShouldReturnAttributes()
    {
        // Use reflection to test private method
        var behavior = CreateBehaviorWithReflection<TraceableRequest, TestResponse>(
            new RelayOptions
            {
                DefaultDistributedTracingOptions = new DistributedTracingOptions
                {
                    EnableAutomaticDistributedTracing = true
                }
            });

        var method = typeof(DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>)
            .GetMethod("ExtractCustomAttributes", BindingFlags.NonPublic | BindingFlags.Instance);

        var result = (Dictionary<string, object?>)method.Invoke(behavior, new object[] { typeof(TraceableRequest) });

        Assert.Contains("TraceAttribute", result.Keys);
    }

    [Fact]
    public void ExtractStatusInformation_WithStatusProperties_ShouldExtractValues()
    {
        // Use reflection to test private generic method
        var behavior = CreateBehaviorWithReflection<TraceableRequest, ResponseWithStatus>(
            new RelayOptions
            {
                DefaultDistributedTracingOptions = new DistributedTracingOptions
                {
                    EnableAutomaticDistributedTracing = true
                }
            });

        var method = typeof(DistributedTracingPipelineBehavior<TraceableRequest, ResponseWithStatus>)
            .GetMethod("ExtractStatusInformation", BindingFlags.NonPublic | BindingFlags.Instance)
            .MakeGenericMethod(typeof(ResponseWithStatus));

        var response = new ResponseWithStatus { Status = "Success", IsSuccess = true, StatusCode = 200 };
        var tags = new Dictionary<string, object?>();

        method.Invoke(behavior, new object[] { response, tags });

        Assert.Contains("response.status", tags.Keys);
        Assert.Contains("response.issuccess", tags.Keys);
        Assert.Contains("response.statuscode", tags.Keys);
        Assert.Equal("Success", tags["response.status"]);
        Assert.Equal(true, tags["response.issuccess"]);
        Assert.Equal(200, tags["response.statuscode"]);
    }

    [Fact]
    public void SerializeMetadata_WithValidData_ShouldReturnJsonString()
    {
        // Use reflection to test private method
        var behavior = CreateBehaviorWithReflection<TraceableRequest, TestResponse>(
            new RelayOptions
            {
                DefaultDistributedTracingOptions = new DistributedTracingOptions
                {
                    EnableAutomaticDistributedTracing = true
                }
            });

        var method = typeof(DistributedTracingPipelineBehavior<TraceableRequest, TestResponse>)
            .GetMethod("SerializeMetadata", BindingFlags.NonPublic | BindingFlags.Instance);

        var metadata = new Dictionary<string, object?>
        {
            ["key1"] = "value1",
            ["key2"] = 42,
            ["key3"] = null
        };

        var result = (string)method.Invoke(behavior, new object[] { metadata });

        Assert.Contains("key1", result);
        Assert.Contains("value1", result);
        Assert.Contains("key2", result);
        Assert.Contains("42", result);
    }

    #endregion

    #region Test Helper Classes

    private class ProblematicObject
    {
        // This object might cause JSON serialization issues in some scenarios
        public object SelfReference { get; set; }

        public ProblematicObject()
        {
            SelfReference = this; // Circular reference
        }
    }

    private class ObjectWithIndexer
    {
        public string RegularProperty { get; set; } = "regular";

        public string this[int index]
        {
            get => $"item{index}";
            set { }
        }
    }

    public class ObjectWithBadProperty
    {
        public string BadProperty
        {
            get => throw new Exception("Property getter failed");
        }
    }

    private class ObjectWithBadField
    {
        public string BadField = null!;

        // Constructor that sets field to cause issues
        public ObjectWithBadField()
        {
            // This might cause issues during reflection
            BadField = null!;
        }
    }

    private class ObjectWithBadAttributes
    {
        // This class might cause issues when getting custom attributes
    }

    public class ResponseWithStatus
    {
        public string Status { get; set; } = "OK";
        public bool IsSuccess { get; set; } = true;
        public int StatusCode { get; set; } = 200;
    }

    private class ResponseWithMixedCaseStatus
    {
        public string status { get; set; } = "MixedCase"; // lowercase
        public bool ISSUCCESS { get; set; } = true; // uppercase
    }

    private class ResponseWithBadStatus
    {
        public string Status
        {
            get => throw new Exception("Status property failed");
        }
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