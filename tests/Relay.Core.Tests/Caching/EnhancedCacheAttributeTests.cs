using FluentAssertions;
using Relay.Core.Caching.Attributes;
using System;
using System.Linq;
using Xunit;

namespace Relay.Core.Tests.Caching;

public class EnhancedCacheAttributeTests
{
    [Fact]
    public void Constructor_WithDefaultValues_ShouldSetDefaults()
    {
        // Arrange & Act
        var attribute = new EnhancedCacheAttribute();

        // Assert
        attribute.AbsoluteExpirationSeconds.Should().Be(300);
        attribute.SlidingExpirationSeconds.Should().Be(0);
        attribute.Tags.Should().BeEmpty();
        attribute.Priority.Should().Be(CachePriority.Normal);
        attribute.EnableCompression.Should().BeTrue();
        attribute.Preload.Should().BeFalse();
        attribute.Region.Should().Be("default");
        attribute.KeyPattern.Should().Be("{RequestType}:{RequestHash}");
        attribute.EnableMetrics.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithExpirationSeconds_ShouldSetExpiration()
    {
        // Arrange & Act
        var attribute = new EnhancedCacheAttribute(600);

        // Assert
        attribute.AbsoluteExpirationSeconds.Should().Be(600);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidExpirationSeconds_ShouldThrowException(int seconds)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new EnhancedCacheAttribute(seconds));
    }

    [Fact]
    public void CanSetAndGetProperties()
    {
        // Arrange
        var attribute = new EnhancedCacheAttribute();

        // Act
        attribute.AbsoluteExpirationSeconds = 1200;
        attribute.SlidingExpirationSeconds = 300;
        attribute.Tags = new[] { "tag1", "tag2" };
        attribute.Priority = CachePriority.High;
        attribute.EnableCompression = false;
        attribute.Preload = true;
        attribute.Region = "test-region";
        attribute.KeyPattern = "custom:{RequestType}:{id}";
        attribute.EnableMetrics = false;

        // Assert
        attribute.AbsoluteExpirationSeconds.Should().Be(1200);
        attribute.SlidingExpirationSeconds.Should().Be(300);
        attribute.Tags.Should().BeEquivalentTo(new[] { "tag1", "tag2" });
        attribute.Priority.Should().Be(CachePriority.High);
        attribute.EnableCompression.Should().BeFalse();
        attribute.Preload.Should().BeTrue();
        attribute.Region.Should().Be("test-region");
        attribute.KeyPattern.Should().Be("custom:{RequestType}:{id}");
        attribute.EnableMetrics.Should().BeFalse();
    }

    [Fact]
    public void AttributeUsage_ShouldAllowClassTargetsAndInheritance()
    {
        // Arrange & Act
        var attributeUsage = typeof(EnhancedCacheAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .First();

        // Assert
        attributeUsage.ValidOn.Should().Be(AttributeTargets.Class);
        attributeUsage.Inherited.Should().BeTrue();
        attributeUsage.AllowMultiple.Should().BeFalse();
    }
}