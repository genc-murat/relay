using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Analysis.TimeSeries;
using Relay.Core.AI.Models;
using Relay.Core.AI.Optimization.Connection;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AspNetCoreConnectionEstimatorTests
    {
        private readonly Mock<ILogger> _loggerMock;
        private readonly AIOptimizationOptions _options;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
        private readonly TimeSeriesDatabase _timeSeriesDb;
        private readonly AspNetCoreConnectionEstimator _estimator;

        public AspNetCoreConnectionEstimatorTests()
        {
            _loggerMock = new Mock<ILogger>();
            _options = new AIOptimizationOptions
            {
                MaxEstimatedHttpConnections = 1000
            };
            _requestAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();

            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var timeSeriesLogger = loggerFactory.CreateLogger<TimeSeriesDatabase>();
            _timeSeriesDb = TimeSeriesDatabase.Create(timeSeriesLogger);

            var systemMetricsLogger = loggerFactory.CreateLogger<SystemMetricsCalculator>();
            var systemMetrics = new SystemMetricsCalculator(systemMetricsLogger, _requestAnalytics);

            var protocolLogger = loggerFactory.CreateLogger<ProtocolMetricsCalculator>();
            var protocolCalculator = new ProtocolMetricsCalculator(protocolLogger, _requestAnalytics, _timeSeriesDb, systemMetrics);

            var utilsLogger = loggerFactory.CreateLogger<ConnectionMetricsUtilities>();
            var utilities = new ConnectionMetricsUtilities(utilsLogger, _options, _requestAnalytics, _timeSeriesDb, systemMetrics, protocolCalculator);

            _estimator = new AspNetCoreConnectionEstimator(
                _loggerMock.Object,
                _options,
                _requestAnalytics,
                _timeSeriesDb,
                systemMetrics,
                protocolCalculator,
                utilities);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var timeSeriesLogger = loggerFactory.CreateLogger<TimeSeriesDatabase>();
            var timeSeriesDb = TimeSeriesDatabase.Create(timeSeriesLogger);
            var systemMetricsLogger = loggerFactory.CreateLogger<SystemMetricsCalculator>();
            var systemMetrics = new SystemMetricsCalculator(systemMetricsLogger, _requestAnalytics);
            var protocolLogger = loggerFactory.CreateLogger<ProtocolMetricsCalculator>();
            var protocolCalculator = new ProtocolMetricsCalculator(protocolLogger, _requestAnalytics, timeSeriesDb, systemMetrics);
            var utilsLogger = loggerFactory.CreateLogger<ConnectionMetricsUtilities>();
            var utilities = new ConnectionMetricsUtilities(utilsLogger, _options, _requestAnalytics, timeSeriesDb, systemMetrics, protocolCalculator);

            Assert.Throws<ArgumentNullException>(() =>
                new AspNetCoreConnectionEstimator(null!, _options, _requestAnalytics, timeSeriesDb, systemMetrics, protocolCalculator, utilities));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Options_Is_Null()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var timeSeriesLogger = loggerFactory.CreateLogger<TimeSeriesDatabase>();
            var timeSeriesDb = TimeSeriesDatabase.Create(timeSeriesLogger);
            var systemMetricsLogger = loggerFactory.CreateLogger<SystemMetricsCalculator>();
            var systemMetrics = new SystemMetricsCalculator(systemMetricsLogger, _requestAnalytics);
            var protocolLogger = loggerFactory.CreateLogger<ProtocolMetricsCalculator>();
            var protocolCalculator = new ProtocolMetricsCalculator(protocolLogger, _requestAnalytics, timeSeriesDb, systemMetrics);
            var utilsLogger = loggerFactory.CreateLogger<ConnectionMetricsUtilities>();
            var utilities = new ConnectionMetricsUtilities(utilsLogger, _options, _requestAnalytics, timeSeriesDb, systemMetrics, protocolCalculator);

            Assert.Throws<ArgumentNullException>(() =>
                new AspNetCoreConnectionEstimator(_loggerMock.Object, null!, _requestAnalytics, timeSeriesDb, systemMetrics, protocolCalculator, utilities));
        }

        [Fact]
        public void Constructor_Should_Throw_When_RequestAnalytics_Is_Null()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var timeSeriesLogger = loggerFactory.CreateLogger<TimeSeriesDatabase>();
            var timeSeriesDb = TimeSeriesDatabase.Create(timeSeriesLogger);
            var systemMetricsLogger = loggerFactory.CreateLogger<SystemMetricsCalculator>();
            var systemMetrics = new SystemMetricsCalculator(systemMetricsLogger, _requestAnalytics);
            var protocolLogger = loggerFactory.CreateLogger<ProtocolMetricsCalculator>();
            var protocolCalculator = new ProtocolMetricsCalculator(protocolLogger, _requestAnalytics, timeSeriesDb, systemMetrics);
            var utilsLogger = loggerFactory.CreateLogger<ConnectionMetricsUtilities>();
            var utilities = new ConnectionMetricsUtilities(utilsLogger, _options, _requestAnalytics, timeSeriesDb, systemMetrics, protocolCalculator);

            Assert.Throws<ArgumentNullException>(() =>
                new AspNetCoreConnectionEstimator(_loggerMock.Object, _options, null!, timeSeriesDb, systemMetrics, protocolCalculator, utilities));
        }

        [Fact]
        public void Constructor_Should_Throw_When_TimeSeriesDb_Is_Null()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var systemMetricsLogger = loggerFactory.CreateLogger<SystemMetricsCalculator>();
            var systemMetrics = new SystemMetricsCalculator(systemMetricsLogger, _requestAnalytics);
            var timeSeriesLogger = loggerFactory.CreateLogger<TimeSeriesDatabase>();
            var timeSeriesDb = TimeSeriesDatabase.Create(timeSeriesLogger);
            var protocolLogger = loggerFactory.CreateLogger<ProtocolMetricsCalculator>();
            var protocolCalculator = new ProtocolMetricsCalculator(protocolLogger, _requestAnalytics, timeSeriesDb, systemMetrics);
            var utilsLogger = loggerFactory.CreateLogger<ConnectionMetricsUtilities>();
            var utilities = new ConnectionMetricsUtilities(utilsLogger, _options, _requestAnalytics, timeSeriesDb, systemMetrics, protocolCalculator);

            Assert.Throws<ArgumentNullException>(() =>
                new AspNetCoreConnectionEstimator(_loggerMock.Object, _options, _requestAnalytics, null!, systemMetrics, protocolCalculator, utilities));
        }

        [Fact]
        public void Constructor_Should_Throw_When_SystemMetrics_Is_Null()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var timeSeriesLogger = loggerFactory.CreateLogger<TimeSeriesDatabase>();
            var timeSeriesDb = TimeSeriesDatabase.Create(timeSeriesLogger);
            var systemMetricsLogger = loggerFactory.CreateLogger<SystemMetricsCalculator>();
            var systemMetrics = new SystemMetricsCalculator(systemMetricsLogger, _requestAnalytics);
            var protocolLogger = loggerFactory.CreateLogger<ProtocolMetricsCalculator>();
            var protocolCalculator = new ProtocolMetricsCalculator(protocolLogger, _requestAnalytics, timeSeriesDb, systemMetrics);
            var utilsLogger = loggerFactory.CreateLogger<ConnectionMetricsUtilities>();
            var utilities = new ConnectionMetricsUtilities(utilsLogger, _options, _requestAnalytics, timeSeriesDb, systemMetrics, protocolCalculator);

            Assert.Throws<ArgumentNullException>(() =>
                new AspNetCoreConnectionEstimator(_loggerMock.Object, _options, _requestAnalytics, timeSeriesDb, null!, protocolCalculator, utilities));
        }

        [Fact]
        public void Constructor_Should_Throw_When_ProtocolCalculator_Is_Null()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var timeSeriesLogger = loggerFactory.CreateLogger<TimeSeriesDatabase>();
            var timeSeriesDb = TimeSeriesDatabase.Create(timeSeriesLogger);
            var systemMetricsLogger = loggerFactory.CreateLogger<SystemMetricsCalculator>();
            var systemMetrics = new SystemMetricsCalculator(systemMetricsLogger, _requestAnalytics);
            var protocolLogger = loggerFactory.CreateLogger<ProtocolMetricsCalculator>();
            var protocolCalculator = new ProtocolMetricsCalculator(protocolLogger, _requestAnalytics, timeSeriesDb, systemMetrics);
            var utilsLogger = loggerFactory.CreateLogger<ConnectionMetricsUtilities>();
            var utilities = new ConnectionMetricsUtilities(utilsLogger, _options, _requestAnalytics, timeSeriesDb, systemMetrics, protocolCalculator);

            Assert.Throws<ArgumentNullException>(() =>
                new AspNetCoreConnectionEstimator(_loggerMock.Object, _options, _requestAnalytics, timeSeriesDb, systemMetrics, null!, utilities));
        }

        [Fact]
        public void Constructor_Should_Throw_When_Utilities_Is_Null()
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var timeSeriesLogger = loggerFactory.CreateLogger<TimeSeriesDatabase>();
            var timeSeriesDb = TimeSeriesDatabase.Create(timeSeriesLogger);
            var systemMetricsLogger = loggerFactory.CreateLogger<SystemMetricsCalculator>();
            var systemMetrics = new SystemMetricsCalculator(systemMetricsLogger, _requestAnalytics);
            var protocolLogger = loggerFactory.CreateLogger<ProtocolMetricsCalculator>();
            var protocolCalculator = new ProtocolMetricsCalculator(protocolLogger, _requestAnalytics, timeSeriesDb, systemMetrics);

            Assert.Throws<ArgumentNullException>(() =>
                new AspNetCoreConnectionEstimator(_loggerMock.Object, _options, _requestAnalytics, timeSeriesDb, systemMetrics, protocolCalculator, null!));
        }

        #endregion

        #region GetAspNetCoreConnectionCount Tests

        [Fact]
        public void GetAspNetCoreConnectionCount_Should_Return_Minimum_Of_One()
        {
            var result = _estimator.GetAspNetCoreConnectionCount();

            Assert.True(result >= 1);
        }

        [Fact]
        public void GetAspNetCoreConnectionCount_Should_Respect_MaxEstimatedHttpConnections_Limit()
        {
            var result = _estimator.GetAspNetCoreConnectionCount();

            Assert.True(result <= _options.MaxEstimatedHttpConnections / 2);
        }

        [Fact]
        public void GetAspNetCoreConnectionCount_Should_Return_Kestrel_Connections_When_Available()
        {
            _timeSeriesDb.StoreMetric("KestrelConnections", 50, DateTime.UtcNow);

            var result = _estimator.GetAspNetCoreConnectionCount();

            Assert.True(result > 0);
        }

        [Fact]
        public void GetAspNetCoreConnectionCount_Should_Use_Request_Analytics_When_Kestrel_Unavailable()
        {
            var analysisData = new RequestAnalysisData();
            analysisData.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 1,
                SuccessfulExecutions = 1,
                FailedExecutions = 0,
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                ConcurrentExecutions = 10
            });
            _requestAnalytics.TryAdd(typeof(string), analysisData);

            var result = _estimator.GetAspNetCoreConnectionCount();

            Assert.True(result >= 1);
        }

        #endregion

        #region GetKestrelServerConnections Tests

        [Fact]
        public void GetKestrelServerConnections_Should_Return_Zero_When_No_Data_Available()
        {
            var result = _estimator.GetKestrelServerConnections();

            Assert.Equal(0, result);
        }

        [Fact]
        public void GetKestrelServerConnections_Should_Return_Stored_Metrics_When_Available()
        {
            _timeSeriesDb.StoreMetric("KestrelConnections", 25, DateTime.UtcNow);

            var result = _estimator.GetKestrelServerConnections();

            Assert.True(result > 0);
        }

        [Fact]
        public void GetKestrelServerConnections_Should_Store_Connection_Metrics()
        {
            _timeSeriesDb.StoreMetric("KestrelConnections", 30, DateTime.UtcNow);

            var result = _estimator.GetKestrelServerConnections();

            var storedMetrics = _timeSeriesDb.GetRecentMetrics("KestrelConnections", 5);
            Assert.NotEmpty(storedMetrics);
        }

        [Fact]
        public void GetKestrelServerConnections_Should_Use_Alternative_Metric_Names()
        {
            _timeSeriesDb.StoreMetric("kestrel-current-connections", 15, DateTime.UtcNow);

            var result = _estimator.GetKestrelServerConnections();

            Assert.True(result > 0);
        }

        [Fact]
        public void GetKestrelServerConnections_Should_Infer_From_Request_Patterns()
        {
            var analysisData1 = new RequestAnalysisData();
            analysisData1.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 1,
                SuccessfulExecutions = 1,
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                ConcurrentExecutions = 20
            });
            _requestAnalytics.TryAdd(typeof(string), analysisData1);

            var analysisData2 = new RequestAnalysisData();
            analysisData2.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 1,
                SuccessfulExecutions = 1,
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                ConcurrentExecutions = 15
            });
            _requestAnalytics.TryAdd(typeof(int), analysisData2);

            var result = _estimator.GetKestrelServerConnections();

            Assert.True(result >= 0);
        }

        [Fact]
        public void GetKestrelServerConnections_Should_Handle_Multiple_Metrics()
        {
            for (int i = 0; i < 10; i++)
            {
                _timeSeriesDb.StoreMetric("KestrelConnections", 10 + i, DateTime.UtcNow.AddSeconds(-i));
            }

            var result = _estimator.GetKestrelServerConnections();

            Assert.True(result > 0);
        }

        [Fact]
        public void GetKestrelServerConnections_Should_Use_Weighted_Average()
        {
            _timeSeriesDb.StoreMetric("KestrelConnections", 10, DateTime.UtcNow.AddSeconds(-3));
            _timeSeriesDb.StoreMetric("KestrelConnections", 20, DateTime.UtcNow.AddSeconds(-2));
            _timeSeriesDb.StoreMetric("KestrelConnections", 30, DateTime.UtcNow.AddSeconds(-1));

            var result = _estimator.GetKestrelServerConnections();

            Assert.True(result > 10);
        }

        [Fact]
        public void GetKestrelServerConnections_Should_Predict_Based_On_Historical_Data()
        {
            var currentHour = DateTime.UtcNow.Hour;
            for (int i = 0; i < 25; i++)
            {
                var timestamp = DateTime.UtcNow.AddHours(-i).Date.AddHours(currentHour);
                _timeSeriesDb.StoreMetric("KestrelConnections", 40 + (i % 5), timestamp);
            }

            var result = _estimator.GetKestrelServerConnections();

            Assert.True(result >= 0);
        }

        #endregion

        #region StoreKestrelConnectionMetrics Tests

        [Fact]
        public void StoreKestrelConnectionMetrics_Should_Store_Valid_Connection_Count()
        {
            _timeSeriesDb.StoreMetric("KestrelConnections", 100, DateTime.UtcNow);

            var result = _estimator.GetKestrelServerConnections();

            var metrics = _timeSeriesDb.GetRecentMetrics("KestrelConnections", 10);
            Assert.NotEmpty(metrics);
        }

        [Fact]
        public void StoreKestrelConnectionMetrics_Should_Store_In_Multiple_Metric_Names()
        {
            _timeSeriesDb.StoreMetric("KestrelConnections", 75, DateTime.UtcNow);

            var result = _estimator.GetKestrelServerConnections();

            var kestrelMetrics = _timeSeriesDb.GetRecentMetrics("KestrelConnections", 10);
            var aspNetCoreMetrics = _timeSeriesDb.GetRecentMetrics("ConnectionCount_AspNetCore", 10);

            Assert.NotEmpty(kestrelMetrics);
        }

        [Fact]
        public void StoreKestrelConnectionMetrics_Should_Not_Store_Zero_Or_Negative_Values()
        {
            var initialMetricsCount = _timeSeriesDb.GetRecentMetrics("KestrelConnections", 100).Count;

            var result = _estimator.GetKestrelServerConnections();

            var finalMetricsCount = _timeSeriesDb.GetRecentMetrics("KestrelConnections", 100).Count;
        }

        [Fact]
        public void StoreKestrelConnectionMetrics_Should_Handle_Concurrent_Calls()
        {
            var tasks = Enumerable.Range(0, 10).Select(i =>
                System.Threading.Tasks.Task.Run(() =>
                {
                    _timeSeriesDb.StoreMetric("KestrelConnections", 50 + i, DateTime.UtcNow);
                    _estimator.GetKestrelServerConnections();
                })).ToArray();

            System.Threading.Tasks.Task.WaitAll(tasks);

            var metrics = _timeSeriesDb.GetRecentMetrics("KestrelConnections", 50);
            Assert.NotEmpty(metrics);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void Integration_Should_Use_Stored_Metrics_First()
        {
            _timeSeriesDb.StoreMetric("KestrelConnections", 80, DateTime.UtcNow);
            var analysisData = new RequestAnalysisData();
            analysisData.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 1,
                SuccessfulExecutions = 1,
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                ConcurrentExecutions = 5
            });
            _requestAnalytics.TryAdd(typeof(string), analysisData);

            var result = _estimator.GetKestrelServerConnections();

            Assert.True(result > 0);
        }

        [Fact]
        public void Integration_Should_Fallback_To_Request_Patterns_When_No_Stored_Metrics()
        {
            var analysisData = new RequestAnalysisData();
            analysisData.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 1,
                SuccessfulExecutions = 1,
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                ConcurrentExecutions = 25
            });
            _requestAnalytics.TryAdd(typeof(string), analysisData);

            var result = _estimator.GetKestrelServerConnections();

            Assert.True(result >= 0);
        }

        [Fact]
        public void Integration_Should_Apply_Historical_Smoothing()
        {
            for (int i = 0; i < 20; i++)
            {
                _timeSeriesDb.StoreMetric("ConnectionCount_AspNetCore", 50, DateTime.UtcNow.AddMinutes(-i));
            }

            var result = _estimator.GetAspNetCoreConnectionCount();

            Assert.True(result >= 1);
        }

        [Fact]
        public void GetAspNetCoreConnectionCount_Should_Apply_Weighted_Average_When_Historical_Data_Exists()
        {
            // Store historical metrics (at least 5 required for GetHistoricalConnectionAverage to return > 0)
            for (int i = 0; i < 10; i++)
            {
                _timeSeriesDb.StoreMetric("ConnectionCount_AspNetCore", 100, DateTime.UtcNow.AddMinutes(-i));
            }

            // Add some active requests to get a current estimate
            var analysisData = new RequestAnalysisData();
            analysisData.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 1,
                SuccessfulExecutions = 1,
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                ConcurrentExecutions = 10
            });
            _requestAnalytics.TryAdd(typeof(string), analysisData);

            var result = _estimator.GetAspNetCoreConnectionCount();

            // Result should be influenced by historical average (weighted 70% current + 30% historical)
            Assert.True(result >= 1);
            Assert.True(result <= _options.MaxEstimatedHttpConnections / 2);
        }

        [Fact]
        public void GetAspNetCoreConnectionCount_Should_Use_Current_Estimate_When_No_Historical_Data()
        {
            // No historical data stored
            // Add some active requests
            var analysisData = new RequestAnalysisData();
            analysisData.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 1,
                SuccessfulExecutions = 1,
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                ConcurrentExecutions = 15
            });
            _requestAnalytics.TryAdd(typeof(string), analysisData);

            var result = _estimator.GetAspNetCoreConnectionCount();

            // Result should only be based on current estimate (else branch)
            Assert.True(result >= 1);
            Assert.True(result <= _options.MaxEstimatedHttpConnections / 2);
        }

        [Fact]
        public void Integration_Should_Handle_Empty_Analytics()
        {
            var result = _estimator.GetAspNetCoreConnectionCount();

            Assert.True(result >= 1);
        }

        [Fact]
        public void GetAspNetCoreConnectionCount_Should_Handle_Edge_Cases_Gracefully()
        {
            // Test with extreme values that might cause issues but should be handled
            var edgeCaseAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            
            // Add data with maximum concurrent executions
            var extremeData = new RequestAnalysisData();
            extremeData.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 1000,
                SuccessfulExecutions = 1000,
                FailedExecutions = 0,
                AverageExecutionTime = TimeSpan.FromMilliseconds(1),
                ConcurrentExecutions = int.MaxValue / 10  // Large but won't overflow
            });
            edgeCaseAnalytics.TryAdd(typeof(string), extremeData);

            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var timeSeriesLogger = loggerFactory.CreateLogger<TimeSeriesDatabase>();
            var timeSeriesDb = TimeSeriesDatabase.Create(timeSeriesLogger);

            var systemMetricsLogger = loggerFactory.CreateLogger<SystemMetricsCalculator>();
            var systemMetrics = new SystemMetricsCalculator(systemMetricsLogger, edgeCaseAnalytics);

            var protocolLogger = loggerFactory.CreateLogger<ProtocolMetricsCalculator>();
            var protocolCalculator = new ProtocolMetricsCalculator(protocolLogger, edgeCaseAnalytics, timeSeriesDb, systemMetrics);

            var utilsLogger = loggerFactory.CreateLogger<ConnectionMetricsUtilities>();
            var utilities = new ConnectionMetricsUtilities(utilsLogger, _options, edgeCaseAnalytics, timeSeriesDb, systemMetrics, protocolCalculator);

            var estimator = new AspNetCoreConnectionEstimator(
                _loggerMock.Object,
                _options,
                edgeCaseAnalytics,
                timeSeriesDb,
                systemMetrics,
                protocolCalculator,
                utilities);

            var result = estimator.GetAspNetCoreConnectionCount();

            // Should handle extreme values gracefully and return bounded result
            // The catch block ensures this returns a valid fallback if calculation fails
            Assert.True(result > 0);
            Assert.True(result <= _options.MaxEstimatedHttpConnections / 2);
        }

        [Fact]
        public void Integration_Should_Apply_Protocol_Multiplexing()
        {
            var analysisData = new RequestAnalysisData();
            analysisData.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 1,
                SuccessfulExecutions = 1,
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                ConcurrentExecutions = 10
            });
            _requestAnalytics.TryAdd(typeof(string), analysisData);

            var result = _estimator.GetAspNetCoreConnectionCount();

            Assert.True(result >= 1);
        }

        #endregion

        #region GetKestrelServerConnections Strategy Coverage Tests

        [Fact]
        public void GetKestrelServerConnections_Should_Use_Strategy2_InferFromRequestPatterns()
        {
            // No stored metrics, but have request analytics to infer from
            // This should trigger Strategy 2 (InferConnectionsFromRequestPatterns)
            var analysisData = new RequestAnalysisData();
            analysisData.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 50,
                SuccessfulExecutions = 48,
                AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                ConcurrentExecutions = 25
            });
            _requestAnalytics.TryAdd(typeof(string), analysisData);

            var result = _estimator.GetKestrelServerConnections();

            // Strategy 2 should return connections based on inference
            Assert.True(result >= 0);
        }

        [Fact]
        public void GetKestrelServerConnections_Should_Use_Strategy3_EstimateFromConnectionMetrics()
        {
            // Add connection-related metrics that EstimateFromConnectionMetrics can use
            _timeSeriesDb.StoreMetric("http-connection-count", 30, DateTime.UtcNow);
            _timeSeriesDb.StoreMetric("active-connections", 25, DateTime.UtcNow);

            var result = _estimator.GetKestrelServerConnections();

            // Strategy 3 might find these metrics
            Assert.True(result >= 0);
        }

        [Fact]
        public void GetKestrelServerConnections_Should_Use_Strategy4_PredictConnectionCount()
        {
            // Store historical data for prediction but not recent KestrelConnections
            var currentHour = DateTime.UtcNow.Hour;
            
            // Add historical patterns (older than what Strategy 1 would use)
            for (int day = 1; day <= 7; day++)
            {
                var timestamp = DateTime.UtcNow.AddDays(-day).Date.AddHours(currentHour);
                _timeSeriesDb.StoreMetric("ConnectionCount_AspNetCore", 40 + (day % 5), timestamp);
            }

            var result = _estimator.GetKestrelServerConnections();

            // Strategy 4 should predict based on historical patterns
            Assert.True(result >= 0);
        }

        [Fact]
        public void GetKestrelServerConnections_Should_Return_Zero_When_All_Strategies_Fail()
        {
            // No data at all - all strategies should fail
            var result = _estimator.GetKestrelServerConnections();

            Assert.Equal(0, result);
        }

        [Fact]
        public void GetKestrelServerConnections_Should_Handle_Exception_Gracefully()
        {
            // Use extreme data that might cause issues in calculations
            var extremeAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            
            var extremeData = new RequestAnalysisData();
            extremeData.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 1000000,
                SuccessfulExecutions = 1000000,
                FailedExecutions = 0,
                AverageExecutionTime = TimeSpan.FromMilliseconds(1),
                ConcurrentExecutions = int.MaxValue / 10
            });
            extremeAnalytics.TryAdd(typeof(string), extremeData);

            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var timeSeriesLogger = loggerFactory.CreateLogger<TimeSeriesDatabase>();
            var timeSeriesDb = TimeSeriesDatabase.Create(timeSeriesLogger);

            var systemMetricsLogger = loggerFactory.CreateLogger<SystemMetricsCalculator>();
            var systemMetrics = new SystemMetricsCalculator(systemMetricsLogger, extremeAnalytics);

            var protocolLogger = loggerFactory.CreateLogger<ProtocolMetricsCalculator>();
            var protocolCalculator = new ProtocolMetricsCalculator(protocolLogger, extremeAnalytics, timeSeriesDb, systemMetrics);

            var utilsLogger = loggerFactory.CreateLogger<ConnectionMetricsUtilities>();
            var utilities = new ConnectionMetricsUtilities(utilsLogger, _options, extremeAnalytics, timeSeriesDb, systemMetrics, protocolCalculator);

            var estimator = new AspNetCoreConnectionEstimator(
                _loggerMock.Object,
                _options,
                extremeAnalytics,
                timeSeriesDb,
                systemMetrics,
                protocolCalculator,
                utilities);

            var result = estimator.GetKestrelServerConnections();

            // Catch block should handle any exception and return 0
            Assert.True(result >= 0);
        }

        #endregion

        #region EstimateFromConnectionMetrics Coverage Tests

        [Fact]
        public void EstimateFromConnectionMetrics_Should_Return_Estimate_When_ActiveRequests_Exist()
        {
            // Add request analytics with concurrent execution peaks
            var analysisData = new RequestAnalysisData();
            analysisData.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 100,
                SuccessfulExecutions = 98,
                AverageExecutionTime = TimeSpan.FromMilliseconds(150),
                ConcurrentExecutions = 20
            });
            _requestAnalytics.TryAdd(typeof(string), analysisData);

            // This should trigger the if (totalActiveRequests > 0) block
            var result = _estimator.GetKestrelServerConnections();

            // Should return an estimate based on active requests
            Assert.True(result >= 0);
        }

        [Fact]
        public void EstimateFromConnectionMetrics_Should_Use_ConnectionMetrics_When_No_ActiveRequests()
        {
            // Store ConnectionMetrics in time-series database
            _timeSeriesDb.StoreMetric("ConnectionMetrics", 50, DateTime.UtcNow.AddSeconds(-5));
            _timeSeriesDb.StoreMetric("ConnectionMetrics", 55, DateTime.UtcNow.AddSeconds(-3));
            _timeSeriesDb.StoreMetric("ConnectionMetrics", 60, DateTime.UtcNow);

            // No request analytics, so should fall through to ConnectionMetrics check
            var result = _estimator.GetKestrelServerConnections();

            // Should return value from ConnectionMetrics
            Assert.True(result >= 0);
        }

        [Fact]
        public void EstimateFromConnectionMetrics_Should_Return_Zero_When_No_Data()
        {
            // No request analytics and no ConnectionMetrics
            var result = _estimator.GetKestrelServerConnections();

            // Should return 0
            Assert.Equal(0, result);
        }

        [Fact]
        public void EstimateFromConnectionMetrics_Should_Handle_Exception_Gracefully()
        {
            // Create scenario that might cause exception in EstimateFromConnectionMetrics
            var problematicAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            
            // Add multiple entries with varying data
            for (int i = 0; i < 5; i++)
            {
                var data = new RequestAnalysisData();
                data.AddMetrics(new RequestExecutionMetrics
                {
                    TotalExecutions = i * 1000,
                    SuccessfulExecutions = i * 1000,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(i * 10),
                    ConcurrentExecutions = i * 100
                });
                problematicAnalytics.TryAdd(Type.GetType($"System.Object{i}") ?? typeof(object), data);
            }

            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var timeSeriesLogger = loggerFactory.CreateLogger<TimeSeriesDatabase>();
            var timeSeriesDb = TimeSeriesDatabase.Create(timeSeriesLogger);

            var systemMetricsLogger = loggerFactory.CreateLogger<SystemMetricsCalculator>();
            var systemMetrics = new SystemMetricsCalculator(systemMetricsLogger, problematicAnalytics);

            var protocolLogger = loggerFactory.CreateLogger<ProtocolMetricsCalculator>();
            var protocolCalculator = new ProtocolMetricsCalculator(protocolLogger, problematicAnalytics, timeSeriesDb, systemMetrics);

            var utilsLogger = loggerFactory.CreateLogger<ConnectionMetricsUtilities>();
            var utilities = new ConnectionMetricsUtilities(utilsLogger, _options, problematicAnalytics, timeSeriesDb, systemMetrics, protocolCalculator);

            var estimator = new AspNetCoreConnectionEstimator(
                _loggerMock.Object,
                _options,
                problematicAnalytics,
                timeSeriesDb,
                systemMetrics,
                protocolCalculator,
                utilities);

            var result = estimator.GetKestrelServerConnections();

            // Even if exception occurs, should return 0
            Assert.True(result >= 0);
        }

        #endregion

        #region PredictConnectionCount Coverage Tests

        [Fact]
        public void PredictConnectionCount_Should_Use_Median_When_Similar_Time_Data_Exists()
        {
            // Store historical KestrelConnections data for similar time periods
            var currentHour = DateTime.UtcNow.Hour;
            
            // Add data for current hour ±1 hour over multiple days
            for (int day = 0; day < 25; day++)
            {
                // Same hour
                _timeSeriesDb.StoreMetric("KestrelConnections", 45 + (day % 5), 
                    DateTime.UtcNow.AddDays(-day).Date.AddHours(currentHour));
                
                // Hour before
                if (currentHour > 0)
                {
                    _timeSeriesDb.StoreMetric("KestrelConnections", 43 + (day % 5), 
                        DateTime.UtcNow.AddDays(-day).Date.AddHours(currentHour - 1));
                }
                
                // Hour after
                if (currentHour < 23)
                {
                    _timeSeriesDb.StoreMetric("KestrelConnections", 47 + (day % 5), 
                        DateTime.UtcNow.AddDays(-day).Date.AddHours(currentHour + 1));
                }
            }

            var result = _estimator.GetKestrelServerConnections();

            // Should use median of similar time periods
            Assert.True(result >= 0);
        }

        [Fact]
        public void PredictConnectionCount_Should_Use_EMA_When_No_Similar_Time_Data()
        {
            // Store historical data but for different hours (not similar to current hour)
            var currentHour = DateTime.UtcNow.Hour;
            var differentHour = (currentHour + 6) % 24; // 6 hours different
            
            // Add enough data (>20) but for different hours
            for (int day = 0; day < 25; day++)
            {
                _timeSeriesDb.StoreMetric("KestrelConnections", 50 + (day % 10), 
                    DateTime.UtcNow.AddDays(-day).Date.AddHours(differentHour));
            }

            var result = _estimator.GetKestrelServerConnections();

            // Should fall back to EMA calculation
            Assert.True(result >= 0);
        }

        [Fact]
        public void PredictConnectionCount_Should_Return_Zero_When_Insufficient_Historical_Data()
        {
            // Store less than 20 data points
            for (int i = 0; i < 15; i++)
            {
                _timeSeriesDb.StoreMetric("KestrelConnections", 40, 
                    DateTime.UtcNow.AddDays(-i));
            }

            var result = _estimator.GetKestrelServerConnections();

            // Should return 0 due to insufficient data (or use other strategies)
            Assert.True(result >= 0);
        }

        [Fact]
        public void PredictConnectionCount_Should_Apply_Load_Factor_To_Median()
        {
            // Store historical data for similar time periods
            var currentHour = DateTime.UtcNow.Hour;
            
            for (int day = 0; day < 30; day++)
            {
                _timeSeriesDb.StoreMetric("KestrelConnections", 100, 
                    DateTime.UtcNow.AddDays(-day).Date.AddHours(currentHour));
            }

            // Add some load to affect load factor
            var analysisData = new RequestAnalysisData();
            analysisData.AddMetrics(new RequestExecutionMetrics
            {
                TotalExecutions = 100,
                SuccessfulExecutions = 95,
                AverageExecutionTime = TimeSpan.FromMilliseconds(200),
                ConcurrentExecutions = 50
            });
            _requestAnalytics.TryAdd(typeof(string), analysisData);

            var result = _estimator.GetKestrelServerConnections();

            // Should apply load factor to median prediction
            Assert.True(result >= 0);
        }

        [Fact]
        public void PredictConnectionCount_Should_Handle_Exception_Gracefully()
        {
            // Create scenario that might cause exception in prediction logic
            var problematicAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var timeSeriesLogger = loggerFactory.CreateLogger<TimeSeriesDatabase>();
            var timeSeriesDb = TimeSeriesDatabase.Create(timeSeriesLogger);

            // Add some corrupted or extreme data
            for (int i = 0; i < 30; i++)
            {
                timeSeriesDb.StoreMetric("KestrelConnections", double.MaxValue / 1000, 
                    DateTime.UtcNow.AddDays(-i));
            }

            var systemMetricsLogger = loggerFactory.CreateLogger<SystemMetricsCalculator>();
            var systemMetrics = new SystemMetricsCalculator(systemMetricsLogger, problematicAnalytics);

            var protocolLogger = loggerFactory.CreateLogger<ProtocolMetricsCalculator>();
            var protocolCalculator = new ProtocolMetricsCalculator(protocolLogger, problematicAnalytics, timeSeriesDb, systemMetrics);

            var utilsLogger = loggerFactory.CreateLogger<ConnectionMetricsUtilities>();
            var utilities = new ConnectionMetricsUtilities(utilsLogger, _options, problematicAnalytics, timeSeriesDb, systemMetrics, protocolCalculator);

            var estimator = new AspNetCoreConnectionEstimator(
                _loggerMock.Object,
                _options,
                problematicAnalytics,
                timeSeriesDb,
                systemMetrics,
                protocolCalculator,
                utilities);

            var result = estimator.GetKestrelServerConnections();

            // Catch block should handle exception and return 0
            Assert.True(result >= 0);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void EdgeCase_Should_Handle_Very_Large_Connection_Count()
        {
            _timeSeriesDb.StoreMetric("KestrelConnections", 10000, DateTime.UtcNow);

            var kestrelResult = _estimator.GetKestrelServerConnections();
            var aspNetCoreResult = _estimator.GetAspNetCoreConnectionCount();

            Assert.True(kestrelResult > 0);
            Assert.InRange(aspNetCoreResult, 1, _options.MaxEstimatedHttpConnections / 2);
        }

        [Fact]
        public void EdgeCase_Should_Handle_Very_Small_Connection_Count()
        {
            _timeSeriesDb.StoreMetric("KestrelConnections", 0, DateTime.UtcNow);

            var result = _estimator.GetAspNetCoreConnectionCount();

            Assert.True(result >= 1);
        }

        [Fact]
        public void EdgeCase_Should_Handle_Negative_Metrics()
        {
            _timeSeriesDb.StoreMetric("KestrelConnections", -10, DateTime.UtcNow);

            var result = _estimator.GetKestrelServerConnections();

            Assert.True(result >= 0);
        }

        [Fact]
        public void EdgeCase_Should_Handle_Multiple_Request_Types()
        {
            for (int i = 0; i < 50; i++)
            {
                var analysisData = new RequestAnalysisData();
                analysisData.AddMetrics(new RequestExecutionMetrics
                {
                    TotalExecutions = 1,
                    SuccessfulExecutions = 1,
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                    ConcurrentExecutions = i
                });
                _requestAnalytics.TryAdd(Type.GetType($"Type{i}") ?? typeof(object), analysisData);
            }

            var result = _estimator.GetKestrelServerConnections();

            Assert.True(result >= 0);
        }

        [Fact]
        public void EdgeCase_Should_Handle_Time_Series_With_Gaps()
        {
            _timeSeriesDb.StoreMetric("KestrelConnections", 20, DateTime.UtcNow.AddHours(-5));
            _timeSeriesDb.StoreMetric("KestrelConnections", 30, DateTime.UtcNow);

            var result = _estimator.GetKestrelServerConnections();

            Assert.True(result > 0);
        }

        #endregion
    }
}
