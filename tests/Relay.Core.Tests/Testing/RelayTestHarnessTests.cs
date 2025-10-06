using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Relay.Core;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.Tests.Testing;
using Xunit;

namespace Relay.Core.Tests.Testing;

public class RelayTestHarnessTests
{
    [Fact]
    public void CreateTestRelay_WithHandlers_ShouldReturnConfiguredRelay()
    {
        // Arrange
        var handler = new TestHandler<TestRequest<string>, string>("test response");

        // Act
        var relay = RelayTestHarness.CreateTestRelay(handler);

        // Assert
        relay.Should().NotBeNull();
        relay.Should().BeAssignableTo<IRelay>();
    }

    [Fact]
    public void CreateMockRelay_ShouldReturnMockWithDefaultBehaviors()
    {
        // Act
        var mockRelay = RelayTestHarness.CreateMockRelay();

        // Assert
        mockRelay.Should().NotBeNull();
        mockRelay.Object.Should().BeAssignableTo<IRelay>();
    }

    [Fact]
    public async Task CreateMockRelay_DefaultBehaviors_ShouldWork()
    {
        // Arrange
        var mockRelay = RelayTestHarness.CreateMockRelay();
        var voidRequest = new TestVoidRequest();
        var notification = new TestNotification();

        // Act & Assert - Should not throw
        await mockRelay.Object.SendAsync(voidRequest);
        await mockRelay.Object.PublishAsync(notification);

        // For generic requests, test that the mock can be configured
        mockRelay.Setup(r => r.SendAsync(It.IsAny<TestRequest<string>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync("test result");

        var request = new TestRequest<string>();
        var result = await mockRelay.Object.SendAsync(request);
        Assert.Equal("test result", result);
    }

    [Fact]
    public void AddHandler_WithInstance_ShouldRegisterHandler()
    {
        // Arrange
        var harness = new RelayTestHarness();
        var handler = new TestHandler<TestRequest<string>, string>("test response");

        // Act
        var result = harness.AddHandler(handler);

        // Assert
        result.Should().BeSameAs(harness);
    }

    [Fact]
    public void AddHandler_WithNullInstance_ShouldThrowArgumentNullException()
    {
        // Arrange
        var harness = new RelayTestHarness();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => harness.AddHandler(null!));
    }

    [Fact]
    public void AddHandler_WithType_ShouldRegisterHandlerType()
    {
        // Arrange
        var harness = new RelayTestHarness();

        // Act
        var result = harness.AddHandler<TestStringHandler>();

        // Assert
        result.Should().BeSameAs(harness);
    }

    [Fact]
    public void AddPipeline_ShouldRegisterPipelineType()
    {
        // Arrange
        var harness = new RelayTestHarness();

        // Act
        var result = harness.AddPipeline<TestPipelineBehavior>();

        // Assert
        result.Should().BeSameAs(harness);
    }

    [Fact]
    public void AddService_ShouldRegisterService()
    {
        // Arrange
        var harness = new RelayTestHarness();

        // Act
        var result = harness.AddService<ITestService, TestService>();

        // Assert
        result.Should().BeSameAs(harness);
    }

    [Fact]
    public void AddSingleton_WithTypes_ShouldRegisterSingleton()
    {
        // Arrange
        var harness = new RelayTestHarness();

        // Act
        var result = harness.AddSingleton<ITestService, TestService>();

        // Assert
        result.Should().BeSameAs(harness);
    }

    [Fact]
    public void AddSingleton_WithInstance_ShouldRegisterInstance()
    {
        // Arrange
        var harness = new RelayTestHarness();
        var service = new TestService();

        // Act
        var result = harness.AddSingleton(service);

        // Assert
        result.Should().BeSameAs(harness);
    }

    [Fact]
    public void WithTelemetry_ShouldConfigureTelemetryProvider()
    {
        // Arrange
        var harness = new RelayTestHarness();

        // Act
        var result = harness.WithTelemetry<TestTelemetryProvider>();

        // Assert
        result.Should().BeSameAs(harness);
    }

    [Fact]
    public void WithoutTelemetry_ShouldConfigureNullTelemetryProvider()
    {
        // Arrange
        var harness = new RelayTestHarness();

        // Act
        var result = harness.WithoutTelemetry();

        // Assert
        result.Should().BeSameAs(harness);
    }

    [Fact]
    public void Build_ShouldReturnRelayInstance()
    {
        // Arrange
        var harness = new RelayTestHarness();

        // Act
        var relay = harness.Build();

        // Assert
        relay.Should().NotBeNull();
        relay.Should().BeAssignableTo<IRelay>();
    }

    [Fact]
    public void Build_CalledMultipleTimes_ShouldReturnSameInstance()
    {
        // Arrange
        var harness = new RelayTestHarness();

        // Act
        var relay1 = harness.Build();
        var relay2 = harness.Build();

        // Assert
        relay1.Should().BeSameAs(relay2);
    }

    [Fact]
    public void GetServiceProvider_ShouldReturnServiceProvider()
    {
        // Arrange
        var harness = new RelayTestHarness();

        // Act
        var serviceProvider = harness.GetServiceProvider();

        // Assert
        serviceProvider.Should().NotBeNull();
    }

    [Fact]
    public void GetService_ShouldResolveService()
    {
        // Arrange
        var harness = new RelayTestHarness()
            .AddSingleton<ITestService, TestService>();

        // Act
        var service = harness.GetService<ITestService>();

        // Assert
        service.Should().NotBeNull();
        service.Should().BeOfType<TestService>();
    }

    [Fact]
    public void GetTestTelemetryProvider_WithTestTelemetry_ShouldReturnProvider()
    {
        // Arrange
        var harness = new RelayTestHarness(); // Default uses TestTelemetryProvider

        // Act
        var telemetryProvider = harness.GetTestTelemetryProvider();

        // Assert
        telemetryProvider.Should().NotBeNull();
        telemetryProvider.Should().BeOfType<TestTelemetryProvider>();
    }

    [Fact]
    public void GetTestTelemetryProvider_WithoutTestTelemetry_ShouldReturnNull()
    {
        // Arrange
        var harness = new RelayTestHarness()
            .WithoutTelemetry();

        // Act
        var telemetryProvider = harness.GetTestTelemetryProvider();

        // Assert
        telemetryProvider.Should().BeNull();
    }

    [Fact]
    public async Task IntegrationTest_WithHandler_ShouldExecuteSuccessfully()
    {
        // Arrange
        var expectedResponse = "test response";
        var handler = new TestHandler<TestRequest<string>, string>(expectedResponse);
        var harness = new RelayTestHarness()
            .AddHandler(handler);

        var relay = harness.Build();
        var request = new TestRequest<string> { ExpectedResponse = expectedResponse };

        // Act
        var response = await relay.SendAsync(request);

        // Assert
        response.Should().Be(expectedResponse);
        handler.WasCalled.Should().BeTrue();
        handler.LastRequest.Should().BeSameAs(request);
    }

    [Fact]
    public async Task IntegrationTest_WithNotificationHandler_ShouldExecuteSuccessfully()
    {
        // Arrange
        var handler = new TestNotificationHandler<TestNotification>();
        var harness = new RelayTestHarness()
            .AddHandler(handler);

        var relay = harness.Build();
        var notification = new TestNotification { Message = "test message" };

        // Act
        await relay.PublishAsync(notification);

        // Assert
        handler.WasCalled.Should().BeTrue();
        handler.LastNotification.Should().BeSameAs(notification);
    }

    // Test helper classes
    private class TestVoidRequest : IRequest
    {
    }

    private class TestStringHandler : IRequestHandler<TestRequest<string>, string>
    {
        public ValueTask<string> HandleAsync(TestRequest<string> request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(request.ExpectedResponse);
        }
    }

    private class TestPipelineBehavior : IPipelineBehavior<TestRequest<string>, string>
    {
        public async ValueTask<string> HandleAsync(TestRequest<string> request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        {
            var result = await next();
            return $"Pipeline: {result}";
        }
    }

    private interface ITestService
    {
        string GetValue();
    }

    private class TestService : ITestService
    {
        public string GetValue() => "test value";
    }
}