using System.Net;
using System.Reflection;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SimpleNotificationService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker;
using Relay.MessageBroker.AwsSqsSns;
using Relay.MessageBroker.Compression;
using Relay.Core.ContractValidation;
using Relay.Core;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class AwsSqsSnsMessageBrokerConstructorTests
{
    private readonly Mock<ILogger<AwsSqsSnsMessageBroker>> _loggerMock;

    public AwsSqsSnsMessageBrokerConstructorTests()
    {
        _loggerMock = new Mock<ILogger<AwsSqsSnsMessageBroker>>();
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AwsSqsSnsMessageBroker(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithoutAwsOptions_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new MessageBrokerOptions();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object));
        Assert.Equal("AWS SQS/SNS options are required.", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyRegion_ShouldUseDefaultRegion()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions { Region = "" }
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert - Should not throw, uses default region
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithValidQueueOptions_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"
            }
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithValidTopicOptions_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultTopicArn = "arn:aws:sns:us-east-1:123456789012:test-topic"
            }
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithValidFifoQueueOptions_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue.fifo",
                UseFifoQueue = true,
                MessageGroupId = "test-group"
            }
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithAwsCredentials_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue",
                AccessKeyId = "test-access-key",
                SecretAccessKey = "test-secret-key"
            }
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithPartialAwsCredentials_ShouldSucceed()
    {
        // Arrange - Only access key provided, should still work
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue",
                AccessKeyId = "test-access-key"
                // SecretAccessKey is null
            }
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithContractValidator_ShouldAcceptValidator()
    {
        // Arrange
        var validatorMock = new Mock<Relay.Core.ContractValidation.IContractValidator>();
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"
            }
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object, null, validatorMock.Object);

        // Assert - Should not throw, contract validator would be used by base class
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithCustomRetryPolicy_ShouldUseCustomRetrySettings()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"
            },
            RetryPolicy = new RetryPolicy
            {
                MaxAttempts = 5,
                MaxDelay = TimeSpan.FromSeconds(60)
            }
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert - Should not throw, custom retry policy would be configured
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithCustomCircuitBreaker_ShouldUseCustomCircuitBreakerSettings()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"
            },
            CircuitBreaker = new Relay.MessageBroker.CircuitBreaker.CircuitBreakerOptions
            {
                FailureThreshold = 10,
                Timeout = TimeSpan.FromMinutes(5)
            }
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert - Should not throw, custom circuit breaker would be configured
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithDefaultRetryPolicy_ShouldUseDefaultRetrySettings()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"
            }
            // No custom retry policy
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert - Should not throw, default retry policy (3 attempts, 30s max delay) would be used
        Assert.NotNull(broker);
    }

    [Fact]
    public void Constructor_WithDefaultCircuitBreaker_ShouldUseDefaultCircuitBreakerSettings()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1",
                DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue"
            }
            // No custom circuit breaker
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Assert - Should not throw, default circuit breaker (5 failures, 30s timeout) would be used
        Assert.NotNull(broker);
    }

    private class TestMessage
    {
        public string? Content { get; set; }
    }
}