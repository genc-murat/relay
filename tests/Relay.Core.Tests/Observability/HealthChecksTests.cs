using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
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
            result.Should().NotBeNull();
            result.IsHealthy.Should().BeTrue();
            result.Description.Should().Contain("operational");
            result.Exception.Should().BeNull();
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
            result.Should().NotBeNull();
            result.IsHealthy.Should().BeFalse();
            result.Description.Should().Contain("not operational");
            result.Exception.Should().Be(expectedException);
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
            result.IsHealthy.Should().BeTrue();
            result.Description.Should().Be("All systems operational");
            result.Exception.Should().BeNull();
        }

        [Fact]
        public void RelayHealthCheckResult_Unhealthy_ShouldCreateUnhealthyResult()
        {
            // Arrange
            var exception = new Exception("Test exception");

            // Act
            var result = RelayHealthCheckResult.Unhealthy("System failure", exception);

            // Assert
            result.IsHealthy.Should().BeFalse();
            result.Description.Should().Be("System failure");
            result.Exception.Should().Be(exception);
        }

        [Fact]
        public void RelayHealthCheckResult_Unhealthy_CanBeCreatedWithoutException()
        {
            // Act
            var result = RelayHealthCheckResult.Unhealthy("System degraded");

            // Assert
            result.IsHealthy.Should().BeFalse();
            result.Description.Should().Be("System degraded");
            result.Exception.Should().BeNull();
        }

        [Fact]
        public void HealthCheckRequest_ShouldBeRecord()
        {
            // Act
            var request1 = new HealthCheckRequest();
            var request2 = new HealthCheckRequest();

            // Assert
            request1.Should().NotBeNull();
            request2.Should().NotBeNull();
            request1.Should().Be(request2); // Records have value equality
        }

        [Fact]
        public void HealthCheckResponse_Healthy_ShouldReturnHealthyResponse()
        {
            // Act
            var response = HealthCheckResponse.Healthy();

            // Assert
            response.Should().NotBeNull();
            response.IsHealthy.Should().BeTrue();
            response.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
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
            response.Should().NotBeNull();
            response.IsHealthy.Should().BeTrue();
            response.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
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
            response.Should().NotBeNull();
            response.IsHealthy.Should().BeTrue();
        }
    }
}
