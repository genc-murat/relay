using System.Collections.Generic;

namespace Relay.Core.Testing;

public class TestActivity
{
    public string OperationName { get; set; } = string.Empty;
    public Dictionary<string, string> Tags { get; set; } = new();
}
