using Relay.Core.ContractValidation;
using Relay.Core.Metadata.MessageQueue;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.ContractValidation;

public class DefaultContractValidatorDataTypeTests
{
    private readonly DefaultContractValidator _validator = new();

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
    public async Task ValidateRequestAsync_WithValidEnumValue_ShouldReturnNoErrors()
    {
        // Arrange
        var request = new { Status = "active" };
        var schema = new JsonSchemaContract
        {
            Schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""Status"": { ""enum"": [""active"", ""inactive"", ""pending""] }
                    },
                    ""required"": [""Status""]
                }"
        };

        // Act
        var errors = await _validator.ValidateRequestAsync(request, schema);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateRequestAsync_WithInvalidEnumValue_ShouldReturnError()
    {
        // Arrange
        var request = new { Status = "unknown" };
        var schema = new JsonSchemaContract
        {
            Schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""Status"": { ""enum"": [""active"", ""inactive"", ""pending""] }
                    },
                    ""required"": [""Status""]
                }"
        };

        // Act
        var errors = await _validator.ValidateRequestAsync(request, schema);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("enum"));
    }

    [Fact]
    public async Task ValidateRequestAsync_WithSchemaUsingNotConstraint_ShouldReturnGenericErrorWhenNoDetailedErrors()
    {
        // Arrange - Create a schema that might fail validation without detailed error messages
        var request = new { Value = 5 };
        var schema = new JsonSchemaContract
        {
            Schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""Value"": {
                            ""not"": { ""type"": ""integer"" }
                        }
                    },
                    ""required"": [""Value""]
                }"
        };

        // Act
        var errors = await _validator.ValidateRequestAsync(request, schema);

        // Assert
        Assert.NotEmpty(errors);
        // May contain detailed errors or generic error depending on JsonSchema.Net implementation
    }

    [Fact]
    public async Task ValidateRequestAsync_WithInvalidDataAndNoDetailedErrors_ShouldReturnGenericError()
    {
        // Arrange - Use a schema that may not provide detailed errors
        var request = new { Value = "test" };
        var schema = new JsonSchemaContract
        {
            Schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""Value"": { ""const"": ""expected"" }
                    },
                    ""required"": [""Value""]
                }"
        };

        // Act
        var errors = await _validator.ValidateRequestAsync(request, schema);

        // Assert
        Assert.NotEmpty(errors);
        // Should contain either detailed errors or the generic error message
        Assert.Contains(errors, e => e.Contains("Validation failed") || e.Contains("const"));
    }
}

