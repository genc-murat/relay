using Relay.Core.AI;
using System;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI;

public class SmartMemoryOptimizationAttributeTests
{
    [Fact]
    public void Constructor_CreatesInstanceWithDefaultValues()
    {
        // Act
        var attribute = new SmartMemoryOptimizationAttribute();

        // Assert
        Assert.NotNull(attribute);
        Assert.IsType<SmartMemoryOptimizationAttribute>(attribute);
    }

    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Act
        var attribute = new SmartMemoryOptimizationAttribute();

        // Assert
        Assert.True(attribute.EnableObjectPooling);
        Assert.True(attribute.PreferStackAllocation);
        Assert.Equal(1024 * 1024, attribute.MemoryThreshold); // 1MB
        Assert.True(attribute.EnableBufferPooling);
        Assert.Equal(OptimizationAggressiveness.Moderate, attribute.Aggressiveness);
    }

    [Fact]
    public void InheritsFrom_SmartAttributeBase()
    {
        // Act
        var attribute = new SmartMemoryOptimizationAttribute();

        // Assert
        Assert.IsAssignableFrom<SmartAttributeBase>(attribute);
    }

    [Fact]
    public void EnableObjectPooling_CanBeSetAndRetrieved()
    {
        // Arrange
        var attribute = new SmartMemoryOptimizationAttribute();

        // Act
        attribute.EnableObjectPooling = false;

        // Assert
        Assert.False(attribute.EnableObjectPooling);
    }

    [Fact]
    public void PreferStackAllocation_CanBeSetAndRetrieved()
    {
        // Arrange
        var attribute = new SmartMemoryOptimizationAttribute();

        // Act
        attribute.PreferStackAllocation = false;

        // Assert
        Assert.False(attribute.PreferStackAllocation);
    }

    [Fact]
    public void MemoryThreshold_CanBeSetAndRetrieved()
    {
        // Arrange
        var attribute = new SmartMemoryOptimizationAttribute();

        // Act
        attribute.MemoryThreshold = 2 * 1024 * 1024; // 2MB

        // Assert
        Assert.Equal(2 * 1024 * 1024, attribute.MemoryThreshold);
    }

    [Fact]
    public void EnableBufferPooling_CanBeSetAndRetrieved()
    {
        // Arrange
        var attribute = new SmartMemoryOptimizationAttribute();

        // Act
        attribute.EnableBufferPooling = false;

        // Assert
        Assert.False(attribute.EnableBufferPooling);
    }

    [Fact]
    public void Aggressiveness_CanBeSetAndRetrieved()
    {
        // Arrange
        var attribute = new SmartMemoryOptimizationAttribute();

        // Act
        attribute.Aggressiveness = OptimizationAggressiveness.Aggressive;

        // Assert
        Assert.Equal(OptimizationAggressiveness.Aggressive, attribute.Aggressiveness);
    }

    [Fact]
    public void AllAggressivenessLevels_CanBeSet()
    {
        // Arrange
        var attribute = new SmartMemoryOptimizationAttribute();

        // Act & Assert
        attribute.Aggressiveness = OptimizationAggressiveness.Conservative;
        Assert.Equal(OptimizationAggressiveness.Conservative, attribute.Aggressiveness);

        attribute.Aggressiveness = OptimizationAggressiveness.Moderate;
        Assert.Equal(OptimizationAggressiveness.Moderate, attribute.Aggressiveness);

        attribute.Aggressiveness = OptimizationAggressiveness.Aggressive;
        Assert.Equal(OptimizationAggressiveness.Aggressive, attribute.Aggressiveness);

        attribute.Aggressiveness = OptimizationAggressiveness.Maximum;
        Assert.Equal(OptimizationAggressiveness.Maximum, attribute.Aggressiveness);
    }

    [Fact]
    public void AttributeUsage_IsCorrect()
    {
        // Arrange
        var attributeType = typeof(SmartMemoryOptimizationAttribute);
        var attributeUsage = attributeType.GetCustomAttributes(typeof(AttributeUsageAttribute), true)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        Assert.NotNull(attributeUsage);
        Assert.Equal(AttributeTargets.Method | AttributeTargets.Class, attributeUsage.ValidOn);
        Assert.False(attributeUsage.AllowMultiple);
    }

    [Fact]
    public void CanBeAppliedTo_Method()
    {
        // Arrange
        var methodInfo = typeof(TestClass).GetMethod(nameof(TestClass.TestMethod));

        // Act
        var attributes = methodInfo.GetCustomAttributes(typeof(SmartMemoryOptimizationAttribute), true);

        // Assert
        Assert.Single(attributes);
        Assert.IsType<SmartMemoryOptimizationAttribute>(attributes[0]);
    }

    [Fact]
    public void CanBeAppliedTo_Class()
    {
        // Arrange
        var type = typeof(TestClass);

        // Act
        var attributes = type.GetCustomAttributes(typeof(SmartMemoryOptimizationAttribute), true);

        // Assert
        Assert.Single(attributes);
        Assert.IsType<SmartMemoryOptimizationAttribute>(attributes[0]);
    }

    [SmartMemoryOptimization]
    private class TestClass
    {
        [SmartMemoryOptimization]
        public void TestMethod() { }
    }
}