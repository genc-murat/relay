using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.Contracts.Core;
using Relay.Core.Contracts.Requests;
using Relay.Core.HandlerVersioning;
using Xunit;

namespace Relay.Core.Tests.HandlerVersioning
{
    public class VersionedRelayTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IRelay> _mockBaseRelay;
        private readonly Mock<ILogger<VersionedRelay>> _mockLogger;
        private readonly VersionedRelay _versionedRelay;

        public VersionedRelayTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockBaseRelay = new Mock<IRelay>();
            _mockLogger = new Mock<ILogger<VersionedRelay>>();

            _versionedRelay = new VersionedRelay(
                _mockServiceProvider.Object,
                _mockBaseRelay.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task SendAsync_ShouldDelegateToBaseRelay_ForNonVersionedRequests()
        {
            // Arrange
            var request = new TestRequest { Value = "test" };
            var expectedResponse = new TestResponse { Result = "result" };

            _mockBaseRelay.Setup(x => x.SendAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _versionedRelay.SendAsync(request, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, result);
            _mockBaseRelay.Verify(x => x.SendAsync(request, It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public async Task SendAsync_WithVersion_ShouldFindAndInvokeVersionedHandler()
        {
            // Arrange
            var request = new TestRequest { Value = "test" };
            var version = new Version(1, 0, 0);
            var expectedResponse = new TestResponse { Result = "result" };

            _mockBaseRelay.Setup(x => x.SendAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _versionedRelay.SendAsync(request, version, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, result);
        }

        [Fact]
        public async Task SendAsync_WithVersion_ShouldThrowException_WhenVersionNotFound()
        {
            // Arrange
            var request = new TestRequest { Value = "test" };
            var version = new Version(99, 0, 0);

            // Act & Assert
            await Assert.ThrowsAsync<HandlerVersionNotFoundException>(async () =>
                await _versionedRelay.SendAsync(request, version, CancellationToken.None));
        }

        [Fact]
        public async Task SendCompatibleAsync_ShouldFindCompatibleVersion()
        {
            // Arrange
            var request = new TestRequest { Value = "test" };
            var minVersion = new Version(1, 0, 0);
            var maxVersion = new Version(2, 0, 0);
            var expectedResponse = new TestResponse { Result = "result" };

            _mockBaseRelay.Setup(x => x.SendAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _versionedRelay.SendCompatibleAsync(request, minVersion, maxVersion, CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, result);
        }

        [Fact]
        public void GetAvailableVersions_ShouldReturnVersions()
        {
            // Act
            var versions = _versionedRelay.GetAvailableVersions<TestRequestIRequest>();

            // Assert
            Assert.NotNull(versions);
            Assert.NotEmpty(versions);
        }

        [Fact]
        public void GetLatestVersion_ShouldReturnLatestVersion()
        {
            // Act
            var version = _versionedRelay.GetLatestVersion<TestRequestIRequest>();

            // Assert
            Assert.NotNull(version);
            Assert.Equal(new Version(1, 0, 0), version);
        }

        [Fact]
        public async Task PublishAsync_ShouldDelegateToBaseRelay()
        {
            // Arrange
            var notification = new TestNotification { Message = "test" };

            _mockBaseRelay.Setup(x => x.PublishAsync(notification, It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);

            // Act
            await _versionedRelay.PublishAsync(notification, CancellationToken.None);

            // Assert
            _mockBaseRelay.Verify(x => x.PublishAsync(notification, It.IsAny<CancellationToken>()), Times.Once());
        }

        [Fact]
        public void HandlerVersionNotFoundException_ShouldContainRequestType()
        {
            // Arrange
            var requestType = typeof(TestRequest);
            var version = new Version(1, 0, 0);

            // Act
            var exception = new HandlerVersionNotFoundException(requestType, version);

            // Assert
            Assert.Equal(requestType, exception.RequestType);
            Assert.Equal(version, exception.RequestedVersion);
        }

        [Fact]
        public void HandlerVersionNotFoundException_WithRange_ShouldContainVersionRange()
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
        }

        public class TestRequest : IRequest<TestResponse>
        {
            public string Value { get; set; } = string.Empty;
        }

        public class TestRequestIRequest : IRequest
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
}
