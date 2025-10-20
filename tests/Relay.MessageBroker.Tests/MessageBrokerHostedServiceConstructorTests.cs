using Microsoft.Extensions.Logging;
using Moq;
using Relay.MessageBroker;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class MessageBrokerHostedServiceConstructorTests
{
    [Fact]
    public async Task Constructor_AllowsNullMessageBroker()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<MessageBrokerHostedService>>();

        // Act & Assert - Should not throw
        var service = new MessageBrokerHostedService(null!, mockLogger.Object);
        Assert.NotNull(service);
    }

    [Fact]
    public async Task Constructor_AllowsNullLogger()
    {
        // Arrange
        var mockBroker = new Mock<IMessageBroker>();

        // Act & Assert - Should not throw
        var service = new MessageBrokerHostedService(mockBroker.Object, null!);
        Assert.NotNull(service);
    }
}