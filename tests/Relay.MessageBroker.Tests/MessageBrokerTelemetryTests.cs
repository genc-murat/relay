using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Telemetry;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class MessageBrokerTelemetryTests
{
    private readonly Mock<ILogger<MessageBrokerTelemetryAdapter>> _loggerMock;
    private readonly UnifiedTelemetryOptions _options;

    public MessageBrokerTelemetryTests()
    {
        _loggerMock = new Mock<ILogger<MessageBrokerTelemetryAdapter>>();
        _options = new UnifiedTelemetryOptions
        {
            Component = UnifiedTelemetryConstants.Components.MessageBroker
        };
    }

    [Fact]
    public void Constructor_Should_Throw_ArgumentNullException_When_Options_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MessageBrokerTelemetryAdapter(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_Should_Initialize_With_Null_Logger()
    {
        // Arrange
        var options = Options.Create(_options);

        // Act
        var adapter = new MessageBrokerTelemetryAdapter(options, null);

        // Assert
        Assert.NotNull(adapter);
    }

    [Fact]
    public void Constructor_Should_Initialize_With_Valid_Parameters()
    {
        // Arrange
        var options = Options.Create(_options);

        // Act
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Assert
        Assert.NotNull(adapter);
    }

    [Fact]
    public void RecordMessagePublished_Should_Log_Debug_Message()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act
        adapter.RecordMessagePublished("TestMessage", 1024, true);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Message published: TestMessage, Size: 1024 bytes, Compressed: True")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordMessagePublished_Should_Handle_Uncompressed_Message()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act
        adapter.RecordMessagePublished("TestMessage", 512, false);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Compressed: False")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordMessagePublished_Should_Work_With_Null_Logger()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, null);

        // Act & Assert - Should not throw
        adapter.RecordMessagePublished("TestMessage", 1024, true);
    }

    [Fact]
    public void RecordMessageReceived_Should_Log_Debug_Message()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act
        adapter.RecordMessageReceived("TestMessage", 2048, false);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Message received: TestMessage, Size: 2048 bytes, Compressed: False")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordMessageReceived_Should_Work_With_Null_Logger()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, null);

        // Act & Assert - Should not throw
        adapter.RecordMessageReceived("TestMessage", 1024, true);
    }

    [Fact]
    public void RecordProcessingDuration_Should_Log_Debug_Message()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);
        var duration = TimeSpan.FromMilliseconds(150);

        // Act
        adapter.RecordProcessingDuration("TestMessage", duration);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Message processing duration: TestMessage, Duration: 150ms")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordProcessingDuration_Should_Work_With_Null_Logger()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, null);
        var duration = TimeSpan.FromSeconds(1);

        // Act & Assert - Should not throw
        adapter.RecordProcessingDuration("TestMessage", duration);
    }

    [Fact]
    public void RecordProcessingDuration_Should_Handle_Zero_Duration()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);
        var duration = TimeSpan.Zero;

        // Act
        adapter.RecordProcessingDuration("TestMessage", duration);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Duration: 0ms")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordError_Should_Log_Error_Message()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act
        adapter.RecordError("ConnectionError", "Failed to connect to message broker");

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("MessageBroker error: ConnectionError - Failed to connect to message broker")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordError_Should_Work_With_Null_Logger()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, null);

        // Act & Assert - Should not throw
        adapter.RecordError("TestError", "Test error message");
    }

    [Fact]
    public void RecordError_Should_Handle_Empty_Error_Type()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act
        adapter.RecordError("", "Error message");

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("MessageBroker error:  - Error message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordError_Should_Handle_Empty_Error_Message()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act
        adapter.RecordError("TestError", "");

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("MessageBroker error: TestError - ")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void All_Methods_Should_Handle_Large_Message_Sizes()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);
        long largeSize = 10L * 1024 * 1024 * 1024; // 10GB

        // Act
        adapter.RecordMessagePublished("LargeMessage", largeSize, true);
        adapter.RecordMessageReceived("LargeMessage", largeSize, true);

        // Assert - Should not throw and should log correctly
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Size: 10737418240 bytes")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    [Fact]
    public void All_Methods_Should_Handle_Zero_Message_Size()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act
        adapter.RecordMessagePublished("EmptyMessage", 0, false);
        adapter.RecordMessageReceived("EmptyMessage", 0, false);

        // Assert - Should not throw and should log correctly
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Size: 0 bytes")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    [Fact]
    public void RecordProcessingDuration_Should_Handle_Negative_Duration()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);
        var negativeDuration = TimeSpan.FromMilliseconds(-100);

        // Act
        adapter.RecordProcessingDuration("TestMessage", negativeDuration);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Duration: -100ms")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
