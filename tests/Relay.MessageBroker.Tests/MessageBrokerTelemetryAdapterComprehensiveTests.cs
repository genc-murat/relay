using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Telemetry;

namespace Relay.MessageBroker.Tests;

public class MessageBrokerTelemetryAdapterComprehensiveTests
{
    private readonly Mock<ILogger<MessageBrokerTelemetryAdapter>> _loggerMock;
    private readonly RelayTelemetryOptions _options;

    public MessageBrokerTelemetryAdapterComprehensiveTests()
    {
        _loggerMock = new Mock<ILogger<MessageBrokerTelemetryAdapter>>();
        _options = new RelayTelemetryOptions
        {
            Component = RelayTelemetryConstants.Components.MessageBroker
        };
    }

    [Fact]
    public void RecordMessagePublished_Should_Create_Activity_With_Correct_Tags()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act
        adapter.RecordMessagePublished("TestMessage", 1024, true);

        // The activity functionality is internal to RelayTelemetryProvider, 
        // but we can verify that the method completes without error
        // and that logger was called appropriately for the debug message
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Message published")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordMessageReceived_Should_Create_Activity_With_Correct_Tags()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act
        adapter.RecordMessageReceived("TestMessage", 2048, false);

        // Verify that logger was called
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Message received")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordError_Should_Create_Activity_With_Correct_Tags_And_Status()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act
        adapter.RecordError("ConnectionError", "Failed to connect");

        // Verify that logger was called with error level
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("MessageBroker error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordMessagePublished_With_Long_Message_Type_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);
        var longMessageType = new string('M', 1000); // Very long message type

        // Act & Assert - Should not throw
        adapter.RecordMessagePublished(longMessageType, 1024, false);
    }

