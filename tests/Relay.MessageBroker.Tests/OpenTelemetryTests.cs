using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Relay.MessageBroker.Telemetry;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class OpenTelemetryTests : IDisposable
{
    private readonly List<Activity> _exportedActivities = new();
    private readonly TracerProvider _tracerProvider;

    public OpenTelemetryTests()
    {
        _tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddRelayMessageBrokerInstrumentation(options =>
            {
                options.ServiceName = "TestService";
                options.ServiceVersion = "1.0.0";
                options.EnableTracing = true;
            })
            .AddInMemoryExporter(_exportedActivities)
            .Build()!;
    }

    public void Dispose()
    {
        _tracerProvider?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void AddRelayMessageBrokerInstrumentation_ShouldConfigureTracerProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        var exportedActivities = new List<Activity>();
        var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddRelayMessageBrokerInstrumentation(options =>
            {
                options.ServiceName = "MyTestService";
                options.ServiceVersion = "2.0.0";
                options.ResourceAttributes["custom.attribute"] = "custom.value";
            })
            .AddInMemoryExporter(exportedActivities)
            .Build();

        // Act
        using (var activity = MessageBrokerTelemetry.ActivitySource.StartActivity("test"))
        {
            activity.Should().NotBeNull();
        }

        tracerProvider.ForceFlush();

        // Assert - Take snapshot to avoid enumeration issues
        var activitiesSnapshot = exportedActivities.ToList();
        activitiesSnapshot.Should().HaveCount(1);

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
        using var activity = OpenTelemetryExtensions.StartPublishActivity(destination, messagingSystem);

        // Assert
        activity.Should().NotBeNull();
        activity!.DisplayName.Should().Be($"{destination} publish");
        activity.Kind.Should().Be(ActivityKind.Producer);
        activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessagingSystem).Should().Be(messagingSystem);
        activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessagingDestination).Should().Be(destination);
        activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessagingOperation).Should().Be("publish");
    }

    [Fact]
    public void StartProcessActivity_ShouldCreateActivityWithCorrectAttributes()
    {
        // Arrange
        const string destination = "test-queue";
        const string messagingSystem = "kafka";

        // Act
        using var activity = OpenTelemetryExtensions.StartProcessActivity(destination, messagingSystem);

        // Assert
        activity.Should().NotBeNull();
        activity!.DisplayName.Should().Be($"{destination} process");
        activity.Kind.Should().Be(ActivityKind.Consumer);
        activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessagingSystem).Should().Be(messagingSystem);
        activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessagingDestination).Should().Be(destination);
        activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessagingOperation).Should().Be("process");
    }

    [Fact]
    public void AddMessageAttributes_ShouldAddCorrectTags()
    {
        // Arrange
        using var activity = OpenTelemetryExtensions.StartPublishActivity("test-topic", "test");
        const string messageType = "TestMessage";
        const string messageId = "msg-123";
        const int payloadSize = 1024;

        // Act
        activity.AddMessageAttributes(messageType, messageId, payloadSize);

        // Assert
        activity!.GetTagItem(MessageBrokerTelemetry.Attributes.MessageType).Should().Be(messageType);
        activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessagingMessageId).Should().Be(messageId);
        activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessagingPayloadSize).Should().Be(payloadSize);
    }

    [Fact]
    public void AddCompressionAttributes_ShouldCalculateCompressionRatio()
    {
        // Arrange
        using var activity = OpenTelemetryExtensions.StartPublishActivity("test-topic", "test");
        const string algorithm = "gzip";
        const int originalSize = 1000;
        const int compressedSize = 300;

        // Act
        activity.AddCompressionAttributes(algorithm, originalSize, compressedSize);

        // Assert
        activity!.GetTagItem(MessageBrokerTelemetry.Attributes.MessageCompressed).Should().Be(true);
        activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessageCompressionAlgorithm).Should().Be(algorithm);
        activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessagingPayloadSize).Should().Be(originalSize);
        activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessagingPayloadCompressedSize).Should().Be(compressedSize);
        
        var ratio = activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessageCompressionRatio);
        ratio.Should().NotBeNull();
        ((double)ratio!).Should().BeApproximately(0.3, 0.01);
    }

    [Fact]
    public void AddCircuitBreakerAttributes_ShouldAddCorrectTags()
    {
        // Arrange
        using var activity = OpenTelemetryExtensions.StartPublishActivity("test-topic", "test");
        const string name = "test-breaker";
        const string state = "Open";

        // Act
        activity.AddCircuitBreakerAttributes(name, state);

        // Assert
        activity!.GetTagItem(MessageBrokerTelemetry.Attributes.CircuitBreakerName).Should().Be(name);
        activity.GetTagItem(MessageBrokerTelemetry.Attributes.CircuitBreakerState).Should().Be(state);
    }

    [Fact]
    public void RecordError_ShouldSetStatusAndAddErrorTags()
    {
        // Arrange
        using var activity = OpenTelemetryExtensions.StartPublishActivity("test-topic", "test");
        var exception = new InvalidOperationException("Test error");

        // Act
        activity.RecordError(exception, captureStackTrace: true);

        // Assert
        activity!.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be(exception.Message);
        activity.GetTagItem(MessageBrokerTelemetry.Attributes.ErrorType).Should().Be(exception.GetType().FullName);
        activity.GetTagItem(MessageBrokerTelemetry.Attributes.ErrorMessage).Should().Be(exception.Message);
    }

    [Fact]
    public void RecordError_WithoutStackTrace_ShouldNotCaptureStackTrace()
    {
        // Arrange
        using var activity = OpenTelemetryExtensions.StartPublishActivity("test-topic", "test");
        var exception = new InvalidOperationException("Test error");

        // Act
        activity.RecordError(exception, captureStackTrace: false);

        // Assert
        activity!.GetTagItem(MessageBrokerTelemetry.Attributes.ErrorStackTrace).Should().BeNull();
    }

    [Fact]
    public void AddMessageEvent_ShouldAddEventWithAttributes()
    {
        // Arrange
        using var activity = OpenTelemetryExtensions.StartPublishActivity("test-topic", "test");
        const string eventName = "TestEvent";
        var attributes = new Dictionary<string, object?>
        {
            { "key1", "value1" },
            { "key2", 123 }
        };

        // Act
        activity.AddMessageEvent(eventName, attributes);

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
        using var activity = OpenTelemetryExtensions.StartPublishActivity("test-topic", "test");
        const string eventName = "TestEvent";

        // Act
        Action act = () => activity.AddMessageEvent(eventName, null);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void TelemetryOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new TelemetryOptions();

        // Assert
        options.Enabled.Should().BeTrue();
        options.EnableTracing.Should().BeTrue();
        options.EnableMetrics.Should().BeTrue();
        options.EnableLogging.Should().BeTrue();
        options.ServiceName.Should().Be("Relay.MessageBroker");
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
        MessageBrokerTelemetry.ActivitySourceName.Should().Be("Relay.MessageBroker");
        MessageBrokerTelemetry.MeterName.Should().Be("Relay.MessageBroker");
    }

    [Fact]
    public void AddMessageAttributes_WithNullActivity_ShouldNotThrow()
    {
        // Arrange
        Activity? activity = null;

        // Act
        Action act = () => activity.AddMessageAttributes("TestMessage", "msg-123", 1024);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void AddCompressionAttributes_WithNullActivity_ShouldNotThrow()
    {
        // Arrange
        Activity? activity = null;

        // Act
        Action act = () => activity.AddCompressionAttributes("gzip", 1000, 300);

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
        Action act = () => activity.RecordError(exception);

        // Assert
        act.Should().NotThrow();
    }
}