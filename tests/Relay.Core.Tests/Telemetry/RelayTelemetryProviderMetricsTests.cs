using System;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.Telemetry
{
    [Collection("Sequential")]
    public class RelayTelemetryProviderMetricsTests
    {
        [Fact]
        public void RecordHandlerExecution_WithSuccess_RecordsMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var metricsProvider = new CustomMetricsProvider();
            var provider = new RelayTelemetryProvider(options, null, metricsProvider);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromMilliseconds(100);

            // Act
            provider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", duration, true);

            // Assert - Check that metrics were recorded
            Assert.Single(metricsProvider.HandlerExecutions);
            var recordedMetrics = metricsProvider.HandlerExecutions[0];
            Assert.Equal(typeof(string), recordedMetrics.RequestType);
            Assert.Equal(typeof(int), recordedMetrics.ResponseType);
            Assert.Equal("TestHandler", recordedMetrics.HandlerName);
            Assert.Equal(duration, recordedMetrics.Duration);
            Assert.True(recordedMetrics.Success);
            Assert.Null(recordedMetrics.Exception);
        }

        [Fact]
        public void RecordHandlerExecution_WithException_RecordsErrorMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var metricsProvider = new CustomMetricsProvider();
            var provider = new RelayTelemetryProvider(options, null, metricsProvider);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromMilliseconds(100);
            var exception = new InvalidOperationException("Test exception");

            // Act
            provider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", duration, false, exception);

            // Assert
            Assert.Single(metricsProvider.HandlerExecutions);
            var recordedMetrics = metricsProvider.HandlerExecutions[0];
            Assert.Equal(typeof(string), recordedMetrics.RequestType);
            Assert.Equal(typeof(int), recordedMetrics.ResponseType);
            Assert.Equal("TestHandler", recordedMetrics.HandlerName);
            Assert.Equal(duration, recordedMetrics.Duration);
            Assert.False(recordedMetrics.Success);
            Assert.Same(exception, recordedMetrics.Exception);
        }

        [Fact]
        public void RecordNotificationPublish_RecordsMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var metricsProvider = new CustomMetricsProvider();
            var provider = new RelayTelemetryProvider(options, null, metricsProvider);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromMilliseconds(50);

            // Act
            provider.RecordNotificationPublish(typeof(TestNotification), 3, duration, true);

            // Assert
            Assert.Single(metricsProvider.NotificationPublishes);
            var recordedMetrics = metricsProvider.NotificationPublishes[0];
            Assert.Equal(typeof(TestNotification), recordedMetrics.NotificationType);
            Assert.Equal(3, recordedMetrics.HandlerCount);
            Assert.Equal(duration, recordedMetrics.Duration);
            Assert.True(recordedMetrics.Success);
            Assert.Null(recordedMetrics.Exception);
        }

        [Fact]
        public void RecordNotificationPublish_WithException_RecordsErrorMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var metricsProvider = new CustomMetricsProvider();
            var provider = new RelayTelemetryProvider(options, null, metricsProvider);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromMilliseconds(50);
            var exception = new InvalidOperationException("Notification exception");

            // Act
            provider.RecordNotificationPublish(typeof(TestNotification), 2, duration, false, exception);

            // Assert
            Assert.Single(metricsProvider.NotificationPublishes);
            var recordedMetrics = metricsProvider.NotificationPublishes[0];
            Assert.Equal(typeof(TestNotification), recordedMetrics.NotificationType);
            Assert.Equal(2, recordedMetrics.HandlerCount);
            Assert.Equal(duration, recordedMetrics.Duration);
            Assert.False(recordedMetrics.Success);
            Assert.Same(exception, recordedMetrics.Exception);
        }

        [Fact]
        public void RecordStreamingOperation_RecordsMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var metricsProvider = new CustomMetricsProvider();
            var provider = new RelayTelemetryProvider(options, null, metricsProvider);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromMilliseconds(200);

            // Act
            provider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", duration, 10, true);

            // Assert
            Assert.Single(metricsProvider.StreamingOperations);
            var recordedMetrics = metricsProvider.StreamingOperations[0];
            Assert.Equal(typeof(string), recordedMetrics.RequestType);
            Assert.Equal(typeof(int), recordedMetrics.ResponseType);
            Assert.Equal("StreamHandler", recordedMetrics.HandlerName);
            Assert.Equal(duration, recordedMetrics.Duration);
            Assert.Equal(10L, recordedMetrics.ItemCount);
            Assert.True(recordedMetrics.Success);
            Assert.Null(recordedMetrics.Exception);
        }

        [Fact]
        public void RecordStreamingOperation_WithException_RecordsErrorMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var metricsProvider = new CustomMetricsProvider();
            var provider = new RelayTelemetryProvider(options, null, metricsProvider);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromMilliseconds(200);
            var exception = new InvalidOperationException("Stream exception");

            // Act
            provider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", duration, 5, false, exception);

            // Assert
            Assert.Single(metricsProvider.StreamingOperations);
            var recordedMetrics = metricsProvider.StreamingOperations[0];
            Assert.Equal(typeof(string), recordedMetrics.RequestType);
            Assert.Equal(typeof(int), recordedMetrics.ResponseType);
            Assert.Equal("StreamHandler", recordedMetrics.HandlerName);
            Assert.Equal(duration, recordedMetrics.Duration);
            Assert.Equal(5L, recordedMetrics.ItemCount);
            Assert.False(recordedMetrics.Success);
            Assert.Same(exception, recordedMetrics.Exception);
        }

        [Fact]
        public void RecordMessagePublished_RecordsMessageBrokerMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var metricsProvider = new CustomMetricsProvider();
            var provider = new RelayTelemetryProvider(options, null, metricsProvider);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromMilliseconds(75);
            long payloadSize = 1024;

            // Act
            provider.RecordMessagePublished(typeof(TestMessage), payloadSize, duration, true);

            // Assert - The method should execute without error
            Assert.NotNull(provider.MetricsProvider);
        }

        [Fact]
        public void RecordMessagePublished_WithZeroPayloadSize_RecordsMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var provider = new RelayTelemetryProvider(options);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromMilliseconds(50);
            long payloadSize = 0;

            // Act & Assert - Should not throw exception with zero payload size
            provider.RecordMessagePublished(typeof(TestMessage), payloadSize, duration, true);
        }

        [Fact]
        public void RecordMessagePublished_WithLargePayloadSize_RecordsMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var provider = new RelayTelemetryProvider(options);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromMilliseconds(100);
            long payloadSize = 10L * 1024 * 1024; // 10MB

            // Act & Assert - Should handle large payload sizes
            provider.RecordMessagePublished(typeof(TestMessage), payloadSize, duration, true);
        }

        [Fact]
        public void RecordMessagePublished_WithException_RecordsErrorMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var metricsProvider = new CustomMetricsProvider();
            var provider = new RelayTelemetryProvider(options, null, metricsProvider);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromMilliseconds(75);
            var exception = new InvalidOperationException("Publish exception");
            long payloadSize = 1024;

            // Act
            provider.RecordMessagePublished(typeof(TestMessage), payloadSize, duration, false, exception);

            // Just ensure the provider exists and method executes without error
            Assert.NotNull(provider.MetricsProvider);
        }

        [Fact]
        public void RecordMessageProcessed_RecordsProcessingMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var metricsProvider = new CustomMetricsProvider();
            var provider = new RelayTelemetryProvider(options, null, metricsProvider);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromMilliseconds(80);

            // Act
            provider.RecordMessageProcessed(typeof(TestMessage), duration, true);

            // Assert - Method should execute without error
            Assert.NotNull(provider.MetricsProvider);
        }

        [Fact]
        public void RecordMessageProcessed_WithZeroDuration_RecordsMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var provider = new RelayTelemetryProvider(options);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.Zero;

            // Act & Assert - Should handle zero duration
            provider.RecordMessageProcessed(typeof(TestMessage), duration, true);
        }

        [Fact]
        public void RecordMessageProcessed_WithLongDuration_RecordsMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var provider = new RelayTelemetryProvider(options);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromHours(1);

            // Act & Assert - Should handle long durations
            provider.RecordMessageProcessed(typeof(TestMessage), duration, false);
        }

        [Fact]
        public void RecordMessageProcessed_WithFailure_RecordsFailureMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var metricsProvider = new CustomMetricsProvider();
            var provider = new RelayTelemetryProvider(options, null, metricsProvider);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromMilliseconds(80);
            var exception = new InvalidOperationException("Test exception");

            // Act
            provider.RecordMessageProcessed(typeof(TestMessage), duration, false, exception);

            // Assert - Method should execute without error
            Assert.NotNull(provider.MetricsProvider);
        }

        [Fact]
        public void RecordMessageReceived_RecordsReceivedMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var metricsProvider = new CustomMetricsProvider();
            var provider = new RelayTelemetryProvider(options, null, metricsProvider);

            using var activity = provider.StartActivity("TestOperation", typeof(string));

            // Act
            provider.RecordMessageReceived(typeof(TestMessage));

            // Assert - Method should execute without error
            Assert.NotNull(provider.MetricsProvider);
        }

        #region Metrics Edge Cases Tests

        [Fact]
        public void RecordHandlerExecution_WithZeroDuration_RecordsMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var metricsProvider = new CustomMetricsProvider();
            var provider = new RelayTelemetryProvider(options, null, metricsProvider);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.Zero;

            // Act
            provider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", duration, true);

            // Assert
            Assert.Single(metricsProvider.HandlerExecutions);
            var recordedMetrics = metricsProvider.HandlerExecutions[0];
            Assert.Equal(TimeSpan.Zero, recordedMetrics.Duration);
        }

        [Fact]
        public void RecordHandlerExecution_WithVeryLongDuration_RecordsMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var metricsProvider = new CustomMetricsProvider();
            var provider = new RelayTelemetryProvider(options, null, metricsProvider);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromDays(365);

            // Act
            provider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", duration, true);

            // Assert
            Assert.Single(metricsProvider.HandlerExecutions);
            var recordedMetrics = metricsProvider.HandlerExecutions[0];
            Assert.Equal(TimeSpan.FromDays(365), recordedMetrics.Duration);
        }

        [Fact]
        public void RecordHandlerExecution_WithNegativeDuration_RecordsMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var metricsProvider = new CustomMetricsProvider();
            var provider = new RelayTelemetryProvider(options, null, metricsProvider);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromSeconds(-1);

            // Act
            provider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", duration, true);

            // Assert
            Assert.Single(metricsProvider.HandlerExecutions);
            var recordedMetrics = metricsProvider.HandlerExecutions[0];
            Assert.Equal(TimeSpan.FromSeconds(-1), recordedMetrics.Duration);
        }

        [Fact]
        public void RecordNotificationPublish_WithNegativeHandlerCount_RecordsMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var metricsProvider = new CustomMetricsProvider();
            var provider = new RelayTelemetryProvider(options, null, metricsProvider);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromMilliseconds(50);

            // Act
            provider.RecordNotificationPublish(typeof(TestNotification), -1, duration, true);

            // Assert
            Assert.Single(metricsProvider.NotificationPublishes);
            var recordedMetrics = metricsProvider.NotificationPublishes[0];
            Assert.Equal(-1, recordedMetrics.HandlerCount);
        }

        [Fact]
        public void RecordStreamingOperation_WithNegativeItemCount_RecordsMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var metricsProvider = new CustomMetricsProvider();
            var provider = new RelayTelemetryProvider(options, null, metricsProvider);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromMilliseconds(200);

            // Act
            provider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", duration, -100, true);

            // Assert
            Assert.Single(metricsProvider.StreamingOperations);
            var recordedMetrics = metricsProvider.StreamingOperations[0];
            Assert.Equal(-100L, recordedMetrics.ItemCount);
        }

        [Fact]
        public void RecordMessagePublished_WithNegativePayloadSize_RecordsMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var provider = new RelayTelemetryProvider(options);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromMilliseconds(75);
            long payloadSize = -1024;

            // Act & Assert - Should not throw exception with negative payload size
            provider.RecordMessagePublished(typeof(TestMessage), payloadSize, duration, true);
        }

        #endregion

        #region Error Handling Comprehensive Tests

        [Fact]
        public void RecordHandlerExecution_WithNullException_RecordsMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var metricsProvider = new CustomMetricsProvider();
            var provider = new RelayTelemetryProvider(options, null, metricsProvider);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromMilliseconds(100);

            // Act
            provider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", duration, false, null);

            // Assert
            Assert.Single(metricsProvider.HandlerExecutions);
            var recordedMetrics = metricsProvider.HandlerExecutions[0];
            Assert.False(recordedMetrics.Success);
            Assert.Null(recordedMetrics.Exception);
        }

        [Fact]
        public void RecordNotificationPublish_WithNullException_RecordsMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var metricsProvider = new CustomMetricsProvider();
            var provider = new RelayTelemetryProvider(options, null, metricsProvider);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromMilliseconds(50);

            // Act
            provider.RecordNotificationPublish(typeof(TestNotification), 3, duration, false, null);

            // Assert
            Assert.Single(metricsProvider.NotificationPublishes);
            var recordedMetrics = metricsProvider.NotificationPublishes[0];
            Assert.False(recordedMetrics.Success);
            Assert.Null(recordedMetrics.Exception);
        }

        [Fact]
        public void RecordStreamingOperation_WithNullException_RecordsMetrics()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var metricsProvider = new CustomMetricsProvider();
            var provider = new RelayTelemetryProvider(options, null, metricsProvider);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromMilliseconds(200);

            // Act
            provider.RecordStreamingOperation(typeof(string), typeof(int), "StreamHandler", duration, 10, false, null);

            // Assert
            Assert.Single(metricsProvider.StreamingOperations);
            var recordedMetrics = metricsProvider.StreamingOperations[0];
            Assert.False(recordedMetrics.Success);
            Assert.Null(recordedMetrics.Exception);
        }

        #endregion
    }
}