using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIOptimizationEngineInsightsTests : IDisposable
    {
        private readonly Mock<ILogger<AIOptimizationEngine>> _loggerMock;
        private readonly AIOptimizationOptions _options;
        private readonly AIOptimizationEngine _engine;

        public AIOptimizationEngineInsightsTests()
        {
            _loggerMock = new Mock<ILogger<AIOptimizationEngine>>();
            _options = new AIOptimizationOptions
            {
                DefaultBatchSize = 10,
                MaxBatchSize = 100,
                ModelUpdateInterval = TimeSpan.FromMinutes(5),
                ModelTrainingDate = DateTime.UtcNow,
                ModelVersion = "1.0.0",
                LastRetrainingDate = DateTime.UtcNow.AddDays(-1)
            };

            var optionsMock = new Mock<IOptions<AIOptimizationOptions>>();
            optionsMock.Setup(o => o.Value).Returns(_options);

            _engine = new AIOptimizationEngine(_loggerMock.Object, optionsMock.Object);
        }

        public void Dispose()
        {
            _engine?.Dispose();
        }

        [Fact]
        public async Task GetSystemInsightsAsync_Should_Throw_When_Disposed()
        {
            // Arrange
            _engine.Dispose();
            var timeWindow = TimeSpan.FromHours(1);

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                await _engine.GetSystemInsightsAsync(timeWindow));
        }

        [Fact]
        public async Task GetSystemInsightsAsync_Should_Return_Insights()
        {
            // Arrange
            var timeWindow = TimeSpan.FromHours(1);

            // Act
            var insights = await _engine.GetSystemInsightsAsync(timeWindow);

            // Assert
            Assert.NotNull(insights);
            Assert.Equal(timeWindow, insights.AnalysisPeriod);
            Assert.True(insights.HealthScore.Overall >= 0 && insights.HealthScore.Overall <= 1);
        }

        [Fact]
        public async Task GetSystemInsightsAsync_Should_Handle_Zero_Time_Window()
        {
            // Arrange
            var timeWindow = TimeSpan.Zero;

            // Act
            var insights = await _engine.GetSystemInsightsAsync(timeWindow);

            // Assert
            Assert.NotNull(insights);
            Assert.Equal(timeWindow, insights.AnalysisPeriod);
        }

        [Fact]
        public async Task GetSystemInsightsAsync_Should_Handle_Large_Time_Window()
        {
            // Arrange
            var timeWindow = TimeSpan.FromDays(365);

            // Act
            var insights = await _engine.GetSystemInsightsAsync(timeWindow);

            // Assert
            Assert.NotNull(insights);
            Assert.Equal(timeWindow, insights.AnalysisPeriod);
        }

        [Fact]
        public async Task GetSystemInsightsAsync_Should_Handle_Negative_Time_Window()
        {
            // Arrange
            var timeWindow = TimeSpan.FromHours(-1);

            // Act
            var insights = await _engine.GetSystemInsightsAsync(timeWindow);

            // Assert
            Assert.NotNull(insights);
        }

        [Fact]
        public async Task GetSystemInsightsAsync_Should_Provide_Consistent_Results()
        {
            // Arrange
            var timeWindow = TimeSpan.FromHours(1);

            // Act
            var insights1 = await _engine.GetSystemInsightsAsync(timeWindow);
            var insights2 = await _engine.GetSystemInsightsAsync(timeWindow);

            // Assert
            Assert.NotNull(insights1);
            Assert.NotNull(insights2);
            Assert.Equal(insights1.AnalysisPeriod, insights2.AnalysisPeriod);
        }

        [Fact]
        public async Task GetSystemInsightsAsync_Should_Include_All_Required_Components()
        {
            // Arrange
            var timeWindow = TimeSpan.FromHours(2);

            // Act
            var insights = await _engine.GetSystemInsightsAsync(timeWindow);

            // Assert
            Assert.NotNull(insights);
            Assert.NotNull(insights.Bottlenecks);
            Assert.NotNull(insights.Opportunities);
            Assert.NotNull(insights.Predictions);
            Assert.NotNull(insights.KeyMetrics);
            Assert.True(insights.HealthScore.Overall >= 0 && insights.HealthScore.Overall <= 1);
            Assert.True(insights.PerformanceGrade >= 'A' && insights.PerformanceGrade <= 'F');
        }
    }
}