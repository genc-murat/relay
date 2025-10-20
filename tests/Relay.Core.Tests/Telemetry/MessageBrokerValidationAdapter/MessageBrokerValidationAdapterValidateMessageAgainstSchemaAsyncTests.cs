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
using Relay.Core.Telemetry;
using Relay.Core.Validation;
using Relay.Core.Validation.Interfaces;
using Xunit;

namespace Relay.Core.Tests.Telemetry;

public class MessageBrokerValidationAdapterValidateMessageAgainstSchemaAsyncTests
{
    private readonly Mock<IContractValidator> _contractValidatorMock;
    private readonly Mock<ILogger<MessageBrokerValidationAdapter>> _loggerMock;
    private readonly RelayTelemetryOptions _options;

    public MessageBrokerValidationAdapterValidateMessageAgainstSchemaAsyncTests()
    {
        _contractValidatorMock = new Mock<IContractValidator>();
        _loggerMock = new Mock<ILogger<MessageBrokerValidationAdapter>>();
        _options = new RelayTelemetryOptions
        {
            Component = RelayTelemetryConstants.Components.MessageBroker
        };
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
}