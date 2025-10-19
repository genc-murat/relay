using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Relay.Core;
using Relay.Core.Configuration.Options;
using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Core;
using Relay.Core.Tests.Testing;

namespace Relay.Core.Tests.Core;

/// <summary>
/// Tests for RelayImplementation SendAsync method (without response)
/// </summary>
public class RelayImplementationSendVoidTests
{
    [Fact]
    public async Task SendAsync_WithVoidRequest_ShouldCallDispatcher()
    {
        // Arrange
        var mockDispatcher = new Mock<IRequestDispatcher>();

        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();

        var relay = new RelayImplementation(serviceProvider);
        var request = new TestVoidRequest();

        // Act
        await relay.SendAsync(request);

        // Assert
        mockDispatcher.Verify(d => d.DispatchAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithNullVoidRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            relay.SendAsync((IRequest)null!).AsTask());
    }

    [Fact]
    public async Task SendAsync_WithoutResponse_WithNullRequest_UsesPreAllocatedExceptions_WhenEnabled()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<RelayOptions>(options =>
        {
            options.Performance.UsePreAllocatedExceptions = true;
        });

        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            relay.SendAsync((IRequest)null!).AsTask());
    }

    [Fact]
    public async Task SendAsync_WithoutResponse_WithMemoryPrefetch_WhenEnabledAndSupported()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<RelayOptions>(options =>
        {
            options.Performance.EnableMemoryPrefetch = true;
        });

        // Mock dispatcher to avoid handler not found
        var mockDispatcher = new Mock<IRequestDispatcher>();
        mockDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<TestVoidRequest>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        services.AddSingleton<IRequestDispatcher>(mockDispatcher.Object);

        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var request = new TestVoidRequest();

        // Act
        await relay.SendAsync(request);

        // Assert
        mockDispatcher.Verify(x => x.DispatchAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}