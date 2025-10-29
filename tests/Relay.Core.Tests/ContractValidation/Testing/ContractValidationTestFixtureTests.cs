using System;
using System.Linq;
using System.Threading.Tasks;
using Relay.Core.ContractValidation;
using Relay.Core.ContractValidation.Models;
using Relay.Core.ContractValidation.Testing;
using Xunit;

namespace Relay.Core.Tests.ContractValidation.Testing;

/// <summary>
/// Tests for ContractValidationTestFixture.
/// </summary>
public class ContractValidationTestFixtureTests
{
    private readonly ContractValidationTestFixture _fixture = new();

    public class TestRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    [Fact]
    public void CreateValidator_WithDefaultConfiguration_ShouldReturnValidator()
    {
        // Act
        var validator = _fixture.CreateValidator();

        // Assert
        Assert.NotNull(validator);
    }

    [Fact]
    public void CreateValidator_WithCustomConfiguration_ShouldApplyConfiguration()
    {
        // Act
        var validator = _fixture.CreateValidator(options =>
        {
            options.ValidateRequests = true;
            options.ValidateResponses = false;
        });

        // Assert
        Assert.NotNull(validator);
    }

    [Fact]
    public void CreateValidatorWithComponents_WithCustomComponents_ShouldReturnValidator()
    {
        // Arrange
        var cache = _fixture.CreateSchemaCache();
        var engine = _fixture.CreateValidationEngine();

        // Act
        var validator = _fixture.CreateValidatorWithComponents(
            schemaCache: cache,
            validationEngine: engine);

        // Assert
        Assert.NotNull(validator);
    }

    [Fact]
    public void CreateTestSchema_WithValidJson_ShouldReturnSchema()
    {
        // Arrange
        var schemaJson = @"{ ""type"": ""object"" }";

        // Act
        var schema = _fixture.CreateTestSchema(schemaJson);

        // Assert
        Assert.NotNull(schema);
        Assert.Equal(schemaJson, schema.Schema);
    }

    [Fact]
    public void CreateSimpleTestSchema_ShouldReturnValidSchema()
    {
        // Act
        var schema = _fixture.CreateSimpleTestSchema();

        // Assert
        Assert.NotNull(schema);
        Assert.NotEmpty(schema.Schema);
        Assert.Contains("Name", schema.Schema);
        Assert.Contains("Value", schema.Schema);
    }

    [Fact]
    public void CreateConstrainedTestSchema_ShouldReturnSchemaWithConstraints()
    {
        // Act
        var schema = _fixture.CreateConstrainedTestSchema();

        // Assert
        Assert.NotNull(schema);
        Assert.NotEmpty(schema.Schema);
        Assert.Contains("minLength", schema.Schema);
        Assert.Contains("maximum", schema.Schema);
    }

    [Fact]
    public void CreateValidationError_WithBasicParameters_ShouldReturnError()
    {
        // Act
        var error = _fixture.CreateValidationError("CV001", "Test error", "$.Name");

        // Assert
        Assert.NotNull(error);
        Assert.Equal("CV001", error.ErrorCode);
        Assert.Equal("Test error", error.Message);
        Assert.Equal("$.Name", error.JsonPath);
    }

    [Fact]
    public void CreateValidationError_WithAllParameters_ShouldReturnDetailedError()
    {
        // Act
        var error = _fixture.CreateValidationError(
            "CV001",
            "Test error",
            "$.Name",
            "expected",
            "actual",
            ValidationSeverity.Warning);

        // Assert
        Assert.NotNull(error);
        Assert.Equal("CV001", error.ErrorCode);
        Assert.Equal("Test error", error.Message);
        Assert.Equal("$.Name", error.JsonPath);
        Assert.Equal("expected", error.ExpectedValue);
        Assert.Equal("actual", error.ActualValue);
        Assert.Equal(ValidationSeverity.Warning, error.Severity);
    }

    [Fact]
    public void CreateRequestContext_ShouldReturnValidContext()
    {
        // Arrange
        var request = new TestRequest { Name = "Test", Value = 123 };
        var schema = _fixture.CreateSimpleTestSchema();

        // Act
        var context = _fixture.CreateRequestContext(typeof(TestRequest), request, schema);

        // Assert
        Assert.NotNull(context);
        Assert.Equal(typeof(TestRequest), context.ObjectType);
        Assert.True(context.IsRequest);
        Assert.Same(request, context.ObjectInstance);
        Assert.Same(schema, context.Schema);
    }

