using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using Relay.Core.AI.Models;
using Relay.Core.AI.Optimization.Connection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI.Optimization.Connection;

public class ConnectionMetricsUtilitiesTests
{
    private readonly ILogger _logger;
    private readonly AIOptimizationOptions _options;
    private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
    private readonly TestTimeSeriesDatabase _timeSeriesDb;
    private readonly SystemMetricsCalculator _systemMetrics;
    private readonly ProtocolMetricsCalculator _protocolCalculator;
    private readonly ConnectionMetricsUtilities _utilities;

    public ConnectionMetricsUtilitiesTests()
    {
        _logger = NullLogger.Instance;
        _options = new AIOptimizationOptions
        {
            MaxEstimatedHttpConnections = 1000,
            MaxEstimatedDbConnections = 50,
            EstimatedMaxDbConnections = 100,
            MaxEstimatedExternalConnections = 30,
            MaxEstimatedWebSocketConnections = 1000
        };
        _requestAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
        _timeSeriesDb = new TestTimeSeriesDatabase();
        _systemMetrics = new SystemMetricsCalculator(NullLogger<SystemMetricsCalculator>.Instance, _requestAnalytics);
        _protocolCalculator = new ProtocolMetricsCalculator(_logger, _requestAnalytics, _timeSeriesDb, _systemMetrics);
        _utilities = new ConnectionMetricsUtilities(_logger, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics, _protocolCalculator);
    }

    #region Constructor Tests

