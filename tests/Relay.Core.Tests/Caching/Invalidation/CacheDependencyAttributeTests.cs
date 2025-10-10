using Relay.Core.Caching.Invalidation;
using System;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.Caching.Invalidation;

public class CacheDependencyAttributeTests
{
    [Fact]
    public void Constructor_WithParameters_ShouldSetProperties()
    {
        // Arrange
        var dependencyKey = "user:123";
        var dependencyType = CacheDependencyType.InvalidateOnUpdate;

        // Act
        var attribute = new CacheDependencyAttribute(dependencyKey, dependencyType);

        // Assert
        Assert.Equal(dependencyKey, attribute.DependencyKey);
        Assert.Equal(dependencyType, attribute.DependencyType);
    }

    [Fact]
    public void Constructor_WithOnlyDependencyKey_ShouldSetDefaultDependencyType()
    {
        // Arrange
        var dependencyKey = "user:123";

        // Act
        var attribute = new CacheDependencyAttribute(dependencyKey);

        // Assert
        Assert.Equal(dependencyKey, attribute.DependencyKey);
        Assert.Equal(CacheDependencyType.InvalidateOnUpdate, attribute.DependencyType);
    }

    [Fact]
    public void Constructor_WithNullDependencyKey_ShouldThrowException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new CacheDependencyAttribute(null!));
    }

    [Theory]
    [InlineData(CacheDependencyType.InvalidateOnUpdate)]
    [InlineData(CacheDependencyType.InvalidateOnCreate)]
    [InlineData(CacheDependencyType.InvalidateOnDelete)]
    [InlineData(CacheDependencyType.InvalidateOnAnyChange)]
    public void Constructor_WithDifferentDependencyTypes_ShouldSetCorrectly(CacheDependencyType dependencyType)
    {
        // Arrange
        var dependencyKey = "test-key";

        // Act
        var attribute = new CacheDependencyAttribute(dependencyKey, dependencyType);

        // Assert
        Assert.Equal(dependencyKey, attribute.DependencyKey);
        Assert.Equal(dependencyType, attribute.DependencyType);
    }

    [Fact]
    public void AttributeUsage_ShouldAllowMultipleAndClassTargets()
    {
        // Arrange & Act
        var attributeUsage = typeof(CacheDependencyAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .First();

        // Assert
        Assert.Equal(AttributeTargets.Class, attributeUsage.ValidOn);
        Assert.False(attributeUsage.Inherited);
        Assert.True(attributeUsage.AllowMultiple);
    }

    [Fact]
    public void CacheDependencyType_Enum_ShouldHaveCorrectValues()
    {
        // Arrange & Act & Assert
        Assert.Equal(4, Enum.GetNames(typeof(CacheDependencyType)).Length);
        Assert.True(Enum.IsDefined(typeof(CacheDependencyType), CacheDependencyType.InvalidateOnUpdate));
        Assert.True(Enum.IsDefined(typeof(CacheDependencyType), CacheDependencyType.InvalidateOnCreate));
        Assert.True(Enum.IsDefined(typeof(CacheDependencyType), CacheDependencyType.InvalidateOnDelete));
        Assert.True(Enum.IsDefined(typeof(CacheDependencyType), CacheDependencyType.InvalidateOnAnyChange));
    }
}