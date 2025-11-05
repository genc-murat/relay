using System;
using Xunit;

namespace Relay.Core.Testing.Tests;

public class VerificationExceptionTests
{
    [Fact]
    public void VerificationException_ConstructsWithMessage()
    {
        // Arrange
        var message = "Test verification failed";

        // Act
        var exception = new VerificationException(message);

        // Assert
        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public void VerificationException_IsAssignableToException()
    {
        // Arrange
        var exception = new VerificationException("test");

        // Act & Assert
        Assert.IsAssignableFrom<Exception>(exception);
    }

    [Fact]
    public void VerificationException_InheritsFromException()
    {
        // Arrange
        var exception = new VerificationException("test");

        // Act & Assert
        Assert.IsAssignableFrom<Exception>(exception);
    }
}