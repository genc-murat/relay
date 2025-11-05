using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Diagnostics.Core;
using Relay.Core.Telemetry;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Testing;

public class RelayTestBuilderTests
{
    [Fact]
    public void Create_ShouldReturnNewInstance()
    {
        // Act
        var builder = RelayTestBuilder.Create();

        // Assert
        Assert.NotNull(builder);
    }

    [Fact]
    public void WithMockHandler_ShouldRegisterHandler()
    {
        // Arrange
        var expectedResponse = "test response";

        // Act
        var (relay, serviceProvider) = RelayTestBuilder.Create()
            .WithMockHandler<TestBuilderRequest, string>(expectedResponse)
            .BuildWithProvider();

        // Assert
        var handler = serviceProvider.GetService<IRequestHandler<TestBuilderRequest, string>>();
        Assert.NotNull(handler);
    }

    [Fact]
    public async Task WithMockHandler_ShouldReturnExpectedResponse()
    {
        // Arrange
        var expectedResponse = "test response";
        var request = new TestBuilderRequest();

        var relay = RelayTestBuilder.Create()
            .WithMockHandler<TestBuilderRequest, string>(expectedResponse)
            .Build();

        // Act
        var response = await relay.SendAsync(request);

        // Assert
        Assert.Equal(expectedResponse, response);
    }

    [Fact]
    public async Task WithMockHandler_WithException_ShouldThrowException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");
        var request = new TestBuilderRequest();

        var relay = RelayTestBuilder.Create()
            .WithMockHandler<TestBuilderRequest, string>(expectedException)
            .Build();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => relay.SendAsync(request).AsTask());
        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public void WithTelemetry_ShouldEnableTelemetry()
    {
        // Act
        var (relay, serviceProvider) = RelayTestBuilder.Create()
            .WithTelemetry()
            .BuildWithProvider();

        // Assert
        var telemetryProvider = serviceProvider.GetService<ITelemetryProvider>();
        Assert.NotNull(telemetryProvider);
        Assert.IsType<TestTelemetryProvider>(telemetryProvider);
    }

    [Fact]
    public void WithTracing_ShouldEnableTracing()
    {
        // Act
        var (relay, serviceProvider) = RelayTestBuilder.Create()
            .WithTracing()
            .BuildWithProvider();

        // Assert
        var tracer = serviceProvider.GetService<IRequestTracer>();
        Assert.NotNull(tracer);
    }

    private class TestBuilderRequest : IRequest<string>
    {
    }
}
