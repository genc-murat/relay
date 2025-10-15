using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Relay.Core.Contracts.Core;
using Relay.Core.Validation.Interfaces;

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

/// <summary>
/// Interface for checking username uniqueness.
/// This abstraction allows for different implementations (database, cache, external service).
/// </summary>
public interface IUsernameUniquenessChecker
{
    /// <summary>
    /// Checks if the given username is unique.
    /// </summary>
    /// <param name="username">The username to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if username is unique, false otherwise</returns>
    ValueTask<bool> IsUsernameUniqueAsync(string username, CancellationToken cancellationToken = default);
}

/// <summary>
/// Database implementation of username uniqueness checker.
/// </summary>
public class DatabaseUsernameUniquenessChecker : IUsernameUniquenessChecker
{
    private readonly DbConnection _connection;

    public DatabaseUsernameUniquenessChecker(DbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async ValueTask<bool> IsUsernameUniqueAsync(string username, CancellationToken cancellationToken = default)
    {
        // In a real implementation, you would use Dapper, EF Core, or ADO.NET
        // This is a simplified example
        await using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Users WHERE Username = @username AND IsActive = 1";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@username";
        parameter.Value = username;
        command.Parameters.Add(parameter);

        await _connection.OpenAsync(cancellationToken);
        try
        {
            var count = (long)(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
            return count == 0;
        }
        finally
        {
            await _connection.CloseAsync();
        }
    }
}

/// <summary>
/// Cached implementation that uses both cache and database.
/// </summary>
public class CachedUsernameUniquenessChecker : IUsernameUniquenessChecker
{
    private readonly IUsernameUniquenessChecker _innerChecker;
    private readonly ICache _cache;

    public CachedUsernameUniquenessChecker(
        IUsernameUniquenessChecker innerChecker,
        ICache cache)
    {
        _innerChecker = innerChecker ?? throw new ArgumentNullException(nameof(innerChecker));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async ValueTask<bool> IsUsernameUniqueAsync(string username, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"username_unique_{username}";

        // Try cache first
        var cached = await _cache.GetAsync<bool?>(cacheKey, cancellationToken);
        if (cached.HasValue)
        {
            return cached.Value;
        }

        // Check database
        var isUnique = await _innerChecker.IsUsernameUniqueAsync(username, cancellationToken);

        // Cache result for 5 minutes
        await _cache.SetAsync(cacheKey, isUnique, TimeSpan.FromMinutes(5), cancellationToken);

        return isUnique;
    }
}

