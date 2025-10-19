using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using Relay.Core.AI.Optimization.Models;
using Xunit;

namespace Relay.Core.Tests.AI.Training
{
    public class MLNetModelManagerTests : IDisposable
    {
        private readonly Mock<ILogger<MLNetModelManager>> _mockLogger;
        private readonly string _testModelPath;
        private MLNetModelManager? _manager;

        public MLNetModelManagerTests()
        {
            _mockLogger = new Mock<ILogger<MLNetModelManager>>();
            _testModelPath = Path.Combine(Path.GetTempPath(), $"RelayAITests_{Guid.NewGuid()}");
        }

        [Fact]
        public void Constructor_CreatesStorageDirectory()
        {
            // Act
            _manager = new MLNetModelManager(_mockLogger.Object, _testModelPath);

            // Assert
            Assert.True(Directory.Exists(_testModelPath));
        }

        [Fact]
        public void Constructor_LoadsExistingModels_WhenAvailable()
        {
            // Arrange - Train and save models first
            _manager = new MLNetModelManager(_mockLogger.Object, _testModelPath);
            var trainingData = CreateSamplePerformanceData(20);
            _manager.TrainRegressionModel(trainingData);
            _manager.Dispose();

            // Act - Create new manager (should load existing models)
            _manager = new MLNetModelManager(_mockLogger.Object, _testModelPath);

            // Assert - Should be able to predict without training
            var prediction = _manager.PredictOptimizationGain(trainingData.First());
            Assert.InRange(prediction, 0f, 1f);
        }

        [Fact]
        public void HasPersistedModels_ReturnsFalse_WhenNoModelsExist()
        {
            // Arrange
            _manager = new MLNetModelManager(_mockLogger.Object, _testModelPath);

            // Act
            var hasModels = _manager.HasPersistedModels();

            // Assert
            Assert.False(hasModels);
        }

        [Fact]
        public void HasPersistedModels_ReturnsTrue_AfterTraining()
        {
            // Arrange
            _manager = new MLNetModelManager(_mockLogger.Object, _testModelPath);
            var trainingData = CreateSamplePerformanceData(20);

            // Act
            _manager.TrainRegressionModel(trainingData);
            var hasModels = _manager.HasPersistedModels();

            // Assert
            Assert.True(hasModels);
        }

        [Fact]
        public void ClearPersistedModels_DeletesAllModels()
        {
            // Arrange
            _manager = new MLNetModelManager(_mockLogger.Object, _testModelPath);
            var trainingData = CreateSamplePerformanceData(20);
            _manager.TrainRegressionModel(trainingData);
            Assert.True(_manager.HasPersistedModels());

            // Act
            _manager.ClearPersistedModels();

            // Assert
            Assert.False(_manager.HasPersistedModels());
        }

        [Fact]
        public void TrainRegressionModel_SavesModelToDisk()
        {
            // Arrange
            _manager = new MLNetModelManager(_mockLogger.Object, _testModelPath);
            var trainingData = CreateSamplePerformanceData(20);

            // Act
            _manager.TrainRegressionModel(trainingData);

            // Assert
            var modelFile = Path.Combine(_testModelPath, "regression_model.zip");
            Assert.True(File.Exists(modelFile));
        }

        [Fact]
        public void TrainRegressionModel_WithValidData_SucceedsAndMakesPredictions()
        {
            // Arrange
            _manager = new MLNetModelManager(_mockLogger.Object, _testModelPath);
            var trainingData = CreateSamplePerformanceData(50);

            // Act
            _manager.TrainRegressionModel(trainingData);
            var prediction = _manager.PredictOptimizationGain(trainingData.First());

            // Assert
            Assert.InRange(prediction, 0f, 1f);
        }

        [Fact]
        public void PredictOptimizationGain_WithoutTraining_ReturnsDefaultValue()
        {
            // Arrange
            _manager = new MLNetModelManager(_mockLogger.Object, _testModelPath);
            var testData = new PerformanceData
            {
                ExecutionTime = 100f,
                ConcurrencyLevel = 5,
                MemoryUsage = 1024,
                DatabaseCalls = 2,
                ExternalApiCalls = 1
            };

            // Act
            var prediction = _manager.PredictOptimizationGain(testData);

            // Assert
            Assert.Equal(0.5f, prediction);
        }

