using System;
using Relay.Core;
using Xunit;

namespace Relay.Core.Tests.Metadata
{
    public class OpenApiParameterTests
    {
        [Fact]
        public void OpenApiParameter_ShouldInitializeWithDefaults()
        {
            // Act
            var parameter = new OpenApiParameter();

            // Assert
            Assert.Equal(string.Empty, parameter.Name);
            Assert.Equal("query", parameter.In);
            Assert.False(parameter.Required);
            Assert.Null(parameter.Description);
            Assert.Null(parameter.Schema);
        }

        [Fact]
        public void OpenApiParameter_ShouldAllowSettingName()
        {
            // Arrange
            var parameter = new OpenApiParameter();
            var name = "userId";

            // Act
            parameter.Name = name;

            // Assert
            Assert.Equal(name, parameter.Name);
        }

        [Fact]
        public void OpenApiParameter_ShouldAllowSettingNameToEmptyString()
        {
            // Arrange
            var parameter = new OpenApiParameter
            {
                Name = "originalName"
            };

            // Act
            parameter.Name = string.Empty;

            // Assert
            Assert.Equal(string.Empty, parameter.Name);
        }

        [Fact]
        public void OpenApiParameter_ShouldAllowSettingIn()
        {
            // Arrange
            var parameter = new OpenApiParameter();

            // Act
            parameter.In = "header";

            // Assert
            Assert.Equal("header", parameter.In);
        }

        [Fact]
        public void OpenApiParameter_ShouldAllowSettingInToPath()
        {
            // Arrange
            var parameter = new OpenApiParameter();

            // Act
            parameter.In = "path";

            // Assert
            Assert.Equal("path", parameter.In);
        }

        [Fact]
        public void OpenApiParameter_ShouldAllowSettingInToCookie()
        {
            // Arrange
            var parameter = new OpenApiParameter();

            // Act
            parameter.In = "cookie";

            // Assert
            Assert.Equal("cookie", parameter.In);
        }

        [Fact]
        public void OpenApiParameter_ShouldAllowSettingRequired()
        {
            // Arrange
            var parameter = new OpenApiParameter();

            // Act
            parameter.Required = true;

            // Assert
            Assert.True(parameter.Required);
        }

        [Fact]
        public void OpenApiParameter_ShouldAllowSettingRequiredToFalse()
        {
            // Arrange
            var parameter = new OpenApiParameter
            {
                Required = true
            };

            // Act
            parameter.Required = false;

            // Assert
            Assert.False(parameter.Required);
        }

        [Fact]
        public void OpenApiParameter_ShouldAllowSettingDescription()
        {
            // Arrange
            var parameter = new OpenApiParameter();
            var description = "The unique identifier of the user";

            // Act
            parameter.Description = description;

            // Assert
            Assert.Equal(description, parameter.Description);
        }

        [Fact]
        public void OpenApiParameter_ShouldAllowSettingDescriptionToNull()
        {
            // Arrange
            var parameter = new OpenApiParameter
            {
                Description = "Initial description"
            };

            // Act
            parameter.Description = null;

            // Assert
            Assert.Null(parameter.Description);
        }

        [Fact]
        public void OpenApiParameter_ShouldAllowSettingSchema()
        {
            // Arrange
            var parameter = new OpenApiParameter();
            var schema = new OpenApiSchema
            {
                Type = "integer",
                Format = "int64"
            };

            // Act
            parameter.Schema = schema;

            // Assert
            Assert.NotNull(parameter.Schema);
            Assert.Equal("integer", parameter.Schema.Type);
            Assert.Equal("int64", parameter.Schema.Format);
        }

        [Fact]
        public void OpenApiParameter_ShouldAllowSettingSchemaToNull()
        {
            // Arrange
            var parameter = new OpenApiParameter
            {
                Schema = new OpenApiSchema { Type = "string" }
            };

            // Act
            parameter.Schema = null;

            // Assert
            Assert.Null(parameter.Schema);
        }

