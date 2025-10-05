using System;

namespace Relay.Core.AI
{
    /// <summary>
    /// Connection breakdown by type
    /// </summary>
    internal class ConnectionBreakdown
    {
        public DateTime Timestamp { get; set; }
        public int TotalConnections { get; set; }
        public int HttpConnections { get; set; }
        public int DatabaseConnections { get; set; }
        public int ExternalServiceConnections { get; set; }
        public int WebSocketConnections { get; set; }
        public int ActiveRequestConnections { get; set; }
        public double ThreadPoolUtilization { get; set; }
        public double DatabasePoolUtilization { get; set; }
    }
}
