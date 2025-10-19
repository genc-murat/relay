using System;
using System.Numerics;
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
/// Tests for RelayImplementation SendBatchAsync method
/// </summary>
public class RelayImplementationSendBatchAsyncTests
{
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
}