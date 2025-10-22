using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using Relay.MessageBroker.Compression;
using Relay.Core.ContractValidation;
using Relay.MessageBroker.RabbitMQ;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class RabbitMQMessageBrokerTests
{
    private readonly Mock<ILogger<RabbitMQMessageBroker>> _loggerMock;
    private readonly Mock<Relay.MessageBroker.Compression.IMessageCompressor> _compressorMock;
    private readonly Mock<Relay.Core.ContractValidation.IContractValidator> _contractValidatorMock;

    public RabbitMQMessageBrokerTests()
    {
        _loggerMock = new Mock<ILogger<RabbitMQMessageBroker>>();
        _compressorMock = new Mock<Relay.MessageBroker.Compression.IMessageCompressor>();
        _contractValidatorMock = new Mock<Relay.Core.ContractValidation.IContractValidator>();
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldNotThrow()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/",
                ExchangeType = "topic"
            },
            DefaultExchange = "test-exchange",
            DefaultRoutingKeyPattern = "test.{MessageType}"
        });

        // Act & Assert
        var exception = Record.Exception(() => new RabbitMQMessageBroker(
            options,
            _loggerMock.Object,
            _compressorMock.Object,
            _contractValidatorMock.Object));

        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_WithNullHostName_ShouldThrowArgumentException()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = null!,
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/",
                ExchangeType = "topic"
            },
            DefaultExchange = "test-exchange",
            DefaultRoutingKeyPattern = "test.{MessageType}"
        });

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new RabbitMQMessageBroker(
            options,
            _loggerMock.Object,
            _compressorMock.Object,
            _contractValidatorMock.Object));

        Assert.Contains("HostName", exception.Message);
    }

    [Fact]
    public void Constructor_WithEmptyHostName_ShouldThrowArgumentException()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "",
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/",
                ExchangeType = "topic"
            },
            DefaultExchange = "test-exchange",
            DefaultRoutingKeyPattern = "test.{MessageType}"
        });

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new RabbitMQMessageBroker(
            options,
            _loggerMock.Object,
            _compressorMock.Object,
            _contractValidatorMock.Object));

        Assert.Contains("HostName", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidPortZero_ShouldThrowArgumentException()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 0,
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/",
                ExchangeType = "topic"
            },
            DefaultExchange = "test-exchange",
            DefaultRoutingKeyPattern = "test.{MessageType}"
        });

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new RabbitMQMessageBroker(
            options,
            _loggerMock.Object,
            _compressorMock.Object,
            _contractValidatorMock.Object));

        Assert.Contains("Port", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidPortTooHigh_ShouldThrowArgumentException()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 70000,
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/",
                ExchangeType = "topic"
            },
            DefaultExchange = "test-exchange",
            DefaultRoutingKeyPattern = "test.{MessageType}"
        });

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new RabbitMQMessageBroker(
            options,
            _loggerMock.Object,
            _compressorMock.Object,
            _contractValidatorMock.Object));

        Assert.Contains("Port", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullUserName_ShouldThrowArgumentException()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672,
                UserName = null!,
                Password = "guest",
                VirtualHost = "/",
                ExchangeType = "topic"
            },
            DefaultExchange = "test-exchange",
            DefaultRoutingKeyPattern = "test.{MessageType}"
        });

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new RabbitMQMessageBroker(
            options,
            _loggerMock.Object,
            _compressorMock.Object,
            _contractValidatorMock.Object));

        Assert.Contains("UserName", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullPassword_ShouldThrowArgumentException()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = null!,
                VirtualHost = "/",
                ExchangeType = "topic"
            },
            DefaultExchange = "test-exchange",
            DefaultRoutingKeyPattern = "test.{MessageType}"
        });

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new RabbitMQMessageBroker(
            options,
            _loggerMock.Object,
            _compressorMock.Object,
            _contractValidatorMock.Object));

        Assert.Contains("Password", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullVirtualHost_ShouldThrowArgumentException()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                VirtualHost = null!,
                ExchangeType = "topic"
            },
            DefaultExchange = "test-exchange",
            DefaultRoutingKeyPattern = "test.{MessageType}"
        });

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new RabbitMQMessageBroker(
            options,
            _loggerMock.Object,
            _compressorMock.Object,
            _contractValidatorMock.Object));

        Assert.Contains("VirtualHost", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullExchangeType_ShouldThrowArgumentException()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/",
                ExchangeType = null!
            },
            DefaultExchange = "test-exchange",
            DefaultRoutingKeyPattern = "test.{MessageType}"
        });

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new RabbitMQMessageBroker(
            options,
            _loggerMock.Object,
            _compressorMock.Object,
            _contractValidatorMock.Object));

        Assert.Contains("ExchangeType", exception.Message);
    }

    [Fact]
    public void Constructor_WithInvalidExchangeType_ShouldThrowArgumentException()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions
        {
            RabbitMQ = new RabbitMQOptions
            {
                HostName = "localhost",
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/",
                ExchangeType = "invalid"
            },
            DefaultExchange = "test-exchange",
            DefaultRoutingKeyPattern = "test.{MessageType}"
        });

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new RabbitMQMessageBroker(
            options,
            _loggerMock.Object,
            _compressorMock.Object,
            _contractValidatorMock.Object));

        Assert.Contains("ExchangeType", exception.Message);
        Assert.Contains("direct, topic, fanout, headers", exception.Message);
    }

    [Fact]
    public void Constructor_WithNullRabbitMQOptions_ShouldUseDefaults()
    {
        // Arrange
        var options = Options.Create(new MessageBrokerOptions
        {
            RabbitMQ = null,
            DefaultExchange = "test-exchange",
            DefaultRoutingKeyPattern = "test.{MessageType}"
        });

        // Act & Assert
        var exception = Record.Exception(() => new RabbitMQMessageBroker(
            options,
            _loggerMock.Object,
            _compressorMock.Object,
            _contractValidatorMock.Object));

        Assert.Null(exception);
    }
}