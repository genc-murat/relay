using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.HandlerVersioning;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.HandlerVersioning;

public class VersionedRelayComprehensiveTests
{
    [Fact]
    public void Constructor_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var mockBaseRelay = new Mock<IRelay>();
        var mockLogger = new Mock<ILogger<VersionedRelay>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new VersionedRelay(
            null!,
            mockBaseRelay.Object,
            mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullBaseRelay_ThrowsArgumentNullException()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockLogger = new Mock<ILogger<VersionedRelay>>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new VersionedRelay(
            mockServiceProvider.Object,
            null!,
            mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockBaseRelay = new Mock<IRelay>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new VersionedRelay(
            mockServiceProvider.Object,
            mockBaseRelay.Object,
            null!));
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockBaseRelay = new Mock<IRelay>();
        var mockLogger = new Mock<ILogger<VersionedRelay>>();

        // Act
        var versionedRelay = new VersionedRelay(
            mockServiceProvider.Object,
            mockBaseRelay.Object,
            mockLogger.Object);

        // Assert
        Assert.NotNull(versionedRelay);
    }

    [Fact]
    public async Task SendAsync_WithoutVersion_WithValidRequest_DelegatesToBaseRelay()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockBaseRelay = new Mock<IRelay>();
        var mockLogger = new Mock<ILogger<VersionedRelay>>();
        var versionedRelay = new VersionedRelay(
            mockServiceProvider.Object,
            mockBaseRelay.Object,
            mockLogger.Object);
        var request = new TestRequest { Value = "test" };
        var expectedResponse = new TestResponse { Result = "result" };

