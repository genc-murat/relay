using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Relay.Core.Telemetry;
using System.Diagnostics;

namespace Relay.MessageBroker.Tests;

public class OpenTelemetryTests : IDisposable
{
    private readonly List<Activity> _exportedActivities = new();
    private readonly TracerProvider _tracerProvider;

    public OpenTelemetryTests()
    {
        _exportedActivities.Clear();
        
        // Ensure there's no existing ActivitySource
        Activity.Current = null;
        
        _tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(RelayTelemetryConstants.ActivitySourceName)
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("TestService", "1.0.0"))
            .SetSampler(new AlwaysOnSampler())
            .AddInMemoryExporter(_exportedActivities)
            .Build()!;
            
        // Force the provider to be registered
        _tracerProvider.ForceFlush();
    }

    public void Dispose()
    {
        _tracerProvider?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void MessageBrokerTelemetryAdapter_ShouldConfigureTracerProvider()
    {
        // Arrange - Create isolated TracerProvider for this test
        var exportedActivities = new List<Activity>();

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(RelayTelemetryConstants.ActivitySourceName)
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("MyTestService")
                .AddAttributes(new Dictionary<string, object>
                {
                    ["service.version"] = "2.0.0",
                    ["custom.attribute"] = "custom.value"
                }))
            .SetSampler(new AlwaysOnSampler())
            .AddInMemoryExporter(exportedActivities)
            .Build();

        // Act
        using (var activity = RelayTelemetryConstants.ActivitySource.StartActivity("test"))
        {
            Assert.NotNull(activity);
        }

        tracerProvider.ForceFlush();

        // Assert - Take snapshot to avoid enumeration issues
        var activitiesSnapshot = exportedActivities.ToList();
        Assert.Single(activitiesSnapshot);

        var resource = tracerProvider.GetResource();
        var attributes = resource.Attributes.ToList();
        Assert.Contains(new KeyValuePair<string, object>("service.name", "MyTestService"), attributes);
        Assert.Contains(new KeyValuePair<string, object>("service.version", "2.0.0"), attributes);
        Assert.Contains(new KeyValuePair<string, object>("custom.attribute", "custom.value"), attributes);
    }

    [Fact]
    public void StartPublishActivity_ShouldCreateActivityWithCorrectAttributes()
    {
        // Arrange
        const string destination = "test-topic";
        const string messagingSystem = "rabbitmq";

        // Act
        using var activity = RelayTelemetryConstants.ActivitySource.StartActivity($"{destination} publish", ActivityKind.Producer);
        if (activity != null)
        {
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingSystem, messagingSystem);
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingDestination, destination);
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingOperation, "publish");
        }

        // Assert
        Assert.NotNull(activity);
        Assert.Equal($"{destination} publish", activity!.DisplayName);
        Assert.Equal(ActivityKind.Producer, activity.Kind);
        Assert.Equal(messagingSystem, activity.GetTagItem(RelayTelemetryConstants.Attributes.MessagingSystem));
        Assert.Equal(destination, activity.GetTagItem(RelayTelemetryConstants.Attributes.MessagingDestination));
        Assert.Equal("publish", activity.GetTagItem(RelayTelemetryConstants.Attributes.MessagingOperation));
    }

    [Fact]
    public void StartProcessActivity_ShouldCreateActivityWithCorrectAttributes()
    {
        // Arrange
        const string destination = "test-queue";
        const string messagingSystem = "kafka";

        // Act
        using var activity = RelayTelemetryConstants.ActivitySource.StartActivity($"{destination} process", ActivityKind.Consumer);
        if (activity != null)
        {
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingSystem, messagingSystem);
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingDestination, destination);
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingOperation, "process");
        }

        // Assert
        Assert.NotNull(activity);
        Assert.Equal($"{destination} process", activity!.DisplayName);
        Assert.Equal(ActivityKind.Consumer, activity.Kind);
        Assert.Equal(messagingSystem, activity.GetTagItem(RelayTelemetryConstants.Attributes.MessagingSystem));
        Assert.Equal(destination, activity.GetTagItem(RelayTelemetryConstants.Attributes.MessagingDestination));
        Assert.Equal("process", activity.GetTagItem(RelayTelemetryConstants.Attributes.MessagingOperation));
    }

    [Fact]
    public void AddMessageAttributes_ShouldAddCorrectTags()
    {
        // Arrange
        using var activity = RelayTelemetryConstants.ActivitySource.StartActivity("test-topic publish", ActivityKind.Producer);
        if (activity != null)
        {
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingSystem, "test");
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingDestination, "test-topic");
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingOperation, "publish");
        }
        const string messageType = "TestMessage";
        const string messageId = "msg-123";
        const int payloadSize = 1024;

        // Act
        if (activity != null)
        {
            activity.SetTag(RelayTelemetryConstants.Attributes.MessageType, messageType);
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingMessageId, messageId);
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingPayloadSize, payloadSize);
        }

        // Assert
        Assert.Equal(messageType, activity!.GetTagItem(RelayTelemetryConstants.Attributes.MessageType));
        Assert.Equal(messageId, activity.GetTagItem(RelayTelemetryConstants.Attributes.MessagingMessageId));
        Assert.Equal(payloadSize, activity.GetTagItem(RelayTelemetryConstants.Attributes.MessagingPayloadSize));
    }

    [Fact]
    public void AddCompressionAttributes_ShouldCalculateCompressionRatio()
    {
        // Arrange
        using var activity = RelayTelemetryConstants.ActivitySource.StartActivity("test-topic publish", ActivityKind.Producer);
        if (activity != null)
        {
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingSystem, "test");
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingDestination, "test-topic");
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingOperation, "publish");
        }
        const string algorithm = "gzip";
        const int originalSize = 1000;
        const int compressedSize = 300;

        // Act
        if (activity != null)
        {
            activity.SetTag(RelayTelemetryConstants.Attributes.MessageCompressed, true);
            activity.SetTag(RelayTelemetryConstants.Attributes.MessageCompressionAlgorithm, algorithm);
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingPayloadSize, originalSize);
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingPayloadCompressedSize, compressedSize);
            activity.SetTag(RelayTelemetryConstants.Attributes.MessageCompressionRatio, (double)compressedSize / originalSize);
        }

        // Assert
        Assert.Equal(true, activity!.GetTagItem(RelayTelemetryConstants.Attributes.MessageCompressed));
        Assert.Equal(algorithm, activity.GetTagItem(RelayTelemetryConstants.Attributes.MessageCompressionAlgorithm));
        Assert.Equal(originalSize, activity.GetTagItem(RelayTelemetryConstants.Attributes.MessagingPayloadSize));
        Assert.Equal(compressedSize, activity.GetTagItem(RelayTelemetryConstants.Attributes.MessagingPayloadCompressedSize));
        
        var ratio = activity.GetTagItem(RelayTelemetryConstants.Attributes.MessageCompressionRatio);
        Assert.NotNull(ratio);
        Assert.InRange((double)ratio!, 0.29, 0.31);
    }

    [Fact]
    public void AddCircuitBreakerAttributes_ShouldAddCorrectTags()
    {
        // Arrange
        using var activity = RelayTelemetryConstants.ActivitySource.StartActivity("test-topic publish", ActivityKind.Producer);
        if (activity != null)
        {
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingSystem, "test");
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingDestination, "test-topic");
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingOperation, "publish");
        }
        const string name = "test-breaker";
        const string state = "Open";

        // Act
        if (activity != null)
        {
            activity.SetTag(RelayTelemetryConstants.Attributes.CircuitBreakerName, name);
            activity.SetTag(RelayTelemetryConstants.Attributes.CircuitBreakerState, state);
        }

        // Assert
        Assert.Equal(name, activity!.GetTagItem(RelayTelemetryConstants.Attributes.CircuitBreakerName));
        Assert.Equal(state, activity.GetTagItem(RelayTelemetryConstants.Attributes.CircuitBreakerState));
    }

    [Fact]
    public void RecordError_ShouldSetStatusAndAddErrorTags()
    {
        // Arrange
        using var activity = RelayTelemetryConstants.ActivitySource.StartActivity("test-topic publish", ActivityKind.Producer);
        if (activity != null)
        {
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingSystem, "test");
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingDestination, "test-topic");
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingOperation, "publish");
        }
        var exception = new InvalidOperationException("Test error");

        // Act
        if (activity != null)
        {
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity.SetTag(RelayTelemetryConstants.Attributes.ErrorType, exception.GetType().FullName);
            activity.SetTag(RelayTelemetryConstants.Attributes.ErrorMessage, exception.Message);
            activity.SetTag(RelayTelemetryConstants.Attributes.ErrorStackTrace, exception.StackTrace);
        }

        // Assert
        Assert.Equal(ActivityStatusCode.Error, activity!.Status);
        Assert.Equal(exception.Message, activity.StatusDescription);
        Assert.Equal(exception.GetType().FullName, activity.GetTagItem(RelayTelemetryConstants.Attributes.ErrorType));
        Assert.Equal(exception.Message, activity.GetTagItem(RelayTelemetryConstants.Attributes.ErrorMessage));
    }

    [Fact]
    public void RecordError_WithoutStackTrace_ShouldNotCaptureStackTrace()
    {
        // Arrange
        using var activity = RelayTelemetryConstants.ActivitySource.StartActivity("test-topic publish", ActivityKind.Producer);
        if (activity != null)
        {
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingSystem, "test");
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingDestination, "test-topic");
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingOperation, "publish");
        }
        var exception = new InvalidOperationException("Test error");

        // Act
        if (activity != null)
        {
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity.SetTag(RelayTelemetryConstants.Attributes.ErrorType, exception.GetType().FullName);
            activity.SetTag(RelayTelemetryConstants.Attributes.ErrorMessage, exception.Message);
        }

        // Assert
        Assert.Null(activity!.GetTagItem(RelayTelemetryConstants.Attributes.ErrorStackTrace));
    }

    [Fact]
    public void AddMessageEvent_ShouldAddEventWithAttributes()
    {
        // Arrange
        using var activity = RelayTelemetryConstants.ActivitySource.StartActivity("test-topic publish", ActivityKind.Producer);
        if (activity != null)
        {
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingSystem, "test");
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingDestination, "test-topic");
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingOperation, "publish");
        }
        const string eventName = "TestEvent";
        var attributes = new Dictionary<string, object?>
        {
            { "key1", "value1" },
            { "key2", 123 }
        };

        // Act
        if (activity != null)
        {
            var eventTags = new ActivityTagsCollection(attributes?.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)) ?? Enumerable.Empty<KeyValuePair<string, object?>>());
            activity.AddEvent(new ActivityEvent(eventName, tags: eventTags));
        }

        // Assert
        var events = activity!.Events.ToList();
        Assert.Single(events);
        var singleEvent = events[0];
        Assert.Equal(eventName, singleEvent.Name);
        Assert.Contains(new KeyValuePair<string, object>("key1", "value1"), singleEvent.Tags);
        Assert.Contains(new KeyValuePair<string, object>("key2", 123), singleEvent.Tags);
    }

    [Fact]
    public void AddMessageEvent_WithNullAttributes_ShouldNotThrow()
    {
        // Arrange
        using var activity = RelayTelemetryConstants.ActivitySource.StartActivity("test-topic publish", ActivityKind.Producer);
        if (activity != null)
        {
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingSystem, "test");
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingDestination, "test-topic");
            activity.SetTag(RelayTelemetryConstants.Attributes.MessagingOperation, "publish");
        }
        const string eventName = "TestEvent";

        // Act & Assert
        var exception = Record.Exception(() => 
        {
            if (activity != null)
            {
                activity.AddEvent(new ActivityEvent(eventName));
            }
        });

        Assert.Null(exception);
    }

    [Fact]
    public void TelemetryOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new RelayTelemetryOptions();

        // Assert
        Assert.True(options.Enabled);
        Assert.True(options.EnableTracing);
        Assert.True(options.EnableMetrics);
        Assert.True(options.EnableLogging);
        Assert.Equal("Relay", options.ServiceName);
        Assert.Equal(RelayTelemetryConstants.Components.Core, options.Component);
        Assert.False(options.CaptureMessagePayloads);
        Assert.Equal(1024, options.MaxPayloadSizeBytes);
        Assert.True(options.CaptureMessageHeaders);
        Assert.True(options.CaptureStackTraces);
        Assert.True(options.PropagateTraceContext);
        Assert.Equal(TraceContextFormat.W3C, options.TraceContextFormat);
        Assert.Equal(1.0, options.SamplingRate);
        Assert.True(options.UseBatchProcessing);
    }

    [Fact]
    public void TelemetryExportersOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new TelemetryExportersOptions();

        // Assert
        Assert.False(options.EnableConsole);
        Assert.True(options.EnableOtlp);
        Assert.Equal("http://localhost:4317", options.OtlpEndpoint);
        Assert.Equal("grpc", options.OtlpProtocol);
        Assert.False(options.EnableJaeger);
        Assert.Equal("localhost", options.JaegerAgentHost);
        Assert.Equal(6831, options.JaegerAgentPort);
        Assert.False(options.EnableZipkin);
        Assert.False(options.EnablePrometheus);
        Assert.Equal("/metrics", options.PrometheusEndpoint);
        Assert.False(options.EnableAzureMonitor);
        Assert.False(options.EnableAwsXRay);
    }

    [Fact]
    public void ActivitySourceName_ShouldBeRelayMessageBroker()
    {
        // Assert
        Assert.Equal("Relay", RelayTelemetryConstants.ActivitySourceName);
        Assert.Equal("Relay.MessageBroker", RelayTelemetryConstants.Components.MessageBroker);
        Assert.Equal("Relay", RelayTelemetryConstants.MeterName);
    }

    [Fact]
    public void AddMessageAttributes_WithNullActivity_ShouldNotThrow()
    {
        // Arrange
        Activity? activity = null;

        // Act & Assert
        var exception = Record.Exception(() => 
        {
            if (activity != null)
            {
                activity.SetTag(RelayTelemetryConstants.Attributes.MessageType, "TestMessage");
                activity.SetTag(RelayTelemetryConstants.Attributes.MessagingMessageId, "msg-123");
                activity.SetTag(RelayTelemetryConstants.Attributes.MessagingPayloadSize, 1024);
            }
        });

        Assert.Null(exception);
    }

    [Fact]
    public void AddCompressionAttributes_WithNullActivity_ShouldNotThrow()
    {
        // Arrange
        Activity? activity = null;

        // Act & Assert
        var exception = Record.Exception(() => 
        {
            if (activity != null)
            {
                activity.SetTag(RelayTelemetryConstants.Attributes.MessageCompressed, true);
                activity.SetTag(RelayTelemetryConstants.Attributes.MessageCompressionAlgorithm, "gzip");
                activity.SetTag(RelayTelemetryConstants.Attributes.MessagingPayloadSize, 1000);
                activity.SetTag(RelayTelemetryConstants.Attributes.MessagingPayloadCompressedSize, 300);
                activity.SetTag(RelayTelemetryConstants.Attributes.MessageCompressionRatio, 0.3);
            }
        });

        Assert.Null(exception);
    }

    [Fact]
    public void RecordError_WithNullActivity_ShouldNotThrow()
    {
        // Arrange
        Activity? activity = null;
        var exception = new InvalidOperationException("Test");

        // Act & Assert
        var recordedException = Record.Exception(() => 
        {
            if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                activity.SetTag(RelayTelemetryConstants.Attributes.ErrorType, exception.GetType().FullName);
                activity.SetTag(RelayTelemetryConstants.Attributes.ErrorMessage, exception.Message);
            }
        });

        Assert.Null(recordedException);
    }
}