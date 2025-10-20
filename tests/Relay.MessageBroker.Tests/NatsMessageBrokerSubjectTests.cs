using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.MessageBroker.Nats;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class NatsMessageBrokerSubjectTests
{
    private readonly Mock<ILogger<NatsMessageBroker>> _loggerMock = new();

    [Fact]
    public void GetSubjectName_WithoutStreamName_ShouldUseDefaultPrefix()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions { Servers = new[] { "nats://localhost:4222" } }
        };
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act
        var subjectName = GetSubjectName(broker, typeof(TestMessage));

        // Assert
        Assert.Equal("relay.TestMessage", subjectName);
    }

    [Fact]
    public void GetSubjectName_WithStreamName_ShouldUseStreamPrefix()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions
            {
                Servers = new[] { "nats://localhost:4222" },
                StreamName = "my-stream"
            }
        };
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act
        var subjectName = GetSubjectName(broker, typeof(TestMessage));

        // Assert
        Assert.Equal("my-stream.TestMessage", subjectName);
    }

    [Fact]
    public void GetSubjectName_WithEmptyStreamName_ShouldUseDefaultPrefix()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions
            {
                Servers = new[] { "nats://localhost:4222" },
                StreamName = ""
            }
        };
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act
        var subjectName = GetSubjectName(broker, typeof(TestMessage));

        // Assert
        Assert.Equal("relay.TestMessage", subjectName);
    }

    [Fact]
    public void GetSubjectName_WithWhitespaceStreamName_ShouldUseDefaultPrefix()
    {
        // Arrange
        var options = new MessageBrokerOptions
        {
            Nats = new NatsOptions
            {
                Servers = new[] { "nats://localhost:4222" },
                StreamName = "   "
            }
        };
        var broker = new NatsMessageBroker(Options.Create(options), _loggerMock.Object);

        // Act
        var subjectName = GetSubjectName(broker, typeof(TestMessage));

        // Assert
        Assert.Equal("relay.TestMessage", subjectName);
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    private static string GetSubjectName(NatsMessageBroker broker, Type type)
    {
        var method = typeof(NatsMessageBroker).GetMethod("GetSubjectName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new[] { typeof(Type) }, null);
        return (string)method!.Invoke(broker, new object[] { type })!;
    }
}