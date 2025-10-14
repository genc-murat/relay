using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.ContractValidation;
using Relay.Core.Telemetry;
using Relay.Core.Validation;
using Relay.Core.Validation.Interfaces;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

public class MessageBrokerValidationAdapterTests
{
    private readonly Mock<IContractValidator> _contractValidatorMock;
    private readonly Mock<ILogger<MessageBrokerValidationAdapter>> _loggerMock;
    private readonly RelayTelemetryOptions _options;

    public MessageBrokerValidationAdapterTests()
    {
        _contractValidatorMock = new Mock<IContractValidator>();
        _loggerMock = new Mock<ILogger<MessageBrokerValidationAdapter>>();
        _options = new RelayTelemetryOptions
        {
            Component = RelayTelemetryConstants.Components.MessageBroker
        };
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_Logger_Is_Null()
    {
        // Arrange
        var options = Options.Create(_options);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MessageBrokerValidationAdapter(_contractValidatorMock.Object, null!, options));
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_Options_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, null!));
    }

    [Fact]
    public void Constructor_Should_Initialize_With_Valid_Parameters()
    {
        // Arrange
        var options = Options.Create(_options);

        // Act
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);

        // Assert
        Assert.NotNull(adapter);
    }

    [Fact]
    public void Constructor_Should_Initialize_With_Null_ContractValidator()
    {
        // Arrange
        var options = Options.Create(_options);

        // Act
        var adapter = new MessageBrokerValidationAdapter(null, _loggerMock.Object, options);

        // Assert
        Assert.NotNull(adapter);
    }

    [Fact]
    public async Task ValidateMessageAsync_Should_Return_False_When_Message_Is_Null()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);

        // Act
        var result = await adapter.ValidateMessageAsync<string>(null!);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Message validation failed: message is null")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateMessageAsync_Should_Return_True_When_Validator_Is_Null()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);
        var message = "test message";

        // Act
        var result = await adapter.ValidateMessageAsync(message, null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateMessageAsync_Should_Return_True_When_Validation_Passes()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);
        var message = "test message";
        var validatorMock = new Mock<IValidator<string>>();
        validatorMock.Setup(v => v.ValidateAsync(message, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((IEnumerable<string>?)null);

        // Act
        var result = await adapter.ValidateMessageAsync(message, validatorMock.Object);

        // Assert
        Assert.True(result);
        validatorMock.Verify(v => v.ValidateAsync(message, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateMessageAsync_Should_Return_False_When_Validation_Fails()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);
        var message = "test message";
        var errors = new[] { "Error 1", "Error 2" };
        var validatorMock = new Mock<IValidator<string>>();
        validatorMock.Setup(v => v.ValidateAsync(message, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(errors);

        // Act
        var result = await adapter.ValidateMessageAsync(message, validatorMock.Object);

        // Assert
        Assert.False(result);
        validatorMock.Verify(v => v.ValidateAsync(message, It.IsAny<CancellationToken>()), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Message validation error: Error 1")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Message validation error: Error 2")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateMessageAsync_Should_Return_False_When_Validator_Throws_Exception()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);
        var message = "test message";
        var validatorMock = new Mock<IValidator<string>>();
        validatorMock.Setup(v => v.ValidateAsync(message, It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("Validation error"));

        // Act
        var result = await adapter.ValidateMessageAsync(message, validatorMock.Object);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Message validation failed with exception")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateMessageAsync_Should_Return_True_When_Validator_Returns_Empty_Errors()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);
        var message = "test message";
        var validatorMock = new Mock<IValidator<string>>();
        validatorMock.Setup(v => v.ValidateAsync(message, It.IsAny<CancellationToken>()))
                     .ReturnsAsync(Enumerable.Empty<string>());

        // Act
        var result = await adapter.ValidateMessageAsync(message, validatorMock.Object);

        // Assert
        Assert.True(result);
        validatorMock.Verify(v => v.ValidateAsync(message, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateMessageAsync_Should_Pass_CancellationToken_To_Validator()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);
        var message = "test message";
        var cancellationToken = new CancellationToken(true);
        var validatorMock = new Mock<IValidator<string>>();
        validatorMock.Setup(v => v.ValidateAsync(message, cancellationToken))
                     .ReturnsAsync((IEnumerable<string>?)null);

        // Act
        var result = await adapter.ValidateMessageAsync(message, validatorMock.Object, cancellationToken);

        // Assert
        Assert.True(result);
        validatorMock.Verify(v => v.ValidateAsync(message, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task ValidateMessageAgainstSchemaAsync_Should_Return_False_When_Message_Is_Null()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);
        var schema = new JsonSchemaContract { Schema = "test", SchemaVersion = "1.0", Properties = new Dictionary<string, object>() };

        // Act
        var result = await adapter.ValidateMessageAgainstSchemaAsync(null!, schema);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Schema validation failed: message is null")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateMessageAgainstSchemaAsync_Should_Return_False_When_Schema_Is_Null()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);
        var message = new { data = "test" };

        // Act
        var result = await adapter.ValidateMessageAgainstSchemaAsync(message, null!);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Schema validation failed: schema is null")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateMessageAgainstSchemaAsync_Should_Return_False_When_ContractValidator_Is_Null()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(null, _loggerMock.Object, options);
        var message = new { data = "test" };
        var schema = new JsonSchemaContract { Schema = "test", SchemaVersion = "1.0", Properties = new Dictionary<string, object>() };

        // Act
        var result = await adapter.ValidateMessageAgainstSchemaAsync(message, schema);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Schema validation failed: no contract validator available")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateMessageAgainstSchemaAsync_Should_Return_True_When_Validation_Passes()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);
        var message = new { data = "test" };
        var schema = new JsonSchemaContract { Schema = "test", SchemaVersion = "1.0", Properties = new Dictionary<string, object>() };
        _contractValidatorMock.Setup(v => v.ValidateRequestAsync(message, schema, It.IsAny<CancellationToken>()))
                             .ReturnsAsync((IEnumerable<string>?)null);

        // Act
        var result = await adapter.ValidateMessageAgainstSchemaAsync(message, schema);

        // Assert
        Assert.True(result);
        _contractValidatorMock.Verify(v => v.ValidateRequestAsync(message, schema, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateMessageAgainstSchemaAsync_Should_Return_False_When_Validation_Fails()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);
        var message = new { data = "test" };
        var schema = new JsonSchemaContract { Schema = "test", SchemaVersion = "1.0", Properties = new Dictionary<string, object>() };
        var errors = new[] { "Schema error 1", "Schema error 2" };
        _contractValidatorMock.Setup(v => v.ValidateRequestAsync(message, schema, It.IsAny<CancellationToken>()))
                             .ReturnsAsync(errors);

        // Act
        var result = await adapter.ValidateMessageAgainstSchemaAsync(message, schema);

        // Assert
        Assert.False(result);
        _contractValidatorMock.Verify(v => v.ValidateRequestAsync(message, schema, It.IsAny<CancellationToken>()), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Schema validation error: Schema error 1")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Schema validation error: Schema error 2")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateMessageAgainstSchemaAsync_Should_Return_False_When_ContractValidator_Throws_Exception()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);
        var message = new { data = "test" };
        var schema = new JsonSchemaContract { Schema = "test", SchemaVersion = "1.0", Properties = new Dictionary<string, object>() };
        _contractValidatorMock.Setup(v => v.ValidateRequestAsync(message, schema, It.IsAny<CancellationToken>()))
                              .ThrowsAsync(new Exception("Contract validation error"));

        // Act
        var result = await adapter.ValidateMessageAgainstSchemaAsync(message, schema);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Schema validation failed with exception")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateMessageAgainstSchemaAsync_Should_Return_True_When_ContractValidator_Returns_Empty_Errors()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);
        var message = new { data = "test" };
        var schema = new JsonSchemaContract { Schema = "test", SchemaVersion = "1.0", Properties = new Dictionary<string, object>() };
        _contractValidatorMock.Setup(v => v.ValidateRequestAsync(message, schema, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(Enumerable.Empty<string>());

        // Act
        var result = await adapter.ValidateMessageAgainstSchemaAsync(message, schema);

        // Assert
        Assert.True(result);
        _contractValidatorMock.Verify(v => v.ValidateRequestAsync(message, schema, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateMessageAgainstSchemaAsync_Should_Pass_CancellationToken_To_ContractValidator()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);
        var message = new { data = "test" };
        var schema = new JsonSchemaContract { Schema = "test", SchemaVersion = "1.0", Properties = new Dictionary<string, object>() };
        var cancellationToken = new CancellationToken(true);
        _contractValidatorMock.Setup(v => v.ValidateRequestAsync(message, schema, cancellationToken))
                              .ReturnsAsync((IEnumerable<string>?)null);

        // Act
        var result = await adapter.ValidateMessageAgainstSchemaAsync(message, schema, cancellationToken);

        // Assert
        Assert.True(result);
        _contractValidatorMock.Verify(v => v.ValidateRequestAsync(message, schema, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task ValidateMessageSchemaAsync_Should_Return_Errors_When_Message_Is_Null()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);
        var schema = new JsonSchemaContract { Schema = "test", SchemaVersion = "1.0", Properties = new Dictionary<string, object>() };

        // Act
        var errors = await adapter.ValidateMessageSchemaAsync(null!, schema);

        // Assert
        Assert.Contains("Message cannot be null", errors);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Schema validation failed: message is null")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateMessageSchemaAsync_Should_Return_Errors_When_Schema_Is_Null()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);
        var message = new { data = "test" };

        // Act
        var errors = await adapter.ValidateMessageSchemaAsync(message, null!);

        // Assert
        Assert.Contains("Schema cannot be null", errors);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Schema validation failed: schema is null")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateMessageSchemaAsync_Should_Return_Empty_Errors_When_ContractValidator_Is_Null()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(null, _loggerMock.Object, options);
        var message = new { data = "test" };
        var schema = new JsonSchemaContract { Schema = "test", SchemaVersion = "1.0", Properties = new Dictionary<string, object>() };

        // Act
        var errors = await adapter.ValidateMessageSchemaAsync(message, schema);

        // Assert
        Assert.Empty(errors);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Schema validation skipped: no contract validator available")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateMessageSchemaAsync_Should_Return_Validation_Errors_When_Validation_Fails()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);
        var message = new { data = "test" };
        var schema = new JsonSchemaContract { Schema = "test", SchemaVersion = "1.0", Properties = new Dictionary<string, object>() };
        var validationErrors = new[] { "Validation error 1", "Validation error 2" };
        _contractValidatorMock.Setup(v => v.ValidateRequestAsync(message, schema, It.IsAny<CancellationToken>()))
                             .ReturnsAsync(validationErrors);

        // Act
        var errors = await adapter.ValidateMessageSchemaAsync(message, schema);

        // Assert
        Assert.Equal(validationErrors, errors);
        _contractValidatorMock.Verify(v => v.ValidateRequestAsync(message, schema, It.IsAny<CancellationToken>()), Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Schema validation error: Validation error 1")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Schema validation error: Validation error 2")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateMessageSchemaAsync_Should_Return_Exception_Error_When_ContractValidator_Throws_Exception()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);
        var message = new { data = "test" };
        var schema = new JsonSchemaContract { Schema = "test", SchemaVersion = "1.0", Properties = new Dictionary<string, object>() };
        var exception = new Exception("Contract validation error");
        _contractValidatorMock.Setup(v => v.ValidateRequestAsync(message, schema, It.IsAny<CancellationToken>()))
                              .ThrowsAsync(exception);

        // Act
        var errors = await adapter.ValidateMessageSchemaAsync(message, schema);

        // Assert
        Assert.Contains("Schema validation failed with exception: Contract validation error", errors);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Schema validation failed with exception")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateMessageSchemaAsync_Should_Return_Empty_Errors_When_ContractValidator_Returns_Empty_Errors()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);
        var message = new { data = "test" };
        var schema = new JsonSchemaContract { Schema = "test", SchemaVersion = "1.0", Properties = new Dictionary<string, object>() };
        _contractValidatorMock.Setup(v => v.ValidateRequestAsync(message, schema, It.IsAny<CancellationToken>()))
                              .ReturnsAsync(Enumerable.Empty<string>());

        // Act
        var errors = await adapter.ValidateMessageSchemaAsync(message, schema);

        // Assert
        Assert.Empty(errors);
        _contractValidatorMock.Verify(v => v.ValidateRequestAsync(message, schema, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateMessageSchemaAsync_Should_Return_Empty_Errors_When_ContractValidator_Returns_Null_Errors()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);
        var message = new { data = "test" };
        var schema = new JsonSchemaContract { Schema = "test", SchemaVersion = "1.0", Properties = new Dictionary<string, object>() };
        _contractValidatorMock.Setup(v => v.ValidateRequestAsync(message, schema, It.IsAny<CancellationToken>()))
                              .ReturnsAsync((IEnumerable<string>?)null);

        // Act
        var errors = await adapter.ValidateMessageSchemaAsync(message, schema);

        // Assert
        Assert.Empty(errors);
        _contractValidatorMock.Verify(v => v.ValidateRequestAsync(message, schema, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateMessageSchemaAsync_Should_Pass_CancellationToken_To_ContractValidator()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);
        var message = new { data = "test" };
        var schema = new JsonSchemaContract { Schema = "test", SchemaVersion = "1.0", Properties = new Dictionary<string, object>() };
        var cancellationToken = new CancellationToken(true);
        _contractValidatorMock.Setup(v => v.ValidateRequestAsync(message, schema, cancellationToken))
                              .ReturnsAsync(Enumerable.Empty<string>());

        // Act
        var errors = await adapter.ValidateMessageSchemaAsync(message, schema, cancellationToken);

        // Assert
        Assert.Empty(errors);
        _contractValidatorMock.Verify(v => v.ValidateRequestAsync(message, schema, cancellationToken), Times.Once);
    }

    [Fact]
    public void ValidateBasicMessageFields_Should_Return_False_When_MessageType_Is_Null()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);

        // Act
        var result = adapter.ValidateBasicMessageFields(null, new { data = "test" });

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Message unknown missing 'type' field")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ValidateBasicMessageFields_Should_Return_False_When_MessageType_Is_Empty()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);

        // Act
        var result = adapter.ValidateBasicMessageFields("", new { data = "test" });

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Message unknown missing 'type' field")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ValidateBasicMessageFields_Should_Return_False_When_MessageType_Is_Whitespace()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);

        // Act
        var result = adapter.ValidateBasicMessageFields("   ", new { data = "test" });

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Message unknown missing 'type' field")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ValidateBasicMessageFields_Should_Return_False_When_MessageData_Is_Null()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);

        // Act
        var result = adapter.ValidateBasicMessageFields("test.type", null);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Message unknown missing 'data' field")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void ValidateBasicMessageFields_Should_Return_True_When_All_Fields_Are_Valid()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);

        // Act
        var result = adapter.ValidateBasicMessageFields("test.type", new { data = "test" });

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ValidateBasicMessageFields_Should_Use_MessageId_In_Log_When_Provided()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerValidationAdapter(_contractValidatorMock.Object, _loggerMock.Object, options);
        var messageId = "msg-123";

        // Act
        var result = adapter.ValidateBasicMessageFields(null, new { data = "test" }, messageId);

        // Assert
        Assert.False(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Message {messageId} missing 'type' field")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}