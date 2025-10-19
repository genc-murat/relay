using System;
using System.Collections.Generic;

namespace Relay.Core.Testing
{
    public class ScenarioResult
    {
        public string ScenarioName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool Success { get; set; } = true;
        public string? Error { get; set; }
        public List<StepResult> StepResults { get; set; } = new();
    }
}