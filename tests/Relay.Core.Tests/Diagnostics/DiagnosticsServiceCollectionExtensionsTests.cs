using Microsoft.Extensions.DependencyInjection;
using Relay.Core.Diagnostics.Configuration;
using Relay.Core.Diagnostics.Core;
using Relay.Core.Diagnostics.Services;
using Relay.Core.Diagnostics.Tracing;
using System;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.Diagnostics;

public class DiagnosticsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddRelayDiagnostics_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddRelayDiagnostics());
    }

    [Fact]
    public void AddRelayDiagnostics_WithNullConfigureOptions_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var exception = Record.Exception(() => services.AddRelayDiagnostics(null));
        Assert.Null(exception);
    }

    [Fact]
    public void AddRelayDiagnostics_AddsRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddRelayDiagnostics();

        // Assert
        Assert.Same(services, result);

        // Check that required services are registered
        var tracerDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IRequestTracer));
        Assert.NotNull(tracerDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, tracerDescriptor.Lifetime);

        var diagnosticsDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IRelayDiagnostics));
        Assert.NotNull(diagnosticsDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, diagnosticsDescriptor.Lifetime);

        var serviceDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(RelayDiagnosticsService));
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
    }

    [Fact]
    public void AddRelayDiagnostics_ConfiguresOptionsWithDefaultValues()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRelayDiagnostics();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DiagnosticsOptions>>();
        Assert.NotNull(options.Value);
        Assert.False(options.Value.EnableRequestTracing);
        Assert.False(options.Value.EnablePerformanceMetrics);
        Assert.Equal(1000, options.Value.TraceBufferSize);
        Assert.Equal(TimeSpan.FromHours(1), options.Value.MetricsRetentionPeriod);
    }

    [Fact]
    public void AddRelayDiagnostics_WithCustomConfiguration_AppliesConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddRelayDiagnostics(options =>
        {
            options.EnableRequestTracing = true;
            options.EnablePerformanceMetrics = true;
            options.TraceBufferSize = 500;
            options.MetricsRetentionPeriod = TimeSpan.FromMinutes(30);
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DiagnosticsOptions>>();
        Assert.True(options.Value.EnableRequestTracing);
        Assert.True(options.Value.EnablePerformanceMetrics);
        Assert.Equal(500, options.Value.TraceBufferSize);
        Assert.Equal(TimeSpan.FromMinutes(30), options.Value.MetricsRetentionPeriod);
    }

    [Fact]
    public void AddRelayDiagnosticsWithTracing_EnablesTracingWithDefaultBufferSize()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddRelayDiagnosticsWithTracing();

        // Assert
        Assert.Same(services, result);

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DiagnosticsOptions>>();
        Assert.True(options.Value.EnableRequestTracing);
        Assert.False(options.Value.EnablePerformanceMetrics);
        Assert.Equal(1000, options.Value.TraceBufferSize);
    }

    [Fact]
    public void AddRelayDiagnosticsWithTracing_WithCustomBufferSize_SetsBufferSize()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddRelayDiagnosticsWithTracing(500);

        // Assert
        Assert.Same(services, result);

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DiagnosticsOptions>>();
        Assert.True(options.Value.EnableRequestTracing);
        Assert.Equal(500, options.Value.TraceBufferSize);
    }

    [Fact]
    public void AddRelayDiagnosticsWithMetrics_EnablesMetricsWithDefaultRetentionPeriod()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddRelayDiagnosticsWithMetrics();

        // Assert
        Assert.Same(services, result);

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DiagnosticsOptions>>();
        Assert.False(options.Value.EnableRequestTracing);
        Assert.True(options.Value.EnablePerformanceMetrics);
        Assert.Equal(TimeSpan.FromHours(1), options.Value.MetricsRetentionPeriod);
    }

    [Fact]
    public void AddRelayDiagnosticsWithMetrics_WithCustomRetentionPeriod_SetsRetentionPeriod()
    {
        // Arrange
        var services = new ServiceCollection();
        var customPeriod = TimeSpan.FromMinutes(30);

        // Act
        var result = services.AddRelayDiagnosticsWithMetrics(customPeriod);

        // Assert
        Assert.Same(services, result);

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DiagnosticsOptions>>();
        Assert.True(options.Value.EnablePerformanceMetrics);
        Assert.Equal(customPeriod, options.Value.MetricsRetentionPeriod);
    }

    [Fact]
    public void AddRelayDiagnosticsWithTracingAndMetrics_EnablesBothFeatures()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddRelayDiagnosticsWithTracingAndMetrics();

        // Assert
        Assert.Same(services, result);

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DiagnosticsOptions>>();
        Assert.True(options.Value.EnableRequestTracing);
        Assert.True(options.Value.EnablePerformanceMetrics);
        Assert.Equal(1000, options.Value.TraceBufferSize);
        Assert.Equal(TimeSpan.FromHours(1), options.Value.MetricsRetentionPeriod);
    }

    [Fact]
    public void AddRelayDiagnosticsWithTracingAndMetrics_WithCustomParameters_SetsParameters()
    {
        // Arrange
        var services = new ServiceCollection();
        var customPeriod = TimeSpan.FromMinutes(45);

        // Act
        var result = services.AddRelayDiagnosticsWithTracingAndMetrics(750, customPeriod);

        // Assert
        Assert.Same(services, result);

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DiagnosticsOptions>>();
        Assert.True(options.Value.EnableRequestTracing);
        Assert.True(options.Value.EnablePerformanceMetrics);
        Assert.Equal(750, options.Value.TraceBufferSize);
        Assert.Equal(customPeriod, options.Value.MetricsRetentionPeriod);
    }

    [Fact]
    public void AddRelayDiagnosticsWithEndpoints_EnablesEndpointsWithDefaultSettings()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddRelayDiagnosticsWithEndpoints();

        // Assert
        Assert.Same(services, result);

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DiagnosticsOptions>>();
        Assert.True(options.Value.EnableDiagnosticEndpoints);
        Assert.Equal("/relay", options.Value.DiagnosticEndpointBasePath);
        Assert.True(options.Value.RequireAuthentication);
    }

    [Fact]
    public void AddRelayDiagnosticsWithEndpoints_WithCustomParameters_SetsParameters()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddRelayDiagnosticsWithEndpoints("/custom", false);

        // Assert
        Assert.Same(services, result);

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DiagnosticsOptions>>();
        Assert.True(options.Value.EnableDiagnosticEndpoints);
        Assert.Equal("/custom", options.Value.DiagnosticEndpointBasePath);
        Assert.False(options.Value.RequireAuthentication);
    }

    [Fact]
    public void AddFullRelayDiagnostics_EnablesAllFeaturesWithDefaultSettings()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddFullRelayDiagnostics();

        // Assert
        Assert.Same(services, result);

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DiagnosticsOptions>>();
        Assert.True(options.Value.EnableRequestTracing);
        Assert.True(options.Value.EnablePerformanceMetrics);
        Assert.True(options.Value.EnableDiagnosticEndpoints);
        Assert.Equal(1000, options.Value.TraceBufferSize);
        Assert.Equal(TimeSpan.FromHours(1), options.Value.MetricsRetentionPeriod);
        Assert.Equal("/relay", options.Value.DiagnosticEndpointBasePath);
        Assert.True(options.Value.RequireAuthentication);
    }

    [Fact]
    public void AddFullRelayDiagnostics_WithCustomConfiguration_AppliesAdditionalConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddFullRelayDiagnostics(options =>
        {
            options.TraceBufferSize = 2000;
            options.IncludeRequestData = true;
        });

        // Assert
        Assert.Same(services, result);

        var serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<DiagnosticsOptions>>();
        Assert.True(options.Value.EnableRequestTracing);
        Assert.True(options.Value.EnablePerformanceMetrics);
        Assert.True(options.Value.EnableDiagnosticEndpoints);
        Assert.Equal(2000, options.Value.TraceBufferSize);
        Assert.True(options.Value.IncludeRequestData);
    }

    [Fact]
    public void AddRelayDiagnosticsWithTracing_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddRelayDiagnosticsWithTracing());
    }

    [Fact]
    public void AddRelayDiagnosticsWithMetrics_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddRelayDiagnosticsWithMetrics());
    }

    [Fact]
    public void AddRelayDiagnosticsWithTracingAndMetrics_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddRelayDiagnosticsWithTracingAndMetrics());
    }

    [Fact]
    public void AddRelayDiagnosticsWithEndpoints_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddRelayDiagnosticsWithEndpoints());
    }

    [Fact]
    public void AddFullRelayDiagnostics_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddFullRelayDiagnostics());
    }
}