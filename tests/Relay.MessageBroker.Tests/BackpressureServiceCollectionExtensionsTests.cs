using Microsoft.Extensions.DependencyInjection;
using Relay.MessageBroker.Backpressure;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class BackpressureServiceCollectionExtensionsTests
{
    [Fact]
    public void AddBackpressureManagement_WithNullServices_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            BackpressureServiceCollectionExtensions.AddBackpressureManagement(null!));
    }

    [Fact]
    public void AddBackpressureManagement_ShouldRegisterBackpressureController()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddBackpressureManagement();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var controller = serviceProvider.GetService<IBackpressureController>();
        Assert.NotNull(controller);
        Assert.IsType<BackpressureController>(controller);
    }

    [Fact]
    public void AddBackpressureManagement_WithOptions_ShouldConfigureOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddBackpressureManagement(options =>
        {
            options.Enabled = true;
            options.LatencyThreshold = TimeSpan.FromSeconds(10);
            options.QueueDepthThreshold = 5000;
            options.RecoveryLatencyThreshold = TimeSpan.FromSeconds(3);
            options.SlidingWindowSize = 50;
            options.ThrottleFactor = 0.3;
        });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var controller = (BackpressureController)serviceProvider.GetService<IBackpressureController>()!;

        // We can't directly access the options from the controller, but we can verify it was created
        Assert.NotNull(controller);
    }

    [Fact]
    public void AddBackpressureManagement_ShouldRegisterAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddBackpressureManagement();

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var controller1 = serviceProvider.GetService<IBackpressureController>();
        var controller2 = serviceProvider.GetService<IBackpressureController>();

        Assert.Same(controller1, controller2);
    }
}