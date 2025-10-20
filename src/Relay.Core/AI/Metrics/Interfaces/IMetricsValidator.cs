namespace Relay.Core.AI.Metrics.Interfaces
{
    /// <summary>
    /// Interface for validating AI model statistics.
    /// </summary>
    public interface IMetricsValidator
    {
        /// <summary>
        /// Validates the provided statistics.
        /// </summary>
        /// <param name="statistics">The statistics to validate.</param>
        /// <exception cref="System.ArgumentException">Thrown when validation fails.</exception>
        void Validate(AIModelStatistics statistics);
    }
}