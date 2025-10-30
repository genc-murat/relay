using Microsoft.Extensions.Logging;

namespace Relay.MessageBroker.Security;

/// <summary>
/// Logger for security-related events.
/// </summary>
public class SecurityEventLogger
{
    private readonly ILogger<SecurityEventLogger> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityEventLogger"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public SecurityEventLogger(ILogger<SecurityEventLogger> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Logs an unauthorized access attempt.
    /// </summary>
    /// <param name="operation">The operation that was attempted.</param>
    /// <param name="reason">The reason for denial.</param>
    /// <param name="roles">The roles of the user.</param>
    /// <param name="messageType">The message type (optional).</param>
    public void LogUnauthorizedAccess(
        string operation,
        string reason,
        IEnumerable<string> roles,
        string? messageType = null)
    {
        _logger.LogWarning(
            "SECURITY: Unauthorized {Operation} attempt. Reason: {Reason}. Roles: [{Roles}]. MessageType: {MessageType}",
            operation,
            reason,
            string.Join(", ", roles),
            messageType ?? "N/A");
    }

    /// <summary>
    /// Logs a successful authorization.
    /// </summary>
    /// <param name="operation">The operation that was authorized.</param>
    /// <param name="roles">The roles of the user.</param>
    /// <param name="messageType">The message type (optional).</param>
    public void LogAuthorizedAccess(
        string operation,
        IEnumerable<string> roles,
        string? messageType = null)
    {
        _logger.LogInformation(
            "SECURITY: Authorized {Operation}. Roles: [{Roles}]. MessageType: {MessageType}",
            operation,
            string.Join(", ", roles),
            messageType ?? "N/A");
    }

    /// <summary>
    /// Logs an authentication failure.
    /// </summary>
    /// <param name="reason">The reason for authentication failure.</param>
    public void LogAuthenticationFailure(string reason)
    {
        _logger.LogWarning(
            "SECURITY: Authentication failed. Reason: {Reason}",
            reason);
    }

    /// <summary>
    /// Logs a successful authentication.
    /// </summary>
    /// <param name="subject">The subject (user) that was authenticated.</param>
    public void LogAuthenticationSuccess(string subject)
    {
        _logger.LogInformation(
            "SECURITY: Authentication successful for subject: {Subject}",
            subject);
    }

    /// <summary>
    /// Logs a token validation error.
    /// </summary>
    /// <param name="error">The validation error.</param>
    public void LogTokenValidationError(string error)
    {
        _logger.LogWarning(
            "SECURITY: Token validation error. Error: {Error}",
            error);
    }
}
