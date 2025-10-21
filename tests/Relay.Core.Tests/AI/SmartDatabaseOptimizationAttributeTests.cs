using Relay.Core.AI;
using System;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.AI;

public class SmartDatabaseOptimizationAttributeTests
{
    [Fact]
    public void Constructor_CreatesInstanceWithDefaultValues()
    {
        // Act
        var attribute = new SmartDatabaseOptimizationAttribute();

        // Assert
        Assert.NotNull(attribute);
        Assert.IsType<SmartDatabaseOptimizationAttribute>(attribute);
    }

    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Act
        var attribute = new SmartDatabaseOptimizationAttribute();

        // Assert
        Assert.True(attribute.EnableQueryBatching);
        Assert.True(attribute.EnableConnectionPooling);
        Assert.Equal(5, attribute.MaxDatabaseCalls);
        Assert.True(attribute.PreferReadReplicas);
        Assert.True(attribute.EnableQueryCaching);
    }

    [Fact]
    public void InheritsFrom_SmartAttributeBase()
    {
        // Act
        var attribute = new SmartDatabaseOptimizationAttribute();

        // Assert
        Assert.IsAssignableFrom<SmartAttributeBase>(attribute);
    }

    [Fact]
    public void EnableQueryBatching_CanBeSetAndRetrieved()
    {
        // Arrange
        var attribute = new SmartDatabaseOptimizationAttribute();

        // Act
        attribute.EnableQueryBatching = false;

        // Assert
        Assert.False(attribute.EnableQueryBatching);
    }

    [Fact]
    public void EnableConnectionPooling_CanBeSetAndRetrieved()
    {
        // Arrange
        var attribute = new SmartDatabaseOptimizationAttribute();

        // Act
        attribute.EnableConnectionPooling = false;

        // Assert
        Assert.False(attribute.EnableConnectionPooling);
    }

    [Fact]
    public void MaxDatabaseCalls_CanBeSetAndRetrieved()
    {
        // Arrange
        var attribute = new SmartDatabaseOptimizationAttribute();

        // Act
        attribute.MaxDatabaseCalls = 10;

        // Assert
        Assert.Equal(10, attribute.MaxDatabaseCalls);
    }

    [Fact]
    public void PreferReadReplicas_CanBeSetAndRetrieved()
    {
        // Arrange
        var attribute = new SmartDatabaseOptimizationAttribute();

        // Act
        attribute.PreferReadReplicas = false;

        // Assert
        Assert.False(attribute.PreferReadReplicas);
    }

    [Fact]
    public void EnableQueryCaching_CanBeSetAndRetrieved()
    {
        // Arrange
        var attribute = new SmartDatabaseOptimizationAttribute();

        // Act
        attribute.EnableQueryCaching = false;

        // Assert
        Assert.False(attribute.EnableQueryCaching);
    }

    [Fact]
    public void AttributeUsage_IsCorrect()
    {
        // Arrange
        var attributeType = typeof(SmartDatabaseOptimizationAttribute);
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
        var attributes = methodInfo.GetCustomAttributes(typeof(SmartDatabaseOptimizationAttribute), true);

        // Assert
        Assert.Single(attributes);
        Assert.IsType<SmartDatabaseOptimizationAttribute>(attributes[0]);
    }

    [Fact]
    public void CanBeAppliedTo_Class()
    {
        // Arrange
        var type = typeof(TestClass);

        // Act
        var attributes = type.GetCustomAttributes(typeof(SmartDatabaseOptimizationAttribute), true);

        // Assert
        Assert.Single(attributes);
        Assert.IsType<SmartDatabaseOptimizationAttribute>(attributes[0]);
    }

    [SmartDatabaseOptimization]
    private class TestClass
    {
        [SmartDatabaseOptimization]
        public void TestMethod() { }
    }
}