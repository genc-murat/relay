using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
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
            errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateRequestAsync_WithNullRequest_ShouldReturnError()
        {
            // Arrange
            var schema = new JsonSchemaContract();

            // Act
            var errors = await _validator.ValidateRequestAsync(null!, schema);

            // Assert
            errors.Should().ContainSingle();
            errors.First().Should().Be("Request cannot be null");
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
            errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateResponseAsync_WithNullResponseAndNonEmptySchema_ShouldReturnError()
        {
            // Arrange
            var schema = new JsonSchemaContract { Schema = "{\"type\":\"object\"}" };

            // Act
            var errors = await _validator.ValidateResponseAsync(null!, schema);

            // Assert
            errors.Should().ContainSingle();
            errors.First().Should().Be("Response cannot be null");
        }

        [Fact]
        public async Task ValidateResponseAsync_WithNullResponseAndEmptySchema_ShouldReturnNoErrors()
        {
            // Arrange
            var schema = new JsonSchemaContract { Schema = "{}" };

            // Act
            var errors = await _validator.ValidateResponseAsync(null!, schema);

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateRequestAsync_WithSerializationFailure_ShouldReturnError()
        {
            // Arrange
            var request = new UnserializableRequest();
            var schema = new JsonSchemaContract();

            // Act
            var errors = await _validator.ValidateRequestAsync(request, schema);

            // Assert
            errors.Should().NotBeEmpty();
            errors.First().Should().Contain("Request validation failed");
        }

        public class UnserializableRequest
        {
            public UnserializableRequest Self => this;
        }
    }
}
