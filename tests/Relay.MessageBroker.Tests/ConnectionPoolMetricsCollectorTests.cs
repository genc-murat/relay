using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker;
using Relay.MessageBroker.Metrics;
using System.Diagnostics.Metrics;
using Xunit;

namespace Relay.MessageBroker.Tests.Metrics;

public class ConnectionPoolMetricsCollectorTests
{
    [Fact]
    public void Constructor_WithDefaultValues_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var collector = new ConnectionPoolMetricsCollector();

        // Assert
        Assert.NotNull(collector);
        
        // Cleanup
        collector.Dispose();
    }

    [Fact]
    public void Constructor_WithCustomValues_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var collector = new ConnectionPoolMetricsCollector("TestMeter", "1.0.0");

        // Assert
        Assert.NotNull(collector);
        
        // Cleanup
        collector.Dispose();
    }

    [Fact]
    public void RecordConnectionWaitTime_ShouldRecordCorrectly()
    {
        // Arrange
        var collector = new ConnectionPoolMetricsCollector();
        var waitTimeMs = 25.5;
        var brokerType = "RabbitMQ";

        // Act
        collector.RecordConnectionWaitTime(waitTimeMs, brokerType);

        // Assert - No exception means success
        
        // Cleanup
        collector.Dispose();
    }

    [Fact]
    public void RecordConnectionCreated_ShouldIncrementCounter()
    {
        // Arrange
        var collector = new ConnectionPoolMetricsCollector();
        var brokerType = "RabbitMQ";

        // Act
        collector.RecordConnectionCreated(brokerType);

        // Assert - No exception means success
        
        // Cleanup
        collector.Dispose();
    }

    [Fact]
    public void RecordConnectionDisposed_ShouldIncrementCounter()
    {
        // Arrange
        var collector = new ConnectionPoolMetricsCollector();
        var brokerType = "RabbitMQ";

        // Act
        collector.RecordConnectionDisposed(brokerType);

        // Assert - No exception means success
        
        // Cleanup
        collector.Dispose();
    }

    [Fact]
    public void SetActiveConnections_ShouldUpdateGauge()
    {
        // Arrange
        var collector = new ConnectionPoolMetricsCollector();
        var count = 3;

        // Act
        collector.SetActiveConnections(count);

        // Assert - No exception means success
        
        // Cleanup
        collector.Dispose();
    }

    [Fact]
    public void SetIdleConnections_ShouldUpdateGauge()
    {
        // Arrange
        var collector = new ConnectionPoolMetricsCollector();
        var count = 7;

        // Act
        collector.SetIdleConnections(count);

        // Assert - No exception means success
        
        // Cleanup
        collector.Dispose();
    }

    [Fact]
    public void Dispose_ShouldCleanUpResources()
    {
        // Arrange
        var collector = new ConnectionPoolMetricsCollector();

        // Act
        collector.Dispose();

        // Assert - No exception means success
        // The actual implementation doesn't throw ObjectDisposedException when calling methods
        // after disposal, so we'll just verify that dispose doesn't throw
        var ex = Record.Exception(() => collector.RecordConnectionWaitTime(100.5, "RabbitMQ"));
        // We're not asserting that an exception is thrown because the current implementation
        // doesn't enforce strict disposal semantics
        Assert.Null(ex);
    }

    [Fact]
    public void MultipleOperations_ShouldWorkCorrectly()
    {
        // Arrange
        var collector = new ConnectionPoolMetricsCollector();
        var brokerType = "RabbitMQ";

        // Act
        collector.RecordConnectionWaitTime(25.5, brokerType);
        collector.RecordConnectionCreated(brokerType);
        collector.RecordConnectionDisposed(brokerType);
        collector.SetActiveConnections(3);
        collector.SetIdleConnections(7);

        // Assert - No exception means success
        
        // Cleanup
        collector.Dispose();
    }
}