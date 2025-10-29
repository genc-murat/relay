using System.Diagnostics;

namespace Relay.Core.ContractValidation.Observability;

/// <summary>
/// Provides ActivitySource for contract validation operations.
/// </summary>
public static class ContractValidationActivitySource
{
    /// <summary>
    /// The name of the activity source.
    /// </summary>
    public const string SourceName = "Relay.Core.ContractValidation";

    /// <summary>
    /// The version of the activity source.
    /// </summary>
    public const string Version = "1.0.0";

    /// <summary>
    /// The ActivitySource instance for contract validation.
    /// </summary>
    public static readonly ActivitySource Instance = new(SourceName, Version);
}
