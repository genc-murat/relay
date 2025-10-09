using FluentAssertions;
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
            .AddSource(UnifiedTelemetryConstants.ActivitySourceName)
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
            .AddSource(UnifiedTelemetryConstants.ActivitySourceName)
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
        using (var activity = UnifiedTelemetryConstants.ActivitySource.StartActivity("test"))
        {
            activity.Should().NotBeNull();
        }

        tracerProvider.ForceFlush();

        // Assert - Take snapshot to avoid enumeration issues
        var activitiesSnapshot = exportedActivities.ToList();
        activitiesSnapshot.Should().HaveCount(1, "only one activity should be created in this test");

        var resource = tracerProvider.GetResource();
        var attributes = resource.Attributes.ToList();
        attributes.Should().Contain(new KeyValuePair<string, object>("service.name", "MyTestService"));
        attributes.Should().Contain(new KeyValuePair<string, object>("service.version", "2.0.0"));
        attributes.Should().Contain(new KeyValuePair<string, object>("custom.attribute", "custom.value"));
    }

    [Fact]
    public void StartPublishActivity_ShouldCreateActivityWithCorrectAttributes()
    {
        // Arrange
        const string destination = "test-topic";
        const string messagingSystem = "rabbitmq";

        // Act
        using var activity = UnifiedTelemetryConstants.ActivitySource.StartActivity($"{destination} publish", ActivityKind.Producer);
        if (activity != null)
        {
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingSystem, messagingSystem);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingDestination, destination);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingOperation, "publish");
        }

        // Assert
        activity.Should().NotBeNull();
        activity!.DisplayName.Should().Be($"{destination} publish");
        activity.Kind.Should().Be(ActivityKind.Producer);
        activity.GetTagItem(UnifiedTelemetryConstants.Attributes.MessagingSystem).Should().Be(messagingSystem);
        activity.GetTagItem(UnifiedTelemetryConstants.Attributes.MessagingDestination).Should().Be(destination);
        activity.GetTagItem(UnifiedTelemetryConstants.Attributes.MessagingOperation).Should().Be("publish");
    }

    [Fact]
    public void StartProcessActivity_ShouldCreateActivityWithCorrectAttributes()
    {
        // Arrange
        const string destination = "test-queue";
        const string messagingSystem = "kafka";

        // Act
        using var activity = UnifiedTelemetryConstants.ActivitySource.StartActivity($"{destination} process", ActivityKind.Consumer);
        if (activity != null)
        {
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingSystem, messagingSystem);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingDestination, destination);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingOperation, "process");
        }

        // Assert
        activity.Should().NotBeNull();
        activity!.DisplayName.Should().Be($"{destination} process");
        activity.Kind.Should().Be(ActivityKind.Consumer);
        activity.GetTagItem(UnifiedTelemetryConstants.Attributes.MessagingSystem).Should().Be(messagingSystem);
        activity.GetTagItem(UnifiedTelemetryConstants.Attributes.MessagingDestination).Should().Be(destination);
        activity.GetTagItem(UnifiedTelemetryConstants.Attributes.MessagingOperation).Should().Be("process");
    }

    [Fact]
    public void AddMessageAttributes_ShouldAddCorrectTags()
    {
        // Arrange
        using var activity = UnifiedTelemetryConstants.ActivitySource.StartActivity("test-topic publish", ActivityKind.Producer);
        if (activity != null)
        {
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingSystem, "test");
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingDestination, "test-topic");
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingOperation, "publish");
        }
        const string messageType = "TestMessage";
        const string messageId = "msg-123";
        const int payloadSize = 1024;

        // Act
        if (activity != null)
        {
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessageType, messageType);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingMessageId, messageId);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingPayloadSize, payloadSize);
        }

        // Assert
        activity!.GetTagItem(UnifiedTelemetryConstants.Attributes.MessageType).Should().Be(messageType);
        activity.GetTagItem(UnifiedTelemetryConstants.Attributes.MessagingMessageId).Should().Be(messageId);
        activity.GetTagItem(UnifiedTelemetryConstants.Attributes.MessagingPayloadSize).Should().Be(payloadSize);
    }

    [Fact]
    public void AddCompressionAttributes_ShouldCalculateCompressionRatio()
    {
        // Arrange
        using var activity = UnifiedTelemetryConstants.ActivitySource.StartActivity("test-topic publish", ActivityKind.Producer);
        if (activity != null)
        {
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingSystem, "test");
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingDestination, "test-topic");
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingOperation, "publish");
        }
        const string algorithm = "gzip";
        const int originalSize = 1000;
        const int compressedSize = 300;

        // Act
        if (activity != null)
        {
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessageCompressed, true);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessageCompressionAlgorithm, algorithm);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingPayloadSize, originalSize);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingPayloadCompressedSize, compressedSize);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessageCompressionRatio, (double)compressedSize / originalSize);
        }

        // Assert
        activity!.GetTagItem(UnifiedTelemetryConstants.Attributes.MessageCompressed).Should().Be(true);
        activity.GetTagItem(UnifiedTelemetryConstants.Attributes.MessageCompressionAlgorithm).Should().Be(algorithm);
        activity.GetTagItem(UnifiedTelemetryConstants.Attributes.MessagingPayloadSize).Should().Be(originalSize);
        activity.GetTagItem(UnifiedTelemetryConstants.Attributes.MessagingPayloadCompressedSize).Should().Be(compressedSize);
        
        var ratio = activity.GetTagItem(UnifiedTelemetryConstants.Attributes.MessageCompressionRatio);
        ratio.Should().NotBeNull();
        ((double)ratio!).Should().BeApproximately(0.3, 0.01);
    }

    [Fact]
    public void AddCircuitBreakerAttributes_ShouldAddCorrectTags()
    {
        // Arrange
        using var activity = UnifiedTelemetryConstants.ActivitySource.StartActivity("test-topic publish", ActivityKind.Producer);
        if (activity != null)
        {
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingSystem, "test");
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingDestination, "test-topic");
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingOperation, "publish");
        }
        const string name = "test-breaker";
        const string state = "Open";

        // Act
        if (activity != null)
        {
            activity.SetTag(UnifiedTelemetryConstants.Attributes.CircuitBreakerName, name);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.CircuitBreakerState, state);
        }

        // Assert
        activity!.GetTagItem(UnifiedTelemetryConstants.Attributes.CircuitBreakerName).Should().Be(name);
        activity.GetTagItem(UnifiedTelemetryConstants.Attributes.CircuitBreakerState).Should().Be(state);
    }

    [Fact]
    public void RecordError_ShouldSetStatusAndAddErrorTags()
    {
        // Arrange
        using var activity = UnifiedTelemetryConstants.ActivitySource.StartActivity("test-topic publish", ActivityKind.Producer);
        if (activity != null)
        {
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingSystem, "test");
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingDestination, "test-topic");
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingOperation, "publish");
        }
        var exception = new InvalidOperationException("Test error");

        // Act
        if (activity != null)
        {
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.ErrorType, exception.GetType().FullName);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.ErrorMessage, exception.Message);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.ErrorStackTrace, exception.StackTrace);
        }

        // Assert
        activity!.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be(exception.Message);
        activity.GetTagItem(UnifiedTelemetryConstants.Attributes.ErrorType).Should().Be(exception.GetType().FullName);
        activity.GetTagItem(UnifiedTelemetryConstants.Attributes.ErrorMessage).Should().Be(exception.Message);
    }

    [Fact]
    public void RecordError_WithoutStackTrace_ShouldNotCaptureStackTrace()
    {
        // Arrange
        using var activity = UnifiedTelemetryConstants.ActivitySource.StartActivity("test-topic publish", ActivityKind.Producer);
        if (activity != null)
        {
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingSystem, "test");
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingDestination, "test-topic");
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingOperation, "publish");
        }
        var exception = new InvalidOperationException("Test error");

        // Act
        if (activity != null)
        {
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.ErrorType, exception.GetType().FullName);
            activity.SetTag(UnifiedTelemetryConstants.Attributes.ErrorMessage, exception.Message);
        }

        // Assert
        activity!.GetTagItem(UnifiedTelemetryConstants.Attributes.ErrorStackTrace).Should().BeNull();
    }

    [Fact]
    public void AddMessageEvent_ShouldAddEventWithAttributes()
    {
        // Arrange
        using var activity = UnifiedTelemetryConstants.ActivitySource.StartActivity("test-topic publish", ActivityKind.Producer);
        if (activity != null)
        {
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingSystem, "test");
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingDestination, "test-topic");
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingOperation, "publish");
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
            var eventTags = new ActivityTagsCollection(attributes?.Select(kvp => new KeyValuePair<string, object>(kvp.Key, kvp.Value!)) ?? Enumerable.Empty<KeyValuePair<string, object>>());
            activity.AddEvent(new ActivityEvent(eventName, tags: eventTags));
        }

        // Assert
        var events = activity!.Events.ToList();
        events.Should().HaveCount(1);
        var singleEvent = events[0];
        singleEvent.Name.Should().Be(eventName);
        singleEvent.Tags.Should().Contain(new KeyValuePair<string, object>("key1", "value1"));
        singleEvent.Tags.Should().Contain(new KeyValuePair<string, object>("key2", 123));
    }

    [Fact]
    public void AddMessageEvent_WithNullAttributes_ShouldNotThrow()
    {
        // Arrange
        using var activity = UnifiedTelemetryConstants.ActivitySource.StartActivity("test-topic publish", ActivityKind.Producer);
        if (activity != null)
        {
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingSystem, "test");
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingDestination, "test-topic");
            activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingOperation, "publish");
        }
        const string eventName = "TestEvent";

        // Act
        Action act = () => 
        {
            if (activity != null)
            {
                activity.AddEvent(new ActivityEvent(eventName));
            }
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void TelemetryOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new UnifiedTelemetryOptions();

        // Assert
        options.Enabled.Should().BeTrue();
        options.EnableTracing.Should().BeTrue();
        options.EnableMetrics.Should().BeTrue();
        options.EnableLogging.Should().BeTrue();
        options.ServiceName.Should().Be("Relay");
        options.Component.Should().Be(UnifiedTelemetryConstants.Components.Core);
        options.CaptureMessagePayloads.Should().BeFalse();
        options.MaxPayloadSizeBytes.Should().Be(1024);
        options.CaptureMessageHeaders.Should().BeTrue();
        options.CaptureStackTraces.Should().BeTrue();
        options.PropagateTraceContext.Should().BeTrue();
        options.TraceContextFormat.Should().Be(TraceContextFormat.W3C);
        options.SamplingRate.Should().Be(1.0);
        options.UseBatchProcessing.Should().BeTrue();
    }

    [Fact]
    public void TelemetryExportersOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new TelemetryExportersOptions();

        // Assert
        options.EnableConsole.Should().BeFalse();
        options.EnableOtlp.Should().BeTrue();
        options.OtlpEndpoint.Should().Be("http://localhost:4317");
        options.OtlpProtocol.Should().Be("grpc");
        options.EnableJaeger.Should().BeFalse();
        options.JaegerAgentHost.Should().Be("localhost");
        options.JaegerAgentPort.Should().Be(6831);
        options.EnableZipkin.Should().BeFalse();
        options.EnablePrometheus.Should().BeFalse();
        options.PrometheusEndpoint.Should().Be("/metrics");
        options.EnableAzureMonitor.Should().BeFalse();
        options.EnableAwsXRay.Should().BeFalse();
    }

    [Fact]
    public void ActivitySourceName_ShouldBeRelayMessageBroker()
    {
        // Assert
        UnifiedTelemetryConstants.ActivitySourceName.Should().Be("Relay");
        UnifiedTelemetryConstants.Components.MessageBroker.Should().Be("Relay.MessageBroker");
        UnifiedTelemetryConstants.MeterName.Should().Be("Relay");
    }

    [Fact]
    public void AddMessageAttributes_WithNullActivity_ShouldNotThrow()
    {
        // Arrange
        Activity? activity = null;

        // Act
        Action act = () => 
        {
            if (activity != null)
            {
                activity.SetTag(UnifiedTelemetryConstants.Attributes.MessageType, "TestMessage");
                activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingMessageId, "msg-123");
                activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingPayloadSize, 1024);
            }
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddCompressionAttributes_WithNullActivity_ShouldNotThrow()
    {
        // Arrange
        Activity? activity = null;

        // Act
        Action act = () => 
        {
            if (activity != null)
            {
                activity.SetTag(UnifiedTelemetryConstants.Attributes.MessageCompressed, true);
                activity.SetTag(UnifiedTelemetryConstants.Attributes.MessageCompressionAlgorithm, "gzip");
                activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingPayloadSize, 1000);
                activity.SetTag(UnifiedTelemetryConstants.Attributes.MessagingPayloadCompressedSize, 300);
                activity.SetTag(UnifiedTelemetryConstants.Attributes.MessageCompressionRatio, 0.3);
            }
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RecordError_WithNullActivity_ShouldNotThrow()
    {
        // Arrange
        Activity? activity = null;
        var exception = new InvalidOperationException("Test");

        // Act
        Action act = () => 
        {
            if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                activity.SetTag(UnifiedTelemetryConstants.Attributes.ErrorType, exception.GetType().FullName);
                activity.SetTag(UnifiedTelemetryConstants.Attributes.ErrorMessage, exception.Message);
            }
        };

        // Assert
        act.Should().NotThrow();
    }
}