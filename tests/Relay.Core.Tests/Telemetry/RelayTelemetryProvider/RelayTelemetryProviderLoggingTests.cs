using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.Core.Telemetry;
using Relay.Core.Testing;
using Xunit;

namespace Relay.Core.Tests.Telemetry
{
    [Collection("Sequential")]
    public class RelayTelemetryProviderLoggingTests
    {
        #region Logging Behavior Tests

        [Fact]
        public void StartActivity_WithLogger_LogsDebugMessage()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });

            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(listener);

            var logger = new TestLogger();
            var provider = new RelayTelemetryProvider(options, logger);

            // Act
            using var activity = provider.StartActivity("TestOperation", typeof(string));

            // Assert
            Assert.NotNull(activity);
            Assert.Contains(logger.LoggedMessages, m =>
                m.Contains("Started activity") &&
                m.Contains("TestComponent") &&
                m.Contains("String"));
        }

        [Fact]
        public void RecordHandlerExecution_WithLogger_LogsDebugMessage()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var logger = new TestLogger();
            var provider = new RelayTelemetryProvider(options, logger);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromMilliseconds(100);

            // Act
            provider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", duration, true);

            // Assert
            Assert.Contains(logger.LoggedMessages, m =>
                m.Contains("Handler execution completed") &&
                m.Contains("String") &&
                m.Contains("Int32") &&
                m.Contains("100") &&
                m.Contains("True"));
        }

        [Fact]
        public void RecordNotificationPublish_WithLogger_LogsDebugMessage()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var logger = new TestLogger();
            var provider = new RelayTelemetryProvider(options, logger);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromMilliseconds(50);

            // Act
            provider.RecordNotificationPublish(typeof(TestNotification), 3, duration, true);

            // Assert
            Assert.Contains(logger.LoggedMessages, m =>
                m.Contains("Notification published") &&
                m.Contains("TestNotification") &&
                m.Contains("3") &&
                m.Contains("50") &&
                m.Contains("True"));
        }

        [Fact]
        public void RecordMessagePublished_WithLogger_LogsDebugMessage()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var logger = new TestLogger();
            var provider = new RelayTelemetryProvider(options, logger);

            using var activity = provider.StartActivity("TestOperation", typeof(string));
            var duration = TimeSpan.FromMilliseconds(75);
            long payloadSize = 1024;

            // Act
            provider.RecordMessagePublished(typeof(TestMessage), payloadSize, duration, true);

            // Assert
            Assert.Contains(logger.LoggedMessages, m =>
                m.Contains("Message published") &&
                m.Contains("TestMessage") &&
                m.Contains("1024") &&
                m.Contains("75") &&
                m.Contains("True"));
        }

        #endregion
    }
}
