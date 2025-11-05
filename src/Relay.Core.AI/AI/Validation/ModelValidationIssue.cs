namespace Relay.Core.AI
{
    public class ModelValidationIssue
    {
        public ModelIssueType Type { get; set; }
        public ValidationSeverity Severity { get; set; }
        public string Description { get; set; } = string.Empty;
        public string RecommendedAction { get; set; } = string.Empty;
    }
}