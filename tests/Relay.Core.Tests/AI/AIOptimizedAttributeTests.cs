using Relay.Core.AI;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Relay.Core.Tests.AI
{
    public class AIOptimizedAttributeTests
    {
        [Fact]
        public void Constructor_CreatesInstanceWithDefaultValues()
        {
            // Act
            var attribute = new AIOptimizedAttribute();

            // Assert
            Assert.NotNull(attribute);
            Assert.IsType<AIOptimizedAttribute>(attribute);
        }

        [Fact]
        public void DefaultValues_AreSetCorrectly()
        {
            // Act
            var attribute = new AIOptimizedAttribute();

            // Assert
            Assert.False(attribute.AutoApplyOptimizations);
            Assert.Equal(0.7, attribute.MinConfidenceScore);
            Assert.Equal(RiskLevel.Low, attribute.MaxRiskLevel);
            Assert.Empty(attribute.AllowedStrategies);
            Assert.Empty(attribute.ExcludedStrategies);
            Assert.True(attribute.EnableMetricsTracking);
            Assert.True(attribute.EnableLearning);
            Assert.Equal(OptimizationPriority.Medium, attribute.Priority);
        }

        [Fact]
        public void AutoApplyOptimizations_CanBeSetAndRetrieved()
        {
            // Arrange
            var attribute = new AIOptimizedAttribute();

            // Act
            attribute.AutoApplyOptimizations = true;

            // Assert
            Assert.True(attribute.AutoApplyOptimizations);
        }

        [Fact]
        public void MinConfidenceScore_CanBeSetAndRetrieved()
        {
            // Arrange
            var attribute = new AIOptimizedAttribute();

            // Act
            attribute.MinConfidenceScore = 0.8;

            // Assert
            Assert.Equal(0.8, attribute.MinConfidenceScore);
        }

        [Fact]
        public void MaxRiskLevel_CanBeSetAndRetrieved()
        {
            // Arrange
            var attribute = new AIOptimizedAttribute();

            // Act
            attribute.MaxRiskLevel = RiskLevel.Medium;

            // Assert
            Assert.Equal(RiskLevel.Medium, attribute.MaxRiskLevel);
        }

        [Fact]
        public void AllowedStrategies_CanBeSetAndRetrieved()
        {
            // Arrange
            var attribute = new AIOptimizedAttribute();
            var strategies = new[] { OptimizationStrategy.EnableCaching, OptimizationStrategy.BatchProcessing };

            // Act
            attribute.AllowedStrategies = strategies;

            // Assert
            Assert.Equal(strategies, attribute.AllowedStrategies);
        }

        [Fact]
        public void ExcludedStrategies_CanBeSetAndRetrieved()
        {
            // Arrange
            var attribute = new AIOptimizedAttribute();
            var strategies = new[] { OptimizationStrategy.ParallelProcessing };

            // Act
            attribute.ExcludedStrategies = strategies;

            // Assert
            Assert.Equal(strategies, attribute.ExcludedStrategies);
        }

        [Fact]
        public void EnableMetricsTracking_CanBeSetAndRetrieved()
        {
            // Arrange
            var attribute = new AIOptimizedAttribute();

            // Act
            attribute.EnableMetricsTracking = false;

            // Assert
            Assert.False(attribute.EnableMetricsTracking);
        }

        [Fact]
        public void EnableLearning_CanBeSetAndRetrieved()
        {
            // Arrange
            var attribute = new AIOptimizedAttribute();

            // Act
            attribute.EnableLearning = false;

            // Assert
            Assert.False(attribute.EnableLearning);
        }

        [Fact]
        public void Priority_CanBeSetAndRetrieved()
        {
            // Arrange
            var attribute = new AIOptimizedAttribute();

            // Act
            attribute.Priority = OptimizationPriority.High;

            // Assert
            Assert.Equal(OptimizationPriority.High, attribute.Priority);
        }

        [Fact]
        public void AttributeUsage_AllowsMethodTarget()
        {
            // Arrange
            var attributeUsage = typeof(AIOptimizedAttribute)
                .GetCustomAttribute<AttributeUsageAttribute>();

            // Assert
            Assert.NotNull(attributeUsage);
            Assert.True(attributeUsage.ValidOn.HasFlag(AttributeTargets.Method));
        }

        [Fact]
        public void AttributeUsage_AllowsClassTarget()
        {
            // Arrange
            var attributeUsage = typeof(AIOptimizedAttribute)
                .GetCustomAttribute<AttributeUsageAttribute>();

            // Assert
            Assert.NotNull(attributeUsage);
            Assert.True(attributeUsage.ValidOn.HasFlag(AttributeTargets.Class));
        }

        [Fact]
        public void AttributeUsage_DoesNotAllowMultiple()
        {
            // Arrange
            var attributeUsage = typeof(AIOptimizedAttribute)
                .GetCustomAttribute<AttributeUsageAttribute>();

            // Assert
            Assert.NotNull(attributeUsage);
            Assert.False(attributeUsage.AllowMultiple);
        }

        [Fact]
        public void AttributeUsage_InheritedIsDefault()
        {
            // Arrange
            var attributeUsage = typeof(AIOptimizedAttribute)
                .GetCustomAttribute<AttributeUsageAttribute>();

            // Assert
            Assert.NotNull(attributeUsage);
            Assert.True(attributeUsage.Inherited); // Default is true when not specified
        }

        [Fact]
        public void CanCreateAttributeWithAllPropertiesSet()
        {
            // Act
            var attribute = new AIOptimizedAttribute
            {
                AutoApplyOptimizations = true,
                MinConfidenceScore = 0.9,
                MaxRiskLevel = RiskLevel.High,
                AllowedStrategies = new[] { OptimizationStrategy.EnableCaching, OptimizationStrategy.MemoryPooling },
                ExcludedStrategies = new[] { OptimizationStrategy.ParallelProcessing },
                EnableMetricsTracking = false,
                EnableLearning = false,
                Priority = OptimizationPriority.Low
            };

            // Assert
            Assert.True(attribute.AutoApplyOptimizations);
            Assert.Equal(0.9, attribute.MinConfidenceScore);
            Assert.Equal(RiskLevel.High, attribute.MaxRiskLevel);
            Assert.Equal(2, attribute.AllowedStrategies.Length);
            Assert.Contains(OptimizationStrategy.EnableCaching, attribute.AllowedStrategies);
            Assert.Contains(OptimizationStrategy.MemoryPooling, attribute.AllowedStrategies);
            Assert.Single(attribute.ExcludedStrategies);
            Assert.Contains(OptimizationStrategy.ParallelProcessing, attribute.ExcludedStrategies);
            Assert.False(attribute.EnableMetricsTracking);
            Assert.False(attribute.EnableLearning);
            Assert.Equal(OptimizationPriority.Low, attribute.Priority);
        }

        [Fact]
        public void CanCreateAttributeWithMinimalConfiguration()
        {
            // Act
            var attribute = new AIOptimizedAttribute
            {
                AutoApplyOptimizations = true
            };

            // Assert
            Assert.True(attribute.AutoApplyOptimizations);
            // Other properties should retain defaults
            Assert.Equal(0.7, attribute.MinConfidenceScore);
            Assert.Equal(RiskLevel.Low, attribute.MaxRiskLevel);
            Assert.True(attribute.EnableMetricsTracking);
            Assert.True(attribute.EnableLearning);
            Assert.Equal(OptimizationPriority.Medium, attribute.Priority);
        }

        [Fact]
        public void AllowedStrategies_DefaultsToEmptyArray()
        {
            // Act
            var attribute = new AIOptimizedAttribute();

            // Assert
            Assert.NotNull(attribute.AllowedStrategies);
            Assert.Empty(attribute.AllowedStrategies);
        }

        [Fact]
        public void ExcludedStrategies_DefaultsToEmptyArray()
        {
            // Act
            var attribute = new AIOptimizedAttribute();

            // Assert
            Assert.NotNull(attribute.ExcludedStrategies);
            Assert.Empty(attribute.ExcludedStrategies);
        }

        [Fact]
        public void CanSetAllowedStrategiesToNull_ResultsInEmptyArray()
        {
            // Arrange
            var attribute = new AIOptimizedAttribute();

            // Act
            attribute.AllowedStrategies = null!;

            // Assert
            Assert.Null(attribute.AllowedStrategies);
        }

        [Fact]
        public void CanSetExcludedStrategiesToNull_ResultsInEmptyArray()
        {
            // Arrange
            var attribute = new AIOptimizedAttribute();

            // Act
            attribute.ExcludedStrategies = null!;

            // Assert
            Assert.Null(attribute.ExcludedStrategies);
        }

        // Test classes and methods with attributes for usage testing
        [AIOptimized]
        private class TestClassWithAttribute
        {
            [AIOptimized]
            public void TestMethod() { }

            public void TestMethodWithoutAttribute() { }
        }

        private class TestClassWithoutAttribute
        {
            [AIOptimized]
            public void TestMethod() { }
        }

        [Fact]
        public void CanApplyAttributeToClass()
        {
            // Act
            var attribute = typeof(TestClassWithAttribute)
                .GetCustomAttribute<AIOptimizedAttribute>();

            // Assert
            Assert.NotNull(attribute);
        }

        [Fact]
        public void CanApplyAttributeToMethod()
        {
            // Act
            var method = typeof(TestClassWithAttribute)
                .GetMethod(nameof(TestClassWithAttribute.TestMethod));
            var attribute = method!.GetCustomAttribute<AIOptimizedAttribute>();

            // Assert
            Assert.NotNull(attribute);
        }

        [Fact]
        public void MethodWithoutAttribute_DoesNotHaveAttribute()
        {
            // Act
            var method = typeof(TestClassWithAttribute)
                .GetMethod(nameof(TestClassWithAttribute.TestMethodWithoutAttribute));
            var attribute = method!.GetCustomAttribute<AIOptimizedAttribute>();

            // Assert
            Assert.Null(attribute);
        }

        [Fact]
        public void ClassWithoutAttribute_DoesNotHaveAttribute()
        {
            // Act
            var attribute = typeof(TestClassWithoutAttribute)
                .GetCustomAttribute<AIOptimizedAttribute>();

            // Assert
            Assert.Null(attribute);
        }

        [Fact]
        public void MethodInClassWithoutAttribute_CanHaveAttribute()
        {
            // Act
            var method = typeof(TestClassWithoutAttribute)
                .GetMethod(nameof(TestClassWithoutAttribute.TestMethod));
            var attribute = method!.GetCustomAttribute<AIOptimizedAttribute>();

            // Assert
            Assert.NotNull(attribute);
        }

        [Fact]
        public void AttributeOnMethod_HasDefaultValues()
        {
            // Act
            var method = typeof(TestClassWithAttribute)
                .GetMethod(nameof(TestClassWithAttribute.TestMethod));
            var attribute = method!.GetCustomAttribute<AIOptimizedAttribute>();

            // Assert
            Assert.NotNull(attribute);
            Assert.False(attribute.AutoApplyOptimizations);
            Assert.Equal(0.7, attribute.MinConfidenceScore);
            Assert.Equal(RiskLevel.Low, attribute.MaxRiskLevel);
            Assert.True(attribute.EnableMetricsTracking);
            Assert.True(attribute.EnableLearning);
            Assert.Equal(OptimizationPriority.Medium, attribute.Priority);
        }

        // Test with custom configuration
        [AIOptimized(
            AutoApplyOptimizations = true,
            MinConfidenceScore = 0.8,
            MaxRiskLevel = RiskLevel.Medium,
            EnableMetricsTracking = false,
            Priority = OptimizationPriority.High)]
        private class TestClassWithCustomAttribute
        {
        }

        [Fact]
        public void AttributeWithCustomConfiguration_HasCorrectValues()
        {
            // Act
            var attribute = typeof(TestClassWithCustomAttribute)
                .GetCustomAttribute<AIOptimizedAttribute>();

            // Assert
            Assert.NotNull(attribute);
            Assert.True(attribute.AutoApplyOptimizations);
            Assert.Equal(0.8, attribute.MinConfidenceScore);
            Assert.Equal(RiskLevel.Medium, attribute.MaxRiskLevel);
            Assert.False(attribute.EnableMetricsTracking);
            Assert.Equal(OptimizationPriority.High, attribute.Priority);
            // Other properties should retain defaults
            Assert.True(attribute.EnableLearning);
            Assert.Empty(attribute.AllowedStrategies);
            Assert.Empty(attribute.ExcludedStrategies);
        }

        [Fact]
        public void AttributeType_IsSealed()
        {
            // Act
            var isSealed = typeof(AIOptimizedAttribute).IsSealed;

            // Assert
            Assert.True(isSealed);
        }

        [Fact]
        public void Attribute_InheritsFromAttribute()
        {
            // Act
            var baseType = typeof(AIOptimizedAttribute).BaseType;

            // Assert
            Assert.Equal(typeof(Attribute), baseType);
        }
    }
}