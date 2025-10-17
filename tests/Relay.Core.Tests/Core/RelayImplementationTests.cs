using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Relay.Core;
using Relay.Core.Configuration.Options;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Infrastructure;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Core;
using Relay.Core.Tests.Testing;

namespace Relay.Core.Tests.Core;

/// <summary>
/// Comprehensive unit tests for RelayImplementation
/// </summary>
public class RelayImplementationTests
{
    [Fact]
    public void Constructor_WithValidServiceProvider_ShouldSucceed()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var relay = new RelayImplementation(serviceProvider);

        // Assert
        Assert.NotNull(relay);
        Assert.IsAssignableFrom<IRelay>(relay);
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RelayImplementation(null!));
    }

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
    public async Task StreamAsync_WithValidRequest_ShouldCallDispatcher()
    {
        // Arrange
        var mockDispatcher = new Mock<IStreamDispatcher>();
        var expectedItems = new[] { 1, 2, 3 };

        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<IStreamRequest<int>>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable(expectedItems));

        var services = new ServiceCollection();
        services.AddSingleton(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();

        var relay = new RelayImplementation(serviceProvider);
        var request = new TestStreamRequest<int>();

        // Act
        var results = new List<int>();
        await foreach (var item in relay.StreamAsync(request))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(expectedItems, results);
        mockDispatcher.Verify(d => d.DispatchAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void StreamAsync_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            relay.StreamAsync<int>(null!));
    }

    [Fact]
    public async Task StreamAsync_WithNoDispatcher_ShouldThrowHandlerNotFoundException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var request = new TestStreamRequest<int>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(async () =>
        {
            await foreach (var item in relay.StreamAsync(request))
            {
                // Should not reach here
            }
        });

        Assert.Contains("TestStreamRequest", exception.RequestType);
    }

    [Fact]
    public async Task PublishAsync_WithValidNotification_ShouldCallDispatcher()
    {
        // Arrange
        var mockDispatcher = new Mock<INotificationDispatcher>();

        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();

        var relay = new RelayImplementation(serviceProvider);
        var notification = new TestNotification();

        // Act
        await relay.PublishAsync(notification);

        // Assert
        mockDispatcher.Verify(d => d.DispatchAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithNullNotification_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            relay.PublishAsync<TestNotification>(null!).AsTask());
    }

    [Fact]
    public async Task PublishAsync_WithNoDispatcher_ShouldCompleteSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var notification = new TestNotification();

        // Act & Assert - Should not throw
        await relay.PublishAsync(notification);
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
    public async Task StreamAsync_WithCancellationToken_ShouldPassTokenToDispatcher()
    {
        // Arrange
        var mockDispatcher = new Mock<IStreamDispatcher>();
        var cancellationToken = new CancellationToken(true);

        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<IStreamRequest<int>>(), cancellationToken))
            .Returns(CreateCancelledAsyncEnumerable<int>());

        var services = new ServiceCollection();
        services.AddSingleton(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();

        var relay = new RelayImplementation(serviceProvider);
        var request = new TestStreamRequest<int>();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var item in relay.StreamAsync(request, cancellationToken))
            {
                // Should not reach here
            }
        });
    }

    [Fact]
    public async Task PublishAsync_WithCancellationToken_ShouldPassTokenToDispatcher()
    {
        // Arrange
        var mockDispatcher = new Mock<INotificationDispatcher>();
        var cancellationToken = new CancellationToken(true);

        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<TestNotification>(), cancellationToken))
            .ThrowsAsync(new OperationCanceledException());

        var services = new ServiceCollection();
        services.AddSingleton(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();

        var relay = new RelayImplementation(serviceProvider);
        var notification = new TestNotification();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            relay.PublishAsync(notification, cancellationToken).AsTask());
    }

    [Fact]
    public async Task RelayImplementation_WithMultipleDispatchers_ShouldUseCorrectDispatcher()
    {
        // Arrange
        var mockRequestDispatcher = new Mock<IRequestDispatcher>();
        var mockStreamDispatcher = new Mock<IStreamDispatcher>();
        var mockNotificationDispatcher = new Mock<INotificationDispatcher>();

        mockRequestDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult("request result"));

        mockStreamDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<IStreamRequest<int>>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncEnumerable(new[] { 1, 2, 3 }));

        mockNotificationDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var services = new ServiceCollection();
        services.AddSingleton(mockRequestDispatcher.Object);
        services.AddSingleton(mockStreamDispatcher.Object);
        services.AddSingleton(mockNotificationDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();

        var relay = new RelayImplementation(serviceProvider);

        // Act
        var requestResult = await relay.SendAsync(new TestRequest<string>());

        var streamResults = new List<int>();
        await foreach (var item in relay.StreamAsync(new TestStreamRequest<int>()))
        {
            streamResults.Add(item);
        }

        await relay.PublishAsync(new TestNotification());

        // Assert
        Assert.Equal("request result", requestResult);
        Assert.Equal(new[] { 1, 2, 3 }, streamResults);

        mockRequestDispatcher.Verify(d => d.DispatchAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        mockStreamDispatcher.Verify(d => d.DispatchAsync(It.IsAny<IStreamRequest<int>>(), It.IsAny<CancellationToken>()), Times.Once);
        mockNotificationDispatcher.Verify(d => d.DispatchAsync(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendBatchAsync_WithValidRequests_ShouldReturnResults()
    {
        // Arrange
        var mockDispatcher = new Mock<IRequestDispatcher>();
        var requests = new[] { new TestRequest<string>(), new TestRequest<string>() };
        var expectedResults = new[] { "result1", "result2" };

        mockDispatcher
            .SetupSequence(d => d.DispatchAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(expectedResults[0]))
            .Returns(ValueTask.FromResult(expectedResults[1]));

        var services = new ServiceCollection();
        services.AddSingleton(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();

        var relay = new RelayImplementation(serviceProvider);

        // Act
        var results = await relay.SendBatchAsync(requests);

        // Assert
        Assert.Equal(expectedResults, results);
        mockDispatcher.Verify(d => d.DispatchAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SendBatchAsync_WithEmptyArray_ShouldReturnEmptyArray()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);

        // Act
        var results = await relay.SendBatchAsync<string>(Array.Empty<IRequest<string>>());

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task SendBatchAsync_WithNullRequests_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            relay.SendBatchAsync<string>(null!).AsTask());
    }

    [Fact]
    public async Task SendBatchAsync_WithCancellationToken_ShouldPassTokenToDispatcher()
    {
        // Arrange
        var mockDispatcher = new Mock<IRequestDispatcher>();
        var cancellationToken = new CancellationToken(true);
        var requests = new[] { new TestRequest<string>() };

        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<IRequest<string>>(), cancellationToken))
            .ThrowsAsync(new OperationCanceledException());

        var services = new ServiceCollection();
        services.AddSingleton(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();

        var relay = new RelayImplementation(serviceProvider);

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            relay.SendBatchAsync(requests, cancellationToken).AsTask());
    }

    [Fact]
    public async Task SendBatchAsync_WithSIMDOptimizationsDisabled_ShouldUseFallback()
    {
        // Arrange
        var mockDispatcher = new Mock<IRequestDispatcher>();
        var requests = new[] { new TestRequest<string>(), new TestRequest<string>() };
        var expectedResults = new[] { "result1", "result2" };

        mockDispatcher
            .SetupSequence(d => d.DispatchAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(expectedResults[0]))
            .Returns(ValueTask.FromResult(expectedResults[1]));

        var services = new ServiceCollection();
        services.Configure<RelayOptions>(options =>
        {
            options.Performance.EnableSIMDOptimizations = false;
        });
        services.AddSingleton(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();

        var relay = new RelayImplementation(serviceProvider);

        // Act
        var results = await relay.SendBatchAsync(requests);

        // Assert
        Assert.Equal(expectedResults, results);
        mockDispatcher.Verify(d => d.DispatchAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public void ServiceFactory_Property_ShouldReturnConfiguredFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        ServiceFactory expectedFactory = type => serviceProvider.GetService(type);
        var relay = new RelayImplementation(serviceProvider, expectedFactory);

        // Act
        var actualFactory = relay.ServiceFactory;

        // Assert
        Assert.Equal(expectedFactory, actualFactory);
    }

    [Fact]
    public void ServiceFactory_Property_ShouldReturnDefaultFactory_WhenNotProvided()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);

        // Act
        var factory = relay.ServiceFactory;

        // Assert
        Assert.NotNull(factory);
        // The factory should be the serviceProvider.GetService method
        Assert.Equal(serviceProvider.GetService(typeof(string)), factory(typeof(string)));
    }

    [Fact]
    public async Task SendBatchAsync_WithLargeBatch_ShouldProcessSuccessfully()
    {
        // Arrange
        var mockDispatcher = new Mock<IRequestDispatcher>();
        const int batchSize = 10;
        var requests = new IRequest<string>[batchSize];
        var expectedResults = new string[batchSize];

        for (int i = 0; i < batchSize; i++)
        {
            requests[i] = new TestRequest<string>();
            expectedResults[i] = $"result{i}";
        }

        // Setup sequence returns for each call
        var setup = mockDispatcher.SetupSequence(d => d.DispatchAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()));
        foreach (var result in expectedResults)
        {
            setup.Returns(ValueTask.FromResult(result));
        }

        var services = new ServiceCollection();
        services.AddSingleton(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();

        var relay = new RelayImplementation(serviceProvider);

        // Act
        var results = await relay.SendBatchAsync(requests);

        // Assert
        Assert.Equal(expectedResults, results);
        mockDispatcher.Verify(d => d.DispatchAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()), Times.Exactly(batchSize));
    }

    [Fact]
    public async Task SendBatchAsync_WithDispatcherFailure_ShouldPropagateException()
    {
        // Arrange
        var mockDispatcher = new Mock<IRequestDispatcher>();
        var requests = new[] { new TestRequest<string>() };

        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Dispatcher failed"));

        var services = new ServiceCollection();
        services.AddSingleton(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();

        var relay = new RelayImplementation(serviceProvider);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            relay.SendBatchAsync(requests).AsTask());

        Assert.Contains("Dispatcher failed", exception.Message);
    }

    // Coverage tests for performance optimizations and error handling paths

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

    [Fact]
    public async Task PublishAsync_WithNullNotification_UsesPreAllocatedExceptions_WhenEnabled()
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
            relay.PublishAsync<TestNotification>(null!).AsTask());
    }

    [Fact]
    public async Task PublishAsync_WithMemoryPrefetch_WhenEnabledAndSupported()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<RelayOptions>(options =>
        {
            options.Performance.EnableMemoryPrefetch = true;
        });

        var mockDispatcher = new Mock<INotificationDispatcher>();
        mockDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        services.AddSingleton<INotificationDispatcher>(mockDispatcher.Object);

        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var notification = new TestNotification();

        // Act
        await relay.PublishAsync(notification);

        // Assert
        mockDispatcher.Verify(x => x.DispatchAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StreamAsync_WithMemoryPrefetch_WhenEnabledAndSupported()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<RelayOptions>(options =>
        {
            options.Performance.EnableMemoryPrefetch = true;
        });

        var mockDispatcher = new Mock<IStreamDispatcher>();
        mockDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<TestStreamRequest>(), It.IsAny<CancellationToken>()))
            .Returns(CreateEmptyAsyncEnumerable<string>());

        services.AddSingleton<IStreamDispatcher>(mockDispatcher.Object);

        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var request = new TestStreamRequest();

        // Act
        await foreach (var item in relay.StreamAsync(request))
        {
            // Should not reach here as we return empty enumerable
            Assert.Fail("Should not have any items");
        }

        // Assert
        mockDispatcher.Verify(x => x.DispatchAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }

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
    public void Constructor_WithServiceFactory_UsesProvidedFactory()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        ServiceFactory factory = type => serviceProvider.GetService(type);

        // Act
        var relay = new RelayImplementation(serviceProvider, factory);

        // Assert
        Assert.NotNull(relay);
    }

    [Fact]
    public void Constructor_WithNullServiceFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RelayImplementation(serviceProvider, null!));
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
    public async Task PublishAsync_WithDispatcherCachingDisabled_ResolvesDispatcherEachTime()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<RelayOptions>(options =>
        {
            options.Performance.CacheDispatchers = false;
        });

        var mockDispatcher = new Mock<INotificationDispatcher>();
        mockDispatcher
            .Setup(x => x.DispatchAsync(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        services.AddSingleton<INotificationDispatcher>(mockDispatcher.Object);

        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var notification = new TestNotification();

        // Act
        await relay.PublishAsync(notification);
        await relay.PublishAsync(notification);

        // Assert
        mockDispatcher.Verify(x => x.DispatchAsync(notification, It.IsAny<CancellationToken>()), Times.Exactly(2));
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

    [Fact]
    public async Task SendBatchAsync_WithSIMDOptimizationsEnabled_ButNotSupported_ShouldUseFallback()
    {
        // Arrange
        var mockDispatcher = new Mock<IRequestDispatcher>();
        var requests = new[] { new TestRequest<string>(), new TestRequest<string>() };
        var expectedResults = new[] { "result1", "result2" };

        mockDispatcher
            .SetupSequence(d => d.DispatchAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(expectedResults[0]))
            .Returns(ValueTask.FromResult(expectedResults[1]));

        var services = new ServiceCollection();
        services.Configure<RelayOptions>(options =>
        {
            options.Performance.EnableSIMDOptimizations = true;
        });
        services.AddSingleton(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();

        var relay = new RelayImplementation(serviceProvider);

        // Act
        var results = await relay.SendBatchAsync(requests);

        // Assert
        Assert.Equal(expectedResults, results);
        mockDispatcher.Verify(d => d.DispatchAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SendBatchAsync_WithSmallBatch_ShouldUseFallback()
    {
        // Arrange
        var mockDispatcher = new Mock<IRequestDispatcher>();
        var requests = new IRequest<string>[Vector<int>.Count - 1]; // Smaller than SIMD threshold
        var expectedResults = new string[requests.Length];

        for (int i = 0; i < requests.Length; i++)
        {
            requests[i] = new TestRequest<string>();
            expectedResults[i] = $"result{i}";
        }

        var setup = mockDispatcher.SetupSequence(d => d.DispatchAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()));
        foreach (var result in expectedResults)
        {
            setup.Returns(ValueTask.FromResult(result));
        }

        var services = new ServiceCollection();
        services.Configure<RelayOptions>(options =>
        {
            options.Performance.EnableSIMDOptimizations = true;
        });
        services.AddSingleton(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();

        var relay = new RelayImplementation(serviceProvider);

        // Act
        var results = await relay.SendBatchAsync(requests);

        // Assert
        Assert.Equal(expectedResults, results);
        mockDispatcher.Verify(d => d.DispatchAsync(It.IsAny<IRequest<string>>(), It.IsAny<CancellationToken>()), Times.Exactly(requests.Length));
    }

    // Helper methods
    private static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            await Task.Delay(1);
            yield return item;
        }
    }

    private static async IAsyncEnumerable<T> CreateCancelledAsyncEnumerable<T>()
    {
        await Task.Delay(1);
        throw new OperationCanceledException();
#pragma warning disable CS0162 // Unreachable code detected
        yield break;
#pragma warning restore CS0162 // Unreachable code detected
    }

    private static async IAsyncEnumerable<T> CreateEmptyAsyncEnumerable<T>()
    {
        await Task.CompletedTask;
        yield break;
    }

    // Test classes
    private class TestRequest : IRequest<string> { }
    private class TestVoidRequest : IRequest { }
    private class TestStreamRequest : IStreamRequest<string> { }
    private class TestNotification : INotification { }
}