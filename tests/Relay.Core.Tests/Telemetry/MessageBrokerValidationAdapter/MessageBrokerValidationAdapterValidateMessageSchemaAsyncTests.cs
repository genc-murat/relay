using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.ContractValidation;
using Relay.Core.Metadata.MessageQueue;
using Relay.Core.Testing;
using Relay.Core.Telemetry;
using Relay.Core.Validation;
using Relay.Core.Validation.Interfaces;
using Xunit;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Relay.Core.Tests.Telemetry;

public class MessageBrokerValidationAdapterValidateMessageSchemaAsyncTests
{
    private readonly Mock<IContractValidator> _contractValidatorMock;
    private readonly Mock<ILogger<MessageBrokerValidationAdapter>> _loggerMock;
    private readonly RelayTelemetryOptions _options;

    public MessageBrokerValidationAdapterValidateMessageSchemaAsyncTests()
    {
        _contractValidatorMock = new Mock<IContractValidator>();
        _loggerMock = new Mock<ILogger<MessageBrokerValidationAdapter>>();
        _options = new RelayTelemetryOptions
        {
            Component = RelayTelemetryConstants.Components.MessageBroker
        };
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
                MsLogLevel.Warning,
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
                MsLogLevel.Warning,
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
                MsLogLevel.Warning,
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
                MsLogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Schema validation error: Validation error 1")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                MsLogLevel.Warning,
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
                MsLogLevel.Error,
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
}
