using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI;
using Relay.Core.AI.Optimization.Models;
using Xunit;

namespace Relay.Core.Tests.AI.Training
{
    /// <summary>
    /// Integration tests for the complete AI training pipeline
    /// </summary>
    public class AITrainingIntegrationTests : IDisposable
    {
        private readonly Mock<ILogger<DefaultAIModelTrainer>> _trainerLogger;
        private readonly Mock<ILogger<MLNetModelManager>> _managerLogger;
        private readonly string _testModelPath;
        private DefaultAIModelTrainer? _trainer;
        private MLNetModelManager? _manager;

        public AITrainingIntegrationTests()
        {
            _trainerLogger = new Mock<ILogger<DefaultAIModelTrainer>>();
            _managerLogger = new Mock<ILogger<MLNetModelManager>>();
            _testModelPath = Path.Combine(Path.GetTempPath(), $"RelayAIIntegrationTests_{Guid.NewGuid()}");
        }

        [Fact]
        public async Task EndToEnd_TrainWithProgressTracking_ReportsAllPhases()
        {
            // Arrange
            var trainingData = CreateComprehensiveTrainingData();
            var progressReports = new System.Collections.Generic.List<TrainingProgress>();
            _trainer = new DefaultAIModelTrainer(_trainerLogger.Object);

            // Act
            await _trainer.TrainModelAsync(trainingData, progress =>
            {
                progressReports.Add(progress);
            });

            // Assert: All phases should be reported
            var reportedPhases = progressReports.Select(p => p.Phase).Distinct().ToList();

            Assert.Contains(TrainingPhase.Validation, reportedPhases);
            Assert.Contains(TrainingPhase.PerformanceModels, reportedPhases);
            Assert.Contains(TrainingPhase.OptimizationClassifiers, reportedPhases);
            Assert.Contains(TrainingPhase.AnomalyDetection, reportedPhases);
            Assert.Contains(TrainingPhase.Forecasting, reportedPhases);
            Assert.Contains(TrainingPhase.Statistics, reportedPhases);
            Assert.Contains(TrainingPhase.Completed, reportedPhases);

            // Progress should reach 100%
            Assert.Equal(100, progressReports.Last().ProgressPercentage);
            Assert.Contains("completed", progressReports.Last().StatusMessage, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task EndToEnd_ModelPersistence_SurvivesRestart()
        {
            // Arrange
            var trainingData = CreateComprehensiveTrainingData();

            // Act 1: Train and save models
            _manager = new MLNetModelManager(_managerLogger.Object, _testModelPath);
            _manager.TrainRegressionModel(trainingData.ExecutionHistory!
                .Select(e => new PerformanceData
                {
                    ExecutionTime = (float)e.AverageExecutionTime.TotalMilliseconds,
                    ConcurrencyLevel = e.ConcurrentExecutions,
                    MemoryUsage = e.MemoryUsage,
                    DatabaseCalls = e.DatabaseCalls,
                    ExternalApiCalls = e.ExternalApiCalls,
                    OptimizationGain = 0.7f
                })
                .ToList());

            var testData = new PerformanceData
            {
                ExecutionTime = 150f,
                ConcurrencyLevel = 5,
                MemoryUsage = 50 * 1024 * 1024,
                DatabaseCalls = 3,
                ExternalApiCalls = 1
            };

            var prediction1 = _manager.PredictOptimizationGain(testData);
            _manager.Dispose();
            _manager = null;

            // Act 2: Load models in new manager instance
            _manager = new MLNetModelManager(_managerLogger.Object, _testModelPath);
            var prediction2 = _manager.PredictOptimizationGain(testData);

            // Assert: Predictions should be consistent (loaded model works)
            Assert.InRange(Math.Abs(prediction1 - prediction2), 0f, 0.01f); // Allow small floating point difference
        }

        [Fact]
        public async Task EndToEnd_FeatureImportance_AfterTraining()
        {
            // Arrange
            var trainingData = CreateComprehensiveTrainingData();
            _manager = new MLNetModelManager(_managerLogger.Object, _testModelPath);

            // Act: Train regression model
            _manager.TrainRegressionModel(trainingData.ExecutionHistory!
                .Select(e => new PerformanceData
                {
                    ExecutionTime = (float)e.AverageExecutionTime.TotalMilliseconds,
                    ConcurrencyLevel = e.ConcurrentExecutions,
                    MemoryUsage = e.MemoryUsage,
                    DatabaseCalls = e.DatabaseCalls,
                    ExternalApiCalls = e.ExternalApiCalls,
                    OptimizationGain = CalculateOptimizationGain(e)
                })
                .ToList());

            var importance = _manager.GetFeatureImportance();

            // Assert: Feature importance should be calculated
            Assert.NotNull(importance);
            Assert.Equal(5, importance.Count);
            Assert.Contains("ExecutionTime", importance.Keys);
            Assert.Contains("ConcurrencyLevel", importance.Keys);
            Assert.Contains("MemoryUsage", importance.Keys);
            Assert.Contains("DatabaseCalls", importance.Keys);
            Assert.Contains("ExternalApiCalls", importance.Keys);

            // All importance scores should be normalized (sum to ~1.0)
            var total = importance.Values.Sum();
            Assert.InRange(total, 0.95, 1.05);

            // All scores should be between 0 and 1
            Assert.All(importance.Values, score => Assert.InRange(score, 0f, 1f));
        }

        [Fact]
        public void ClearPersistedModels_RemovesAllModelFiles()
        {
            // Arrange
            _manager = new MLNetModelManager(_managerLogger.Object, _testModelPath);
            var trainingData = CreateSimpleTrainingData();

            _manager.TrainRegressionModel(trainingData);
            Assert.True(_manager.HasPersistedModels());

            // Act
            _manager.ClearPersistedModels();

            // Assert
            Assert.False(_manager.HasPersistedModels());
        }

        // Helper methods
        private AITrainingData CreateComprehensiveTrainingData()
        {
            var random = new Random(42);

            return new AITrainingData
            {
                ExecutionHistory = Enumerable.Range(0, 50)
                    .Select(i =>
                    {
                        var total = 100 + random.Next(900);
                        var successful = (long)(total * (0.9 + random.NextDouble() * 0.1));
                        return new RequestExecutionMetrics
                        {
                            TotalExecutions = total,
                            SuccessfulExecutions = successful,
                            FailedExecutions = total - successful,
                            AverageExecutionTime = TimeSpan.FromMilliseconds(50 + random.Next(200)),
                            P95ExecutionTime = TimeSpan.FromMilliseconds(100 + random.Next(300)),
                            ConcurrentExecutions = random.Next(1, 10),
                            MemoryUsage = random.Next(10, 100) * 1024 * 1024,
                            DatabaseCalls = random.Next(0, 10),
                            ExternalApiCalls = random.Next(0, 5),
                            LastExecution = DateTime.UtcNow.AddMinutes(-50 + i)
                        };
                    })
                    .ToArray(),

                OptimizationHistory = Enumerable.Range(0, 30)
                    .Select(i => new AIOptimizationResult
                    {
                        Strategy = OptimizationStrategy.EnableCaching,
                        Success = random.NextDouble() > 0.2,
                        ExecutionTime = TimeSpan.FromMilliseconds(50 + random.Next(200)),
                        PerformanceImprovement = random.NextDouble() * 0.3,
                        Timestamp = DateTime.UtcNow.AddMinutes(-30 + i)
                    })
                    .ToArray(),

                SystemLoadHistory = Enumerable.Range(0, 100)
                    .Select(i => new SystemLoadMetrics
                    {
                        Timestamp = DateTime.UtcNow.AddMinutes(-100 + i),
                        CpuUtilization = 0.3 + random.NextDouble() * 0.5,
                        MemoryUtilization = 0.4 + random.NextDouble() * 0.4,
                        ThroughputPerSecond = 100 + random.Next(200)
                    })
                    .ToArray()
            };
        }

        private System.Collections.Generic.List<PerformanceData> CreateSimpleTrainingData()
        {
            return Enumerable.Range(0, 20)
                .Select(i => new PerformanceData
                {
                    ExecutionTime = 50f + i * 10,
                    ConcurrencyLevel = 1 + (i % 5),
                    MemoryUsage = 10 * 1024 * 1024,
                    DatabaseCalls = i % 3,
                    ExternalApiCalls = i % 2,
                    OptimizationGain = 0.5f + (i % 10) * 0.05f
                })
                .ToList();
        }

        private float CalculateOptimizationGain(RequestExecutionMetrics metrics)
        {
            var successRate = (float)metrics.SuccessRate;
            var avgTime = (float)metrics.AverageExecutionTime.TotalMilliseconds;

            var timeBasedGain = Math.Min(1.0f, 1000.0f / Math.Max(avgTime, 10f));
            var reliabilityGain = successRate;

            return (timeBasedGain * 0.6f) + (reliabilityGain * 0.4f);
        }

        public void Dispose()
        {
            _trainer?.Dispose();
            _manager?.Dispose();

            // Clean up test directory
            if (Directory.Exists(_testModelPath))
            {
                try
                {
                    Directory.Delete(_testModelPath, recursive: true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
}
