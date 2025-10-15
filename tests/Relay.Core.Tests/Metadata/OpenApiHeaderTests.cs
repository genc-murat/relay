using System;
using Relay.Core;
using Xunit;

namespace Relay.Core.Tests.Metadata
{
    public class OpenApiHeaderTests
    {
        [Fact]
        public void OpenApiHeader_ShouldInitializeWithDefaults()
        {
            // Act
            var header = new OpenApiHeader();

            // Assert
            Assert.Null(header.Description);
            Assert.False(header.Required);
            Assert.Null(header.Schema);
        }

        [Fact]
        public void OpenApiHeader_ShouldAllowSettingDescription()
        {
            // Arrange
            var header = new OpenApiHeader();
            var description = "Authorization header for API access";

            // Act
            header.Description = description;

            // Assert
            Assert.Equal(description, header.Description);
        }

        [Fact]
        public void OpenApiHeader_ShouldAllowSettingDescriptionToNull()
        {
            // Arrange
            var header = new OpenApiHeader
            {
                Description = "Initial description"
            };

            // Act
            header.Description = null;

            // Assert
            Assert.Null(header.Description);
        }

        [Fact]
        public void OpenApiHeader_ShouldAllowSettingRequired()
        {
            // Arrange
            var header = new OpenApiHeader();

            // Act
            header.Required = true;

            // Assert
            Assert.True(header.Required);
        }

        [Fact]
        public void OpenApiHeader_ShouldAllowSettingRequiredToFalse()
        {
            // Arrange
            var header = new OpenApiHeader
            {
                Required = true
            };

            // Act
            header.Required = false;

            // Assert
            Assert.False(header.Required);
        }

        [Fact]
        public void OpenApiHeader_ShouldAllowSettingSchema()
        {
            // Arrange
            var header = new OpenApiHeader();
            var schema = new OpenApiSchema
            {
                Type = "string",
                Description = "JWT token"
            };

            // Act
            header.Schema = schema;

            // Assert
            Assert.NotNull(header.Schema);
            Assert.Equal("string", header.Schema.Type);
            Assert.Equal("JWT token", header.Schema.Description);
        }

        [Fact]
        public void OpenApiHeader_ShouldAllowSettingSchemaToNull()
        {
            // Arrange
            var header = new OpenApiHeader
            {
                Schema = new OpenApiSchema { Type = "string" }
            };

            // Act
            header.Schema = null;

            // Assert
            Assert.Null(header.Schema);
        }

        [Fact]
        public void OpenApiHeader_ShouldAllowObjectInitialization()
        {
            // Arrange
            var schema = new OpenApiSchema
            {
                Type = "string",
                Format = "bearer"
            };

            // Act
            var header = new OpenApiHeader
            {
                Description = "Bearer token for authentication",
                Required = true,
                Schema = schema
            };

            // Assert
            Assert.Equal("Bearer token for authentication", header.Description);
            Assert.True(header.Required);
            Assert.NotNull(header.Schema);
            Assert.Equal("string", header.Schema.Type);
            Assert.Equal("bearer", header.Schema.Format);
        }

        [Fact]
        public void OpenApiHeader_ShouldAllowPartialObjectInitialization()
        {
            // Act
            var header = new OpenApiHeader
            {
                Description = "Optional header",
                Required = false
            };

            // Assert
            Assert.Equal("Optional header", header.Description);
            Assert.False(header.Required);
            Assert.Null(header.Schema);
        }

        [Fact]
        public void OpenApiHeader_ShouldHandleEmptyStringDescription()
        {
            // Arrange
            var header = new OpenApiHeader();

            // Act
            header.Description = string.Empty;

            // Assert
            Assert.Equal(string.Empty, header.Description);
        }

        [Fact]
        public void OpenApiHeader_ShouldBeIndependentInstances()
        {
            // Act
            var header1 = new OpenApiHeader
            {
                Description = "Header 1",
                Required = true,
                Schema = new OpenApiSchema { Type = "string" }
            };

            var header2 = new OpenApiHeader
            {
                Description = "Header 2",
                Required = false,
                Schema = new OpenApiSchema { Type = "integer" }
            };

            // Assert
            Assert.Equal("Header 1", header1.Description);
            Assert.True(header1.Required);
            Assert.Equal("string", header1.Schema.Type);

            Assert.Equal("Header 2", header2.Description);
            Assert.False(header2.Required);
            Assert.Equal("integer", header2.Schema.Type);
        }
    }
}