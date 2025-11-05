using System;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using Relay.Core.Telemetry;
using Relay.Core.Testing;
using Xunit;

namespace Relay.Core.Tests.Telemetry
{
    [Collection("Sequential")]
    public class RelayTelemetryProviderCorrelationTests
    {
        [Fact]
        public void SetCorrelationId_ShouldUpdateContext()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions
            {
                Component = "TestComponent",
                EnableTracing = true
            });
            var provider = new RelayTelemetryProvider(options);

            // Act
            provider.SetCorrelationId("new-correlation-id");

            // Assert
            Assert.Equal("new-correlation-id", provider.GetCorrelationId());
        }

        [Fact]
        public void GetCorrelationId_WhenNoActivity_ShouldReturnContextValue()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions { Component = "TestComponent" });
            var provider = new RelayTelemetryProvider(options);

            // Act
            provider.SetCorrelationId("context-correlation-id");
            var correlationId = provider.GetCorrelationId();

            // Assert
            Assert.Equal("context-correlation-id", correlationId);
        }

        [Fact]
        public void MetricsProvider_Property_ShouldReturnProvidedMetricsProvider()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions { Component = "TestComponent" });
            var expectedMetricsProvider = new CustomMetricsProvider();

            // Act
            var provider = new RelayTelemetryProvider(options, null, expectedMetricsProvider);

            // Assert
            Assert.Same(expectedMetricsProvider, provider.MetricsProvider);
        }

        [Fact]
        public void MetricsProvider_Property_WithNull_ShouldReturnNull()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions { Component = "TestComponent" });

            // Act
            var provider = new RelayTelemetryProvider(options);

            // Assert
            Assert.Null(provider.MetricsProvider);
        }

        #region Correlation ID Complex Scenarios Tests

        [Fact]
        public void SetCorrelationId_OverwritesPreviousCorrelationId()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions { Component = "TestComponent" });
            var provider = new RelayTelemetryProvider(options);

            // Act
            provider.SetCorrelationId("first-id");
            provider.SetCorrelationId("second-id");

            // Assert
            Assert.Equal("second-id", provider.GetCorrelationId());
        }

        [Fact]
        public void GetCorrelationId_WhenContextIsNull_ReturnsNull()
        {
            // Arrange
            var options = Options.Create(new RelayTelemetryOptions { Component = "TestComponent" });
            var provider = new RelayTelemetryProvider(options);

            // Set and then clear correlation ID
            provider.SetCorrelationId("some-id");
            provider.SetCorrelationId(null);

            // Act
            var correlationId = provider.GetCorrelationId();

            // Assert
            Assert.Null(correlationId);
        }

        [Fact]
        public void CorrelationId_IsIsolatedBetweenProviderInstances()
        {
            // Arrange
            var options1 = Options.Create(new RelayTelemetryOptions { Component = "Component1" });
            var options2 = Options.Create(new RelayTelemetryOptions { Component = "Component2" });

            var provider1 = new RelayTelemetryProvider(options1);
            var provider2 = new RelayTelemetryProvider(options2);

            // Act
            provider1.SetCorrelationId("correlation-1");
            provider2.SetCorrelationId("correlation-2");

            // Assert
            Assert.Equal("correlation-1", provider1.GetCorrelationId());
            Assert.Equal("correlation-2", provider2.GetCorrelationId());
        }

        #endregion
    }
}
