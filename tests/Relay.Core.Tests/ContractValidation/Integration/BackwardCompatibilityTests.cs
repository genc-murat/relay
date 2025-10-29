using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Relay.Core.Configuration.Options.ContractValidation;
using Relay.Core.ContractValidation;
using Relay.Core.ContractValidation.Caching;
using Relay.Core.ContractValidation.Models;
using Relay.Core.ContractValidation.SchemaDiscovery;
using Relay.Core.ContractValidation.Testing;
using Relay.Core.Metadata.MessageQueue;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.Integration;

/// <summary>
/// Backward compatibility tests to ensure new features don't break existing functionality.
/// Tests that existing code continues to work with default behavior.
/// </summary>
public sealed class BackwardCompatibilityTests : IDisposable
{
    private readonly string _testSchemaDirectory;
    private readonly ContractValidationTestFixture _fixture;

    public BackwardCompatibilityTests()
    {
        _fixture = new ContractValidationTestFixture();
        
        var currentDirectory = Directory.GetCurrentDirectory();
        _testSchemaDirectory = Path.Combine(currentDirectory, "..", "..", "..", "ContractValidation", "TestSchemas");
        _testSchemaDirectory = Path.GetFullPath(_testSchemaDirectory);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    [Fact]
    public async Task BackwardCompatibility_DefaultContractValidator_WithoutNewFeatures_ShouldWorkAsExpected()
    {
        // Arrange - Create validator using old pattern (no schema resolver, no validation engine)
        var validator = new DefaultContractValidator();

        var schema = _fixture.CreateSimpleTestSchema();
        var request = new { Name = "Test", Value = 42 };

        // Act
        var errors = await validator.ValidateRequestAsync(request, schema, CancellationToken.None);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task BackwardCompatibility_DefaultContractValidator_WithInvalidData_ShouldReturnErrors()
    {
        // Arrange
        var validator = new DefaultContractValidator();
        var schema = _fixture.CreateSimpleTestSchema();
        var request = new { Name = "", Value = -1 }; // Invalid

        // Act
        var errors = await validator.ValidateRequestAsync(request, schema, CancellationToken.None);

        // Assert
        Assert.NotEmpty(errors);
    }

    [Fact]
    public async Task BackwardCompatibility_IContractValidator_Interface_RemainsUnchanged()
    {
        // Arrange
        IContractValidator validator = new DefaultContractValidator();
        var schema = _fixture.CreateSimpleTestSchema();
        var request = new { Name = "Test", Value = 42 };
        var response = new { Success = true };

        // Act - Use original interface methods
        var requestErrors = await validator.ValidateRequestAsync(request, schema, CancellationToken.None);
        var responseErrors = await validator.ValidateResponseAsync(response, schema, CancellationToken.None);

        // Assert
        Assert.Empty(requestErrors);
        Assert.Empty(responseErrors);
        
        // Verify return type is IEnumerable<string> as expected
        Assert.IsAssignableFrom<IEnumerable<string>>(requestErrors);
        Assert.IsAssignableFrom<IEnumerable<string>>(responseErrors);
    }

    [Fact]
    public async Task BackwardCompatibility_ExistingCode_WithDefaultOptions_ShouldBehaveIdentically()
    {
        // Arrange - Simulate existing code with default options
        var options = new ContractValidationOptions(); // All defaults
        var validator = _fixture.CreateValidator(opt =>
        {
            // No configuration - use defaults
        });

        var schema = _fixture.CreateSimpleTestSchema();
        var request = new { Name = "Test", Value = 42 };

        // Act
        var errors = await validator.ValidateRequestAsync(request, schema, CancellationToken.None);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task BackwardCompatibility_SchemaCache_WithDefaultBehavior_ShouldWork()
    {
        // Arrange - Create validator with schema cache (new feature) but default behavior
        var cacheOptions = Options.Create(new SchemaCacheOptions());
        var schemaCache = new LruSchemaCache(cacheOptions);
        var validator = new DefaultContractValidator(schemaCache: schemaCache);

        var schema = _fixture.CreateSimpleTestSchema();
        var request = new { Name = "Test", Value = 42 };

        // Act
        var errors = await validator.ValidateRequestAsync(request, schema, CancellationToken.None);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task BackwardCompatibility_NewFeatures_WhenDisabled_ShouldNotAffectBehavior()
    {
        // Arrange - Configure with all new features disabled
        var options = new ContractValidationOptions
        {
            EnableAutomaticContractValidation = false,
            EnableDetailedErrors = false,
            EnableSuggestedFixes = false,
            EnablePerformanceMetrics = false
        };

        var validator = _fixture.CreateValidator(opt =>
        {
            opt.EnableAutomaticContractValidation = false;
            opt.EnableDetailedErrors = false;
            opt.EnableSuggestedFixes = false;
            opt.EnablePerformanceMetrics = false;
        });

        var schema = _fixture.CreateSimpleTestSchema();
        var request = new { Name = "Test", Value = 42 };

        // Act
        var errors = await validator.ValidateRequestAsync(request, schema, CancellationToken.None);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task BackwardCompatibility_ErrorFormat_RemainsConsistent()
    {
        // Arrange
        var validator = new DefaultContractValidator();
        var schema = _fixture.CreateConstrainedTestSchema();
        var request = new { Name = "", Value = 2000, Email = "invalid" }; // Multiple errors

        // Act
        var errors = await validator.ValidateRequestAsync(request, schema, CancellationToken.None);

        // Assert
        Assert.NotEmpty(errors);
        
        // Verify errors are still strings (backward compatible format)
        Assert.All(errors, error => Assert.IsType<string>(error));
        
        // Verify errors contain expected information
        Assert.Contains(errors, e => e.Contains("Name") || e.Contains("minLength"));
        Assert.Contains(errors, e => e.Contains("Value") || e.Contains("maximum"));
    }

    [Fact]
    public async Task BackwardCompatibility_NullSchema_ShouldHandleGracefully()
    {
        // Arrange
        var validator = new DefaultContractValidator();
        var request = new { Name = "Test", Value = 42 };

        // Act & Assert - Should not throw, should return empty errors or handle gracefully
        var errors = await validator.ValidateRequestAsync(request, null!, CancellationToken.None);
        
        // Behavior: returns empty errors when schema is null (no validation performed)
        Assert.Empty(errors);
    }

    [Fact]
    public async Task BackwardCompatibility_EmptySchema_ShouldHandleGracefully()
    {
        // Arrange
        var validator = new DefaultContractValidator();
        var schema = new JsonSchemaContract { Schema = "{}" };
        var request = new { Name = "Test", Value = 42 };

        // Act
        var errors = await validator.ValidateRequestAsync(request, schema, CancellationToken.None);

        // Assert - Empty schema should pass validation
        Assert.Empty(errors);
    }

    [Fact]
    public async Task BackwardCompatibility_CancellationToken_ShouldBeRespected()
    {
        // Arrange
        var validator = new DefaultContractValidator();
        var schema = _fixture.CreateSimpleTestSchema();
        var request = new { Name = "Test", Value = 42 };
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await validator.ValidateRequestAsync(request, schema, cts.Token));
    }

    [Fact]
    public async Task BackwardCompatibility_MultipleValidations_ShouldProduceSameResults()
    {
        // Arrange
        var validator = new DefaultContractValidator();
        var schema = _fixture.CreateSimpleTestSchema();
        var request = new { Name = "Test", Value = 42 };

        // Act - Validate multiple times
        var errors1 = await validator.ValidateRequestAsync(request, schema, CancellationToken.None);
        var errors2 = await validator.ValidateRequestAsync(request, schema, CancellationToken.None);
        var errors3 = await validator.ValidateRequestAsync(request, schema, CancellationToken.None);

        // Assert - Results should be consistent
        Assert.Empty(errors1);
        Assert.Empty(errors2);
        Assert.Empty(errors3);
    }

    [Fact]
    public async Task BackwardCompatibility_DifferentDataTypes_ShouldValidateCorrectly()
    {
        // Arrange
        var validator = new DefaultContractValidator();
        
        // Test with different data types
        var stringSchema = _fixture.CreateTestSchema(@"{""type"": ""string""}");
        var numberSchema = _fixture.CreateTestSchema(@"{""type"": ""number""}");
        var booleanSchema = _fixture.CreateTestSchema(@"{""type"": ""boolean""}");
        var arraySchema = _fixture.CreateTestSchema(@"{""type"": ""array""}");

        // Act
        var stringErrors = await validator.ValidateRequestAsync("test", stringSchema, CancellationToken.None);
        var numberErrors = await validator.ValidateRequestAsync(42, numberSchema, CancellationToken.None);
        var booleanErrors = await validator.ValidateRequestAsync(true, booleanSchema, CancellationToken.None);
        var arrayErrors = await validator.ValidateRequestAsync(new[] { 1, 2, 3 }, arraySchema, CancellationToken.None);

        // Assert
        Assert.Empty(stringErrors);
        Assert.Empty(numberErrors);
        Assert.Empty(booleanErrors);
        Assert.Empty(arrayErrors);
    }

    [Fact]
    public async Task BackwardCompatibility_ComplexNestedObjects_ShouldValidate()
    {
        // Arrange
        var validator = new DefaultContractValidator();
        var schema = _fixture.CreateTestSchema(@"{
            ""type"": ""object"",
            ""properties"": {
                ""Id"": { ""type"": ""integer"" },
                ""Name"": { ""type"": ""string"" },
                ""Address"": {
                    ""type"": ""object"",
                    ""properties"": {
                        ""Street"": { ""type"": ""string"" },
                        ""City"": { ""type"": ""string"" }
                    }
                },
                ""Tags"": {
                    ""type"": ""array"",
                    ""items"": { ""type"": ""string"" }
                }
            }
        }");

        var request = new
        {
            Id = 1,
            Name = "Test",
            Address = new
            {
                Street = "123 Main St",
                City = "Test City"
            },
            Tags = new[] { "tag1", "tag2" }
        };

        // Act
        var errors = await validator.ValidateRequestAsync(request, schema, CancellationToken.None);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public async Task BackwardCompatibility_ExistingTestCode_ShouldContinueToWork()
    {
        // Arrange - Simulate existing test code pattern
        var validator = new DefaultContractValidator();
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

        var validRequest = new { Name = "Test", Value = 42 };
        var invalidRequest = new { Name = "", Value = "not a number" };

        // Act
        var validErrors = await validator.ValidateRequestAsync(validRequest, schema, CancellationToken.None);
        var invalidErrors = await validator.ValidateRequestAsync(invalidRequest, schema, CancellationToken.None);

        // Assert
        Assert.Empty(validErrors);
        Assert.NotEmpty(invalidErrors);
    }

    [Fact]
    public void BackwardCompatibility_ContractValidationOptions_DefaultValues_ShouldRemainUnchanged()
    {
        // Arrange & Act
        var options = new ContractValidationOptions();

        // Assert - Verify default values haven't changed
        Assert.False(options.EnableAutomaticContractValidation);
        Assert.True(options.ValidateRequests);
        Assert.True(options.ValidateResponses);
        Assert.True(options.ThrowOnValidationFailure);
        Assert.Equal(-750, options.DefaultOrder);
    }

    [Fact]
    public void BackwardCompatibility_JsonSchemaContract_Structure_RemainsUnchanged()
    {
        // Arrange & Act
        var contract = new JsonSchemaContract
        {
            Schema = "{}",
            SchemaVersion = "draft-07",
            ContentType = "application/json"
        };

        // Assert - Verify properties are accessible
        Assert.NotNull(contract.Schema);
        Assert.NotNull(contract.SchemaVersion);
        Assert.NotNull(contract.ContentType);
    }

    [Fact]
    public async Task BackwardCompatibility_ValidationWithNullValues_ShouldHandleCorrectly()
    {
        // Arrange
        var validator = new DefaultContractValidator();
        var schema = _fixture.CreateTestSchema(@"{
            ""type"": ""object"",
            ""properties"": {
                ""Name"": { ""type"": [""string"", ""null""] },
                ""Value"": { ""type"": [""integer"", ""null""] }
            }
        }");

        var request = new { Name = (string?)null, Value = (int?)null };

        // Act
        var errors = await validator.ValidateRequestAsync(request, schema, CancellationToken.None);

        // Assert
        Assert.Empty(errors);
    }
}
