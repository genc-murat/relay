using System.Linq;
using System.Threading.Tasks;
using Relay.Core.ContractValidation;
using Relay.Core.Metadata.MessageQueue;
using Xunit;

namespace Relay.Core.Tests.ContractValidation
{
    public class DefaultContractValidatorSchemaTests
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
        public async Task ValidateRequestAsync_WithInvalidJsonSchema_ShouldReturnError()
        {
            // Arrange
            var request = new TestRequest { Name = "Test", Value = 123 };
            var schema = new JsonSchemaContract { Schema = "invalid json schema {" };

            // Act
            var errors = await _validator.ValidateRequestAsync(request, schema);

            // Assert
            Assert.Single(errors);
            Assert.Contains("Invalid JSON schema format", errors.First());
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
        public async Task ValidateResponseAsync_WithNullResponseAndSchemaDisallowingNull_ShouldReturnError()
        {
            // Arrange
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
            var errors = await _validator.ValidateResponseAsync(null!, schema);

            // Assert
            Assert.Single(errors);
            Assert.Contains("cannot be null", errors.First());
        }
    }
}