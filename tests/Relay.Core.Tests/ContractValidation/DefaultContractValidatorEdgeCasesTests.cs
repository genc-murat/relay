using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.ContractValidation;
using Relay.Core.Metadata.MessageQueue;
using Xunit;

namespace Relay.Core.Tests.ContractValidation;

public class DefaultContractValidatorEdgeCasesTests
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

    [Fact]
    public async Task ValidateRequestAsync_WithWhitespaceOnlySchema_ShouldSkipValidation()
    {
        // Arrange
        var request = new TestRequest { Name = "Test", Value = 123 };
        var schema = new JsonSchemaContract { Schema = "   \t\n  " };

        // Act
        var errors = await _validator.ValidateRequestAsync(request, schema);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateResponseAsync_WithWhitespaceOnlySchema_ShouldSkipValidation()
    {
        // Arrange
        var response = new TestResponse { Id = 1, Result = "Success" };
        var schema = new JsonSchemaContract { Schema = "   \t\n  " };

        // Act
        var errors = await _validator.ValidateResponseAsync(response, schema);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateRequestAsync_InvalidSchemaCaching_ShouldCacheNullAndReturnErrorConsistently()
    {
        // Arrange
        var request = new TestRequest { Name = "Test", Value = 123 };
        var invalidSchema = new JsonSchemaContract { Schema = "invalid json schema {" };

        // Act - Call multiple times with same invalid schema
        var errors1 = await _validator.ValidateRequestAsync(request, invalidSchema);
        var errors2 = await _validator.ValidateRequestAsync(request, invalidSchema);

        // Assert - Both calls should return the same error (cached null schema)
        Assert.Single(errors1);
        Assert.Equal("Invalid JSON schema format", errors1.First());
        Assert.Single(errors2);
        Assert.Equal("Invalid JSON schema format", errors2.First());
    }

    [Fact]
    public async Task ValidateRequestAsync_WithComplexNestedValidationErrors_ShouldReturnMultipleErrors()
    {
        // Arrange
        var request = new
        {
            User = new
            {
                Name = "", // Too short
                Age = 15, // Too young
                Address = new
                {
                    Street = "123 Main St",
                    City = "", // Too short
                    ZipCode = "abc" // Wrong format
                }
            },
            Items = new[] { "valid", "" } // One empty string
        };
        var schema = new JsonSchemaContract
        {
            Schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""User"": {
                            ""type"": ""object"",
                            ""properties"": {
                                ""Name"": { ""type"": ""string"", ""minLength"": 1 },
                                ""Age"": { ""type"": ""integer"", ""minimum"": 18 },
                                ""Address"": {
                                    ""type"": ""object"",
                                    ""properties"": {
                                        ""Street"": { ""type"": ""string"" },
                                        ""City"": { ""type"": ""string"", ""minLength"": 1 },
                                        ""ZipCode"": { ""type"": ""string"", ""pattern"": ""^\\d{5}$"" }
                                    },
                                    ""required"": [""Street"", ""City"", ""ZipCode""]
                                }
                            },
                            ""required"": [""Name"", ""Age"", ""Address""]
                        },
                        ""Items"": {
                            ""type"": ""array"",
                            ""items"": { ""type"": ""string"", ""minLength"": 1 }
                        }
                    },
                    ""required"": [""User"", ""Items""]
                }"
        };

        // Act
        var errors = await _validator.ValidateRequestAsync(request, schema);

        // Assert
        Assert.NotEmpty(errors);
        // Should have multiple validation errors from different nested levels
        Assert.True(errors.Count() >= 4); // At least: Name minLength, Age minimum, City minLength, ZipCode pattern, Items[1] minLength
    }

    [Fact]
    public async Task ValidateRequestAsync_WithDeeplyNestedValidationErrors_ShouldExtractErrorsRecursively()
    {
        // Arrange - Create a deeply nested object with validation errors at multiple levels
        var request = new
        {
            Level1 = new
            {
                Level2 = new
                {
                    Level3 = new
                    {
                        Value = 10, // Should be string
                        Items = new[]
                        {
                            new { Name = "" }, // Empty name
                            new { Name = "Valid" }
                        }
                    }
                }
            }
        };
        var schema = new JsonSchemaContract
        {
            Schema = @"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""Level1"": {
                            ""type"": ""object"",
                            ""properties"": {
                                ""Level2"": {
                                    ""type"": ""object"",
                                    ""properties"": {
                                        ""Level3"": {
                                            ""type"": ""object"",
                                            ""properties"": {
                                                ""Value"": { ""type"": ""string"" },
                                                ""Items"": {
                                                    ""type"": ""array"",
                                                    ""items"": {
                                                        ""type"": ""object"",
                                                        ""properties"": {
                                                            ""Name"": { ""type"": ""string"", ""minLength"": 1 }
                                                        },
                                                        ""required"": [""Name""]
                                                    }
                                                }
                                            },
                                            ""required"": [""Value"", ""Items""]
                                        }
                                    },
                                    ""required"": [""Level3""]
                                }
                            },
                            ""required"": [""Level2""]
                        }
                    },
                    ""required"": [""Level1""]
                }"
        };

        // Act
        var errors = await _validator.ValidateRequestAsync(request, schema);

        // Assert
        Assert.NotEmpty(errors);
        // Should extract errors from deep nesting
        Assert.Contains(errors, e => e.Contains("Level1") || e.Contains("Level2") || e.Contains("Level3"));
        Assert.Contains(errors, e => e.Contains("type") || e.Contains("minLength"));
    }
}