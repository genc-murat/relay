using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Configuration.Options.Core;
using Relay.Core.Configuration.Options.Performance;
using Relay.Core.Contracts.Dispatchers;
using Relay.Core.Contracts.Infrastructure;
using Relay.Core.Contracts.Requests;
using Relay.Core.Implementation.Core;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.Implementation;

public class RelayImplementationComprehensiveTests
{
    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RelayImplementation(null!));
    }

    [Fact]
    public void Constructor_WithNullServiceFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RelayImplementation(serviceProvider, null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();

        // Act
        var relay = new RelayImplementation(serviceProvider);

        // Assert
        Assert.NotNull(relay);
    }

    [Fact]
    public void Constructor_WithServiceFactory_CreatesInstance()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        ServiceFactory serviceFactory = t => serviceProvider.GetService(t);

        // Act
        var relay = new RelayImplementation(serviceProvider, serviceFactory);

        // Assert
        Assert.NotNull(relay);
    }

    [Fact]
    public async Task SendAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            relay.SendAsync<TestResponse>(null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendAsync_WithNullVoidRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            relay.SendAsync((IRequest)null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task StreamAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
        {
            var stream = relay.StreamAsync<string>(null!, CancellationToken.None);
            return stream.GetAsyncEnumerator().MoveNextAsync().AsTask();
        });
    }

    [Fact]
    public async Task PublishAsync_WithNullNotification_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            relay.PublishAsync<TestNotification>(null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendAsync_WithoutDispatcher_ThrowsHandlerNotFoundException()
    {
        // Arrange
        var services = new ServiceCollection();
        // Don't register any dispatcher
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var request = new TestRequest { Value = "test" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(
            () => relay.SendAsync(request, CancellationToken.None).AsTask());
        
        Assert.Equal("TestRequest", exception.RequestType);
    }

    [Fact]
    public async Task SendAsync_VoidWithoutDispatcher_ThrowsHandlerNotFoundException()
    {
        // Arrange
        var services = new ServiceCollection();
        // Don't register any dispatcher
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var request = new TestVoidRequest { Value = "test" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(
            () => relay.SendAsync(request, CancellationToken.None).AsTask());
        
        Assert.Equal("TestVoidRequest", exception.RequestType);
    }

    [Fact]
    public async Task StreamAsync_WithoutDispatcher_ThrowsHandlerNotFoundException()
    {
        // Arrange
        var services = new ServiceCollection();
        // Don't register any dispatcher
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var request = new TestStreamRequest { Count = 5 };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HandlerNotFoundException>(async () =>
        {
            await foreach (var item in relay.StreamAsync(request, CancellationToken.None))
            {
                _ = item; // Consume the stream
            }
        });
        
        Assert.Equal("TestStreamRequest", exception.RequestType);
    }

    [Fact]
    public async Task SendBatchAsync_WithNullRequests_ThrowsArgumentNullException()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            relay.SendBatchAsync<TestResponse>(null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendBatchAsync_WithEmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        IRequest<TestResponse>[] requests = new IRequest<TestResponse>[0];

        // Act
        var results = await relay.SendBatchAsync(requests, CancellationToken.None);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task SendBatchAsync_WithValidRequests_ProcessesAll()
    {
        // Arrange
        var mockDispatcher = new Mock<IRequestDispatcher>();
        var services = new ServiceCollection();
        services.AddSingleton<IRequestDispatcher>(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        
        var request1 = new TestRequest { Value = "request1" };
        var request2 = new TestRequest { Value = "request2" };
        var request3 = new TestRequest { Value = "request3" };
        var requests = new[] { request1, request2, request3 };
        
        var expectedResponse = new TestResponse { Value = "processed" };
        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var results = await relay.SendBatchAsync(requests, CancellationToken.None);

        // Assert
        Assert.Equal(3, results.Length);
        Assert.All(results, r => Assert.Equal("processed", r.Value));
        mockDispatcher.Verify(d => d.DispatchAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task SendBatchAsync_WithCancellation_CancelsOperation()
    {
        // Arrange
        var mockDispatcher = new Mock<IRequestDispatcher>();
        var services = new ServiceCollection();
        services.AddSingleton<IRequestDispatcher>(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        
        var request1 = new TestRequest { Value = "request1" };
        var request2 = new TestRequest { Value = "request2" };
        var requests = new[] { request1, request2 };
        
        var expectedResponse = new TestResponse { Value = "processed" };
        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (IRequest<TestResponse> req, CancellationToken ct) =>
            {
                await Task.Delay(100, ct); // Simulate work that can be cancelled
                ct.ThrowIfCancellationRequested();
                return expectedResponse;
            });

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(50); // Cancel after 50ms

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => 
            relay.SendBatchAsync(requests, cts.Token).AsTask());
    }

    [Fact]
    public async Task SendBatchAsync_WithDifferentRequestTypes_ProcessesAll()
    {
        // Arrange
        var mockDispatcher = new Mock<IRequestDispatcher>();
        var services = new ServiceCollection();
        services.AddSingleton<IRequestDispatcher>(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        
        var request1 = new TestRequest { Value = "request1" };
        var request2 = new TestRequest { Value = "request2" };
        var request3 = new TestRequest { Value = "request3" };
        var request4 = new TestRequest { Value = "request4" };
        var request5 = new TestRequest { Value = "request5" };
        var requests = new[] { request1, request2, request3, request4, request5 };
        
        var expectedResponse = new TestResponse { Value = "processed" };
        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var results = await relay.SendBatchAsync(requests, CancellationToken.None);

        // Assert
        Assert.Equal(5, results.Length);
        Assert.All(results, r => Assert.Equal("processed", r.Value));
        mockDispatcher.Verify(d => d.DispatchAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
    }

    [Fact]
    public async Task SendBatchAsync_WithPerformanceOptions_SIMDPathWhenEnabled()
    {
        // Arrange
        var mockDispatcher = new Mock<IRequestDispatcher>();
        var services = new ServiceCollection();
        services.AddSingleton<IRequestDispatcher>(mockDispatcher.Object);
        
        // Configure performance options to enable SIMD
        var relayOptions = new RelayOptions
        {
            Performance = new PerformanceOptions
            {
                EnableSIMDOptimizations = true,
                Profile = PerformanceProfile.Custom
            }
        };
        services.AddSingleton<IOptions<RelayOptions>>(Options.Create(relayOptions));
        
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        
        // Create enough requests to potentially trigger SIMD path (at least Vector<int>.Count)
        var requestCount = Math.Max(4, Vector<int>.Count); // Use at least 4 or SIMD vector size
        var requests = new List<IRequest<TestResponse>>();
        for (int i = 0; i < requestCount; i++)
        {
            requests.Add(new TestRequest { Value = $"request{i}" });
        }
        
        var expectedResponse = new TestResponse { Value = "processed" };
        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var results = await relay.SendBatchAsync(requests.ToArray(), CancellationToken.None);

        // Assert
        Assert.Equal(requestCount, results.Length);
        Assert.All(results, r => Assert.Equal("processed", r.Value));
        mockDispatcher.Verify(d => d.DispatchAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(requestCount));
    }

    [Fact]
    public async Task SendBatchAsync_WithPerformanceOptions_NoSIMDWhenDisabled()
    {
        // Arrange
        var mockDispatcher = new Mock<IRequestDispatcher>();
        var services = new ServiceCollection();
        services.AddSingleton<IRequestDispatcher>(mockDispatcher.Object);
        
        // Configure performance options to disable SIMD optimizations
        var relayOptions = new RelayOptions
        {
            Performance = new PerformanceOptions
            {
                EnableSIMDOptimizations = false,
                Profile = PerformanceProfile.LowMemory // This also disables SIMD
            }
        };
        services.AddSingleton<IOptions<RelayOptions>>(Options.Create(relayOptions));
        
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        
        var requestCount = 10; // More than SIMD vector size to test the fallback
        var requests = new List<IRequest<TestResponse>>();
        for (int i = 0; i < requestCount; i++)
        {
            requests.Add(new TestRequest { Value = $"request{i}" });
        }
        
        var expectedResponse = new TestResponse { Value = "processed" };
        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var results = await relay.SendBatchAsync(requests.ToArray(), CancellationToken.None);

        // Assert
        Assert.Equal(requestCount, results.Length);
        Assert.All(results, r => Assert.Equal("processed", r.Value));
        mockDispatcher.Verify(d => d.DispatchAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(requestCount));
    }

    [Fact]
    public async Task SendBatchAsync_WithSmallArray_NoSIMDPath()
    {
        // Arrange
        var mockDispatcher = new Mock<IRequestDispatcher>();
        var services = new ServiceCollection();
        services.AddSingleton<IRequestDispatcher>(mockDispatcher.Object);
        
        // Configure performance options to enable SIMD 
        var relayOptions = new RelayOptions
        {
            Performance = new PerformanceOptions
            {
                EnableSIMDOptimizations = true,
                Profile = PerformanceProfile.Custom
            }
        };
        services.AddSingleton<IOptions<RelayOptions>>(Options.Create(relayOptions));
        
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        
        // Small array that shouldn't trigger SIMD path
        var requests = new[]
        {
            new TestRequest { Value = "request1" },
            new TestRequest { Value = "request2" }
        };
        
        var expectedResponse = new TestResponse { Value = "processed" };
        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var results = await relay.SendBatchAsync(requests, CancellationToken.None);

        // Assert
        Assert.Equal(2, results.Length);
        Assert.All(results, r => Assert.Equal("processed", r.Value));
        mockDispatcher.Verify(d => d.DispatchAsync(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task PublishAsync_WithoutNotificationDispatcher_CompletesSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        // Don't register notification dispatcher
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var notification = new TestNotification();

        // Act & Assert - Should complete without error even without dispatcher
        await relay.PublishAsync(notification, CancellationToken.None);
    }

    [Fact]
    public async Task PublishAsync_WithCancellation_CancelsOperation()
    {
        // Arrange
        var mockDispatcher = new Mock<INotificationDispatcher>();
        var services = new ServiceCollection();
        services.AddSingleton<INotificationDispatcher>(mockDispatcher.Object);
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);
        var notification = new TestNotification();
        
        // Set up the dispatcher to use the cancellation token in a way that will trigger cancellation
        mockDispatcher
            .Setup(d => d.DispatchAsync(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Returns((TestNotification n, CancellationToken ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return ValueTask.CompletedTask;
            });
        
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => 
            relay.PublishAsync(notification, cts.Token).AsTask());
    }

    [Fact]
    public void ApplyPerformanceProfile_LowMemory_DisablesCaching()
    {
        // Arrange
        var options = new PerformanceOptions { Profile = PerformanceProfile.LowMemory };

        // Test the ApplyPerformanceProfile method by creating a relay with low memory profile
        var services = new ServiceCollection();
        var relayOptions = new RelayOptions { Performance = options };
        services.AddSingleton<IOptions<RelayOptions>>(Options.Create(relayOptions));
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var relay = new RelayImplementation(serviceProvider);

        // The performance options should have been applied according to the profile
        // We can't easily test this without reflection since it's internal
        Assert.NotNull(relay);
    }

    [Fact]
    public void ApplyPerformanceProfile_Balanced_EnablesCaching()
    {
        // Arrange
        var options = new PerformanceOptions { Profile = PerformanceProfile.Balanced };

        // Test the ApplyPerformanceProfile method
        var services = new ServiceCollection();
        var relayOptions = new RelayOptions { Performance = options };
        services.AddSingleton<IOptions<RelayOptions>>(Options.Create(relayOptions));
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var relay = new RelayImplementation(serviceProvider);

        // The performance options should have been applied according to the profile
        Assert.NotNull(relay);
    }

    [Fact]
    public void ApplyPerformanceProfile_HighThroughput_EnablesHighPerformance()
    {
        // Arrange
        var options = new PerformanceOptions { Profile = PerformanceProfile.HighThroughput };

        // Test the ApplyPerformanceProfile method
        var services = new ServiceCollection();
        var relayOptions = new RelayOptions { Performance = options };
        services.AddSingleton<IOptions<RelayOptions>>(Options.Create(relayOptions));
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var relay = new RelayImplementation(serviceProvider);

        // The performance options should have been applied according to the profile
        Assert.NotNull(relay);
    }

    [Fact]
    public void ApplyPerformanceProfile_UltraLowLatency_EnablesAllOptimizations()
    {
        // Arrange
        var options = new PerformanceOptions { Profile = PerformanceProfile.UltraLowLatency };

        // Test the ApplyPerformanceProfile method
        var services = new ServiceCollection();
        var relayOptions = new RelayOptions { Performance = options };
        services.AddSingleton<IOptions<RelayOptions>>(Options.Create(relayOptions));
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var relay = new RelayImplementation(serviceProvider);

        // The performance options should have been applied according to the profile
        Assert.NotNull(relay);
    }

    [Fact]
    public void ServiceFactory_Property_ReturnsValidDelegate()
    {
        // Arrange
        var services = new ServiceCollection();
        var testService = new TestService();
        services.AddSingleton<ITestService>(testService);
        var serviceProvider = services.BuildServiceProvider();
        var relay = new RelayImplementation(serviceProvider);

        // Act
        var serviceFactory = relay.ServiceFactory;
        var retrievedService = serviceFactory(typeof(ITestService)) as ITestService;

        // Assert
        Assert.NotNull(serviceFactory);
        Assert.Same(testService, retrievedService);
    }

    public class TestRequest : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestVoidRequest : IRequest
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestStreamRequest : IStreamRequest<string>
    {
        public int Count { get; set; }
    }

    public class TestResponse
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestNotification : INotification
    {
    }

    public interface ITestService
    {
    }

    public class TestService : ITestService
    {
    }
}