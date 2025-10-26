using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Relay.Core.AI;
using Relay.Core.AI.Models;
using Relay.Core.AI.Optimization.Strategies;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class DataCleanupManagerConcurrencyTests
    {
        private readonly ILogger<DataCleanupManager> _logger;
        private readonly ConcurrentDictionary<Type, RequestAnalysisData> _requestAnalytics;
        private readonly ConcurrentDictionary<Type, CachingAnalysisData> _cachingAnalytics;
        private readonly ConcurrentQueue<PredictionResult> _recentPredictions;

        public DataCleanupManagerConcurrencyTests()
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
    }
}