        [Fact]
        public void TrainClassificationModel_SavesModelToDisk()
        {
            // Arrange
            _manager = new MLNetModelManager(_mockLogger.Object, _testModelPath);
            var trainingData = CreateSampleStrategyData(20);

            // Act
            _manager.TrainClassificationModel(trainingData);

            // Assert
            var modelFile = Path.Combine(_testModelPath, "classification_model.zip");
            Assert.True(File.Exists(modelFile));
        }

        [Fact]
        public void TrainClassificationModel_WithValidData_SucceedsAndMakesPredictions()
        {
            // Arrange
            _manager = new MLNetModelManager(_mockLogger.Object, _testModelPath);
            var trainingData = CreateSampleStrategyData(50);

            // Act
            _manager.TrainClassificationModel(trainingData);
            var (shouldOptimize, confidence) = _manager.PredictOptimizationStrategy(trainingData.First());

            // Assert
            Assert.InRange(confidence, 0f, 1f);
        }

        [Fact]
        public void PredictOptimizationStrategy_WithoutTraining_ReturnsDefaultValue()
        {
            // Arrange
            _manager = new MLNetModelManager(_mockLogger.Object, _testModelPath);
            var testData = new OptimizationStrategyData
            {
                ExecutionTime = 100f,
                RepeatRate = 0.8f,
                ConcurrencyLevel = 5f,
                MemoryPressure = 0.5f,
                ErrorRate = 0.02f
            };

            // Act
            var (shouldOptimize, confidence) = _manager.PredictOptimizationStrategy(testData);

            // Assert
            Assert.False(shouldOptimize);
            Assert.Equal(0.5f, confidence);
        }

        [Fact]
        public void TrainAnomalyDetectionModel_SavesModelToDisk()
        {
            // Arrange
            _manager = new MLNetModelManager(_mockLogger.Object, _testModelPath);
            var trainingData = CreateSampleMetricData(50);

            // Act
            _manager.TrainAnomalyDetectionModel(trainingData);

            // Assert
            var modelFile = Path.Combine(_testModelPath, "anomaly_model.zip");
            Assert.True(File.Exists(modelFile));
        }

        [Fact]
        public void DetectAnomaly_WithoutTraining_ReturnsFalse()
        {
            // Arrange
            _manager = new MLNetModelManager(_mockLogger.Object, _testModelPath);
            var testData = new MetricData
            {
                Timestamp = DateTime.UtcNow,
                Value = 100f
            };

            // Act
            var isAnomaly = _manager.DetectAnomaly(testData);

            // Assert
            Assert.False(isAnomaly);
        }

        [Fact]
        public void TrainForecastingModel_SavesModelToDisk()
        {
            // Arrange
            _manager = new MLNetModelManager(_mockLogger.Object, _testModelPath);
            var trainingData = CreateSampleMetricData(100);

            // Act
            _manager.TrainForecastingModel(trainingData, horizon: 12);

            // Assert
            var modelFile = Path.Combine(_testModelPath, "forecast_model.zip");
            Assert.True(File.Exists(modelFile));
        }

        [Fact]
        public void ForecastMetric_WithoutTraining_ReturnsNull()
        {
            // Arrange
            _manager = new MLNetModelManager(_mockLogger.Object, _testModelPath);

            // Act
            var forecast = _manager.ForecastMetric(horizon: 12);

            // Assert
            Assert.Null(forecast);
        }

        [Fact]
        public void GetFeatureImportance_WithoutTraining_ReturnsNull()
        {
            // Arrange
            _manager = new MLNetModelManager(_mockLogger.Object, _testModelPath);

            // Act
            var importance = _manager.GetFeatureImportance();

            // Assert
            Assert.Null(importance);
        }

