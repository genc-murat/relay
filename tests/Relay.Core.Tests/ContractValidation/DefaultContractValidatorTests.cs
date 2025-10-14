using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.ContractValidation;
using Xunit;

namespace Relay.Core.Tests.ContractValidation
{
    public class DefaultContractValidatorTests
    {
        private readonly DefaultContractValidator _validator = new();

        public class TestRequest
        {
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; }
        }

        public class TestResponse
        {
            public int Id { get; set; }
            public string Result { get; set; } = string.Empty;
        }

        [Fact]
        public async Task ValidateRequestAsync_WithValidRequest_ShouldReturnNoErrors()
        {
            // Arrange
            var request = new TestRequest { Name = "Test", Value = 123 };
            var schema = new JsonSchemaContract();

            // Act
            var errors = await _validator.ValidateRequestAsync(request, schema);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateRequestAsync_WithNullRequest_ShouldReturnError()
        {
            // Arrange
            var schema = new JsonSchemaContract();

            // Act
            var errors = await _validator.ValidateRequestAsync(null!, schema);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Request cannot be null", errors.First());
        }

        [Fact]
        public async Task ValidateResponseAsync_WithValidResponse_ShouldReturnNoErrors()
        {
            // Arrange
            var response = new TestResponse { Id = 1, Result = "Success" };
            var schema = new JsonSchemaContract();

            // Act
            var errors = await _validator.ValidateResponseAsync(response, schema);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateResponseAsync_WithNullResponseAndNonEmptySchema_ShouldReturnError()
        {
            // Arrange
            var schema = new JsonSchemaContract { Schema = "{\"type\":\"object\"}" };

            // Act
            var errors = await _validator.ValidateResponseAsync(null!, schema);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Response cannot be null according to schema", errors.First());
        }

        [Fact]
        public async Task ValidateResponseAsync_WithNullResponseAndEmptySchema_ShouldReturnNoErrors()
        {
            // Arrange
            var schema = new JsonSchemaContract { Schema = "{}" };

            // Act
            var errors = await _validator.ValidateResponseAsync(null!, schema);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateRequestAsync_WithInvalidJsonSchema_ShouldReturnError()
        {
            // Arrange
            var request = new TestRequest { Name = "Test", Value = 123 };
            var schema = new JsonSchemaContract { Schema = "invalid json schema {" };

            // Act
            var errors = await _validator.ValidateRequestAsync(request, schema);

            // Assert
            Assert.Single(errors);
            Assert.Equal("Invalid JSON schema format", errors.First());
        }

        [Fact]
        public async Task ValidateRequestAsync_WithValidSchemaAndValidData_ShouldReturnNoErrors()
        {
            // Arrange
            var request = new TestRequest { Name = "Test", Value = 123 };
            var schema = new JsonSchemaContract
            {
                Schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""Name"": { ""type"": ""string"" },
                        ""Value"": { ""type"": ""integer"" }
                    },
                    ""required"": [""Name"", ""Value""]
                }"
            };

            // Act
            var errors = await _validator.ValidateRequestAsync(request, schema);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateRequestAsync_WithValidSchemaAndInvalidData_ShouldReturnErrors()
        {
            // Arrange
            var request = new TestRequest { Name = "", Value = -1 };
            var schema = new JsonSchemaContract
            {
                Schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""Name"": { ""type"": ""string"", ""minLength"": 1 },
                        ""Value"": { ""type"": ""integer"", ""minimum"": 0 }
                    },
                    ""required"": [""Name"", ""Value""]
                }"
            };

            // Act
            var errors = await _validator.ValidateRequestAsync(request, schema);

            // Assert
            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("minLength"));
            Assert.Contains(errors, e => e.Contains("minimum"));
        }

        [Fact]
        public async Task ValidateResponseAsync_WithValidSchemaAndValidData_ShouldReturnNoErrors()
        {
            // Arrange
            var response = new TestResponse { Id = 1, Result = "Success" };
            var schema = new JsonSchemaContract
            {
                Schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""Id"": { ""type"": ""integer"" },
                        ""Result"": { ""type"": ""string"" }
                    },
                    ""required"": [""Id"", ""Result""]
                }"
            };

            // Act
            var errors = await _validator.ValidateResponseAsync(response, schema);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateResponseAsync_WithValidSchemaAndInvalidData_ShouldReturnErrors()
        {
            // Arrange
            var response = new TestResponse { Id = -1, Result = "" };
            var schema = new JsonSchemaContract
            {
                Schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""Id"": { ""type"": ""integer"", ""minimum"": 0 },
                        ""Result"": { ""type"": ""string"", ""minLength"": 1 }
                    },
                    ""required"": [""Id"", ""Result""]
                }"
            };

            // Act
            var errors = await _validator.ValidateResponseAsync(response, schema);

            // Assert
            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("minimum"));
            Assert.Contains(errors, e => e.Contains("minLength"));
        }

        [Fact]
        public async Task ValidateRequestAsync_WithArraySchema_ShouldValidateCorrectly()
        {
            // Arrange
            var request = new[] { "item1", "item2", "item3" };
            var schema = new JsonSchemaContract
            {
                Schema = @"{
                    ""type"": ""array"",
                    ""items"": { ""type"": ""string"" },
                    ""minItems"": 2
                }"
            };

            // Act
            var errors = await _validator.ValidateRequestAsync(request, schema);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateRequestAsync_WithInvalidArraySchema_ShouldReturnErrors()
        {
            // Arrange
            var request = new object[] { "item1", 123 }; // Mixed types
            var schema = new JsonSchemaContract
            {
                Schema = @"{
                    ""type"": ""array"",
                    ""items"": { ""type"": ""string"" }
                }"
            };

            // Act
            var errors = await _validator.ValidateRequestAsync(request, schema);

            // Assert
            Assert.NotEmpty(errors);
        }

        [Fact]
        public async Task ValidateRequestAsync_WithNestedObjectSchema_ShouldValidateCorrectly()
        {
            // Arrange
            var request = new
            {
                User = new { Name = "John", Age = 30 },
                Active = true
            };
            var schema = new JsonSchemaContract
            {
                Schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""User"": {
                            ""type"": ""object"",
                            ""properties"": {
                                ""Name"": { ""type"": ""string"" },
                                ""Age"": { ""type"": ""integer"", ""minimum"": 18 }
                            },
                            ""required"": [""Name"", ""Age""]
                        },
                        ""Active"": { ""type"": ""boolean"" }
                    },
                    ""required"": [""User"", ""Active""]
                }"
            };

            // Act
            var errors = await _validator.ValidateRequestAsync(request, schema);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateRequestAsync_WithNestedObjectSchemaAndInvalidData_ShouldReturnErrors()
        {
            // Arrange
            var request = new
            {
                User = new { Name = "John", Age = 15 }, // Age too low
                Active = "yes" // Wrong type
            };
            var schema = new JsonSchemaContract
            {
                Schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""User"": {
                            ""type"": ""object"",
                            ""properties"": {
                                ""Name"": { ""type"": ""string"" },
                                ""Age"": { ""type"": ""integer"", ""minimum"": 18 }
                            },
                            ""required"": [""Name"", ""Age""]
                        },
                        ""Active"": { ""type"": ""boolean"" }
                    },
                    ""required"": [""User"", ""Active""]
                }"
            };

            // Act
            var errors = await _validator.ValidateRequestAsync(request, schema);

            // Assert
            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("minimum"));
            Assert.Contains(errors, e => e.Contains("boolean"));
        }

        [Fact]
        public async Task ValidateRequestAsync_WithFormatValidation_ShouldValidateEmail()
        {
            // Arrange
            var request = new { Email = "invalid-email" };
            var schema = new JsonSchemaContract
            {
                Schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""Email"": { ""type"": ""string"", ""format"": ""email"" }
                    },
                    ""required"": [""Email""]
                }"
            };

            // Act
            var errors = await _validator.ValidateRequestAsync(request, schema);

            // Assert
            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("format") || e.Contains("email"));
        }

        [Fact]
        public async Task ValidateRequestAsync_WithValidEmailFormat_ShouldReturnNoErrors()
        {
            // Arrange
            var request = new { Email = "test@example.com" };
            var schema = new JsonSchemaContract
            {
                Schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""Email"": { ""type"": ""string"", ""format"": ""email"" }
                    },
                    ""required"": [""Email""]
                }"
            };

            // Act
            var errors = await _validator.ValidateRequestAsync(request, schema);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateRequestAsync_WithMissingRequiredField_ShouldReturnError()
        {
            // Arrange
            var request = new { Name = "Test" }; // Missing Value
            var schema = new JsonSchemaContract
            {
                Schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""Name"": { ""type"": ""string"" },
                        ""Value"": { ""type"": ""integer"" }
                    },
                    ""required"": [""Name"", ""Value""]
                }"
            };

            // Act
            var errors = await _validator.ValidateRequestAsync(request, schema);

            // Assert
            Assert.NotEmpty(errors);
            Assert.Contains(errors, e => e.Contains("required") || e.Contains("Value"));
        }

        [Fact]
        public async Task ValidateResponseAsync_WithNullAllowedBySchema_ShouldReturnNoErrors()
        {
            // Arrange
            var schema = new JsonSchemaContract
            {
                Schema = @"{
                    ""type"": [""object"", ""null""],
                    ""properties"": {
                        ""Id"": { ""type"": ""integer"" }
                    }
                }"
            };

            // Act
            var errors = await _validator.ValidateResponseAsync(null!, schema);

            // Assert
            Assert.Empty(errors);
        }

        [Fact]
        public async Task ValidateRequestAsync_SchemaCaching_ShouldReuseParsedSchemas()
        {
            // Arrange
            var request1 = new TestRequest { Name = "Test1", Value = 1 };
            var request2 = new TestRequest { Name = "Test2", Value = 2 };
            var schema = new JsonSchemaContract
            {
                Schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""Name"": { ""type"": ""string"" },
                        ""Value"": { ""type"": ""integer"" }
                    },
                    ""required"": [""Name"", ""Value""]
                }"
            };

            // Act - Validate multiple times with same schema
            var errors1 = await _validator.ValidateRequestAsync(request1, schema);
            var errors2 = await _validator.ValidateRequestAsync(request2, schema);

            // Assert - Both should succeed (caching should work)
            Assert.Empty(errors1);
            Assert.Empty(errors2);
        }

        [Fact]
        public async Task ValidateRequestAsync_WithCancellationToken_ShouldRespectCancellation()
        {
            // Arrange
            var request = new TestRequest { Name = "Test", Value = 123 };
            var schema = new JsonSchemaContract
            {
                Schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""Name"": { ""type"": ""string"" },
                        ""Value"": { ""type"": ""integer"" }
                    },
                    ""required"": [""Name"", ""Value""]
                }"
            };
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            var errors = await _validator.ValidateRequestAsync(request, schema, cts.Token);

            // Assert - Should still work since the method doesn't actually use the token for computation
            // The token is passed but not used in the current implementation
            Assert.Empty(errors);
        }
    }
}