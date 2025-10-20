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
        catch (Exception)
        {
            // Log the error but don't fail validation - allow registration to proceed
            // In production, you might want to have a fallback behavior
            errors.Add("Unable to verify username uniqueness. Please try again later.");
        }

        return errors;
    }
}