        [Fact]
        public void GetFeatureImportance_AfterTraining_ReturnsImportanceScores()
        {
            // Arrange
            _manager = new MLNetModelManager(_mockLogger.Object, _testModelPath);
            var trainingData = CreateSamplePerformanceData(50);
            _manager.TrainRegressionModel(trainingData);

            // Act
            var importance = _manager.GetFeatureImportance();

            // Assert
            Assert.NotNull(importance);
            Assert.NotEmpty(importance);
            Assert.Contains("ExecutionTime", importance.Keys);
            Assert.Contains("ConcurrencyLevel", importance.Keys);
            Assert.Contains("MemoryUsage", importance.Keys);
            Assert.Contains("DatabaseCalls", importance.Keys);
            Assert.Contains("ExternalApiCalls", importance.Keys);

            // All importance scores should be between 0 and 1
            foreach (var score in importance.Values)
            {
                Assert.InRange(score, 0f, 1f);
            }

            // Sum of importance scores should be approximately 1.0 (normalized)
            var total = importance.Values.Sum();
            Assert.InRange(total, 0.95, 1.05); // Allow small floating point error
        }

        [Fact]
        public void GetModelStoragePath_ReturnsCorrectPath()
        {
            // Arrange
            _manager = new MLNetModelManager(_mockLogger.Object, _testModelPath);

            // Act
            var path = _manager.GetModelStoragePath();

            // Assert
            Assert.Equal(_testModelPath, path);
        }

        // Helper methods to create sample data
        private List<PerformanceData> CreateSamplePerformanceData(int count)
        {
            var random = new Random(42);
            var data = new List<PerformanceData>();

            for (int i = 0; i < count; i++)
            {
                var executionTime = 50f + random.Next(200);
                var concurrency = random.Next(1, 10);
                var memoryUsage = random.Next(10, 100) * 1024 * 1024;
                var dbCalls = random.Next(0, 10);
                var apiCalls = random.Next(0, 5);

                // Simulate optimization gain based on metrics
                var gain = 0.5f + (200f - executionTime) / 400f + (5f - dbCalls) / 10f;
                gain = Math.Clamp(gain, 0f, 1f);

                data.Add(new PerformanceData
                {
                    ExecutionTime = executionTime,
                    ConcurrencyLevel = concurrency,
                    MemoryUsage = memoryUsage,
                    DatabaseCalls = dbCalls,
                    ExternalApiCalls = apiCalls,
                    OptimizationGain = gain
                });
            }

            return data;
        }

        private List<OptimizationStrategyData> CreateSampleStrategyData(int count)
        {
            var random = new Random(42);
            var data = new List<OptimizationStrategyData>();

            for (int i = 0; i < count; i++)
            {
                var executionTime = 50f + random.Next(200);
                var repeatRate = (float)random.NextDouble();
                var errorRate = (float)random.NextDouble() * 0.1f;

                // Decide if should optimize based on patterns
                var shouldOptimize = executionTime > 150 && repeatRate > 0.6 && errorRate < 0.05;

                data.Add(new OptimizationStrategyData
                {
                    ExecutionTime = executionTime,
                    RepeatRate = repeatRate,
                    ConcurrencyLevel = random.Next(1, 10),
                    MemoryPressure = (float)random.NextDouble(),
                    ErrorRate = errorRate,
                    ShouldOptimize = shouldOptimize
                });
            }

            return data;
        }

        private List<MetricData> CreateSampleMetricData(int count)
        {
            var random = new Random(42);
            var data = new List<MetricData>();
            var baseTime = DateTime.UtcNow.AddHours(-count);

            for (int i = 0; i < count; i++)
            {
                // Create time series with trend and seasonality
                var trend = i * 0.1f;
                var seasonal = (float)Math.Sin(i * Math.PI / 12) * 10f;
                var noise = (float)(random.NextDouble() - 0.5) * 5f;
                var value = 50f + trend + seasonal + noise;

                data.Add(new MetricData
                {
                    Timestamp = baseTime.AddMinutes(i * 5),
                    Value = Math.Max(0, value)
                });
            }

            return data;
        }

        public void Dispose()
        {
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
