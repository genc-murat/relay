using System.Linq;
using System.Threading.Tasks;
using Relay.Core.ContractValidation;
using Xunit;

namespace Relay.Core.Tests.ContractValidation
{
    public class DefaultContractValidatorBasicTests
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
    }
}