namespace Relay.CLI.Commands.Models;

public class TestConfiguration
{
    public int Iterations { get; set; }
    public int WarmupIterations { get; set; }
    public int Threads { get; set; }
    public DateTime Timestamp { get; set; }
    public string MachineName { get; set; } = "";
    public int ProcessorCount { get; set; }
    public string RuntimeVersion { get; set; } = "";
}
