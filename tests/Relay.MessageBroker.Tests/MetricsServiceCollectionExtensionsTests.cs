using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker;
using Relay.MessageBroker.Metrics;
using Xunit;

namespace Relay.MessageBroker.Tests.Metrics;

public class MetricsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMessageBrokerMetrics_WithDefaultOptions_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMessageBrokerMetrics();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var metrics = serviceProvider.GetService<MessageBrokerMetrics>();
        
        Assert.NotNull(metrics);
    }

    [Fact]
    public void AddMessageBrokerMetrics_WithDisabledMetrics_ShouldNotRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMessageBrokerMetrics(options =>
        {
            options.Enabled = false;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var metrics = serviceProvider.GetService<MessageBrokerMetrics>();
        
        Assert.Null(metrics);
    }

    [Fact]
    public void AddMessageBrokerMetrics_WithCustomOptions_ShouldRegisterServicesWithCustomSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        var customMeterName = "CustomMeter";
        var customVersion = "2.0.0";

        // Act
        services.AddMessageBrokerMetrics(options =>
        {
            options.MeterName = customMeterName;
            options.MeterVersion = customVersion;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var metrics = serviceProvider.GetService<MessageBrokerMetrics>();
        
        Assert.NotNull(metrics);
    }

    [Fact]
    public void AddMessageBrokerMetrics_WithConnectionPoolMetricsEnabled_ShouldRegisterConnectionPoolMetrics()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMessageBrokerMetrics(options =>
        {
            options.EnableConnectionPoolMetrics = true;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var metrics = serviceProvider.GetService<MessageBrokerMetrics>();
        var connectionPoolMetrics = serviceProvider.GetService<ConnectionPoolMetricsCollector>();
        
        Assert.NotNull(metrics);
        Assert.NotNull(connectionPoolMetrics);
    }

    [Fact]
    public void AddMessageBrokerMetrics_WithConnectionPoolMetricsDisabled_ShouldNotRegisterConnectionPoolMetrics()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMessageBrokerMetrics(options =>
        {
            options.EnableConnectionPoolMetrics = false;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var metrics = serviceProvider.GetService<MessageBrokerMetrics>();
        var connectionPoolMetrics = serviceProvider.GetService<ConnectionPoolMetricsCollector>();
        
        Assert.NotNull(metrics);
        Assert.Null(connectionPoolMetrics);
    }

    [Fact]
    public void AddMessageBrokerMetrics_MultipleTimes_ShouldNotDuplicateRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMessageBrokerMetrics();
        services.AddMessageBrokerMetrics(); // Second call

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var metrics = serviceProvider.GetServices<MessageBrokerMetrics>();
        
        // Should only have one instance
        Assert.Single(metrics);
    }

    [Fact]
    public void AddMessageBrokerMetrics_WithOptionsCallback_ShouldApplyOptionsCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var testBrokerType = MessageBrokerType.Kafka;

        // Act
        services.AddMessageBrokerMetrics(options =>
        {
            options.BrokerType = testBrokerType.ToString();
            options.DefaultTenantId = "test-tenant";
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var optionsAccessor = serviceProvider.GetService<IOptions<MetricsOptions>>();
        Assert.NotNull(optionsAccessor);
        
        var options = optionsAccessor.Value;
        Assert.NotNull(options);
        Assert.True(options.BrokerType == testBrokerType.ToString());
        Assert.True(options.DefaultTenantId == "test-tenant");
    }
}