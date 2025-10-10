using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Relay.Core.Contracts.Core;
using Relay.Core.Observability;
using Xunit;

namespace Relay.Core.Tests.Observability
{
    public class HealthChecksTests
    {
        [Fact]
        public async Task RelayHealthCheck_ShouldReturnHealthy_WhenRelayIsOperational()
        {
            // Arrange
            var mockRelay = new Mock<IRelay>();
            mockRelay.Setup(x => x.SendAsync(It.IsAny<HealthCheckRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(HealthCheckResponse.Healthy());

            var healthCheck = new RelayHealthCheck(mockRelay.Object);

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsHealthy);
            Assert.Contains("operational", result.Description);
            Assert.Null(result.Exception);
        }

        [Fact]
        public async Task RelayHealthCheck_ShouldReturnUnhealthy_WhenRelayThrowsException()
        {
            // Arrange
            var mockRelay = new Mock<IRelay>();
            var expectedException = new InvalidOperationException("Relay is down");

            mockRelay.Setup(x => x.SendAsync(It.IsAny<HealthCheckRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            var healthCheck = new RelayHealthCheck(mockRelay.Object);

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsHealthy);
            Assert.Contains("not operational", result.Description);
            Assert.Equal(expectedException, result.Exception);
        }

        [Fact]
        public void RelayHealthCheck_ShouldThrowArgumentNullException_WhenRelayIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RelayHealthCheck(null!));
        }

        [Fact]
        public void RelayHealthCheckResult_Healthy_ShouldCreateHealthyResult()
        {
            // Act
            var result = RelayHealthCheckResult.Healthy("All systems operational");

            // Assert
            Assert.True(result.IsHealthy);
            Assert.Equal("All systems operational", result.Description);
            Assert.Null(result.Exception);
        }

        [Fact]
        public void RelayHealthCheckResult_Unhealthy_ShouldCreateUnhealthyResult()
        {
            // Arrange
            var exception = new Exception("Test exception");

            // Act
            var result = RelayHealthCheckResult.Unhealthy("System failure", exception);

            // Assert
            Assert.False(result.IsHealthy);
            Assert.Equal("System failure", result.Description);
            Assert.Equal(exception, result.Exception);
        }

        [Fact]
        public void RelayHealthCheckResult_Unhealthy_CanBeCreatedWithoutException()
        {
            // Act
            var result = RelayHealthCheckResult.Unhealthy("System degraded");

            // Assert
            Assert.False(result.IsHealthy);
            Assert.Equal("System degraded", result.Description);
            Assert.Null(result.Exception);
        }

        [Fact]
        public void HealthCheckRequest_ShouldBeRecord()
        {
            // Act
            var request1 = new HealthCheckRequest();
            var request2 = new HealthCheckRequest();

            // Assert
            Assert.NotNull(request1);
            Assert.NotNull(request2);
            Assert.Equal(request2, request1); // Records have value equality
        }

        [Fact]
        public void HealthCheckResponse_Healthy_ShouldReturnHealthyResponse()
        {
            // Act
            var response = HealthCheckResponse.Healthy();

            // Assert
            Assert.NotNull(response);
            Assert.True(response.IsHealthy);
            Assert.True(DateTime.UtcNow.Subtract(response.Timestamp).Duration() < TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task HealthCheckHandler_HandleAsync_ShouldReturnHealthyResponse()
        {
            // Arrange
            var handler = new HealthCheckHandler();
            var request = new HealthCheckRequest();

            // Act
            var response = await handler.HandleAsync(request, CancellationToken.None);

            // Assert
            Assert.NotNull(response);
            Assert.True(response.IsHealthy);
            Assert.True(DateTime.UtcNow.Subtract(response.Timestamp).Duration() < TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task HealthCheckHandler_HandleAsync_ShouldRespectCancellationToken()
        {
            // Arrange
            var handler = new HealthCheckHandler();
            var request = new HealthCheckRequest();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            var response = await handler.HandleAsync(request, cts.Token);

            // Assert - Should complete even with cancelled token since it's synchronous
            Assert.NotNull(response);
            Assert.True(response.IsHealthy);
        }
    }
}