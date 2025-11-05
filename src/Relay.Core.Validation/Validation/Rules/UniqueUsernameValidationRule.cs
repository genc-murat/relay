using Relay.Core.Validation.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Validation.Rules;

/// <summary>
/// Advanced validation rule that checks username uniqueness against a database.
/// Demonstrates validation rules with external dependencies (database lookups).
/// </summary>
public class UniqueUsernameValidationRule : IValidationRule<string>
{
    private readonly IUsernameUniquenessChecker _uniquenessChecker;

    public UniqueUsernameValidationRule(IUsernameUniquenessChecker uniquenessChecker)
    {
        _uniquenessChecker = uniquenessChecker ?? throw new ArgumentNullException(nameof(uniquenessChecker));
    }

    public async ValueTask<IEnumerable<string>> ValidateAsync(
        string request,
        CancellationToken cancellationToken = default)
    {
        // Check for cancellation before doing any work
        cancellationToken.ThrowIfCancellationRequested();

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request))
        {
            return errors; // Let other rules handle empty validation
        }

        try
        {
            var isUnique = await _uniquenessChecker.IsUsernameUniqueAsync(request, cancellationToken);

            if (!isUnique)
            {
                errors.Add("Username is already taken. Please choose a different username.");
            }
        }
        catch (OperationCanceledException)
        {
            // OperationCanceledException (including TaskCanceledException) that reaches this point 
            // could be due to our token being cancelled or the checker's own cancellation
            // If our token is cancelled, we re-throw; otherwise it's from the checker
            if (cancellationToken.IsCancellationRequested)
            {
                // This exception is related to our cancellation token
                throw;
            }
            else
            {
                // Exception from checker, not due to our token - treat as service error
                errors.Add("Unable to verify username uniqueness. Please try again later.");
            }
        }
        catch (Exception)
        {
            // For any other exception (timeouts, network issues, etc.), 
            // return an error message instead of throwing
            errors.Add("Unable to verify username uniqueness. Please try again later.");
        }

        return errors;
    }
}

