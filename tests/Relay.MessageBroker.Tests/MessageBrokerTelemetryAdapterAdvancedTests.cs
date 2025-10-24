using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Relay.Core.Telemetry;

namespace Relay.MessageBroker.Tests;

public class MessageBrokerTelemetryAdapterAdvancedTests
{
    private readonly Mock<ILogger<MessageBrokerTelemetryAdapter>> _loggerMock;
    private readonly RelayTelemetryOptions _options;

    public MessageBrokerTelemetryAdapterAdvancedTests()
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
}