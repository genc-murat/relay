using Relay.Core.AI;
using System;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI;

public class SmartParallelizationAttributeTests
{
    [Fact]
    public void Constructor_CreatesInstanceWithDefaultValues()
    {
        // Act
        var attribute = new SmartParallelizationAttribute();

        // Assert
        Assert.NotNull(attribute);
        Assert.IsType<SmartParallelizationAttribute>(attribute);
    }

    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Act
        var attribute = new SmartParallelizationAttribute();

        // Assert
        Assert.Equal(100, attribute.MinCollectionSize);
        Assert.Equal(-1, attribute.MaxDegreeOfParallelism);
        Assert.Equal(ParallelizationStrategy.Dynamic, attribute.Strategy);
        Assert.True(attribute.EnableWorkStealing);
    }

    [Fact]
    public void InheritsFrom_SmartAttributeBase()
    {
        // Act
        var attribute = new SmartParallelizationAttribute();

        // Assert
        Assert.IsAssignableFrom<SmartAttributeBase>(attribute);
    }

    [Fact]
    public void MinCollectionSize_CanBeSetAndRetrieved()
    {
        // Arrange
        var attribute = new SmartParallelizationAttribute();

        // Act
        attribute.MinCollectionSize = 50;

        // Assert
        Assert.Equal(50, attribute.MinCollectionSize);
    }

    [Fact]
    public void MaxDegreeOfParallelism_CanBeSetAndRetrieved()
    {
        // Arrange
        var attribute = new SmartParallelizationAttribute();

        // Act
        attribute.MaxDegreeOfParallelism = 4;

        // Assert
        Assert.Equal(4, attribute.MaxDegreeOfParallelism);
    }

    [Fact]
    public void Strategy_CanBeSetAndRetrieved()
    {
        // Arrange
        var attribute = new SmartParallelizationAttribute();

        // Act
        attribute.Strategy = ParallelizationStrategy.Static;

        // Assert
        Assert.Equal(ParallelizationStrategy.Static, attribute.Strategy);
    }

    [Fact]
    public void EnableWorkStealing_CanBeSetAndRetrieved()
    {
        // Arrange
        var attribute = new SmartParallelizationAttribute();

        // Act
        attribute.EnableWorkStealing = false;

        // Assert
        Assert.False(attribute.EnableWorkStealing);
    }

    [Fact]
    public void AllStrategies_CanBeSet()
    {
        // Arrange
        var attribute = new SmartParallelizationAttribute();

        // Act & Assert
        attribute.Strategy = ParallelizationStrategy.None;
        Assert.Equal(ParallelizationStrategy.None, attribute.Strategy);

        attribute.Strategy = ParallelizationStrategy.Static;
        Assert.Equal(ParallelizationStrategy.Static, attribute.Strategy);

        attribute.Strategy = ParallelizationStrategy.Dynamic;
        Assert.Equal(ParallelizationStrategy.Dynamic, attribute.Strategy);

        attribute.Strategy = ParallelizationStrategy.WorkStealing;
        Assert.Equal(ParallelizationStrategy.WorkStealing, attribute.Strategy);

        attribute.Strategy = ParallelizationStrategy.AIPredictive;
        Assert.Equal(ParallelizationStrategy.AIPredictive, attribute.Strategy);
    }

    [Fact]
    public void AttributeUsage_IsCorrect()
    {
        // Arrange
        var attributeType = typeof(SmartParallelizationAttribute);
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
        var attributes = methodInfo.GetCustomAttributes(typeof(SmartParallelizationAttribute), true);

        // Assert
        Assert.Single(attributes);
        Assert.IsType<SmartParallelizationAttribute>(attributes[0]);
    }

    [Fact]
    public void CanBeAppliedTo_Class()
    {
        // Arrange
        var type = typeof(TestClass);

        // Act
        var attributes = type.GetCustomAttributes(typeof(SmartParallelizationAttribute), true);

        // Assert
        Assert.Single(attributes);
        Assert.IsType<SmartParallelizationAttribute>(attributes[0]);
    }

    [SmartParallelization]
    private class TestClass
    {
        [SmartParallelization]
        public void TestMethod() { }
    }
}