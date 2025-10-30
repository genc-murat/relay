using Microsoft.Extensions.Logging;
using Moq;
using Relay.MessageBroker.Security;
using Xunit;

namespace Relay.MessageBroker.Tests.Security;

public class SecurityEventLoggerTests
{
    private readonly Mock<ILogger<SecurityEventLogger>> _loggerMock;
    private readonly SecurityEventLogger _securityEventLogger;

    public SecurityEventLoggerTests()
    {
        _loggerMock = new Mock<ILogger<SecurityEventLogger>>();
        _securityEventLogger = new SecurityEventLogger(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithValidLogger_ShouldInitializeSuccessfully()
    {
        // Arrange & Act
        var logger = new SecurityEventLogger(_loggerMock.Object);

        // Assert
        Assert.NotNull(logger);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SecurityEventLogger(null!));
    }

    [Fact]
    public void LogUnauthorizedAccess_WithAllParameters_ShouldLogWarningWithCorrectMessage()
    {
        // Arrange
        var operation = "publish";
        var reason = "Insufficient permissions";
        var roles = new[] { "user", "guest" };
        var messageType = "order";

        // Act
        _securityEventLogger.LogUnauthorizedAccess(operation, reason, roles, messageType);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("SECURITY: Unauthorized publish attempt. Reason: Insufficient permissions. Roles: [user, guest]. MessageType: order")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogUnauthorizedAccess_WithNullMessageType_ShouldUseDefaultValue()
    {
        // Arrange
        var operation = "subscribe";
        var reason = "Invalid token";
        var roles = new[] { "user" };

        // Act
        _securityEventLogger.LogUnauthorizedAccess(operation, reason, roles, null);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("SECURITY: Unauthorized subscribe attempt. Reason: Invalid token. Roles: [user]. MessageType: N/A")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogUnauthorizedAccess_WithEmptyRoles_ShouldLogEmptyRoles()
    {
        // Arrange
        var operation = "publish";
        var reason = "No roles";
        var roles = Array.Empty<string>();

        // Act
        _securityEventLogger.LogUnauthorizedAccess(operation, reason, roles);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("SECURITY: Unauthorized publish attempt. Reason: No roles. Roles: []. MessageType: N/A")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogAuthorizedAccess_WithAllParameters_ShouldLogInformationWithCorrectMessage()
    {
        // Arrange
        var operation = "publish";
        var roles = new[] { "admin", "publisher" };
        var messageType = "news";

        // Act
        _securityEventLogger.LogAuthorizedAccess(operation, roles, messageType);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("SECURITY: Authorized publish. Roles: [admin, publisher]. MessageType: news")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogAuthorizedAccess_WithNullMessageType_ShouldUseDefaultValue()
    {
        // Arrange
        var operation = "subscribe";
        var roles = new[] { "subscriber" };

        // Act
        _securityEventLogger.LogAuthorizedAccess(operation, roles, null);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("SECURITY: Authorized subscribe. Roles: [subscriber]. MessageType: N/A")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogAuthorizedAccess_WithEmptyRoles_ShouldLogEmptyRoles()
    {
        // Arrange
        var operation = "publish";
        var roles = Array.Empty<string>();

        // Act
        _securityEventLogger.LogAuthorizedAccess(operation, roles);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("SECURITY: Authorized publish. Roles: []. MessageType: N/A")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogAuthenticationFailure_WithReason_ShouldLogWarningWithCorrectMessage()
    {
        // Arrange
        var reason = "Invalid signature";

        // Act
        _securityEventLogger.LogAuthenticationFailure(reason);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("SECURITY: Authentication failed. Reason: Invalid signature")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogAuthenticationSuccess_WithSubject_ShouldLogInformationWithCorrectMessage()
    {
        // Arrange
        var subject = "user123";

        // Act
        _securityEventLogger.LogAuthenticationSuccess(subject);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("SECURITY: Authentication successful for subject: user123")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogTokenValidationError_WithError_ShouldLogWarningWithCorrectMessage()
    {
        // Arrange
        var error = "Token expired";

        // Act
        _securityEventLogger.LogTokenValidationError(error);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("SECURITY: Token validation error. Error: Token expired")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void MultipleLogCalls_ShouldAllBeLoggedCorrectly()
    {
        // Arrange
        var roles = new[] { "user" };

        // Act
        _securityEventLogger.LogAuthenticationSuccess("user1");
        _securityEventLogger.LogUnauthorizedAccess("publish", "No permission", roles);
        _securityEventLogger.LogAuthenticationFailure("Invalid token");
        _securityEventLogger.LogTokenValidationError("Malformed token");

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(4));
    }
}