using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class MessageBrokerGeneralValidationTests
{
    [Fact]
    public void MessageBrokerOptions_WithInvalidBrokerType_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = (MessageBrokerType)999; // Invalid enum value
            }));

        Assert.Contains("Unsupported broker type", exception.Message);
    }

    [Fact]
    public void MessageBrokerOptions_WithInvalidExchangeName_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.DefaultExchange = ""; // Empty exchange name
            }));

        Assert.Contains("DefaultExchange", exception.Message);
    }

    [Fact]
    public void MessageBrokerOptions_WithInvalidRoutingKeyPattern_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.DefaultRoutingKeyPattern = ""; // Empty routing key pattern
            }));

        Assert.Contains("DefaultRoutingKeyPattern", exception.Message);
    }

    [Fact]
    public void MessageBrokerOptions_WithInvalidSerializerType_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.SerializerType = (MessageSerializerType)999; // Invalid enum value
            }));

        Assert.Contains("Unsupported serializer type", exception.Message);
    }

    [Fact]
    public void RetryPolicy_WithInvalidMaxAttempts_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = -1 // Negative value
                };
            }));

        Assert.Contains("MaxAttempts", exception.Message);
    }

    [Fact]
    public void RetryPolicy_WithInvalidInitialDelay_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.RetryPolicy = new RetryPolicy
                {
                    InitialDelay = TimeSpan.FromSeconds(-1) // Negative delay
                };
            }));

        Assert.Contains("InitialDelay", exception.Message);
    }

    [Fact]
    public void RetryPolicy_WithInvalidMaxDelay_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.RetryPolicy = new RetryPolicy
                {
                    MaxDelay = TimeSpan.FromSeconds(-1) // Negative delay
                };
            }));

        Assert.Contains("MaxDelay", exception.Message);
    }

    [Fact]
    public void RetryPolicy_WithInvalidBackoffMultiplier_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.RetryPolicy = new RetryPolicy
                {
                    BackoffMultiplier = 0 // Invalid multiplier
                };
            }));

        Assert.Contains("BackoffMultiplier", exception.Message);
    }

    [Fact]
    public void CircuitBreakerOptions_WithInvalidFailureThreshold_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    Enabled = true,
                    FailureThreshold = 0 // Invalid threshold
                };
            }));

        Assert.Contains("FailureThreshold", exception.Message);
    }

    [Fact]
    public void CircuitBreakerOptions_WithInvalidSuccessThreshold_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    Enabled = true,
                    SuccessThreshold = 0 // Invalid threshold
                };
            }));

        Assert.Contains("SuccessThreshold", exception.Message);
    }

    [Fact]
    public void CircuitBreakerOptions_WithInvalidTimeout_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    Enabled = true,
                    Timeout = TimeSpan.Zero // Invalid timeout
                };
            }));

        Assert.Contains("Timeout", exception.Message);
    }

    [Fact]
    public void CircuitBreakerOptions_WithInvalidSamplingDuration_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    Enabled = true,
                    SamplingDuration = TimeSpan.Zero // Invalid duration
                };
            }));

        Assert.Contains("SamplingDuration", exception.Message);
    }

    [Fact]
    public void CircuitBreakerOptions_WithInvalidMinimumThroughput_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    Enabled = true,
                    MinimumThroughput = 0 // Invalid throughput
                };
            }));

        Assert.Contains("MinimumThroughput", exception.Message);
    }

    [Fact]
    public void CircuitBreakerOptions_WithInvalidFailureRateThreshold_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    Enabled = true,
                    FailureRateThreshold = 1.5 // Invalid threshold (>1)
                };
            }));

        Assert.Contains("FailureRateThreshold", exception.Message);
    }

    [Fact]
    public void CircuitBreakerOptions_WithInvalidHalfOpenDuration_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    Enabled = true,
                    HalfOpenDuration = TimeSpan.Zero // Invalid duration
                };
            }));

        Assert.Contains("HalfOpenDuration", exception.Message);
    }

    [Fact]
    public void CircuitBreakerOptions_WithInvalidSlowCallDurationThreshold_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    Enabled = true,
                    SlowCallDurationThreshold = TimeSpan.Zero // Invalid duration
                };
            }));

        Assert.Contains("SlowCallDurationThreshold", exception.Message);
    }

    [Fact]
    public void CircuitBreakerOptions_WithInvalidSlowCallRateThreshold_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    Enabled = true,
                    SlowCallRateThreshold = 1.5 // Invalid threshold (>1)
                };
            }));

        Assert.Contains("SlowCallRateThreshold", exception.Message);
    }

    [Fact]
    public void CompressionOptions_WithInvalidAlgorithm_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.Compression = new Compression.CompressionOptions
                {
                    Enabled = true,
                    Algorithm = (Relay.Core.Caching.Compression.CompressionAlgorithm)999 // Invalid algorithm
                };
            }));

        Assert.Contains("Unsupported compression algorithm", exception.Message);
    }

    [Fact]
    public void CompressionOptions_WithInvalidLevel_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.Compression = new Compression.CompressionOptions
                {
                    Enabled = true,
                    Level = 10 // Invalid level (>9)
                };
            }));

        Assert.Contains("Level", exception.Message);
    }

    [Fact]
    public void CompressionOptions_WithInvalidMinimumSizeBytes_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.Compression = new Compression.CompressionOptions
                {
                    Enabled = true,
                    MinimumSizeBytes = -1 // Invalid size
                };
            }));

        Assert.Contains("MinimumSizeBytes", exception.Message);
    }

    [Fact]
    public void CompressionOptions_WithInvalidExpectedCompressionRatio_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.Compression = new Compression.CompressionOptions
                {
                    Enabled = true,
                    ExpectedCompressionRatio = 1.5 // Invalid ratio (>1)
                };
            }));

        Assert.Contains("ExpectedCompressionRatio", exception.Message);
    }

    [Fact]
    public void MessageBrokerOptions_WithConflictingSettings_ShouldThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.EnableSerialization = false;
                options.SerializerType = MessageSerializerType.MessagePack; // Should not be set when disabled
            }));

        Assert.Contains("Serialization is disabled", exception.Message);
    }

    [Fact]
    public void MessageBrokerOptions_WithValidConfiguration_ShouldNotThrowException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert - Should not throw
        services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.RabbitMQ;
            options.DefaultExchange = "test-exchange";
            options.DefaultRoutingKeyPattern = "{MessageType}";
            options.EnableSerialization = true;
            options.SerializerType = MessageSerializerType.Json;
            options.RetryPolicy = new RetryPolicy
            {
                MaxAttempts = 3,
                InitialDelay = TimeSpan.FromSeconds(1)
            };
        });

        var serviceProvider = services.BuildServiceProvider();
        var messageBroker = serviceProvider.GetService<IMessageBroker>();
        Assert.NotNull(messageBroker);
    }
}