namespace Relay
{
    /// <summary>
    /// Relay configuration scenarios for optimized service registration.
    /// </summary>
    public enum RelayScenario
    {
        /// <summary>
        /// Minimal Relay setup with only core services.
        /// </summary>
        Minimal,

        /// <summary>
        /// Optimized for Web API applications with validation, telemetry, and exception handling.
        /// </summary>
        WebApi,

        /// <summary>
        /// High-performance scenario with AI optimization and performance monitoring.
        /// </summary>
        HighPerformance,

        /// <summary>
        /// Event-driven architecture with transactions and full pipeline support.
        /// </summary>
        EventDriven,

        /// <summary>
        /// Microservices architecture with full feature set including AI optimization.
        /// </summary>
        Microservices
    }
}