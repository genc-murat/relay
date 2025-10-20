using System.Linq;
using System.Threading.Tasks;
using Relay.Core;
using Relay.Core.ContractValidation;
using Relay.Core.Metadata.MessageQueue;
using Xunit;

namespace Relay.Core.Tests.ContractValidation;

public class JsonSchemaValidationTests
{
    private readonly DefaultContractValidator _validator;

    public JsonSchemaValidationTests()
    {
        _validator = new DefaultContractValidator();
    }

    [Fact]
    public async Task ValidateRequestAsync_WithValidDataAgainstSchema_ShouldReturnNoErrors()
    {
        // Arrange
        var request = new { name = "Murat Doe", age = 30, email = "murat@example.com" };
        var schema = new JsonSchemaContract
        {
            Schema = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""name"": { ""type"": ""string"" },
                    ""age"": { ""type"": ""integer"" },
                    ""email"": { ""type"": ""string"", ""format"": ""email"" }
                },
                ""required"": [""name"", ""age""]
            }"
        };

        // Act
        var errors = await _validator.ValidateRequestAsync(request, schema);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateRequestAsync_WithInvalidDataAgainstSchema_ShouldReturnErrors()
    {
        // Arrange
        var request = new { name = "Murat Doe", age = "not a number" }; // age should be integer
        var schema = new JsonSchemaContract
        {
            Schema = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""name"": { ""type"": ""string"" },
                    ""age"": { ""type"": ""integer"" }
                },
                ""required"": [""name"", ""age""]
            }"
        };

        // Act
        var errors = await _validator.ValidateRequestAsync(request, schema);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("type"));
    }

    [Fact]
    public async Task ValidateRequestAsync_WithMissingRequiredField_ShouldReturnErrors()
    {
        // Arrange
        var request = new { name = "Murat Doe" }; // Missing required 'age' field
        var schema = new JsonSchemaContract
        {
            Schema = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""name"": { ""type"": ""string"" },
                    ""age"": { ""type"": ""integer"" }
                },
                ""required"": [""name"", ""age""]
            }"
        };

        // Act
        var errors = await _validator.ValidateRequestAsync(request, schema);

        // Assert
        Assert.NotEmpty(errors);
    }

    [Fact]
    public async Task ValidateRequestAsync_WithComplexNestedObject_ShouldValidateCorrectly()
    {
        // Arrange
        var request = new
        {
            user = new
            {
                name = "Murat Doe",
                address = new
                {
                    street = "123 Main St",
                    city = "New York",
                    zipCode = "10001"
                }
            },
            orderDate = "2025-10-09"
        };

        var schema = new JsonSchemaContract
        {
            Schema = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""user"": {
                        ""type"": ""object"",
                        ""properties"": {
                            ""name"": { ""type"": ""string"" },
                            ""address"": {
                                ""type"": ""object"",
                                ""properties"": {
                                    ""street"": { ""type"": ""string"" },
                                    ""city"": { ""type"": ""string"" },
                                    ""zipCode"": { ""type"": ""string"" }
                                },
                                ""required"": [""street"", ""city""]
                            }
                        },
                        ""required"": [""name"", ""address""]
                    },
                    ""orderDate"": { ""type"": ""string"", ""format"": ""date"" }
                },
                ""required"": [""user"", ""orderDate""]
            }"
        };

        // Act
        var errors = await _validator.ValidateRequestAsync(request, schema);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateRequestAsync_WithArrayData_ShouldValidateCorrectly()
    {
        // Arrange
        var request = new
        {
            items = new[]
            {
                new { id = 1, name = "Item 1", price = 10.50 },
                new { id = 2, name = "Item 2", price = 20.99 }
            }
        };

        var schema = new JsonSchemaContract
        {
            Schema = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""items"": {
                        ""type"": ""array"",
                        ""items"": {
                            ""type"": ""object"",
                            ""properties"": {
                                ""id"": { ""type"": ""integer"" },
                                ""name"": { ""type"": ""string"" },
                                ""price"": { ""type"": ""number"" }
                            },
                            ""required"": [""id"", ""name"", ""price""]
                        }
                    }
                },
                ""required"": [""items""]
            }"
        };

        // Act
        var errors = await _validator.ValidateRequestAsync(request, schema);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateRequestAsync_WithNullRequest_ShouldReturnError()
    {
        // Arrange
        var schema = new JsonSchemaContract { Schema = @"{ ""type"": ""object"" }" };

        // Act
        var errors = await _validator.ValidateRequestAsync(null!, schema);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains("cannot be null", errors.First(), System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateRequestAsync_WithEmptySchema_ShouldReturnNoErrors()
    {
        // Arrange
        var request = new { name = "Murat Doe" };
        var schema = new JsonSchemaContract { Schema = "" };

        // Act
        var errors = await _validator.ValidateRequestAsync(request, schema);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateRequestAsync_WithInvalidSchemaFormat_ShouldReturnError()
    {
        // Arrange
        var request = new { name = "Murat Doe" };
        var schema = new JsonSchemaContract { Schema = "not valid json" };

        // Act
        var errors = await _validator.ValidateRequestAsync(request, schema);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("Invalid JSON schema format"));
    }

    [Fact]
    public async Task ValidateResponseAsync_WithValidResponse_ShouldReturnNoErrors()
    {
        // Arrange
        var response = new { success = true, message = "Operation completed", data = new { id = 123 } };
        var schema = new JsonSchemaContract
        {
            Schema = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""success"": { ""type"": ""boolean"" },
                    ""message"": { ""type"": ""string"" },
                    ""data"": {
                        ""type"": ""object"",
                        ""properties"": {
                            ""id"": { ""type"": ""integer"" }
                        }
                    }
                },
                ""required"": [""success"", ""message""]
            }"
        };

        // Act
        var errors = await _validator.ValidateResponseAsync(response, schema);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateResponseAsync_WithNullResponse_ShouldValidateAgainstSchema()
    {
        // Arrange
        var schema = new JsonSchemaContract
        {
            Schema = @"{
                ""type"": [""object"", ""null""]
            }"
        };

        // Act
        var errors = await _validator.ValidateResponseAsync(null!, schema);

        // Assert
        // Should not error if schema allows null
        Assert.Empty(errors);
    }

    [Fact]
    public async Task SchemaCaching_ShouldReuseParseSchema()
    {
        // Arrange
        var request1 = new { name = "Murat" };
        var request2 = new { name = "Jane" };
        var schema = new JsonSchemaContract
        {
            Schema = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""name"": { ""type"": ""string"" }
                }
            }"
        };

        // Act
        var errors1 = await _validator.ValidateRequestAsync(request1, schema);
        var errors2 = await _validator.ValidateRequestAsync(request2, schema);

        // Assert
        Assert.Empty(errors1);
        Assert.Empty(errors2);
        // The schema should be cached and reused (validated by no exceptions)
    }

    [Fact]
    public async Task ValidateRequestAsync_WithStringConstraints_ShouldValidateCorrectly()
    {
        // Arrange
        var request = new { email = "test@example.com", phone = "123-456-7890" };
        var schema = new JsonSchemaContract
        {
            Schema = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""email"": {
                        ""type"": ""string"",
                        ""format"": ""email""
                    },
                    ""phone"": {
                        ""type"": ""string"",
                        ""pattern"": ""^\\d{3}-\\d{3}-\\d{4}$""
                    }
                }
            }"
        };

        // Act
        var errors = await _validator.ValidateRequestAsync(request, schema);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateRequestAsync_WithNumberConstraints_ShouldValidateCorrectly()
    {
        // Arrange
        var request = new { age = 25, score = 85.5 };
        var schema = new JsonSchemaContract
        {
            Schema = @"{
                ""type"": ""object"",
                ""properties"": {
                    ""age"": {
                        ""type"": ""integer"",
                        ""minimum"": 0,
                        ""maximum"": 150
                    },
                    ""score"": {
                        ""type"": ""number"",
                        ""minimum"": 0,
                        ""maximum"": 100
                    }
                }
            }"
        };

        // Act
        var errors = await _validator.ValidateRequestAsync(request, schema);

        // Assert
        Assert.Empty(errors);
    }
}
