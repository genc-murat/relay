using System;
using System.Linq;
using FluentAssertions;
using Relay.Core;
using Xunit;

namespace Relay.Core.Tests
{
    public class AttributeValidationTests
    {
        public class ExposeAsEndpointAttributeValidationTests
        {
            [Fact]
            public void ValidateExposeAsEndpointAttribute_WithNullAttribute_ShouldThrowArgumentNullException()
            {
                // Act & Assert
                Assert.Throws<ArgumentNullException>(() => 
                    AttributeValidation.ValidateExposeAsEndpointAttribute(null!));
            }

            [Fact]
            public void ValidateExposeAsEndpointAttribute_WithValidAttribute_ShouldReturnNoErrors()
            {
                // Arrange
                var attribute = new ExposeAsEndpointAttribute
                {
                    Route = "/api/users",
                    HttpMethod = "GET",
                    Version = "v1"
                };

                // Act
                var errors = AttributeValidation.ValidateExposeAsEndpointAttribute(attribute);

                // Assert
                errors.Should().BeEmpty();
            }

            [Fact]
            public void ValidateExposeAsEndpointAttribute_WithDefaultValues_ShouldReturnNoErrors()
            {
                // Arrange
                var attribute = new ExposeAsEndpointAttribute();

                // Act
                var errors = AttributeValidation.ValidateExposeAsEndpointAttribute(attribute);

                // Assert
                errors.Should().BeEmpty();
            }

            [Theory]
            [InlineData("GET")]
            [InlineData("POST")]
            [InlineData("PUT")]
            [InlineData("DELETE")]
            [InlineData("PATCH")]
            [InlineData("HEAD")]
            [InlineData("OPTIONS")]
            [InlineData("get")] // Case insensitive
            [InlineData("Post")] // Case insensitive
            public void ValidateExposeAsEndpointAttribute_WithValidHttpMethods_ShouldReturnNoErrors(string httpMethod)
            {
                // Arrange
                var attribute = new ExposeAsEndpointAttribute { HttpMethod = httpMethod };

                // Act
                var errors = AttributeValidation.ValidateExposeAsEndpointAttribute(attribute);

                // Assert
                errors.Should().BeEmpty();
            }

            [Theory]
            [InlineData("")]
            [InlineData(" ")]
            [InlineData("\t")]
            [InlineData(null)]
            public void ValidateExposeAsEndpointAttribute_WithInvalidHttpMethod_ShouldReturnError(string? httpMethod)
            {
                // Arrange
                var attribute = new ExposeAsEndpointAttribute { HttpMethod = httpMethod! };

                // Act
                var errors = AttributeValidation.ValidateExposeAsEndpointAttribute(attribute).ToList();

                // Assert
                errors.Should().HaveCount(1);
                errors[0].Should().Contain("HttpMethod cannot be null or empty");
            }

            [Theory]
            [InlineData("INVALID")]
            [InlineData("TRACE")]
            [InlineData("CONNECT")]
            public void ValidateExposeAsEndpointAttribute_WithUnsupportedHttpMethod_ShouldReturnError(string httpMethod)
            {
                // Arrange
                var attribute = new ExposeAsEndpointAttribute { HttpMethod = httpMethod };

                // Act
                var errors = AttributeValidation.ValidateExposeAsEndpointAttribute(attribute).ToList();

                // Assert
                errors.Should().HaveCount(1);
                errors[0].Should().Contain($"HttpMethod '{httpMethod}' is not valid");
            }

            [Theory]
            [InlineData("/api/users")]
            [InlineData("/")]
            [InlineData("api/users")]
            [InlineData("/api/users/{id}")]
            [InlineData("/api/v1/users")]
            public void ValidateExposeAsEndpointAttribute_WithValidRoutes_ShouldReturnNoErrors(string route)
            {
                // Arrange
                var attribute = new ExposeAsEndpointAttribute { Route = route };

                // Act
                var errors = AttributeValidation.ValidateExposeAsEndpointAttribute(attribute);

                // Assert
                errors.Should().BeEmpty();
            }

            [Theory]
            [InlineData("/api//users")]
            [InlineData("//api/users")]
            public void ValidateExposeAsEndpointAttribute_WithConsecutiveSlashes_ShouldReturnError(string route)
            {
                // Arrange
                var attribute = new ExposeAsEndpointAttribute { Route = route };

                // Act
                var errors = AttributeValidation.ValidateExposeAsEndpointAttribute(attribute).ToList();

                // Assert
                errors.Should().Contain(e => e.Contains("Route cannot contain consecutive forward slashes"));
            }

