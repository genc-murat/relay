using System;
using System.Threading;
using System.Threading.Tasks;

namespace Relay.Core.Validation.Rules;

/// <summary>
/// Simple cache interface (same as before).
/// </summary>
public interface ICache
{
    ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    ValueTask SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default);
}