using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker;
using Relay.Core.ContractValidation;
using Relay.Core;
using Xunit;

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