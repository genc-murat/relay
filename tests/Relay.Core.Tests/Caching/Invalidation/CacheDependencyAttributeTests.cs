using FluentAssertions;
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
        attribute.DependencyKey.Should().Be(dependencyKey);
        attribute.DependencyType.Should().Be(dependencyType);
    }

    [Fact]
    public void Constructor_WithOnlyDependencyKey_ShouldSetDefaultDependencyType()
    {
        // Arrange
        var dependencyKey = "user:123";

        // Act
        var attribute = new CacheDependencyAttribute(dependencyKey);

        // Assert
        attribute.DependencyKey.Should().Be(dependencyKey);
        attribute.DependencyType.Should().Be(CacheDependencyType.InvalidateOnUpdate);
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
        attribute.DependencyKey.Should().Be(dependencyKey);
        attribute.DependencyType.Should().Be(dependencyType);
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
        attributeUsage.ValidOn.Should().Be(AttributeTargets.Class);
        attributeUsage.Inherited.Should().BeFalse();
        attributeUsage.AllowMultiple.Should().BeTrue();
    }

    [Fact]
    public void CacheDependencyType_Enum_ShouldHaveCorrectValues()
    {
        // Arrange & Act & Assert
        Enum.GetNames(typeof(CacheDependencyType)).Should().HaveCount(4);
        Enum.IsDefined(typeof(CacheDependencyType), CacheDependencyType.InvalidateOnUpdate).Should().BeTrue();
        Enum.IsDefined(typeof(CacheDependencyType), CacheDependencyType.InvalidateOnCreate).Should().BeTrue();
        Enum.IsDefined(typeof(CacheDependencyType), CacheDependencyType.InvalidateOnDelete).Should().BeTrue();
        Enum.IsDefined(typeof(CacheDependencyType), CacheDependencyType.InvalidateOnAnyChange).Should().BeTrue();
    }
}