using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Strategies;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class DataCleanupManagerTests
    {
        private readonly ILogger<DataCleanupManager> _logger;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
        private readonly ConcurrentDictionary<Type, CachingAnalysisData> _cachingAnalytics;
        private readonly ConcurrentQueue<PredictionResult> _recentPredictions;

        public DataCleanupManagerTests()
        {
            _logger = NullLogger<DataCleanupManager>.Instance;
            _requestAnalytics = new ConcurrentDictionary<Type, RequestAnalysisData>();
            _cachingAnalytics = new ConcurrentDictionary<Type, CachingAnalysisData>();
            _recentPredictions = new ConcurrentQueue<PredictionResult>();
        }

        private DataCleanupManager CreateManager()
        {
            return new DataCleanupManager(_logger, _requestAnalytics, _cachingAnalytics, _recentPredictions);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_Should_Initialize_With_Valid_Parameters()
        {
            // Arrange & Act
            var manager = CreateManager();

            // Assert
            Assert.NotNull(manager);
        }

        [Fact]
        public void Constructor_Should_Throw_When_Logger_Is_Null()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new DataCleanupManager(null!, _requestAnalytics, _cachingAnalytics, _recentPredictions));

            Assert.Equal("logger", exception.ParamName);
        }

        [Fact]
        public void Constructor_Should_Throw_When_RequestAnalytics_Is_Null()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new DataCleanupManager(_logger, null!, _cachingAnalytics, _recentPredictions));

            Assert.Equal("requestAnalytics", exception.ParamName);
        }

        [Fact]
        public void Constructor_Should_Throw_When_CachingAnalytics_Is_Null()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new DataCleanupManager(_logger, _requestAnalytics, null!, _recentPredictions));

            Assert.Equal("cachingAnalytics", exception.ParamName);
        }

        [Fact]
        public void Constructor_Should_Throw_When_RecentPredictions_Is_Null()
        {
            // Arrange, Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new DataCleanupManager(_logger, _requestAnalytics, _cachingAnalytics, null!));

            Assert.Equal("recentPredictions", exception.ParamName);
        }

        #endregion

        #region CleanupOldData Tests

        [Fact]
        public void CleanupOldData_Should_Execute_Successfully_With_Empty_Collections()
        {
            // Arrange
            var manager = CreateManager();

            // Act - Should not throw
            var exception = Record.Exception(() => manager.CleanupOldData());

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CleanupOldData_Should_Remove_Old_Request_Analytics()
        {
            // Arrange
            var manager = CreateManager();
            var oldData = new RequestAnalysisData();
            var oldTime = DateTime.UtcNow.AddHours(-48); // Older than 24 hours

            // Use reflection to set LastActivityTime
            var property = typeof(RequestAnalysisData).GetProperty("LastActivityTime");
            property?.SetValue(oldData, oldTime);

            _requestAnalytics.TryAdd(typeof(string), oldData);
            var initialCount = _requestAnalytics.Count;

            // Act
            manager.CleanupOldData();

            // Assert
            Assert.True(_requestAnalytics.Count < initialCount || _requestAnalytics.Count == 0);
        }

        [Fact]
        public void CleanupOldData_Should_Keep_Recent_Request_Analytics()
        {
            // Arrange
            var manager = CreateManager();
            var recentData = new RequestAnalysisData();

            // LastActivityTime defaults to DateTime.UtcNow in constructor, so it's recent
            _requestAnalytics.TryAdd(typeof(int), recentData);
            var initialCount = _requestAnalytics.Count;

            // Act
            manager.CleanupOldData();

            // Assert
            Assert.Equal(initialCount, _requestAnalytics.Count);
        }

        [Fact]
        public void CleanupOldData_Should_Remove_Old_Caching_Analytics()
        {
            // Arrange
            var manager = CreateManager();
            var oldData = new CachingAnalysisData();
            var oldTime = DateTime.UtcNow.AddHours(-48);

            // Use reflection to set LastAccessTime
            var property = typeof(CachingAnalysisData).GetProperty("LastAccessTime");
            property?.SetValue(oldData, oldTime);

            _cachingAnalytics.TryAdd(typeof(string), oldData);
            var initialCount = _cachingAnalytics.Count;

            // Act
            manager.CleanupOldData();

            // Assert
            Assert.True(_cachingAnalytics.Count < initialCount || _cachingAnalytics.Count == 0);
        }

        [Fact]
        public void CleanupOldData_Should_Keep_Recent_Caching_Analytics()
        {
            // Arrange
            var manager = CreateManager();
            var recentData = new CachingAnalysisData();

            // LastAccessTime defaults to DateTime.UtcNow in constructor
            _cachingAnalytics.TryAdd(typeof(int), recentData);
            var initialCount = _cachingAnalytics.Count;

            // Act
            manager.CleanupOldData();

            // Assert
            Assert.Equal(initialCount, _cachingAnalytics.Count);
        }

        [Fact]
        public void CleanupOldData_Should_Remove_Old_Prediction_Results()
        {
            // Arrange
            var manager = CreateManager();
            var oldPrediction = new PredictionResult
            {
                RequestType = typeof(string),
                PredictedStrategies = new[] { OptimizationStrategy.EnableCaching },
                ActualImprovement = TimeSpan.FromMilliseconds(100),
                Timestamp = DateTime.UtcNow.AddHours(-48),
                Metrics = new RequestExecutionMetrics
                {
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                    TotalExecutions = 10,
                    SuccessfulExecutions = 9,
                    FailedExecutions = 1
                }
            };

            _recentPredictions.Enqueue(oldPrediction);
            var initialCount = _recentPredictions.Count;

            // Act
            manager.CleanupOldData();

            // Assert
            Assert.True(_recentPredictions.Count < initialCount);
        }

        [Fact]
        public void CleanupOldData_Should_Keep_Recent_Prediction_Results()
        {
            // Arrange
            var manager = CreateManager();
            var recentPrediction = new PredictionResult
            {
                RequestType = typeof(string),
                PredictedStrategies = new[] { OptimizationStrategy.EnableCaching },
                ActualImprovement = TimeSpan.FromMilliseconds(100),
                Timestamp = DateTime.UtcNow,
                Metrics = new RequestExecutionMetrics
                {
                    AverageExecutionTime = TimeSpan.FromMilliseconds(100),
                    TotalExecutions = 10,
                    SuccessfulExecutions = 9,
                    FailedExecutions = 1
                }
            };

            _recentPredictions.Enqueue(recentPrediction);
            var initialCount = _recentPredictions.Count;

            // Act
            manager.CleanupOldData();

            // Assert
            Assert.Equal(initialCount, _recentPredictions.Count);
        }

        [Fact]
        public void CleanupOldData_Should_Handle_Mixed_Old_And_Recent_Data()
        {
            // Arrange
            var manager = CreateManager();

            // Add old request analytics
            var oldRequestData = new RequestAnalysisData();
            var oldRequestTime = DateTime.UtcNow.AddHours(-48);
            var requestProperty = typeof(RequestAnalysisData).GetProperty("LastActivityTime");
            requestProperty?.SetValue(oldRequestData, oldRequestTime);
            _requestAnalytics.TryAdd(typeof(string), oldRequestData);

            // Add recent request analytics
            var recentRequestData = new RequestAnalysisData();
            _requestAnalytics.TryAdd(typeof(int), recentRequestData);

            // Add old predictions
            var oldPrediction = new PredictionResult
            {
                RequestType = typeof(string),
                PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                Timestamp = DateTime.UtcNow.AddHours(-48),
                Metrics = new RequestExecutionMetrics()
            };
            _recentPredictions.Enqueue(oldPrediction);

            // Add recent predictions
            var recentPrediction = new PredictionResult
            {
                RequestType = typeof(int),
                PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                Timestamp = DateTime.UtcNow,
                Metrics = new RequestExecutionMetrics()
            };
            _recentPredictions.Enqueue(recentPrediction);

            // Act
            manager.CleanupOldData();

            // Assert
            Assert.True(_requestAnalytics.Count > 0); // Recent data should remain
            Assert.True(_recentPredictions.Count > 0); // Recent predictions should remain
        }

        [Fact]
        public void CleanupOldData_Should_Be_Callable_Multiple_Times()
        {
            // Arrange
            var manager = CreateManager();

            // Act & Assert - Should not throw
            var exception = Record.Exception(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    manager.CleanupOldData();
                }
            });

            Assert.Null(exception);
        }

        [Fact]
        public void CleanupOldData_Should_Handle_Large_Collections()
        {
            // Arrange
            var manager = CreateManager();

            // Add many items
            for (int i = 0; i < 1000; i++)
            {
                var data = new RequestAnalysisData();
                _requestAnalytics.TryAdd(Type.GetType($"System.Int{i}") ?? typeof(int), data);
            }

            // Act
            var exception = Record.Exception(() => manager.CleanupOldData());

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CleanupOldData_Should_Handle_Empty_Predictions_Queue()
        {
            // Arrange
            var manager = CreateManager();

            // Act
            var exception = Record.Exception(() => manager.CleanupOldData());

            // Assert
            Assert.Null(exception);
            Assert.Empty(_recentPredictions);
        }

        [Fact]
        public void CleanupOldData_Should_Handle_All_Old_Data()
        {
            // Arrange
            var manager = CreateManager();
            var oldTime = DateTime.UtcNow.AddHours(-48);

            // Add all old data
            var oldRequestData = new RequestAnalysisData();
            var requestProperty = typeof(RequestAnalysisData).GetProperty("LastActivityTime");
            requestProperty?.SetValue(oldRequestData, oldTime);
            _requestAnalytics.TryAdd(typeof(string), oldRequestData);

            var oldCachingData = new CachingAnalysisData();
            var cachingProperty = typeof(CachingAnalysisData).GetProperty("LastAccessTime");
            cachingProperty?.SetValue(oldCachingData, oldTime);
            _cachingAnalytics.TryAdd(typeof(string), oldCachingData);

            var oldPrediction = new PredictionResult
            {
                RequestType = typeof(string),
                PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                Timestamp = oldTime,
                Metrics = new RequestExecutionMetrics()
            };
            _recentPredictions.Enqueue(oldPrediction);

            // Act
            manager.CleanupOldData();

            // Assert - All old data should be removed
            Assert.True(_requestAnalytics.Count == 0);
            Assert.True(_cachingAnalytics.Count == 0);
            Assert.True(_recentPredictions.Count == 0);
        }

        #endregion

        #region Concurrent Access Tests

        [Fact]
        public void CleanupOldData_Should_Handle_Concurrent_Calls()
        {
            // Arrange
            var manager = CreateManager();
            var exceptions = new ConcurrentBag<Exception>();

            // Add some test data
            for (int i = 0; i < 10; i++)
            {
                _requestAnalytics.TryAdd(Type.GetType($"System.String{i}") ?? typeof(string), new RequestAnalysisData());
                _cachingAnalytics.TryAdd(Type.GetType($"System.Int{i}") ?? typeof(int), new CachingAnalysisData());
            }

            // Act
            System.Threading.Tasks.Parallel.For(0, 10, i =>
            {
                try
                {
                    manager.CleanupOldData();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Assert
            Assert.Empty(exceptions);
        }

        [Fact]
        public void CleanupOldData_Should_Be_Thread_Safe_With_Concurrent_Additions()
        {
            // Arrange
            var manager = CreateManager();
            var exceptions = new ConcurrentBag<Exception>();

            // Act - Add data and cleanup concurrently
            System.Threading.Tasks.Parallel.Invoke(
                () =>
                {
                    for (int i = 0; i < 50; i++)
                    {
                        _requestAnalytics.TryAdd(Type.GetType($"System.Type{i}") ?? typeof(object), new RequestAnalysisData());
                        System.Threading.Thread.Sleep(1);
                    }
                },
                () =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        try
                        {
                            manager.CleanupOldData();
                            System.Threading.Thread.Sleep(5);
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }
            );

            // Assert
            Assert.Empty(exceptions);
        }

        #endregion

        #region Edge Case Tests

        [Fact]
        public void CleanupOldData_Should_Handle_Null_Property_Values()
        {
            // Arrange
            var manager = CreateManager();
            
            // Add data with default values (which shouldn't be null)
            _requestAnalytics.TryAdd(typeof(string), new RequestAnalysisData());
            _cachingAnalytics.TryAdd(typeof(int), new CachingAnalysisData());

            // Act
            var exception = Record.Exception(() => manager.CleanupOldData());

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CleanupOldData_Should_Handle_Boundary_Time()
        {
            // Arrange
            var manager = CreateManager();
            var boundaryTime = DateTime.UtcNow.AddHours(-24); // Exactly at cutoff

            var data = new RequestAnalysisData();
            var property = typeof(RequestAnalysisData).GetProperty("LastActivityTime");
            property?.SetValue(data, boundaryTime);
            _requestAnalytics.TryAdd(typeof(string), data);

            // Act
            var exception = Record.Exception(() => manager.CleanupOldData());

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void CleanupOldData_Should_Handle_Future_Timestamps()
        {
            // Arrange
            var manager = CreateManager();
            var futureTime = DateTime.UtcNow.AddHours(24); // Future time

            var data = new RequestAnalysisData();
            var property = typeof(RequestAnalysisData).GetProperty("LastActivityTime");
            property?.SetValue(data, futureTime);
            _requestAnalytics.TryAdd(typeof(string), data);

            var initialCount = _requestAnalytics.Count;

            // Act
            manager.CleanupOldData();

            // Assert - Future data should be kept
            Assert.Equal(initialCount, _requestAnalytics.Count);
        }

        [Fact]
        public void CleanupOldData_Should_Handle_MinValue_Timestamps()
        {
            // Arrange
            var manager = CreateManager();

            var data = new RequestAnalysisData();
            var property = typeof(RequestAnalysisData).GetProperty("LastActivityTime");
            property?.SetValue(data, DateTime.MinValue);
            _requestAnalytics.TryAdd(typeof(string), data);

            // Act
            var exception = Record.Exception(() => manager.CleanupOldData());

            // Assert
            Assert.Null(exception);
            Assert.Empty(_requestAnalytics); // MinValue should be cleaned up
        }

        [Fact]
        public void CleanupOldData_Should_Handle_MaxValue_Timestamps()
        {
            // Arrange
            var manager = CreateManager();

            var data = new RequestAnalysisData();
            var property = typeof(RequestAnalysisData).GetProperty("LastActivityTime");
            property?.SetValue(data, DateTime.MaxValue);
            _requestAnalytics.TryAdd(typeof(string), data);

            var initialCount = _requestAnalytics.Count;

            // Act
            manager.CleanupOldData();

            // Assert - MaxValue should be kept
            Assert.Equal(initialCount, _requestAnalytics.Count);
        }

        #endregion

        #region Prediction Results Tests

        [Fact]
        public void CleanupOldData_Should_Remove_Multiple_Old_Predictions()
        {
            // Arrange
            var manager = CreateManager();
            var oldTime = DateTime.UtcNow.AddHours(-48);

            // Add multiple old predictions
            for (int i = 0; i < 10; i++)
            {
                var oldPrediction = new PredictionResult
                {
                    RequestType = typeof(string),
                    PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                    Timestamp = oldTime.AddMinutes(i),
                    Metrics = new RequestExecutionMetrics()
                };
                _recentPredictions.Enqueue(oldPrediction);
            }

            // Add a recent one at the end
            var recentPrediction = new PredictionResult
            {
                RequestType = typeof(int),
                PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                Timestamp = DateTime.UtcNow,
                Metrics = new RequestExecutionMetrics()
            };
            _recentPredictions.Enqueue(recentPrediction);

            // Act
            manager.CleanupOldData();

            // Assert
            Assert.Single(_recentPredictions); // Only recent one should remain
        }

        [Fact]
        public void CleanupOldData_Should_Stop_At_First_Recent_Prediction()
        {
            // Arrange
            var manager = CreateManager();
            var oldTime = DateTime.UtcNow.AddHours(-48);

            // Add a recent prediction first
            var recentPrediction = new PredictionResult
            {
                RequestType = typeof(int),
                PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                Timestamp = DateTime.UtcNow,
                Metrics = new RequestExecutionMetrics()
            };
            _recentPredictions.Enqueue(recentPrediction);

            // Add old predictions after
            for (int i = 0; i < 5; i++)
            {
                var oldPrediction = new PredictionResult
                {
                    RequestType = typeof(string),
                    PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                    Timestamp = oldTime,
                    Metrics = new RequestExecutionMetrics()
                };
                _recentPredictions.Enqueue(oldPrediction);
            }

            var initialCount = _recentPredictions.Count;

            // Act
            manager.CleanupOldData();

            // Assert - Should stop at first recent, keeping all after it
            Assert.Equal(initialCount, _recentPredictions.Count);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void CleanupOldData_Should_Clean_All_Collection_Types()
        {
            // Arrange
            var manager = CreateManager();
            var oldTime = DateTime.UtcNow.AddHours(-48);

            // Add old data to all collections
            var oldRequestData = new RequestAnalysisData();
            var requestProperty = typeof(RequestAnalysisData).GetProperty("LastActivityTime");
            requestProperty?.SetValue(oldRequestData, oldTime);
            _requestAnalytics.TryAdd(typeof(string), oldRequestData);

            var oldCachingData = new CachingAnalysisData();
            var cachingProperty = typeof(CachingAnalysisData).GetProperty("LastAccessTime");
            cachingProperty?.SetValue(oldCachingData, oldTime);
            _cachingAnalytics.TryAdd(typeof(double), oldCachingData);

            var oldPrediction = new PredictionResult
            {
                RequestType = typeof(bool),
                PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                Timestamp = oldTime,
                Metrics = new RequestExecutionMetrics()
            };
            _recentPredictions.Enqueue(oldPrediction);

            // Act
            manager.CleanupOldData();

            // Assert - All should be cleaned
            Assert.Empty(_requestAnalytics);
            Assert.Empty(_cachingAnalytics);
            Assert.Empty(_recentPredictions);
        }

        [Fact]
        public void CleanupOldData_Should_Preserve_All_Recent_Data()
        {
            // Arrange
            var manager = CreateManager();

            // Add recent data to all collections
            _requestAnalytics.TryAdd(typeof(string), new RequestAnalysisData());
            _requestAnalytics.TryAdd(typeof(int), new RequestAnalysisData());
            _cachingAnalytics.TryAdd(typeof(double), new CachingAnalysisData());
            _cachingAnalytics.TryAdd(typeof(bool), new CachingAnalysisData());

            for (int i = 0; i < 5; i++)
            {
                var prediction = new PredictionResult
                {
                    RequestType = typeof(string),
                    PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                    Timestamp = DateTime.UtcNow,
                    Metrics = new RequestExecutionMetrics()
                };
                _recentPredictions.Enqueue(prediction);
            }

            var requestCount = _requestAnalytics.Count;
            var cachingCount = _cachingAnalytics.Count;
            var predictionCount = _recentPredictions.Count;

            // Act
            manager.CleanupOldData();

            // Assert - All should be preserved
            Assert.Equal(requestCount, _requestAnalytics.Count);
            Assert.Equal(cachingCount, _cachingAnalytics.Count);
            Assert.Equal(predictionCount, _recentPredictions.Count);
        }

        #endregion

        #region Performance Tests

        [Fact]
        public void CleanupOldData_Should_Complete_Quickly_With_Many_Items()
        {
            // Arrange
            var manager = CreateManager();

            // Add many items
            for (int i = 0; i < 100; i++)
            {
                _requestAnalytics.TryAdd(Type.GetType($"System.Type{i}") ?? typeof(object), new RequestAnalysisData());
                _cachingAnalytics.TryAdd(Type.GetType($"System.Cache{i}") ?? typeof(object), new CachingAnalysisData());
                
                var prediction = new PredictionResult
                {
                    RequestType = typeof(string),
                    PredictedStrategies = Array.Empty<OptimizationStrategy>(),
                    Timestamp = DateTime.UtcNow.AddMinutes(-i),
                    Metrics = new RequestExecutionMetrics()
                };
                _recentPredictions.Enqueue(prediction);
            }

            var startTime = DateTime.UtcNow;

            // Act
            manager.CleanupOldData();

            var duration = DateTime.UtcNow - startTime;

            // Assert - Should complete in reasonable time (< 1 second)
            Assert.True(duration.TotalSeconds < 1);
        }

        #endregion
    }
}
