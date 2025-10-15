using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Validation.Rules;

/// <summary>
/// Base class for custom validation rules that provides helper methods for common validation patterns.
/// </summary>
/// <typeparam name="TRequest">The type of request to validate.</typeparam>
public abstract class CustomValidationRuleBase<TRequest> : IValidationRuleConfiguration<TRequest>
{
    /// <summary>
    /// Validates the request asynchronously.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A collection of validation errors, empty if valid.</returns>
    public async ValueTask<IEnumerable<string>> ValidateAsync(TRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        await ValidateCoreAsync(request, errors, cancellationToken);
        return errors;
    }

    /// <summary>
    /// Core validation logic to be implemented by derived classes.
    /// </summary>
    /// <param name="request">The request to validate.</param>
    /// <param name="errors">The list to add validation errors to.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task representing the asynchronous validation operation.</returns>
    protected abstract ValueTask ValidateCoreAsync(TRequest request, List<string> errors, CancellationToken cancellationToken);

    /// <summary>
    /// Adds an error message to the errors list if the condition is true.
    /// </summary>
    /// <param name="condition">The condition to check.</param>
    /// <param name="errorMessage">The error message to add if condition is true.</param>
    /// <param name="errors">The list to add the error to.</param>
    protected static void AddErrorIf(bool condition, string errorMessage, List<string> errors)
    {
        if (condition)
        {
            errors.Add(errorMessage);
        }
    }

    /// <summary>
    /// Adds an error message to the errors list if the condition function returns true.
    /// </summary>
    /// <param name="condition">The condition function to evaluate.</param>
    /// <param name="errorMessage">The error message to add if condition is true.</param>
    /// <param name="errors">The list to add the error to.</param>
    protected static void AddErrorIf(Func<bool> condition, string errorMessage, List<string> errors)
    {
        if (condition())
        {
            errors.Add(errorMessage);
        }
    }

    /// <summary>
    /// Adds an error message to the errors list if the condition function returns true.
    /// </summary>
    /// <param name="condition">The condition function to evaluate with the request.</param>
    /// <param name="errorMessage">The error message to add if condition is true.</param>
    /// <param name="errors">The list to add the error to.</param>
    protected static void AddErrorIf(Func<TRequest, bool> condition, TRequest request, string errorMessage, List<string> errors)
    {
        if (condition(request))
        {
            errors.Add(errorMessage);
        }
    }
}