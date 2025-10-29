# Contract Validation Testing Utilities

This directory contains testing utilities and fixtures for contract validation testing.

## Overview

The testing utilities provide a comprehensive set of tools for testing contract validation functionality, including:

- **ContractValidationTestFixture**: Factory methods for creating validators, schemas, and test data
- **MockContractValidator**: Test double for IContractValidator with configurable behavior
- **ValidationAssertions**: Fluent assertion helpers for validation results
- **TestSchemaProvider**: In-memory schema provider for isolated testing

## Components

### ContractValidationTestFixture

A test fixture that provides factory methods for creating validation components and test data.

```csharp
var fixture = new ContractValidationTestFixture();

// Create a validator with default configuration
var validator = fixture.CreateValidator();

// Create a validator with custom configuration
var validator = fixture.CreateValidator(options =>
{
    options.ValidateRequests = true;
    options.ValidateResponses = true;
});

// Create test schemas
var simpleSchema = fixture.CreateSimpleTestSchema();
var constrainedSchema = fixture.CreateConstrainedTestSchema();
var customSchema = fixture.CreateTestSchema(@"{ ""type"": ""object"" }");

// Create validation errors
var error = fixture.CreateValidationError("CV001", "Test error", "$.Name");

// Create validation contexts
var requestContext = fixture.CreateRequestContext(typeof(MyRequest), request);
var responseContext = fixture.CreateResponseContext(typeof(MyResponse), response);

// Create test data
var testData = fixture.CreateSampleTestData();
```

### MockContractValidator

A mock implementation of IContractValidator for testing purposes.

```csharp
var mock = new MockContractValidator();

// Configure mock behavior
mock.SetupRequestSuccess();
mock.SetupResponseFailure("CV001", "Validation failed");

// Use in tests
await mock.ValidateRequestAsync(request, schema);

// Verify calls
Assert.True(mock.VerifyRequestValidated(request));
Assert.Equal(1, mock.RequestValidationCallCount);

// Get call details
var lastCall = mock.GetLastCall();
Assert.NotNull(lastCall);
Assert.True(lastCall.IsRequest);

// Reset mock
mock.Reset();
```

### ValidationAssertions

Fluent assertion helpers for validation results and errors.

```csharp
var result = await validator.ValidateRequestDetailedAsync(request, schema, context);

// Assert validation results
result.ShouldBeValid();
result.ShouldBeInvalid();
result.ShouldHaveErrorCount(2);
result.ShouldHaveError("CV001");
result.ShouldHaveErrorAtPath("$.Name");
result.ShouldHaveErrorMessage("required");
result.ShouldCompleteWithin(TimeSpan.FromMilliseconds(100));
result.ShouldHaveValidatorName("DefaultContractValidator");

// Assert validation errors
var error = result.Errors.First();
error.ShouldHaveSeverity(ValidationSeverity.Error);
error.ShouldSuggestFix("Provide a value");
error.ShouldHaveExpectedAndActualValues();
```

### TestSchemaProvider

An in-memory schema provider for isolated testing without file system dependencies.

```csharp
var provider = new TestSchemaProvider();

// Register schemas by type
provider.RegisterSchema(typeof(MyRequest), schemaJson);

// Register schemas by name
provider.RegisterSchemaByName("MyRequest", schemaJson);

// Use with schema resolver
var resolver = new DefaultSchemaResolver(new[] { provider });

// Create provider with sample schemas
var provider = TestSchemaProvider.CreateWithSampleSchemas();
```

## Usage Examples

### Basic Validation Testing

```csharp
[Fact]
public async Task ValidateRequest_WithValidData_ShouldSucceed()
{
    // Arrange
    var fixture = new ContractValidationTestFixture();
    var validator = fixture.CreateValidator();
    var schema = fixture.CreateSimpleTestSchema();
    var request = new { Name = "Test", Value = 123 };
    var context = fixture.CreateRequestContext(request.GetType(), request, schema);

    // Act
    var result = await validator.ValidateRequestDetailedAsync(request, schema, context);

    // Assert
    result.ShouldBeValid();
}
```

