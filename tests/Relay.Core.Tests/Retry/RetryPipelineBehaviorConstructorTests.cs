using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Configuration.Options;
using Relay.Core.Configuration.Options.Core;
using Relay.Core.Configuration.Options.Retry;
using Relay.Core.Retry;
using Xunit;
using TestRequest = Relay.Core.Tests.Retry.RetryPipelineBehaviorTestUtilities.TestRequest;
using TestResponse = Relay.Core.Tests.Retry.RetryPipelineBehaviorTestUtilities.TestResponse;

namespace Relay.Core.Tests.Retry;

public class RetryPipelineBehaviorConstructorTests
{
    private readonly Mock<ILogger<RetryPipelineBehavior<TestRequest, TestResponse>>> _loggerMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly RelayOptions _relayOptions;

    public RetryPipelineBehaviorConstructorTests()
    {
        _loggerMock = new Mock<ILogger<RetryPipelineBehavior<TestRequest, TestResponse>>>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _relayOptions = new RelayOptions
        {
            DefaultRetryOptions = new RetryOptions()
        };
    }

    [Fact]
    public void Constructor_Should_ThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act
        Action act = () => new RetryPipelineBehavior<TestRequest, TestResponse>(
            null!,
            Options.Create(_relayOptions),
            _serviceProviderMock.Object);

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("logger", ex.ParamName);
    }

    [Fact]
    public void Constructor_Should_ThrowArgumentNullException_WhenOptionsIsNull()
    {
        // Act
        Action act = () => new RetryPipelineBehavior<TestRequest, TestResponse>(
            _loggerMock.Object,
            null!,
            _serviceProviderMock.Object);

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("options", ex.ParamName);
    }

    [Fact]
    public void Constructor_Should_ThrowArgumentNullException_WhenServiceProviderIsNull()
    {
        // Act
        Action act = () => new RetryPipelineBehavior<TestRequest, TestResponse>(
            _loggerMock.Object,
            Options.Create(_relayOptions),
            null!);

        // Assert
        var ex = Assert.Throws<ArgumentNullException>(act);
        Assert.Equal("serviceProvider", ex.ParamName);
    }

    [Fact]
    public void Constructor_Should_InitializeSuccessfully_WithValidParameters()
    {
        // Act
        var behavior = new RetryPipelineBehavior<TestRequest, TestResponse>(
            _loggerMock.Object,
            Options.Create(_relayOptions),
            _serviceProviderMock.Object);

        // Assert
        Assert.NotNull(behavior);
    }
}