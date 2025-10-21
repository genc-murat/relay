using System;
using Relay.Core.AI;
using Relay.Core.AI.Metrics.Implementations;
using Xunit;

namespace Relay.Core.Tests.AI.Metrics
{
    public class DefaultMetricsValidatorTests
    {
        private readonly DefaultMetricsValidator _validator;
        private readonly AIModelStatistics _validStatistics;

        public DefaultMetricsValidatorTests()
        {
            _validator = new DefaultMetricsValidator();
            _validStatistics = new AIModelStatistics
            {
                ModelVersion = "v1.2.3",
                ModelTrainingDate = DateTime.UtcNow.AddDays(-7),
                LastRetraining = DateTime.UtcNow.AddDays(-1),
                TotalPredictions = 10000,
                AccuracyScore = 0.85,
                PrecisionScore = 0.82,
                RecallScore = 0.88,
                F1Score = 0.85,
                ModelConfidence = 0.78,
                AveragePredictionTime = TimeSpan.FromMilliseconds(45.5),
                TrainingDataPoints = 50000
            };
        }

        #region ModelVersion Validation Tests

        [Fact]
        public void Validate_Should_Throw_When_ModelVersion_Is_Null()
        {
            // Arrange
            var invalidStats = new AIModelStatistics { ModelVersion = null! };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate(invalidStats));
            Assert.Contains("Model version cannot be null, empty, or whitespace", exception.Message);
            Assert.Equal("ModelVersion", exception.ParamName);
        }

