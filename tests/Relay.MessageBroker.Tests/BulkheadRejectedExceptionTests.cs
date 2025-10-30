using Relay.MessageBroker.Bulkhead;
using Xunit;

namespace Relay.MessageBroker.Tests;

public class BulkheadRejectedExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Arrange
        var message = "Bulkhead is full";

        // Act
        var exception = new BulkheadRejectedException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(0, exception.ActiveOperations);
        Assert.Equal(0, exception.QueuedOperations);
    }

    [Fact]
    public void Constructor_WithMessageAndOperations_ShouldSetProperties()
    {
        // Arrange
        var message = "Bulkhead is full";
        var activeOperations = 5;
        var queuedOperations = 10;

        // Act
        var exception = new BulkheadRejectedException(message, activeOperations, queuedOperations);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(activeOperations, exception.ActiveOperations);
        Assert.Equal(queuedOperations, exception.QueuedOperations);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetProperties()
    {
        // Arrange
        var message = "Bulkhead is full";
        var innerException = new Exception("Inner exception");

        // Act
        var exception = new BulkheadRejectedException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
        Assert.Equal(0, exception.ActiveOperations);
        Assert.Equal(0, exception.QueuedOperations);
    }


}