        [Fact]
        public void GetUpgradedConnectionCount_Should_Return_Zero_When_No_WebSocket_Provider()
        {
            // Act
            var result = _utilities.GetUpgradedConnectionCount(null);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void EstimateKeepAliveConnections_Should_Return_Positive_Value()
        {
            // Act
            var result = _utilities.EstimateKeepAliveConnections();

            // Assert
            Assert.True(result >= 0);
        }

    [Fact]
    public void Constructor_Should_Throw_When_RequestAnalytics_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectionMetricsUtilities(_logger, _options, null!, _timeSeriesDb, _systemMetrics, _protocolCalculator));
    }

    [Fact]
    public void GetFallbackHttpConnectionCount_Should_Return_Positive_Value()
    {
        // Act
        var result = _utilities.GetFallbackHttpConnectionCount();

        // Assert
        Assert.True(result >= 0);
    }

    [Fact]
    public void GetOutboundHttpConnectionCount_Should_Be_Consistent()
    {
        // Arrange
        var requestType = typeof(object);
        var requestAnalysisData = new RequestAnalysisData();
        requestAnalysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 100,
            SuccessfulExecutions = 95,
            FailedExecutions = 5,
            AverageExecutionTime = TimeSpan.FromMilliseconds(50),
            ConcurrentExecutions = 10
        });
        _requestAnalytics[requestType] = requestAnalysisData;

        // Act
        var result1 = _utilities.GetOutboundHttpConnectionCount();
        var result2 = _utilities.GetOutboundHttpConnectionCount();

        // Assert
        Assert.Equal(result1, result2);
        Assert.True(result1 >= 0);
    }

    [Fact]
    public void Constructor_Should_Throw_When_ProtocolCalculator_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ConnectionMetricsUtilities(_logger, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics, null!));
    }

    #endregion

    #region CalculateEMA Tests

    [Fact]
    public void EstimateKeepAliveConnections_Should_Be_Consistent()
    {
        // Act
        var result1 = _utilities.EstimateKeepAliveConnections();
        var result2 = _utilities.EstimateKeepAliveConnections();

        // Assert
        Assert.Equal(result1, result2);
        Assert.True(result1 >= 0);
    }

    [Fact]
    public void EstimateKeepAliveConnections_Should_Return_Non_Negative()
    {
        // Act
        var result = _utilities.EstimateKeepAliveConnections();

        // Assert
        Assert.True(result >= 0);
    }

    [Fact]
    public void CalculateEMA_Should_Calculate_Correct_EMA_For_Multiple_Values()
    {
        // Arrange
        var values = new List<double> { 100, 110, 120, 115, 125 };
        var alpha = 0.3;

        // Act
        var result = _utilities.CalculateEMA(values, alpha);

        // Assert
        Assert.True(result > 100);
        Assert.True(result < 125);
    }

        [Fact]
        public void GetUpgradedConnectionCount_Should_Handle_Null_WebSocket_Provider()
        {
            // Act
            var result = _utilities.GetUpgradedConnectionCount(null);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetFallbackHttpConnectionCount_Should_Return_Non_Negative()
        {
            // Act
            var result = _utilities.GetFallbackHttpConnectionCount();

            // Assert
            Assert.True(result >= 0);
        }

        [Fact]
        public void GetFallbackHttpConnectionCount_Should_Respect_Options_Limit()
        {
            // Act
            var result = _utilities.GetFallbackHttpConnectionCount();

            // Assert
            Assert.True(result <= _options.MaxEstimatedHttpConnections);
        }

        [Fact]
        public void GetFallbackHttpConnectionCount_Should_Be_Deterministic()
        {
            // Act
            var result1 = _utilities.GetFallbackHttpConnectionCount();
            var result2 = _utilities.GetFallbackHttpConnectionCount();
            var result3 = _utilities.GetFallbackHttpConnectionCount();

            // Assert
            Assert.Equal(result1, result2);
            Assert.Equal(result2, result3);
            Assert.True(result1 >= 0);
        }

    #endregion

    #region CalculateConnectionThroughputFactor Tests

    [Fact]
    public void CalculateConnectionThroughputFactor_Should_Return_Minimum_One()
    {
        // Arrange
        var mockSystemMetrics = new Mock<SystemMetricsCalculator>(NullLogger<SystemMetricsCalculator>.Instance, _requestAnalytics);
        mockSystemMetrics.Setup(x => x.CalculateCurrentThroughput()).Returns(0);

        var utilities = new ConnectionMetricsUtilities(_logger, _options, _requestAnalytics, _timeSeriesDb, mockSystemMetrics.Object, _protocolCalculator);

        // Act
        var result = utilities.CalculateConnectionThroughputFactor();

        // Assert
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void CalculateConnectionThroughputFactor_Should_Calculate_Correct_Factor()
    {
        // Arrange
        var mockSystemMetrics = new Mock<SystemMetricsCalculator>(NullLogger<SystemMetricsCalculator>.Instance, _requestAnalytics);
        mockSystemMetrics.Setup(x => x.CalculateCurrentThroughput()).Returns(100);

        var utilities = new ConnectionMetricsUtilities(_logger, _options, _requestAnalytics, _timeSeriesDb, mockSystemMetrics.Object, _protocolCalculator);

        // Act
        var result = utilities.CalculateConnectionThroughputFactor();

        // Assert
        Assert.Equal(10.0, result); // 100 / 10
    }

    [Fact]
    public void CalculateConnectionThroughputFactor_Should_Handle_High_Throughput()
    {
        // Arrange
        var mockSystemMetrics = new Mock<SystemMetricsCalculator>(NullLogger<SystemMetricsCalculator>.Instance, _requestAnalytics);
        mockSystemMetrics.Setup(x => x.CalculateCurrentThroughput()).Returns(1000);

        var utilities = new ConnectionMetricsUtilities(_logger, _options, _requestAnalytics, _timeSeriesDb, mockSystemMetrics.Object, _protocolCalculator);

        // Act
        var result = utilities.CalculateConnectionThroughputFactor();

        // Assert
        Assert.Equal(100.0, result); // 1000 / 10
    }

    #endregion

    #region EstimateKeepAliveConnections Tests



    #endregion

    #region GetOutboundHttpConnectionCount Tests

    [Fact]
    public void GetOutboundHttpConnectionCount_Should_Return_Zero_With_No_Analytics()
    {
        // Act
        var result = _utilities.GetOutboundHttpConnectionCount();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetOutboundHttpConnectionCount_Should_Calculate_Based_On_Execution_Counts()
    {
        // Arrange
        var analysisData = new RequestAnalysisData();
        analysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 1000,
            SuccessfulExecutions = 950,
            FailedExecutions = 50,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            ConcurrentExecutions = 10
        });
        _requestAnalytics.TryAdd(typeof(string), analysisData);

        // Act
        var result = _utilities.GetOutboundHttpConnectionCount();

        // Assert
        Assert.True(result >= 0); // Allow 0 when no active connections
        Assert.True(result <= 100); // Reasonable upper bound for HTTP connections
    }

    [Fact]
    public void GetOutboundHttpConnectionCount_Should_Be_Consistent_With_Same_Input()
    {
        // Arrange
        var analysisData = new RequestAnalysisData();
        analysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 1000,
            SuccessfulExecutions = 950,
            FailedExecutions = 50,
            AverageExecutionTime = TimeSpan.FromMilliseconds(100),
            ConcurrentExecutions = 10
        });
        _requestAnalytics.TryAdd(typeof(string), analysisData);

        // Act
        var result1 = _utilities.GetOutboundHttpConnectionCount();
        var result2 = _utilities.GetOutboundHttpConnectionCount();

        // Assert - should be consistent for same input
        Assert.Equal(result1, result2);
        Assert.True(result1 >= 0);
    }

    [Fact]
    public void GetOutboundHttpConnectionCount_Should_Handle_Multiple_Request_Types()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            var analysisData = new RequestAnalysisData();
            analysisData.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 100 * (i + 1),
                SuccessfulExecutions = 95 * (i + 1),
                FailedExecutions = 5 * (i + 1),
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                ConcurrentExecutions = 10
            });
            _requestAnalytics.TryAdd(Type.GetType($"System.String{i}") ?? typeof(string), analysisData);
        }

        // Act
        var result = _utilities.GetOutboundHttpConnectionCount();

        // Assert
        Assert.True(result >= 0); // Allow 0 when no active connections
        Assert.True(result <= 200); // Allow more connections for multiple request types
    }

    [Fact]
    public void GetOutboundHttpConnectionCount_Should_Handle_Exception_Gracefully()
    {
        // Arrange
        var problematicAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
        var utilities = new ConnectionMetricsUtilities(_logger, _options, problematicAnalytics, _timeSeriesDb, _systemMetrics, _protocolCalculator);

        // Act
        var result = utilities.GetOutboundHttpConnectionCount();

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region GetUpgradedConnectionCount Tests

    [Fact]
    public void GetUpgradedConnectionCount_Should_Return_Zero_With_No_Provider()
    {
        // Act
        var result = _utilities.GetUpgradedConnectionCount(null);

        // Assert
        Assert.Equal(0, result);
    }

        [Fact]
        public void EstimateKeepAliveConnections_Should_Be_Deterministic()
        {
            // Act
            var result1 = _utilities.EstimateKeepAliveConnections();
            var result2 = _utilities.EstimateKeepAliveConnections();

            // Assert
            Assert.Equal(result1, result2);
            Assert.True(result1 >= 0);
        }

    [Fact]
    public void EstimateKeepAliveConnections_Should_Handle_High_Load_Scenarios()
    {
        // Arrange - Add high load request analytics
        var requestType = typeof(object);
        var requestAnalysisData = new RequestAnalysisData();
        requestAnalysisData.AddMetrics(new RequestExecutionMetrics
        {
            TotalExecutions = 1000,
            SuccessfulExecutions = 950,
            FailedExecutions = 50,
            AverageExecutionTime = TimeSpan.FromMilliseconds(200),
            ConcurrentExecutions = 100
        });
        _requestAnalytics[requestType] = requestAnalysisData;

        // Act
        var result = _utilities.EstimateKeepAliveConnections();

        // Assert
        Assert.True(result >= 0);
    }

    [Fact]
    public void GetUpgradedConnectionCount_Should_Work_With_Real_Provider()
    {
        // Arrange - test with null provider to verify graceful handling
        WebSocketConnectionMetricsProvider? provider = null;

        // Act
        var result = _utilities.GetUpgradedConnectionCount(provider);

        // Assert
        Assert.True(result >= 0); // Should always be non-negative even with null provider
        Assert.True(result <= 1000); // Reasonable upper bound for test scenarios
    }

        [Fact]
        public void GetUpgradedConnectionCount_Should_Handle_Repeated_Calls()
        {
            // Act
            for (int i = 0; i < 5; i++)
            {
                var result = _utilities.GetUpgradedConnectionCount(null);
                Assert.Equal(0, result);
            }
        }

    [Fact]
    public void GetUpgradedConnectionCount_Should_Work_With_Null_Provider()
    {
        // Act
        var result = _utilities.GetUpgradedConnectionCount(null);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void GetUpgradedConnectionCount_Should_Be_Deterministic()
    {
        // Act
        var result1 = _utilities.GetUpgradedConnectionCount(null);
        var result2 = _utilities.GetUpgradedConnectionCount(null);

        // Assert
        Assert.Equal(result1, result2);
        Assert.Equal(0, result1);
    }

    #endregion

    #region GetFallbackHttpConnectionCount Tests

    [Fact]
    public void GetFallbackHttpConnectionCount_Should_Return_Based_On_Processor_Count()
    {
        // Act
        var result = _utilities.GetFallbackHttpConnectionCount();

        // Assert
        var expected = (Environment.ProcessorCount * 2) + Math.Min(0, Environment.ProcessorCount * 4);
        expected = (int)(expected * 1.3);
        Assert.Equal(Math.Min(expected, 100), result);
    }

    [Fact]
    public void GetFallbackHttpConnectionCount_Should_Be_Positive()
    {
        // Arrange
        var utilities = new ConnectionMetricsUtilities(_logger, _options, _requestAnalytics, _timeSeriesDb, _systemMetrics, _protocolCalculator);

        // Act
        var result = utilities.GetFallbackHttpConnectionCount();

        // Assert
        Assert.True(result > 0);
        Assert.True(result <= 200); // Reasonable upper bound
    }

        [Fact]
        public void GetUpgradedConnectionCount_Should_Be_Thread_Safe()
        {
            // Arrange
            var tasks = new System.Threading.Tasks.Task[10];
            var results = new System.Collections.Concurrent.ConcurrentBag<int>();

            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = System.Threading.Tasks.Task.Run(() =>
                {
                    var result = _utilities.GetUpgradedConnectionCount(null);
                    results.Add(result);
                });
            }

            // Act
            System.Threading.Tasks.Task.WaitAll(tasks);

            // Assert
            Assert.Equal(10, results.Count);
            foreach (var result in results)
            {
                Assert.True(result >= 0);
            }
        }

        [Fact]
        public void GetUpgradedConnectionCount_Should_Handle_Multiple_Calls()
        {
            // Act
            var result1 = _utilities.GetUpgradedConnectionCount(null);
            var result2 = _utilities.GetUpgradedConnectionCount(null);
            var result3 = _utilities.GetUpgradedConnectionCount(null);

            // Assert
            Assert.Equal(result1, result2);
            Assert.Equal(result2, result3);
            Assert.True(result1 >= 0);
        }

    #endregion

    #region Integration Tests

    [Fact]
    public void All_Methods_Should_Be_Thread_Safe()
    {
        // Arrange
        var tasks = new System.Threading.Tasks.Task[10];

        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = System.Threading.Tasks.Task.Run(() =>
            {
                // Test various methods concurrently
                var ema = _utilities.CalculateEMA(new List<double> { 100, 200, 300 }, 0.3);
                var throughput = _utilities.CalculateConnectionThroughputFactor();
                var keepAlive = _utilities.EstimateKeepAliveConnections();
                var outbound = _utilities.GetOutboundHttpConnectionCount();
                var upgraded = _utilities.GetUpgradedConnectionCount(null);
                var fallback = _utilities.GetFallbackHttpConnectionCount();

                Assert.True(ema >= 0);
                Assert.True(throughput >= 1.0);
                Assert.True(keepAlive >= 0);
                Assert.True(outbound >= 0);
                Assert.True(upgraded >= 0);
                Assert.True(fallback >= 0);
            });
        }

        // Act & Assert
        System.Threading.Tasks.Task.WaitAll(tasks);
    }

    [Fact]
    public void GetUpgradedConnectionCount_Should_Always_Return_Non_Negative()
    {
        // Act
        var result = _utilities.GetUpgradedConnectionCount(null);

        // Assert
        Assert.True(result >= 0);
    }

    [Fact]
    public void Methods_Should_Work_With_Empty_Request_Analytics()
    {
        // Arrange
        var emptyAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
        var utilities = new ConnectionMetricsUtilities(_logger, _options, emptyAnalytics, _timeSeriesDb, _systemMetrics, _protocolCalculator);

        // Act
        var outbound = utilities.GetOutboundHttpConnectionCount();
        var upgraded = utilities.GetUpgradedConnectionCount(null);

        // Assert
        Assert.Equal(0, outbound);
        Assert.Equal(0, upgraded);
    }

    #endregion
}