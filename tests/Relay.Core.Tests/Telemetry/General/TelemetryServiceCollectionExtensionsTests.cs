using Microsoft.Extensions.DependencyInjection;
using Moq;
using Relay.Core.Telemetry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

public class TelemetryServiceCollectionExtensionsTests
{
    [Fact]
    public void AddRelayTelemetry_Should_Register_Default_Services()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRelayTelemetry();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // Check that default telemetry provider is registered
        var telemetryProvider = serviceProvider.GetService<ITelemetryProvider>();
        Assert.NotNull(telemetryProvider);
        Assert.IsType<DefaultTelemetryProvider>(telemetryProvider);

        // Check that default metrics provider is registered
        var metricsProvider = serviceProvider.GetService<IMetricsProvider>();
        Assert.NotNull(metricsProvider);
        Assert.IsType<DefaultMetricsProvider>(metricsProvider);
    }

    [Fact]
    public void AddRelayTelemetry_Should_Return_Same_ServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddRelayTelemetry();

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddRelayTelemetry_With_Configure_Action_Should_Register_Services()
    {
        // Arrange
        var services = new ServiceCollection();
        bool configureCalled = false;

        // Act
        services.AddRelayTelemetry(provider => configureCalled = true);

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // Check that services are registered
        var telemetryProvider = serviceProvider.GetService<ITelemetryProvider>();
        Assert.NotNull(telemetryProvider);
        Assert.IsType<DefaultTelemetryProvider>(telemetryProvider);

        var metricsProvider = serviceProvider.GetService<IMetricsProvider>();
        Assert.NotNull(metricsProvider);
        Assert.IsType<DefaultMetricsProvider>(metricsProvider);

        // Note: The configure action is called during service resolution
        // In this case, it should have been called when getting ITelemetryProvider
        Assert.True(configureCalled);
    }

    [Fact]
    public void AddRelayTelemetry_Generic_Should_Register_Custom_Telemetry_Provider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRelayTelemetry<CustomTelemetryProvider>();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // Check that custom telemetry provider is registered
        var telemetryProvider = serviceProvider.GetService<ITelemetryProvider>();
        Assert.NotNull(telemetryProvider);
        Assert.IsType<CustomTelemetryProvider>(telemetryProvider);
    }

    [Fact]
    public void AddRelayTelemetry_Generic_Should_Return_Same_ServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddRelayTelemetry<CustomTelemetryProvider>();

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddRelayTelemetry_With_Factory_Should_Register_Custom_Telemetry_Provider()
    {
        // Arrange
        var services = new ServiceCollection();
        var customProvider = new CustomTelemetryProvider();

        // Act
        services.AddRelayTelemetry(_ => customProvider);

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // Check that custom telemetry provider is registered
        var telemetryProvider = serviceProvider.GetService<ITelemetryProvider>();
        Assert.NotNull(telemetryProvider);
        Assert.Same(customProvider, telemetryProvider);

        // Check that default metrics provider is also registered
        var metricsProvider = serviceProvider.GetService<IMetricsProvider>();
        Assert.NotNull(metricsProvider);
        Assert.IsType<DefaultMetricsProvider>(metricsProvider);
    }

    [Fact]
    public void AddRelayTelemetry_With_Factory_Should_Return_Same_ServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var customProvider = new CustomTelemetryProvider();

        // Act
        var result = services.AddRelayTelemetry(_ => customProvider);

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddRelayMetrics_Generic_Should_Register_Custom_Metrics_Provider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRelayMetrics<CustomMetricsProvider>();

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // Check that custom metrics provider is registered
        var metricsProvider = serviceProvider.GetService<IMetricsProvider>();
        Assert.NotNull(metricsProvider);
        Assert.IsType<CustomMetricsProvider>(metricsProvider);
    }

    [Fact]
    public void AddRelayMetrics_Generic_Should_Return_Same_ServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddRelayMetrics<CustomMetricsProvider>();

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddRelayMetrics_With_Factory_Should_Register_Custom_Metrics_Provider()
    {
        // Arrange
        var services = new ServiceCollection();
        var customProvider = new CustomMetricsProvider();

        // Act
        services.AddRelayMetrics(_ => customProvider);

        // Assert
        var serviceProvider = services.BuildServiceProvider();

        // Check that custom metrics provider is registered
        var metricsProvider = serviceProvider.GetService<IMetricsProvider>();
        Assert.NotNull(metricsProvider);
        Assert.Same(customProvider, metricsProvider);
    }

    [Fact]
    public void AddRelayMetrics_With_Factory_Should_Return_Same_ServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var customProvider = new CustomMetricsProvider();

        // Act
        var result = services.AddRelayMetrics(_ => customProvider);

        // Assert
        Assert.Same(services, result);
    }



    // Test helper classes
    private class CustomTelemetryProvider : ITelemetryProvider
    {
        public IMetricsProvider MetricsProvider => new CustomMetricsProvider();
        public void RecordRequest(string requestType, TimeSpan duration, bool success) { }
        public void RecordStream(string streamType, long itemsProcessed, TimeSpan duration) { }
        public void RecordNotification(string notificationType, int subscriberCount, TimeSpan duration) { }
        public void RecordError(string operationType, Exception exception) { }
        public void RecordHandlerExecution(Type requestType, Type? responseType, string? handlerName, TimeSpan duration, bool success, Exception? exception) { }
        public void RecordNotificationPublish(Type notificationType, int subscriberCount, TimeSpan duration, bool success, Exception? exception) { }
        public void RecordStreamingOperation(Type requestType, Type responseType, string? handlerName, TimeSpan duration, long itemsProcessed, bool success, Exception? exception) { }
        public Activity? StartActivity(string operationName, Type requestType, string? handlerName) => null;
        public void SetCorrelationId(string correlationId) { }
        public string? GetCorrelationId() => null;
    }

    private class CustomMetricsProvider : IMetricsProvider
    {
        public void IncrementCounter(string name, double value = 1, params (string Key, string Value)[] tags) { }
        public void RecordHistogram(string name, double value, params (string Key, string Value)[] tags) { }
        public void RecordGauge(string name, double value, params (string Key, string Value)[] tags) { }
        public void RecordHandlerExecution(HandlerExecutionMetrics metrics) { }
        public void RecordNotificationPublish(NotificationPublishMetrics metrics) { }
        public void RecordStreamingOperation(StreamingOperationMetrics metrics) { }
        public HandlerExecutionStats GetHandlerExecutionStats(Type requestType, string? handlerName) => new HandlerExecutionStats();
        public NotificationPublishStats GetNotificationPublishStats(Type notificationType) => new NotificationPublishStats();
        public StreamingOperationStats GetStreamingOperationStats(Type requestType, string? handlerName) => new StreamingOperationStats();
        public IEnumerable<PerformanceAnomaly> DetectAnomalies(TimeSpan timeWindow) => Array.Empty<PerformanceAnomaly>();
        public TimingBreakdown GetTimingBreakdown(string operationName) => new TimingBreakdown();
        public void RecordTimingBreakdown(TimingBreakdown breakdown) { }
    }
}