namespace Relay.Core.Configuration;

/// <summary>
/// Strategies for version selection.
/// </summary>
public enum VersionSelectionStrategy
{
    /// <summary>
    /// Exact match version selection.
    /// </summary>
    ExactMatch,

    /// <summary>
    /// Latest compatible version selection.
    /// </summary>
    LatestCompatible,

    /// <summary>
    /// Latest version selection.
    /// </summary>
    Latest
}