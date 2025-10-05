namespace Relay.Core.AI
{
    /// <summary>
    /// AI optimization scenarios for preconfigured setups.
    /// </summary>
    public enum AIOptimizationScenario
    {
        /// <summary>Optimized for high throughput applications</summary>
        HighThroughput,
        
        /// <summary>Optimized for low latency requirements</summary>
        LowLatency,
        
        /// <summary>Optimized for resource-constrained environments</summary>
        ResourceConstrained,
        
        /// <summary>Optimized for development environments</summary>
        Development,
        
        /// <summary>Optimized for production environments</summary>
        Production
    }
}