            [Fact]
            public void ValidateExposeAsEndpointAttribute_WithConsecutiveSlashesAndTrailingSlash_ShouldReturnMultipleErrors()
            {
                // Arrange
                var attribute = new ExposeAsEndpointAttribute { Route = "/api/users//" };

                // Act
                var errors = AttributeValidation.ValidateExposeAsEndpointAttribute(attribute).ToList();

                // Assert
                errors.Should().HaveCount(2);
                errors.Should().Contain(e => e.Contains("Route cannot contain consecutive forward slashes"));
                errors.Should().Contain(e => e.Contains("Route should not end with a trailing slash"));
            }

            [Theory]
            [InlineData("/api/users/")]
            [InlineData("/api/")]
            public void ValidateExposeAsEndpointAttribute_WithTrailingSlash_ShouldReturnError(string route)
            {
                // Arrange
                var attribute = new ExposeAsEndpointAttribute { Route = route };

                // Act
                var errors = AttributeValidation.ValidateExposeAsEndpointAttribute(attribute).ToList();

                // Assert
                errors.Should().HaveCount(1);
                errors[0].Should().Contain("Route should not end with a trailing slash");
            }

            [Theory]
            [InlineData("/api users")]
            [InlineData("/api\tusers")]
            [InlineData("/api\nusers")]
            [InlineData("/api\rusers")]
            public void ValidateExposeAsEndpointAttribute_WithWhitespaceInRoute_ShouldReturnError(string route)
            {
                // Arrange
                var attribute = new ExposeAsEndpointAttribute { Route = route };

                // Act
                var errors = AttributeValidation.ValidateExposeAsEndpointAttribute(attribute).ToList();

                // Assert
                errors.Should().HaveCount(1);
                errors[0].Should().Contain("Route cannot contain whitespace characters");
            }

            [Theory]
            [InlineData("v1")]
            [InlineData("1.0")]
            [InlineData("2024-01-01")]
            [InlineData("beta")]
            public void ValidateExposeAsEndpointAttribute_WithValidVersions_ShouldReturnNoErrors(string version)
            {
                // Arrange
                var attribute = new ExposeAsEndpointAttribute { Version = version };

                // Act
                var errors = AttributeValidation.ValidateExposeAsEndpointAttribute(attribute);

                // Assert
                errors.Should().BeEmpty();
            }

            [Theory]
            [InlineData(" ")]
            [InlineData("   ")]
            public void ValidateExposeAsEndpointAttribute_WithWhitespaceOnlyVersion_ShouldReturnError(string version)
            {
                // Arrange
                var attribute = new ExposeAsEndpointAttribute { Version = version };

                // Act
                var errors = AttributeValidation.ValidateExposeAsEndpointAttribute(attribute).ToList();

                // Assert
                errors.Should().HaveCount(1);
                errors[0].Should().Contain("Version cannot be whitespace only");
            }

            [Fact]
            public void ValidateExposeAsEndpointAttribute_WithTabCharacterVersion_ShouldReturnMultipleErrors()
            {
                // Arrange
                var attribute = new ExposeAsEndpointAttribute { Version = "\t" };

                // Act
                var errors = AttributeValidation.ValidateExposeAsEndpointAttribute(attribute).ToList();

                // Assert
                errors.Should().HaveCount(2);
                errors.Should().Contain(e => e.Contains("Version cannot be whitespace only"));
                errors.Should().Contain(e => e.Contains("Version cannot contain control characters"));
            }

            [Fact]
            public void ValidateExposeAsEndpointAttribute_WithControlCharacterInVersion_ShouldReturnError()
            {
                // Arrange
                var attribute = new ExposeAsEndpointAttribute { Version = "v1\x00" };

                // Act
                var errors = AttributeValidation.ValidateExposeAsEndpointAttribute(attribute).ToList();

                // Assert
                errors.Should().HaveCount(1);
                errors[0].Should().Contain("Version cannot contain control characters");
            }
        }

        public class PriorityValidationTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(100)]
            [InlineData(-100)]
            [InlineData(10000)]
            [InlineData(-10000)]
            public void ValidatePriority_WithValidValues_ShouldReturnNoErrors(int priority)
            {
                // Act
                var errors = AttributeValidation.ValidatePriority(priority);

                // Assert
                errors.Should().BeEmpty();
            }

