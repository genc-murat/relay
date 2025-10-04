
using System;
using System.Reflection;
using FluentAssertions;
using Relay.Core.Caching;
using Xunit;

namespace Relay.Core.Tests.Caching
{
    public class DistributedCacheAttributeTests
    {
        [Fact]
        public void Constructor_ShouldHaveCorrectDefaultValues()
        {
            // Arrange & Act
            var attribute = new DistributedCacheAttribute();

            // Assert
            attribute.AbsoluteExpirationSeconds.Should().Be(300);
            attribute.SlidingExpirationSeconds.Should().Be(0);
            attribute.KeyPattern.Should().Be("{RequestType}:{RequestHash}");
            attribute.Region.Should().Be("default");
            attribute.Enabled.Should().Be(true);
        }

        [Fact]
        public void Properties_CanBeSetCorrectly()
        {
            // Arrange
            var attribute = new DistributedCacheAttribute
            {
                AbsoluteExpirationSeconds = 120,
                SlidingExpirationSeconds = 30,
                KeyPattern = "custom:{Region}",
                Region = "custom-region",
                Enabled = false
            };

            // Assert
            attribute.AbsoluteExpirationSeconds.Should().Be(120);
            attribute.SlidingExpirationSeconds.Should().Be(30);
            attribute.KeyPattern.Should().Be("custom:{Region}");
            attribute.Region.Should().Be("custom-region");
            attribute.Enabled.Should().Be(false);
        }

        [Fact]
        public void AttributeUsage_ShouldBeClassOnly()
        {
            // Arrange
            var usage = typeof(DistributedCacheAttribute).GetCustomAttribute<AttributeUsageAttribute>();

            // Assert
            usage.Should().NotBeNull();
            usage!.ValidOn.Should().Be(AttributeTargets.Class);
            usage.AllowMultiple.Should().BeFalse();
            usage.Inherited.Should().BeTrue();
        }

        [DistributedCache(
            AbsoluteExpirationSeconds = 600,
            SlidingExpirationSeconds = 60,
            KeyPattern = "MyRegion:{RequestType}",
            Region = "MyRegion",
            Enabled = false)]
        private class TestRequestWithAttribute { }

        [Fact]
        public void GetFromClass_ShouldReturnCorrectlyConfiguredAttribute()
        {
            // Arrange
            var type = typeof(TestRequestWithAttribute);

            // Act
            var attribute = type.GetCustomAttribute<DistributedCacheAttribute>();

            // Assert
            attribute.Should().NotBeNull();
            attribute!.AbsoluteExpirationSeconds.Should().Be(600);
            attribute.SlidingExpirationSeconds.Should().Be(60);
            attribute.KeyPattern.Should().Be("MyRegion:{RequestType}");
            attribute.Region.Should().Be("MyRegion");
            attribute.Enabled.Should().Be(false);
        }
    }
}
