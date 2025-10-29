using System.Collections.Generic;

namespace Relay.SourceGenerator.Configuration;

/// <summary>
/// Equality comparer for RelayConfiguration to support incremental generation caching.
/// </summary>
public sealed class RelayConfigurationComparer : IEqualityComparer<RelayConfiguration>
{
    /// <summary>
    /// Singleton instance of the comparer.
    /// </summary>
    public static RelayConfigurationComparer Instance { get; } = new RelayConfigurationComparer();

    private RelayConfigurationComparer()
    {
    }

    public bool Equals(RelayConfiguration? x, RelayConfiguration? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        return x.Equals(y);
    }

    public int GetHashCode(RelayConfiguration obj)
    {
        return obj?.GetHashCode() ?? 0;
    }
}
