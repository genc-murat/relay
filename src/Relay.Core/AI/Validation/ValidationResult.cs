using System;

namespace Relay.Core.AI
{
    // Supporting types for validation framework
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public ValidationSeverity Severity { get; set; }
        public string[] Errors { get; set; } = Array.Empty<string>();
        public string[] Warnings { get; set; } = Array.Empty<string>();
        public DateTime ValidationTime { get; set; }
        public OptimizationStrategy ValidatedStrategy { get; set; }
    }
}