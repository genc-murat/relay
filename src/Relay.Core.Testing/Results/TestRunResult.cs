using System;
using System.Collections.Generic;
using System.Linq;

namespace Relay.Core.Testing
{
    public class TestRunResult
    {
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public List<ScenarioResult> ScenarioResults { get; set; } = new();
        public bool Success => ScenarioResults.All(s => s.Success);
    }
}