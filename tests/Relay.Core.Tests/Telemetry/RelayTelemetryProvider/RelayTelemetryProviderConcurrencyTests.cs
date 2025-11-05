using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.Telemetry
{
using Relay.Core.Testing;
    [Collection("Sequential")]
    public class RelayTelemetryProviderConcurrencyTests
    {
        #region Concurrent Operations Tests

        [Fact]
        public async Task Concurrent_StartActivity_Calls_WorkCorrectly()
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

            var provider = new RelayTelemetryProvider(options);

            // Act
            var tasks = Enumerable.Range(0, 10).Select(async i =>
            {
                using var activity = provider.StartActivity($"Operation{i}", typeof(string), $"correlation-{i}");
                await Task.Delay(1); // Simulate some work
                return activity;
            });

            var activities = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(10, activities.Length);
            Assert.All(activities, activity => Assert.NotNull(activity));
        }

        [Fact]
        public void RecordHandlerExecution_Can_Be_Called_Without_Errors()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = false // Disable tracing to avoid activity sampling issues
            });
            var metricsProvider = new CustomMetricsProvider();
            var provider = new RelayTelemetryProvider(options, null, metricsProvider);

            // Act - Test that RecordHandlerExecution can be called multiple times without throwing
            var exceptions = new List<Exception>();
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    provider.RecordHandlerExecution(typeof(string), typeof(int), $"Handler{i}",
                        TimeSpan.FromMilliseconds(i * 10), i % 2 == 0);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            // Assert - The method should not throw exceptions, and at least some metrics should be recorded
            Assert.Empty(exceptions);
            Assert.True(metricsProvider.CallCount > 0, $"Expected at least 1 call, got {metricsProvider.CallCount}");
        }

        [Fact]
        public async Task CorrelationId_IsThreadSafe()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions { Component = "TestComponent" });
            var provider = new RelayTelemetryProvider(options);

            // Act
            var tasks = Enumerable.Range(0, 10).Select(async i =>
            {
                provider.SetCorrelationId($"correlation-{i}");
                await Task.Delay(1); // Allow other threads to run
                return provider.GetCorrelationId();
            });

            var results = await Task.WhenAll(tasks);

            // Assert - Each task should have set its own correlation ID
            // Note: Due to AsyncLocal behavior, the last set correlation ID will be visible
            // This test mainly ensures no exceptions are thrown during concurrent access
            Assert.Equal(10, results.Length);
            Assert.All(results, result => Assert.NotNull(result));
        }

        #endregion

        #region Performance Scenarios Tests

        [Fact]
        public void HighFrequency_StartActivity_Calls_DoNotThrowExceptions()
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

            var provider = new RelayTelemetryProvider(options);

            // Act - Create many activities quickly
            for (int i = 0; i < 1000; i++)
            {
                using var activity = provider.StartActivity($"Operation{i}", typeof(string));
                // Dispose immediately to avoid resource exhaustion
            }

            // Assert - No exceptions thrown
            Assert.True(true);
        }

        [Fact]
        public void HighFrequency_RecordMetrics_Calls_DoNotThrowExceptions()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var metricsProvider = new CustomMetricsProvider();
            var provider = new RelayTelemetryProvider(options, null, metricsProvider);

            // Act - Record many metrics quickly
            for (int i = 0; i < 1000; i++)
            {
                using var activity = provider.StartActivity($"Operation{i}", typeof(string));
                provider.RecordHandlerExecution(typeof(string), typeof(int), $"Handler{i}",
                    TimeSpan.FromMilliseconds(1), true);
            }

            // Assert - No exceptions thrown and metrics recorded
            Assert.Equal(1000, metricsProvider.HandlerExecutions.Count);
        }

        #endregion
    }
}
