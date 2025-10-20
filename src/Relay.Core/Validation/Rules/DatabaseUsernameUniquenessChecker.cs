using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Validation.Rules;

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

