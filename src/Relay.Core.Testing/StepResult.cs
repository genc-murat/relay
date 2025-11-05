using System;

namespace Relay.Core.Testing
{
    public class StepResult
    {
        public string StepName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
        public object? Response { get; set; }
    }
}