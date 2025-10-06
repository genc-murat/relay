namespace Relay.CLI.Commands;

public class HandlerInfo
{
    public string Name { get; set; } = "";
    public string FilePath { get; set; } = "";
    public bool IsAsync { get; set; }
    public bool HasDependencies { get; set; }
    public bool UsesValueTask { get; set; }
    public bool HasCancellationToken { get; set; }
    public bool HasLogging { get; set; }
    public bool HasValidation { get; set; }
    public int LineCount { get; set; }
}


