using System;
using System.Threading.Tasks;
using Relay.Core.Contracts.Requests;
using Xunit;

namespace Relay.Core.Testing.Tests;

public class RelayXUnitAssertionsTests : RelayTestBase
{
    [Fact]
    public async Task ShouldHaveHandled_WithHandledRequest_Passes()
    {
        // Arrange
        await RunScenarioAsync("HandleRequest", builder =>
        {
            builder.SendRequest(new TestRequest { Value = "test" });
        });

        // Act & Assert - Should not throw
        TestRelay.ShouldHaveHandled<TestRequest>();
    }

    [Fact]
    public async Task ShouldHavePublished_WithPublishedNotification_Passes()
    {
        // Arrange
        await RunScenarioAsync("PublishNotification", builder =>
        {
            builder.PublishNotification(new TestNotification { Message = "test" });
        });

        // Act & Assert - Should not throw
        TestRelay.ShouldHavePublished<TestNotification>();
    }

    [Fact]
    public async Task ShouldHavePublishedInOrder_WithCorrectOrder_Passes()
    {
        // Arrange
        await RunScenarioAsync("OrderedNotifications", builder =>
        {
            builder.PublishNotification(new TestNotification { Message = "first" });
            builder.PublishNotification(new TestNotification { Message = "second" });
        });

        // Act & Assert - Should not throw
        TestRelay.ShouldHavePublishedInOrder(typeof(TestNotification), typeof(TestNotification));
    }

    [Fact]
    public async Task ShouldHavePublishedInOrder_Throws_WhenWrongOrder()
    {
        // Arrange
        await RunScenarioAsync("WrongOrderNotifications", builder =>
        {
            builder.PublishNotification(new TestNotification2 { Message = "first" });
            builder.PublishNotification(new TestNotification { Message = "second" });
        });

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            TestRelay.ShouldHavePublishedInOrder(typeof(TestNotification), typeof(TestNotification2)));

        Assert.Contains("Expected notification at position 0 to be of type 'TestNotification', but was 'TestNotification2'", exception.Message);
    }

    [Fact]
    public async Task ShouldHavePublishedInOrder_Throws_WhenInsufficientNotifications()
    {
        // Arrange
        await RunScenarioAsync("InsufficientNotifications", builder =>
        {
            builder.PublishNotification(new TestNotification { Message = "only one" });
        });

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            TestRelay.ShouldHavePublishedInOrder(typeof(TestNotification), typeof(TestNotification2)));

        Assert.Contains("Expected at least 2 notifications, but only 1 were published", exception.Message);
    }

    [Fact]
    public async Task ShouldHavePublishedInOrder_Throws_WhenWrongTypeAtPosition()
    {
        // Arrange
        await RunScenarioAsync("WrongTypeNotifications", builder =>
        {
            builder.PublishNotification(new TestNotification2 { Message = "first" });
            builder.PublishNotification(new TestNotification { Message = "second" });
        });

        // Act & Assert
        var exception = Assert.Throws<Xunit.Sdk.XunitException>(() =>
            TestRelay.ShouldHavePublishedInOrder(typeof(TestNotification), typeof(TestNotification)));

        Assert.Contains("Expected notification at position 0 to be of type 'TestNotification', but was 'TestNotification2'", exception.Message);
    }

    [Fact]
    public void ShouldBeSuccessful_WithSuccessfulResult_Passes()
    {
        // Arrange
        var result = new ScenarioResult
        {
            Success = true
        };

        // Act & Assert - Should not throw
        result.ShouldBeSuccessful();
    }

    [Fact]
    public void ShouldHaveFailed_WithFailedResult_Passes()
    {
        // Arrange
        var result = new ScenarioResult
        {
            Success = false,
            Error = "Failed"
        };

        // Act & Assert - Should not throw
        result.ShouldHaveFailed();
    }

    [Fact]
    public void ShouldHaveFailedWith_WithCorrectExceptionType_Passes()
    {
        // Arrange
        var result = new ScenarioResult
        {
            Success = false,
            Error = "Failed"
        };

        // Act & Assert - Should not throw
        result.ShouldHaveFailedWith<InvalidOperationException>();
    }

    [Fact]
    public void ShouldMeetPerformanceExpectations_WithGoodResults_Passes()
    {
        // Arrange
        var result = new LoadTestResult
        {
            SuccessfulRequests = 95,
            FailedRequests = 5,
            AverageResponseTime = 50.0,
            TotalDuration = TimeSpan.FromSeconds(10) // 10 seconds for 100 requests = 10 RPS
        };

        // Act & Assert - Should not throw
        result.ShouldMeetPerformanceExpectations(
            maxAverageResponseTime: TimeSpan.FromMilliseconds(100),
            maxErrorRate: 0.1,
            minRequestsPerSecond: 5.0); // Lower threshold since we have 10 RPS
    }

    // Test request and notification classes
    public class TestRequest : Relay.Core.Contracts.Requests.IRequest
    {
        public string? Value { get; set; }
    }

    public class TestNotification : INotification
    {
        public string? Message { get; set; }
    }

    public class TestNotification2 : INotification
    {
        public string? Message { get; set; }
    }
}