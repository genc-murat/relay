using System.Collections.Generic;

namespace Relay.Core.AI
{
    public class StrategyValidationResult
    {
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}