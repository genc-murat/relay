using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Relay.Core;
using Relay.Core.Diagnostics;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.Core.Tests.Testing;

public class RelayTestBuilderTests
{
    [Fact]
    public void Create_ShouldReturnNewInstance()
    {
        // Act
        var builder = RelayTestBuilder.Create();
        
        // Assert
        builder.Should().NotBeNull();
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
        handler.Should().NotBeNull();
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
        response.Should().Be(expectedResponse);
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
        await relay.Invoking(r => r.SendAsync(request).AsTask())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
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
        telemetryProvider.Should().NotBeNull();
        telemetryProvider.Should().BeOfType<TestTelemetryProvider>();
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
        tracer.Should().NotBeNull();
    }

    private class TestBuilderRequest : IRequest<string>
    {
    }
}