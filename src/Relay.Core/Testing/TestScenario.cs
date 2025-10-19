using System.Collections.Generic;

namespace Relay.Core.Testing
{
    // Supporting classes and enums for the test framework
    public class TestScenario
    {
        public string Name { get; set; } = string.Empty;
        public List<TestStep> Steps { get; set; } = new();
    }
}