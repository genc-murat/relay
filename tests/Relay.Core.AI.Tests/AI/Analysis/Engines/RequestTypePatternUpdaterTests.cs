using System;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.Engines;
using Relay.Core.AI.Optimization.Data;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines
{
    public class RequestTypePatternUpdaterTests
    {
        private readonly Mock<ILogger<RequestTypePatternUpdater>> _loggerMock;
        private readonly PatternRecognitionConfig _config;
        private readonly RequestTypePatternUpdater _updater;

        public RequestTypePatternUpdaterTests()
        {
            _loggerMock = new Mock<ILogger<RequestTypePatternUpdater>>();
            _config = new PatternRecognitionConfig
            {
                WeightUpdateAlpha = 0.3
            };
            _updater = new RequestTypePatternUpdater(_loggerMock.Object, _config);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new RequestTypePatternUpdater(null!, _config));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Config_Is_Null()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new RequestTypePatternUpdater(_loggerMock.Object, null!));
        }

        [Fact]
        public void UpdatePatterns_Should_Update_Single_Request_Type()
        {
            // Arrange
            var predictions = new[]
            {
                CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(50)),
                CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(30)),
                CreatePredictionResult(typeof(string), TimeSpan.Zero)
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _updater.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(1, result);
            
            // Verify debug logging for the request type
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Updated pattern for String")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            // Verify info logging for count
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Updated patterns for 1 request types")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void UpdatePatterns_Should_Update_Multiple_Request_Types()
        {
            // Arrange
            var predictions = new[]
            {
                CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(50)),
                CreatePredictionResult(typeof(int), TimeSpan.FromMilliseconds(30)),
                CreatePredictionResult(typeof(string), TimeSpan.Zero),
                CreatePredictionResult(typeof(int), TimeSpan.FromMilliseconds(40))
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _updater.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(2, result);
            
            // Verify debug logging for both request types
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Updated pattern for String")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Updated pattern for Int32")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            // Verify info logging for count
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Updated patterns for 2 request types")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void UpdatePatterns_Should_Calculate_Correct_Success_Rate()
        {
            // Arrange
            var predictions = new[]
            {
                CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(50)),
                CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(30)),
                CreatePredictionResult(typeof(string), TimeSpan.Zero),
                CreatePredictionResult(typeof(string), TimeSpan.Zero)
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _updater.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(1, result);
            
            // Verify success rate is 50%
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("50.00 %")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void UpdatePatterns_Should_Calculate_Correct_Average_Improvement()
        {
            // Arrange
            var predictions = new[]
            {
                CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(50)),
                CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(30)),
                CreatePredictionResult(typeof(string), TimeSpan.Zero)
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _updater.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(1, result);
            
            // Verify average improvement is 40ms (only successful predictions)
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("40")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void UpdatePatterns_Should_Calculate_New_Pattern_Weight_Correctly()
        {
            // Arrange
            _config.WeightUpdateAlpha = 0.5;
            var predictions = new[]
            {
                CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(50)),
                CreatePredictionResult(typeof(string), TimeSpan.Zero)
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _updater.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(1, result);
            
            // Verify weight calculation: currentWeight * (1 - alpha) + successRate * alpha
            // 1.0 * (1 - 0.5) + 0.5 * 0.5 = 0.5 + 0.25 = 0.75
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Weight=0.75")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void UpdatePatterns_Should_Handle_All_Successful_Predictions()
        {
            // Arrange
            var predictions = new[]
            {
                CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(50)),
                CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(30)),
                CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(40))
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _updater.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(1, result);
            
            // Verify success rate is 100%
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("100.00 %")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void UpdatePatterns_Should_Handle_All_Failed_Predictions()
        {
            // Arrange
            var predictions = new[]
            {
                CreatePredictionResult(typeof(string), TimeSpan.Zero),
                CreatePredictionResult(typeof(string), TimeSpan.Zero),
                CreatePredictionResult(typeof(string), TimeSpan.Zero)
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _updater.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(1, result);
            
            // Verify success rate is 0% and average improvement is 0
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("0.00 %") && v.ToString()!.Contains("0ms")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void UpdatePatterns_Should_Handle_Empty_Predictions()
        {
            // Arrange
            var predictions = Array.Empty<PredictionResult>();
            var analysis = new PatternAnalysisResult();

            // Act
            var result = _updater.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void UpdatePatterns_Should_Handle_Exception_And_Return_Zero()
        {
            // Arrange
            var predictions = new PredictionResult[] { null! };
            var analysis = new PatternAnalysisResult();

            // Act
            var result = _updater.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(0, result);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void UpdatePatterns_Should_Handle_Zero_Weight_Update_Alpha()
        {
            // Arrange
            _config.WeightUpdateAlpha = 0.0;
            var predictions = new[]
            {
                CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(50)),
                CreatePredictionResult(typeof(string), TimeSpan.Zero)
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _updater.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(1, result);
            
            // Verify weight calculation: currentWeight * (1 - 0) + successRate * 0 = 1.0
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Weight=1.00")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public void UpdatePatterns_Should_Handle_Maximum_Weight_Update_Alpha()
        {
            // Arrange
            _config.WeightUpdateAlpha = 1.0;
            var predictions = new[]
            {
                CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(50)),
                CreatePredictionResult(typeof(string), TimeSpan.Zero)
            };

            var analysis = new PatternAnalysisResult();

            // Act
            var result = _updater.UpdatePatterns(predictions, analysis);

            // Assert
            Assert.Equal(1, result);
            
            // Verify weight calculation: currentWeight * (1 - 1) + successRate * 1 = successRate
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Weight=0.50")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        private static PredictionResult CreatePredictionResult(Type requestType, TimeSpan actualImprovement)
        {
            return new PredictionResult
            {
                RequestType = requestType,
                PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                ActualImprovement = actualImprovement,
                Timestamp = DateTime.UtcNow,
                Metrics = new RequestExecutionMetrics
                {
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                    MemoryUsage = 1024,
                    CpuUsage = 50,
                    TotalExecutions = 1,
                    SuccessfulExecutions = 1,
                    FailedExecutions = 0
                }
            };
        }
    }
}