        mockBaseRelay.Setup(x => x.SendAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await versionedRelay.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResponse, result);
        mockBaseRelay.Verify(x => x.SendAsync(request, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task SendAsync_Void_WithoutVersion_WithValidRequest_DelegatesToBaseRelay()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockBaseRelay = new Mock<IRelay>();
        var mockLogger = new Mock<ILogger<VersionedRelay>>();
        var versionedRelay = new VersionedRelay(
            mockServiceProvider.Object,
            mockBaseRelay.Object,
            mockLogger.Object);
        var request = new TestRequestIRequest { Value = "test" };

        mockBaseRelay.Setup(x => x.SendAsync(request, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await versionedRelay.SendAsync(request, CancellationToken.None);

        // Assert
        mockBaseRelay.Verify(x => x.SendAsync(request, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task StreamAsync_WithoutVersion_WithValidRequest_DelegatesToBaseRelay()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockBaseRelay = new Mock<IRelay>();
        var mockLogger = new Mock<ILogger<VersionedRelay>>();
        var versionedRelay = new VersionedRelay(
            mockServiceProvider.Object,
            mockBaseRelay.Object,
            mockLogger.Object);
        var request = new TestStreamRequest { Value = "test" };
        
        var expectedResult = new List<string> { "item1", "item2" };
        var mockStream = CreateTestStream(expectedResult);

        mockBaseRelay.Setup(x => x.StreamAsync<string>(request, It.IsAny<CancellationToken>()))
            .Returns(mockStream);

        // Act
        var results = new List<string>();
        await foreach (var item in versionedRelay.StreamAsync(request, CancellationToken.None))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(expectedResult, results);
        mockBaseRelay.Verify(x => x.StreamAsync<string>(request, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task PublishAsync_WithoutVersion_WithValidNotification_DelegatesToBaseRelay()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockBaseRelay = new Mock<IRelay>();
        var mockLogger = new Mock<ILogger<VersionedRelay>>();
        var versionedRelay = new VersionedRelay(
            mockServiceProvider.Object,
            mockBaseRelay.Object,
            mockLogger.Object);
        var notification = new TestNotification { Message = "test" };

        mockBaseRelay.Setup(x => x.PublishAsync(notification, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await versionedRelay.PublishAsync(notification, CancellationToken.None);

        // Assert
        mockBaseRelay.Verify(x => x.PublishAsync(notification, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task SendAsync_WithVersion_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockBaseRelay = new Mock<IRelay>();
        var mockLogger = new Mock<ILogger<VersionedRelay>>();
        var versionedRelay = new VersionedRelay(
            mockServiceProvider.Object,
            mockBaseRelay.Object,
            mockLogger.Object);
        var version = new Version(1, 0, 0);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            versionedRelay.SendAsync<TestResponse>(null!, version, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendAsync_WithVersion_WithNullVersion_ThrowsArgumentNullException()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockBaseRelay = new Mock<IRelay>();
        var mockLogger = new Mock<ILogger<VersionedRelay>>();
        var versionedRelay = new VersionedRelay(
            mockServiceProvider.Object,
            mockBaseRelay.Object,
            mockLogger.Object);
        var request = new TestRequest { Value = "test" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            versionedRelay.SendAsync(request, null!, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendCompatibleAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockBaseRelay = new Mock<IRelay>();
        var mockLogger = new Mock<ILogger<VersionedRelay>>();
        var versionedRelay = new VersionedRelay(
            mockServiceProvider.Object,
            mockBaseRelay.Object,
            mockLogger.Object);
        var minVersion = new Version(1, 0, 0);
        var maxVersion = new Version(2, 0, 0);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            versionedRelay.SendCompatibleAsync<TestResponse>(null!, minVersion, maxVersion, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendCompatibleAsync_WithVersionNotFound_Range_ThrowsHandlerVersionNotFoundException()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockBaseRelay = new Mock<IRelay>();
        var mockLogger = new Mock<ILogger<VersionedRelay>>();
        var versionedRelay = new VersionedRelay(
            mockServiceProvider.Object,
            mockBaseRelay.Object,
            mockLogger.Object);
        var request = new TestRequest { Value = "test" };
        var minVersion = new Version(99, 0, 0); // Version that doesn't exist
        var maxVersion = new Version(100, 0, 0); // Version that doesn't exist

        // Act & Assert
        await Assert.ThrowsAsync<HandlerVersionNotFoundException>(() => 
            versionedRelay.SendCompatibleAsync(request, minVersion, maxVersion, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendCompatibleAsync_WithMinVersionOnly_WhenNoCompatibleFound_ThrowsHandlerVersionNotFoundException()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockBaseRelay = new Mock<IRelay>();
        var mockLogger = new Mock<ILogger<VersionedRelay>>();
        var versionedRelay = new VersionedRelay(
            mockServiceProvider.Object,
            mockBaseRelay.Object,
            mockLogger.Object);
        var request = new TestRequest { Value = "test" };
        var minVersion = new Version(99, 0, 0); // Version that doesn't exist

        // Act & Assert
        await Assert.ThrowsAsync<HandlerVersionNotFoundException>(() => 
            versionedRelay.SendCompatibleAsync(request, minVersion, null, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendCompatibleAsync_WithMaxVersionOnly_WhenNoCompatibleFound_ThrowsHandlerVersionNotFoundException()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockBaseRelay = new Mock<IRelay>();
        var mockLogger = new Mock<ILogger<VersionedRelay>>();
        var versionedRelay = new VersionedRelay(
            mockServiceProvider.Object,
            mockBaseRelay.Object,
            mockLogger.Object);
        var request = new TestRequest { Value = "test" };
        var maxVersion = new Version(0, 0, 0); // Version that doesn't exist

        // Act & Assert
        await Assert.ThrowsAsync<HandlerVersionNotFoundException>(() => 
            versionedRelay.SendCompatibleAsync(request, null, maxVersion, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task SendAsync_WithVersion_ToSpecificHandler_DelegatesCorrectly()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockBaseRelay = new Mock<IRelay>();
        var mockLogger = new Mock<ILogger<VersionedRelay>>();
        var versionedRelay = new VersionedRelay(
            mockServiceProvider.Object,
            mockBaseRelay.Object,
            mockLogger.Object);
        var request = new TestRequest { Value = "test" };
        var version = new Version(1, 0, 0);
        var expectedResponse = new TestResponse { Result = "result" };

        mockBaseRelay.Setup(x => x.SendAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await versionedRelay.SendAsync(request, version, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResponse, result);
        mockBaseRelay.Verify(x => x.SendAsync(request, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task SendCompatibleAsync_WithSpecificVersion_RunsSuccessfully()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockBaseRelay = new Mock<IRelay>();
        var mockLogger = new Mock<ILogger<VersionedRelay>>();
        var versionedRelay = new VersionedRelay(
            mockServiceProvider.Object,
            mockBaseRelay.Object,
            mockLogger.Object);
        var request = new TestRequest { Value = "test" };
        var minVersion = new Version(1, 0, 0);
        var maxVersion = new Version(2, 0, 0);
        var expectedResponse = new TestResponse { Result = "result" };

        mockBaseRelay.Setup(x => x.SendAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await versionedRelay.SendCompatibleAsync(request, minVersion, maxVersion, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResponse, result);
        mockBaseRelay.Verify(x => x.SendAsync(request, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public void GetAvailableVersions_WithMultipleRequests_ReturnsCorrectVersions()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockBaseRelay = new Mock<IRelay>();
        var mockLogger = new Mock<ILogger<VersionedRelay>>();
        var versionedRelay = new VersionedRelay(
            mockServiceProvider.Object,
            mockBaseRelay.Object,
            mockLogger.Object);

        // Act
        var versions1 = versionedRelay.GetAvailableVersions<TestRequestIRequest>();
        var versions2 = versionedRelay.GetAvailableVersions<AnotherTestRequestIRequest>();

        // Assert
        Assert.NotNull(versions1);
        Assert.NotNull(versions2);
        Assert.NotEmpty(versions1);
        Assert.NotEmpty(versions2);
    }

    [Fact]
    public void GetLatestVersion_WithValidRequest_ReturnsVersion()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockBaseRelay = new Mock<IRelay>();
        var mockLogger = new Mock<ILogger<VersionedRelay>>();
        var versionedRelay = new VersionedRelay(
            mockServiceProvider.Object,
            mockBaseRelay.Object,
            mockLogger.Object);

        // Act
        var version = versionedRelay.GetLatestVersion<TestRequestIRequest>();

        // Assert
        Assert.NotNull(version);
        Assert.Equal(new Version(1, 0, 0), version); // Based on the default implementation
    }

    [Fact]
    public async Task StreamAsync_DelegatesToBaseRelay()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockBaseRelay = new Mock<IRelay>();
        var mockLogger = new Mock<ILogger<VersionedRelay>>();
        var versionedRelay = new VersionedRelay(
            mockServiceProvider.Object,
            mockBaseRelay.Object,
            mockLogger.Object);
        var request = new TestStreamRequest { Value = "test" };
        
        var expectedResult = new List<string> { "item1", "item2" };
        var mockStream = CreateTestStream(expectedResult);

        mockBaseRelay.Setup(x => x.StreamAsync<string>(request, It.IsAny<CancellationToken>()))
            .Returns(mockStream);

        // Act
        var results = new List<string>();
        await foreach (var item in versionedRelay.StreamAsync(request, CancellationToken.None))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(expectedResult, results);
        mockBaseRelay.Verify(x => x.StreamAsync<string>(request, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task PublishAsync_DelegatesToBaseRelay()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockBaseRelay = new Mock<IRelay>();
        var mockLogger = new Mock<ILogger<VersionedRelay>>();
        var versionedRelay = new VersionedRelay(
            mockServiceProvider.Object,
            mockBaseRelay.Object,
            mockLogger.Object);
        var notification = new TestNotification { Message = "test" };

        mockBaseRelay.Setup(x => x.PublishAsync(notification, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await versionedRelay.PublishAsync(notification, CancellationToken.None);

        // Assert
        mockBaseRelay.Verify(x => x.PublishAsync(notification, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public void HandlerVersionNotFoundException_Constructor_WithRequestTypeAndVersion_SetsProperties()
    {
        // Arrange
        var requestType = typeof(TestRequest);
        var version = new Version(1, 2, 3);

        // Act
        var exception = new HandlerVersionNotFoundException(requestType, version);

        // Assert
        Assert.Equal(requestType, exception.RequestType);
        Assert.Equal(version, exception.RequestedVersion);
        Assert.Null(exception.MinVersion);
        Assert.Null(exception.MaxVersion);
    }

    [Fact]
    public void HandlerVersionNotFoundException_Constructor_WithRequestTypeAndVersionRange_SetsProperties()
    {
        // Arrange
        var requestType = typeof(TestRequest);
        var minVersion = new Version(1, 0, 0);
        var maxVersion = new Version(2, 0, 0);

        // Act
        var exception = new HandlerVersionNotFoundException(requestType, minVersion, maxVersion);

        // Assert
        Assert.Equal(requestType, exception.RequestType);
        Assert.Equal(minVersion, exception.MinVersion);
        Assert.Equal(maxVersion, exception.MaxVersion);
        Assert.Null(exception.RequestedVersion);
    }

    private static IAsyncEnumerable<string> CreateTestStream(List<string> items)
    {
        // Create a simple async enumerable for testing
        async IAsyncEnumerable<string> Stream()
        {
            foreach (var item in items)
            {
                await Task.Yield();
                yield return item;
            }
        }
        return Stream();
    }

    // Test implementations for different request types
    public class TestRequest : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestRequestIRequest : IRequest
    {
        public string Value { get; set; } = string.Empty;
    }

    public class AnotherTestRequestIRequest : IRequest
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestStreamRequest : IStreamRequest<string>
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }

    public class TestNotification : INotification
    {
        public string Message { get; set; } = string.Empty;
    }
}
