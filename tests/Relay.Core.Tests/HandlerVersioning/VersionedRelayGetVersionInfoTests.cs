using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Handlers;
using Relay.Core.Contracts.Requests;
using Relay.Core.HandlerVersioning;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.HandlerVersioning;

public class VersionedRelayGetVersionInfoTests
{
    #region Test Request and Handler Types

    // Test request with response
    public class TestRequest : IRequest<TestResponse>, IRequest
    {
        public string Value { get; set; }

        public TestRequest(string value)
        {
            Value = value;
        }
    }

    public class TestResponse
    {
        public string Result { get; set; }

        public TestResponse(string result)
        {
            Result = result;
        }
    }

    // Test request without response
    public class VoidTestRequest : IRequest
    {
        public string Data { get; set; }

        public VoidTestRequest(string data)
        {
            Data = data;
        }
    }

    // Handler with version 1.0
    public class TestHandlerV1 : IRequestHandler<TestRequest, TestResponse>
    {
        [HandlerVersion("1.0")]
        public ValueTask<TestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return new ValueTask<TestResponse>(new TestResponse("V1: " + request.Value));
        }
    }

    // Handler with version 2.0
    public class TestHandlerV2 : IRequestHandler<TestRequest, TestResponse>
    {
        [HandlerVersion("2.0")]
        public ValueTask<TestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return new ValueTask<TestResponse>(new TestResponse("V2: " + request.Value));
        }
    }

    // Handler with version 2.1.5
    public class TestHandlerV215 : IRequestHandler<TestRequest, TestResponse>
    {
        [HandlerVersion("2.1.5")]
        public ValueTask<TestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return new ValueTask<TestResponse>(new TestResponse("V2.1.5: " + request.Value));
        }
    }

    // Handler without version attribute (should default to 1.0)
    public class TestHandlerNoVersion : IRequestHandler<TestRequest, TestResponse>
    {
        public ValueTask<TestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return new ValueTask<TestResponse>(new TestResponse("NoVersion: " + request.Value));
        }
    }

    // Handler with invalid version format
    public class TestHandlerInvalidVersion : IRequestHandler<TestRequest, TestResponse>
    {
        [HandlerVersion("invalid.version.format")]
        public ValueTask<TestResponse> HandleAsync(TestRequest request, CancellationToken cancellationToken)
        {
            return new ValueTask<TestResponse>(new TestResponse("Invalid: " + request.Value));
        }
    }

    // Void request handler with version
    public class VoidTestHandlerV1 : IRequestHandler<VoidTestRequest>
    {
        [HandlerVersion("1.0")]
        public ValueTask HandleAsync(VoidTestRequest request, CancellationToken cancellationToken)
        {
            return new ValueTask();
        }
    }

    #endregion

    private VersionedRelay CreateVersionedRelay(params object[] handlers)
    {
        var services = new ServiceCollection();
        var mockBaseRelay = new Mock<IRelay>();

        // Register handlers
        foreach (var handler in handlers)
        {
            var handlerType = handler.GetType();
            var interfaces = handlerType.GetInterfaces();

            foreach (var iface in interfaces)
            {
                if (iface.IsGenericType)
                {
                    var genericDef = iface.GetGenericTypeDefinition();
                    if (genericDef == typeof(IRequestHandler<,>) || genericDef == typeof(IRequestHandler<>))
                    {
                        services.AddSingleton(iface, handler);
                    }
                }
            }
        }

        var serviceProvider = services.BuildServiceProvider();
        var mockLogger = new Mock<ILogger<VersionedRelay>>();

        return new VersionedRelay(serviceProvider, mockBaseRelay.Object, mockLogger.Object);
    }

    #region GetAvailableVersions Tests

    [Fact]
    public void GetAvailableVersions_WithSingleVersionedHandler_ReturnsOneVersion()
    {
        // Arrange
        var handler = new TestHandlerV1();
        var versionedRelay = CreateVersionedRelay(handler);

        // Act
        var versions = versionedRelay.GetAvailableVersions<TestRequest>();

        // Assert
        Assert.NotNull(versions);
        Assert.Single(versions);
        Assert.Equal(new Version(1, 0), versions[0]);
    }

    [Fact]
    public void GetAvailableVersions_WithMultipleVersionedHandlers_ReturnsAllVersions()
    {
        // Arrange
        var handler1 = new TestHandlerV1();
        var handler2 = new TestHandlerV2();
        var handler3 = new TestHandlerV215();
        var versionedRelay = CreateVersionedRelay(handler1, handler2, handler3);

        // Act
        var versions = versionedRelay.GetAvailableVersions<TestRequest>();

        // Assert
        Assert.NotNull(versions);
        Assert.Equal(3, versions.Count);
        Assert.Contains(new Version(1, 0), versions);
        Assert.Contains(new Version(2, 0), versions);
        Assert.Contains(new Version(2, 1, 5), versions);
    }

    [Fact]
    public void GetAvailableVersions_ReturnsVersionsInDescendingOrder()
    {
        // Arrange
        var handler1 = new TestHandlerV1();
        var handler2 = new TestHandlerV2();
        var handler3 = new TestHandlerV215();
        var versionedRelay = CreateVersionedRelay(handler1, handler2, handler3);

        // Act
        var versions = versionedRelay.GetAvailableVersions<TestRequest>();

        // Assert
        Assert.Equal(new Version(2, 1, 5), versions[0]); // Highest first
        Assert.Equal(new Version(2, 0), versions[1]);
        Assert.Equal(new Version(1, 0), versions[2]); // Lowest last
    }

    [Fact]
    public void GetAvailableVersions_WithNoRegisteredHandlers_ReturnsEmptyList()
    {
        // Arrange
        var versionedRelay = CreateVersionedRelay(); // No handlers

        // Act
        var versions = versionedRelay.GetAvailableVersions<TestRequest>();

        // Assert
        Assert.NotNull(versions);
        Assert.Empty(versions);
    }

    [Fact]
    public void GetAvailableVersions_WithHandlerWithoutVersion_ReturnsDefaultVersion()
    {
        // Arrange
        var handler = new TestHandlerNoVersion();
        var versionedRelay = CreateVersionedRelay(handler);

        // Act
        var versions = versionedRelay.GetAvailableVersions<TestRequest>();

        // Assert
        Assert.NotNull(versions);
        Assert.Single(versions);
        Assert.Equal(new Version(1, 0), versions[0]);
    }

    [Fact]
    public void GetAvailableVersions_WithInvalidVersionFormat_IgnoresHandler()
    {
        // Arrange
        var handlerValid = new TestHandlerV1();
        var handlerInvalid = new TestHandlerInvalidVersion();
        var versionedRelay = CreateVersionedRelay(handlerValid, handlerInvalid);

        // Act
        var versions = versionedRelay.GetAvailableVersions<TestRequest>();

        // Assert - Should only include valid version
        Assert.NotNull(versions);
        Assert.Single(versions);
        Assert.Equal(new Version(1, 0), versions[0]);
    }

    [Fact]
    public void GetAvailableVersions_WithMixOfVersionedAndUnversioned_ReturnsAll()
    {
        // Arrange
        var handlerVersioned = new TestHandlerV2();
        var handlerUnversioned = new TestHandlerNoVersion();
        var versionedRelay = CreateVersionedRelay(handlerVersioned, handlerUnversioned);

        // Act
        var versions = versionedRelay.GetAvailableVersions<TestRequest>();

        // Assert
        Assert.NotNull(versions);
        Assert.Equal(2, versions.Count);
        Assert.Contains(new Version(1, 0), versions);
        Assert.Contains(new Version(2, 0), versions);
    }

    #endregion

    #region GetLatestVersion Tests

    [Fact]
    public void GetLatestVersion_WithMultipleVersions_ReturnsHighestVersion()
    {
        // Arrange
        var handler1 = new TestHandlerV1();
        var handler2 = new TestHandlerV2();
        var handler3 = new TestHandlerV215();
        var versionedRelay = CreateVersionedRelay(handler1, handler2, handler3);

        // Act
        var latestVersion = versionedRelay.GetLatestVersion<TestRequest>();

        // Assert
        Assert.NotNull(latestVersion);
        Assert.Equal(new Version(2, 1, 5), latestVersion);
    }

    [Fact]
    public void GetLatestVersion_WithSingleVersion_ReturnsThatVersion()
    {
        // Arrange
        var handler = new TestHandlerV1();
        var versionedRelay = CreateVersionedRelay(handler);

        // Act
        var latestVersion = versionedRelay.GetLatestVersion<TestRequest>();

        // Assert
        Assert.NotNull(latestVersion);
        Assert.Equal(new Version(1, 0), latestVersion);
    }

    [Fact]
    public void GetLatestVersion_WithNoHandlers_ReturnsNull()
    {
        // Arrange
        var versionedRelay = CreateVersionedRelay();

        // Act
        var latestVersion = versionedRelay.GetLatestVersion<TestRequest>();

        // Assert
        Assert.Null(latestVersion);
    }

    #endregion

    #region Void Request Handler Tests

    [Fact]
    public void GetAvailableVersions_WithVoidRequestHandler_ReturnsVersion()
    {
        // Arrange
        var handler = new VoidTestHandlerV1();
        var versionedRelay = CreateVersionedRelay(handler);

        // Act
        var versions = versionedRelay.GetAvailableVersions<VoidTestRequest>();

        // Assert
        Assert.NotNull(versions);
        Assert.Single(versions);
        Assert.Equal(new Version(1, 0), versions[0]);
    }

    #endregion

    #region Cache Tests

    [Fact]
    public void GetAvailableVersions_CalledMultipleTimes_UsesCachedResults()
    {
        // Arrange
        var handler = new TestHandlerV1();
        var versionedRelay = CreateVersionedRelay(handler);

        // Act
        var versions1 = versionedRelay.GetAvailableVersions<TestRequest>();
        var versions2 = versionedRelay.GetAvailableVersions<TestRequest>();
        var versions3 = versionedRelay.GetAvailableVersions<TestRequest>();

        // Assert
        Assert.Equal(versions1, versions2);
        Assert.Equal(versions2, versions3);

        // Cache should have one entry
        Assert.Equal(1, versionedRelay.GetCachedVersionCount());
    }

    [Fact]
    public void ClearVersionCache_RemovesAllCachedEntries()
    {
        // Arrange
        var handler = new TestHandlerV1();
        var versionedRelay = CreateVersionedRelay(handler);

        // Populate cache
        versionedRelay.GetAvailableVersions<TestRequest>();
        Assert.Equal(1, versionedRelay.GetCachedVersionCount());

        // Act
        versionedRelay.ClearVersionCache();

        // Assert
        Assert.Equal(0, versionedRelay.GetCachedVersionCount());

        // Calling GetAvailableVersions again should repopulate cache
        versionedRelay.GetAvailableVersions<TestRequest>();
        Assert.Equal(1, versionedRelay.GetCachedVersionCount());
    }

    [Fact]
    public void GetAvailableVersions_ForDifferentRequestTypes_CachesIndependently()
    {
        // Arrange
        var handler1 = new TestHandlerV1();
        var handler2 = new VoidTestHandlerV1();
        var versionedRelay = CreateVersionedRelay(handler1, handler2);

        // Act
        var versionsRequest1 = versionedRelay.GetAvailableVersions<TestRequest>();
        var versionsRequest2 = versionedRelay.GetAvailableVersions<VoidTestRequest>();

        // Assert
        Assert.Single(versionsRequest1);
        Assert.Single(versionsRequest2);
        Assert.Equal(2, versionedRelay.GetCachedVersionCount()); // Two different request types cached
    }

    #endregion

    #region SendAsync With Version Tests

    [Fact]
    public async Task SendAsync_WithValidVersion_FindsHandler()
    {
        // Arrange
        var handler = new TestHandlerV1();
        var versionedRelay = CreateVersionedRelay(handler);
        var request = new TestRequest("test");

        // Act & Assert - Should not throw
        await versionedRelay.SendAsync(request, new Version(1, 0), CancellationToken.None);
    }

    [Fact]
    public async Task SendAsync_WithNonExistentVersion_ThrowsException()
    {
        // Arrange
        var handler = new TestHandlerV1();
        var versionedRelay = CreateVersionedRelay(handler);
        var request = new TestRequest("test");

        // Act & Assert
        await Assert.ThrowsAsync<HandlerVersionNotFoundException>(async () =>
            await versionedRelay.SendAsync(request, new Version(99, 0), CancellationToken.None));
    }

    #endregion

    #region FindCompatibleVersion Tests

    [Fact]
    public async Task SendCompatibleAsync_WithMinVersion_FindsCompatibleVersion()
    {
        // Arrange
        var handler1 = new TestHandlerV1();
        var handler2 = new TestHandlerV2();
        var handler3 = new TestHandlerV215();
        var versionedRelay = CreateVersionedRelay(handler1, handler2, handler3);
        var request = new TestRequest("test");

        // Act - Request minimum version 2.0
        await versionedRelay.SendCompatibleAsync(request, new Version(2, 0), null, CancellationToken.None);

        // Assert - Should find version 2.1.5 (highest compatible)
        // No exception means it found a compatible version
    }

    [Fact]
    public async Task SendCompatibleAsync_WithMaxVersion_FindsCompatibleVersion()
    {
        // Arrange
        var handler1 = new TestHandlerV1();
        var handler2 = new TestHandlerV2();
        var handler3 = new TestHandlerV215();
        var versionedRelay = CreateVersionedRelay(handler1, handler2, handler3);
        var request = new TestRequest("test");

        // Act - Request maximum version 2.0
        await versionedRelay.SendCompatibleAsync(request, null, new Version(2, 0), CancellationToken.None);

        // Assert - Should find version 2.0 (highest within limit)
        // No exception means it found a compatible version
    }

    [Fact]
    public async Task SendCompatibleAsync_WithMinAndMaxVersion_FindsCompatibleVersion()
    {
        // Arrange
        var handler1 = new TestHandlerV1();
        var handler2 = new TestHandlerV2();
        var handler3 = new TestHandlerV215();
        var versionedRelay = CreateVersionedRelay(handler1, handler2, handler3);
        var request = new TestRequest("test");

        // Act - Request version between 1.5 and 2.0
        await versionedRelay.SendCompatibleAsync(request,
            new Version(1, 5),
            new Version(2, 0),
            CancellationToken.None);

        // Assert - Should find version 2.0
        // No exception means it found a compatible version
    }

    [Fact]
    public async Task SendCompatibleAsync_WithNoCompatibleVersion_ThrowsException()
    {
        // Arrange
        var handler1 = new TestHandlerV1();
        var versionedRelay = CreateVersionedRelay(handler1);
        var request = new TestRequest("test");

        // Act & Assert - Request version 5.0+, which doesn't exist
        await Assert.ThrowsAsync<HandlerVersionNotFoundException>(async () =>
            await versionedRelay.SendCompatibleAsync(request,
                new Version(5, 0),
                null,
                CancellationToken.None));
    }

    #endregion
}
