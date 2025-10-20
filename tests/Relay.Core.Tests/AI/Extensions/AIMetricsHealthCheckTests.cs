using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Metrics.Interfaces;
using Xunit;

namespace Relay.Core.Tests.AI.Extensions
{
    public class AIMetricsHealthCheckTests
    {
        private readonly Mock<IAIMetricsExporter> _exporterMock;
        private readonly ILogger<AIMetricsHealthCheck> _logger;
        private readonly AIHealthCheckOptions _options;

        public AIMetricsHealthCheckTests()
        {
            _exporterMock = new Mock<IAIMetricsExporter>();
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<AIMetricsHealthCheck>();
            _options = new AIHealthCheckOptions
            {
                MinAccuracyScore = 0.7
            };
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_Throw_When_Exporter_Is_Null()
        {
            // Arrange
            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AIMetricsHealthCheck(null!, _logger, optionsMock.Object));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Arrange
            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AIMetricsHealthCheck(_exporterMock.Object, null!, optionsMock.Object));
        }

        [Fact]
        public void Constructor_Should_Use_Default_Options_When_Options_Is_Null()
        {
            // Arrange
            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns((AIHealthCheckOptions)null!);

            // Act
            var healthCheck = new AIMetricsHealthCheck(_exporterMock.Object, _logger, optionsMock.Object);

            // Assert - should not throw, uses default options
            Assert.NotNull(healthCheck);
        }

        #endregion

        #region CheckHealthAsync Tests

        [Fact]
        public async Task CheckHealthAsync_Should_Return_Healthy_Result_When_Export_Succeeds()
        {
            // Arrange
            _exporterMock.Setup(e => e.ExportMetricsAsync(It.IsAny<AIModelStatistics>(), It.IsAny<CancellationToken>()))
                        .Returns(ValueTask.CompletedTask);

            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            var healthCheck = new AIMetricsHealthCheck(_exporterMock.Object, _logger, optionsMock.Object);

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.True(result.IsHealthy);
            Assert.Equal("Operational", result.Status);
            Assert.Equal("AI Metrics Exporter", result.ComponentName);
            Assert.Equal(1.0, result.HealthScore);
            Assert.Contains("operational", result.Description);
            Assert.True(result.Duration > TimeSpan.Zero);
            Assert.Equal(true, result.Data["TestExportSuccessful"]);

            // Verify that ExportMetricsAsync was called with test statistics
            _exporterMock.Verify(e => e.ExportMetricsAsync(
                It.Is<AIModelStatistics>(stats =>
                    stats.AccuracyScore == 0.85 &&
                    stats.ModelVersion == "health-check-test" &&
                    stats.TotalPredictions == 0),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Return_Failed_Result_When_Export_Fails()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Export failed");
            _exporterMock.Setup(e => e.ExportMetricsAsync(It.IsAny<AIModelStatistics>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(expectedException);

            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            var healthCheck = new AIMetricsHealthCheck(_exporterMock.Object, _logger, optionsMock.Object);

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.False(result.IsHealthy);
            Assert.Equal("Failed", result.Status);
            Assert.Contains("Export failed", result.Description);
            Assert.Equal(0.0, result.HealthScore);
            Assert.NotNull(result.Exception);
            Assert.Equal(expectedException, result.Exception);
            Assert.Contains("Export failed", result.Errors[0]);
            Assert.True(result.Duration > TimeSpan.Zero);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Pass_CancellationToken_To_Exporter()
        {
            // Arrange
            var cancellationToken = new CancellationToken(true);
            _exporterMock.Setup(e => e.ExportMetricsAsync(It.IsAny<AIModelStatistics>(), cancellationToken))
                        .Returns(ValueTask.CompletedTask);

            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            var healthCheck = new AIMetricsHealthCheck(_exporterMock.Object, _logger, optionsMock.Object);

            // Act
            await healthCheck.CheckHealthAsync(cancellationToken);

            // Assert
            _exporterMock.Verify(e => e.ExportMetricsAsync(It.IsAny<AIModelStatistics>(), cancellationToken), Times.Once);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Use_Default_CancellationToken_When_None_Provided()
        {
            // Arrange
            _exporterMock.Setup(e => e.ExportMetricsAsync(It.IsAny<AIModelStatistics>(), CancellationToken.None))
                        .Returns(ValueTask.CompletedTask);

            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            var healthCheck = new AIMetricsHealthCheck(_exporterMock.Object, _logger, optionsMock.Object);

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.True(result.IsHealthy);
            _exporterMock.Verify(e => e.ExportMetricsAsync(It.IsAny<AIModelStatistics>(), CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Handle_Generic_Exception()
        {
            // Arrange
            _exporterMock.Setup(e => e.ExportMetricsAsync(It.IsAny<AIModelStatistics>(), It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Generic error"));

            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            var healthCheck = new AIMetricsHealthCheck(_exporterMock.Object, _logger, optionsMock.Object);

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.False(result.IsHealthy);
            Assert.Equal("Failed", result.Status);
            Assert.Contains("Generic error", result.Description);
            Assert.Contains("Generic error", result.Errors[0]);
        }

        #endregion
    }
}