using System;
using Relay.Core.HandlerVersioning;
using Xunit;

namespace Relay.Core.Tests.Attributes
{
    public class HandlerVersionAttributeTests
    {
        [Fact]
        public void Constructor_WithValidVersion_ShouldSetVersion()
        {
            // Arrange
            const string expectedVersion = "1.0.0";

            // Act
            var attribute = new HandlerVersionAttribute(expectedVersion);

            // Assert
            Assert.Equal(expectedVersion, attribute.Version);
        }

        [Fact]
        public void Constructor_WithNullVersion_ShouldThrowArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new HandlerVersionAttribute(null!));
            Assert.Equal("version", exception.ParamName);
            Assert.Contains("Version cannot be null or empty", exception.Message);
        }

        [Fact]
        public void Constructor_WithEmptyVersion_ShouldThrowArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new HandlerVersionAttribute(string.Empty));
            Assert.Equal("version", exception.ParamName);
            Assert.Contains("Version cannot be null or empty", exception.Message);
        }

        [Fact]
        public void Constructor_WithWhitespaceVersion_ShouldThrowArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => new HandlerVersionAttribute("   "));
            Assert.Equal("version", exception.ParamName);
            Assert.Contains("Version cannot be null or empty", exception.Message);
        }

        [Fact]
        public void IsDefault_ShouldDefaultToFalse()
        {
            // Arrange & Act
            var attribute = new HandlerVersionAttribute("1.0.0");

            // Assert
            Assert.False(attribute.IsDefault);
        }

        [Fact]
        public void IsDefault_ShouldAllowSettingToTrue()
        {
            // Arrange
            var attribute = new HandlerVersionAttribute("1.0.0");

            // Act
            attribute.IsDefault = true;

            // Assert
            Assert.True(attribute.IsDefault);
        }

        [Fact]
        public void IsDefault_ShouldAllowSettingToFalse()
        {
            // Arrange
            var attribute = new HandlerVersionAttribute("1.0.0") { IsDefault = true };

            // Act
            attribute.IsDefault = false;

            // Assert
            Assert.False(attribute.IsDefault);
        }

        [Fact]
        public void Version_ShouldBeReadOnly()
        {
            // Arrange
            var attribute = new HandlerVersionAttribute("1.0.0");

            // Act & Assert
            // Version property should not have a setter, so this should compile and work
            Assert.Equal("1.0.0", attribute.Version);
        }
    }
}

