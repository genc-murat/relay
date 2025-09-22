namespace Relay.Core.Configuration
{
    /// <summary>
    /// Configuration options for handler versioning.
    /// </summary>
    public class HandlerVersioningOptions
    {
        /// <summary>
        /// Gets or sets whether to enable automatic handler versioning.
        /// </summary>
        public bool EnableAutomaticVersioning { get; set; } = false;

        /// <summary>
        /// Gets or sets the default version to use when no version is specified.
        /// </summary>
        public string DefaultVersion { get; set; } = "1.0";

        /// <summary>
        /// Gets or sets whether to throw an exception when a requested version is not found.
        /// </summary>
        public bool ThrowOnVersionNotFound { get; set; } = true;

        /// <summary>
        /// Gets or sets the version selection strategy.
        /// </summary>
        public VersionSelectionStrategy VersionSelectionStrategy { get; set; } = VersionSelectionStrategy.ExactMatch;
    }

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
}