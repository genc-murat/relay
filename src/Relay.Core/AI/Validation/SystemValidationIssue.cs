using System;

namespace Relay.Core.AI
{
    public class SystemValidationIssue
    {
        public string Component { get; set; } = string.Empty;
        public ValidationSeverity Severity { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
        public string[] RecommendedActions { get; set; } = Array.Empty<string>();
    }
}