            [Theory]
            [InlineData(10001)]
            [InlineData(-10001)]
            [InlineData(int.MaxValue)]
            [InlineData(int.MinValue)]
            public void ValidatePriority_WithInvalidValues_ShouldReturnError(int priority)
            {
                // Act
                var errors = AttributeValidation.ValidatePriority(priority).ToList();

                // Assert
                errors.Should().HaveCount(1);
                errors[0].Should().Contain("Priority must be between -10000 and 10000");
            }

            [Fact]
            public void ValidatePriority_WithCustomParameterName_ShouldUseCustomName()
            {
                // Act
                var errors = AttributeValidation.ValidatePriority(20000, "CustomPriority").ToList();

                // Assert
                errors.Should().HaveCount(1);
                errors[0].Should().Contain("CustomPriority must be between -10000 and 10000");
            }
        }

        public class PipelineOrderValidationTests
        {
            [Theory]
            [InlineData(0)]
            [InlineData(1000)]
            [InlineData(-1000)]
            [InlineData(100000)]
            [InlineData(-100000)]
            public void ValidatePipelineOrder_WithValidValues_ShouldReturnNoErrors(int order)
            {
                // Act
                var errors = AttributeValidation.ValidatePipelineOrder(order);

                // Assert
                errors.Should().BeEmpty();
            }

            [Theory]
            [InlineData(100001)]
            [InlineData(-100001)]
            [InlineData(int.MaxValue)]
            [InlineData(int.MinValue)]
            public void ValidatePipelineOrder_WithInvalidValues_ShouldReturnError(int order)
            {
                // Act
                var errors = AttributeValidation.ValidatePipelineOrder(order).ToList();

                // Assert
                errors.Should().HaveCount(1);
                errors[0].Should().Contain("Pipeline order must be between -100000 and 100000");
            }
        }

        public class HandlerNameValidationTests
        {
            [Theory]
            [InlineData(null)]
            [InlineData("")]
            [InlineData("ValidName")]
            [InlineData("Handler123")]
            [InlineData("My-Handler_Name")]
            public void ValidateHandlerName_WithValidNames_ShouldReturnNoErrors(string? name)
            {
                // Act
                var errors = AttributeValidation.ValidateHandlerName(name);

                // Assert
                errors.Should().BeEmpty();
            }

            [Theory]
            [InlineData(" ")]
            [InlineData("   ")]
            public void ValidateHandlerName_WithWhitespaceOnlyName_ShouldReturnError(string name)
            {
                // Act
                var errors = AttributeValidation.ValidateHandlerName(name).ToList();

                // Assert
                errors.Should().HaveCount(1);
                errors[0].Should().Contain("Handler name cannot be whitespace only");
            }

            [Fact]
            public void ValidateHandlerName_WithTabCharacter_ShouldReturnMultipleErrors()
            {
                // Act
                var errors = AttributeValidation.ValidateHandlerName("\t").ToList();

                // Assert
                errors.Should().HaveCount(2);
                errors.Should().Contain(e => e.Contains("Handler name cannot be whitespace only"));
                errors.Should().Contain(e => e.Contains("Handler name cannot contain control characters"));
            }

            [Fact]
            public void ValidateHandlerName_WithTooLongName_ShouldReturnError()
            {
                // Arrange
                var longName = new string('a', 201);

                // Act
                var errors = AttributeValidation.ValidateHandlerName(longName).ToList();

                // Assert
                errors.Should().HaveCount(1);
                errors[0].Should().Contain("Handler name cannot exceed 200 characters");
            }

            [Fact]
            public void ValidateHandlerName_WithControlCharacter_ShouldReturnError()
            {
                // Arrange
                var nameWithControlChar = "Handler\x00Name";

                // Act
                var errors = AttributeValidation.ValidateHandlerName(nameWithControlChar).ToList();

                // Assert
                errors.Should().HaveCount(1);
                errors[0].Should().Contain("Handler name cannot contain control characters");
            }
        }

        public class ValidHttpMethodsTests
        {
            [Fact]
            public void ValidHttpMethods_ShouldContainExpectedMethods()
            {
                // Assert
                AttributeValidation.ValidHttpMethods.Should().Contain(new[]
                {
                    "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS"
                });
            }

            [Fact]
            public void ValidHttpMethods_ShouldBeCaseInsensitive()
            {
                // Assert
                AttributeValidation.ValidHttpMethods.Contains("get").Should().BeTrue();
                AttributeValidation.ValidHttpMethods.Contains("GET").Should().BeTrue();
                AttributeValidation.ValidHttpMethods.Contains("Post").Should().BeTrue();
                AttributeValidation.ValidHttpMethods.Contains("POST").Should().BeTrue();
            }
        }
    }
}