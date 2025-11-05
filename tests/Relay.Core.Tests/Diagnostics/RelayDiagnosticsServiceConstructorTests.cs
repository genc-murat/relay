using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.Diagnostics;
using Relay.Core.Diagnostics.Configuration;
using Relay.Core.Diagnostics.Services;
using Relay.Core.Diagnostics.Tracing;
using Xunit;

namespace Relay.Core.Tests.Diagnostics;

/// <summary>
/// Tests for RelayDiagnosticsService constructor validation
/// </summary>
public class RelayDiagnosticsServiceConstructorTests
{
    private class TestRequest : IRequest<string>
    {
        public string Data { get; set; } = "test";
    }

    private class TestVoidRequest : IRequest
    {
    }

    private IServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRelay, TestRelay>();
        return services.BuildServiceProvider();
    }

    private class TestRelay : IRelay
    {
        public ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            return new ValueTask<TResponse>(default(TResponse)!);
        }

        public ValueTask SendAsync(IRequest request, CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
        }

        public IAsyncEnumerable<TResponse> StreamAsync<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public ValueTask PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            throw new NotImplementedException();
        }
    }

    [Fact]
    public void Constructor_ShouldThrowOnNullDiagnostics()
    {
        // Arrange
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions());
        var serviceProvider = CreateServiceProvider();

        // Act
        Action act = () => new RelayDiagnosticsService(null!, tracer, options, serviceProvider);

        // Assert
        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("diagnostics", exception.ParamName);
    }

    [Fact]
    public void Constructor_ShouldThrowOnNullTracer()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var options = Options.Create(new DiagnosticsOptions());
        var serviceProvider = CreateServiceProvider();

        // Act
        Action act = () => new RelayDiagnosticsService(diagnostics, null!, options, serviceProvider);

        // Assert
        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("tracer", exception.ParamName);
    }

    [Fact]
    public void Constructor_ShouldThrowOnNullOptions()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var tracer = new RequestTracer();
        var serviceProvider = CreateServiceProvider();

        // Act
        Action act = () => new RelayDiagnosticsService(diagnostics, tracer, null!, serviceProvider);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowOnNullServiceProvider()
    {
        // Arrange
        var diagnostics = new DefaultRelayDiagnostics(new RequestTracer(), new DiagnosticsOptions());
        var tracer = new RequestTracer();
        var options = Options.Create(new DiagnosticsOptions());

        // Act
        Action act = () => new RelayDiagnosticsService(diagnostics, tracer, options, null!);

        // Assert
        var exception = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("serviceProvider", exception.ParamName);
    }
}
