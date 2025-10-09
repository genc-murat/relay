using System;
using Relay.Core.Caching.Attributes;
using Xunit;

namespace Relay.Core.Tests.Caching;

public class UnifiedCacheAttributeTests
{
    [Fact]
    public void Constructor_WithDefaultValues_ShouldSetCorrectDefaults()
    {
        // Arrange & Act
        var attribute = new UnifiedCacheAttribute();

        // Assert
        Assert.Equal(300, attribute.AbsoluteExpirationSeconds);
        Assert.Equal(0, attribute.SlidingExpirationSeconds);
        Assert.Empty(attribute.Tags);
        Assert.Equal(CachePriority.Normal, attribute.Priority);
        Assert.True(attribute.EnableCompression);
        Assert.False(attribute.Preload);
        Assert.Equal("default", attribute.Region);
        Assert.Equal("{RequestType}:{RequestHash}", attribute.KeyPattern);
        Assert.True(attribute.EnableMetrics);
        Assert.False(attribute.UseDistributedCache);
        Assert.True(attribute.Enabled);
    }

    [Fact]
    public void Constructor_WithAbsoluteExpiration_ShouldSetCorrectValue()
    {
        // Arrange & Act
        var attribute = new UnifiedCacheAttribute(600);

        // Assert
        Assert.Equal(600, attribute.AbsoluteExpirationSeconds);
        Assert.Equal(0, attribute.SlidingExpirationSeconds);
    }

    [Fact]
    public void Constructor_WithAbsoluteAndSlidingExpiration_ShouldSetCorrectValues()
    {
        // Arrange & Act
        var attribute = new UnifiedCacheAttribute(600, 300);

        // Assert
        Assert.Equal(600, attribute.AbsoluteExpirationSeconds);
        Assert.Equal(300, attribute.SlidingExpirationSeconds);
    }

    [Fact]
    public void Constructor_WithInvalidAbsoluteExpiration_ShouldThrowException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnifiedCacheAttribute(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnifiedCacheAttribute(-1));
    }

    [Fact]
    public void Constructor_WithInvalidSlidingExpiration_ShouldThrowException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new UnifiedCacheAttribute(300, -1));
    }

    [Theory]
    [InlineData(CachePriority.Low)]
    [InlineData(CachePriority.Normal)]
    [InlineData(CachePriority.High)]
    [InlineData(CachePriority.Never)]
    public void Priority_SetAndGet_ShouldWorkCorrectly(CachePriority priority)
    {
        // Arrange & Act
        var attribute = new UnifiedCacheAttribute();
        attribute.Priority = priority;

        // Assert
        Assert.Equal(priority, attribute.Priority);
    }

    [Fact]
    public void Tags_SetAndGet_ShouldWorkCorrectly()
    {
        // Arrange
        var tags = new[] { "tag1", "tag2", "tag3" };
        var attribute = new UnifiedCacheAttribute();

        // Act
        attribute.Tags = tags;

        // Assert
        Assert.Equal(tags, attribute.Tags);
    }

    [Fact]
    public void Region_SetAndGet_ShouldWorkCorrectly()
    {
        // Arrange
        const string region = "test-region";
        var attribute = new UnifiedCacheAttribute();

        // Act
        attribute.Region = region;

        // Assert
        Assert.Equal(region, attribute.Region);
    }

    [Fact]
    public void KeyPattern_SetAndGet_ShouldWorkCorrectly()
    {
        // Arrange
        const string keyPattern = "custom-{RequestType}-{RequestHash}";
        var attribute = new UnifiedCacheAttribute();

        // Act
        attribute.KeyPattern = keyPattern;

        // Assert
        Assert.Equal(keyPattern, attribute.KeyPattern);
    }

    [Fact]
    public void BooleanProperties_SetAndGet_ShouldWorkCorrectly()
    {
        // Arrange
        var attribute = new UnifiedCacheAttribute();

        // Act & Assert
        attribute.EnableCompression = false;
        Assert.False(attribute.EnableCompression);

        attribute.Preload = true;
        Assert.True(attribute.Preload);

        attribute.EnableMetrics = false;
        Assert.False(attribute.EnableMetrics);

        attribute.UseDistributedCache = true;
        Assert.True(attribute.UseDistributedCache);

        attribute.Enabled = false;
        Assert.False(attribute.Enabled);
    }

    [Fact]
    public void AttributeUsage_ShouldAllowClassTargetsAndInheritance()
    {
        // Arrange & Act
        var attributeUsage = typeof(UnifiedCacheAttribute).GetCustomAttributes(
            typeof(AttributeUsageAttribute), false)[0] as AttributeUsageAttribute;

        // Assert
        Assert.NotNull(attributeUsage);
        Assert.True(attributeUsage.Inherited);
        Assert.False(attributeUsage.AllowMultiple);
        Assert.Equal(AttributeTargets.Class, attributeUsage.ValidOn);
    }
}