using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI.Analysis.TimeSeries;
using System;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI.Analysis.TimeSeries.Managers;

public class ForecastingMethodManagerTests
{
    private readonly Mock<ILogger<ForecastingMethodManager>> _loggerMock;
    private readonly ForecastingConfiguration _config;
    private readonly ForecastingMethodManager _manager;

    public ForecastingMethodManagerTests()
    {
        _loggerMock = new Mock<ILogger<ForecastingMethodManager>>();
        _config = new ForecastingConfiguration
        {
            DefaultForecastingMethod = ForecastingMethod.SSA
        };

        _manager = new ForecastingMethodManager(_loggerMock.Object, _config);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_Should_Throw_When_Logger_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ForecastingMethodManager(null!, _config));
    }

    [Fact]
    public void Constructor_Should_Not_Throw_When_Config_Is_Null()
    {
        // Act & Assert - should not throw, uses default method
        var manager = new ForecastingMethodManager(_loggerMock.Object, null!);
        Assert.NotNull(manager);
    }

    [Fact]
    public void Constructor_Should_Use_Default_Method_From_Config()
    {
        // Arrange
        var config = new ForecastingConfiguration
        {
            DefaultForecastingMethod = ForecastingMethod.ExponentialSmoothing
        };

        // Act
        var manager = new ForecastingMethodManager(_loggerMock.Object, config);

        // Assert
        var method = manager.GetForecastingMethod("any.metric");
        Assert.Equal(ForecastingMethod.ExponentialSmoothing, method);
    }

    [Fact]
    public void Constructor_Should_Use_SSA_As_Default_When_Config_Default_Is_Null()
    {
        // Arrange
        var config = new ForecastingConfiguration
        {
            DefaultForecastingMethod = default // null for enum
        };

        // Act
        var manager = new ForecastingMethodManager(_loggerMock.Object, config);

        // Assert
        var method = manager.GetForecastingMethod("any.metric");
        Assert.Equal(ForecastingMethod.SSA, method);
    }

    [Fact]
    public void Constructor_Should_Register_Default_Strategies()
    {
        // Act
        var strategies = _manager.GetAvailableStrategies().ToList();

        // Assert
        Assert.Equal(4, strategies.Count); // SSA, ExponentialSmoothing, MovingAverage, Ensemble

        var methods = strategies.Select(s => s.Method).ToList();
        Assert.Contains(ForecastingMethod.SSA, methods);
        Assert.Contains(ForecastingMethod.ExponentialSmoothing, methods);
        Assert.Contains(ForecastingMethod.MovingAverage, methods);
        Assert.Contains(ForecastingMethod.Ensemble, methods);
    }

    [Fact]
    public void Constructor_Should_Log_Initialization()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ForecastingMethodManager>>();
        var config = new ForecastingConfiguration
        {
            DefaultForecastingMethod = ForecastingMethod.ExponentialSmoothing
        };

        // Act
        var manager = new ForecastingMethodManager(loggerMock.Object, config);

        // Assert
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Forecasting method manager initialized") &&
                                                o.ToString()!.Contains("ExponentialSmoothing")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RegisterStrategy_Should_Log_Debug()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ForecastingMethodManager>>();
        var config = new ForecastingConfiguration();
        var manager = new ForecastingMethodManager(loggerMock.Object, config);
        var mockStrategy = new Mock<IForecastingStrategy>();
        mockStrategy.Setup(s => s.Method).Returns(ForecastingMethod.SSA);

        // Act
        // We can't call RegisterStrategy directly as it's private, but it's called in constructor
        // So we verify the logs from constructor
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Registered forecasting strategy") &&
                                                o.ToString()!.Contains("SSA")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce); // At least once for SSA
    }

    #endregion

    #region GetForecastingMethod Tests

    [Fact]
    public void GetForecastingMethod_Should_Throw_When_MetricName_Is_Null()
    {
        Assert.Throws<ArgumentException>(() => _manager.GetForecastingMethod(null!));
    }

    [Fact]
    public void GetForecastingMethod_Should_Throw_When_MetricName_Is_Empty()
    {
        Assert.Throws<ArgumentException>(() => _manager.GetForecastingMethod(string.Empty));
    }

    [Fact]
    public void GetForecastingMethod_Should_Throw_When_MetricName_Is_Whitespace()
    {
        Assert.Throws<ArgumentException>(() => _manager.GetForecastingMethod("   "));
    }

    [Theory]
    [InlineData("very_long_metric_name_that_exceeds_normal_length_and_should_still_work_fine")]
    [InlineData("metric.with.dots")]
    [InlineData("metric-with-dashes")]
    [InlineData("metric_with_underscores")]
    [InlineData("metric123")]
    [InlineData("métric")]  // Unicode
    [InlineData("метрика")]  // Cyrillic
    [InlineData("指標")]  // Chinese
    [InlineData("metric@domain")]
    [InlineData("metric#tag")]
    public void GetForecastingMethod_Should_Accept_Various_Metric_Name_Formats(string metricName)
    {
        // Act
        var method = _manager.GetForecastingMethod(metricName);

        // Assert
        Assert.Equal(_config.DefaultForecastingMethod, method);
    }

    [Theory]
    [InlineData("very_long_metric_name_that_exceeds_normal_length_and_should_still_work_fine")]
    [InlineData("metric.with.dots")]
    [InlineData("metric-with-dashes")]
    [InlineData("metric_with_underscores")]
    [InlineData("metric123")]
    [InlineData("métric")]  // Unicode
    [InlineData("метрика")]  // Cyrillic
    [InlineData("指標")]  // Chinese
    [InlineData("metric@domain")]
    [InlineData("metric#tag")]
    public void SetForecastingMethod_Should_Accept_Various_Metric_Name_Formats(string metricName)
    {
        // Act
        _manager.SetForecastingMethod(metricName, ForecastingMethod.ExponentialSmoothing);

        // Assert
        Assert.Equal(ForecastingMethod.ExponentialSmoothing, _manager.GetForecastingMethod(metricName));
    }

    [Fact]
    public void GetForecastingMethod_Should_Return_Default_Method_For_Unset_Metric()
    {
        // Act
        var method = _manager.GetForecastingMethod("new.metric");

        // Assert
        Assert.Equal(_config.DefaultForecastingMethod, method);
    }

    [Fact]
    public void GetForecastingMethod_Should_Return_Set_Method_For_Metric()
    {
        // Arrange
        var metricName = "test.metric";
        var expectedMethod = ForecastingMethod.ExponentialSmoothing;
        _manager.SetForecastingMethod(metricName, expectedMethod);

        // Act
        var method = _manager.GetForecastingMethod(metricName);

        // Assert
        Assert.Equal(expectedMethod, method);
    }

    [Fact]
    public void GetForecastingMethod_Should_Return_Correct_Method_For_Different_Metrics()
    {
        // Arrange
        _manager.SetForecastingMethod("metric1", ForecastingMethod.SSA);
        _manager.SetForecastingMethod("metric2", ForecastingMethod.MovingAverage);

        // Act & Assert
        Assert.Equal(ForecastingMethod.SSA, _manager.GetForecastingMethod("metric1"));
        Assert.Equal(ForecastingMethod.MovingAverage, _manager.GetForecastingMethod("metric2"));
        Assert.Equal(_config.DefaultForecastingMethod, _manager.GetForecastingMethod("metric3"));
    }

    #endregion

    #region SetForecastingMethod Tests

    [Fact]
    public void SetForecastingMethod_Should_Throw_When_MetricName_Is_Null()
    {
        Assert.Throws<ArgumentException>(() =>
            _manager.SetForecastingMethod(null!, ForecastingMethod.SSA));
    }

    [Fact]
    public void SetForecastingMethod_Should_Throw_When_MetricName_Is_Empty()
    {
        Assert.Throws<ArgumentException>(() =>
            _manager.SetForecastingMethod(string.Empty, ForecastingMethod.SSA));
    }

    [Fact]
    public void SetForecastingMethod_Should_Throw_When_MetricName_Is_Whitespace()
    {
        Assert.Throws<ArgumentException>(() =>
            _manager.SetForecastingMethod("   ", ForecastingMethod.SSA));
    }

    [Fact]
    public void SetForecastingMethod_Should_Set_Method_For_Metric()
    {
        // Arrange
        var metricName = "test.metric";
        var method = ForecastingMethod.ExponentialSmoothing;

        // Act
        _manager.SetForecastingMethod(metricName, method);

        // Assert
        Assert.Equal(method, _manager.GetForecastingMethod(metricName));
    }

    [Fact]
    public void SetForecastingMethod_Should_Allow_Overriding_Previous_Method()
    {
        // Arrange
        var metricName = "test.metric";

        // Act
        _manager.SetForecastingMethod(metricName, ForecastingMethod.SSA);
        _manager.SetForecastingMethod(metricName, ForecastingMethod.MovingAverage);

        // Assert
        Assert.Equal(ForecastingMethod.MovingAverage, _manager.GetForecastingMethod(metricName));
    }

    [Fact]
    public void SetForecastingMethod_Should_Log_Information()
    {
        // Arrange
        var metricName = "test.metric";
        var method = ForecastingMethod.SSA;

        // Act
        _manager.SetForecastingMethod(metricName, method);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Forecasting method for") &&
                                               o.ToString()!.Contains(metricName) &&
                                               o.ToString()!.Contains(method.ToString())),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetAvailableStrategies Tests

    [Fact]
    public void GetAvailableStrategies_Should_Return_All_Registered_Strategies()
    {
        // Act
        var strategies = _manager.GetAvailableStrategies().ToList();

        // Assert
        Assert.Equal(4, strategies.Count);
        Assert.All(strategies, s => Assert.NotNull(s));
    }

    [Fact]
    public void GetAvailableStrategies_Should_Return_Distinct_Strategies()
    {
        // Act
        var strategies = _manager.GetAvailableStrategies().ToList();
        var methods = strategies.Select(s => s.Method).ToList();

        // Assert
        Assert.Equal(methods.Count, methods.Distinct().Count());
    }

    #endregion

    #region GetStrategy Tests

    [Fact]
    public void GetStrategy_Should_Return_Correct_Strategy_For_Registered_Method()
    {
        // Act
        var strategy = _manager.GetStrategy(ForecastingMethod.SSA);

        // Assert
        Assert.NotNull(strategy);
        Assert.Equal(ForecastingMethod.SSA, strategy!.Method);
    }

    [Theory]
    [InlineData(ForecastingMethod.SSA)]
    [InlineData(ForecastingMethod.ExponentialSmoothing)]
    [InlineData(ForecastingMethod.MovingAverage)]
    [InlineData(ForecastingMethod.Ensemble)]
    public void GetStrategy_Should_Return_Correct_Strategy_For_All_Registered_Methods(ForecastingMethod method)
    {
        // Act
        var strategy = _manager.GetStrategy(method);

        // Assert
        Assert.NotNull(strategy);
        Assert.Equal(method, strategy!.Method);
    }

    [Fact]
    public void GetStrategy_Should_Return_Null_For_Unregistered_Method()
    {
        // Act
        var strategy = _manager.GetStrategy((ForecastingMethod)999); // Invalid method

        // Assert
        Assert.Null(strategy);
    }

    [Fact]
    public void GetStrategy_Should_Return_Same_Instance_For_Same_Method()
    {
        // Act
        var strategy1 = _manager.GetStrategy(ForecastingMethod.SSA);
        var strategy2 = _manager.GetStrategy(ForecastingMethod.SSA);

        // Assert
        Assert.Same(strategy1, strategy2);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void Manager_Should_Be_Thread_Safe_For_Concurrent_Access()
    {
        // Arrange
        var tasks = new System.Threading.Tasks.Task[10];

        for (int i = 0; i < tasks.Length; i++)
        {
            var metricName = $"metric{i}";
            tasks[i] = System.Threading.Tasks.Task.Run(() =>
            {
                _manager.SetForecastingMethod(metricName, ForecastingMethod.SSA);
                var method = _manager.GetForecastingMethod(metricName);
                Assert.Equal(ForecastingMethod.SSA, method);
            });
        }

        // Act & Assert
        System.Threading.Tasks.Task.WaitAll(tasks);
    }

    [Fact]
    public void GetAvailableStrategies_Should_Be_Thread_Safe_For_Concurrent_Access()
    {
        // Arrange
        var tasks = new System.Threading.Tasks.Task[10];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = System.Threading.Tasks.Task.Run(() =>
            {
                var strategies = _manager.GetAvailableStrategies().ToList();
                Assert.Equal(4, strategies.Count);
                Assert.All(strategies, s => Assert.NotNull(s));
            });
        }

        // Act & Assert
        System.Threading.Tasks.Task.WaitAll(tasks);
    }

    [Fact]
    public void GetStrategy_Should_Be_Thread_Safe_For_Concurrent_Access()
    {
        // Arrange
        var tasks = new System.Threading.Tasks.Task[10];

        for (int i = 0; i < tasks.Length; i++)
        {
            var method = (ForecastingMethod)(i % 4); // Cycle through 0-3
            tasks[i] = System.Threading.Tasks.Task.Run(() =>
            {
                var strategy = _manager.GetStrategy(method);
                Assert.NotNull(strategy);
                Assert.Equal(method, strategy!.Method);
            });
        }

        // Act & Assert
        System.Threading.Tasks.Task.WaitAll(tasks);
    }

    #endregion
}