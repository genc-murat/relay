using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.Engines;
using System;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.Engines;

public class DefaultPatternAnalyzerTests
{
    private readonly Mock<ILogger<DefaultPatternAnalyzer>> _loggerMock;
    private readonly PatternRecognitionConfig _config;
    private readonly DefaultPatternAnalyzer _analyzer;

    public DefaultPatternAnalyzerTests()
    {
        _loggerMock = new Mock<ILogger<DefaultPatternAnalyzer>>();
        _config = new PatternRecognitionConfig
        {
            ImprovementThresholds = new ImprovementThresholds
            {
                LowImpact = 10,
                HighImpact = 100
            },
            TopRequestTypesCount = 3
        };
        _analyzer = new DefaultPatternAnalyzer(_loggerMock.Object, _config);
    }

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DefaultPatternAnalyzer(null!, _config));
    }

    [Fact]
    public void Constructor_Should_Throw_When_Config_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new DefaultPatternAnalyzer(_loggerMock.Object, null!));
    }

    [Fact]
    public void AnalyzePatterns_Should_Calculate_Correct_Metrics_For_All_Successful_Predictions()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(typeof(int), TimeSpan.FromMilliseconds(150)),
            CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(20))
        };

        // Act
        var result = _analyzer.AnalyzePatterns(predictions);

        // Assert
        Assert.Equal(3, result.TotalPredictions);
        Assert.Equal(3, result.SuccessfulPredictions.Length);
        Assert.Empty(result.FailedPredictions);
        Assert.Equal(1.0, result.OverallAccuracy);
        Assert.Equal(1.0, result.SuccessRate);
        Assert.Equal(0.0, result.FailureRate);
        Assert.Equal(1, result.HighImpactSuccesses);
        Assert.Equal(0, result.LowImpactSuccesses);
        Assert.Equal(2, result.MediumImpactSuccesses);
        Assert.Equal(73.33333333333333, result.AverageImprovement);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Pattern analysis")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void AnalyzePatterns_Should_Calculate_Correct_Metrics_For_Mixed_Predictions()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(typeof(int), TimeSpan.Zero),
            CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(150)),
            CreatePredictionResult(typeof(double), TimeSpan.Zero)
        };

        // Act
        var result = _analyzer.AnalyzePatterns(predictions);

        // Assert
        Assert.Equal(4, result.TotalPredictions);
        Assert.Equal(2, result.SuccessfulPredictions.Length);
        Assert.Equal(2, result.FailedPredictions.Length);
        Assert.Equal(0.5, result.OverallAccuracy);
        Assert.Equal(0.5, result.SuccessRate);
        Assert.Equal(0.5, result.FailureRate);
        Assert.Equal(1, result.HighImpactSuccesses);
        Assert.Equal(0, result.LowImpactSuccesses);
        Assert.Equal(1, result.MediumImpactSuccesses);
        Assert.Equal(100.0, result.AverageImprovement);
    }

    [Fact]
    public void AnalyzePatterns_Should_Calculate_Correct_Metrics_For_All_Failed_Predictions()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(typeof(string), TimeSpan.Zero),
            CreatePredictionResult(typeof(int), TimeSpan.Zero),
            CreatePredictionResult(typeof(string), TimeSpan.Zero)
        };

        // Act
        var result = _analyzer.AnalyzePatterns(predictions);

        // Assert
        Assert.Equal(3, result.TotalPredictions);
        Assert.Empty(result.SuccessfulPredictions);
        Assert.Equal(3, result.FailedPredictions.Length);
        Assert.Equal(0.0, result.OverallAccuracy);
        Assert.Equal(0.0, result.SuccessRate);
        Assert.Equal(1.0, result.FailureRate);
        Assert.Equal(0, result.HighImpactSuccesses);
        Assert.Equal(0, result.MediumImpactSuccesses);
        Assert.Equal(0, result.LowImpactSuccesses);
        Assert.Equal(0, result.AverageImprovement);
    }

    [Fact]
    public void AnalyzePatterns_Should_Handle_Negative_Improvements()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(-10)),
            CreatePredictionResult(typeof(int), TimeSpan.FromMilliseconds(-5)),
            CreatePredictionResult(typeof(double), TimeSpan.FromMilliseconds(-20))
        };

        // Act
        var result = _analyzer.AnalyzePatterns(predictions);

        // Assert
        Assert.Equal(3, result.TotalPredictions);
        Assert.Empty(result.SuccessfulPredictions);
        Assert.Equal(3, result.FailedPredictions.Length);
        Assert.Equal(0.0, result.OverallAccuracy);
        Assert.Equal(0.0, result.SuccessRate);
        Assert.Equal(1.0, result.FailureRate);
        Assert.Equal(0, result.HighImpactSuccesses);
        Assert.Equal(0, result.MediumImpactSuccesses);
        Assert.Equal(0, result.LowImpactSuccesses);
        Assert.Equal(0, result.AverageImprovement);
    }

    [Fact]
    public void AnalyzePatterns_Should_Handle_Null_RequestType()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(null, TimeSpan.FromMilliseconds(100)),
            CreatePredictionResult(null, TimeSpan.Zero),
            CreatePredictionResult(typeof(int), TimeSpan.FromMilliseconds(20))
        };

        // Act
        var result = _analyzer.AnalyzePatterns(predictions);

        // Assert
        Assert.Equal(4, result.TotalPredictions);
        Assert.Equal(3, result.SuccessfulPredictions.Length);
        Assert.Equal(1, result.FailedPredictions.Length);
        Assert.Equal(0.75, result.OverallAccuracy);
        Assert.Equal(0, result.HighImpactSuccesses);
        Assert.Equal(3, result.MediumImpactSuccesses);
        Assert.Equal(0, result.LowImpactSuccesses);
        Assert.Equal(56.666666666666664, result.AverageImprovement);
        Assert.Contains(typeof(string), result.BestRequestTypes);
        Assert.Contains(typeof(int), result.BestRequestTypes);
        Assert.Contains(null, result.BestRequestTypes);
    }

    [Fact]
    public void AnalyzePatterns_Should_Identify_Best_And_Worst_Request_Types()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(typeof(string), TimeSpan.Zero),
            CreatePredictionResult(typeof(int), TimeSpan.FromMilliseconds(150)),
            CreatePredictionResult(typeof(int), TimeSpan.FromMilliseconds(120)),
            CreatePredictionResult(typeof(double), TimeSpan.Zero),
            CreatePredictionResult(typeof(double), TimeSpan.Zero),
            CreatePredictionResult(typeof(bool), TimeSpan.FromMilliseconds(20))
        };

        // Act
        var result = _analyzer.AnalyzePatterns(predictions);

        // Assert
        Assert.Equal(3, result.BestRequestTypes.Length);
        Assert.Contains(typeof(int), result.BestRequestTypes);
        Assert.Contains(typeof(bool), result.BestRequestTypes);
        Assert.Contains(typeof(string), result.BestRequestTypes);

        Assert.Equal(3, result.WorstRequestTypes.Length);
        Assert.Contains(typeof(double), result.WorstRequestTypes);
    }

    [Fact]
    public void AnalyzePatterns_Should_Handle_Empty_Predictions()
    {
        // Arrange
        var predictions = Array.Empty<PredictionResult>();

        // Act
        var result = _analyzer.AnalyzePatterns(predictions);

        // Assert
        Assert.Equal(0, result.TotalPredictions);
        Assert.Empty(result.SuccessfulPredictions);
        Assert.Empty(result.FailedPredictions);
        Assert.True(double.IsNaN(result.OverallAccuracy));
        Assert.True(double.IsNaN(result.SuccessRate));
        Assert.True(double.IsNaN(result.FailureRate));
        Assert.Equal(0, result.HighImpactSuccesses);
        Assert.Equal(0, result.MediumImpactSuccesses);
        Assert.Equal(0, result.LowImpactSuccesses);
        Assert.Equal(0, result.AverageImprovement);
        Assert.Empty(result.BestRequestTypes);
        Assert.Empty(result.WorstRequestTypes);
    }

    [Fact]
    public void AnalyzePatterns_Should_Classify_Medium_Impact_Successes()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(typeof(int), TimeSpan.FromMilliseconds(75)),
            CreatePredictionResult(typeof(double), TimeSpan.FromMilliseconds(25))
        };

        // Act
        var result = _analyzer.AnalyzePatterns(predictions);

        // Assert
        Assert.Equal(3, result.TotalPredictions);
        Assert.Equal(3, result.SuccessfulPredictions.Length);
        Assert.Equal(0, result.HighImpactSuccesses);
        Assert.Equal(3, result.MediumImpactSuccesses);
        Assert.Equal(0, result.LowImpactSuccesses);
    }

    [Fact]
    public void AnalyzePatterns_Should_Set_Analysis_Timestamp()
    {
        // Arrange
        var predictions = new[]
        {
            CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(50))
        };
        var before = DateTime.UtcNow;

        // Act
        var result = _analyzer.AnalyzePatterns(predictions);

        // Assert
        Assert.True(result.AnalysisTimestamp >= before);
        Assert.True(result.AnalysisTimestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void AnalyzePatterns_Should_Handle_Exception_And_Return_Default_Result()
    {
        // Arrange
        var predictions = new PredictionResult[] { null! };

        // Act
        var result = _analyzer.AnalyzePatterns(predictions);

        // Assert - when exception occurs, should return default result with TotalPredictions set
        Assert.Equal(1, result.TotalPredictions);
        Assert.Empty(result.SuccessfulPredictions);
        Assert.Empty(result.FailedPredictions);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error analyzing prediction patterns")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void AnalyzePatterns_Should_Respect_TopRequestTypesCount_Configuration()
    {
        // Arrange
        _config.TopRequestTypesCount = 2;
        var predictions = new[]
        {
            CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(typeof(int), TimeSpan.FromMilliseconds(150)),
            CreatePredictionResult(typeof(double), TimeSpan.FromMilliseconds(120)),
            CreatePredictionResult(typeof(bool), TimeSpan.FromMilliseconds(20))
        };

        // Act
        var result = _analyzer.AnalyzePatterns(predictions);

        // Assert
        Assert.Equal(2, result.BestRequestTypes.Length);
        Assert.Equal(2, result.WorstRequestTypes.Length);
    }

    [Fact]
    public void AnalyzePatterns_Should_Handle_TopRequestTypesCount_Zero()
    {
        // Arrange
        _config.TopRequestTypesCount = 0;
        var predictions = new[]
        {
            CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(typeof(int), TimeSpan.FromMilliseconds(150)),
            CreatePredictionResult(typeof(double), TimeSpan.Zero)
        };

        // Act
        var result = _analyzer.AnalyzePatterns(predictions);

        // Assert
        Assert.Empty(result.BestRequestTypes);
        Assert.Empty(result.WorstRequestTypes);
    }

    [Fact]
    public void AnalyzePatterns_Should_Handle_TopRequestTypesCount_Greater_Than_Unique_Types()
    {
        // Arrange
        _config.TopRequestTypesCount = 5;
        var predictions = new[]
        {
            CreatePredictionResult(typeof(string), TimeSpan.FromMilliseconds(50)),
            CreatePredictionResult(typeof(int), TimeSpan.FromMilliseconds(150)),
            CreatePredictionResult(typeof(string), TimeSpan.Zero)
        };

        // Act
        var result = _analyzer.AnalyzePatterns(predictions);

        // Assert
        Assert.Equal(2, result.BestRequestTypes.Length); // only 2 unique types
        Assert.Equal(2, result.WorstRequestTypes.Length);
        Assert.Contains(typeof(int), result.BestRequestTypes); // int has higher success rate
        Assert.Contains(typeof(string), result.WorstRequestTypes); // string has lower
    }

    private static PredictionResult CreatePredictionResult(Type? requestType, TimeSpan actualImprovement)
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