using System;
using System.Linq;
using System.Reflection;
using Relay.Core.AI;
using Xunit;

namespace Relay.Core.Tests.AI.Extensions.Attributes;

public class PerformanceHintAttributeTests
{
    [Fact]
    public void PerformanceHintAttribute_Constructor_WithValidHint_Succeeds()
    {
        // Act
        var attribute = new PerformanceHintAttribute("Test hint");

        // Assert
        Assert.Equal("Test hint", attribute.Hint);
    }

    [Fact]
    public void PerformanceHintAttribute_Constructor_WithNullHint_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PerformanceHintAttribute(null!));
    }

    [Fact]
    public void PerformanceHintAttribute_Constructor_WithEmptyHint_Succeeds()
    {
        // Act
        var attribute = new PerformanceHintAttribute(string.Empty);

        // Assert
        Assert.Equal(string.Empty, attribute.Hint);
    }

    [Fact]
    public void PerformanceHintAttribute_DefaultProperties_AreSetCorrectly()
    {
        // Act
        var attribute = new PerformanceHintAttribute("Test hint");

        // Assert
        Assert.Equal("General", attribute.Category);
        Assert.Equal(OptimizationPriority.Medium, attribute.Priority);
        Assert.False(attribute.IsRequired);
    }

    [Fact]
    public void PerformanceHintAttribute_Category_CanBeSet()
    {
        // Arrange
        var attribute = new PerformanceHintAttribute("Test hint");

        // Act
        attribute.Category = "Performance";

        // Assert
        Assert.Equal("Performance", attribute.Category);
    }

    [Fact]
    public void PerformanceHintAttribute_Priority_CanBeSet()
    {
        // Arrange
        var attribute = new PerformanceHintAttribute("Test hint");

        // Act
        attribute.Priority = OptimizationPriority.High;

        // Assert
        Assert.Equal(OptimizationPriority.High, attribute.Priority);
    }

    [Fact]
    public void PerformanceHintAttribute_IsRequired_CanBeSet()
    {
        // Arrange
        var attribute = new PerformanceHintAttribute("Test hint");

        // Act
        attribute.IsRequired = true;

        // Assert
        Assert.True(attribute.IsRequired);
    }

    [Fact]
    public void PerformanceHintAttribute_AllProperties_CanBeSet()
    {
        // Arrange
        var attribute = new PerformanceHintAttribute("Test hint");

        // Act
        attribute.Category = "Memory";
        attribute.Priority = OptimizationPriority.Critical;
        attribute.IsRequired = true;

        // Assert
        Assert.Equal("Test hint", attribute.Hint);
        Assert.Equal("Memory", attribute.Category);
        Assert.Equal(OptimizationPriority.Critical, attribute.Priority);
        Assert.True(attribute.IsRequired);
    }

    [Fact]
    public void PerformanceHintAttribute_Hint_IsReadOnly()
    {
        // Arrange
        var attribute = new PerformanceHintAttribute("Original hint");

        // Assert - Hint property should not have a setter
        Assert.Equal("Original hint", attribute.Hint);
        // Note: We can't test that Hint is read-only directly, but the constructor sets it
    }

    [Fact]
    public void PerformanceHintAttribute_InheritsFromAttribute()
    {
        // Act
        var attribute = new PerformanceHintAttribute("Test hint");

        // Assert
        Assert.IsAssignableFrom<Attribute>(attribute);
    }

    [Fact]
    public void PerformanceHintAttribute_HasCorrectAttributeUsage()
    {
        // Act
        var attributeType = typeof(PerformanceHintAttribute);
        var attributeUsage = attributeType.GetCustomAttributes(typeof(AttributeUsageAttribute), false);

        // Assert
        Assert.Single(attributeUsage);
        var usage = (AttributeUsageAttribute)attributeUsage[0];
        Assert.Equal(AttributeTargets.Method | AttributeTargets.Class, usage.ValidOn);
        Assert.True(usage.AllowMultiple);
    }

    [Fact]
    public void PerformanceHintAttribute_CanBeAppliedToClass()
    {
        // Act
        var attribute = typeof(TestClassWithPerformanceHint).GetCustomAttribute<PerformanceHintAttribute>();

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("Class-level performance hint", attribute.Hint);
        Assert.Equal("Architecture", attribute.Category);
        Assert.Equal(OptimizationPriority.High, attribute.Priority);
        Assert.True(attribute.IsRequired);
    }

    [Fact]
    public void PerformanceHintAttribute_CanBeAppliedToMethod()
    {
        // Act
        var method = typeof(TestClassWithPerformanceHint).GetMethod(nameof(TestClassWithPerformanceHint.TestMethod));
        var attribute = method!.GetCustomAttribute<PerformanceHintAttribute>();

        // Assert
        Assert.NotNull(attribute);
        Assert.Equal("Method-level performance hint", attribute.Hint);
        Assert.Equal("Performance", attribute.Category);
        Assert.Equal(OptimizationPriority.Critical, attribute.Priority);
        Assert.False(attribute.IsRequired);
    }

    [Fact]
    public void PerformanceHintAttribute_AllowsMultipleAttributes()
    {
        // Act
        var method = typeof(TestClassWithPerformanceHint).GetMethod(nameof(TestClassWithPerformanceHint.TestMethodWithMultipleHints));
        var attributes = method!.GetCustomAttributes<PerformanceHintAttribute>();

        // Assert
        Assert.Equal(2, attributes.Count());
        var firstHint = attributes.First(a => a.Hint.Contains("First"));
        var secondHint = attributes.First(a => a.Hint.Contains("Second"));

        Assert.Equal("First performance hint", firstHint.Hint);
        Assert.Equal("Memory", firstHint.Category);
        Assert.Equal(OptimizationPriority.Medium, firstHint.Priority);

        Assert.Equal("Second performance hint", secondHint.Hint);
        Assert.Equal("CPU", secondHint.Category);
        Assert.Equal(OptimizationPriority.High, secondHint.Priority);
    }

    [PerformanceHint("Class-level performance hint", Category = "Architecture", Priority = OptimizationPriority.High, IsRequired = true)]
    private class TestClassWithPerformanceHint
    {
        [PerformanceHint("Method-level performance hint", Category = "Performance", Priority = OptimizationPriority.Critical)]
        public void TestMethod()
        {
        }

        [PerformanceHint("First performance hint", Category = "Memory", Priority = OptimizationPriority.Medium)]
        [PerformanceHint("Second performance hint", Category = "CPU", Priority = OptimizationPriority.High)]
        public void TestMethodWithMultipleHints()
        {
        }
    }
}