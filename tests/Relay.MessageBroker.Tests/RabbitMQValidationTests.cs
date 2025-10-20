using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class RabbitMQValidationTests
{
    [Fact]
    public void RabbitMQOptions_WithInvalidHostName_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddRabbitMQ(options =>
            {
                options.HostName = ""; // Empty hostname
            }));

        Assert.Contains("HostName", exception.Message);
    }

    [Fact]
    public void RabbitMQOptions_WithInvalidPort_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddRabbitMQ(options =>
            {
                options.Port = 0; // Invalid port
            }));

        Assert.Contains("Port", exception.Message);
    }

    [Fact]
    public void RabbitMQOptions_WithInvalidUserName_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddRabbitMQ(options =>
            {
                options.UserName = ""; // Empty username
            }));

        Assert.Contains("UserName", exception.Message);
    }

    [Fact]
    public void RabbitMQOptions_WithInvalidPassword_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddRabbitMQ(options =>
            {
                options.Password = ""; // Empty password
            }));

        Assert.Contains("Password", exception.Message);
    }

    [Fact]
    public void RabbitMQOptions_WithInvalidVirtualHost_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddRabbitMQ(options =>
            {
                options.VirtualHost = ""; // Empty virtual host
            }));

        Assert.Contains("VirtualHost", exception.Message);
    }

    [Fact]
    public void RabbitMQOptions_WithInvalidPrefetchCount_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddRabbitMQ(options =>
            {
                options.PrefetchCount = 0; // Invalid prefetch count
            }));

        Assert.Contains("PrefetchCount", exception.Message);
    }

    [Fact]
    public void RabbitMQOptions_WithInvalidExchangeType_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddRabbitMQ(options =>
            {
                options.ExchangeType = ""; // Empty exchange type
            }));

        Assert.Contains("ExchangeType", exception.Message);
    }
}