### Testing with Mock Validator

```csharp
[Fact]
public async Task Pipeline_WithValidationFailure_ShouldThrowException()
{
    // Arrange
    var mock = new MockContractValidator();
    mock.SetupRequestFailure("CV001", "Validation failed");
    
    var pipeline = new ContractValidationPipelineBehavior(mock, options);

    // Act & Assert
    await Assert.ThrowsAsync<ContractValidationException>(
        () => pipeline.Handle(request, next, cancellationToken));
    
    Assert.True(mock.VerifyRequestValidated(request));
}
```

### Testing with Custom Validators

```csharp
[Fact]
public async Task ValidateRequest_WithCustomValidator_ShouldExecuteCustomValidation()
{
    // Arrange
    var fixture = new ContractValidationTestFixture();
    var customValidator = fixture.CreateTestValidator(
        appliesTo: type => type == typeof(MyRequest),
        validateFunc: (obj, ctx) => new[]
        {
            fixture.CreateValidationError("CUSTOM001", "Custom validation failed")
        });
    
    var engine = fixture.CreateValidationEngine(customValidator);
    var validator = fixture.CreateValidator(validationEngine: engine);
    
    // Act
    var result = await validator.ValidateRequestDetailedAsync(request, schema, context);

    // Assert
    result.ShouldBeInvalid();
    result.ShouldHaveError("CUSTOM001");
}
```

### Integration Testing with Test Schemas

```csharp
[Fact]
public async Task ValidateRequest_WithFileSystemSchemas_ShouldLoadFromTestDirectory()
{
    // Arrange
    var options = new SchemaDiscoveryOptions
    {
        SchemaDirectories = new List<string> { "TestSchemas" }
    };
    
    var provider = new FileSystemSchemaProvider(Options.Create(options));
    var resolver = new DefaultSchemaResolver(new[] { provider });
    var validator = new DefaultContractValidator(schemaResolver: resolver);
    
    // Act
    var schema = await resolver.ResolveSchemaAsync(
        typeof(SimpleRequest),
        new SchemaContext { RequestType = typeof(SimpleRequest), IsRequest = true });

    // Assert
    Assert.NotNull(schema);
    Assert.NotEmpty(schema.Schema);
}
```

## Test Schema Files

Sample schema files are provided in `tests/Relay.Core.Tests/ContractValidation/TestSchemas/`:

- **SimpleRequest.schema.json**: Basic request schema with string and integer properties
- **UserRequest.schema.json**: User request schema with email validation and patterns
- **OrderRequest.schema.json**: Complex nested schema with arrays and objects
- **ProductResponse.schema.json**: Response schema with enums and date-time formats
- **ValidationErrorResponse.schema.json**: Schema for validation error responses

## Best Practices

1. **Use ContractValidationTestFixture** for creating test components to ensure consistency
2. **Use MockContractValidator** for unit testing pipeline behaviors and handlers
3. **Use ValidationAssertions** for clear, readable test assertions
4. **Use TestSchemaProvider** for isolated unit tests without file system dependencies
5. **Use actual schema files** for integration tests to validate real-world scenarios
6. **Reset mocks** between tests to avoid test pollution
7. **Create focused test schemas** that test specific validation scenarios
8. **Use fluent assertions** to make test failures more informative

## Error Handling

All testing utilities include proper error handling and validation:

- Null argument checks with ArgumentNullException
- Validation of required parameters
- Clear exception messages for assertion failures
- ValidationAssertionException for failed assertions

## Thread Safety

- ContractValidationTestFixture is thread-safe for read operations
- MockContractValidator is NOT thread-safe; create separate instances per test
- TestSchemaProvider is NOT thread-safe; create separate instances per test
- ValidationAssertions are static extension methods and thread-safe
