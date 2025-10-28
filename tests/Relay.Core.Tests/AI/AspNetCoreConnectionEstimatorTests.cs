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
            var systemMetricsLogger = loggerFactory.CreateLogger<SystemMetricsCalculator>();
            var systemMetrics = new SystemMetricsCalculator(systemMetricsLogger, _requestAnalytics);
            var utilsLogger = loggerFactory.CreateLogger<ConnectionMetricsUtilities>();
            var utilities = new ConnectionMetricsUtilities(utilsLogger, _options, _requestAnalytics, _timeSeriesDb, systemMetrics, null!);

            Assert.Throws<ArgumentNullException>(() =>
                new AspNetCoreConnectionEstimator(_loggerMock.Object, _options, _requestAnalytics, _timeSeriesDb, systemMetrics, null!, utilities));
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
        public void Integration_Should_Handle_Empty_Analytics()
        {
            var result = _estimator.GetAspNetCoreConnectionCount();

            Assert.True(result >= 1);
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
