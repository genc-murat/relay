using System.Collections.Generic;

namespace Relay.Core.AI
{
    /// <summary>
    /// System health scoring.
    /// </summary>
    public sealed class SystemHealthScore
    {
        public double Overall { get; init; }
        public double Performance { get; init; }
        public double Reliability { get; init; }
        public double Scalability { get; init; }
        public double Security { get; init; }
        public double Maintainability { get; init; }
        
        /// <summary>
        /// Health status description
        /// </summary>
        public string Status { get; init; } = string.Empty;
        
        /// <summary>
        /// Areas needing immediate attention
        /// </summary>
        public List<string> CriticalAreas { get; init; } = new();
    }
}