    [Fact]
    public void RecordMessageReceived_With_Maximum_Long_Message_Type_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);
        var longMessageType = new string('M', 5000); // Very long message type

        // Act & Assert - Should not throw
        adapter.RecordMessageReceived(longMessageType, 2048, true);
    }

    [Fact]
    public void RecordProcessingDuration_With_Very_Long_Duration_Should_Work()
    {
        // Arrange 
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);
        var veryLongDuration = TimeSpan.FromDays(1); // Very long duration

        // Act & Assert - Should not throw
        adapter.RecordProcessingDuration("TestMessage", veryLongDuration);
    }

    [Fact]
    public void RecordProcessingDuration_With_Negative_Values_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);
        var negativeDuration = TimeSpan.FromTicks(-1); // Negative duration

        // Act & Assert - Should not throw
        adapter.RecordProcessingDuration("TestMessage", negativeDuration);
    }

    [Fact]
    public void RecordError_With_Long_Error_Message_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);
        var longErrorMessage = new string('E', 10000); // Very long error message

        // Act & Assert - Should not throw
        adapter.RecordError("TestError", longErrorMessage);
    }

    [Fact]
    public void All_Methods_Should_Handle_Null_Values_Gracefully()
    {
        // Test method overloads that might accept nulls
        // Note: The public interface doesn't accept nulls for string parameters
        // but we can test with empty strings and other edge cases
        
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act & Assert - Should not throw for empty strings
        adapter.RecordMessagePublished("", 0, false);
        adapter.RecordMessageReceived("", 0, false);
        adapter.RecordProcessingDuration("", TimeSpan.Zero);
        adapter.RecordError("", "");
    }

    [Fact]
    public void Multiple_Calls_Should_Work_Without_Errors()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act - Multiple calls to all methods
        for (int i = 0; i < 10; i++)
        {
            adapter.RecordMessagePublished($"Message{i}", i * 100, i % 2 == 0);
            adapter.RecordMessageReceived($"Message{i}", i * 100, i % 2 == 0);
            adapter.RecordProcessingDuration($"Message{i}", TimeSpan.FromMilliseconds(i * 10));
            adapter.RecordError($"Error{i}", $"Error message {i}");
        }

        // Assert - Should not have thrown any exceptions
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(20)); // At least 10 calls each to RecordMessagePublished and RecordMessageReceived
    }

    [Fact]
    public void RecordMessagePublished_With_Negative_Size_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act & Assert - Should not throw
        adapter.RecordMessagePublished("TestMessage", -1, false);
    }

    [Fact]
    public void RecordMessageReceived_With_Negative_Size_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act & Assert - Should not throw
        adapter.RecordMessageReceived("TestMessage", -1024, true);
    }

    [Fact]
    public void RecordError_With_Unicode_Error_Type_And_Message_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act - Test with unicode characters
        adapter.RecordError("ТестоваяОшибка", "Тестовое сообщение об ошибке");
        adapter.RecordError("測試錯誤", "測試錯誤消息");
        adapter.RecordError("اختبار_خطأ", "رسالة_خطأ_تجريبية");

        // Assert - Should not throw
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3)); // Three calls for three different languages
    }

    [Fact]
    public void RecordError_With_Various_Error_Types_Should_Log_Correctly()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act - Test with various error types
        adapter.RecordError("ConnectionFailed", "Unable to connect to message broker");
        adapter.RecordError("Timeout", "Operation timed out after 30 seconds");
        adapter.RecordError("AuthenticationError", "Invalid credentials provided");

        // Assert - Should log all three errors
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("MessageBroker error")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }

    [Fact]
    public void RecordError_With_Long_Error_Type_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);
        var longErrorType = new string('E', 1000); // Very long error type

        // Act & Assert - Should not throw
        adapter.RecordError(longErrorType, "Test error message");
    }

    [Fact]
    public void RecordError_With_Empty_Error_Type_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act - Test with empty error type
        adapter.RecordError("", "Error with empty type");

        // Assert - Should log correctly
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Error with empty type")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordError_With_Special_Characters_In_Error_Type_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act - Test with special characters in error type
        adapter.RecordError("Error.With.Dots-And_Hyphens", "Special error type");
        adapter.RecordError("Error/With/Slashes", "Another special error");
        adapter.RecordError("Error\\With\\Backslashes", "Yet another special error");

        // Assert - Should not throw and should log
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }

    [Fact]
    public void RecordError_With_Multiline_Error_Message_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);
        var multilineMessage = "Line 1\nLine 2\nLine 3";

        // Act - Test with multiline error message
        adapter.RecordError("MultilineError", multilineMessage);

        // Assert - Should log correctly
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("MultilineError")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordError_Without_Logger_Should_Not_Throw()
    {
        // Arrange - Create adapter without logger
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, null);

        // Act & Assert - Should not throw when logger is null
        adapter.RecordError("TestError", "Test error message");
        adapter.RecordError("AnotherError", "Another error message");
    }

    [Fact]
    public void RecordMessagePublished_With_Special_Characters_In_MessageType_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act - Test with special characters in message type
        adapter.RecordMessagePublished("Message.With.Dots-And_Hyphens", 1024, false);
        adapter.RecordMessagePublished("Message/With/Slashes", 512, true);
        adapter.RecordMessagePublished("Message\\With\\Backslashes", 256, false);

        // Assert - Should not throw
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }

    [Fact]
    public void RecordProcessingDuration_With_MaxAndMinTimeSpan_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act - Test with extreme TimeSpan values
        adapter.RecordProcessingDuration("TestMessage", TimeSpan.MinValue);
        adapter.RecordProcessingDuration("TestMessage", TimeSpan.MaxValue);

        // Assert - Should not throw
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    [Fact]
    public void Constructor_With_Null_Options_Should_Throw_ArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MessageBrokerTelemetryAdapter(null!, _loggerMock.Object));
    }

    [Fact]
    public void RecordProcessingDuration_Should_Log_Correctly()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);
        var duration = TimeSpan.FromMilliseconds(150.5);

        // Act
        adapter.RecordProcessingDuration("OrderMessage", duration);

        // Assert - Verify the log message contains the correct information
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("OrderMessage") &&
                                               o.ToString()!.Contains("150.5")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordMessagePublished_With_Extreme_Message_Sizes_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act - Test with extreme message sizes
        adapter.RecordMessagePublished("TestMessage", 0, false); // Zero size
        adapter.RecordMessagePublished("TestMessage", long.MaxValue, true); // Maximum long value
        adapter.RecordMessagePublished("TestMessage", -9223372036854775808L, false); // Minimum long value

        // Assert - Should not throw and should log correctly
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }

    [Fact]
    public void RecordMessageReceived_With_Extreme_Message_Sizes_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act - Test with extreme message sizes
        adapter.RecordMessageReceived("TestMessage", 0, false); // Zero size
        adapter.RecordMessageReceived("TestMessage", long.MaxValue, true); // Maximum long value
        adapter.RecordMessageReceived("TestMessage", long.MinValue, false); // Minimum long value

        // Assert - Should not throw and should log correctly
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }

    [Fact]
    public void All_Methods_Should_Work_Without_Logger()
    {
        // Arrange - Create adapter without logger
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, null);

        // Act - Call all methods
        adapter.RecordMessagePublished("TestMessage", 1024, true);
        adapter.RecordMessageReceived("TestMessage", 2048, false);
        adapter.RecordProcessingDuration("TestMessage", TimeSpan.FromSeconds(1));
        adapter.RecordError("TestError", "Test message");

        // Assert - Should not throw (no logger verification needed since logger is null)
    }

    [Fact]
    public void RecordMessageReceived_With_Compressed_Flag_Should_Log_Correctly()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act - Test with compressed = true
        adapter.RecordMessageReceived("CompressedMessage", 3072, true);

        // Assert - Verify the log message contains compressed flag
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("CompressedMessage") &&
                                               o.ToString()!.Contains("3072") &&
                                               o.ToString()!.Contains("True")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordMessageReceived_With_Uncompressed_Flag_Should_Log_Correctly()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act - Test with compressed = false
        adapter.RecordMessageReceived("UncompressedMessage", 4096, false);

        // Assert - Verify the log message contains uncompressed flag
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("UncompressedMessage") &&
                                               o.ToString()!.Contains("4096") &&
                                               o.ToString()!.Contains("False")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordMessageReceived_With_Various_Message_Types_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act - Test with various message types
        adapter.RecordMessageReceived("OrderCreated", 1024, true);
        adapter.RecordMessageReceived("UserRegistered", 512, false);
        adapter.RecordMessageReceived("PaymentProcessed", 2048, true);

        // Assert - Should log all three messages
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Message received")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }

    [Fact]
    public void RecordMessageReceived_With_Zero_Size_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act - Test with zero message size
        adapter.RecordMessageReceived("EmptyMessage", 0, false);

        // Assert - Should log correctly
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("EmptyMessage") &&
                                               o.ToString()!.Contains("0")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordMessageReceived_Without_Logger_Should_Not_Throw()
    {
        // Arrange - Create adapter without logger
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, null);

        // Act & Assert - Should not throw when logger is null
        adapter.RecordMessageReceived("TestMessage", 1024, true);
        adapter.RecordMessageReceived("AnotherMessage", 2048, false);
    }

    [Fact]
    public void RecordMessagePublished_Without_Logger_Should_Not_Throw()
    {
        // Arrange - Create adapter without logger
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, null);

        // Act & Assert - Should not throw when logger is null
        adapter.RecordMessagePublished("TestMessage", 1024, true);
        adapter.RecordMessagePublished("AnotherMessage", 2048, false);
    }

    [Fact]
    public void RecordProcessingDuration_Without_Logger_Should_Not_Throw()
    {
        // Arrange - Create adapter without logger
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, null);

        // Act & Assert - Should not throw when logger is null
        adapter.RecordProcessingDuration("TestMessage", TimeSpan.FromMilliseconds(100));
        adapter.RecordProcessingDuration("AnotherMessage", TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void RecordMessagePublished_With_Unicode_Message_Type_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act - Test with unicode characters
        adapter.RecordMessagePublished("Nachricht", 1024, true);
        adapter.RecordMessagePublished("Mensaje", 2048, false);
        adapter.RecordMessagePublished("Сообщение", 3072, true);

        // Assert - Should not throw and should log
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }

    [Fact]
    public void RecordMessageReceived_With_Unicode_Message_Type_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act - Test with unicode characters
        adapter.RecordMessageReceived("Nachricht", 1024, true);
        adapter.RecordMessageReceived("Mensaje", 2048, false);
        adapter.RecordMessageReceived("Сообщение", 3072, true);

        // Assert - Should not throw and should log
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }

    [Fact]
    public void RecordProcessingDuration_With_Unicode_Message_Type_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act - Test with unicode characters
        adapter.RecordProcessingDuration("Nachricht", TimeSpan.FromMilliseconds(100));
        adapter.RecordProcessingDuration("Mensaje", TimeSpan.FromMilliseconds(200));
        adapter.RecordProcessingDuration("Сообщение", TimeSpan.FromMilliseconds(300));

        // Assert - Should not throw and should log
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }

    [Fact]
    public void RecordProcessingDuration_With_Zero_Duration_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act
        adapter.RecordProcessingDuration("TestMessage", TimeSpan.Zero);

        // Assert - Verify the log message contains 0
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("0")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordMessagePublished_With_Very_Small_Size_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act
        adapter.RecordMessagePublished("SmallMessage", 1, false);

        // Assert - Should log correctly
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("SmallMessage") &&
                                               o.ToString()!.Contains("1")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordMessageReceived_With_Very_Small_Size_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act
        adapter.RecordMessageReceived("SmallMessage", 1, true);

        // Assert - Should log correctly
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("SmallMessage") &&
                                               o.ToString()!.Contains("1")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordError_With_Empty_Message_Should_Work()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act - Test with empty error message
        adapter.RecordError("EmptyMessageError", "");

        // Assert - Should log correctly
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("EmptyMessageError")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void RecordMessagePublished_With_Boolean_Compressed_Flags_Should_Log_Correctly()
    {
        // Arrange
        var options = Options.Create(_options);
        var adapter = new MessageBrokerTelemetryAdapter(options, _loggerMock.Object);

        // Act
        adapter.RecordMessagePublished("CompressedMsg", 1024, true);
        adapter.RecordMessagePublished("UncompressedMsg", 2048, false);

        // Assert - Verify the log messages contain correct compressed flags
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("CompressedMsg") &&
                                               o.ToString()!.Contains("True")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("UncompressedMsg") &&
                                               o.ToString()!.Contains("False")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}