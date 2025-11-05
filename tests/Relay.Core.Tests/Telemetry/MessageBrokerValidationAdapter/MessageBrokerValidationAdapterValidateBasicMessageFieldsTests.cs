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

namespace Relay.Core.Tests.Telemetry;

public class MessageBrokerValidationAdapterValidateBasicMessageFieldsTests
{
    private readonly Mock<IContractValidator> _contractValidatorMock;
    private readonly Mock<ILogger<MessageBrokerValidationAdapter>> _loggerMock;
    private readonly RelayTelemetryOptions _options;

    public MessageBrokerValidationAdapterValidateBasicMessageFieldsTests()
    {
        _contractValidatorMock = new Mock<IContractValidator>();
        _loggerMock = new Mock<ILogger<MessageBrokerValidationAdapter>>();
        _options = new RelayTelemetryOptions
        {
            Component = RelayTelemetryConstants.Components.MessageBroker
        };
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
