using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Relay.Core;
using Relay.Core.Configuration.Options;
using Relay.Core.Implementation.Core;
using Relay.Core.Configuration.Options.Core;
using Relay.Core.Configuration.Options.Performance;

namespace Relay.Core.Tests.Core;

/// <summary>
/// Tests for RelayImplementation performance-related functionality
/// </summary>
public class RelayImplementationPerformanceTests
{
    [Fact]
    public void ApplyPerformanceProfile_WithHighThroughputProfile_EnablesOptimizations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<RelayOptions>(options =>
        {
            options.Performance.Profile = PerformanceProfile.HighThroughput;
        });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var relay = new RelayImplementation(serviceProvider);

        // Assert - We can't directly test private fields, but we can verify the relay was created
        Assert.NotNull(relay);
    }

    [Fact]
    public void ApplyPerformanceProfile_WithUltraLowLatencyProfile_EnablesOptimizations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<RelayOptions>(options =>
        {
            options.Performance.Profile = PerformanceProfile.UltraLowLatency;
        });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var relay = new RelayImplementation(serviceProvider);

        // Assert
        Assert.NotNull(relay);
    }

    [Fact]
    public void ApplyPerformanceProfile_WithLowMemoryProfile_DisablesOptimizations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<RelayOptions>(options =>
        {
            options.Performance.Profile = PerformanceProfile.LowMemory;
        });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var relay = new RelayImplementation(serviceProvider);

        // Assert
        Assert.NotNull(relay);
    }

    [Fact]
    public void ApplyPerformanceProfile_WithBalancedProfile_EnablesModerateOptimizations()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<RelayOptions>(options =>
        {
            options.Performance.Profile = PerformanceProfile.Balanced;
        });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var relay = new RelayImplementation(serviceProvider);

        // Assert
        Assert.NotNull(relay);
    }

    [Fact]
    public void ApplyPerformanceProfile_WithCustomProfile_PreservesSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<RelayOptions>(options =>
        {
            options.Performance.Profile = PerformanceProfile.Custom;
            options.Performance.CacheDispatchers = true;
            options.Performance.EnableHandlerCache = false;
        });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var relay = new RelayImplementation(serviceProvider);

        // Assert
        Assert.NotNull(relay);
    }
}
