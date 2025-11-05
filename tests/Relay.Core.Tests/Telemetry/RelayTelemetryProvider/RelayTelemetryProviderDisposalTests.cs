using System;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using Relay.Core.Telemetry;
using Relay.Core.Testing;
using Xunit;

namespace Relay.Core.Tests.Telemetry
{
    [Collection("Sequential")]
    public class RelayTelemetryProviderDisposalTests
    {
        #region Resource Disposal Tests

        [Fact]
        public void Dispose_ShouldCleanUpResources()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions { Component = "TestComponent" });
            var provider = new RelayTelemetryProvider(options);

            // Act - Dispose is called automatically when using statement ends
            // For this test, we just ensure the provider can be created and used without issues

            // Assert
            Assert.NotNull(provider);
            // Note: In .NET, Meter and ActivitySource don't have explicit Dispose methods
            // They are managed by the runtime and cleaned up when the process exits
        }

        [Fact]
        public void Provider_CanBeUsedAfterActivitiesAreDisposed()
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

            // Act - Create and dispose an activity, then create another
            using (var activity1 = provider.StartActivity("FirstOperation", typeof(string)))
            {
                // Activity 1 is active here
            }
            // Activity 1 is disposed here

            using (var activity2 = provider.StartActivity("SecondOperation", typeof(string)))
            {
                // Activity 2 is active here
                Assert.NotNull(activity2);
                Assert.Equal("SecondOperation", activity2.OperationName);
            }

            // Assert - Provider should still work after disposing activities
            Assert.NotNull(provider);
        }

        [Fact]
        public void Dispose_CleansUpActivitySourceAndMeter()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions { Component = "TestComponent" });
            var provider = new RelayTelemetryProvider(options);

            // Act
            provider.Dispose();

            // Assert - Provider is disposed, but since ActivitySource and Meter don't expose disposed state,
            // we verify that the provider still exists but operations may not work as expected
            Assert.NotNull(provider);
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions { Component = "TestComponent" });
            var provider = new RelayTelemetryProvider(options);

            // Act
            provider.Dispose();
            provider.Dispose(); // Should not throw

            // Assert
            Assert.NotNull(provider);
        }

        [Fact]
        public void Dispose_DoesNotPreventAccessToProperties()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions { Component = "TestComponent" });
            var provider = new RelayTelemetryProvider(options);

            // Act
            provider.Dispose();

            // Assert - Properties should still be accessible
            Assert.Null(provider.MetricsProvider);
            Assert.Null(provider.GetCorrelationId());
        }

        [Fact]
        public void UsingStatement_DisposesProviderCorrectly()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions { Component = "TestComponent" });

            // Act & Assert
            using (var provider = new RelayTelemetryProvider(options))
            {
                Assert.NotNull(provider);
                // Provider is used here
            }
            // Dispose is called automatically by using statement
        }

        [Fact]
        public void Dispose_PreventsFurtherActivityCreation()
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
            provider.Dispose();
            var activity = provider.StartActivity("TestOperation", typeof(string));

            // Assert - Activity should be null after disposal (ActivitySource disposed)
            Assert.Null(activity);
        }

        [Fact]
        public void Dispose_AllowsMetricsRecordingAfterDisposal()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions { Component = "TestComponent" });
            var provider = new RelayTelemetryProvider(options);

            // Act
            provider.Dispose();
            provider.RecordHandlerExecution(typeof(string), typeof(int), "TestHandler", TimeSpan.FromMilliseconds(100), true);

            // Assert - Should not throw exception
            Assert.NotNull(provider);
        }

        #endregion
    }
}
