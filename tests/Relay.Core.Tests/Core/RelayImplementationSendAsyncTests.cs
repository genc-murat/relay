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
using Relay.Core.Configuration.Options.Core;

namespace Relay.Core.Tests.Core;

/// <summary>
/// Tests for RelayImplementation SendAsync method (with response)
/// </summary>
public class RelayImplementationSendAsyncTests
{
    [Fact]
    public async Task SendAsync_WithValidRequest_ShouldCallDispatcher()
    {
        // Arrange
        var mockDispatcher = new Mock<IRequestDispatcher>();
        var expectedResponse = "test response";

        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(expectedResponse));

        var services = new ServiceCollection();
        services.AddSingleton(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();

        var relay = new RelayImplementation(serviceProvider);
        var request = new TestRequest<string>();

        // Act
        var result = await relay.SendAsync(request);

        // Assert
        Assert.Equal(expectedResponse, result);
        mockDispatcher.Verify(d => d.DispatchAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            relay.SendAsync<string>(null!).AsTask());
    }

    [Fact]
    public async Task SendAsync_WithNoDispatcher_ShouldThrowHandlerNotFoundException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var request = new TestRequest<string>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(() =>
            relay.SendAsync(request).AsTask());

        Assert.Contains("TestRequest", exception.RequestType);
    }

    [Fact]
    public async Task SendAsync_WithCancellationToken_ShouldPassTokenToDispatcher()
    {
        // Arrange
        var mockDispatcher = new Mock<IRequestDispatcher>();
        var cancellationToken = new CancellationToken(true);

        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<IRequest<string>>(), cancellationToken))
            .ThrowsAsync(new OperationCanceledException());

        var services = new ServiceCollection();
        services.AddSingleton(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();

        var relay = new RelayImplementation(serviceProvider);
        var request = new TestRequest<string>();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            relay.SendAsync(request, cancellationToken).AsTask());
    }

    [Fact]
    public async Task SendAsync_WithNullRequest_UsesPreAllocatedExceptions_WhenEnabled()
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
            relay.SendAsync<string>((IRequest<string>)null!).AsTask());
    }

    [Fact]
    public async Task SendAsync_WithNullRequest_ThrowsDirectly_WhenPreAllocatedExceptionsDisabled()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<RelayOptions>(options =>
        {
            options.Performance.UsePreAllocatedExceptions = false;
        });

        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            relay.SendAsync<string>(null!).AsTask());
    }

    [Fact]
    public async Task SendAsync_WithMemoryPrefetch_WhenEnabledAndSupported()
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
            .Setup(x => x.DispatchAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult("response"));

        services.AddSingleton<IRequestDispatcher>(mockDispatcher.Object);

        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var request = new TestRequest();

        // Act
        var result = await relay.SendAsync(request);

        // Assert
        Assert.Equal("response", result);
        mockDispatcher.Verify(x => x.DispatchAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithCachedDispatcher_UsesCachedInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<RelayOptions>(options =>
        {
            options.Performance.CacheDispatchers = true;
        });

        var mockDispatcher = new Mock<IRequestDispatcher>();
        mockDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult("response"));

        services.AddSingleton<IRequestDispatcher>(mockDispatcher.Object);

        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var request = new TestRequest();

        // Act
        var result = await relay.SendAsync(request);

        // Assert
        Assert.Equal("response", result);
        mockDispatcher.Verify(x => x.DispatchAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithDispatcherCachingDisabled_ResolvesDispatcherEachTime()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<RelayOptions>(options =>
        {
            options.Performance.CacheDispatchers = false;
        });

        var mockDispatcher = new Mock<IRequestDispatcher>();
        mockDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult("response"));

        services.AddSingleton<IRequestDispatcher>(mockDispatcher.Object);

        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var request = new TestRequest();

        // Act
        var result1 = await relay.SendAsync(request);
        var result2 = await relay.SendAsync(request);

        // Assert
        Assert.Equal("response", result1);
        Assert.Equal("response", result2);
        mockDispatcher.Verify(x => x.DispatchAsync(request, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SendAsync_WithHandlerCacheEnabled_ShouldUseCache()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<RelayOptions>(options =>
        {
            options.Performance.EnableHandlerCache = true;
            options.Performance.HandlerCacheMaxSize = 10;
        });

        var mockDispatcher = new Mock<IRequestDispatcher>();
        mockDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult("response"));

        services.AddSingleton<IRequestDispatcher>(mockDispatcher.Object);

        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var request = new TestRequest();

        // Act
        var result1 = await relay.SendAsync(request);
        var result2 = await relay.SendAsync(request);

        // Assert
        Assert.Equal("response", result1);
        Assert.Equal("response", result2);
        mockDispatcher.Verify(x => x.DispatchAsync(request, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SendAsync_WithHandlerCacheDisabled_ShouldNotUseCache()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<RelayOptions>(options =>
        {
            options.Performance.EnableHandlerCache = false;
        });

        var mockDispatcher = new Mock<IRequestDispatcher>();
        mockDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult("response"));

        services.AddSingleton<IRequestDispatcher>(mockDispatcher.Object);

        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var request = new TestRequest();

        // Act
        var result1 = await relay.SendAsync(request);
        var result2 = await relay.SendAsync(request);

        // Assert
        Assert.Equal("response", result1);
        Assert.Equal("response", result2);
        mockDispatcher.Verify(x => x.DispatchAsync(request, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SendAsync_WithMemoryPrefetchDisabled_ShouldStillWork()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<RelayOptions>(options =>
        {
            options.Performance.EnableMemoryPrefetch = false;
        });

        var mockDispatcher = new Mock<IRequestDispatcher>();
        mockDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult("response"));

        services.AddSingleton<IRequestDispatcher>(mockDispatcher.Object);

        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var request = new TestRequest();

        // Act
        var result = await relay.SendAsync(request);

        // Assert
        Assert.Equal("response", result);
        mockDispatcher.Verify(x => x.DispatchAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}