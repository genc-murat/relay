using System.Diagnostics;
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
    public void StartPublishActivity_ShouldCreateActivityWithCorrectAttributes()
    {
        // Arrange
        const string destination = "test-topic";
        const string messagingSystem = "rabbitmq";

        // Act
        using var activity = OpenTelemetryExtensions.StartPublishActivity(destination, messagingSystem);

        // Assert
        Assert.NotNull(activity);
        Assert.Equal($"{destination} publish", activity.DisplayName);
        Assert.Equal(ActivityKind.Producer, activity.Kind);
        Assert.Equal(messagingSystem, activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessagingSystem));
        Assert.Equal(destination, activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessagingDestination));
        Assert.Equal("publish", activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessagingOperation));
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
        Assert.NotNull(activity);
        Assert.Equal($"{destination} process", activity.DisplayName);
        Assert.Equal(ActivityKind.Consumer, activity.Kind);
        Assert.Equal(messagingSystem, activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessagingSystem));
        Assert.Equal(destination, activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessagingDestination));
        Assert.Equal("process", activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessagingOperation));
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
        Assert.Equal(messageType, activity!.GetTagItem(MessageBrokerTelemetry.Attributes.MessageType));
        Assert.Equal(messageId, activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessagingMessageId));
        Assert.Equal(payloadSize, activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessagingPayloadSize));
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
        Assert.Equal(true, activity!.GetTagItem(MessageBrokerTelemetry.Attributes.MessageCompressed));
        Assert.Equal(algorithm, activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessageCompressionAlgorithm));
        Assert.Equal(originalSize, activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessagingPayloadSize));
        Assert.Equal(compressedSize, activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessagingPayloadCompressedSize));
        
        var ratio = activity.GetTagItem(MessageBrokerTelemetry.Attributes.MessageCompressionRatio);
        Assert.NotNull(ratio);
        Assert.Equal(0.3, (double)ratio!, precision: 2);
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
        Assert.Equal(name, activity!.GetTagItem(MessageBrokerTelemetry.Attributes.CircuitBreakerName));
        Assert.Equal(state, activity.GetTagItem(MessageBrokerTelemetry.Attributes.CircuitBreakerState));
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
        Assert.Equal(ActivityStatusCode.Error, activity!.Status);
        Assert.Equal(exception.Message, activity.StatusDescription);
        Assert.Equal(exception.GetType().FullName, activity.GetTagItem(MessageBrokerTelemetry.Attributes.ErrorType));
        Assert.Equal(exception.Message, activity.GetTagItem(MessageBrokerTelemetry.Attributes.ErrorMessage));
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
        Assert.Null(activity!.GetTagItem(MessageBrokerTelemetry.Attributes.ErrorStackTrace));
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
        Assert.Single(events);
        Assert.Equal(eventName, events[0].Name);
    }

    [Fact]
    public void AddMessageEvent_WithNullAttributes_ShouldNotThrow()
    {
        // Arrange
        using var activity = OpenTelemetryExtensions.StartPublishActivity("test-topic", "test");
        const string eventName = "TestEvent";

        // Act & Assert
        var exception = Record.Exception(() => activity.AddMessageEvent(eventName, null));
        Assert.Null(exception);
    }

    [Fact]
    public void TelemetryOptions_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var options = new TelemetryOptions();

        // Assert
        Assert.True(options.Enabled);
        Assert.True(options.EnableTracing);
        Assert.True(options.EnableMetrics);
        Assert.True(options.EnableLogging);
        Assert.Equal("Relay.MessageBroker", options.ServiceName);
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
        Assert.Equal("Relay.MessageBroker", MessageBrokerTelemetry.ActivitySourceName);
        Assert.Equal("Relay.MessageBroker", MessageBrokerTelemetry.MeterName);
    }

    [Fact]
    public void AddMessageAttributes_WithNullActivity_ShouldNotThrow()
    {
        // Arrange
        Activity? activity = null;

        // Act & Assert
        var exception = Record.Exception(() => 
            activity.AddMessageAttributes("TestMessage", "msg-123", 1024));
        Assert.Null(exception);
    }

    [Fact]
    public void AddCompressionAttributes_WithNullActivity_ShouldNotThrow()
    {
        // Arrange
        Activity? activity = null;

        // Act & Assert
        var exception = Record.Exception(() => 
            activity.AddCompressionAttributes("gzip", 1000, 300));
        Assert.Null(exception);
    }

    [Fact]
    public void RecordError_WithNullActivity_ShouldNotThrow()
    {
        // Arrange
        Activity? activity = null;
        var exception = new InvalidOperationException("Test");

        // Act & Assert
        var recordException = Record.Exception(() => 
            activity.RecordError(exception));
        Assert.Null(recordException);
    }
}