        [Fact]
        public void Validate_Should_Throw_When_ModelVersion_Is_Empty()
        {
            // Arrange
            var invalidStats = new AIModelStatistics { ModelVersion = string.Empty };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate(invalidStats));
            Assert.Contains("Model version cannot be null, empty, or whitespace", exception.Message);
            Assert.Equal("ModelVersion", exception.ParamName);
        }

        [Fact]
        public void Validate_Should_Throw_When_ModelVersion_Is_Whitespace()
        {
            // Arrange
            var invalidStats = new AIModelStatistics { ModelVersion = "   " };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate(invalidStats));
            Assert.Contains("Model version cannot be null, empty, or whitespace", exception.Message);
            Assert.Equal("ModelVersion", exception.ParamName);
        }

        #endregion

        #region TotalPredictions Validation Tests

        [Fact]
        public void Validate_Should_Throw_When_TotalPredictions_Is_Negative()
        {
            // Arrange
            var invalidStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                TotalPredictions = -1
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate(invalidStats));
            Assert.Contains("Total predictions cannot be negative", exception.Message);
            Assert.Equal("TotalPredictions", exception.ParamName);
        }

        [Fact]
        public void Validate_Should_Accept_Zero_TotalPredictions()
        {
            // Arrange
            var stats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                TotalPredictions = 0,
                AccuracyScore = 0.5,
                PrecisionScore = 0.5,
                RecallScore = 0.5,
                F1Score = 0.5,
                ModelConfidence = 0.5,
                AveragePredictionTime = TimeSpan.FromMilliseconds(1),
                TrainingDataPoints = 100
            };

            // Act & Assert
            _validator.Validate(stats); // Should not throw
        }

        #endregion

        #region AccuracyScore Validation Tests

        [Fact]
        public void Validate_Should_Throw_When_AccuracyScore_Is_NaN()
        {
            // Arrange
            var invalidStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                AccuracyScore = double.NaN
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate(invalidStats));
            Assert.Contains("Accuracy score must be a valid number between 0 and 1", exception.Message);
            Assert.Equal("AccuracyScore", exception.ParamName);
        }

        [Fact]
        public void Validate_Should_Throw_When_AccuracyScore_Is_Negative()
        {
            // Arrange
            var invalidStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                AccuracyScore = -0.1
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate(invalidStats));
            Assert.Contains("Accuracy score must be a valid number between 0 and 1", exception.Message);
            Assert.Equal("AccuracyScore", exception.ParamName);
        }

        [Fact]
        public void Validate_Should_Throw_When_AccuracyScore_Is_Greater_Than_One()
        {
            // Arrange
            var invalidStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                AccuracyScore = 1.1
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate(invalidStats));
            Assert.Contains("Accuracy score must be a valid number between 0 and 1", exception.Message);
            Assert.Equal("AccuracyScore", exception.ParamName);
        }

        [Fact]
        public void Validate_Should_Accept_AccuracyScore_Of_Zero()
        {
            // Arrange
            var stats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                TotalPredictions = 100,
                AccuracyScore = 0.0,
                PrecisionScore = 0.5,
                RecallScore = 0.5,
                F1Score = 0.5,
                ModelConfidence = 0.5,
                AveragePredictionTime = TimeSpan.FromMilliseconds(1),
                TrainingDataPoints = 100
            };

            // Act & Assert
            _validator.Validate(stats); // Should not throw
        }

        [Fact]
        public void Validate_Should_Accept_AccuracyScore_Of_One()
        {
            // Arrange
            var stats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                TotalPredictions = 100,
                AccuracyScore = 1.0,
                PrecisionScore = 0.5,
                RecallScore = 0.5,
                F1Score = 0.5,
                ModelConfidence = 0.5,
                AveragePredictionTime = TimeSpan.FromMilliseconds(1),
                TrainingDataPoints = 100
            };

            // Act & Assert
            _validator.Validate(stats); // Should not throw
        }

        #endregion

        #region PrecisionScore Validation Tests

        [Fact]
        public void Validate_Should_Throw_When_PrecisionScore_Is_NaN()
        {
            // Arrange
            var invalidStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                PrecisionScore = double.NaN
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate(invalidStats));
            Assert.Contains("Precision score must be a valid number between 0 and 1", exception.Message);
            Assert.Equal("PrecisionScore", exception.ParamName);
        }

        [Fact]
        public void Validate_Should_Throw_When_PrecisionScore_Is_Negative()
        {
            // Arrange
            var invalidStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                PrecisionScore = -0.1
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate(invalidStats));
            Assert.Contains("Precision score must be a valid number between 0 and 1", exception.Message);
            Assert.Equal("PrecisionScore", exception.ParamName);
        }

        [Fact]
        public void Validate_Should_Throw_When_PrecisionScore_Is_Greater_Than_One()
        {
            // Arrange
            var invalidStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                PrecisionScore = 1.1
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate(invalidStats));
            Assert.Contains("Precision score must be a valid number between 0 and 1", exception.Message);
            Assert.Equal("PrecisionScore", exception.ParamName);
        }

        #endregion

        #region RecallScore Validation Tests

        [Fact]
        public void Validate_Should_Throw_When_RecallScore_Is_NaN()
        {
            // Arrange
            var invalidStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                RecallScore = double.NaN
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate(invalidStats));
            Assert.Contains("Recall score must be a valid number between 0 and 1", exception.Message);
            Assert.Equal("RecallScore", exception.ParamName);
        }

        [Fact]
        public void Validate_Should_Throw_When_RecallScore_Is_Negative()
        {
            // Arrange
            var invalidStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                RecallScore = -0.1
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate(invalidStats));
            Assert.Contains("Recall score must be a valid number between 0 and 1", exception.Message);
            Assert.Equal("RecallScore", exception.ParamName);
        }

        [Fact]
        public void Validate_Should_Throw_When_RecallScore_Is_Greater_Than_One()
        {
            // Arrange
            var invalidStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                RecallScore = 1.1
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate(invalidStats));
            Assert.Contains("Recall score must be a valid number between 0 and 1", exception.Message);
            Assert.Equal("RecallScore", exception.ParamName);
        }

        #endregion

        #region F1Score Validation Tests

        [Fact]
        public void Validate_Should_Throw_When_F1Score_Is_NaN()
        {
            // Arrange
            var invalidStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                F1Score = double.NaN
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate(invalidStats));
            Assert.Contains("F1 score must be a valid number between 0 and 1", exception.Message);
            Assert.Equal("F1Score", exception.ParamName);
        }

        [Fact]
        public void Validate_Should_Throw_When_F1Score_Is_Negative()
        {
            // Arrange
            var invalidStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                F1Score = -0.1
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate(invalidStats));
            Assert.Contains("F1 score must be a valid number between 0 and 1", exception.Message);
            Assert.Equal("F1Score", exception.ParamName);
        }

        [Fact]
        public void Validate_Should_Throw_When_F1Score_Is_Greater_Than_One()
        {
            // Arrange
            var invalidStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                F1Score = 1.1
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate(invalidStats));
            Assert.Contains("F1 score must be a valid number between 0 and 1", exception.Message);
            Assert.Equal("F1Score", exception.ParamName);
        }

        #endregion

        #region ModelConfidence Validation Tests

        [Fact]
        public void Validate_Should_Throw_When_ModelConfidence_Is_NaN()
        {
            // Arrange
            var invalidStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                ModelConfidence = double.NaN
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate(invalidStats));
            Assert.Contains("Model confidence must be a valid number between 0 and 1", exception.Message);
            Assert.Equal("ModelConfidence", exception.ParamName);
        }

        [Fact]
        public void Validate_Should_Throw_When_ModelConfidence_Is_Negative()
        {
            // Arrange
            var invalidStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                ModelConfidence = -0.1
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate(invalidStats));
            Assert.Contains("Model confidence must be a valid number between 0 and 1", exception.Message);
            Assert.Equal("ModelConfidence", exception.ParamName);
        }

        [Fact]
        public void Validate_Should_Throw_When_ModelConfidence_Is_Greater_Than_One()
        {
            // Arrange
            var invalidStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                ModelConfidence = 1.1
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate(invalidStats));
            Assert.Contains("Model confidence must be a valid number between 0 and 1", exception.Message);
            Assert.Equal("ModelConfidence", exception.ParamName);
        }

        #endregion

        #region AveragePredictionTime Validation Tests

        [Fact]
        public void Validate_Should_Throw_When_AveragePredictionTime_Is_Negative()
        {
            // Arrange
            var invalidStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                AveragePredictionTime = TimeSpan.FromMilliseconds(-1)
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate(invalidStats));
            Assert.Contains("Average prediction time cannot be negative", exception.Message);
            Assert.Equal("AveragePredictionTime", exception.ParamName);
        }

        [Fact]
        public void Validate_Should_Accept_Zero_AveragePredictionTime()
        {
            // Arrange
            var stats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                TotalPredictions = 100,
                AccuracyScore = 0.5,
                PrecisionScore = 0.5,
                RecallScore = 0.5,
                F1Score = 0.5,
                ModelConfidence = 0.5,
                AveragePredictionTime = TimeSpan.Zero,
                TrainingDataPoints = 100
            };

            // Act & Assert
            _validator.Validate(stats); // Should not throw
        }

        #endregion

        #region TrainingDataPoints Validation Tests

        [Fact]
        public void Validate_Should_Throw_When_TrainingDataPoints_Is_Negative()
        {
            // Arrange
            var invalidStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                TrainingDataPoints = -1
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate(invalidStats));
            Assert.Contains("Training data points cannot be negative", exception.Message);
            Assert.Equal("TrainingDataPoints", exception.ParamName);
        }

        [Fact]
        public void Validate_Should_Accept_Zero_TrainingDataPoints()
        {
            // Arrange
            var stats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                TotalPredictions = 100,
                AccuracyScore = 0.5,
                PrecisionScore = 0.5,
                RecallScore = 0.5,
                F1Score = 0.5,
                ModelConfidence = 0.5,
                AveragePredictionTime = TimeSpan.FromMilliseconds(1),
                TrainingDataPoints = 0
            };

            // Act & Assert
            _validator.Validate(stats); // Should not throw
        }

        #endregion

        #region Valid Statistics Tests

        [Fact]
        public void Validate_Should_Accept_Valid_Statistics()
        {
            // Arrange - _validStatistics is already set up with valid values

            // Act & Assert
            _validator.Validate(_validStatistics); // Should not throw
        }

        [Fact]
        public void Validate_Should_Accept_Statistics_With_Boundary_Values()
        {
            // Arrange
            var boundaryStats = new AIModelStatistics
            {
                ModelVersion = "v1.0.0",
                TotalPredictions = 0,
                AccuracyScore = 0.0,
                PrecisionScore = 0.0,
                RecallScore = 0.0,
                F1Score = 0.0,
                ModelConfidence = 0.0,
                AveragePredictionTime = TimeSpan.Zero,
                TrainingDataPoints = 0
            };

            // Act & Assert
            _validator.Validate(boundaryStats); // Should not throw
        }

        [Fact]
        public void Validate_Should_Accept_Statistics_With_Maximum_Valid_Values()
        {
            // Arrange
            var maxStats = new AIModelStatistics
            {
                ModelVersion = "v999.999.999",
                TotalPredictions = int.MaxValue,
                AccuracyScore = 1.0,
                PrecisionScore = 1.0,
                RecallScore = 1.0,
                F1Score = 1.0,
                ModelConfidence = 1.0,
                AveragePredictionTime = TimeSpan.MaxValue,
                TrainingDataPoints = int.MaxValue
            };

            // Act & Assert
            _validator.Validate(maxStats); // Should not throw
        }

        #endregion

        #region Edge Cases and Integration Tests

        [Fact]
        public void Validate_Should_Throw_On_First_Validation_Error_When_Multiple_Errors_Exist()
        {
            // Arrange - Multiple validation errors, should throw on first one (ModelVersion)
            var invalidStats = new AIModelStatistics
            {
                ModelVersion = null!, // First error
                TotalPredictions = -1, // Second error
                AccuracyScore = 1.5 // Third error
            };

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => _validator.Validate(invalidStats));
            Assert.Contains("Model version cannot be null, empty, or whitespace", exception.Message);
            Assert.Equal("ModelVersion", exception.ParamName);
        }

        [Fact]
        public void Validate_Should_Be_Idempotent()
        {
            // Arrange
            var stats = _validStatistics;

            // Act - Call validate multiple times
            _validator.Validate(stats);
            _validator.Validate(stats);
            _validator.Validate(stats);

            // Assert - Should not throw
            _validator.Validate(stats); // Should not throw
        }

        #endregion
    }
}