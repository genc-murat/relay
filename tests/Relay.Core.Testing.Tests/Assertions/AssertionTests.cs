using System;
using System.Threading.Tasks;
using Relay.Core.Testing;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Testing.Tests;

/// <summary>
/// Test classes for assertion testing
/// </summary>
public class AssertionTestRequest : IRequest<string>
{
    public string Name { get; set; } = "Test";
}

public class AssertionTestRequest2 : IRequest<string>
{
    public string Name { get; set; } = "Test2";
}

public class AssertionTestNotification : INotification
{
    public string Message { get; set; } = "Test Message";
}

public class AssertionTests
{
    [Fact]
    public async Task ShouldHaveHandled_Throws_WhenNoRequestsOfType()
    {
        // Arrange
        var relay = new TestRelay();

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            relay.ShouldHaveHandled<AssertionTestRequest>());

        Assert.Contains("Expected to find at least one request of type 'AssertionTestRequest'", exception.Message);
    }

    [Fact]
    public async Task ShouldHaveHandled_Succeeds_WhenRequestExists()
    {
        // Arrange
        var relay = new TestRelay();
        await relay.SendAsync(new AssertionTestRequest());

        // Act & Assert - Should not throw
        relay.ShouldHaveHandled<AssertionTestRequest>();
    }

    [Fact]
    public async Task ShouldHaveHandled_WithCount_Succeeds_WhenExactCount()
    {
        // Arrange
        var relay = new TestRelay();
        await relay.SendAsync(new AssertionTestRequest());
        await relay.SendAsync(new AssertionTestRequest());

        // Act & Assert - Should not throw
        relay.ShouldHaveHandled<AssertionTestRequest>(2);
    }

    [Fact]
    public async Task ShouldHaveHandled_WithCount_Throws_WhenWrongCount()
    {
        // Arrange
        var relay = new TestRelay();
        await relay.SendAsync(new AssertionTestRequest());

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            relay.ShouldHaveHandled<AssertionTestRequest>(2));

        Assert.Contains("Expected to find 2 request(s) of type 'AssertionTestRequest', but found 1", exception.Message);
    }

    [Fact]
    public async Task ShouldHavePublished_Throws_WhenNoNotificationsOfType()
    {
        // Arrange
        var relay = new TestRelay();

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            relay.ShouldHavePublished<AssertionTestNotification>());

        Assert.Contains("Expected to find at least one notification of type 'AssertionTestNotification'", exception.Message);
    }

    [Fact]
    public async Task ShouldHavePublished_Succeeds_WhenNotificationExists()
    {
        // Arrange
        var relay = new TestRelay();
        await relay.PublishAsync(new AssertionTestNotification());

        // Act & Assert - Should not throw
        relay.ShouldHavePublished<AssertionTestNotification>();
    }

    [Fact]
    public async Task ShouldHavePublished_WithPredicate_Succeeds_WhenMatching()
    {
        // Arrange
        var relay = new TestRelay();
        await relay.PublishAsync(new AssertionTestNotification { Message = "Specific Message" });

        // Act & Assert - Should not throw
        relay.ShouldHavePublished<AssertionTestNotification>(n => n.Message == "Specific Message");
    }

    [Fact]
    public async Task ShouldHavePublished_WithPredicate_Throws_WhenNotMatching()
    {
        // Arrange
        var relay = new TestRelay();
        await relay.PublishAsync(new AssertionTestNotification { Message = "Wrong Message" });

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            relay.ShouldHavePublished<AssertionTestNotification>(n => n.Message == "Specific Message"));

        Assert.Contains("Expected to find at least one notification of type 'AssertionTestNotification' matching the specified predicate", exception.Message);
    }

    [Fact]
    public async Task ShouldHavePublished_WithCount_Succeeds_WhenExactCount()
    {
        // Arrange
        var relay = new TestRelay();
        await relay.PublishAsync(new AssertionTestNotification());
        await relay.PublishAsync(new AssertionTestNotification());

        // Act & Assert - Should not throw
        relay.ShouldHavePublished<AssertionTestNotification>(2);
    }

    [Fact]
    public async Task ShouldHavePublished_WithCount_Throws_WhenWrongCount()
    {
        // Arrange
        var relay = new TestRelay();
        await relay.PublishAsync(new AssertionTestNotification());

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            relay.ShouldHavePublished<AssertionTestNotification>(2));

        Assert.Contains("Expected to find 2 notification(s) of type 'AssertionTestNotification', but found 1", exception.Message);
    }

    [Fact]
    public async Task ShouldHaveHandledInOrder_Succeeds_WhenCorrectOrder()
    {
        // Arrange
        var relay = new TestRelay();
        await relay.SendAsync(new AssertionTestRequest());
        await relay.SendAsync(new AssertionTestRequest2());

        // Act & Assert - Should not throw
        relay.ShouldHaveHandledInOrder(typeof(AssertionTestRequest), typeof(AssertionTestRequest2));
    }

    [Fact]
    public async Task ShouldHaveHandledInOrder_Throws_WhenWrongOrder()
    {
        // Arrange
        var relay = new TestRelay();
        await relay.SendAsync(new AssertionTestRequest2());
        await relay.SendAsync(new AssertionTestRequest());

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            relay.ShouldHaveHandledInOrder(typeof(AssertionTestRequest), typeof(AssertionTestRequest2)));

        Assert.Contains("Expected request at position 0 to be of type 'AssertionTestRequest', but was 'AssertionTestRequest2'", exception.Message);
    }

    [Fact]
    public async Task ShouldHaveHandledInOrder_Throws_WhenInsufficientRequests()
    {
        // Arrange
        var relay = new TestRelay();
        await relay.SendAsync(new AssertionTestRequest());

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            relay.ShouldHaveHandledInOrder(typeof(AssertionTestRequest), typeof(AssertionTestRequest2)));

        Assert.Contains("Expected at least 2 requests, but only 1 were sent", exception.Message);
    }

    [Fact]
    public async Task ShouldNotHaveHandled_Throws_WhenRequestsExist()
    {
        // Arrange
        var relay = new TestRelay();
        await relay.SendAsync(new AssertionTestRequest());

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            relay.ShouldNotHaveHandled<AssertionTestRequest>());

        Assert.Contains("Expected no requests of type 'AssertionTestRequest' to be handled, but found 1", exception.Message);
    }

    [Fact]
    public async Task ShouldNotHaveHandled_Succeeds_WhenNoRequests()
    {
        // Arrange
        var relay = new TestRelay();

        // Act & Assert - Should not throw
        relay.ShouldNotHaveHandled<AssertionTestRequest>();
    }

    [Fact]
    public async Task ShouldNotHavePublished_Throws_WhenNotificationsExist()
    {
        // Arrange
        var relay = new TestRelay();
        await relay.PublishAsync(new AssertionTestNotification());

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            relay.ShouldNotHavePublished<AssertionTestNotification>());

        Assert.Contains("Expected no notifications of type 'AssertionTestNotification' to be published, but found 1", exception.Message);
    }

    [Fact]
    public async Task ShouldNotHavePublished_Succeeds_WhenNoNotifications()
    {
        // Arrange
        var relay = new TestRelay();

        // Act & Assert - Should not throw
        relay.ShouldNotHavePublished<AssertionTestNotification>();
    }

    [Fact]
    public async Task ShouldHaveNoSentRequests_Throws_WhenRequestsExist()
    {
        // Arrange
        var relay = new TestRelay();
        await relay.SendAsync(new AssertionTestRequest());

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            relay.ShouldHaveNoSentRequests());

        Assert.Contains("Expected no sent requests, but found 1", exception.Message);
    }

    [Fact]
    public async Task ShouldHaveNoSentRequests_Succeeds_WhenNoRequests()
    {
        // Arrange
        var relay = new TestRelay();

        // Act & Assert - Should not throw
        relay.ShouldHaveNoSentRequests();
    }

    [Fact]
    public async Task ShouldHaveNoPublishedNotifications_Throws_WhenNotificationsExist()
    {
        // Arrange
        var relay = new TestRelay();
        await relay.PublishAsync(new TestNotification());

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            relay.ShouldHaveNoPublishedNotifications());

        Assert.Contains("Expected no published notifications, but found 1", exception.Message);
    }

    [Fact]
    public async Task ShouldHaveNoPublishedNotifications_Succeeds_WhenNoNotifications()
    {
        // Arrange
        var relay = new TestRelay();

        // Act & Assert - Should not throw
        relay.ShouldHaveNoPublishedNotifications();
    }
}