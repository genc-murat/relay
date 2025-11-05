using System;
using System.Collections.Generic;

namespace Relay.Core.Testing
{
    public class LoadTestResult
    {
        public string RequestType { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public LoadTestConfiguration Configuration { get; set; } = new();
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public List<double> ResponseTimes { get; set; } = new();
        public double AverageResponseTime { get; set; }
        public double MedianResponseTime { get; set; }
        public double P95ResponseTime { get; set; }
        public double P99ResponseTime { get; set; }
        public double RequestsPerSecond => TotalDuration.TotalSeconds > 0 ? (SuccessfulRequests + FailedRequests) / TotalDuration.TotalSeconds : 0;
        public double SuccessRate => (SuccessfulRequests + FailedRequests) > 0 ? (double)SuccessfulRequests / (SuccessfulRequests + FailedRequests) : 0;
    }
}