        [Fact]
        public void OpenApiParameter_ShouldAllowObjectInitialization()
        {
            // Arrange
            var schema = new OpenApiSchema
            {
                Type = "string",
                Format = "email"
            };

            // Act
            var parameter = new OpenApiParameter
            {
                Name = "searchTerm",
                In = "query",
                Required = false,
                Description = "Search term for filtering results",
                Schema = schema
            };

            // Assert
            Assert.Equal("searchTerm", parameter.Name);
            Assert.Equal("query", parameter.In);
            Assert.False(parameter.Required);
            Assert.Equal("Search term for filtering results", parameter.Description);
            Assert.NotNull(parameter.Schema);
            Assert.Equal("string", parameter.Schema.Type);
            Assert.Equal("email", parameter.Schema.Format);
        }

        [Fact]
        public void OpenApiParameter_ShouldAllowPartialObjectInitialization()
        {
            // Act
            var parameter = new OpenApiParameter
            {
                Name = "id",
                In = "path",
                Required = true
            };

            // Assert
            Assert.Equal("id", parameter.Name);
            Assert.Equal("path", parameter.In);
            Assert.True(parameter.Required);
            Assert.Null(parameter.Description);
            Assert.Null(parameter.Schema);
        }

        [Fact]
        public void OpenApiParameter_ShouldHandleEmptyStringDescription()
        {
            // Arrange
            var parameter = new OpenApiParameter();

            // Act
            parameter.Description = string.Empty;

            // Assert
            Assert.Equal(string.Empty, parameter.Description);
        }

        [Fact]
        public void OpenApiParameter_ShouldBeIndependentInstances()
        {
            // Act
            var parameter1 = new OpenApiParameter
            {
                Name = "param1",
                In = "query",
                Required = true,
                Description = "First parameter",
                Schema = new OpenApiSchema { Type = "string" }
            };

            var parameter2 = new OpenApiParameter
            {
                Name = "param2",
                In = "header",
                Required = false,
                Description = "Second parameter",
                Schema = new OpenApiSchema { Type = "integer" }
            };

            // Assert
            Assert.Equal("param1", parameter1.Name);
            Assert.Equal("query", parameter1.In);
            Assert.True(parameter1.Required);
            Assert.Equal("First parameter", parameter1.Description);
            Assert.Equal("string", parameter1.Schema.Type);

            Assert.Equal("param2", parameter2.Name);
            Assert.Equal("header", parameter2.In);
            Assert.False(parameter2.Required);
            Assert.Equal("Second parameter", parameter2.Description);
            Assert.Equal("integer", parameter2.Schema.Type);
        }

        [Fact]
        public void OpenApiParameter_ShouldSupportPathParameter()
        {
            // Act
            var parameter = new OpenApiParameter
            {
                Name = "userId",
                In = "path",
                Required = true,
                Description = "User identifier from URL path",
                Schema = new OpenApiSchema { Type = "integer", Format = "int64" }
            };

            // Assert
            Assert.Equal("userId", parameter.Name);
            Assert.Equal("path", parameter.In);
            Assert.True(parameter.Required);
            Assert.Equal("User identifier from URL path", parameter.Description);
            Assert.NotNull(parameter.Schema);
            Assert.Equal("integer", parameter.Schema.Type);
            Assert.Equal("int64", parameter.Schema.Format);
        }

        [Fact]
        public void OpenApiParameter_ShouldSupportHeaderParameter()
        {
            // Act
            var parameter = new OpenApiParameter
            {
                Name = "Authorization",
                In = "header",
                Required = true,
                Description = "Bearer token for authentication",
                Schema = new OpenApiSchema { Type = "string" }
            };

            // Assert
            Assert.Equal("Authorization", parameter.Name);
            Assert.Equal("header", parameter.In);
            Assert.True(parameter.Required);
            Assert.Equal("Bearer token for authentication", parameter.Description);
            Assert.Equal("string", parameter.Schema.Type);
        }

        [Fact]
        public void OpenApiParameter_ShouldSupportCookieParameter()
        {
            // Act
            var parameter = new OpenApiParameter
            {
                Name = "sessionId",
                In = "cookie",
                Required = false,
                Description = "Session identifier from cookie",
                Schema = new OpenApiSchema { Type = "string" }
            };

            // Assert
            Assert.Equal("sessionId", parameter.Name);
            Assert.Equal("cookie", parameter.In);
            Assert.False(parameter.Required);
            Assert.Equal("Session identifier from cookie", parameter.Description);
            Assert.Equal("string", parameter.Schema.Type);
        }
    }
}