    [Fact]
    public void CreateResponseContext_ShouldReturnValidContext()
    {
        // Arrange
        var response = new TestRequest { Name = "Test", Value = 123 };
        var schema = _fixture.CreateSimpleTestSchema();

        // Act
        var context = _fixture.CreateResponseContext(typeof(TestRequest), response, schema);

        // Assert
        Assert.NotNull(context);
        Assert.Equal(typeof(TestRequest), context.ObjectType);
        Assert.False(context.IsRequest);
        Assert.Same(response, context.ObjectInstance);
        Assert.Same(schema, context.Schema);
    }

    [Fact]
    public void CreateSchemaCache_WithDefaultSize_ShouldReturnCache()
    {
        // Act
        var cache = _fixture.CreateSchemaCache();

        // Assert
        Assert.NotNull(cache);
        var metrics = cache.GetMetrics();
        Assert.Equal(0, metrics.CurrentSize);
    }

    [Fact]
    public void CreateSchemaCache_WithCustomSize_ShouldReturnCache()
    {
        // Act
        var cache = _fixture.CreateSchemaCache(maxCacheSize: 50);

        // Assert
        Assert.NotNull(cache);
        var metrics = cache.GetMetrics();
        Assert.Equal(50, metrics.MaxSize);
    }

    [Fact]
    public void CreateValidationEngine_WithoutValidators_ShouldReturnEngine()
    {
        // Act
        var engine = _fixture.CreateValidationEngine();

        // Assert
        Assert.NotNull(engine);
        Assert.False(engine.HasCustomValidators);
    }

    [Fact]
    public void CreateValidationEngine_WithValidators_ShouldReturnEngineWithValidators()
    {
        // Arrange
        var validator = _fixture.CreateTestValidator(
            appliesTo: type => type == typeof(TestRequest),
            validateFunc: (obj, ctx) => Enumerable.Empty<ValidationError>());

        // Act
        var engine = _fixture.CreateValidationEngine(validator);

        // Assert
        Assert.NotNull(engine);
        Assert.True(engine.HasCustomValidators);
    }

    [Fact]
    public async Task CreateTestValidator_ShouldExecuteValidationLogic()
    {
        // Arrange
        var executed = false;
        var validator = _fixture.CreateTestValidator(
            appliesTo: type => type == typeof(TestRequest),
            validateFunc: (obj, ctx) =>
            {
                executed = true;
                return Enumerable.Empty<ValidationError>();
            });

        var request = new TestRequest();
        var context = _fixture.CreateRequestContext(typeof(TestRequest), request);

        // Act
        await validator.ValidateAsync(request, context);

        // Assert
        Assert.True(executed);
    }

    [Fact]
    public async Task CreateTestValidator_WithErrors_ShouldReturnErrors()
    {
        // Arrange
        var error = _fixture.CreateValidationError("TEST001", "Test error");
        var validator = _fixture.CreateTestValidator(
            appliesTo: type => type == typeof(TestRequest),
            validateFunc: (obj, ctx) => new[] { error });

        var request = new TestRequest();
        var context = _fixture.CreateRequestContext(typeof(TestRequest), request);

        // Act
        var errors = await validator.ValidateAsync(request, context);

        // Assert
        Assert.Single(errors);
        Assert.Equal("TEST001", errors.First().ErrorCode);
    }

    [Fact]
    public void CreateSampleTestData_ShouldReturnTestData()
    {
        // Act
        var testData = _fixture.CreateSampleTestData();

        // Assert
        Assert.NotNull(testData);
        Assert.NotEmpty(testData);
        Assert.True(testData.ContainsKey("ValidObject"));
        Assert.True(testData.ContainsKey("InvalidObject"));
        Assert.True(testData.ContainsKey("ComplexObject"));
    }

    [Fact]
    public async Task IntegrationTest_ValidatorWithFixture_ShouldValidateSuccessfully()
    {
        // Arrange
        var validator = _fixture.CreateValidator() as DefaultContractValidator;
        var schema = _fixture.CreateSimpleTestSchema();
        var request = new TestRequest { Name = "Test", Value = 123 };
        var context = _fixture.CreateRequestContext(typeof(TestRequest), request, schema);

        // Act
        var result = await validator!.ValidateRequestDetailedAsync(request, schema, context);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
