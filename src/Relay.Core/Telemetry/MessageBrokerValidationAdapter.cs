using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Relay.Core.ContractValidation;
using Relay.Core.Validation;
using Relay.Core.Validation.Interfaces;

namespace Relay.Core.Telemetry;

/// <summary>
/// Adapter that provides validation capabilities for MessageBroker using Relay.Core's validation system.
/// </summary>
public class MessageBrokerValidationAdapter
{
    private readonly IContractValidator? _contractValidator;
    private readonly ILogger<MessageBrokerValidationAdapter> _logger;
    private readonly UnifiedTelemetryOptions _options;

    /// <summary>
    /// Initializes a new instance of the MessageBrokerValidationAdapter class.
    /// </summary>
    /// <param name="contractValidator">Optional contract validator for schema validation.</param>
    /// <param name="logger">Logger for the adapter.</param>
    /// <param name="options">Telemetry options.</param>
    public MessageBrokerValidationAdapter(
        IContractValidator? contractValidator,
        ILogger<MessageBrokerValidationAdapter> logger,
        IOptions<UnifiedTelemetryOptions> options)
    {
        _contractValidator = contractValidator;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Validates a message using the core validation system.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to validate.</typeparam>
    /// <param name="message">The message to validate.</param>
    /// <param name="validator">Optional validator for the message type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if validation passes, false otherwise.</returns>
    public async ValueTask<bool> ValidateMessageAsync<TMessage>(
        TMessage message,
        IValidator<TMessage>? validator = null,
        CancellationToken cancellationToken = default)
    {
        if (message == null)
        {
            _logger.LogWarning("Message validation failed: message is null");
            return false;
        }

        try
        {
            // Use provided validator if available
            if (validator != null)
            {
                var errors = await validator.ValidateAsync(message, cancellationToken);
                if (errors != null)
                {
                    foreach (var error in errors)
                    {
                        _logger.LogWarning("Message validation error: {Error}", error);
                    }
                    return !errors.GetEnumerator().MoveNext(); // Return false if there are errors
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Message validation failed with exception");
            return false;
        }
    }

    /// <summary>
    /// Validates a message against a JSON schema contract.
    /// </summary>
    /// <param name="message">The message to validate.</param>
    /// <param name="schema">The JSON schema to validate against.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if validation passes, false otherwise.</returns>
    public async ValueTask<bool> ValidateMessageAgainstSchemaAsync(
        object message,
        JsonSchemaContract schema,
        CancellationToken cancellationToken = default)
    {
        if (message == null)
        {
            _logger.LogWarning("Schema validation failed: message is null");
            return false;
        }

        if (schema == null)
        {
            _logger.LogWarning("Schema validation failed: schema is null");
            return false;
        }

        if (_contractValidator == null)
        {
            _logger.LogWarning("Schema validation failed: no contract validator available");
            return false;
        }

        try
        {
            var errors = await _contractValidator.ValidateRequestAsync(message, schema, cancellationToken);
            if (errors != null)
            {
                foreach (var error in errors)
                {
                    _logger.LogWarning("Schema validation error: {Error}", error);
                }
                return !errors.GetEnumerator().MoveNext(); // Return false if there are errors
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Schema validation failed with exception");
            return false;
        }
    }

    /// <summary>
    /// Validates a message against a JSON schema contract and returns validation errors.
    /// </summary>
    /// <param name="message">The message to validate.</param>
    /// <param name="schema">The JSON schema to validate against.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of validation errors, empty if valid.</returns>
    public async ValueTask<IEnumerable<string>> ValidateMessageSchemaAsync(
        object message,
        JsonSchemaContract schema,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        if (message == null)
        {
            errors.Add("Message cannot be null");
            _logger.LogWarning("Schema validation failed: message is null");
            return errors;
        }

        if (schema == null)
        {
            errors.Add("Schema cannot be null");
            _logger.LogWarning("Schema validation failed: schema is null");
            return errors;
        }

        if (_contractValidator == null)
        {
            _logger.LogWarning("Schema validation skipped: no contract validator available");
            return errors; // Return empty errors - validation is optional
        }

        try
        {
            var validationErrors = await _contractValidator.ValidateRequestAsync(message, schema, cancellationToken);
            if (validationErrors != null && validationErrors.Any())
            {
                errors.AddRange(validationErrors);
                foreach (var error in validationErrors)
                {
                    _logger.LogWarning("Schema validation error: {Error}", error);
                }
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Schema validation failed with exception: {ex.Message}");
            _logger.LogError(ex, "Schema validation failed with exception");
        }

        return errors;
    }

    /// <summary>
    /// Validates basic message fields (common validation for all message brokers).
    /// </summary>
    /// <param name="messageType">The message type name.</param>
    /// <param name="messageData">The message data.</param>
    /// <param name="messageId">The message ID for logging.</param>
    /// <returns>True if basic validation passes, false otherwise.</returns>
    public bool ValidateBasicMessageFields(
        string? messageType,
        object? messageData,
        string? messageId = null)
    {
        var messageIdLog = messageId ?? "unknown";

        if (string.IsNullOrWhiteSpace(messageType))
        {
            _logger.LogWarning("Message {MessageId} missing 'type' field", messageIdLog);
            return false;
        }

        if (messageData == null)
        {
            _logger.LogWarning("Message {MessageId} missing 'data' field", messageIdLog);
            return false;
        }

        return true;
    }
}