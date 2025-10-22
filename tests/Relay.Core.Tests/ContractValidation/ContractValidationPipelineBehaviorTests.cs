using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Configuration.Options.ContractValidation;
using Relay.Core.Configuration.Options.Core;
using Relay.Core.Contracts.Pipeline;
using Relay.Core.Contracts.Requests;
using Relay.Core.ContractValidation;
using Relay.Core.Metadata.MessageQueue;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Relay.Core.Tests.ContractValidation;

public class ContractValidationPipelineBehaviorTests
{
    private readonly Mock<IContractValidator> _mockValidator;
    private readonly Mock<ILogger<ContractValidationPipelineBehavior<TestRequest, TestResponse>>> _mockLogger;
    private readonly Mock<IOptions<RelayOptions>> _mockOptions;
    private readonly ContractValidationPipelineBehavior<TestRequest, TestResponse> _behavior;

    public ContractValidationPipelineBehaviorTests()
    {
        _mockValidator = new Mock<IContractValidator>();
        _mockLogger = new Mock<ILogger<ContractValidationPipelineBehavior<TestRequest, TestResponse>>>();
        _mockOptions = new Mock<IOptions<RelayOptions>>();

        var relayOptions = new RelayOptions
        {
            DefaultContractValidationOptions = new ContractValidationOptions
            {
                EnableAutomaticContractValidation = true,
                ValidateRequests = true,
                ValidateResponses = true,
                ThrowOnValidationFailure = true
            }
        };
        _mockOptions.Setup(x => x.Value).Returns(relayOptions);

        _behavior = new ContractValidationPipelineBehavior<TestRequest, TestResponse>(
            _mockValidator.Object,
            _mockLogger.Object,
            _mockOptions.Object);
    }

    [Fact]
    public async Task HandleAsync_ShouldValidateRequestAndResponse_WhenValidationEnabled()
    {
        // Arrange
        var request = new TestRequest { Value = "test" };
        var response = new TestResponse { Result = "result" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(response));

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
                EnableAutomaticContractValidation = false
            }
        };
        _mockOptions.Setup(x => x.Value).Returns(relayOptions);

        var behavior = new ContractValidationPipelineBehavior<TestRequest, TestResponse>(
            _mockValidator.Object,
            _mockLogger.Object,
            _mockOptions.Object);

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
                ThrowOnValidationFailure = false
            }
        };
        _mockOptions.Setup(x => x.Value).Returns(relayOptions);

        var behavior = new ContractValidationPipelineBehavior<TestRequest, TestResponse>(
            _mockValidator.Object,
            _mockLogger.Object,
            _mockOptions.Object);

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
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowContractValidationException_WithMultipleErrors_WhenRequestValidationFails()
    {
        // Arrange
        var request = new TestRequest { Value = "test" };
        var errors = new[] { "Error 1", "Error 2", "Error 3" };
        var next = new RequestHandlerDelegate<TestResponse>(() => new ValueTask<TestResponse>(new TestResponse()));

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

    public class TestRequest : IRequest<TestResponse>
    {
        public string Value { get; set; } = string.Empty;
    }

    public class TestResponse
    {
        public string Result { get; set; } = string.Empty;
    }
}