using System;
using Xunit;

namespace Relay.Core.Testing.Tests;

public class AssertionExceptionTests
{
    [Fact]
    public void AssertionException_ConstructsWithMessage()
    {
        // Arrange
        var message = "Test assertion failed";

        // Act
        var exception = new AssertionException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void AssertionException_ConstructsWithMessageAndInnerException()
    {
        // Arrange
        var message = "Test assertion failed";
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception = new AssertionException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void AssertionException_IsAssignableToException()
    {
        // Arrange
        var exception = new AssertionException("test");

        // Act & Assert
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void AssertionException_InheritsFromException()
    {
        // Arrange
        var exception = new AssertionException("test");

        // Act & Assert
        Assert.IsAssignableFrom<Exception>(exception);
    }
}