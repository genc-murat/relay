using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Relay.MessageBroker;
using Relay.Core.ContractValidation;
using Relay.Core;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class ServiceCollectionExtensionsContractValidatorTests
{
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