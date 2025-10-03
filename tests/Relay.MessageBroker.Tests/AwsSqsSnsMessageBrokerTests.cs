using FluentAssertions;
using Relay.MessageBroker.AwsSqsSns;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class AwsSqsSnsMessageBrokerTests
{
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new AwsSqsSnsMessageBroker(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithoutAwsOptions_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var options = new MessageBrokerOptions();

        // Act
        Action act = () => new AwsSqsSnsMessageBroker(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("AWS SQS/SNS options are required.");
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldSucceed()
    {
        // Arrange
        var options = new MessageBrokerOptions 
        { 
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1"
            }
        };

        // Act
        var broker = new AwsSqsSnsMessageBroker(options);

        // Assert
        broker.Should().NotBeNull();
    }

    [Fact]
    public async Task StopAsync_BeforeStart_ShouldNotThrow()
    {
        // Arrange
        var options = new MessageBrokerOptions 
        { 
            AwsSqsSns = new AwsSqsSnsOptions
            {
                Region = "us-east-1"
            }
        };
        var broker = new AwsSqsSnsMessageBroker(options);

        // Act
        Func<Task> act = async () => await broker.StopAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    private class TestMessage
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
