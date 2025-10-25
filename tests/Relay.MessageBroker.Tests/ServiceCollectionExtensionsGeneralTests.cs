using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker;
using Relay.Core.ContractValidation;
using Relay.Core;
using Xunit;
using Relay.Core.Metadata.MessageQueue;

namespace Relay.MessageBroker.Tests;

public class ServiceCollectionExtensionsGeneralTests
{
    [Fact]
    public void AddMessageBroker_ShouldRegisterIMessageBroker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.RabbitMQ;
            options.RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672
            };
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var messageBroker = serviceProvider.GetService<IMessageBroker>();
        Assert.NotNull(messageBroker);
    }

    [Fact]
    public void AddMessageBrokerHostedService_ShouldRegisterHostedService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRabbitMQ();

        // Act
        services.AddMessageBrokerHostedService();

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var hostedServices = serviceProvider.GetServices<IHostedService>();
        Assert.Contains(hostedServices, s => s is MessageBrokerHostedService);
    }

    [Fact]
    public void AddMessageBroker_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection? services = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services!.AddMessageBroker(_ => { }));
    }

    [Fact]
    public void AddMessageBroker_WithNullConfigure_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => services.AddMessageBroker(null!));
    }

    [Fact]
    public void AddMessageBroker_WithUnsupportedBrokerType_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = (MessageBrokerType)999; // Invalid broker type
            }));
    }

    [Fact]
    public void AddMessageBroker_MultipleTimes_ShouldReplaceRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddMessageBroker(options => options.BrokerType = MessageBrokerType.RabbitMQ);
        services.AddMessageBroker(options => options.BrokerType = MessageBrokerType.Kafka);

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var messageBroker = serviceProvider.GetService<IMessageBroker>();
        Assert.NotNull(messageBroker);
        // Last registration should win
        Assert.IsType<Kafka.KafkaMessageBroker>(messageBroker);
    }

    [Fact]
    public void AddMessageBroker_ShouldAllowChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        var chainedServices = services.AddRabbitMQ().AddMessageBrokerHostedService();

        // Assert
        Assert.Same(services, chainedServices);
    }

    [Fact]
    public void AddMessageBroker_WithContractValidator_ShouldPassValidatorToBroker()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IContractValidator, TestContractValidator>();

        // Act
        services.AddMessageBroker(options =>
        {
            options.BrokerType = MessageBrokerType.RabbitMQ;
            options.RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672
            };
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var messageBroker = serviceProvider.GetService<IMessageBroker>();
        Assert.NotNull(messageBroker);
    }

    [Fact]
    public void AddMessageBroker_WithNullDefaultExchange_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.DefaultExchange = null!;
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithEmptyDefaultExchange_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.DefaultExchange = "";
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithNullDefaultRoutingKeyPattern_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.DefaultRoutingKeyPattern = null!;
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithEmptyDefaultRoutingKeyPattern_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.DefaultRoutingKeyPattern = "";
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithUnsupportedSerializerType_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.SerializerType = (MessageSerializerType)999; // Invalid serializer type
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithSerializationDisabledAndNonDefaultSerializer_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.EnableSerialization = false;
                options.SerializerType = MessageSerializerType.MessagePack; // Non-default when disabled
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithRetryPolicyNegativeMaxAttempts_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = -1
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithRetryPolicyZeroInitialDelay_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 3,
                    InitialDelay = TimeSpan.Zero
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithRetryPolicyNegativeInitialDelay_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 3,
                    InitialDelay = TimeSpan.FromSeconds(-1)
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithRetryPolicyZeroMaxDelay_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 3,
                    InitialDelay = TimeSpan.FromSeconds(1),
                    MaxDelay = TimeSpan.Zero
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithRetryPolicyNegativeMaxDelay_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 3,
                    InitialDelay = TimeSpan.FromSeconds(1),
                    MaxDelay = TimeSpan.FromSeconds(-1)
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithRetryPolicyZeroBackoffMultiplier_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 3,
                    InitialDelay = TimeSpan.FromSeconds(1),
                    MaxDelay = TimeSpan.FromSeconds(10),
                    BackoffMultiplier = 0
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithRetryPolicyNegativeBackoffMultiplier_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 3,
                    InitialDelay = TimeSpan.FromSeconds(1),
                    MaxDelay = TimeSpan.FromSeconds(10),
                    BackoffMultiplier = -1
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithCircuitBreakerZeroFailureThreshold_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    FailureThreshold = 0
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithCircuitBreakerNegativeFailureThreshold_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    FailureThreshold = -1
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithCircuitBreakerZeroSuccessThreshold_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    FailureThreshold = 5,
                    SuccessThreshold = 0
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithCircuitBreakerZeroTimeout_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    FailureThreshold = 5,
                    SuccessThreshold = 3,
                    Timeout = TimeSpan.Zero
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithCircuitBreakerNegativeTimeout_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    FailureThreshold = 5,
                    SuccessThreshold = 3,
                    Timeout = TimeSpan.FromSeconds(-1)
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithCircuitBreakerZeroSamplingDuration_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    FailureThreshold = 5,
                    SuccessThreshold = 3,
                    Timeout = TimeSpan.FromSeconds(10),
                    SamplingDuration = TimeSpan.Zero
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithCircuitBreakerZeroMinimumThroughput_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    FailureThreshold = 5,
                    SuccessThreshold = 3,
                    Timeout = TimeSpan.FromSeconds(10),
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 0
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithCircuitBreakerNegativeFailureRateThreshold_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    FailureThreshold = 5,
                    SuccessThreshold = 3,
                    Timeout = TimeSpan.FromSeconds(10),
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 10,
                    FailureRateThreshold = -0.1
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithCircuitBreakerFailureRateThresholdGreaterThanOne_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    FailureThreshold = 5,
                    SuccessThreshold = 3,
                    Timeout = TimeSpan.FromSeconds(10),
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 10,
                    FailureRateThreshold = 1.1
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithCircuitBreakerZeroHalfOpenDuration_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    FailureThreshold = 5,
                    SuccessThreshold = 3,
                    Timeout = TimeSpan.FromSeconds(10),
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 10,
                    FailureRateThreshold = 0.5,
                    HalfOpenDuration = TimeSpan.Zero
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithCircuitBreakerZeroSlowCallDurationThreshold_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    FailureThreshold = 5,
                    SuccessThreshold = 3,
                    Timeout = TimeSpan.FromSeconds(10),
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 10,
                    FailureRateThreshold = 0.5,
                    HalfOpenDuration = TimeSpan.FromSeconds(5),
                    SlowCallDurationThreshold = TimeSpan.Zero
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithCircuitBreakerNegativeSlowCallRateThreshold_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    FailureThreshold = 5,
                    SuccessThreshold = 3,
                    Timeout = TimeSpan.FromSeconds(10),
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 10,
                    FailureRateThreshold = 0.5,
                    HalfOpenDuration = TimeSpan.FromSeconds(5),
                    SlowCallDurationThreshold = TimeSpan.FromSeconds(2),
                    SlowCallRateThreshold = -0.1
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithCircuitBreakerSlowCallRateThresholdGreaterThanOne_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.CircuitBreaker = new CircuitBreaker.CircuitBreakerOptions
                {
                    FailureThreshold = 5,
                    SuccessThreshold = 3,
                    Timeout = TimeSpan.FromSeconds(10),
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    MinimumThroughput = 10,
                    FailureRateThreshold = 0.5,
                    HalfOpenDuration = TimeSpan.FromSeconds(5),
                    SlowCallDurationThreshold = TimeSpan.FromSeconds(2),
                    SlowCallRateThreshold = 1.1
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithCompressionUnsupportedAlgorithm_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.Compression = new Compression.CompressionOptions
                {
                    Algorithm = (Relay.Core.Caching.Compression.CompressionAlgorithm)999 // Invalid algorithm
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithCompressionNegativeLevel_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.Compression = new Compression.CompressionOptions
                {
                    Algorithm = Relay.Core.Caching.Compression.CompressionAlgorithm.GZip,
                    Level = -1
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithCompressionLevelGreaterThanNine_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.Compression = new Compression.CompressionOptions
                {
                    Algorithm = Relay.Core.Caching.Compression.CompressionAlgorithm.GZip,
                    Level = 10
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithCompressionNegativeMinimumSizeBytes_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.Compression = new Compression.CompressionOptions
                {
                    Algorithm = Relay.Core.Caching.Compression.CompressionAlgorithm.GZip,
                    Level = 6,
                    MinimumSizeBytes = -1
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithCompressionNegativeExpectedCompressionRatio_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.Compression = new Compression.CompressionOptions
                {
                    Algorithm = Relay.Core.Caching.Compression.CompressionAlgorithm.GZip,
                    Level = 6,
                    MinimumSizeBytes = 1024,
                    ExpectedCompressionRatio = -0.1
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    [Fact]
    public void AddMessageBroker_WithCompressionExpectedCompressionRatioGreaterThanOne_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            services.AddMessageBroker(options =>
            {
                options.BrokerType = MessageBrokerType.RabbitMQ;
                options.Compression = new Compression.CompressionOptions
                {
                    Algorithm = Relay.Core.Caching.Compression.CompressionAlgorithm.GZip,
                    Level = 6,
                    MinimumSizeBytes = 1024,
                    ExpectedCompressionRatio = 1.1
                };
                options.RabbitMQ = new RabbitMQOptions
                {
                    HostName = "localhost",
                    Port = 5672
                };
            }));
    }

    private class TestContractValidator : IContractValidator
    {
        public ValueTask<IEnumerable<string>> ValidateRequestAsync(object request, JsonSchemaContract schema, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(Enumerable.Empty<string>());
        }

        public ValueTask<IEnumerable<string>> ValidateResponseAsync(object response, JsonSchemaContract schema, CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(Enumerable.Empty<string>());
        }
    }
}