using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Moq;
using Relay.Core.AI.Analysis.TimeSeries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.TimeSeries.Managers;

public class ForecastingModelManagerTests
{
    private readonly Mock<ILogger<ForecastingModelManager>> _loggerMock;
    private readonly ForecastingModelManager _manager;

    public ForecastingModelManagerTests()
    {
        _loggerMock = new Mock<ILogger<ForecastingModelManager>>();
        _manager = new ForecastingModelManager(_loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ForecastingModelManager(null!));
    }

    #endregion

    #region StoreModel Tests

    [Fact]
    public void StoreModel_Should_Throw_When_MetricName_Is_Null()
    {
        // Arrange
        var model = Mock.Of<ITransformer>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _manager.StoreModel(null!, model, ForecastingMethod.SSA));
        Assert.Contains("Metric name cannot be null, empty, or whitespace", exception.Message);
    }

    [Fact]
    public void StoreModel_Should_Throw_When_MetricName_Is_Empty()
    {
        // Arrange
        var model = Mock.Of<ITransformer>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _manager.StoreModel(string.Empty, model, ForecastingMethod.SSA));
        Assert.Contains("Metric name cannot be null, empty, or whitespace", exception.Message);
    }

    [Fact]
    public void StoreModel_Should_Throw_When_MetricName_Is_Whitespace()
    {
        // Arrange
        var model = Mock.Of<ITransformer>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _manager.StoreModel("   ", model, ForecastingMethod.SSA));
        Assert.Contains("Metric name cannot be null, empty, or whitespace", exception.Message);
    }

    [Fact]
    public void StoreModel_Should_Throw_When_Method_Is_Invalid()
    {
        // Arrange
        var model = Mock.Of<ITransformer>();
        var invalidMethod = (ForecastingMethod)999;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _manager.StoreModel("test.metric", model, invalidMethod));
        Assert.Contains("Invalid forecasting method", exception.Message);
    }

    [Fact]
    public void StoreModel_Should_Throw_When_Model_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _manager.StoreModel("test.metric", null!, ForecastingMethod.SSA));
    }

    [Fact]
    public void StoreModel_Should_Store_Model_And_Method()
    {
        // Arrange
        var metricName = "test.metric";
        var model = Mock.Of<ITransformer>();
        var method = ForecastingMethod.ExponentialSmoothing;

        // Act
        _manager.StoreModel(metricName, model, method);

        // Assert
        Assert.True(_manager.HasModel(metricName));
        var retrievedModel = _manager.GetModel(metricName);
        Assert.Equal(model, retrievedModel);
    }

    [Fact]
    public void StoreModel_Should_Overwrite_Existing_Model()
    {
        // Arrange
        var metricName = "test.metric";
        var oldModel = Mock.Of<ITransformer>();
        var newModel = Mock.Of<ITransformer>();
        var oldMethod = ForecastingMethod.SSA;
        var newMethod = ForecastingMethod.ExponentialSmoothing;

        // Act
        _manager.StoreModel(metricName, oldModel, oldMethod);
        _manager.StoreModel(metricName, newModel, newMethod);

        // Assert
        var retrievedModel = _manager.GetModel(metricName);
        Assert.Equal(newModel, retrievedModel);
    }

    [Fact]
    public void StoreModel_Should_Log_Debug_Message()
    {
        // Arrange
        var metricName = "test.metric";
        var model = Mock.Of<ITransformer>();
        var method = ForecastingMethod.MovingAverage;

        // Act
        _manager.StoreModel(metricName, model, method);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Stored") && o.ToString()!.Contains("MovingAverage") && o.ToString()!.Contains(metricName)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetModel Tests

    [Fact]
    public void GetModel_Should_Throw_When_MetricName_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _manager.GetModel(null!));
        Assert.Contains("Metric name cannot be null, empty, or whitespace", exception.Message);
    }

    [Fact]
    public void GetModel_Should_Throw_When_MetricName_Is_Empty()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _manager.GetModel(string.Empty));
        Assert.Contains("Metric name cannot be null, empty, or whitespace", exception.Message);
    }

    [Fact]
    public void GetModel_Should_Throw_When_MetricName_Is_Whitespace()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _manager.GetModel("   "));
        Assert.Contains("Metric name cannot be null, empty, or whitespace", exception.Message);
    }

    [Fact]
    public void GetModel_Should_Return_Model_When_Exists()
    {
        // Arrange
        var metricName = "test.metric";
        var model = Mock.Of<ITransformer>();
        _manager.StoreModel(metricName, model, ForecastingMethod.SSA);

        // Act
        var result = _manager.GetModel(metricName);

        // Assert
        Assert.Equal(model, result);
    }

    [Fact]
    public void GetModel_Should_Return_Null_When_Model_Does_Not_Exist()
    {
        // Act
        var result = _manager.GetModel("nonexistent.metric");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region HasModel Tests

    [Fact]
    public void HasModel_Should_Throw_When_MetricName_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _manager.HasModel(null!));
        Assert.Contains("Metric name cannot be null, empty, or whitespace", exception.Message);
    }

    [Fact]
    public void HasModel_Should_Throw_When_MetricName_Is_Empty()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _manager.HasModel(string.Empty));
        Assert.Contains("Metric name cannot be null, empty, or whitespace", exception.Message);
    }

    [Fact]
    public void HasModel_Should_Throw_When_MetricName_Is_Whitespace()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _manager.HasModel("   "));
        Assert.Contains("Metric name cannot be null, empty, or whitespace", exception.Message);
    }

    [Fact]
    public void HasModel_Should_Return_True_When_Model_Exists()
    {
        // Arrange
        var metricName = "test.metric";
        var model = Mock.Of<ITransformer>();
        _manager.StoreModel(metricName, model, ForecastingMethod.SSA);

        // Act
        var result = _manager.HasModel(metricName);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasModel_Should_Return_False_When_Model_Does_Not_Exist()
    {
        // Act
        var result = _manager.HasModel("nonexistent.metric");

        // Assert
        Assert.False(result);
    }

    #endregion

    #region GetAvailableMetrics Tests

    [Fact]
    public void GetAvailableMetrics_Should_Return_Empty_When_No_Models_Stored()
    {
        // Act
        var result = _manager.GetAvailableMetrics();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetAvailableMetrics_Should_Return_All_Stored_Metrics()
    {
        // Arrange
        var metrics = new[] { "metric1", "metric2", "metric3" };
        foreach (var metric in metrics)
        {
            var model = Mock.Of<ITransformer>();
            _manager.StoreModel(metric, model, ForecastingMethod.SSA);
        }

        // Act
        var result = _manager.GetAvailableMetrics().ToList();

        // Assert
        Assert.Equal(metrics.Length, result.Count);
        foreach (var metric in metrics)
        {
            Assert.Contains(metric, result);
        }
    }

    [Fact]
    public void GetAvailableMetrics_Should_Return_New_List_Instance()
    {
        // Arrange
        var model = Mock.Of<ITransformer>();
        _manager.StoreModel("test.metric", model, ForecastingMethod.SSA);

        // Act
        var result1 = _manager.GetAvailableMetrics();
        var result2 = _manager.GetAvailableMetrics();

        // Assert
        Assert.NotSame(result1, result2);
    }

    #endregion

    #region RemoveModel Tests

    [Fact]
    public void RemoveModel_Should_Throw_When_MetricName_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _manager.RemoveModel(null!));
        Assert.Contains("Metric name cannot be null, empty, or whitespace", exception.Message);
    }

    [Fact]
    public void RemoveModel_Should_Throw_When_MetricName_Is_Empty()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _manager.RemoveModel(string.Empty));
        Assert.Contains("Metric name cannot be null, empty, or whitespace", exception.Message);
    }

    [Fact]
    public void RemoveModel_Should_Throw_When_MetricName_Is_Whitespace()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _manager.RemoveModel("   "));
        Assert.Contains("Metric name cannot be null, empty, or whitespace", exception.Message);
    }

    [Fact]
    public void RemoveModel_Should_Remove_Model_And_Method()
    {
        // Arrange
        var metricName = "test.metric";
        var model = Mock.Of<ITransformer>();
        _manager.StoreModel(metricName, model, ForecastingMethod.SSA);

        // Act
        _manager.RemoveModel(metricName);

        // Assert
        Assert.False(_manager.HasModel(metricName));
        Assert.Null(_manager.GetModel(metricName));
    }

    [Fact]
    public void RemoveModel_Should_Not_Throw_When_Model_Does_Not_Exist()
    {
        // Act & Assert (should not throw)
        _manager.RemoveModel("nonexistent.metric");
    }

    [Fact]
    public void RemoveModel_Should_Log_Debug_Message()
    {
        // Arrange
        var metricName = "test.metric";
        var model = Mock.Of<ITransformer>();
        _manager.StoreModel(metricName, model, ForecastingMethod.SSA);

        // Act
        _manager.RemoveModel(metricName);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Removed") && o.ToString()!.Contains(metricName)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetMethod Tests

    [Fact]
    public void GetMethod_Should_Throw_When_MetricName_Is_Null()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _manager.GetMethod(null!));
        Assert.Contains("Metric name cannot be null, empty, or whitespace", exception.Message);
    }

    [Fact]
    public void GetMethod_Should_Throw_When_MetricName_Is_Empty()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _manager.GetMethod(string.Empty));
        Assert.Contains("Metric name cannot be null, empty, or whitespace", exception.Message);
    }

    [Fact]
    public void GetMethod_Should_Throw_When_MetricName_Is_Whitespace()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _manager.GetMethod("   "));
        Assert.Contains("Metric name cannot be null, empty, or whitespace", exception.Message);
    }

    [Fact]
    public void GetMethod_Should_Return_Method_When_Model_Exists()
    {
        // Arrange
        var metricName = "test.metric";
        var model = Mock.Of<ITransformer>();
        var method = ForecastingMethod.ExponentialSmoothing;
        _manager.StoreModel(metricName, model, method);

        // Act
        var result = _manager.GetMethod(metricName);

        // Assert
        Assert.Equal(method, result);
    }

    [Fact]
    public void GetMethod_Should_Return_Null_When_Model_Does_Not_Exist()
    {
        // Act
        var result = _manager.GetMethod("nonexistent.metric");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetModelCount Tests

    [Fact]
    public void GetModelCount_Should_Return_Zero_When_No_Models()
    {
        // Act
        var result = _manager.GetModelCount();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetModelCount_Should_Return_Correct_Count_After_Storing_Models()
    {
        // Arrange
        var models = new[]
        {
            ("metric1", Mock.Of<ITransformer>(), ForecastingMethod.SSA),
            ("metric2", Mock.Of<ITransformer>(), ForecastingMethod.ExponentialSmoothing),
            ("metric3", Mock.Of<ITransformer>(), ForecastingMethod.MovingAverage)
        };

        foreach (var (metric, model, method) in models)
        {
            _manager.StoreModel(metric, model, method);
        }

        // Act
        var result = _manager.GetModelCount();

        // Assert
        Assert.Equal(models.Length, result);
    }

    [Fact]
    public void GetModelCount_Should_Update_After_Removing_Models()
    {
        // Arrange
        var metricName = "test.metric";
        var model = Mock.Of<ITransformer>();
        _manager.StoreModel(metricName, model, ForecastingMethod.SSA);

        // Act & Assert - Before removal
        Assert.Equal(1, _manager.GetModelCount());

        // Remove model
        _manager.RemoveModel(metricName);

        // Assert - After removal
        Assert.Equal(0, _manager.GetModelCount());
    }

    #endregion

    #region StoreModels Tests

    [Fact]
    public void StoreModels_Should_Throw_When_Models_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _manager.StoreModels(null!));
    }

    [Fact]
    public void StoreModels_Should_Store_All_Models()
    {
        // Arrange
        var models = new Dictionary<string, (ITransformer, ForecastingMethod)>
        {
            ["metric1"] = (Mock.Of<ITransformer>(), ForecastingMethod.SSA),
            ["metric2"] = (Mock.Of<ITransformer>(), ForecastingMethod.ExponentialSmoothing),
            ["metric3"] = (Mock.Of<ITransformer>(), ForecastingMethod.MovingAverage)
        };

        // Act
        _manager.StoreModels(models);

        // Assert
        Assert.Equal(3, _manager.GetModelCount());
        foreach (var metric in models.Keys)
        {
            Assert.True(_manager.HasModel(metric));
        }
    }

    [Fact]
    public void StoreModels_Should_Log_Information_Message()
    {
        // Arrange
        var models = new Dictionary<string, (ITransformer, ForecastingMethod)>
        {
            ["metric1"] = (Mock.Of<ITransformer>(), ForecastingMethod.SSA)
        };

        // Act
        _manager.StoreModels(models);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Stored") && o.ToString()!.Contains("1") && o.ToString()!.Contains("models in batch")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region RemoveModels Tests

    [Fact]
    public void RemoveModels_Should_Throw_When_MetricNames_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _manager.RemoveModels(null!));
    }

    [Fact]
    public void RemoveModels_Should_Remove_Existing_Models()
    {
        // Arrange
        var metricNames = new[] { "metric1", "metric2", "metric3" };
        foreach (var metric in metricNames)
        {
            var model = Mock.Of<ITransformer>();
            _manager.StoreModel(metric, model, ForecastingMethod.SSA);
        }

        // Act
        _manager.RemoveModels(metricNames);

        // Assert
        Assert.Equal(0, _manager.GetModelCount());
        foreach (var metric in metricNames)
        {
            Assert.False(_manager.HasModel(metric));
        }
    }

    [Fact]
    public void RemoveModels_Should_Handle_NonExistent_Models_Gracefully()
    {
        // Arrange
        var existingMetric = "existing.metric";
        var nonExistentMetrics = new[] { "nonexistent1", "nonexistent2" };

        var model = Mock.Of<ITransformer>();
        _manager.StoreModel(existingMetric, model, ForecastingMethod.SSA);

        // Act
        _manager.RemoveModels(new[] { existingMetric }.Concat(nonExistentMetrics));

        // Assert
        Assert.Equal(0, _manager.GetModelCount());
    }

    [Fact]
    public void RemoveModels_Should_Log_Information_Message_When_Models_Are_Removed()
    {
        // Arrange
        var metricName = "test.metric";
        var model = Mock.Of<ITransformer>();
        _manager.StoreModel(metricName, model, ForecastingMethod.SSA);

        // Act
        _manager.RemoveModels(new[] { metricName });

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Removed") && o.ToString()!.Contains("1") && o.ToString()!.Contains("models in batch")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RemoveModels_Should_Not_Log_When_No_Models_Are_Removed()
    {
        // Act
        _manager.RemoveModels(new[] { "nonexistent.metric" });

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Removed") && o.ToString()!.Contains("models in batch")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    #endregion

    #region ClearAll Tests

    [Fact]
    public void ClearAll_Should_Remove_All_Models()
    {
        // Arrange
        var models = new[]
        {
            ("metric1", Mock.Of<ITransformer>(), ForecastingMethod.SSA),
            ("metric2", Mock.Of<ITransformer>(), ForecastingMethod.ExponentialSmoothing),
            ("metric3", Mock.Of<ITransformer>(), ForecastingMethod.MovingAverage)
        };

        foreach (var (metric, model, method) in models)
        {
            _manager.StoreModel(metric, model, method);
        }

        // Act
        _manager.ClearAll();

        // Assert
        Assert.Equal(0, _manager.GetModelCount());
        Assert.Empty(_manager.GetAvailableMetrics());
    }

    [Fact]
    public void ClearAll_Should_Log_Information_Message_When_Models_Are_Cleared()
    {
        // Arrange
        var model = Mock.Of<ITransformer>();
        _manager.StoreModel("test.metric", model, ForecastingMethod.SSA);

        // Act
        _manager.ClearAll();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Cleared all") && o.ToString()!.Contains("1") && o.ToString()!.Contains("models")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ClearAll_Should_Not_Log_When_No_Models_To_Clear()
    {
        // Act
        _manager.ClearAll();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Cleared all")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void Manager_Should_Be_Thread_Safe_For_Concurrent_Operations()
    {
        // Arrange
        var metrics = Enumerable.Range(0, 100).Select(i => $"metric{i}").ToList();
        var exceptions = new List<Exception>();

        // Act
        var tasks = metrics.Select(async metric =>
        {
            try
            {
                var model = Mock.Of<ITransformer>();
                _manager.StoreModel(metric, model, ForecastingMethod.SSA);
                await Task.Delay(1); // Simulate some work
                Assert.True(_manager.HasModel(metric));
                _manager.RemoveModel(metric);
                Assert.False(_manager.HasModel(metric));
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        });

        Task.WaitAll(tasks.ToArray());

        // Assert
        Assert.Empty(exceptions);
    }

    #endregion
}