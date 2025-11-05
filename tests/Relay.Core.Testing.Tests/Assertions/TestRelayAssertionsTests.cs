using System.Linq;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Testing.Tests;

/// <summary>
/// Test data classes for testing assertions
/// </summary>
public class TestRelayAssertionsTestRequest : IRequest<string> { }
public class AnotherTestRelayAssertionsTestRequest : IRequest<int> { }
public class TestRelayAssertionsTestNotification : INotification { }
public class AnotherTestRelayAssertionsTestNotification : INotification { }

public class TestRelayAssertionsTests
{
    [Fact]
    public async Task ShouldHaveReceivedRequest_WithMatchingRequest_Passes()
    {
        // Arrange
        var relay = new TestRelay();
        var request = new TestRelayAssertionsTestRequest();

        // Act
        await relay.SendAsync(request);

        // Assert
        relay.ShouldHaveReceivedRequest<TestRelayAssertionsTestRequest>();
    }

    [Fact]
    public void ShouldHaveReceivedRequest_WithNoMatchingRequest_Throws()
    {
        // Arrange
        var relay = new TestRelay();

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            relay.ShouldHaveReceivedRequest<TestRelayAssertionsTestRequest>());
        Assert.Contains("Expected to find at least one request", exception.Message);
    }

    [Fact]
    public async Task ShouldHaveReceivedRequest_WithExpectedCount_Passes()
    {
        // Arrange
        var relay = new TestRelay();

        // Act
        await relay.SendAsync(new TestRelayAssertionsTestRequest());
        await relay.SendAsync(new TestRelayAssertionsTestRequest());

        // Assert
        relay.ShouldHaveReceivedRequest<TestRelayAssertionsTestRequest>(2);
    }

    [Fact]
    public async Task ShouldHaveReceivedRequest_WithWrongCount_Throws()
    {
        // Arrange
        var relay = new TestRelay();

        // Act
        await relay.SendAsync(new TestRelayAssertionsTestRequest());

        // Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            relay.ShouldHaveReceivedRequest<TestRelayAssertionsTestRequest>(2));
        Assert.Contains("Expected to find 2 request(s)", exception.Message);
    }

    [Fact]
    public void ShouldNotHaveReceivedRequest_WithNoRequests_Passes()
    {
        // Arrange
        var relay = new TestRelay();

        // Act & Assert
        relay.ShouldNotHaveReceivedRequest<TestRelayAssertionsTestRequest>();
    }

    [Fact]
    public async Task ShouldNotHaveReceivedRequest_WithMatchingRequest_Throws()
    {
        // Arrange
        var relay = new TestRelay();

        // Act
        await relay.SendAsync(new TestRelayAssertionsTestRequest());

        // Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            relay.ShouldNotHaveReceivedRequest<TestRelayAssertionsTestRequest>());
        Assert.Contains("Expected no requests of type", exception.Message);
    }

    [Fact]
    public async Task ShouldHavePublishedNotification_WithMatchingPredicate_Passes()
    {
        // Arrange
        var relay = new TestRelay();
        var notification = new TestRelayAssertionsTestNotification();

        // Act
        await relay.PublishAsync(notification);

        // Assert
        relay.ShouldHavePublishedNotification<TestRelayAssertionsTestNotification>(n => n == notification);
    }

    [Fact]
    public async Task ShouldHavePublishedNotification_WithNonMatchingPredicate_Throws()
    {
        // Arrange
        var relay = new TestRelay();

        // Act
        await relay.PublishAsync(new TestRelayAssertionsTestNotification());

        // Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            relay.ShouldHavePublishedNotification<TestRelayAssertionsTestNotification>(n => false));
        Assert.Contains("Expected to find at least one notification", exception.Message);
    }

    [Fact]
    public async Task ShouldHavePublishedNotification_WithExpectedNotification_Passes()
    {
        // Arrange
        var relay = new TestRelay();
        var notification = new TestRelayAssertionsTestNotification();

        // Act
        await relay.PublishAsync(notification);

        // Assert
        relay.ShouldHavePublishedNotification(notification);
    }

    [Fact]
    public async Task ShouldHavePublishedNotification_WithDifferentNotification_Throws()
    {
        // Arrange
        var relay = new TestRelay();

        // Act
        await relay.PublishAsync(new TestRelayAssertionsTestNotification());
        var differentNotification = new TestRelayAssertionsTestNotification();

        // Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            relay.ShouldHavePublishedNotification(differentNotification));
        Assert.Contains("Expected to find the specified notification", exception.Message);
    }

    [Fact]
    public void ShouldHaveNoSentRequests_WithNoRequests_Passes()
    {
        // Arrange
        var relay = new TestRelay();

        // Act & Assert
        relay.ShouldHaveNoSentRequests();
    }

    [Fact]
    public async Task ShouldHaveNoSentRequests_WithRequests_Throws()
    {
        // Arrange
        var relay = new TestRelay();

        // Act
        await relay.SendAsync(new TestRelayAssertionsTestRequest());

        // Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            relay.ShouldHaveNoSentRequests());
        Assert.Contains("Expected no sent requests, but found 1", exception.Message);
    }

    [Fact]
    public void ShouldHaveNoPublishedNotifications_WithNoNotifications_Passes()
    {
        // Arrange
        var relay = new TestRelay();

        // Act & Assert
        relay.ShouldHaveNoPublishedNotifications();
    }

    [Fact]
    public async Task ShouldHaveNoPublishedNotifications_WithNotifications_Throws()
    {
        // Arrange
        var relay = new TestRelay();

        // Act
        await relay.PublishAsync(new TestRelayAssertionsTestNotification());

        // Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            relay.ShouldHaveNoPublishedNotifications());
        Assert.Contains("Expected no published notifications, but found 1", exception.Message);
    }

    [Fact]
    public async Task ShouldHaveSentRequest_WithExpectedRequest_Passes()
    {
        // Arrange
        var relay = new TestRelay();
        var request = new TestRelayAssertionsTestRequest();

        // Act
        await relay.SendAsync(request);

        // Assert
        relay.ShouldHaveSentRequest(request);
    }

    [Fact]
    public async Task ShouldHaveSentRequest_WithDifferentRequest_Throws()
    {
        // Arrange
        var relay = new TestRelay();

        // Act
        await relay.SendAsync(new TestRelayAssertionsTestRequest());
        var differentRequest = new TestRelayAssertionsTestRequest();

        // Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            relay.ShouldHaveSentRequest(differentRequest));
        Assert.Contains("Expected to find the specified request", exception.Message);
    }

    [Fact]
    public async Task ShouldHaveSentLastRequest_WithMatchingType_Passes()
    {
        // Arrange
        var relay = new TestRelay();

        // Act
        await relay.SendAsync(new AnotherTestRelayAssertionsTestRequest());
        await relay.SendAsync(new TestRelayAssertionsTestRequest());

        // Assert
        relay.ShouldHaveSentLastRequest<TestRelayAssertionsTestRequest>();
    }

    [Fact]
    public void ShouldHaveSentLastRequest_WithNoRequests_Throws()
    {
        // Arrange
        var relay = new TestRelay();

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            relay.ShouldHaveSentLastRequest<TestRelayAssertionsTestRequest>());
        Assert.Contains("Expected at least one sent request", exception.Message);
    }

    [Fact]
    public async Task ShouldHaveSentLastRequest_WithWrongType_Throws()
    {
        // Arrange
        var relay = new TestRelay();

        // Act
        await relay.SendAsync(new AnotherTestRelayAssertionsTestRequest());

        // Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            relay.ShouldHaveSentLastRequest<TestRelayAssertionsTestRequest>());
        Assert.Contains("Expected the last sent request to be of type 'TestRelayAssertionsTestRequest'", exception.Message);
    }

    [Fact]
    public async Task ShouldHavePublishedLastNotification_WithMatchingType_Passes()
    {
        // Arrange
        var relay = new TestRelay();

        // Act
        await relay.PublishAsync(new AnotherTestRelayAssertionsTestNotification());
        await relay.PublishAsync(new TestRelayAssertionsTestNotification());

        // Assert
        relay.ShouldHavePublishedLastNotification<TestRelayAssertionsTestNotification>();
    }

    [Fact]
    public void ShouldHavePublishedLastNotification_WithNoNotifications_Throws()
    {
        // Arrange
        var relay = new TestRelay();

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            relay.ShouldHavePublishedLastNotification<TestRelayAssertionsTestNotification>());
        Assert.Contains("Expected at least one published notification", exception.Message);
    }

    [Fact]
    public async Task ShouldHavePublishedLastNotification_WithWrongType_Throws()
    {
        // Arrange
        var relay = new TestRelay();

        // Act
        await relay.PublishAsync(new AnotherTestRelayAssertionsTestNotification());

        // Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            relay.ShouldHavePublishedLastNotification<TestRelayAssertionsTestNotification>());
        Assert.Contains("Expected the last published notification to be of type 'TestRelayAssertionsTestNotification'", exception.Message);
    }
}