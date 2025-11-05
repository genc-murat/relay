using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI.Extensions
{
    public class AIModelHealthCheckTests
    {
        private readonly Mock<IAIOptimizationEngine> _engineMock;
        private readonly ILogger<AIModelHealthCheck> _logger;
        private readonly AIHealthCheckOptions _options;

        public AIModelHealthCheckTests()
        {
            _engineMock = new Mock<IAIOptimizationEngine>();
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<AIModelHealthCheck>();
            _options = new AIHealthCheckOptions
            {
                MinAccuracyScore = 0.7,
                MinF1Score = 0.65,
                MinConfidence = 0.6,
                MaxPredictionTimeMs = 100.0,
                MaxDaysSinceRetraining = 30
            };
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_Throw_When_Engine_Is_Null()
        {
            // Arrange
            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AIModelHealthCheck(null!, _logger, optionsMock.Object));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Arrange
            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AIModelHealthCheck(_engineMock.Object, null!, optionsMock.Object));
        }

        [Fact]
        public void Constructor_Should_Use_Default_Options_When_Options_Is_Null()
        {
            // Arrange
            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns((AIHealthCheckOptions)null!);

            // Act
            var healthCheck = new AIModelHealthCheck(_engineMock.Object, _logger, optionsMock.Object);

            // Assert - should not throw, uses default options
            Assert.NotNull(healthCheck);
        }

        #endregion

        #region CheckHealthAsync Tests

        [Fact]
        public async Task CheckHealthAsync_Should_Return_Healthy_Result_For_Good_Statistics()
        {
            // Arrange
            var stats = CreateGoodModelStatistics();
            _engineMock.Setup(e => e.GetModelStatistics()).Returns(stats);

            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            var healthCheck = new AIModelHealthCheck(_engineMock.Object, _logger, optionsMock.Object);

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.True(result.IsHealthy);
            Assert.Equal("Healthy", result.Status);
            Assert.Equal("AI Model", result.ComponentName);
            Assert.True(result.HealthScore > 0);
            Assert.Contains("Accuracy=", result.Description);
            Assert.Contains("F1=", result.Description);
            Assert.True(result.Duration > TimeSpan.Zero);
            Assert.Equal(stats.AccuracyScore, result.Data["Accuracy"]);
            Assert.Equal(stats.F1Score, result.Data["F1Score"]);
            Assert.Equal(stats.TotalPredictions, result.Data["TotalPredictions"]);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Return_Degraded_Result_For_Low_Accuracy()
        {
            // Arrange
            var stats = new AIModelStatistics
            {
                ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
                TotalPredictions = 10000,
                AccuracyScore = 0.5, // Below threshold
                PrecisionScore = 0.82,
                RecallScore = 0.88,
                F1Score = 0.85,
                AveragePredictionTime = TimeSpan.FromMilliseconds(50),
                TrainingDataPoints = 50000,
                ModelVersion = "1.2.3",
                LastRetraining = DateTime.UtcNow.AddDays(-7),
                ModelConfidence = 0.9
            };
            _engineMock.Setup(e => e.GetModelStatistics()).Returns(stats);

            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            var healthCheck = new AIModelHealthCheck(_engineMock.Object, _logger, optionsMock.Object);

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.False(result.IsHealthy);
            Assert.Equal("Degraded", result.Status);
            Assert.Contains("below threshold", result.Warnings[0]);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Return_Degraded_Result_For_Low_F1_Score()
        {
            // Arrange
            var stats = new AIModelStatistics
            {
                ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
                TotalPredictions = 10000,
                AccuracyScore = 0.85,
                PrecisionScore = 0.82,
                RecallScore = 0.88,
                F1Score = 0.5, // Below threshold
                AveragePredictionTime = TimeSpan.FromMilliseconds(50),
                TrainingDataPoints = 50000,
                ModelVersion = "1.2.3",
                LastRetraining = DateTime.UtcNow.AddDays(-7),
                ModelConfidence = 0.9
            };
            _engineMock.Setup(e => e.GetModelStatistics()).Returns(stats);

            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            var healthCheck = new AIModelHealthCheck(_engineMock.Object, _logger, optionsMock.Object);

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.False(result.IsHealthy);
            Assert.Equal("Degraded", result.Status);
            Assert.Contains("F1 Score", result.Warnings[0]);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Add_Warning_For_Low_Confidence()
        {
            // Arrange
            var stats = new AIModelStatistics
            {
                ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
                TotalPredictions = 10000,
                AccuracyScore = 0.85,
                PrecisionScore = 0.82,
                RecallScore = 0.88,
                F1Score = 0.85,
                AveragePredictionTime = TimeSpan.FromMilliseconds(50),
                TrainingDataPoints = 50000,
                ModelVersion = "1.2.3",
                LastRetraining = DateTime.UtcNow.AddDays(-7),
                ModelConfidence = 0.4 // Below threshold
            };
            _engineMock.Setup(e => e.GetModelStatistics()).Returns(stats);

            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            var healthCheck = new AIModelHealthCheck(_engineMock.Object, _logger, optionsMock.Object);

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.True(result.IsHealthy); // Still healthy, just warning
            Assert.Contains("confidence", result.Warnings[0]);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Add_Warning_For_Slow_Predictions()
        {
            // Arrange
            var stats = new AIModelStatistics
            {
                ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
                TotalPredictions = 10000,
                AccuracyScore = 0.85,
                PrecisionScore = 0.82,
                RecallScore = 0.88,
                F1Score = 0.85,
                AveragePredictionTime = TimeSpan.FromMilliseconds(150), // Above threshold
                TrainingDataPoints = 50000,
                ModelVersion = "1.2.3",
                LastRetraining = DateTime.UtcNow.AddDays(-7),
                ModelConfidence = 0.9
            };
            _engineMock.Setup(e => e.GetModelStatistics()).Returns(stats);

            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            var healthCheck = new AIModelHealthCheck(_engineMock.Object, _logger, optionsMock.Object);

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.True(result.IsHealthy); // Still healthy, just warning
            Assert.Contains("prediction time", result.Warnings[0]);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Add_Warning_For_Stale_Model()
        {
            // Arrange
            var stats = new AIModelStatistics
            {
                ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
                TotalPredictions = 10000,
                AccuracyScore = 0.85,
                PrecisionScore = 0.82,
                RecallScore = 0.88,
                F1Score = 0.85,
                AveragePredictionTime = TimeSpan.FromMilliseconds(50),
                TrainingDataPoints = 50000,
                ModelVersion = "1.2.3",
                LastRetraining = DateTime.UtcNow.AddDays(-40), // Stale
                ModelConfidence = 0.9
            };
            _engineMock.Setup(e => e.GetModelStatistics()).Returns(stats);

            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            var healthCheck = new AIModelHealthCheck(_engineMock.Object, _logger, optionsMock.Object);

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.True(result.IsHealthy); // Still healthy, just warning
            Assert.Contains("retrained", result.Warnings[0]);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Return_Failed_Result_On_Exception()
        {
            // Arrange
            _engineMock.Setup(e => e.GetModelStatistics()).Throws(new InvalidOperationException("Test exception"));

            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            var healthCheck = new AIModelHealthCheck(_engineMock.Object, _logger, optionsMock.Object);

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.False(result.IsHealthy);
            Assert.Equal("Failed", result.Status);
            Assert.Contains("Test exception", result.Description);
            Assert.NotNull(result.Exception);
            Assert.Contains("Test exception", result.Errors[0]);
            Assert.True(result.Duration > TimeSpan.Zero);
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Calculate_Health_Score_Correctly()
        {
            // Arrange
            var stats = new AIModelStatistics
            {
                AccuracyScore = 0.8, // 0.30 * 0.8 = 0.24
                PrecisionScore = 0.75, // 0.20 * 0.75 = 0.15
                RecallScore = 0.85, // 0.20 * 0.85 = 0.17
                F1Score = 0.8, // 0.20 * 0.8 = 0.16
                ModelConfidence = 0.9, // 0.10 * 0.9 = 0.09
                AveragePredictionTime = TimeSpan.FromMilliseconds(50), // No penalty
                LastRetraining = DateTime.UtcNow.AddDays(-10) // No penalty
            };
            _engineMock.Setup(e => e.GetModelStatistics()).Returns(stats);

            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            var healthCheck = new AIModelHealthCheck(_engineMock.Object, _logger, optionsMock.Object);

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.Equal(0.81, result.HealthScore, 2); // 0.24 + 0.15 + 0.17 + 0.16 + 0.09
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Apply_Penalty_For_Slow_Predictions_In_Score()
        {
            // Arrange
            var stats = new AIModelStatistics
            {
                ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
                TotalPredictions = 10000,
                AccuracyScore = 0.85,
                PrecisionScore = 0.82,
                RecallScore = 0.88,
                F1Score = 0.85,
                AveragePredictionTime = TimeSpan.FromMilliseconds(150), // Above threshold
                TrainingDataPoints = 50000,
                ModelVersion = "1.2.3",
                LastRetraining = DateTime.UtcNow.AddDays(-7),
                ModelConfidence = 0.9
            };
            _engineMock.Setup(e => e.GetModelStatistics()).Returns(stats);

            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            var healthCheck = new AIModelHealthCheck(_engineMock.Object, _logger, optionsMock.Object);

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.True(result.HealthScore < 0.9); // Should be penalized (original ~0.9 * 0.9 = ~0.81)
        }

        [Fact]
        public async Task CheckHealthAsync_Should_Apply_Penalty_For_Stale_Model_In_Score()
        {
            // Arrange
            var stats = new AIModelStatistics
            {
                ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
                TotalPredictions = 10000,
                AccuracyScore = 0.85,
                PrecisionScore = 0.82,
                RecallScore = 0.88,
                F1Score = 0.85,
                AveragePredictionTime = TimeSpan.FromMilliseconds(50),
                TrainingDataPoints = 50000,
                ModelVersion = "1.2.3",
                LastRetraining = DateTime.UtcNow.AddDays(-40), // Stale
                ModelConfidence = 0.9
            };
            _engineMock.Setup(e => e.GetModelStatistics()).Returns(stats);

            var optionsMock = new Mock<IOptions<AIHealthCheckOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            var healthCheck = new AIModelHealthCheck(_engineMock.Object, _logger, optionsMock.Object);

            // Act
            var result = await healthCheck.CheckHealthAsync();

            // Assert
            Assert.True(result.HealthScore < 0.85); // Should be penalized (original ~0.9 * 0.95 = ~0.855)
        }

        #endregion

        #region Helper Methods

        private static AIModelStatistics CreateGoodModelStatistics()
        {
            return new AIModelStatistics
            {
                ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
                TotalPredictions = 10000,
                AccuracyScore = 0.85,
                PrecisionScore = 0.82,
                RecallScore = 0.88,
                F1Score = 0.85,
                AveragePredictionTime = TimeSpan.FromMilliseconds(50),
                TrainingDataPoints = 50000,
                ModelVersion = "1.2.3",
                LastRetraining = DateTime.UtcNow.AddDays(-7),
                ModelConfidence = 0.9
            };
        }

        #endregion
    }
}