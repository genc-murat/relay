using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.ContractValidation.Models;
using Relay.Core.Metadata.MessageQueue;

namespace Relay.Core.ContractValidation.Testing;

/// <summary>
/// Mock implementation of IContractValidator for testing purposes.
/// Allows configuring validation behavior and capturing validation calls.
/// </summary>
public sealed class MockContractValidator : IContractValidator
{
    private readonly List<ValidationCall> _calls = new();
    private Func<object, JsonSchemaContract, ValidationResult>? _requestValidationFunc;
    private Func<object, JsonSchemaContract, ValidationResult>? _responseValidationFunc;

    /// <summary>
    /// Gets all validation calls made to this mock.
    /// </summary>
    public IReadOnlyList<ValidationCall> Calls => _calls.AsReadOnly();

    /// <summary>
    /// Gets the number of times ValidateRequestAsync was called.
    /// </summary>
    public int RequestValidationCallCount => _calls.Count(c => c.IsRequest);

    /// <summary>
    /// Gets the number of times ValidateResponseAsync was called.
    /// </summary>
    public int ResponseValidationCallCount => _calls.Count(c => !c.IsRequest);

    /// <summary>
    /// Configures the mock to return specific validation results for requests.
    /// </summary>
    /// <param name="validationFunc">Function that returns validation results.</param>
    public void SetupRequestValidation(Func<object, JsonSchemaContract, ValidationResult> validationFunc)
    {
        _requestValidationFunc = validationFunc;
    }

    /// <summary>
    /// Configures the mock to return specific validation results for responses.
    /// </summary>
    /// <param name="validationFunc">Function that returns validation results.</param>
    public void SetupResponseValidation(Func<object, JsonSchemaContract, ValidationResult> validationFunc)
    {
        _responseValidationFunc = validationFunc;
    }

    /// <summary>
    /// Configures the mock to always return success for requests.
    /// </summary>
    public void SetupRequestSuccess()
    {
        _requestValidationFunc = (_, _) => ValidationResult.Success();
    }

    /// <summary>
    /// Configures the mock to always return success for responses.
    /// </summary>
    public void SetupResponseSuccess()
    {
        _responseValidationFunc = (_, _) => ValidationResult.Success();
    }

    /// <summary>
    /// Configures the mock to always return failure for requests.
    /// </summary>
    /// <param name="errorCode">The error code to return.</param>
    /// <param name="errorMessage">The error message to return.</param>
    public void SetupRequestFailure(string errorCode, string errorMessage)
    {
        _requestValidationFunc = (_, _) => ValidationResult.Failure(errorCode, errorMessage);
    }

    /// <summary>
    /// Configures the mock to always return failure for responses.
    /// </summary>
    /// <param name="errorCode">The error code to return.</param>
    /// <param name="errorMessage">The error message to return.</param>
    public void SetupResponseFailure(string errorCode, string errorMessage)
    {
        _responseValidationFunc = (_, _) => ValidationResult.Failure(errorCode, errorMessage);
    }

    /// <summary>
    /// Resets all recorded calls and configurations.
    /// </summary>
    public void Reset()
    {
        _calls.Clear();
        _requestValidationFunc = null;
        _responseValidationFunc = null;
    }

    /// <summary>
    /// Verifies that ValidateRequestAsync was called with the specified object.
    /// </summary>
    /// <param name="request">The expected request object.</param>
    /// <returns>True if the request was validated, false otherwise.</returns>
    public bool VerifyRequestValidated(object request)
    {
        return _calls.Any(c => c.IsRequest && ReferenceEquals(c.Object, request));
    }

    /// <summary>
    /// Verifies that ValidateResponseAsync was called with the specified object.
    /// </summary>
    /// <param name="response">The expected response object.</param>
    /// <returns>True if the response was validated, false otherwise.</returns>
    public bool VerifyResponseValidated(object response)
    {
        return _calls.Any(c => !c.IsRequest && ReferenceEquals(c.Object, response));
    }

    /// <summary>
    /// Gets the last validation call made to this mock.
    /// </summary>
    /// <returns>The last validation call, or null if no calls were made.</returns>
    public ValidationCall? GetLastCall()
    {
        return _calls.LastOrDefault();
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<string>> ValidateRequestAsync(
        object request,
        JsonSchemaContract schema,
        CancellationToken cancellationToken = default)
    {
        var call = new ValidationCall(request, schema, true, DateTime.UtcNow);
        _calls.Add(call);

        var result = _requestValidationFunc?.Invoke(request, schema) ?? ValidationResult.Success();
        var errors = result.Errors.Select(e => e.Message);

        return ValueTask.FromResult(errors);
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<string>> ValidateResponseAsync(
        object response,
        JsonSchemaContract schema,
        CancellationToken cancellationToken = default)
    {
        var call = new ValidationCall(response, schema, false, DateTime.UtcNow);
        _calls.Add(call);

        var result = _responseValidationFunc?.Invoke(response, schema) ?? ValidationResult.Success();
        var errors = result.Errors.Select(e => e.Message);

        return ValueTask.FromResult(errors);
    }

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateRequestDetailedAsync(
        object request,
        JsonSchemaContract schema,
        ValidationContext context,
        CancellationToken cancellationToken = default)
    {
        var call = new ValidationCall(request, schema, true, DateTime.UtcNow);
        _calls.Add(call);

        var result = _requestValidationFunc?.Invoke(request, schema) ?? ValidationResult.Success();
        return ValueTask.FromResult(result);
    }

    /// <inheritdoc />
    public ValueTask<ValidationResult> ValidateResponseDetailedAsync(
        object response,
        JsonSchemaContract schema,
        ValidationContext context,
        CancellationToken cancellationToken = default)
    {
        var call = new ValidationCall(response, schema, false, DateTime.UtcNow);
        _calls.Add(call);

        var result = _responseValidationFunc?.Invoke(response, schema) ?? ValidationResult.Success();
        return ValueTask.FromResult(result);
    }

    /// <summary>
    /// Represents a validation call made to the mock validator.
    /// </summary>
    public sealed class ValidationCall
    {
        /// <summary>
        /// Initializes a new instance of the ValidationCall class.
        /// </summary>
        public ValidationCall(object obj, JsonSchemaContract schema, bool isRequest, DateTime timestamp)
        {
            Object = obj;
            Schema = schema;
            IsRequest = isRequest;
            Timestamp = timestamp;
        }

        /// <summary>
        /// Gets the object that was validated.
        /// </summary>
        public object Object { get; }

        /// <summary>
        /// Gets the schema used for validation.
        /// </summary>
        public JsonSchemaContract Schema { get; }

        /// <summary>
        /// Gets a value indicating whether this was a request validation.
        /// </summary>
        public bool IsRequest { get; }

        /// <summary>
        /// Gets the timestamp when the validation was called.
        /// </summary>
        public DateTime Timestamp { get; }
    }
}
