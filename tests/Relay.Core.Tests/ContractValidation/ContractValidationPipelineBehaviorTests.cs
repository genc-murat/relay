using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Configuration.Options.ContractValidation;
using Relay.Core.Configuration.Options.Core;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.ContractValidation;
using Relay.Core.ContractValidation.SchemaDiscovery;
using Relay.Core.ContractValidation.Strategies;
using Relay.Core.Metadata.MessageQueue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.ContractValidation;

public class ContractValidationPipelineBehaviorTests
{
    private readonly Mock<IContractValidator> _mockValidator;
    private readonly Mock<ILogger<ContractValidationPipelineBehavior<TestRequest, TestResponse>>> _mockLogger;
    private readonly Mock<IOptions<RelayOptions>> _mockOptions;
    private readonly Mock<ISchemaResolver> _mockSchemaResolver;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly ValidationStrategyFactory _strategyFactory;
    private readonly ContractValidationPipelineBehavior<TestRequest, TestResponse> _behavior;

    public ContractValidationPipelineBehaviorTests()
    {
        _mockValidator = new Mock<IContractValidator>();
        _mockLogger = new Mock<ILogger<ContractValidationPipelineBehavior<TestRequest, TestResponse>>>();
        _mockOptions = new Mock<IOptions<RelayOptions>>();
        _mockSchemaResolver = new Mock<ISchemaResolver>();
        _mockServiceProvider = new Mock<IServiceProvider>();

        // Setup service provider to return a logger for LenientValidationStrategy
        _mockServiceProvider.Setup(x => x.GetService(typeof(ILogger<LenientValidationStrategy>)))
            .Returns(Mock.Of<ILogger<LenientValidationStrategy>>());

        _strategyFactory = new ValidationStrategyFactory(_mockServiceProvider.Object);

        var relayOptions = new RelayOptions
        {
            DefaultContractValidationOptions = new ContractValidationOptions
            {
                EnableAutomaticContractValidation = true,
                ValidateRequests = true,
                ValidateResponses = true,
                ThrowOnValidationFailure = true,
                ValidationStrategy = "Strict",
                EnablePerformanceMetrics = false // Disable for cleaner test output
            }
        };
        _mockOptions.Setup(x => x.Value).Returns(relayOptions);

        // Setup default schema resolver to return null (no schema found)
        _mockSchemaResolver.Setup(x => x.ResolveSchemaAsync(
            It.IsAny<Type>(),
            It.IsAny<SchemaContext>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((JsonSchemaContract?)null);

        _behavior = new ContractValidationPipelineBehavior<TestRequest, TestResponse>(
            _mockValidator.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            _mockSchemaResolver.Object,
            _strategyFactory);
    }

    [Fact]
    public async Task HandleAsync_ShouldValidateRequestAndResponse_WhenValidationEnabled()
    {
        // Arrange
        var request = new TestRequest { Value = "test" };
        var response = new TestResponse { Result = "result" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        var schema = new JsonSchemaContract
        {
            Schema = "{\"type\": \"object\"}",
            ContentType = "application/json"
        };

        // Setup schema resolver to return schemas
        _mockSchemaResolver.Setup(x => x.ResolveSchemaAsync(
            typeof(TestRequest),
            It.IsAny<SchemaContext>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(schema);

        _mockSchemaResolver.Setup(x => x.ResolveSchemaAsync(
            typeof(TestResponse),
            It.IsAny<SchemaContext>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(schema);

        _mockValidator.Setup(x => x.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        _mockValidator.Setup(x => x.ValidateResponseAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var result = await _behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        _mockValidator.Verify(x => x.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockValidator.Verify(x => x.ValidateResponseAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowContractValidationException_WhenRequestValidationFails()
    {
        // Arrange
        var request = new TestRequest { Value = "test" };
        var errors = new[] { "Invalid request property" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(new TestResponse()));

        var schema = new JsonSchemaContract
        {
            Schema = "{\"type\": \"object\"}",
            ContentType = "application/json"
        };

        // Setup schema resolver to return schema for request
        _mockSchemaResolver.Setup(x => x.ResolveSchemaAsync(
            typeof(TestRequest),
            It.IsAny<SchemaContext>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(schema);

        _mockValidator.Setup(x => x.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(errors);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ContractValidationException>(async () =>
            await _behavior.HandleAsync(request, next, CancellationToken.None));

        Assert.Equal(typeof(TestRequest), exception.ObjectType);
        Assert.Equal(errors, exception.Errors);
        Assert.Contains("Contract validation failed for TestRequest", exception.Message);
        Assert.Contains("Invalid request property", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowContractValidationException_WhenResponseValidationFails()
    {
        // Arrange
        var request = new TestRequest { Value = "test" };
        var response = new TestResponse { Result = "result" };
        var errors = new[] { "Invalid response property" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        var schema = new JsonSchemaContract
        {
            Schema = "{\"type\": \"object\"}",
            ContentType = "application/json"
        };

        // Setup schema resolver to return schemas
        _mockSchemaResolver.Setup(x => x.ResolveSchemaAsync(
            typeof(TestRequest),
            It.IsAny<SchemaContext>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(schema);

        _mockSchemaResolver.Setup(x => x.ResolveSchemaAsync(
            typeof(TestResponse),
            It.IsAny<SchemaContext>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(schema);

        _mockValidator.Setup(x => x.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        _mockValidator.Setup(x => x.ValidateResponseAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(errors);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ContractValidationException>(async () =>
            await _behavior.HandleAsync(request, next, CancellationToken.None));

        Assert.Equal(typeof(TestResponse), exception.ObjectType);
        Assert.Equal(errors, exception.Errors);
        Assert.Contains("Contract validation failed for TestResponse", exception.Message);
        Assert.Contains("Invalid response property", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotValidate_WhenValidationDisabled()
    {
        // Arrange
        var relayOptions = new RelayOptions
        {
            DefaultContractValidationOptions = new ContractValidationOptions
            {
                EnableAutomaticContractValidation = false,
                EnablePerformanceMetrics = false
            }
        };
        _mockOptions.Setup(x => x.Value).Returns(relayOptions);

        var behavior = new ContractValidationPipelineBehavior<TestRequest, TestResponse>(
            _mockValidator.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            _mockSchemaResolver.Object,
            _strategyFactory);

        var request = new TestRequest { Value = "test" };
        var response = new TestResponse { Result = "result" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        _mockValidator.Verify(x => x.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockValidator.Verify(x => x.ValidateResponseAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotThrow_WhenThrowOnValidationFailureIsFalse()
    {
        // Arrange
        var relayOptions = new RelayOptions
        {
            DefaultContractValidationOptions = new ContractValidationOptions
            {
                EnableAutomaticContractValidation = true,
                ValidateRequests = true,
                ThrowOnValidationFailure = false,
                ValidationStrategy = "Lenient", // Use Lenient strategy to not throw
                EnablePerformanceMetrics = false
            }
        };
        _mockOptions.Setup(x => x.Value).Returns(relayOptions);

        var schema = new JsonSchemaContract
        {
            Schema = "{\"type\": \"object\"}",
            ContentType = "application/json"
        };

        // Setup schema resolver to return schema for request
        _mockSchemaResolver.Setup(x => x.ResolveSchemaAsync(
            typeof(TestRequest),
            It.IsAny<SchemaContext>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(schema);

        var behavior = new ContractValidationPipelineBehavior<TestRequest, TestResponse>(
            _mockValidator.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            _mockSchemaResolver.Object,
            _strategyFactory);

        var request = new TestRequest { Value = "test" };
        var response = new TestResponse { Result = "result" };
        var errors = new[] { "Invalid request property" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        _mockValidator.Setup(x => x.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(errors);
        _mockValidator.Setup(x => x.ValidateResponseAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        // The key assertion is that no exception was thrown despite validation errors
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowContractValidationException_WithMultipleErrors_WhenRequestValidationFails()
    {
        // Arrange
        var request = new TestRequest { Value = "test" };
        var errors = new[] { "Error 1", "Error 2", "Error 3" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(new TestResponse()));

        var schema = new JsonSchemaContract
        {
            Schema = "{\"type\": \"object\"}",
            ContentType = "application/json"
        };

        // Setup schema resolver to return schema for request
        _mockSchemaResolver.Setup(x => x.ResolveSchemaAsync(
            typeof(TestRequest),
            It.IsAny<SchemaContext>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(schema);

        _mockValidator.Setup(x => x.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(errors);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ContractValidationException>(async () =>
            await _behavior.HandleAsync(request, next, CancellationToken.None));

        Assert.Equal(typeof(TestRequest), exception.ObjectType);
        Assert.Equal(errors, exception.Errors);
        Assert.Contains("Error 1, Error 2, Error 3", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowContractValidationException_WithMultipleErrors_WhenResponseValidationFails()
    {
        // Arrange
        var request = new TestRequest { Value = "test" };
        var response = new TestResponse { Result = "result" };
        var errors = new[] { "Response error 1", "Response error 2" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        var schema = new JsonSchemaContract
        {
            Schema = "{\"type\": \"object\"}",
            ContentType = "application/json"
        };

        // Setup schema resolver to return schemas
        _mockSchemaResolver.Setup(x => x.ResolveSchemaAsync(
            typeof(TestRequest),
            It.IsAny<SchemaContext>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(schema);

        _mockSchemaResolver.Setup(x => x.ResolveSchemaAsync(
            typeof(TestResponse),
            It.IsAny<SchemaContext>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(schema);

        _mockValidator.Setup(x => x.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        _mockValidator.Setup(x => x.ValidateResponseAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(errors);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ContractValidationException>(async () =>
            await _behavior.HandleAsync(request, next, CancellationToken.None));

        Assert.Equal(typeof(TestResponse), exception.ObjectType);
        Assert.Equal(errors, exception.Errors);
        Assert.Contains("Response error 1, Response error 2", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_ShouldValidateBothRequestAndResponse_WhenBothFail()
    {
        // Arrange
        var request = new TestRequest { Value = "test" };
        var response = new TestResponse { Result = "result" };
        var requestErrors = new[] { "Request validation failed" };
        var responseErrors = new[] { "Response validation failed" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        var schema = new JsonSchemaContract
        {
            Schema = "{\"type\": \"object\"}",
            ContentType = "application/json"
        };

        // Setup schema resolver to return schema for request
        _mockSchemaResolver.Setup(x => x.ResolveSchemaAsync(
            typeof(TestRequest),
            It.IsAny<SchemaContext>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(schema);

        _mockValidator.Setup(x => x.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(requestErrors);
        _mockValidator.Setup(x => x.ValidateResponseAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseErrors);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ContractValidationException>(async () =>
            await _behavior.HandleAsync(request, next, CancellationToken.None));

        // Should throw for request validation first
        Assert.Equal(typeof(TestRequest), exception.ObjectType);
        Assert.Equal(requestErrors, exception.Errors);
    }

    [Fact]
    public async Task HandleAsync_ShouldUseSchemaResolver_WhenAvailable()
    {
        // Arrange
        var request = new TestRequest { Value = "test" };
        var response = new TestResponse { Result = "result" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        var schema = new JsonSchemaContract
        {
            Schema = "{\"type\": \"object\"}",
            ContentType = "application/json"
        };

        _mockSchemaResolver.Setup(x => x.ResolveSchemaAsync(
            typeof(TestRequest),
            It.IsAny<SchemaContext>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(schema);

        _mockSchemaResolver.Setup(x => x.ResolveSchemaAsync(
            typeof(TestResponse),
            It.IsAny<SchemaContext>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(schema);

        _mockValidator.Setup(x => x.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        _mockValidator.Setup(x => x.ValidateResponseAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var result = await _behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        _mockSchemaResolver.Verify(x => x.ResolveSchemaAsync(
            typeof(TestRequest),
            It.Is<SchemaContext>(c => c.IsRequest == true),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockSchemaResolver.Verify(x => x.ResolveSchemaAsync(
            typeof(TestResponse),
            It.Is<SchemaContext>(c => c.IsRequest == false),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldUseValidationStrategy_WhenConfigured()
    {
        // Arrange
        var relayOptions = new RelayOptions
        {
            DefaultContractValidationOptions = new ContractValidationOptions
            {
                EnableAutomaticContractValidation = true,
                ValidateRequests = true,
                ValidateResponses = true,
                ThrowOnValidationFailure = false, // Don't throw so we can verify strategy was used
                ValidationStrategy = "Lenient",
                EnablePerformanceMetrics = false
            }
        };
        _mockOptions.Setup(x => x.Value).Returns(relayOptions);

        var behavior = new ContractValidationPipelineBehavior<TestRequest, TestResponse>(
            _mockValidator.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            _mockSchemaResolver.Object,
            _strategyFactory);

        var request = new TestRequest { Value = "test" };
        var response = new TestResponse { Result = "result" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        _mockValidator.Setup(x => x.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        _mockValidator.Setup(x => x.ValidateResponseAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        // Verify that lenient strategy was used (it logs warnings instead of throwing)
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never); // No warnings should be logged for successful validation
    }

    [Fact]
    public async Task HandleAsync_ShouldLogPerformanceMetrics_WhenEnabled()
    {
        // Arrange
        var relayOptions = new RelayOptions
        {
            DefaultContractValidationOptions = new ContractValidationOptions
            {
                EnableAutomaticContractValidation = true,
                ValidateRequests = true,
                ValidateResponses = true,
                ThrowOnValidationFailure = true,
                EnablePerformanceMetrics = true
            }
        };
        _mockOptions.Setup(x => x.Value).Returns(relayOptions);

        var behavior = new ContractValidationPipelineBehavior<TestRequest, TestResponse>(
            _mockValidator.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            _mockSchemaResolver.Object,
            _strategyFactory);

        var request = new TestRequest { Value = "test" };
        var response = new TestResponse { Result = "result" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        _mockValidator.Setup(x => x.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());
        _mockValidator.Setup(x => x.ValidateResponseAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<string>());

        // Act
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        
        // Verify that debug logs were called for performance metrics
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("validation completed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleAsync_ShouldSkipValidation_WhenNoSchemaFound()
    {
        // Arrange
        var request = new TestRequest { Value = "test" };
        var response = new TestResponse { Result = "result" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Schema resolver returns null (no schema found)
        _mockSchemaResolver.Setup(x => x.ResolveSchemaAsync(
            It.IsAny<Type>(),
            It.IsAny<SchemaContext>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((JsonSchemaContract?)null);

        // Act
        var result = await _behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        
        // Validator should not be called when no schema is found
        _mockValidator.Verify(x => x.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockValidator.Verify(x => x.ValidateResponseAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldHandleSchemaResolverException_Gracefully()
    {
        // Arrange
        var request = new TestRequest { Value = "test" };
        var response = new TestResponse { Result = "result" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        // Schema resolver throws exception
        _mockSchemaResolver.Setup(x => x.ResolveSchemaAsync(
            It.IsAny<Type>(),
            It.IsAny<SchemaContext>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Schema resolution failed"));

        // Act
        var result = await _behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        
        // Should log warning about schema resolution failure
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to resolve schema")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowContractValidationException_WhenLenientStrategyReturnsInvalidResultAndThrowOnFailureIsTrue_ForRequest()
    {
        // Arrange
        var relayOptions = new RelayOptions
        {
            DefaultContractValidationOptions = new ContractValidationOptions
            {
                EnableAutomaticContractValidation = true,
                ValidateRequests = true,
                ValidateResponses = false,
                ThrowOnValidationFailure = true, // This should trigger the exception after strategy handling
                ValidationStrategy = "Lenient", // Lenient strategy doesn't throw in HandleResultAsync
                EnablePerformanceMetrics = false
            }
        };
        _mockOptions.Setup(x => x.Value).Returns(relayOptions);

        var behavior = new ContractValidationPipelineBehavior<TestRequest, TestResponse>(
            _mockValidator.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            _mockSchemaResolver.Object,
            _strategyFactory);

        var request = new TestRequest { Value = "test" };
        var response = new TestResponse { Result = "result" };
        var errors = new[] { "Invalid request property" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        var schema = new JsonSchemaContract
        {
            Schema = "{\"type\": \"object\"}",
            ContentType = "application/json"
        };

        // Setup schema resolver to return schema for request
        _mockSchemaResolver.Setup(x => x.ResolveSchemaAsync(
            typeof(TestRequest),
            It.IsAny<SchemaContext>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(schema);

        _mockValidator.Setup(x => x.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(errors);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ContractValidationException>(async () =>
            await behavior.HandleAsync(request, next, CancellationToken.None));

        Assert.Equal(typeof(TestRequest), exception.ObjectType);
        Assert.Contains("Invalid request property", exception.Errors.Single());
        Assert.Contains("Contract validation failed for TestRequest", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowContractValidationException_WhenLenientStrategyReturnsInvalidResultAndThrowOnFailureIsTrue_ForResponse()
    {
        // Arrange
        var relayOptions = new RelayOptions
        {
            DefaultContractValidationOptions = new ContractValidationOptions
            {
                EnableAutomaticContractValidation = true,
                ValidateRequests = false,
                ValidateResponses = true,
                ThrowOnValidationFailure = true, // This should trigger the exception after strategy handling
                ValidationStrategy = "Lenient", // Lenient strategy doesn't throw in HandleResultAsync
                EnablePerformanceMetrics = false
            }
        };
        _mockOptions.Setup(x => x.Value).Returns(relayOptions);

        var behavior = new ContractValidationPipelineBehavior<TestRequest, TestResponse>(
            _mockValidator.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            _mockSchemaResolver.Object,
            _strategyFactory);

        var request = new TestRequest { Value = "test" };
        var response = new TestResponse { Result = "result" };
        var errors = new[] { "Invalid response property" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        var schema = new JsonSchemaContract
        {
            Schema = "{\"type\": \"object\"}",
            ContentType = "application/json"
        };

        // Setup schema resolver to return schema for response
        _mockSchemaResolver.Setup(x => x.ResolveSchemaAsync(
            typeof(TestResponse),
            It.IsAny<SchemaContext>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(schema);

        _mockValidator.Setup(x => x.ValidateResponseAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(errors);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ContractValidationException>(async () =>
            await behavior.HandleAsync(request, next, CancellationToken.None));

        Assert.Equal(typeof(TestResponse), exception.ObjectType);
        Assert.Contains("Invalid response property", exception.Errors.Single());
        Assert.Contains("Contract validation failed for TestResponse", exception.Message);
    }

    [Fact]
    public async Task HandleAsync_ShouldUseHandlerSpecificOverride_WhenConfigured()
    {
        // Arrange
        var handlerKey = typeof(TestRequest).FullName ?? typeof(TestRequest).Name;
        var handlerSpecificOptions = new ContractValidationOptions
        {
            EnableAutomaticContractValidation = true,
            ValidateRequests = true,
            ValidateResponses = false,
            ThrowOnValidationFailure = false,
            ValidationStrategy = "Lenient",
            EnablePerformanceMetrics = false
        };

        var relayOptions = new RelayOptions
        {
            DefaultContractValidationOptions = new ContractValidationOptions
            {
                EnableAutomaticContractValidation = false, // Default is disabled
                ValidateRequests = false,
                ValidateResponses = false,
                ThrowOnValidationFailure = true,
                ValidationStrategy = "Strict",
                EnablePerformanceMetrics = false
            },
            ContractValidationOverrides = new Dictionary<string, ContractValidationOptions>
            {
                [handlerKey] = handlerSpecificOptions // This should be used instead of defaults
            }
        };
        _mockOptions.Setup(x => x.Value).Returns(relayOptions);

        var behavior = new ContractValidationPipelineBehavior<TestRequest, TestResponse>(
            _mockValidator.Object,
            _mockLogger.Object,
            _mockOptions.Object,
            _mockSchemaResolver.Object,
            _strategyFactory);

        var request = new TestRequest { Value = "test" };
        var response = new TestResponse { Result = "result" };
        var errors = new[] { "Invalid request property" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

        var schema = new JsonSchemaContract
        {
            Schema = "{\"type\": \"object\"}",
            ContentType = "application/json"
        };

        // Setup schema resolver to return schema for request
        _mockSchemaResolver.Setup(x => x.ResolveSchemaAsync(
            typeof(TestRequest),
            It.IsAny<SchemaContext>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(schema);

        _mockValidator.Setup(x => x.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(errors);

        // Act - Should not throw because handler-specific override has ThrowOnValidationFailure = false
        var result = await behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        Assert.Equal(response, result);
        
        // Verify that validation was attempted (because handler override enables it)
        _mockValidator.Verify(x => x.ValidateRequestAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()), Times.Once);
        
        // Verify that response validation was NOT attempted (because handler override disables it)
        _mockValidator.Verify(x => x.ValidateResponseAsync(It.IsAny<object>(), It.IsAny<JsonSchemaContract>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    public class TestRequest : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }
}