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

public class MessageBrokerValidationAdapterConstructorTests
{
    private readonly Mock<IContractValidator> _contractValidatorMock;
    private readonly Mock<ILogger<MessageBrokerValidationAdapter>> _loggerMock;
    private readonly RelayTelemetryOptions _options;

    public MessageBrokerValidationAdapterConstructorTests()
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
}