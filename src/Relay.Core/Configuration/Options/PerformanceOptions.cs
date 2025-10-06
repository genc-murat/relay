namespace Relay.Core.Configuration.Options;

/// <summary>
/// Performance optimization configuration options for Relay
/// </summary>
public class PerformanceOptions
{
    /// <summary>
    /// Gets or sets whether to enable aggressive inlining optimizations
    /// </summary>
    public bool EnableAggressiveInlining { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to cache dispatchers at initialization
    /// </summary>
    public bool CacheDispatchers { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use pre-allocated exception tasks for common errors
    /// </summary>
    public bool UsePreAllocatedExceptions { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to enable handler caching for ultra-fast resolution
    /// </summary>
    public bool EnableHandlerCache { get; set; } = true;

    /// <summary>
    /// Gets or sets the handler cache maximum size (0 = unlimited)
    /// </summary>
    public int HandlerCacheMaxSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets whether to enable SIMD optimizations (requires hardware support)
    /// </summary>
    public bool EnableSIMDOptimizations { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable memory prefetching for better cache performance
    /// </summary>
    public bool EnableMemoryPrefetch { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to use struct-based implementations where possible (experimental)
    /// </summary>
    public bool PreferStructImplementations { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to optimize for AOT compilation scenarios
    /// AOT (Ahead-of-Time) compilation avoids JIT overhead and enables faster startup
    /// </summary>
    public bool OptimizeForAOT { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to enable zero-allocation fast paths
    /// </summary>
    public bool EnableZeroAllocationPaths { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use frozen collections for better read performance
    /// </summary>
    public bool UseFrozenCollections { get; set; } = true;

    /// <summary>
    /// Gets or sets the performance profile to use
    /// </summary>
    public PerformanceProfile Profile { get; set; } = PerformanceProfile.Balanced;
}
