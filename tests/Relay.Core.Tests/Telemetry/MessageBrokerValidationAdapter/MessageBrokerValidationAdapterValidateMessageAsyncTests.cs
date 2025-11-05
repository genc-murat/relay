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
using Relay.Core.Testing;
using Relay.Core.Validation;
using Relay.Core.Validation.Interfaces;
using Xunit;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Relay.Core.Tests.Telemetry;

public class MessageBrokerValidationAdapterValidateMessageAsyncTests
{
    private readonly Mock<IContractValidator> _contractValidatorMock;
    private readonly Mock<ILogger<MessageBrokerValidationAdapter>> _loggerMock;
    private readonly RelayTelemetryOptions _options;

    public MessageBrokerValidationAdapterValidateMessageAsyncTests()
    {
        _contractValidatorMock = new Mock<IContractValidator>();
        _loggerMock = new Mock<ILogger<MessageBrokerValidationAdapter>>();
        _options = new RelayTelemetryOptions
        {
            Component = RelayTelemetryConstants.Components.MessageBroker
        };
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
                MsLogLevel.Warning,
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
                MsLogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Message validation error: Error 1")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        _loggerMock.Verify(
            x => x.Log(
                MsLogLevel.Warning,
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
                MsLogLevel.Error,
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
}
